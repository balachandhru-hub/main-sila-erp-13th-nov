import React, { useEffect, useMemo, useRef, useState } from "react";
import { Modal, toastService } from "@vosox/shared-ui";
import {
  getLocations,
  getMyLocations,
  type SilaLocation,
} from "../../../api/silaMe/silaInventoryApi";
import {
  createRecipe,
  createRecipeRevision,
  deleteRecipeDraft,
  getRecipe,
  removeRecipePicture,
  setRecipePicture,
  submitRecipe,
  updateRecipe,
  type SilaRecipeDetail,
} from "../../../api/silaMe/silaRecipeApi";
import {
  getRecipeMasters,
  searchIngredients,
  type SilaIngredientOption,
  type SilaRecipeMaster,
} from "../../../api/silaMe/silaRecipeToolsApi";
import {
  duplicateLine,
  materialLine,
  rememberMaterial,
  replaceMaterialOnLine,
  costPerPortion as draftCostPerPortion,
} from "./recipeBuilderState";
import {
  lineCost,
  toNumber,
  totalCostAt,
  type IngredientLine,
} from "./recipeEditorModel";
import { formatCost, quantityForUomChange, recipeBadgeClass, versionsSummary } from "./recipeFormat";
import {
  buildStudioCreateRequest,
  validateStudioDraft,
  verifyPersistedDraft,
  type StudioSaveFieldErrors,
} from "./recipeStudioDraftSave";
import { studioEditAccess } from "./recipeStudioEditAccess";
import {
  canDeleteDraftRecipe,
  DELETE_DRAFT_BODY,
  DELETE_DRAFT_CONFIRM_LABEL,
  DELETE_DRAFT_SUCCESS,
  DELETE_DRAFT_TITLE,
  DISCARD_CHANGES_LABEL,
} from "./recipeStudioDraftDelete";
import { formFromRecipeDetail } from "./recipeStudioEditLoad";
import {
  loadTargetFoodCostPercent,
  recommendMaterials,
  saveTargetFoodCostPercent,
  suggestClassification,
  suggestSellingPrice,
} from "./recipeStudioSuggestions";
import SilaRecipeCanvas from "./SilaRecipeCanvas";
import SilaRecipeMaterialLibrary from "./SilaRecipeMaterialLibrary";
import SilaRecipePicture from "./SilaRecipePicture";
import { RECIPE_WORKSPACE_BACK_LABEL } from "./recipeWorkspaceShellCopy";
import "./SilaRecipeStudioPresentation.css";

export type RecipeStudioMode = "new" | "edit";

interface SilaRecipeStudioPresentationProps {
  mode: RecipeStudioMode;
  /** Route id in Edit mode. */
  recipeId?: string | null;
  onBackToCatalog: () => void;
  /** After successful create + verify, parent navigates to Recipe Detail. */
  onDraftSaved?: (recipeId: string) => void;
}

/**
 * Step 7 Recipe Studio presentation.
 * Create + Edit load/update, picture management, explicit revision, and Submit.
 */
