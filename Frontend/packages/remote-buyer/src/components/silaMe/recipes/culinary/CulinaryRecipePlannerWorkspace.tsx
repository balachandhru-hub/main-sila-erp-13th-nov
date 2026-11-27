import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Loader, toastService } from "@vosox/shared-ui";
import {
  cancelCulinaryGeneration,
  cancelCulinaryRecipeDraft,
  duplicateCulinaryRecipeDraft,
  exportCulinaryRecipeDraft,
  extractCulinaryRecipeDocument,
  generateCulinaryRecipe,
  getCulinaryPlannerContext,
  getCulinaryRecipeDraft,
  listCulinaryRecipeDrafts,
  prepareCulinaryDishImage,
  refineCulinaryRecipe,
  regenerateCulinaryRecipeSection,
  restoreCulinaryRecipeDraftRevision,
  saveCulinaryRecipeDraft,
  scaleCulinaryRecipeV2,
  type CulinaryConversationTurn,
  type CulinaryDishImagePrepared,
  type CulinaryDocumentExtract,
  type CulinaryGenerationMode,
  type CulinaryIngredientLine,
  type CulinaryInstructionStep,
  type CulinaryPlannerContext,
  type CulinaryRecipeDraftDetail,
  type CulinaryRecipeDraftSummary,
  type CulinaryRefineResponse,
  type CulinaryScaleV2Result,
  type CulinaryStructuredRecipePayload,
  type CulinaryValidationWarning,
} from "../../../../api/silaMe/silaCulinaryRecipesApi";
import {
  calculateRecipeScaling,
  DEMO_SALMON_RECIPE,
  type DemoRecipeIngredient,
  type ScalingRule,
} from "./culinarySimulatorsMath";
import {
  compareRecipePayloads,
  createBlankIngredient,
  createBlankStep,
  scaleDraftLocally,
  triggerBlobDownload,
} from "./culinaryRecipePlannerHelpers";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import "./CulinarySuite.css";

interface CulinaryRecipePlannerWorkspaceProps {
  onOpenWorkspace: (target: CulinaryWorkspaceKind) => void;
  onContinueToMaterialMapping?: (draftId: string) => void;
}

const MODE_OPTIONS: { id: CulinaryGenerationMode; label: string; hint: string }[] = [
  { id: "DISH_NAME", label: "Mode A · Dish Name", hint: "Enter a dish name to generate a full recipe" },
  { id: "DETAILED_INSTRUCTIONS", label: "Mode B · Instructions", hint: "Provide detailed chef instructions & portion notes" },
  { id: "EXISTING_RECIPE", label: "Mode C · Existing Recipe", hint: "Propose a variation without altering the original" },
  { id: "AVAILABLE_INGREDIENTS", label: "Mode D · Kitchen Stock", hint: "Suggest recipes using available authorized stock" },
  { id: "DOCUMENT", label: "Mode E · Document", hint: "Upload PDF, DOCX, XLSX, or TXT to structure" },
  { id: "DISH_IMAGE", label: "Mode F · Dish Photo", hint: "Upload a dish photo for inferred inspiration" },
  { id: "BUDGET", label: "Mode G · Budget Target", hint: "Target ingredient budget or cost per serving" },
];

const QUICK_REFINEMENTS = [
  "Make this recipe less spicy",
  "Replace chicken with paneer",
  "Increase the recipe to 50 portions",
  "Remove garlic",
  "Use less butter",
  "Separate the sauce as a sub-recipe",
  "Suggest a lower-cost version",
  "Suggest a vegan version",
];

const RULE_LABELS: Record<ScalingRule, string> = {
  LINEAR: "Linear",
  SPICE_NONLINEAR: "Non-linear Spice (0.85^)",
  SEASONING_STEPPED: "Stepped Seasoning (0.80^)",
  LIQUID_EVAPORATION: "Liquid Evaporation (0.92^)",
  FIXED_BATCH: "Fixed Batch",
};

const SCALING_METHODS = [
  "LINEAR",
  "FIXED_QUANTITY",
  "BATCH_BASED",
  "CHEF_DEFINED_FACTOR",
  "CHEF_DEFINED_CURVE",
  "MANUAL_REVIEW",
];

const REGEN_SECTIONS = [
  { id: "DESCRIPTION", label: "Description" },
  { id: "INGREDIENTS", label: "Ingredients" },
  { id: "INSTRUCTIONS", label: "Instructions" },
  { id: "COOKING_METHOD", label: "Cooking Method" },
  { id: "SUB_RECIPES", label: "Sub-Recipes" },
  { id: "DIETARY", label: "Dietary & Allergens" },
];

const splitCsvList = (value: string): string[] =>
  value
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);