const SilaRecipeStudioPresentation: React.FC<SilaRecipeStudioPresentationProps> = ({
  mode,
  recipeId,
  onBackToCatalog,
  onDraftSaved,
}) => {
  const isEdit = mode === "edit";
  const idText = (recipeId ?? "").trim();

  const [name, setName] = useState("");
  const [recipeCode, setRecipeCode] = useState("");
  const [familyId, setFamilyId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [cuisine, setCuisine] = useState("");
  const [mealPeriod, setMealPeriod] = useState("");
  const [yieldQty, setYieldQty] = useState("1");
  const [servingUom, setServingUom] = useState("EA");
  const [prepTime, setPrepTime] = useState("");
  const [targetFoodCost, setTargetFoodCost] = useState(() => String(loadTargetFoodCostPercent()));
  const [classConfirmed, setClassConfirmed] = useState(false);
  const [menuItem, setMenuItem] = useState("");
  const [outletNote, setOutletNote] = useState("");
  const [outletPrices, setOutletPrices] = useState<Record<string, string>>({});

  const [lines, setLines] = useState<IngredientLine[]>([]);
  const [replaceKey, setReplaceKey] = useState<string | null>(null);
  const [outlets, setOutlets] = useState<SilaLocation[]>([]);
  const [outletLocationId, setOutletLocationId] = useState("");
  const [families, setFamilies] = useState<SilaRecipeMaster[]>([]);
  const [categories, setCategories] = useState<SilaRecipeMaster[]>([]);
  const [catalogPool, setCatalogPool] = useState<SilaIngredientOption[]>([]);
  const [helperNotice, setHelperNotice] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<StudioSaveFieldErrors>({});
  const [saving, setSaving] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [revising, setRevising] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [verifyWarning, setVerifyWarning] = useState<string | null>(null);
  const [loadedRecipe, setLoadedRecipe] = useState<SilaRecipeDetail | null>(null);
  const [loadingRecipe, setLoadingRecipe] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [dirty, setDirty] = useState(false);
  const [localPictureFile, setLocalPictureFile] = useState<File | null>(null);
  const [localPreviewUrl, setLocalPreviewUrl] = useState<string | null>(null);
  const [hasPicture, setHasPicture] = useState(false);
  const [pictureBusy, setPictureBusy] = useState(false);
  const [pictureError, setPictureError] = useState<string | null>(null);
  const [confirmDeleteDraft, setConfirmDeleteDraft] = useState(false);
  const [deletingDraft, setDeletingDraft] = useState(false);
  /** Full OUTLET catalog (not just authorized) used to resolve priced / newly selected outlets like FL10. */
  const [outletCatalog, setOutletCatalog] = useState<SilaLocation[]>([]);
  const outletCatalogRef = useRef<SilaLocation[]>([]);
  const [pictureEpoch, setPictureEpoch] = useState(0);
  /** Prevents a second CREATE for the same local draft if navigation is slow. */
  const createdIdRef = useRef<string | null>(null);
  const revisionGuardRef = useRef(false);
  const previewUrlRef = useRef<string | null>(null);

  const markDirty = () => setDirty(true);

  const clearLocalPicture = () => {
    if (previewUrlRef.current) {
      URL.revokeObjectURL(previewUrlRef.current);
      previewUrlRef.current = null;
    }
    setLocalPreviewUrl(null);
    setLocalPictureFile(null);
  };

  const assignLocalPicture = (file: File) => {
    clearLocalPicture();
    const url = URL.createObjectURL(file);
    previewUrlRef.current = url;
    setLocalPreviewUrl(url);
    setLocalPictureFile(file);
    setPictureError(null);
    markDirty();
  };

  const applyLoadedForm = (detail: SilaRecipeDetail) => {
    const form = formFromRecipeDetail(detail, []);
    setName(form.name);
    setRecipeCode(form.recipeCode);
    setFamilyId(form.familyId);
    setCategoryId(form.categoryId);
    setCuisine(form.cuisine);
    setMealPeriod(form.mealPeriod);
    setYieldQty(form.yieldQty);
    setServingUom(form.servingUom);
    setPrepTime(form.prepTime);
    setMenuItem(form.menuItem);
    setOutletNote(form.outletNote);
    setClassConfirmed(form.classConfirmed);
    setLines(form.lines);
    setOutletPrices(form.outletPrices);
    setHasPicture(Boolean(detail.hasPicture));
    clearLocalPicture();
    setPictureError(null);
    setPictureEpoch((value) => value + 1);
    setDirty(false);
    setFieldErrors({});
    setReplaceKey(null);
  };

  useEffect(() => () => {
    if (previewUrlRef.current) URL.revokeObjectURL(previewUrlRef.current);
  }, []);

  useEffect(() => {
    let active = true;
    const boot = async () => {
      try {
        const [allLocations, myLocations, loadedFamilies, loadedCategories, browse] = await Promise.all([
          getLocations().catch(() => [] as SilaLocation[]),
          getMyLocations()
            .then((items) => ({ ok: true as const, items }))
            .catch(() => ({ ok: false as const, items: [] as SilaLocation[] })),
          getRecipeMasters("families").catch(() => [] as SilaRecipeMaster[]),
          getRecipeMasters("categories").catch(() => [] as SilaRecipeMaster[]),
          searchIngredients({ search: "", limit: 60 }).catch(() => ({
            items: [] as SilaIngredientOption[],
            total: 0,
            index: 0,
            limit: 60,
          })),
        ]);
        if (!active) return;
        const allOutlets = allLocations.filter((location) => location.locationType === "OUTLET");
        const scoped = (myLocations.ok ? myLocations.items : allLocations)
          .filter((location) => location.locationType === "OUTLET");
        // Prefer authorized outlets; keep full OUTLET catalog as fallback so FL* codes stay selectable.
        const sorted = (scoped.length > 0 ? scoped : allOutlets)
          .slice()
          .sort((a, b) => a.locationName.localeCompare(b.locationName));
        const catalog = (allOutlets.length > 0 ? allOutlets : sorted)
          .slice()
          .sort((a, b) => a.locationName.localeCompare(b.locationName));
        outletCatalogRef.current = catalog;
        setOutletCatalog(catalog);
        setOutlets(sorted);
        setFamilies(loadedFamilies);
        setCategories(loadedCategories);
        setCatalogPool(browse.items);
        setHelperNotice(null);
      } catch (err: unknown) {
        if (active) {
          setHelperNotice(err instanceof Error ? err.message : "Could not load Material Catalog helpers.");
        }
      }
    };
    void boot();
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (!isEdit) {
      setLoadedRecipe(null);
      setLoadError(null);
      setLoadingRecipe(false);
      return undefined;
    }
    if (!idText) {
      setLoadedRecipe(null);
      setLoadError("Edit URL is missing a recipe id.");
      setLoadingRecipe(false);
      return undefined;
    }
    let active = true;
    const load = async () => {
      setLoadingRecipe(true);
      setLoadError(null);
      try {
        const detail = await getRecipe(idText);
        if (!active) return;
        setLoadedRecipe(detail);
        applyLoadedForm(detail);
        // Ensure every saved outlet (and common codes like FL10) appears even if not in myLocations.
        setOutlets((current) => {
          const byId = new Map(current.map((outlet) => [outlet.id, outlet]));
          for (const outlet of outletCatalogRef.current) byId.set(outlet.id, outlet);
          for (const price of detail.outletPrices ?? []) {
            if (byId.has(price.outletLocationId)) continue;
            const found = outletCatalogRef.current.find((outlet) => outlet.id === price.outletLocationId);
            if (found) byId.set(found.id, found);
          }
          return Array.from(byId.values()).sort((a, b) => a.locationName.localeCompare(b.locationName));
        });
        const firstOutlet = detail.outletPrices?.[0]?.outletLocationId;
        if (firstOutlet) setOutletLocationId(firstOutlet);
      } catch (err: unknown) {
        if (!active) return;
        setLoadedRecipe(null);
        setLoadError(err instanceof Error ? err.message : "Could not load the recipe.");
      } finally {
        if (active) setLoadingRecipe(false);
      }
    };
    void load();
    return () => {
      active = false;
    };
  }, [isEdit, idText]);

  const costs = useMemo(() => lines.map((line) => lineCost(line, outletLocationId)), [lines, outletLocationId]);
  const totalCost = useMemo(() => totalCostAt(lines, outletLocationId), [lines, outletLocationId]);
  const currency = lines.find((line) => line.material?.currency)?.material?.currency ?? "";
  const portions = toNumber(yieldQty);
  const targetPercent = (() => {
    const value = Number(targetFoodCost);
    return Number.isFinite(value) && value > 0 && value < 100 ? value : 30;
  })();
  const pricing = useMemo(
    () => suggestSellingPrice(totalCost, portions > 0 ? portions : 1, targetPercent, costs),
    [totalCost, portions, targetPercent, costs],
  );
  const portionCost = draftCostPerPortion(totalCost, portions > 0 ? portions : 1);
  const materialRecs = useMemo(
    () => recommendMaterials(lines, [], catalogPool, 24),
    [lines, catalogPool],
  );
  const classification = useMemo(
    () => suggestClassification(lines, families, categories),
    [lines, families, categories],
  );
  const addedIds = useMemo(
    () => lines.map((line) => line.material?.id).filter((id): id is string => Boolean(id)),
    [lines],
  );
  const selectedOutlet = outlets.find((outlet) => outlet.id === outletLocationId) ?? null;
  const heading = isEdit ? "Edit Recipe" : "Create New Recipe";
  const familyName = families.find((item) => item.id === familyId)?.name ?? "";
  const categoryName = categories.find((item) => item.id === categoryId)?.name ?? "";
  const status = loadedRecipe?.status ?? (isEdit ? "" : "DRAFT");
  const editAccess = isEdit ? studioEditAccess(status) : { kind: "editable" as const };
  const canEdit = !isEdit || editAccess.kind === "editable";
  const busy = saving || submitting || revising || loadingRecipe || deletingDraft || pictureBusy;
  const showDeleteDraft = isEdit && Boolean(loadedRecipe) && canDeleteDraftRecipe(status);
  const showDiscardChanges = !isEdit;
  const persistedEditId = (loadedRecipe?.id || idText || "").trim();

  const commitLines = (next: IngredientLine[] | ((current: IngredientLine[]) => IngredientLine[])) => {
    if (!canEdit) return;
    markDirty();
    setLines((current) => (typeof next === "function" ? next(current) : next));
  };

  const buildOptionalDescription = (): string => {
    const notes = [
      cuisine.trim() ? `Cuisine: ${cuisine.trim()}` : "",
      mealPeriod.trim() ? `Meal period: ${mealPeriod.trim()}` : "",
      prepTime.trim() ? `Prep: ${prepTime.trim()}` : "",
      menuItem.trim() ? `Menu item note: ${menuItem.trim()}` : "",
      outletNote.trim() ? `Outlet note: ${outletNote.trim()}` : "",
    ].filter(Boolean);
    return notes.join(" · ");
  };

  const buildRequest = () => {
    const header = {
      name,
      description: buildOptionalDescription(),
      familyId,
      categoryId,
      itemMode: "RECIPE" as const,
      servingQty: yieldQty,
      servingUom,
      sellingUom: loadedRecipe?.sellingUom || "EA",
      currency: currency || loadedRecipe?.currency || undefined,
    };
    const draftLines = lines.map((line) => ({
      materialId: line.material?.id ?? null,
      subRecipeId: line.subRecipe?.id ?? null,
      quantity: line.quantity,
      uom: line.uom,
    }));
    const prices = Object.entries(outletPrices)
      .filter(([, value]) => value.trim() !== "")
      .map(([outletLocationIdKey, value]) => ({
        outletLocationId: outletLocationIdKey,
        menuPrice: toNumber(value),
        currency: currency || loadedRecipe?.currency || null,
      }));
    return {
      header,
      draftLines,
      request: buildStudioCreateRequest(header, draftLines, prices),
      errors: validateStudioDraft(header, draftLines),
    };
  };

  const verifyDetail = (
    detail: SilaRecipeDetail,
    request: ReturnType<typeof buildStudioCreateRequest>,
  ) => verifyPersistedDraft(
    {
      name: detail.name,
      status: detail.status,
      ingredients: detail.ingredients.map((ingredient) => ({
        sequence: ingredient.sequence,
        materialId: ingredient.materialId,
        quantity: ingredient.quantity,
        uom: ingredient.uom,
      })),
    },
    request,
  );

  const persistPendingPicture = async (recipeId: string): Promise<boolean> => {
    if (!localPictureFile) return true;
    setPictureBusy(true);
    setPictureError(null);
    try {
      await setRecipePicture(recipeId, localPictureFile);
      clearLocalPicture();
      setHasPicture(true);
      setPictureEpoch((value) => value + 1);
      return true;
    } catch (err: unknown) {
      const message = err instanceof Error
        ? `Recipe saved, but the picture was not stored: ${err.message}`
        : "Recipe saved, but the picture was not stored.";
      setPictureError(message);
      toastService.error(message);
      return false;
    } finally {
      setPictureBusy(false);
    }
  };

  const handleChoosePicture = async (file: File) => {
    if (!canEdit) return;
    assignLocalPicture(file);
    const targetId = isEdit ? persistedEditId : (createdIdRef.current || "");
    if (!targetId) return;
    setPictureBusy(true);
    setPictureError(null);
    try {
      await setRecipePicture(targetId, file);
      clearLocalPicture();
      setHasPicture(true);
      setPictureEpoch((value) => value + 1);
      toastService.success("Recipe picture saved.");
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Could not save the recipe picture.";
      setPictureError(message);
      toastService.error(message);
    } finally {
      setPictureBusy(false);
    }
  };

  const handleRemovePicture = async () => {
    if (!canEdit) return;
    setPictureError(null);
    if (localPictureFile || localPreviewUrl) {
      clearLocalPicture();
      markDirty();
      return;
    }
    const targetId = isEdit ? persistedEditId : (createdIdRef.current || "");
    if (!targetId || !hasPicture) {
      setHasPicture(false);
      return;
    }
    setPictureBusy(true);
    try {
      const info = await removeRecipePicture(targetId);
      setHasPicture(Boolean(info.hasPicture));
      setPictureEpoch((value) => value + 1);
      toastService.success("Recipe picture removed.");
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Could not remove the recipe picture.";
      setPictureError(message);
      toastService.error(message);
    } finally {
      setPictureBusy(false);
    }
  };

  const handleSaveDraft = async (options?: { skipNavigate?: boolean }): Promise<string | null> => {
    if (saving || submitting || revising) return null;
    if (isEdit && editAccess.kind !== "editable") {
      toastService.error(editAccess.reason);
      return null;
    }
    setSaveError(null);
    setVerifyWarning(null);
    const { request, errors } = buildRequest();
    setFieldErrors(errors);
    const problem = errors.name || errors.serving || errors.ingredients || null;
    if (problem) {
      toastService.error(problem);
      return null;
    }
    setSaving(true);
    let persistedId = isEdit ? persistedEditId : (createdIdRef.current || "");
    try {
      if (persistedId) {
        // Existing Draft / create-guard id: always PUT — never POST create again.
        await updateRecipe(persistedId, request);
      } else {
        persistedId = await createRecipe(request);
        createdIdRef.current = persistedId;
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Could not save the recipe.";
      setSaveError(message);
      toastService.error(message);
      setSaving(false);
      return null;
    }
    if (!persistedId) {
      const message = "Save completed but no recipe id was returned. Your draft is still on this page.";
      setSaveError(message);
      toastService.error(message);
      setSaving(false);
      return null;
    }
    try {
      const detail = await getRecipe(persistedId);
      const check = verifyDetail(detail, request);
      if (!check.ok) {
        setVerifyWarning(check.warning || "Saved recipe could not be fully verified.");
        toastService.error(check.warning || "Saved, but persistence verification reported issues.");
        setSaving(false);
        return persistedId;
      }
      const pictureOk = await persistPendingPicture(persistedId);
      const refreshed = pictureOk ? await getRecipe(persistedId).catch(() => detail) : detail;
      setLoadedRecipe(refreshed);
      applyLoadedForm(refreshed);
      toastService.success(
        isEdit
          ? `Draft updated (${refreshed.recipeCode || persistedId}).`
          : `Recipe saved as draft (${refreshed.recipeCode || persistedId}).`,
      );
      if (!isEdit && !options?.skipNavigate) onDraftSaved?.(persistedId);
      return persistedId;
    } catch (err: unknown) {
      const message = err instanceof Error
        ? `Draft saved (${persistedId}) but could not be re-read: ${err.message}`
        : `Draft saved (${persistedId}) but could not be re-read.`;
      setVerifyWarning(message);
      toastService.error(message);
      await persistPendingPicture(persistedId);
      return persistedId;
    } finally {
      setSaving(false);
    }
  };

  const handleCreateRevision = async () => {
    if (!loadedRecipe || revising || revisionGuardRef.current) return;
    if (editAccess.kind !== "revision-required") return;
    revisionGuardRef.current = true;
    setRevising(true);
    setSaveError(null);
    try {
      const nextVersion = await createRecipeRevision(loadedRecipe);
      const detail = await getRecipe(loadedRecipe.id);
      setLoadedRecipe(detail);
      applyLoadedForm(detail);
      toastService.success(`Draft revision v${nextVersion} created. Approved selling version is unchanged.`);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Could not create a revision.";
      setSaveError(message);
      toastService.error(message);
    } finally {
      setRevising(false);
      revisionGuardRef.current = false;
    }
  };

  const handleSubmit = async () => {
    if (submitting || saving || revising) return;
    if (isEdit && editAccess.kind !== "editable") {
      toastService.error(editAccess.reason);
      return;
    }
    // Persist first without leaving Create mode, then submit via existing API.
    const savedId = await handleSaveDraft({ skipNavigate: true });
    if (!savedId) return;
    setSubmitting(true);
    setSaveError(null);
    try {
      await submitRecipe(savedId);
      const detail = await getRecipe(savedId);
      setLoadedRecipe(detail);
      applyLoadedForm(detail);
      toastService.success(`Submitted for approval (${detail.status}).`);
      if (!isEdit) onDraftSaved?.(savedId);
    } catch (err: unknown) {
      const message = err instanceof Error
        ? `Saved as draft, but submit failed: ${err.message}`
        : "Saved as draft, but submit failed.";
      setSaveError(message);
      toastService.error(message);
    } finally {
      setSubmitting(false);
    }
  };

  const handleBack = () => {
    if (dirty && !window.confirm("You have unsaved changes. Leave Recipe Studio anyway?")) return;
    onBackToCatalog();
  };

  const handleDeleteDraft = async () => {
    const targetId = (loadedRecipe?.id || idText).trim();
    if (!targetId || !showDeleteDraft) return;
    setDeletingDraft(true);
    setSaveError(null);
    try {
      await deleteRecipeDraft(targetId);
      toastService.success(DELETE_DRAFT_SUCCESS);
      setConfirmDeleteDraft(false);
      setDirty(false);
      onBackToCatalog();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Could not delete the draft recipe.";
      setSaveError(message);
      toastService.error(message);
    } finally {
      setDeletingDraft(false);
    }
  };

  const addMaterial = (option: SilaIngredientOption) => {
    if (!canEdit) return;
    rememberMaterial(option.id);
    const line = materialLine(option);
    setFieldErrors((current) => ({ ...current, ingredients: undefined }));
    commitLines((current) => (current.some((x) => x.material?.id === line.material?.id) ? current : [...current, line]));
  };

  const addMaterials = (options: SilaIngredientOption[]) => {
    if (!canEdit) return;
    options.forEach((option) => rememberMaterial(option.id));
    setFieldErrors((current) => ({ ...current, ingredients: undefined }));
    commitLines((current) => {
      const next = [...current];
      options.forEach((option) => {
        if (!next.some((x) => x.material?.id === option.id)) next.push(materialLine(option));
      });
      return next;
    });
  };

  const changeLine = (key: string, change: Partial<IngredientLine>) => {
    if (!canEdit) return;
    commitLines((current) =>
      current.map((line) => {
        if (line.key !== key) return line;
        if (change.uom != null && line.material && change.uom !== line.uom) {
          const qty = toNumber(change.quantity ?? line.quantity);
          const converted = quantityForUomChange(line.material, qty, line.uom, change.uom);
          const qtyText = Number.isInteger(converted.quantity)
            ? String(converted.quantity)
            : String(Number(converted.quantity.toFixed(4)));
          return {
            ...line,
            ...change,
            quantity: change.quantity ?? (converted.convertible ? qtyText : line.quantity),
            uom: change.uom,
          };
        }
        return { ...line, ...change };
      }),
    );
  };

  const moveLine = (key: string, direction: -1 | 1) => {
    commitLines((current) => {
      const index = current.findIndex((line) => line.key === key);
      const target = index + direction;
      if (index < 0 || target < 0 || target >= current.length) return current;
      const next = [...current];
      const [row] = next.splice(index, 1);
      next.splice(target, 0, row);
      return next;
    });
  };

  const applyClassificationSuggestion = () => {
    if (!canEdit || !classification) return;
    markDirty();
    setFamilyId(classification.familyId || familyId);
    setCategoryId(classification.categoryId || categoryId);
    if (classification.cuisine) setCuisine(classification.cuisine);
    if (classification.mealPeriod) setMealPeriod(classification.mealPeriod);
    setClassConfirmed(true);
  };

  const costLabel = (value: number | null | undefined): string =>
    value == null ? "COST PENDING" : formatCost(value, currency);

  const setHeader = <T,>(setter: React.Dispatch<React.SetStateAction<T>>, value: T) => {
    if (!canEdit) return;
    markDirty();
    setter(value);
  };

  const versionLabel = loadedRecipe
    ? versionsSummary(loadedRecipe.versions?.length
      ? loadedRecipe.versions
      : [{ version: loadedRecipe.version, status: loadedRecipe.status }])
    : "";

  if (isEdit && loadingRecipe && !loadedRecipe) {
    return (
      <div className="rstudio-build" data-testid="recipe-studio-presentation" data-mode={mode}>
        <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm rstudio-back" onClick={handleBack}>
          ← {RECIPE_WORKSPACE_BACK_LABEL}
        </button>
        <div className="sila-alert sila-alert--info" role="status" data-testid="recipe-studio-loading">
          Loading recipe {idText}…
        </div>
      </div>
    );
  }

  if (isEdit && loadError && !loadedRecipe) {
    return (
      <div className="rstudio-build" data-testid="recipe-studio-presentation" data-mode={mode}>
        <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm rstudio-back" onClick={handleBack}>
          ← {RECIPE_WORKSPACE_BACK_LABEL}
        </button>
        <div className="sila-alert sila-alert--danger" role="alert" data-testid="recipe-studio-load-error">
          {loadError}
        </div>
      </div>
    );
  }

  return (
    <div className="rstudio-build" data-testid="recipe-studio-presentation" data-mode={mode}>
      <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm rstudio-back" onClick={handleBack}>
        ← {RECIPE_WORKSPACE_BACK_LABEL}
      </button>

      <header className="rstudio-build__top">
        <div>
          <p className="rstudio-kicker">SILA Recipe Studio · Step 7 of 7</p>
          <h1>{heading}</h1>
          <p className="rstudio-build__top-meta">
            {isEdit
              ? "Load, edit, picture, revise, and submit using the existing Recipe Master APIs. Approved versions stay read-only until you create a Draft revision."
              : "Live Material Catalog with picture, Save Draft and Submit. Successful create opens Recipe Detail; Edit reuses this same Studio."}
          </p>
          <div className="rstudio-build__badges">
            <span className={recipeBadgeClass(status || "DRAFT")} data-testid="recipe-studio-status">
              {status || "DRAFT"}
            </span>
            <span className="rstudio-build__badge rstudio-build__badge--mode">{isEdit ? "Edit mode" : "Create mode"}</span>
            {loadedRecipe && (
              <span className="rstudio-build__badge rstudio-build__badge--step" data-testid="recipe-studio-version">
                v{loadedRecipe.version}{versionLabel ? ` · ${versionLabel}` : ""}
              </span>
            )}
            {isEdit && (
              <span className="rstudio-build__badge rstudio-build__badge--mode" data-testid="recipe-studio-edit-id">
                ID {idText || "—"}
              </span>
            )}
            {dirty && canEdit && (
              <span className="rstudio-build__badge rstudio-build__badge--mode" data-testid="recipe-studio-dirty">
                Unsaved changes
              </span>
            )}
          </div>
          {editAccess.kind !== "editable" && isEdit && (
            <p className="rstudio-build__top-meta" data-testid="recipe-studio-access-note" role="status">
              {editAccess.reason}
            </p>
          )}
        </div>
      </header>

      {helperNotice && (
        <div className="sila-alert sila-alert--warning" role="status">{helperNotice}</div>
      )}

      <div className="rstudio-build__panels">
        {/* LEFT — My Recipe */}
        <section className="rstudio-panel rstudio-panel--recipe" aria-label="My Recipe">
          <div>
            <h2 className="rstudio-panel__title">My Recipe</h2>
            <p className="rstudio-panel__hint">
              Ingredient-first canvas. Add from the catalog, edit quantity/UOM, and preview draft costs locally.
            </p>
          </div>

          <div className="rstudio-section rstudio-section--sky">
            <p className="rstudio-section__label">Recipe header</p>
            <div className="rstudio-header-grid">
              <div className="rstudio-fields">
                <div className="rstudio-field rstudio-field--wide">
                  <label htmlFor="rstudio-name">Recipe name</label>
                  <input
                    id="rstudio-name"
                    value={name}
                    placeholder="e.g. House citrus spritz"
                    disabled={!canEdit}
                    onChange={(e) => {
                      setHeader(setName, e.target.value);
                      setFieldErrors((current) => ({ ...current, name: undefined }));
                    }}
                    maxLength={200}
                    aria-invalid={Boolean(fieldErrors.name)}
                  />
                  {fieldErrors.name && <span className="rstudio-field-error">{fieldErrors.name}</span>}
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-code">Recipe code</label>
                  <input
                    id="rstudio-code"
                    value={recipeCode || (isEdit ? "—" : "Assigned on save")}
                    disabled
                    readOnly
                    data-testid="recipe-studio-code"
                  />
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-family">Family</label>
                  <select
                    id="rstudio-family"
                    value={familyId}
                    disabled={!canEdit}
                    onChange={(e) => {
                      setHeader(setFamilyId, e.target.value);
                      setClassConfirmed(true);
                    }}
                  >
                    <option value="">Select family</option>
                    {families.map((item) => (
                      <option key={item.id} value={item.id}>{item.name}</option>
                    ))}
                  </select>
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-category">Category</label>
                  <select
                    id="rstudio-category"
                    value={categoryId}
                    disabled={!canEdit}
                    onChange={(e) => {
                      setHeader(setCategoryId, e.target.value);
                      setClassConfirmed(true);
                    }}
                  >
                    <option value="">Select category</option>
                    {categories.map((item) => (
                      <option key={item.id} value={item.id}>{item.name}</option>
                    ))}
                  </select>
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-cuisine">Cuisine</label>
                  <input
                    id="rstudio-cuisine"
                    value={cuisine}
                    placeholder="Optional"
                    disabled={!canEdit}
                    onChange={(e) => setHeader(setCuisine, e.target.value)}
                  />
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-meal">Meal period</label>
                  <input
                    id="rstudio-meal"
                    value={mealPeriod}
                    placeholder="Optional"
                    disabled={!canEdit}
                    onChange={(e) => setHeader(setMealPeriod, e.target.value)}
                  />
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-yield">Yield / portions</label>
                  <input
                    id="rstudio-yield"
                    type="number"
                    min="0"
                    step="any"
                    value={yieldQty}
                    disabled={!canEdit}
                    onChange={(e) => {
                      setHeader(setYieldQty, e.target.value);
                      setFieldErrors((current) => ({ ...current, serving: undefined }));
                    }}
                    aria-invalid={Boolean(fieldErrors.serving)}
                  />
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-serving-uom">Serving UOM</label>
                  <input
                    id="rstudio-serving-uom"
                    value={servingUom}
                    disabled={!canEdit}
                    onChange={(e) => {
                      setHeader(setServingUom, e.target.value.toUpperCase());
                      setFieldErrors((current) => ({ ...current, serving: undefined }));
                    }}
                    maxLength={10}
                    aria-invalid={Boolean(fieldErrors.serving)}
                  />
                  {fieldErrors.serving && <span className="rstudio-field-error">{fieldErrors.serving}</span>}
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-prep">Preparation time</label>
                  <input
                    id="rstudio-prep"
                    value={prepTime}
                    placeholder="e.g. 12 min (local note)"
                    disabled={!canEdit}
                    onChange={(e) => setHeader(setPrepTime, e.target.value)}
                  />
                </div>
              </div>
              <SilaRecipePicture
                key={`studio-pic-${persistedEditId || "new"}-${pictureEpoch}`}
                recipeId={persistedEditId || createdIdRef.current}
                preferDraft
                hasPicture={hasPicture}
                localPreviewUrl={localPreviewUrl}
                placeholderTitle={name || "Recipe"}
                placeholderCategory={categoryName}
                placeholderFamily={familyName}
                locked={!canEdit}
                variant="studio"
                busy={pictureBusy || busy}
                error={pictureError}
                onChooseFile={handleChoosePicture}
                onRemove={handleRemovePicture}
              />
            </div>
            {canEdit && classification && !classConfirmed && (
              <div className="rstudio-suggest" role="status">
                <span>
                  Suggested classification: {classification.familyName} / {classification.categoryName}
                  {classification.mealPeriod ? ` · ${classification.mealPeriod}` : ""} — {classification.reason}
                </span>
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={applyClassificationSuggestion}>
                  Apply suggestion
                </button>
              </div>
            )}
            {(familyName || categoryName) && classConfirmed && (
              <p className="rstudio-panel__hint">Classification confirmed: {[familyName, categoryName].filter(Boolean).join(" / ")}</p>
            )}
          </div>

          <div className="rstudio-section rstudio-section--canvas">
            <p className="rstudio-section__label">Ingredient canvas</p>
            {fieldErrors.ingredients && (
              <div className="sila-alert sila-alert--warning" role="alert">{fieldErrors.ingredients}</div>
            )}
            <SilaRecipeCanvas
              lines={lines}
              costs={costs}
              mode="RECIPE"
              currency={currency}
              locked={!canEdit}
              onReorder={commitLines}
              onChange={changeLine}
              onRemove={(key) => commitLines((current) => current.filter((line) => line.key !== key))}
              onDuplicate={(key) =>
                commitLines((current) => {
                  const index = current.findIndex((line) => line.key === key);
                  if (index < 0) return current;
                  const copy = duplicateLine(current[index]);
                  const next = [...current];
                  next.splice(index + 1, 0, copy);
                  return next;
                })
              }
              onReplace={(key) => { if (canEdit) setReplaceKey(key); }}
              onAddMaterial={addMaterial}
              onReplaceMaterial={(key, material) => {
                rememberMaterial(material.id);
                setReplaceKey(null);
                commitLines((current) =>
                  current.map((line) =>
                    line.key === key ? replaceMaterialOnLine(line, material, line.quantity, material.baseUom) : line,
                  ),
                );
              }}
              onMove={moveLine}
            />
          </div>

          <div className="rstudio-section rstudio-section--amber">
            <p className="rstudio-section__label">Cost summary · draft preview</p>
            <div className="rstudio-cost-grid">
              <div className={`rstudio-cost-tile${totalCost == null ? " rstudio-cost-tile--pending" : ""}`}>
                <span>Total recipe cost</span>
                <strong>{costLabel(totalCost)}</strong>
              </div>
              <div className={`rstudio-cost-tile${portionCost == null ? " rstudio-cost-tile--pending" : ""}`}>
                <span>Cost per portion</span>
                <strong>{costLabel(portionCost)}</strong>
              </div>
              <div className="rstudio-cost-tile">
                <span>Target food cost %</span>
                <strong>
                  <input
                    aria-label="Target food cost percent"
                    value={targetFoodCost}
                    disabled={!canEdit}
                    onChange={(e) => {
                      setHeader(setTargetFoodCost, e.target.value);
                      const value = Number(e.target.value);
                      if (Number.isFinite(value) && value > 0 && value < 100) saveTargetFoodCostPercent(value);
                    }}
                    style={{ width: "4rem", border: "none", background: "transparent", fontWeight: 700 }}
                  />
                  %
                </strong>
              </div>
              <div className={`rstudio-cost-tile${pricing.suggestedSellingPrice == null ? " rstudio-cost-tile--pending" : ""}`}>
                <span>Suggested selling price</span>
                <strong>{costLabel(pricing.suggestedSellingPrice)}</strong>
              </div>
            </div>
            <p className="rstudio-panel__hint">
              Missing or unapproved material costs show COST PENDING — never treated as zero. Draft only; Menu Item prices are not updated.
            </p>
          </div>

          <div className="rstudio-section rstudio-section--lavender">
            <p className="rstudio-section__label">Menu and outlet</p>
            <div className="rstudio-fields">
              <div className="rstudio-field rstudio-field--wide">
                <label htmlFor="rstudio-stock-outlet">Outlet</label>
                <select
                  id="rstudio-stock-outlet"
                  value={outletLocationId}
                  onChange={(e) => {
                    const nextId = e.target.value;
                    setOutletLocationId(nextId);
                    // Pull from full catalog when selecting an outlet not yet in the short list (e.g. FL10).
                    if (nextId && !outlets.some((outlet) => outlet.id === nextId)) {
                      const found = outletCatalogRef.current.find((outlet) => outlet.id === nextId);
                      if (found) {
                        setOutlets((current) => [...current, found].sort((a, b) => a.locationName.localeCompare(b.locationName)));
                      }
                    }
                  }}
                  data-testid="recipe-studio-outlet"
                >
                  <option value="">Select outlet for stock and menu price</option>
                  {(outletCatalog.length > 0 ? outletCatalog : outlets).map((outlet) => (
                      <option key={outlet.id} value={outlet.id}>
                        {outlet.locationCode ? `${outlet.locationCode} · ` : ""}
                        {outlet.locationName}
                        {outlet.plantCode || outlet.storageLocationCode
                          ? ` · ${[outlet.plantCode, outlet.storageLocationCode].filter(Boolean).join("/")}`
                          : ""}
                      </option>
                    ))}
                </select>
              </div>
              <div className="rstudio-field">
                <label htmlFor="rstudio-menu-price">Menu price for selected outlet</label>
                <input
                  id="rstudio-menu-price"
                  inputMode="decimal"
                  value={outletLocationId ? (outletPrices[outletLocationId] ?? "") : ""}
                  placeholder={
                    pricing.suggestedSellingPrice != null
                      ? `e.g. ${pricing.suggestedSellingPrice.toFixed(2)}`
                      : "Enter selling price"
                  }
                  disabled={!canEdit || !outletLocationId}
                  data-testid="recipe-studio-outlet-menu-price"
                  onChange={(e) => {
                    if (!outletLocationId) return;
                    const value = e.target.value;
                    markDirty();
                    setOutletPrices((current) => {
                      if (value.trim() === "") {
                        const next = { ...current };
                        delete next[outletLocationId];
                        return next;
                      }
                      return { ...current, [outletLocationId]: value };
                    });
                  }}
                />
              </div>
              <div className="rstudio-field">
                <label htmlFor="rstudio-menu">Linked menu item</label>
                <input
                  id="rstudio-menu"
                  value={menuItem}
                  placeholder="Optional note / menu item name"
                  disabled={!canEdit}
                  onChange={(e) => setHeader(setMenuItem, e.target.value)}
                />
              </div>
              {Object.keys(outletPrices).length > 0 && (
                <div className="rstudio-field rstudio-field--wide" data-testid="recipe-studio-outlet-prices">
                  <label>Assigned outlet menu prices</label>
                  <ul className="rstudio-outlet-price-list">
                    {Object.entries(outletPrices).map(([outletId, price]) => {
                      const outlet = outlets.find((item) => item.id === outletId)
                        || outletCatalogRef.current.find((item) => item.id === outletId);
                      const label = outlet
                        ? `${outlet.locationCode ? `${outlet.locationCode} · ` : ""}${outlet.locationName}`
                        : outletId;
                      return (
                        <li key={outletId}>
                          <button
                            type="button"
                            className="rstudio-outlet-price-list__pick"
                            onClick={() => setOutletLocationId(outletId)}
                          >
                            {label}: {price}{currency ? ` ${currency}` : ""}
                          </button>
                          {canEdit && (
                            <button
                              type="button"
                              className="sila-btn sila-btn--ghost sila-btn--sm"
                              aria-label={`Remove menu price for ${label}`}
                              onClick={() => {
                                markDirty();
                                setOutletPrices((current) => {
                                  const next = { ...current };
                                  delete next[outletId];
                                  return next;
                                });
                              }}
                            >
                              Remove
                            </button>
                          )}
                        </li>
                      );
                    })}
                  </ul>
                </div>
              )}
            </div>
            <p className="rstudio-panel__hint">
              {selectedOutlet
                ? `Stock/costing uses ${selectedOutlet.locationCode || ""} ${selectedOutlet.locationName}. Enter a menu price for this outlet, then Save Draft to persist it on the recipe.`
                : "Select an outlet (e.g. FL10), enter its menu item price, then Save Draft."}
            </p>
          </div>
        </section>

        {/* RIGHT — Material Catalog */}
        <aside className="rstudio-panel rstudio-panel--catalog" aria-label="Material Catalog">
          <div>
            <h2 className="rstudio-panel__title">Material Catalog</h2>
            <p className="rstudio-panel__hint">
              Live ItemBuyerMaster search. Drag or tap Add. Stock reflects the selected outlet when available.
            </p>
          </div>
          <div className="rstudio-section rstudio-section--mint rstudio-section--catalog-live">
            <SilaRecipeMaterialLibrary
              addedIds={addedIds}
              onAdd={addMaterial}
              onAddMany={addMaterials}
              replaceTargetKey={canEdit ? replaceKey : null}
              onReplacePick={(material) => {
                if (!replaceKey || !canEdit) return;
                rememberMaterial(material.id);
                commitLines((current) =>
                  current.map((line) =>
                    line.key === replaceKey
                      ? replaceMaterialOnLine(line, material, line.quantity, material.baseUom)
                      : line,
                  ),
                );
                setReplaceKey(null);
              }}
              recommended={materialRecs.items}
              recommendReason={materialRecs.reason}
              outletLocationId={outletLocationId || null}
              locked={!canEdit}
            />
          </div>
        </aside>
      </div>

      {(saveError || verifyWarning) && (
        <div className={`sila-alert ${saveError ? "sila-alert--danger" : "sila-alert--warning"}`} role="alert">
          {saveError || verifyWarning}
        </div>
      )}

      <div className="rstudio-actions" role="toolbar" aria-label="Recipe Studio actions">
        <button type="button" className="sila-btn sila-btn--secondary" onClick={handleBack} disabled={busy}>
          {RECIPE_WORKSPACE_BACK_LABEL}
        </button>
        {showDiscardChanges && (
          <button
            type="button"
            className="sila-btn sila-btn--secondary"
            onClick={handleBack}
            disabled={busy}
            data-testid="recipe-studio-discard-changes"
          >
            {DISCARD_CHANGES_LABEL}
          </button>
        )}
        {editAccess.kind === "revision-required" && isEdit && (
          <button
            type="button"
            className="sila-btn sila-btn--secondary"
            onClick={() => { void handleCreateRevision(); }}
            disabled={busy || !loadedRecipe}
            data-testid="recipe-studio-create-revision"
          >
            {revising ? "Creating revision…" : "Create Draft revision"}
          </button>
        )}
        <button
          type="button"
          className="sila-btn sila-btn--secondary"
          onClick={() => { void handleSaveDraft(); }}
          disabled={busy || !canEdit}
          data-testid="recipe-studio-save-draft"
          title={!canEdit ? (editAccess.kind === "editable" ? undefined : editAccess.reason) : undefined}
        >
          {saving ? "Saving Draft…" : "Save Draft"}
        </button>
        <button
          type="button"
          className="sila-btn sila-btn--primary"
          onClick={() => { void handleSubmit(); }}
          disabled={busy || !canEdit}
          data-testid="recipe-studio-submit"
          title={!canEdit ? (editAccess.kind === "editable" ? undefined : editAccess.reason) : undefined}
        >
          {submitting ? "Submitting…" : "Submit for approval"}
        </button>
        {showDeleteDraft && (
          <button
            type="button"
            className="sila-btn sila-btn--danger"
            onClick={() => setConfirmDeleteDraft(true)}
            disabled={busy}
            data-testid="recipe-studio-delete-draft"
          >
            Delete Draft
          </button>
        )}
        <p className="rstudio-actions__note">
          {isEdit
            ? canEdit
              ? "Save Draft uses PUT updateRecipe on this recipe id. Submit posts to the existing approval workflow. Opening an approved recipe does not create a revision until you click Create Draft revision."
              : editAccess.kind === "revision-required"
                ? "Create an explicit Draft revision before editing. The approved selling version stays unchanged until the revision is approved."
                : editAccess.reason
            : "Save Draft posts createRecipe (or updateRecipe after first save). Submit validates, saves, then calls submitRecipe. No auto-approve. Use Discard Changes for an unsaved new recipe."}
        </p>
      </div>

      <Modal
        isOpen={confirmDeleteDraft}
        onClose={() => { if (!deletingDraft) setConfirmDeleteDraft(false); }}
        variant="danger"
        headerProps={{ heading: DELETE_DRAFT_TITLE }}
        footerProps={{
          secondaryButton: {
            text: "Cancel",
            onClick: () => setConfirmDeleteDraft(false),
            disabled: deletingDraft,
          },
          primaryButton: {
            text: DELETE_DRAFT_CONFIRM_LABEL,
            loading: deletingDraft,
            disabled: deletingDraft,
            onClick: () => { void handleDeleteDraft(); },
          },
        }}
      >
        <p className="sila-modal-text">{DELETE_DRAFT_BODY}</p>
        <p className="sila-modal-text">
          <strong>{loadedRecipe?.name || name || "—"}</strong>
          {" · "}
          {loadedRecipe?.recipeCode || recipeCode || "—"}
        </p>
      </Modal>
    </div>
  );
};

export default SilaRecipeStudioPresentation;