const CulinaryRecipePlannerWorkspace: React.FC<CulinaryRecipePlannerWorkspaceProps> = ({
  onOpenWorkspace,
  onContinueToMaterialMapping,
}) => {
  // Switch between Live Phase 1A Workspace and Phase 0 Scaling Simulator Demo
  const [workspaceMode, setWorkspaceMode] = useState<"PHASE1A" | "PHASE0_DEMO">("PHASE1A");

  // Context & Draft History
  const [context, setContext] = useState<CulinaryPlannerContext | null>(null);
  const [contextLoading, setContextLoading] = useState(true);
  const [contextError, setContextError] = useState<string | null>(null);
  const [drafts, setDrafts] = useState<CulinaryRecipeDraftSummary[]>([]);
  const [draftSearch, setDraftSearch] = useState("");
  const [draftStatusFilter, setDraftStatusFilter] = useState("");

  // Left Panel Inputs (Modes A-G)
  const [generationMode, setGenerationMode] = useState<CulinaryGenerationMode>("DISH_NAME");
  const [locationId, setLocationId] = useState<string>("");
  const [recipeName, setRecipeName] = useState("");
  const [recipeDescription, setRecipeDescription] = useState("");
  const [cuisine, setCuisine] = useState("");
  const [category, setCategory] = useState("");
  const [recipeType, setRecipeType] = useState("STANDARD_RECIPE");
  const [servings, setServings] = useState(10);
  const [servingSize, setServingSize] = useState(180);
  const [servingUom, setServingUom] = useState("G");
  const [prepStyle, setPrepStyle] = useState("");
  const [cookingMethod, setCookingMethod] = useState("");
  const [spiceLevel, setSpiceLevel] = useState("MEDIUM");
  const [dietaryText, setDietaryText] = useState("");
  const [includeText, setIncludeText] = useState("");
  const [excludeText, setExcludeText] = useState("");
  const [allergensAvoidText, setAllergensAvoidText] = useState("");
  const [preferredText, setPreferredText] = useState("");
  const [additionalInstructions, setAdditionalInstructions] = useState("");
  const [preferredProvider, setPreferredProvider] = useState("AUTO");
  const [sourceRecipeId, setSourceRecipeId] = useState("");
  const [selectedStockIds, setSelectedStockIds] = useState<string[]>([]);
  const [targetIngredientBudget, setTargetIngredientBudget] = useState("");
  const [targetCostPerServing, setTargetCostPerServing] = useState("");
  const [targetSellingPrice, setTargetSellingPrice] = useState("");
  const [targetFoodCostPercent, setTargetFoodCostPercent] = useState("");

  // Mode E (Document) & Mode F (Dish Image)
  const [docExtract, setDocExtract] = useState<CulinaryDocumentExtract | null>(null);
  const [docUploading, setDocUploading] = useState(false);
  const [dishImage, setDishImage] = useState<CulinaryDishImagePrepared | null>(null);
  const [dishImageName, setDishImageName] = useState<string | null>(null);
  const [imageUploading, setImageUploading] = useState(false);

  // Generation Lifecycle & Progress
  const [generating, setGenerating] = useState(false);
  const [generationStage, setGenerationStage] = useState<string | null>(null);
  const [generationError, setGenerationError] = useState<string | null>(null);
  const activeGenRef = useRef<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  // Active Draft State
  const [activeDraft, setActiveDraft] = useState<CulinaryRecipeDraftDetail | null>(null);
  const [payload, setPayload] = useState<CulinaryStructuredRecipePayload | null>(null);
  const [previousPayload, setPreviousPayload] = useState<CulinaryStructuredRecipePayload | null>(null);
  const [conversation, setConversation] = useState<CulinaryConversationTurn[]>([]);
  const [validationWarnings, setValidationWarnings] = useState<CulinaryValidationWarning[]>([]);
  const [dirty, setDirty] = useState(false);
  const [saving, setSaving] = useState(false);

  // Interactive Scaling State (for active draft)
  const [scaleTargetServings, setScaleTargetServings] = useState(10);
  const [scaleTargetServingSize, setScaleTargetServingSize] = useState(180);
  const [scalePrepBufferPct, setScalePrepBufferPct] = useState(0);
  const [scaleUseNonlinear, setScaleUseNonlinear] = useState(true);
  const [serverScaleResult, setServerScaleResult] = useState<CulinaryScaleV2Result | null>(null);
  const [scalingBusy, setScalingBusy] = useState(false);

  // Right Panel: Assistant, Refinement, Selective Regen, History
  const [rightTab, setRightTab] = useState<"ASSISTANT" | "REGEN" | "HISTORY">("ASSISTANT");
  const [refineInstruction, setRefineInstruction] = useState("");
  const [refining, setRefining] = useState(false);
  const [pendingRefine, setPendingRefine] = useState<CulinaryRefineResponse | null>(null);
  const [regenSections, setRegenSections] = useState<string[]>(["INGREDIENTS"]);
  const [lockedFields, setLockedFields] = useState<string[]>(["base_servings", "serving_size"]);
  const [regenGuidance, setRegenGuidance] = useState("");
  const [compareRevisionNumber, setCompareRevisionNumber] = useState<number | null>(null);

  // Phase 0 Demo Simulator state (preserved verbatim)
  const [demoServings, setDemoServings] = useState(10);
  const [demoServingSizeGrams, setDemoServingSizeGrams] = useState(180);
  const [demoPrepBufferPercent, setDemoPrepBufferPercent] = useState(0);
  const [demoWasteAllowancePercent, setDemoWasteAllowancePercent] = useState(0);
  const [demoIngredients, setDemoIngredients] = useState<DemoRecipeIngredient[]>(
    DEMO_SALMON_RECIPE.ingredients
  );

  const demoResult = useMemo(
    () =>
      calculateRecipeScaling({
        baseServings: DEMO_SALMON_RECIPE.baseServings,
        targetServings: demoServings,
        baseServingSizeGrams: DEMO_SALMON_RECIPE.baseServingSizeGrams,
        targetServingSizeGrams: demoServingSizeGrams,
        prepBufferPercent: demoPrepBufferPercent,
        wasteAllowancePercent: demoWasteAllowancePercent,
        ingredients: demoIngredients,
      }),
    [
      demoServings,
      demoServingSizeGrams,
      demoPrepBufferPercent,
      demoWasteAllowancePercent,
      demoIngredients,
    ]
  );

  const loadContextAndHistory = useCallback(async (locId?: string | null) => {
    setContextLoading(true);
    setContextError(null);
    try {
      const [ctx, list] = await Promise.all([
        getCulinaryPlannerContext(locId),
        listCulinaryRecipeDrafts(),
      ]);
      setContext(ctx);
      setDrafts(list);
      if (!locId && ctx.locations.length > 0) {
        setLocationId((prev) => prev || ctx.locations[0].locationId);
      }
    } catch (err: unknown) {
      setContextError(err instanceof Error ? err.message : "Failed to load Culinary Planner.");
    } finally {
      setContextLoading(false);
    }
  }, []);

  useEffect(() => {
    loadContextAndHistory();
  }, [loadContextAndHistory]);

  const refreshHistory = useCallback(async () => {
    try {
      const list = await listCulinaryRecipeDrafts(
        draftStatusFilter || undefined,
        draftSearch.trim() || undefined
      );
      setDrafts(list);
    } catch {
      // Keep existing list on transient filter error
    }
  }, [draftStatusFilter, draftSearch]);

  const hydrateDraft = useCallback((detail: CulinaryRecipeDraftDetail) => {
    setActiveDraft(detail);
    setPayload(detail.payload);
    setPreviousPayload(null);
    setPendingRefine(null);
    setConversation(detail.conversation ?? []);
    setValidationWarnings(detail.validationWarnings ?? []);
    setScaleTargetServings(detail.payload.header.base_servings || 10);
    setScaleTargetServingSize(detail.payload.header.serving_size || 180);
    setScalePrepBufferPct(0);
    setServerScaleResult(null);
    setCompareRevisionNumber(null);
    setDirty(false);
  }, []);

  const handleLocationChange = async (newLocationId: string) => {
    setLocationId(newLocationId);
    try {
      const ctx = await getCulinaryPlannerContext(newLocationId || null);
      setContext(ctx);
    } catch {
      // ignore
    }
  };

  const handleDocumentUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setDocUploading(true);
    try {
      const extracted = await extractCulinaryRecipeDocument(file);
      setDocExtract(extracted);
      if (extracted.detected_title && !recipeName.trim()) {
        setRecipeName(extracted.detected_title);
      }
      if (extracted.detected_servings && extracted.detected_servings > 0) {
        setServings(extracted.detected_servings);
      }
      toastService.success(`Extracted ${extracted.line_count} lines from ${extracted.filename}`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Document extraction failed.");
    } finally {
      setDocUploading(false);
      e.target.value = "";
    }
  };

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setImageUploading(true);
    try {
      const prepared = await prepareCulinaryDishImage(file);
      setDishImage(prepared);
      setDishImageName(file.name);
      toastService.success(
        `Prepared dish photo (${prepared.width}x${prepared.height}). Inferred ingredients will be clearly labeled.`
      );
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Dish image preparation failed.");
    } finally {
      setImageUploading(false);
      e.target.value = "";
    }
  };

  const handleGenerate = async (e?: React.FormEvent) => {
    e?.preventDefault();
    setGenerationError(null);

    if (generationMode === "DISH_NAME" && !recipeName.trim()) {
      toastService.error("Enter a recipe name before generating.");
      return;
    }
    if (generationMode === "DETAILED_INSTRUCTIONS" && !recipeDescription.trim() && !additionalInstructions.trim()) {
      toastService.error("Enter detailed chef instructions.");
      return;
    }
    if (generationMode === "EXISTING_RECIPE" && !sourceRecipeId) {
      toastService.error("Select an existing authorized recipe to reference.");
      return;
    }
    if (generationMode === "DOCUMENT" && !docExtract?.extracted_text) {
      toastService.error("Upload a supported recipe document first.");
      return;
    }
    if (generationMode === "DISH_IMAGE" && !dishImage?.base64) {
      toastService.error("Upload a dish photograph first.");
      return;
    }

    const genRef = `GEN-${Date.now()}`;
    activeGenRef.current = genRef;
    const controller = new AbortController();
    abortRef.current = controller;

    setGenerating(true);
    setGenerationStage("Validating input & context...");

    const stockNames =
      context?.availableStock
        .filter((s) => selectedStockIds.includes(s.materialId))
        .map((s) => `${s.description} (${s.onHandQty} ${s.baseUom})`) ?? [];

    const combinedInclude = [
      ...splitCsvList(includeText),
      ...(generationMode === "AVAILABLE_INGREDIENTS" ? stockNames : []),
    ];

    try {
      setGenerationStage("Connecting to configured AI engine & structuring recipe...");
      const result = await generateCulinaryRecipe(
        {
          draftId: null,
          generationMode,
          recipeName: recipeName.trim() || docExtract?.detected_title || "Chef AI Recipe",
          recipeDescription: recipeDescription.trim(),
          cuisine: cuisine.trim(),
          category: category.trim(),
          recipeType,
          servings: Math.max(1, servings),
          servingSize: Math.max(1, servingSize),
          servingUom,
          preparationStyle: prepStyle.trim(),
          cookingMethod: cookingMethod.trim(),
          spiceLevel,
          dietaryPreferences: splitCsvList(dietaryText),
          ingredientsToInclude: combinedInclude,
          ingredientsToExclude: splitCsvList(excludeText),
          allergensToAvoid: splitCsvList(allergensAvoidText),
          preferredIngredients: splitCsvList(preferredText),
          targetIngredientBudget: targetIngredientBudget ? Number(targetIngredientBudget) : null,
          targetCostPerServing: targetCostPerServing ? Number(targetCostPerServing) : null,
          targetSellingPrice: targetSellingPrice ? Number(targetSellingPrice) : null,
          targetFoodCostPercent: targetFoodCostPercent ? Number(targetFoodCostPercent) : null,
          locationId: locationId || null,
          sourceRecipeId: sourceRecipeId || null,
          sourceDocumentName: docExtract?.filename ?? null,
          extractedDocumentText: docExtract?.extracted_text ?? null,
          imageBase64: dishImage?.base64 ?? null,
          imageMimeType: dishImage?.mime_type ?? null,
          additionalInstructions: additionalInstructions.trim(),
          preferredProvider:
            context?.canSelectProvider && preferredProvider !== "AUTO" ? preferredProvider : null,
          generationReference: genRef,
        },
        controller.signal
      );

      setGenerationStage("Ready for chef review");
      hydrateDraft(result);
      await refreshHistory();
      toastService.success(
        `Generated "${result.recipeName}" via ${result.provider} (${result.model}).`
      );
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Recipe generation failed.";
      setGenerationError(msg);
      toastService.error(msg);
    } finally {
      setGenerating(false);
      activeGenRef.current = null;
      abortRef.current = null;
    }
  };

  const handleCancelGeneration = async () => {
    abortRef.current?.abort();
    if (activeGenRef.current) {
      await cancelCulinaryGeneration(activeGenRef.current);
    }
    setGenerating(false);
    setGenerationStage("Cancelled by chef");
    toastService.info("Recipe generation cancelled. Existing draft preserved.");
  };

  const updatePayload = (updater: (prev: CulinaryStructuredRecipePayload) => CulinaryStructuredRecipePayload) => {
    setPayload((prev) => {
      if (!prev) return prev;
      setPreviousPayload(prev);
      setDirty(true);
      return updater(prev);
    });
  };

  const handleIngredientChange = (
    idx: number,
    patch: Partial<CulinaryIngredientLine>
  ) => {
    updatePayload((prev) => {
      const next = [...prev.ingredients];
      next[idx] = {
        ...next[idx],
        ...patch,
        provenance: next[idx].provenance === "AI_GENERATED" ? "CHEF_EDITED" : next[idx].provenance,
      };
      return { ...prev, ingredients: next };
    });
  };

  const handleAddIngredient = () => {
    updatePayload((prev) => ({
      ...prev,
      ingredients: [...prev.ingredients, createBlankIngredient(prev.ingredients.length + 1)],
    }));
  };

  const handleRemoveIngredient = (idx: number) => {
    updatePayload((prev) => ({
      ...prev,
      ingredients: prev.ingredients.filter((_, i) => i !== idx),
    }));
  };

  const handleStepChange = (idx: number, patch: Partial<CulinaryInstructionStep>) => {
    updatePayload((prev) => {
      const next = [...prev.instructions];
      next[idx] = {
        ...next[idx],
        ...patch,
        provenance: next[idx].provenance === "AI_GENERATED" ? "CHEF_EDITED" : next[idx].provenance,
      };
      return { ...prev, instructions: next };
    });
  };

  const handleMoveStep = (idx: number, delta: -1 | 1) => {
    updatePayload((prev) => {
      const target = idx + delta;
      if (target < 0 || target >= prev.instructions.length) return prev;
      const next = [...prev.instructions];
      const [moved] = next.splice(idx, 1);
      next.splice(target, 0, moved);
      return {
        ...prev,
        instructions: next.map((step, i) => ({ ...step, step_number: i + 1 })),
      };
    });
  };

  const handleAddStep = () => {
    updatePayload((prev) => ({
      ...prev,
      instructions: [...prev.instructions, createBlankStep(prev.instructions.length + 1)],
    }));
  };

  const handleRemoveStep = (idx: number) => {
    updatePayload((prev) => ({
      ...prev,
      instructions: prev.instructions
        .filter((_, i) => i !== idx)
        .map((step, i) => ({ ...step, step_number: i + 1 })),
    }));
  };

  // Deterministic Scaling preview (instant) + optional server verification
  const localScalePreview = useMemo(() => {
    if (!payload) return null;
    return scaleDraftLocally({
      recipeName: payload.header.proposed_name,
      baseServings: payload.header.base_servings || 10,
      targetServings: scaleTargetServings,
      baseServingSize: payload.header.serving_size || 180,
      targetServingSize: scaleTargetServingSize,
      prepBufferPercent: scalePrepBufferPct,
      useNonlinearRules: scaleUseNonlinear,
      ingredients: payload.ingredients,
    });
  }, [
    payload,
    scaleTargetServings,
    scaleTargetServingSize,
    scalePrepBufferPct,
    scaleUseNonlinear,
  ]);

  const handleSyncServerScale = async () => {
    if (!payload) return;
    setScalingBusy(true);
    try {
      const res = await scaleCulinaryRecipeV2(
        {
          recipeName: payload.header.proposed_name,
          baseServings: payload.header.base_servings || 10,
          targetServings: scaleTargetServings,
          baseServingSize: payload.header.serving_size || 180,
          targetServingSize: scaleTargetServingSize,
          prepBufferPercent: scalePrepBufferPct,
          useNonlinearRules: scaleUseNonlinear,
          ingredients: payload.ingredients.map((i) => ({
            temp_ref: i.temp_ref,
            description: i.description,
            uom: i.recipe_uom,
            base_qty: i.quantity,
            quantity_basis: i.quantity_basis,
            yield_percent: i.yield_percent,
            scaling_method: i.scaling_method,
            chef_scaling_factor: i.chef_scaling_factor,
          })),
        },
        activeDraft?.id
      );
      setServerScaleResult(res);
      toastService.success("Deterministic Python scaling verified & logged.");
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Scaling verification failed.");
    } finally {
      setScalingBusy(false);
    }
  };

  const handleRefine = async (instructionText?: string) => {
    if (!payload) return;
    const text = (instructionText ?? refineInstruction).trim();
    if (!text) return;
    setRefining(true);
    try {
      const res = await refineCulinaryRecipe(
        {
          instruction: text,
          currentPayload: payload,
          preferredProvider:
            context?.canSelectProvider && preferredProvider !== "AUTO" ? preferredProvider : null,
        },
        activeDraft?.id
      );
      setPendingRefine(res);
      setConversation((prev) => [
        ...prev,
        { role: "CHEF", message: text, timestampUtc: new Date().toISOString() },
        {
          role: "AI",
          message: res.summaryMessage || `Proposed updates ready (${res.diffSummary.length} change(s)).`,
          provider: res.provider,
          model: res.model,
          timestampUtc: new Date().toISOString(),
        },
      ]);
      if (!instructionText) setRefineInstruction("");
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Refinement failed.");
    } finally {
      setRefining(false);
    }
  };

  const handleRegenerateSelected = async () => {
    if (!payload || regenSections.length === 0) return;
    setRefining(true);
    try {
      const lockedIngredientRefs = payload.ingredients
        .filter((i) => i.locked)
        .map((i) => i.temp_ref);
      const res = await regenerateCulinaryRecipeSection(
        {
          currentPayload: payload,
          sections: regenSections,
          lockedFields,
          lockedIngredientRefs,
          chefGuidance: regenGuidance.trim(),
          preferredProvider:
            context?.canSelectProvider && preferredProvider !== "AUTO" ? preferredProvider : null,
        },
        activeDraft?.id
      );
      setPendingRefine(res);
      setRightTab("ASSISTANT");
      toastService.info("Proposed section regeneration ready for chef review.");
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Selective regeneration failed.");
    } finally {
      setRefining(false);
    }
  };

  const handleAcceptProposed = () => {
    if (!pendingRefine || !payload) return;
    setPreviousPayload(payload);
    setPayload(pendingRefine.proposedPayload);
    setValidationWarnings(pendingRefine.validationWarnings ?? []);
    setPendingRefine(null);
    setDirty(true);
    toastService.success("Proposed changes applied to working draft. Save Draft to persist.");
  };

  const handleRejectProposed = () => {
    setPendingRefine(null);
    toastService.info("Proposed changes rejected. Active recipe unchanged.");
  };

  const handleUndoLastEdit = () => {
    if (!previousPayload) return;
    setPayload(previousPayload);
    setPreviousPayload(null);
    setDirty(true);
    toastService.info("Reverted to previous in-session version.");
  };

  const handleSaveDraft = async (nextStatus?: string) => {
    if (!activeDraft || !payload) return;
    setSaving(true);
    try {
      const saved = await saveCulinaryRecipeDraft(activeDraft.id, {
        expectedRevisionNumber: activeDraft.revisionNumber,
        status: nextStatus ?? "CHEF_REVIEW",
        changeType: "CHEF_EDIT",
        changeSummary: "Chef reviewed and saved recipe draft.",
        payload,
        conversation,
        validationWarnings,
      });
      hydrateDraft(saved);
      await refreshHistory();
      toastService.success(`Saved draft ${saved.draftReference} (Rev ${saved.revisionNumber}).`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Save failed.");
    } finally {
      setSaving(false);
    }
  };

  const handleDuplicateDraft = async () => {
    if (!activeDraft) return;
    try {
      const copy = await duplicateCulinaryRecipeDraft(activeDraft.id);
      hydrateDraft(copy);
      await refreshHistory();
      toastService.success(`Duplicated into ${copy.draftReference}.`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Duplicate failed.");
    }
  };

  const handleOpenDraft = async (draftId: string) => {
    try {
      const detail = await getCulinaryRecipeDraft(draftId);
      hydrateDraft(detail);
      toastService.info(`Loaded draft ${detail.draftReference}`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to load draft.");
    }
  };

  const handleRestoreRevision = async (revNum: number) => {
    if (!activeDraft) return;
    try {
      const restored = await restoreCulinaryRecipeDraftRevision(activeDraft.id, revNum);
      hydrateDraft(restored);
      await refreshHistory();
      toastService.success(`Restored revision #${revNum} as revision #${restored.revisionNumber}.`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to restore revision.");
    }
  };

  const handleCancelDraft = async () => {
    if (!activeDraft) return;
    try {
      const cancelled = await cancelCulinaryRecipeDraft(activeDraft.id);
      hydrateDraft(cancelled);
      await refreshHistory();
      toastService.info(`Draft ${cancelled.draftReference} marked CANCELLED.`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to cancel draft.");
    }
  };

  const handleExport = async (format: "JSON" | "CSV") => {
    if (!activeDraft) return;
    try {
      const { blob, filename } = await exportCulinaryRecipeDraft(activeDraft.id, format);
      triggerBlobDownload(blob, filename);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Export failed.");
    }
  };

  const revisionComparison = useMemo(() => {
    if (!activeDraft || !payload || compareRevisionNumber == null) return null;
    const rev = activeDraft.revisions.find((r) => r.revisionNumber === compareRevisionNumber);
    if (!rev) return null;
    return compareRecipePayloads(rev.payload, payload);
  }, [activeDraft, payload, compareRevisionNumber]);

  const activeScalingDisplay = serverScaleResult ?? localScalePreview;

  return (
    <div className="cs2-shell" data-testid="culinary-recipe-planner-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">AI Recipe Planner &amp; Culinary Intelligence Engine</h2>
          <p className="cs2-subtitle">
            Phase 1A Chef AI Workspace — Generate structured hospitality recipes across Local AI, Gemini, Claude, or Hybrid routing,
            scale deterministically, refine conversationally, and prepare drafts for Phase 1B Material Mapping.
          </p>
        </div>
        <div className="cs2-actions-row">
          <button
            type="button"
            className={`sila-btn sila-btn--sm ${workspaceMode === "PHASE1A" ? "sila-btn--primary" : "sila-btn--secondary"}`}
            onClick={() => setWorkspaceMode("PHASE1A")}
          >
            Live AI Planner (Phase 1A)
          </button>
          <button
            type="button"
            className={`sila-btn sila-btn--sm ${workspaceMode === "PHASE0_DEMO" ? "sila-btn--primary" : "sila-btn--secondary"}`}
            onClick={() => setWorkspaceMode("PHASE0_DEMO")}
          >
            Phase 0 Scaling Simulator (Demo)
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--sm sila-btn--secondary"
            onClick={() => onOpenWorkspace("ai-setup")}
          >
            AI Engine Setup
          </button>
        </div>
      </div>

      {workspaceMode === "PHASE0_DEMO" ? (
        <>
          <div className="cs2-banner" role="status">
            <div>
              <strong>DEMONSTRATION DATA — ILLUSTRATIVE RECIPE ONLY ({DEMO_SALMON_RECIPE.title})</strong>
              <div>
                Isolated Phase 0 deterministic scaling &amp; costing simulator. Switch back to &ldquo;Live AI Planner (Phase 1A)&rdquo;
                for real AI recipe generation and tenant draft management.
              </div>
            </div>
            <span className="cs2-badge cs2-badge--demo">Phase 0 Interactive Demo</span>
          </div>

          <section className="cs2-card" aria-label="Interactive Recipe Scaling Controls">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Interactive Recipe Scaling Controls</h3>
              <div className="cs2-actions-row">
                <span className="cs2-badge cs2-badge--demo">Scale Factor: {demoResult.scaleFactor}x</span>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--secondary"
                  onClick={() => {
                    setDemoServings(10);
                    setDemoServingSizeGrams(180);
                    setDemoPrepBufferPercent(0);
                    setDemoWasteAllowancePercent(0);
                    setDemoIngredients(DEMO_SALMON_RECIPE.ingredients);
                  }}
                >
                  Reset Demonstration
                </button>
              </div>
            </div>

            <div className="cs2-form-grid">
              <div className="cs2-slider-row">
                <label className="sila-label" htmlFor="cs2-scale-servings">
                  Target Servings: <strong>{demoServings}</strong> (base: {DEMO_SALMON_RECIPE.baseServings})
                </label>
                <input
                  id="cs2-scale-servings"
                  type="range"
                  min={1}
                  max={500}
                  value={demoServings}
                  onChange={(e) => setDemoServings(Number(e.target.value))}
                />
                <input
                  type="number"
                  className="sila-input"
                  aria-label="Target servings numeric input"
                  min={1}
                  max={5000}
                  value={demoServings}
                  onChange={(e) => setDemoServings(Math.max(1, Number(e.target.value) || 1))}
                />
              </div>

              <div className="cs2-slider-row">
                <label className="sila-label" htmlFor="cs2-scale-size">
                  Portion Size (g): <strong>{demoServingSizeGrams} g</strong> (base: 180 g)
                </label>
                <input
                  id="cs2-scale-size"
                  type="range"
                  min={80}
                  max={400}
                  step={10}
                  value={demoServingSizeGrams}
                  onChange={(e) => setDemoServingSizeGrams(Number(e.target.value))}
                />
              </div>

              <div className="cs2-slider-row">
                <label className="sila-label" htmlFor="cs2-scale-prep">
                  Preparation Buffer: <strong>{demoPrepBufferPercent}%</strong>
                </label>
                <input
                  id="cs2-scale-prep"
                  type="range"
                  min={0}
                  max={30}
                  step={1}
                  value={demoPrepBufferPercent}
                  onChange={(e) => setDemoPrepBufferPercent(Number(e.target.value))}
                />
              </div>

              <div className="cs2-slider-row">
                <label className="sila-label" htmlFor="cs2-scale-waste">
                  Waste Allowance: <strong>{demoWasteAllowancePercent}%</strong>
                </label>
                <input
                  id="cs2-scale-waste"
                  type="range"
                  min={0}
                  max={25}
                  step={1}
                  value={demoWasteAllowancePercent}
                  onChange={(e) => setDemoWasteAllowancePercent(Number(e.target.value))}
                />
              </div>
            </div>
          </section>

          <section className="cs2-card">
            <div className="cs2-table-wrap">
              <table className="cs2-table" data-testid="recipe-scaling-table">
                <thead>
                  <tr>
                    <th>Ingredient</th>
                    <th>Match Status (Demo)</th>
                    <th>Scaling Rule</th>
                    <th className="cs2-num">Base Qty</th>
                    <th className="cs2-num">Scaled Qty</th>
                    <th className="cs2-num">Gross w/ Buffers</th>
                    <th>UOM</th>
                    <th className="cs2-num">Scaled Ext. Cost</th>
                  </tr>
                </thead>
                <tbody>
                  {demoResult.items.map((item) => (
                    <tr key={item.id}>
                      <td><strong>{item.name}</strong></td>
                      <td>{item.demoMaterialRef ?? "Unmatched"}</td>
                      <td>
                        <select
                          className="sila-select"
                          value={item.scalingRule}
                          onChange={(e) =>
                            setDemoIngredients((prev) =>
                              prev.map((row) =>
                                row.id === item.id ? { ...row, scalingRule: e.target.value as ScalingRule } : row
                              )
                            )
                          }
                        >
                          {(Object.keys(RULE_LABELS) as ScalingRule[]).map((rk) => (
                            <option key={rk} value={rk}>{RULE_LABELS[rk]}</option>
                          ))}
                        </select>
                      </td>
                      <td className="cs2-num">{item.baseQuantity.toFixed(3)}</td>
                      <td className="cs2-num">{item.scaledQuantity.toFixed(3)}</td>
                      <td className="cs2-num"><strong>{item.grossQuantityWithBuffers.toFixed(3)}</strong></td>
                      <td>{item.uom}</td>
                      <td className="cs2-num"><strong>AED {item.scaledExtendedCostAed.toFixed(2)}</strong></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </>
      ) : contextLoading ? (
        <Loader size={24} message="Loading Culinary Recipe Planner context..." />
      ) : (
        <>
          {contextError && (
            <div className="cs2-banner cs2-banner--warn" role="alert">
              <div><strong>Planner context notice:</strong> {contextError}</div>
              <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => loadContextAndHistory()}>
                Retry
              </button>
            </div>
          )}

          {/* Guardrail & Context Banner */}
          <div className="cs2-banner cs2-banner--phase" role="status">
            <div>
              <strong>Phase 1A Guardrail Active:</strong> AI drafts stay isolated from production Recipe Master, Material Master, Live Inventory, and SAP postings.
              {context && (
                <span>
                  {" "}· Configured Mode: <strong>{context.operatingMode}</strong> · Enabled:{" "}
                  <strong>{context.enabledProviders.join(", ") || "None"}</strong> · Prompt:{" "}
                  <code>{context.promptVersion}</code>
                </span>
              )}
            </div>
            <span className="cs2-badge cs2-badge--indigo">Drafts Only · No Production Writes</span>
          </div>

          <div className="cs2-planner-grid">
            {/* LEFT PANEL — RECIPE INPUTS */}
            <form className="cs2-card" onSubmit={handleGenerate} data-testid="culinary-planner-inputs">
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">1. Recipe Generation Brief</h3>
                <span className="cs2-badge cs2-badge--indigo">Modes A–G</span>
              </div>

              <div className="cs2-mode-tabs" role="tablist" aria-label="Generation Modes">
                {MODE_OPTIONS.map((m) => (
                  <button
                    key={m.id}
                    type="button"
                    role="tab"
                    aria-selected={generationMode === m.id}
                    className={`cs2-mode-btn ${generationMode === m.id ? "is-active" : ""}`}
                    onClick={() => setGenerationMode(m.id)}
                    title={m.hint}
                  >
                    {m.label}
                  </button>
                ))}
              </div>

              <div className="cs2-shell" style={{ marginTop: 12, gap: 10 }}>
                {context && context.locations.length > 0 && (
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-location">Property / Kitchen / Outlet</label>
                    <select
                      id="p1a-location"
                      className="sila-select"
                      value={locationId}
                      onChange={(e) => handleLocationChange(e.target.value)}
                    >
                      {context.locations.map((loc) => (
                        <option key={loc.locationId} value={loc.locationId}>
                          {loc.locationName} ({loc.locationCode})
                          {loc.outletName ? ` · ${loc.outletName}` : ""}
                        </option>
                      ))}
                    </select>
                  </div>
                )}

                <div className="sila-field">
                  <label className="sila-label" htmlFor="p1a-name">Recipe Name</label>
                  <input
                    id="p1a-name"
                    className="sila-input"
                    placeholder="e.g., Butter Chicken"
                    value={recipeName}
                    onChange={(e) => setRecipeName(e.target.value)}
                  />
                </div>

                {(generationMode === "DETAILED_INSTRUCTIONS" || generationMode === "DISH_NAME") && (
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-desc">
                      {generationMode === "DETAILED_INSTRUCTIONS" ? "Detailed Chef Instructions" : "Recipe Description / Brief"}
                    </label>
                    <textarea
                      id="p1a-desc"
                      className="sila-input"
                      rows={3}
                      placeholder="e.g., Create a mildly spicy Indian butter chicken recipe for fifty guests using boneless chicken."
                      value={recipeDescription}
                      onChange={(e) => setRecipeDescription(e.target.value)}
                    />
                  </div>
                )}

                {generationMode === "EXISTING_RECIPE" && (
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-existing">Reference Existing Recipe (Read-Only)</label>
                    <select
                      id="p1a-existing"
                      className="sila-select"
                      value={sourceRecipeId}
                      onChange={(e) => setSourceRecipeId(e.target.value)}
                    >
                      <option value="">-- Select authorized recipe --</option>
                      {(context?.existingRecipes ?? []).map((r) => (
                        <option key={r.recipeId} value={r.recipeId}>
                          {r.recipeCode} · {r.name} ({r.servingQty} {r.servingUom})
                        </option>
                      ))}
                    </select>
                  </div>
                )}

                {generationMode === "AVAILABLE_INGREDIENTS" && (
                  <div className="sila-field">
                    <label className="sila-label">Available Kitchen Stock (Read-Only)</label>
                    <div style={{ maxHeight: 140, overflowY: "auto", border: "1px solid #e2e8f0", borderRadius: 8, padding: 8 }}>
                      {(context?.availableStock ?? []).length === 0 ? (
                        <p className="cs2-empty-note">No positive stock rows found for this kitchen; type ingredients below.</p>
                      ) : (
                        context?.availableStock.map((item) => (
                          <label key={item.materialId} className="cs2-checkbox-row" style={{ marginBottom: 4, fontSize: 12 }}>
                            <input
                              type="checkbox"
                              checked={selectedStockIds.includes(item.materialId)}
                              onChange={(e) =>
                                setSelectedStockIds((prev) =>
                                  e.target.checked
                                    ? [...prev, item.materialId]
                                    : prev.filter((id) => id !== item.materialId)
                                )
                              }
                            />
                            <span>
                              {item.description} ({item.onHandQty} {item.baseUom})
                            </span>
                          </label>
                        ))
                      )}
                    </div>
                  </div>
                )}

                {generationMode === "DOCUMENT" && (
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-doc-upload">Upload Recipe Document (PDF, DOCX, XLSX, TXT)</label>
                    <input
                      id="p1a-doc-upload"
                      type="file"
                      accept=".pdf,.docx,.xlsx,.txt"
                      disabled={docUploading}
                      onChange={handleDocumentUpload}
                    />
                    {docUploading && <span className="cs2-empty-note">Extracting document...</span>}
                    {docExtract && (
                      <div className="cs2-diff-box" style={{ marginTop: 6 }}>
                        <div style={{ fontSize: 12 }}>
                          <strong>{docExtract.filename}</strong> ({docExtract.file_type}) · {docExtract.line_count} lines
                        </div>
                        {docExtract.missing_quantities_detected.length > 0 && (
                          <div className="cs2-badge cs2-badge--warn">
                            Missing quantities: {docExtract.missing_quantities_detected.length} line(s)
                          </div>
                        )}
                        {docExtract.warnings.map((w, idx) => (
                          <div key={idx} style={{ fontSize: 11.5, color: "#92400e" }}>{w}</div>
                        ))}
                      </div>
                    )}
                  </div>
                )}

                {generationMode === "DISH_IMAGE" && (
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-img-upload">Upload Dish Photograph (JPG, PNG, WEBP)</label>
                    <input
                      id="p1a-img-upload"
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      disabled={imageUploading}
                      onChange={handleImageUpload}
                    />
                    {imageUploading && <span className="cs2-empty-note">Validating image...</span>}
                    {dishImage && (
                      <div className="cs2-diff-box" style={{ marginTop: 6 }}>
                        <img
                          src={`data:${dishImage.mime_type};base64,${dishImage.base64}`}
                          alt={dishImageName ?? "Dish inspiration"}
                          style={{ maxWidth: "100%", maxHeight: 130, objectFit: "cover", borderRadius: 8 }}
                        />
                        <div style={{ fontSize: 11.5, color: "#475569" }}>
                          {dishImageName} ({dishImage.width}x{dishImage.height}). Photo inspiration only — inferred ingredients &amp; quantities will require chef verification.
                        </div>
                      </div>
                    )}
                  </div>
                )}

                {generationMode === "BUDGET" && (
                  <div className="cs2-form-grid" style={{ gridTemplateColumns: "1fr 1fr" }}>
                    <div className="sila-field">
                      <label className="sila-label" htmlFor="p1a-budget">Target Budget ({context?.currency ?? "AED"})</label>
                      <input id="p1a-budget" type="number" step="0.5" className="sila-input" value={targetIngredientBudget} onChange={(e) => setTargetIngredientBudget(e.target.value)} />
                    </div>
                    <div className="sila-field">
                      <label className="sila-label" htmlFor="p1a-cps">Cost / Serving</label>
                      <input id="p1a-cps" type="number" step="0.5" className="sila-input" value={targetCostPerServing} onChange={(e) => setTargetCostPerServing(e.target.value)} />
                    </div>
                    <div className="sila-field">
                      <label className="sila-label" htmlFor="p1a-sell">Target Selling Price</label>
                      <input id="p1a-sell" type="number" step="0.5" className="sila-input" value={targetSellingPrice} onChange={(e) => setTargetSellingPrice(e.target.value)} />
                    </div>
                    <div className="sila-field">
                      <label className="sila-label" htmlFor="p1a-fcp">Target Food Cost %</label>
                      <input id="p1a-fcp" type="number" step="1" className="sila-input" value={targetFoodCostPercent} onChange={(e) => setTargetFoodCostPercent(e.target.value)} />
                    </div>
                  </div>
                )}

                <div className="cs2-form-grid" style={{ gridTemplateColumns: "1fr 1fr" }}>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-cuisine">Cuisine</label>
                    <input id="p1a-cuisine" className="sila-input" placeholder="e.g., Indian" value={cuisine} onChange={(e) => setCuisine(e.target.value)} />
                  </div>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-category">Category</label>
                    <input id="p1a-category" className="sila-input" placeholder="e.g., Main Course" value={category} onChange={(e) => setCategory(e.target.value)} />
                  </div>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-servings">Servings</label>
                    <input id="p1a-servings" type="number" min={1} className="sila-input" value={servings} onChange={(e) => setServings(Math.max(1, Number(e.target.value) || 1))} />
                  </div>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-size">Serving Size</label>
                    <div style={{ display: "flex", gap: 4 }}>
                      <input id="p1a-size" type="number" min={1} className="sila-input" value={servingSize} onChange={(e) => setServingSize(Math.max(1, Number(e.target.value) || 100))} />
                      <select className="sila-select" aria-label="Serving UOM" value={servingUom} onChange={(e) => setServingUom(e.target.value)} style={{ width: 74 }}>
                        <option value="G">G</option>
                        <option value="ML">ML</option>
                        <option value="EA">EA</option>
                      </select>
                    </div>
                  </div>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-rtype">Recipe Type</label>
                    <select id="p1a-rtype" className="sila-select" value={recipeType} onChange={(e) => setRecipeType(e.target.value)}>
                      <option value="STANDARD_RECIPE">Standard Recipe</option>
                      <option value="SUB_RECIPE">Sub-Recipe / Base</option>
                      <option value="BANQUET_BATCH">Banquet / Buffet Batch</option>
                    </select>
                  </div>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-spice">Spice Level</label>
                    <select id="p1a-spice" className="sila-select" value={spiceLevel} onChange={(e) => setSpiceLevel(e.target.value)}>
                      <option value="MILD">Mild</option>
                      <option value="MEDIUM">Medium</option>
                      <option value="SPICY">Spicy</option>
                      <option value="NONE">None</option>
                    </select>
                  </div>
                </div>

                <div className="cs2-form-grid" style={{ gridTemplateColumns: "1fr 1fr" }}>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-prepstyle">Prep Style</label>
                    <input id="p1a-prepstyle" className="sila-input" placeholder="e.g., Scratch" value={prepStyle} onChange={(e) => setPrepStyle(e.target.value)} />
                  </div>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-method">Cooking Method</label>
                    <input id="p1a-method" className="sila-input" placeholder="e.g., Simmer / Grill" value={cookingMethod} onChange={(e) => setCookingMethod(e.target.value)} />
                  </div>
                </div>

                <div className="sila-field">
                  <label className="sila-label" htmlFor="p1a-dietary">Dietary Preferences (comma-separated)</label>
                  <input id="p1a-dietary" className="sila-input" placeholder="e.g., Halal, Gluten-free" value={dietaryText} onChange={(e) => setDietaryText(e.target.value)} />
                </div>
                <div className="sila-field">
                  <label className="sila-label" htmlFor="p1a-include">Ingredients to Include</label>
                  <input id="p1a-include" className="sila-input" placeholder="e.g., Boneless chicken, Cream" value={includeText} onChange={(e) => setIncludeText(e.target.value)} />
                </div>
                <div className="sila-field">
                  <label className="sila-label" htmlFor="p1a-exclude">Ingredients to Exclude</label>
                  <input id="p1a-exclude" className="sila-input" placeholder="e.g., Cashews, Garlic" value={excludeText} onChange={(e) => setExcludeText(e.target.value)} />
                </div>
                <div className="sila-field">
                  <label className="sila-label" htmlFor="p1a-allergens">Potential Allergens to Avoid</label>
                  <input id="p1a-allergens" className="sila-input" placeholder="e.g., Peanuts, Shellfish" value={allergensAvoidText} onChange={(e) => setAllergensAvoidText(e.target.value)} />
                </div>
                <div className="sila-field">
                  <label className="sila-label" htmlFor="p1a-preferred">Preferred Ingredients</label>
                  <input id="p1a-preferred" className="sila-input" placeholder="e.g., Kashmiri chilli, Ghee" value={preferredText} onChange={(e) => setPreferredText(e.target.value)} />
                </div>
                <div className="sila-field">
                  <label className="sila-label" htmlFor="p1a-notes">Additional Instructions</label>
                  <textarea id="p1a-notes" className="sila-input" rows={2} placeholder="Plating, buffet hold, equipment constraints..." value={additionalInstructions} onChange={(e) => setAdditionalInstructions(e.target.value)} />
                </div>

                <div className="sila-field">
                  <label className="sila-label" htmlFor="p1a-provider">AI Engine Selection</label>
                  <select
                    id="p1a-provider"
                    className="sila-select"
                    disabled={!context?.canSelectProvider}
                    value={preferredProvider}
                    onChange={(e) => setPreferredProvider(e.target.value)}
                  >
                    <option value="AUTO">Configured Default ({context?.operatingMode ?? "LOCAL"})</option>
                    {(context?.enabledProviders ?? []).map((p) => (
                      <option key={p} value={p}>{p}</option>
                    ))}
                  </select>
                  {!context?.canSelectProvider && (
                    <span className="cs2-empty-note">Using administrator-configured engine ({context?.operatingMode ?? "LOCAL"}).</span>
                  )}
                </div>

                <div className="cs2-actions-row">
                  <button
                    type="submit"
                    className="sila-btn sila-btn--sm sila-btn--primary"
                    disabled={generating}
                    data-testid="p1a-generate-btn"
                  >
                    {generating ? "Generating..." : "Generate Recipe"}
                  </button>
                  {generating && (
                    <button
                      type="button"
                      className="sila-btn sila-btn--sm sila-btn--secondary"
                      onClick={handleCancelGeneration}
                    >
                      Cancel
                    </button>
                  )}
                </div>

                {generationStage && (
                  <div className="cs2-progress-list" aria-live="polite">
                    <div className={`cs2-progress-step ${generating ? "is-active" : "is-done"}`}>
                      <span>{generating ? "●" : "✓"}</span>
                      <span>{generationStage}</span>
                    </div>
                  </div>
                )}

                {generationError && (
                  <div className="cs2-banner cs2-banner--warn" role="alert">
                    <div style={{ fontSize: 12 }}>
                      <strong>Generation notice:</strong> {generationError}
                    </div>
                    <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => handleGenerate()}>
                      Retry
                    </button>
                  </div>
                )}
              </div>
            </form>

            {/* CENTER PANEL — MAIN WORKSPACE (RECIPE OUTPUT & EDITOR) */}
            <div className="cs2-shell" data-testid="culinary-planner-editor">
              {!payload ? (
                <section className="cs2-card">
                  <div className="cs2-card__head">
                    <h3 className="cs2-card__title">2. Interactive Recipe Workspace</h3>
                    <span className="cs2-badge cs2-badge--planned">Awaiting Recipe Generation or Draft Selection</span>
                  </div>
                  <p className="cs2-subtitle">
                    Enter a dish name (for example, <strong>Butter Chicken</strong>) or select a saved draft from History to generate,
                    edit ingredients, scale portions deterministically, manage sub-recipes, and prepare for Phase 1B Material Mapping.
                  </p>
                </section>
              ) : (
                <>
                  {/* Header & Provenance */}
                  <section className="cs2-card">
                    <div className="cs2-card__head">
                      <div>
                        <span className="cs2-badge cs2-badge--indigo">
                          {activeDraft?.draftReference ?? "DRAFT"} · Rev {activeDraft?.revisionNumber ?? 1} · {activeDraft?.status ?? "GENERATED"}
                        </span>
                        {dirty && (
                          <span className="cs2-badge cs2-badge--warn" style={{ marginLeft: 6 }}>Unsaved Edits</span>
                        )}
                      </div>
                      <div className="cs2-actions-row">
                        <span className="cs2-badge cs2-badge--live">
                          {activeDraft?.provider} / {activeDraft?.model}
                        </span>
                        <span className="cs2-badge cs2-badge--demo">
                          {activeDraft?.totalTokens ?? 0} tok · ${(activeDraft?.estimatedCostUsd ?? 0).toFixed(4)}
                        </span>
                      </div>
                    </div>

                    <div className="cs2-form-grid">
                      <div className="sila-field">
                        <label className="sila-label" htmlFor="p1a-edit-name">Recipe Title</label>
                        <input
                          id="p1a-edit-name"
                          className="sila-input"
                          value={payload.header.proposed_name}
                          onChange={(e) =>
                            updatePayload((p) => ({ ...p, header: { ...p.header, proposed_name: e.target.value } }))
                          }
                        />
                      </div>
                      <div className="sila-field">
                        <label className="sila-label" htmlFor="p1a-edit-cuisine">Cuisine</label>
                        <input
                          id="p1a-edit-cuisine"
                          className="sila-input"
                          value={payload.header.cuisine}
                          onChange={(e) =>
                            updatePayload((p) => ({ ...p, header: { ...p.header, cuisine: e.target.value } }))
                          }
                        />
                      </div>
                      <div className="sila-field">
                        <label className="sila-label" htmlFor="p1a-edit-cat">Category</label>
                        <input
                          id="p1a-edit-cat"
                          className="sila-input"
                          value={payload.header.category}
                          onChange={(e) =>
                            updatePayload((p) => ({ ...p, header: { ...p.header, category: e.target.value } }))
                          }
                        />
                      </div>
                      <div className="sila-field">
                        <label className="sila-label" htmlFor="p1a-edit-prepmin">Prep / Cook (min)</label>
                        <div style={{ display: "flex", gap: 6 }}>
                          <input
                            id="p1a-edit-prepmin"
                            type="number"
                            min={0}
                            className="sila-input"
                            value={payload.header.prep_duration_minutes}
                            onChange={(e) => {
                              const prep = Math.max(0, Number(e.target.value) || 0);
                              updatePayload((p) => ({
                                ...p,
                                header: {
                                  ...p.header,
                                  prep_duration_minutes: prep,
                                  total_duration_minutes: prep + p.header.cook_duration_minutes,
                                },
                              }));
                            }}
                          />
                          <input
                            aria-label="Cook duration minutes"
                            type="number"
                            min={0}
                            className="sila-input"
                            value={payload.header.cook_duration_minutes}
                            onChange={(e) => {
                              const cook = Math.max(0, Number(e.target.value) || 0);
                              updatePayload((p) => ({
                                ...p,
                                header: {
                                  ...p.header,
                                  cook_duration_minutes: cook,
                                  total_duration_minutes: p.header.prep_duration_minutes + cook,
                                },
                              }));
                            }}
                          />
                        </div>
                      </div>
                    </div>

                    <div className="sila-field" style={{ marginTop: 10 }}>
                      <label className="sila-label" htmlFor="p1a-edit-desc">Description</label>
                      <textarea
                        id="p1a-edit-desc"
                        className="sila-input"
                        rows={2}
                        value={payload.header.description}
                        onChange={(e) =>
                          updatePayload((p) => ({ ...p, header: { ...p.header, description: e.target.value } }))
                        }
                      />
                    </div>

                    <div className="cs2-actions-row" style={{ marginTop: 10 }}>
                      <span className="cs2-badge cs2-badge--warn">{payload.cost_validation_note}</span>
                      <span className="cs2-badge cs2-badge--planned">
                        Nutrition: {payload.header.nutrition_status}
                      </span>
                      {payload.header.chef_verification_required && (
                        <span className="cs2-badge cs2-badge--warn">Chef Verification Required</span>
                      )}
                    </div>

                    {payload.visually_observed_characteristics.length > 0 && (
                      <div className="cs2-banner" style={{ marginTop: 10 }}>
                        <div>
                          <strong>Dish Image Visual vs Inferred Separation:</strong>
                          <div style={{ fontSize: 12 }}>
                            Observed: {payload.visually_observed_characteristics.join("; ")}
                          </div>
                          {payload.inferred_uncertain_items.length > 0 && (
                            <div style={{ fontSize: 12 }}>
                              AI-Inferred / Uncertain Quantities: {payload.inferred_uncertain_items.join("; ")}
                            </div>
                          )}
                        </div>
                      </div>
                    )}
                  </section>

                  {/* Interactive Recipe Scaling Bar */}
                  <section className="cs2-card" data-testid="p1a-scaling-panel">
                    <div className="cs2-card__head">
                      <h3 className="cs2-card__title">Interactive Recipe Scaling &amp; Yield Engine</h3>
                      <div className="cs2-actions-row">
                        <span className="cs2-badge cs2-badge--demo">
                          Factor: {activeScalingDisplay?.linear_factor ?? 1}x
                        </span>
                        <button
                          type="button"
                          className="sila-btn sila-btn--sm sila-btn--secondary"
                          onClick={() => {
                            setScaleTargetServings(payload.header.base_servings || 10);
                            setScaleTargetServingSize(payload.header.serving_size || 180);
                            setScalePrepBufferPct(0);
                            setServerScaleResult(null);
                          }}
                        >
                          Reset to Base
                        </button>
                        <button
                          type="button"
                          className="sila-btn sila-btn--sm sila-btn--secondary"
                          disabled={scalingBusy}
                          onClick={handleSyncServerScale}
                        >
                          {scalingBusy ? "Verifying..." : "Verify via Python Engine"}
                        </button>
                      </div>
                    </div>

                    <div className="cs2-form-grid">
                      <div className="cs2-slider-row">
                        <label className="sila-label" htmlFor="p1a-scale-srv">
                          Target Servings: <strong>{scaleTargetServings}</strong> (base: {payload.header.base_servings})
                        </label>
                        <input
                          id="p1a-scale-srv"
                          type="range"
                          min={1}
                          max={500}
                          value={scaleTargetServings}
                          onChange={(e) => {
                            setScaleTargetServings(Number(e.target.value));
                            setServerScaleResult(null);
                          }}
                        />
                        <input
                          type="number"
                          className="sila-input"
                          aria-label="Target servings numeric"
                          min={1}
                          max={5000}
                          value={scaleTargetServings}
                          onChange={(e) => {
                            setScaleTargetServings(Math.max(1, Number(e.target.value) || 1));
                            setServerScaleResult(null);
                          }}
                        />
                      </div>
                      <div className="cs2-slider-row">
                        <label className="sila-label" htmlFor="p1a-scale-sz">
                          Serving Size ({payload.header.serving_uom}): <strong>{scaleTargetServingSize}</strong> (base: {payload.header.serving_size})
                        </label>
                        <input
                          id="p1a-scale-sz"
                          type="number"
                          min={1}
                          className="sila-input"
                          value={scaleTargetServingSize}
                          onChange={(e) => {
                            setScaleTargetServingSize(Math.max(1, Number(e.target.value) || 1));
                            setServerScaleResult(null);
                          }}
                        />
                      </div>
                      <div className="cs2-slider-row">
                        <label className="sila-label" htmlFor="p1a-scale-buf">
                          Prep Buffer %: <strong>{scalePrepBufferPct}%</strong>
                        </label>
                        <input
                          id="p1a-scale-buf"
                          type="range"
                          min={0}
                          max={30}
                          value={scalePrepBufferPct}
                          onChange={(e) => {
                            setScalePrepBufferPct(Number(e.target.value));
                            setServerScaleResult(null);
                          }}
                        />
                        <label className="cs2-checkbox-row">
                          <input
                            type="checkbox"
                            checked={scaleUseNonlinear}
                            onChange={(e) => {
                              setScaleUseNonlinear(e.target.checked);
                              setServerScaleResult(null);
                            }}
                          />
                          Flag non-linear spices &amp; seasonings above 2x
                        </label>
                      </div>
                    </div>
                  </section>

                  {/* Editable Ingredients Table */}
                  <section className="cs2-card">
                    <div className="cs2-card__head">
                      <h3 className="cs2-card__title">
                        Ingredients ({payload.ingredients.length}) · Phase 1B Handover Ready
                      </h3>
                      <button
                        type="button"
                        className="sila-btn sila-btn--sm sila-btn--secondary"
                        onClick={handleAddIngredient}
                      >
                        + Add Ingredient
                      </button>
                    </div>
                    <div className="cs2-table-wrap">
                      <table className="cs2-table" data-testid="p1a-ingredients-table">
                        <thead>
                          <tr>
                            <th>Lock</th>
                            <th>Ingredient &amp; Normalized Form</th>
                            <th className="cs2-num">Base Qty</th>
                            <th>UOM</th>
                            <th>Basis / Yield %</th>
                            <th>Scaling Method</th>
                            <th className="cs2-num">Scaled Edible</th>
                            <th className="cs2-num">Scaled Raw</th>
                            <th>Phase 1B Status</th>
                            <th />
                          </tr>
                        </thead>
                        <tbody>
                          {payload.ingredients.map((ing, idx) => {
                            const scaledRow = activeScalingDisplay?.ingredients.find(
                              (s) => s.temp_ref === ing.temp_ref
                            );
                            return (
                              <tr key={`${ing.temp_ref}-${idx}`}>
                                <td>
                                  <input
                                    type="checkbox"
                                    title="Lock ingredient during selective regeneration"
                                    aria-label={`Lock ${ing.description}`}
                                    checked={ing.locked}
                                    onChange={(e) =>
                                      handleIngredientChange(idx, { locked: e.target.checked })
                                    }
                                  />
                                </td>
                                <td style={{ minWidth: 190 }}>
                                  <input
                                    className="sila-input"
                                    aria-label={`Ingredient ${idx + 1} description`}
                                    value={ing.description}
                                    onChange={(e) =>
                                      handleIngredientChange(idx, { description: e.target.value })
                                    }
                                  />
                                  <div className="cs2-empty-note" style={{ marginTop: 2 }}>
                                    {ing.temp_ref} · Norm: {ing.normalized_description || "—"} ·{" "}
                                    <span>{ing.provenance}</span>
                                  </div>
                                  {scaledRow?.requires_manual_review && (
                                    <div className="cs2-badge cs2-badge--warn" style={{ marginTop: 3 }}>
                                      Review: {scaledRow.note || "Manual taste check"}
                                    </div>
                                  )}
                                </td>
                                <td className="cs2-num" style={{ width: 90 }}>
                                  <input
                                    type="number"
                                    step="0.001"
                                    min={0.0001}
                                    className="sila-input"
                                    aria-label={`Quantity for ${ing.description}`}
                                    value={ing.quantity}
                                    onChange={(e) =>
                                      handleIngredientChange(idx, {
                                        quantity: Math.max(0.0001, Number(e.target.value) || 0.001),
                                      })
                                    }
                                  />
                                </td>
                                <td style={{ width: 82 }}>
                                  <input
                                    className="sila-input"
                                    aria-label={`UOM for ${ing.description}`}
                                    value={ing.recipe_uom}
                                    onChange={(e) =>
                                      handleIngredientChange(idx, {
                                        recipe_uom: e.target.value.toUpperCase(),
                                      })
                                    }
                                  />
                                </td>
                                <td style={{ minWidth: 135 }}>
                                  <div style={{ display: "flex", gap: 4 }}>
                                    <select
                                      className="sila-select"
                                      aria-label={`Basis for ${ing.description}`}
                                      value={ing.quantity_basis}
                                      onChange={(e) =>
                                        handleIngredientChange(idx, { quantity_basis: e.target.value })
                                      }
                                    >
                                      <option value="RAW">RAW</option>
                                      <option value="EDIBLE">EDIBLE</option>
                                    </select>
                                    <input
                                      type="number"
                                      min={1}
                                      max={100}
                                      className="sila-input"
                                      style={{ width: 62 }}
                                      aria-label={`Yield % for ${ing.description}`}
                                      value={ing.yield_percent}
                                      onChange={(e) =>
                                        handleIngredientChange(idx, {
                                          yield_percent: Math.min(100, Math.max(1, Number(e.target.value) || 100)),
                                          yield_source: "CHEF_CONFIRMED",
                                        })
                                      }
                                    />
                                  </div>
                                  <div className="cs2-empty-note">{ing.yield_source}</div>
                                </td>
                                <td style={{ minWidth: 145 }}>
                                  <select
                                    className="sila-select"
                                    aria-label={`Scaling method for ${ing.description}`}
                                    value={ing.scaling_method}
                                    onChange={(e) =>
                                      handleIngredientChange(idx, { scaling_method: e.target.value })
                                    }
                                  >
                                    {SCALING_METHODS.map((m) => (
                                      <option key={m} value={m}>{m}</option>
                                    ))}
                                  </select>
                                </td>
                                <td className="cs2-num">
                                  <strong>{scaledRow?.scaled_edible_qty ?? ing.quantity}</strong> {ing.recipe_uom}
                                </td>
                                <td className="cs2-num">
                                  <strong>{scaledRow?.scaled_raw_qty ?? ing.quantity}</strong> {ing.recipe_uom}
                                </td>
                                <td>
                                  <span className="cs2-badge cs2-badge--planned">
                                    {ing.material_mapping_status}
                                  </span>
                                </td>
                                <td>
                                  <button
                                    type="button"
                                    className="sila-btn sila-btn--sm sila-btn--secondary"
                                    onClick={() => handleRemoveIngredient(idx)}
                                    title="Remove ingredient"
                                  >
                                    ×
                                  </button>
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                    </div>
                  </section>

                  {/* Preparation Instructions Editor */}
                  <section className="cs2-card">
                    <div className="cs2-card__head">
                      <h3 className="cs2-card__title">Preparation &amp; Cooking Instructions ({payload.instructions.length})</h3>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={handleAddStep}>
                        + Add Step
                      </button>
                    </div>
                    <div className="cs2-shell" style={{ gap: 10 }}>
                      {payload.instructions.map((step, idx) => (
                        <div key={`step-${idx}`} className="cs2-step-card">
                          <div className="cs2-card__head" style={{ marginBottom: 4 }}>
                            <div className="cs2-actions-row">
                              <span className="cs2-badge cs2-badge--indigo">Step {step.step_number}</span>
                              <input
                                className="sila-input"
                                style={{ width: 220 }}
                                value={step.step_title}
                                onChange={(e) => handleStepChange(idx, { step_title: e.target.value })}
                              />
                              <label className="cs2-checkbox-row" style={{ fontSize: 12 }}>
                                <input
                                  type="checkbox"
                                  checked={step.locked}
                                  onChange={(e) => handleStepChange(idx, { locked: e.target.checked })}
                                />
                                Lock Step
                              </label>
                            </div>
                            <div className="cs2-actions-row">
                              <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={idx === 0} onClick={() => handleMoveStep(idx, -1)}>↑</button>
                              <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={idx === payload.instructions.length - 1} onClick={() => handleMoveStep(idx, 1)}>↓</button>
                              <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => handleRemoveStep(idx)}>×</button>
                            </div>
                          </div>
                          <textarea
                            className="sila-input"
                            rows={2}
                            value={step.instruction}
                            onChange={(e) => handleStepChange(idx, { instruction: e.target.value })}
                          />
                          <div className="cs2-form-grid" style={{ gridTemplateColumns: "110px 1fr 1fr" }}>
                            <input
                              type="number"
                              min={0}
                              className="sila-input"
                              placeholder="Min"
                              value={step.estimated_duration_minutes}
                              onChange={(e) =>
                                handleStepChange(idx, { estimated_duration_minutes: Math.max(0, Number(e.target.value) || 0) })
                              }
                            />
                            <input
                              className="sila-input"
                              placeholder="Equipment suggestion"
                              value={step.equipment_suggestion}
                              onChange={(e) => handleStepChange(idx, { equipment_suggestion: e.target.value })}
                            />
                            <input
                              className="sila-input"
                              placeholder="Food safety / quality checkpoint"
                              value={step.food_safety_verification || step.quality_checkpoint}
                              onChange={(e) =>
                                handleStepChange(idx, { food_safety_verification: e.target.value })
                              }
                            />
                          </div>
                        </div>
                      ))}
                    </div>
                  </section>

                  {/* Sub-Recipes & Dietary / Allergens */}
                  <div className="cs2-grid-2">
                    <section className="cs2-card">
                      <div className="cs2-card__head">
                        <h3 className="cs2-card__title">Sub-Recipe Suggestions ({payload.sub_recipes.length})</h3>
                        <span className="cs2-badge cs2-badge--planned">Draft Suggestions Only</span>
                      </div>
                      {payload.sub_recipes.length === 0 ? (
                        <p className="cs2-empty-note">
                          No sub-recipes suggested yet. Ask the assistant: &ldquo;Separate the sauce as a sub-recipe&rdquo;.
                        </p>
                      ) : (
                        <div className="cs2-shell" style={{ gap: 10 }}>
                          {payload.sub_recipes.map((sr, idx) => (
                            <div key={sr.sub_recipe_ref || idx} className="cs2-step-card">
                              <div className="cs2-card__head" style={{ marginBottom: 4 }}>
                                <strong>{sr.sub_recipe_name}</strong>
                                <span className="cs2-badge cs2-badge--demo">
                                  Batch: {sr.batch_yield_qty} {sr.batch_yield_uom} · Parent Req: {sr.required_qty_in_parent} {sr.required_uom_in_parent}
                                </span>
                              </div>
                              <p className="cs2-empty-note">{sr.description}</p>
                              <div style={{ fontSize: 12 }}>
                                {sr.ingredients.map((si) => `${si.description} (${si.quantity} ${si.recipe_uom})`).join(", ")}
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </section>

                    <section className="cs2-card">
                      <div className="cs2-card__head">
                        <h3 className="cs2-card__title">Dietary Tags &amp; Potential Allergens</h3>
                        <span className="cs2-badge cs2-badge--warn">Requires Chef Verification</span>
                      </div>
                      <div className="sila-field">
                        <label className="sila-label" htmlFor="p1a-edit-diet">Dietary Tags (comma-separated)</label>
                        <input
                          id="p1a-edit-diet"
                          className="sila-input"
                          value={payload.header.dietary_tags.join(", ")}
                          onChange={(e) =>
                            updatePayload((p) => ({
                              ...p,
                              header: { ...p.header, dietary_tags: splitCsvList(e.target.value) },
                            }))
                          }
                        />
                      </div>
                      <div className="sila-field" style={{ marginTop: 8 }}>
                        <label className="sila-label" htmlFor="p1a-edit-allergens">Potential Allergens (comma-separated)</label>
                        <input
                          id="p1a-edit-allergens"
                          className="sila-input"
                          value={payload.header.potential_allergens.join(", ")}
                          onChange={(e) =>
                            updatePayload((p) => ({
                              ...p,
                              header: { ...p.header, potential_allergens: splitCsvList(e.target.value) },
                            }))
                          }
                        />
                      </div>
                      <div className="sila-field" style={{ marginTop: 8 }}>
                        <label className="sila-label" htmlFor="p1a-edit-chefnotes">Chef Notes</label>
                        <textarea
                          id="p1a-edit-chefnotes"
                          className="sila-input"
                          rows={2}
                          value={payload.chef_notes}
                          onChange={(e) =>
                            updatePayload((p) => ({ ...p, chef_notes: e.target.value }))
                          }
                        />
                      </div>
                      <p className="cs2-empty-note" style={{ marginTop: 8 }}>
                        AI-suggested dietary tags and allergen warnings are not certified allergen-free or Halal certifications.
                        {payload.header.cross_contact_warnings.length > 0 && (
                          <> Cross-contact: {payload.header.cross_contact_warnings.join("; ")}</>
                        )}
                      </p>
                    </section>
                  </div>
                </>
              )}
            </div>

            {/* RIGHT PANEL — CULINARY AI ASSISTANT, REGEN & HISTORY */}
            <aside className="cs2-card cs2-planner-assistant" data-testid="culinary-planner-assistant">
              <div className="cs2-nav-tabs" style={{ marginBottom: 12 }}>
                <button
                  type="button"
                  className={`cs2-nav-tab ${rightTab === "ASSISTANT" ? "is-active" : ""}`}
                  onClick={() => setRightTab("ASSISTANT")}
                >
                  AI Assistant
                </button>
                <button
                  type="button"
                  className={`cs2-nav-tab ${rightTab === "REGEN" ? "is-active" : ""}`}
                  onClick={() => setRightTab("REGEN")}
                >
                  Selective Regen
                </button>
                <button
                  type="button"
                  className={`cs2-nav-tab ${rightTab === "HISTORY" ? "is-active" : ""}`}
                  onClick={() => {
                    setRightTab("HISTORY");
                    refreshHistory();
                  }}
                >
                  History ({drafts.length})
                </button>
              </div>

              {rightTab === "ASSISTANT" && (
                <div className="cs2-shell" style={{ gap: 10 }}>
                  <div className="cs2-chip-row">
                    {QUICK_REFINEMENTS.map((prompt) => (
                      <button
                        key={prompt}
                        type="button"
                        className="cs2-chip"
                        disabled={!payload || refining}
                        onClick={() => handleRefine(prompt)}
                      >
                        {prompt}
                      </button>
                    ))}
                  </div>

                  <div className="cs2-chat-log">
                    {conversation.length === 0 ? (
                      <p className="cs2-empty-note">
                        Generate or load a recipe, then refine it conversationally (e.g., &ldquo;Replace chicken with paneer&rdquo;).
                      </p>
                    ) : (
                      conversation.map((turn, idx) => (
                        <div
                          key={idx}
                          className={`cs2-chat-turn ${turn.role === "CHEF" ? "cs2-chat-turn--chef" : "cs2-chat-turn--ai"}`}
                        >
                          <div style={{ fontSize: 10.5, fontWeight: 700, opacity: 0.75 }}>
                            {turn.role}
                            {turn.provider ? ` · ${turn.provider}/${turn.model}` : ""}
                          </div>
                          <div>{turn.message}</div>
                        </div>
                      ))
                    )}
                  </div>

                  {pendingRefine && (
                    <div className="cs2-diff-box" data-testid="p1a-refine-preview">
                      <div style={{ fontWeight: 700, fontSize: 12.5 }}>
                        Proposed Change Preview ({pendingRefine.diffSummary.length} diff(s))
                      </div>
                      <div style={{ fontSize: 11.5, color: "#312e81" }}>
                        {pendingRefine.provider} / {pendingRefine.model} · {pendingRefine.summaryMessage}
                      </div>
                      {pendingRefine.diffSummary.slice(0, 8).map((d, i) => (
                        <div key={i} style={{ fontSize: 11.5, borderTop: "1px solid #e0e7ff", paddingTop: 4 }}>
                          <strong>[{d.section}] {d.field}:</strong>{" "}
                          <span style={{ textDecoration: "line-through", color: "#991b1b" }}>{d.before}</span>{" "}
                          → <span style={{ color: "#065f46" }}>{d.after}</span>
                        </div>
                      ))}
                      <div className="cs2-actions-row">
                        <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={handleAcceptProposed}>
                          Accept Proposed
                        </button>
                        <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={handleRejectProposed}>
                          Reject
                        </button>
                      </div>
                    </div>
                  )}

                  <div style={{ display: "flex", gap: 6 }}>
                    <input
                      className="sila-input"
                      aria-label="Conversational refinement instruction"
                      placeholder="Ask AI to refine active recipe..."
                      disabled={!payload || refining}
                      value={refineInstruction}
                      onChange={(e) => setRefineInstruction(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") {
                          e.preventDefault();
                          handleRefine();
                        }
                      }}
                    />
                    <button
                      type="button"
                      className="sila-btn sila-btn--sm sila-btn--primary"
                      disabled={!payload || refining || !refineInstruction.trim()}
                      onClick={() => handleRefine()}
                    >
                      {refining ? "..." : "Send"}
                    </button>
                  </div>

                  {previousPayload && (
                    <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={handleUndoLastEdit}>
                      Undo Last Change
                    </button>
                  )}

                  {validationWarnings.length > 0 && (
                    <div className="cs2-banner cs2-banner--warn" style={{ flexDirection: "column", gap: 4 }}>
                      <strong style={{ fontSize: 12 }}>Validation Warnings ({validationWarnings.length})</strong>
                      {validationWarnings.map((w, i) => (
                        <div key={i} style={{ fontSize: 11.5 }}>
                          [{w.severity}] {w.message}
                        </div>
                      ))}
                    </div>
                  )}

                  {payload && payload.assumptions.length > 0 && (
                    <div style={{ fontSize: 12, color: "#475569" }}>
                      <strong>AI Assumptions:</strong>
                      <ul style={{ margin: "4px 0 0 16px", padding: 0 }}>
                        {payload.assumptions.map((a, i) => (
                          <li key={i}>{a}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              )}

              {rightTab === "REGEN" && (
                <div className="cs2-shell" style={{ gap: 10 }}>
                  <p className="cs2-empty-note">
                    Select sections to regenerate while preserving locked fields and locked ingredients.
                  </p>
                  <div>
                    <div className="sila-label" style={{ marginBottom: 4 }}>Sections to Regenerate</div>
                    {REGEN_SECTIONS.map((sec) => (
                      <label key={sec.id} className="cs2-checkbox-row" style={{ marginBottom: 4, fontSize: 12.5 }}>
                        <input
                          type="checkbox"
                          checked={regenSections.includes(sec.id)}
                          onChange={(e) =>
                            setRegenSections((prev) =>
                              e.target.checked ? [...prev, sec.id] : prev.filter((s) => s !== sec.id)
                            )
                          }
                        />
                        {sec.label}
                      </label>
                    ))}
                  </div>
                  <div>
                    <div className="sila-label" style={{ marginBottom: 4 }}>Locked Fields</div>
                    {[
                      ["base_servings", "Keep serving count unchanged"],
                      ["serving_size", "Keep serving size unchanged"],
                      ["instructions", "Keep approved preparation instructions"],
                    ].map(([fieldId, label]) => (
                      <label key={fieldId} className="cs2-checkbox-row" style={{ marginBottom: 4, fontSize: 12.5 }}>
                        <input
                          type="checkbox"
                          checked={lockedFields.includes(fieldId)}
                          onChange={(e) =>
                            setLockedFields((prev) =>
                              e.target.checked ? [...prev, fieldId] : prev.filter((f) => f !== fieldId)
                            )
                          }
                        />
                        {label}
                      </label>
                    ))}
                  </div>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1a-regen-guide">Chef Guidance</label>
                    <textarea
                      id="p1a-regen-guide"
                      className="sila-input"
                      rows={2}
                      placeholder="Optional guidance for selected sections..."
                      value={regenGuidance}
                      onChange={(e) => setRegenGuidance(e.target.value)}
                    />
                  </div>
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--primary"
                    disabled={!payload || refining || regenSections.length === 0}
                    onClick={handleRegenerateSelected}
                  >
                    {refining ? "Regenerating..." : "Regenerate Selected Sections"}
                  </button>
                </div>
              )}

              {rightTab === "HISTORY" && (
                <div className="cs2-shell" style={{ gap: 10 }}>
                  <div style={{ display: "flex", gap: 6 }}>
                    <input
                      className="sila-input"
                      placeholder="Search drafts..."
                      value={draftSearch}
                      onChange={(e) => setDraftSearch(e.target.value)}
                    />
                    <select
                      className="sila-select"
                      aria-label="Filter draft status"
                      value={draftStatusFilter}
                      onChange={(e) => setDraftStatusFilter(e.target.value)}
                      style={{ width: 125 }}
                    >
                      <option value="">All Status</option>
                      <option value="GENERATED">Generated</option>
                      <option value="CHEF_REVIEW">Chef Review</option>
                      <option value="READY_FOR_MATERIAL_MAPPING">Ready 1B</option>
                      <option value="CANCELLED">Cancelled</option>
                    </select>
                  </div>
                  <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={refreshHistory}>
                    Refresh History
                  </button>
                  <div style={{ maxHeight: 280, overflowY: "auto", display: "flex", flexDirection: "column", gap: 6 }}>
                    {drafts.map((d) => (
                      <div key={d.id} className="cs2-step-card" style={{ padding: 8 }}>
                        <div style={{ display: "flex", justifyContent: "space-between", gap: 6 }}>
                          <strong style={{ fontSize: 12.5 }}>{d.recipeName}</strong>
                          <span className="cs2-badge cs2-badge--indigo">Rev {d.revisionNumber}</span>
                        </div>
                        <div style={{ fontSize: 11, color: "#64748b" }}>
                          {d.draftReference} · {d.provider}/{d.model} · {d.baseServings} srv · {d.ingredientCount} ing · {d.status}
                        </div>
                        <div className="cs2-actions-row">
                          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => handleOpenDraft(d.id)}>
                            Open
                          </button>
                        </div>
                      </div>
                    ))}
                    {drafts.length === 0 && <p className="cs2-empty-note">No AI recipe drafts found yet.</p>}
                  </div>

                  {activeDraft && activeDraft.revisions.length > 0 && (
                    <div style={{ borderTop: "1px solid #e2e8f0", paddingTop: 8 }}>
                      <div className="sila-label" style={{ marginBottom: 4 }}>
                        Active Draft Revisions ({activeDraft.revisions.length})
                      </div>
                      <div style={{ maxHeight: 170, overflowY: "auto", display: "flex", flexDirection: "column", gap: 4 }}>
                        {activeDraft.revisions.map((r) => (
                          <div key={r.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", fontSize: 11.5 }}>
                            <span>
                              Rev #{r.revisionNumber} · {r.changeType} ({r.actorName})
                            </span>
                            <div className="cs2-actions-row">
                              <button
                                type="button"
                                className="cs2-chip"
                                onClick={() => setCompareRevisionNumber(r.revisionNumber)}
                              >
                                Compare
                              </button>
                              <button
                                type="button"
                                className="cs2-chip"
                                onClick={() => handleRestoreRevision(r.revisionNumber)}
                              >
                                Restore
                              </button>
                            </div>
                          </div>
                        ))}
                      </div>
                      {revisionComparison && (
                        <div className="cs2-diff-box" style={{ marginTop: 8 }}>
                          <strong style={{ fontSize: 12 }}>
                            Diff: Rev #{compareRevisionNumber} → Current ({revisionComparison.length})
                          </strong>
                          {revisionComparison.length === 0 ? (
                            <span className="cs2-empty-note">No differences detected.</span>
                          ) : (
                            revisionComparison.map((d, i) => (
                              <div key={i} style={{ fontSize: 11 }}>
                                <strong>{d.field}:</strong> {d.before} → {d.after}
                              </div>
                            ))
                          )}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              )}
            </aside>
          </div>

          {/* STICKY CONTEXTUAL ACTION BAR */}
          {activeDraft && payload && (
            <div className="cs2-footer-bar" data-testid="p1a-action-bar">
              <div className="cs2-actions-row">
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  disabled={saving}
                  onClick={() => handleSaveDraft("CHEF_REVIEW")}
                >
                  {saving ? "Saving..." : "Save Draft"}
                </button>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--secondary"
                  disabled={saving}
                  onClick={() => handleSaveDraft("READY_FOR_MATERIAL_MAPPING")}
                >
                  Mark Ready for Material Mapping
                </button>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={handleDuplicateDraft}>
                  Duplicate
                </button>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => handleExport("JSON")}>
                  Export JSON
                </button>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => handleExport("CSV")}>
                  Export CSV
                </button>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={handleCancelDraft}>
                  Cancel Draft
                </button>
              </div>
              <div className="cs2-actions-row">
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  data-testid="p1a-continue-material-mapping"
                  disabled={saving}
                  onClick={() => {
                    if (onContinueToMaterialMapping) {
                      onContinueToMaterialMapping(activeDraft.id);
                    } else {
                      onOpenWorkspace("materials");
                    }
                  }}
                >
                  Continue to Material Mapping (Phase 1B)
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
};

export default CulinaryRecipePlannerWorkspace;
