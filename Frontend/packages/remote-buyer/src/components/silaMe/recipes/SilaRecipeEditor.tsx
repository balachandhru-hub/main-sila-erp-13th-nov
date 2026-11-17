import React, { useEffect, useMemo, useRef, useState } from "react";
import { EmptyState, Loader, Modal, toastService } from "@vosox/shared-ui";
import { getLocations, getMyLocations, type SilaLocation, type SilaMaterial } from "../../../api/silaMe/silaInventoryApi";
import {
  createRecipe,
  deactivateRecipe,
  getRecipeUoms,
  getRecipes,
  submitRecipe,
  updateRecipe,
  type SilaRecipeDetail,
  type SilaRecipeListItem,
  type SilaRecipeWrite,
} from "../../../api/silaMe/silaRecipeApi";
import {
  getRecipeMasters,
  searchIngredients,
  type SilaIngredientOption,
  type SilaRecipeMaster,
} from "../../../api/silaMe/silaRecipeToolsApi";
import { getMenuEngineering, saveMenuEngineering, type MenuItemDetail } from "../../../api/silaMe/silaMenuEngineeringApi";
import { getItemMappings, getPosSources } from "../../../api/silaMe/silaPosMasterApi";
import SilaMaterialPricePanel from "../materials/SilaMaterialPricePanel";
import {
  editorIssues,
  headerFrom,
  lineCost,
  linesFrom,
  nextKey,
  toNumber,
  totalCostAt,
  type HeaderValues,
  type IngredientLine,
  type IngredientMaterial,
} from "./recipeEditorModel";
import {
  duplicateLine,
  materialLine,
  pushUndo,
  rememberMaterial,
  replaceMaterialOnLine,
  snapshotOf,
  snapshotsEqual,
  type BuilderSnapshot,
} from "./recipeBuilderState";
import { formatCost, formatQty, readinessBadgeClass, readinessLabel, recipeBadgeClass, recipeLabel, versionsSummary } from "./recipeFormat";
import SilaRecipeCanvas from "./SilaRecipeCanvas";
import SilaRecipeMaterialLibrary from "./SilaRecipeMaterialLibrary";
import SilaRecipeReadinessPanel from "./SilaRecipeReadinessPanel";
import SilaRecipeReplacePanel from "./SilaRecipeReplacePanel";
import SilaRecipeSmartSummary, { type PosIdOption } from "./SilaRecipeSmartSummary";
import {
  loadTargetFoodCostPercent,
  recommendMaterials,
  recommendMenuItems,
  recommendOutlets,
  saveTargetFoodCostPercent,
  suggestClassification,
  suggestRecipeNames,
  suggestSellingPrice,
  type ClassificationSuggestion,
} from "./recipeStudioSuggestions";
import { resolveStudioKind, StudioCover } from "./recipeStudioVisuals";
import "./SilaRecipes.css";
import "./SilaRecipeStudio.css";

interface SilaRecipeEditorProps {
  /** The recipe (latest version) to edit; a new recipe when absent. */
  recipe?: SilaRecipeDetail | null;
  /** Shows "Update price" on material lines (MANAGE_SILA_MASTER_DATA). */
  canRequestPrice?: boolean;
  onCancel: () => void;
  onSaved: (recipeId: string) => void;
  /** Called after soft-delete (deactivate) so the catalog can refresh. */
  onDeleted?: () => void;
}

type Saving = "" | "draft" | "submit";

const asPanelMaterial = (material: IngredientMaterial): SilaMaterial => ({
  id: material.id,
  materialCode: material.materialCode,
  description: material.description,
  baseUom: material.baseUom,
  unitCost: material.unitCost,
  currency: material.currency,
  isInventoryItem: false,
  priceStatus: material.priceStatus,
  conversions: material.conversions,
});

/** Ingredient-first Recipe Studio: My Recipe (left) + Recommended Material Catalog (right). */
const SilaRecipeEditor: React.FC<SilaRecipeEditorProps> = ({ recipe, canRequestPrice = false, onCancel, onSaved, onDeleted }) => {
  const [header, setHeader] = useState<HeaderValues>(() => headerFrom(recipe));
  const [lines, setLines] = useState<IngredientLine[]>([]);
  const [prices, setPrices] = useState<Record<string, string>>(() =>
    Object.fromEntries((recipe?.outletPrices ?? []).map((price) => [price.outletLocationId, String(price.menuPrice)])),
  );
  const [draftMenuPrice, setDraftMenuPrice] = useState("");
  const [outlets, setOutlets] = useState<SilaLocation[]>([]);
  const [subRecipes, setSubRecipes] = useState<SilaRecipeListItem[]>([]);
  const [approvedRecipes, setApprovedRecipes] = useState<SilaRecipeListItem[]>([]);
  const [families, setFamilies] = useState<SilaRecipeMaster[]>([]);
  const [categories, setCategories] = useState<SilaRecipeMaster[]>([]);
  const [uoms, setUoms] = useState<string[]>([]);
  const [catalogPool, setCatalogPool] = useState<SilaIngredientOption[]>([]);
  const [subRecipeId, setSubRecipeId] = useState("");
  const [priceMaterial, setPriceMaterial] = useState<IngredientMaterial | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saving, setSaving] = useState<Saving>("");
  const [menuSearch, setMenuSearch] = useState("");
  const [menuMatches, setMenuMatches] = useState<MenuItemDetail[]>([]);
  const [menuPool, setMenuPool] = useState<MenuItemDetail[]>([]);
  const [linkedMenuItem, setLinkedMenuItem] = useState<MenuItemDetail | null>(null);
  const [linkingMenu, setLinkingMenu] = useState(false);
  const [updatingMenuPrice, setUpdatingMenuPrice] = useState(false);
  const [posOptions, setPosOptions] = useState<PosIdOption[]>([]);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [undoStack, setUndoStack] = useState<BuilderSnapshot[]>([]);
  const [baseline, setBaseline] = useState<BuilderSnapshot>(() => snapshotOf([], headerFrom(recipe).servingQty));
  const [replaceKey, setReplaceKey] = useState<string | null>(null);
  const [nameConfirmed, setNameConfirmed] = useState(() => Boolean(recipe?.name?.trim()));
  const [classConfirmed, setClassConfirmed] = useState(() => Boolean(recipe?.familyId || recipe?.categoryId));
  const [targetFoodCostPercent, setTargetFoodCostPercent] = useState(() => loadTargetFoodCostPercent());
  const [reviewOpen, setReviewOpen] = useState(false);
  const [pendingSubmit, setPendingSubmit] = useState(false);
  const baselineReady = useRef(false);

  useEffect(() => {
    let active = true;
    const load = async () => {
      setLoading(true);
      setLoadError(null);
      try {
        // Keep Edit/New unblocked: optional calls must not fail the whole editor open.
        const [allLocations, myLocationsResult, approved, allRecipes, loadedFamilies, loadedCategories, loadedUoms, browse, menus] = await Promise.all([
          getLocations().catch(() => [] as SilaLocation[]),
          getMyLocations()
            .then((items) => ({ ok: true as const, items }))
            .catch(() => ({ ok: false as const, items: [] as SilaLocation[] })),
          getRecipes({ active: true, limit: 200 }).catch(() => ({ items: [] as SilaRecipeListItem[], total: 0, index: 0, limit: 200 })),
          getRecipes({ limit: 200 }).catch(() => ({ items: [] as SilaRecipeListItem[], total: 0, index: 0, limit: 200 })),
          getRecipeMasters("families").catch(() => [] as SilaRecipeMaster[]),
          getRecipeMasters("categories").catch(() => [] as SilaRecipeMaster[]),
          getRecipeUoms().catch(() => [] as string[]),
          searchIngredients({ search: "", limit: 60 }).catch(() => ({ items: [] as SilaIngredientOption[], total: 0, index: 0, limit: 60 })),
          getMenuEngineering("items", { search: "", limit: 80 }).catch(() => null),
        ]);
        if (!active) return;
        const loaded = linesFrom(recipe, approved.items);
        const dropped = (recipe?.ingredients.length ?? 0) - loaded.length;
        if (dropped > 0) toastService.error(`${dropped} sub-recipe(s) are no longer approved and were removed from the editor.`);
        setLines(loaded);
        const snap = snapshotOf(loaded, headerFrom(recipe).servingQty);
        setBaseline(snap);
        setUndoStack([]);
        baselineReady.current = true;

        // Prefer outlets the user can access; keep already-priced recipe outlets visible.
        // If /locations/mine is unavailable, fall back to all org outlets (admin/full-access path).
        const scoped = (myLocationsResult.ok ? myLocationsResult.items : allLocations)
          .filter((location) => location.locationType === "OUTLET");
        const mine = new Map(scoped.map((location) => [location.id, location]));
        for (const price of recipe?.outletPrices ?? []) {
          if (mine.has(price.outletLocationId)) continue;
          const found = allLocations.find((location) => location.id === price.outletLocationId);
          if (found) mine.set(found.id, found);
        }
        setOutlets(Array.from(mine.values()).sort((a, b) => a.locationName.localeCompare(b.locationName)));
        setApprovedRecipes(allRecipes.items.length > 0 ? allRecipes.items : approved.items);
        setSubRecipes(approved.items.filter((x) => x.id !== recipe?.id));
        setFamilies(loadedFamilies);
        setCategories(loadedCategories);
        setUoms(loadedUoms);
        setCatalogPool(browse.items);
        setMenuPool(menus?.items?.items ?? []);
      } catch (err: unknown) {
        if (active) setLoadError(err instanceof Error ? err.message : "Could not load the recipe editor.");
      } finally {
        if (active) setLoading(false);
      }
    };
    load();
    return () => {
      active = false;
    };
  }, [recipe]);

  // POS ID picker options — never block Edit/New open on POS master APIs.
  useEffect(() => {
    let active = true;
    const loadPos = async () => {
      try {
        const [sources, recipesPage] = await Promise.all([
          getPosSources().catch(() => []),
          getRecipes({ limit: 200 }).catch(() => ({ items: [] as SilaRecipeListItem[] })),
        ]);
        const pages = await Promise.all(
          sources.slice(0, 8).map((source) => getItemMappings(source.id, "", 0, 100).catch(() => null)),
        );
        if (!active) return;

        const takenByOther = new Map<string, string>();
        for (const row of recipesPage.items) {
          if (row.id === recipe?.id || row.status === "INACTIVE") continue;
          const code = row.posCode?.trim();
          if (code) takenByOther.set(code.toUpperCase(), row.recipeCode || row.name);
        }

        const map = new Map<string, PosIdOption>();
        for (const page of pages) {
          for (const row of page?.items ?? []) {
            const code = row.posItemCode?.trim();
            if (!code) continue;
            const taken = takenByOther.has(code.toUpperCase())
              && !(recipe?.id && row.recipeId === recipe.id);
            // Offer free codes, or codes already mapped to this recipe.
            if (taken && row.recipeId !== recipe?.id) continue;
            const description = row.posItemDescription?.trim() || "";
            map.set(code, {
              code,
              description,
              label: description ? `${code} · ${description}` : code,
              taken: false,
            });
          }
        }

        // Current recipe's POS code stays selectable even if not in mappings.
        const currentCode = (recipe?.posCode ?? "").trim();
        if (currentCode && !map.has(currentCode)) {
          const description = (recipe?.posItem ?? "").trim();
          map.set(currentCode, {
            code: currentCode,
            description,
            label: description ? `${currentCode} · ${description}` : currentCode,
            taken: false,
          });
        }

        setPosOptions(Array.from(map.values()).sort((a, b) => a.code.localeCompare(b.code)));
      } catch {
        if (active) setPosOptions([]);
      }
    };
    loadPos();
    return () => {
      active = false;
    };
  }, [recipe?.id, recipe?.posCode, recipe?.posItem]);

  useEffect(() => {
    if (!recipe?.id) return;
    let active = true;
    getMenuEngineering("items", { search: recipe.name, limit: 50 })
      .then((envelope) => {
        if (!active) return;
        const found = (envelope.items?.items ?? []).find((row) => row.linkedRecipeId === recipe.id) ?? null;
        setLinkedMenuItem(found);
      })
      .catch(() => {
        if (active) setLinkedMenuItem(null);
      });
    return () => {
      active = false;
    };
  }, [recipe?.id, recipe?.name]);

  const searchMenuItems = async () => {
    try {
      const envelope = await getMenuEngineering("items", { search: menuSearch.trim(), limit: 30 });
      setMenuMatches(envelope.items?.items ?? []);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not search menu items.");
    }
  };

  const linkMenuItem = async (menuItemId?: string) => {
    if (!recipe?.id || !menuItemId) {
      toastService.error("Save the recipe first, then link a menu item.");
      return;
    }
    setLinkingMenu(true);
    try {
      await saveMenuEngineering({ action: "LINK_RECIPE", menuItemId, recipeId: recipe.id });
      const envelope = await getMenuEngineering("item", { menuItemId });
      setLinkedMenuItem(envelope.item ?? null);
      toastService.success("Menu item linked. Selling price stays on the menu item until you update it.");
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not link the menu item.");
    } finally {
      setLinkingMenu(false);
    }
  };

  const pricedOutletIds = useMemo(() => Object.keys(prices).filter((id) => prices[id].trim() !== ""), [prices]);
  const pricedOutlets = pricedOutletIds.length;
  const activeOutlet = pricedOutletIds[0] ?? "";
  const activeOutletName = outlets.find((outlet) => outlet.id === activeOutlet)?.locationName;
  const assignedIds = Object.keys(prices);
  const menuOutletId = (activeOutlet && assignedIds.includes(activeOutlet) ? activeOutlet : assignedIds[0]) ?? "";
  const menuOutletName = outlets.find((outlet) => outlet.id === menuOutletId)?.locationName;
  const menuPriceText = menuOutletId ? (prices[menuOutletId] ?? "") : draftMenuPrice;
  const costs = useMemo(() => lines.map((line) => lineCost(line, activeOutlet)), [lines, activeOutlet]);
  const issues = editorIssues(lines, header.itemMode, pricedOutletIds, header.familyId, header.categoryId);
  const costComplete = lines.length > 0 && costs.every((cost) => cost.cost != null);
  const totalCost = totalCostAt(lines, activeOutlet);
  const currency = header.currency.trim() || lines.find((line) => line.material?.currency)?.material?.currency || "";
  const dirty = baselineReady.current && !snapshotsEqual(baseline, snapshotOf(lines, header.servingQty));
  const replaceLine = replaceKey ? lines.find((line) => line.key === replaceKey) ?? null : null;

  const nameSuggestions = useMemo(() => (nameConfirmed ? [] : suggestRecipeNames(lines)), [lines, nameConfirmed]);
  const classification = useMemo(() => suggestClassification(lines, families, categories), [lines, families, categories]);
  const pricing = useMemo(
    () => suggestSellingPrice(totalCost, toNumber(header.servingQty), targetFoodCostPercent, costs),
    [totalCost, header.servingQty, targetFoodCostPercent, costs],
  );

  const addOutletById = (outletId: string) => {
    const outlet = outlets.find((row) => row.id === outletId);
    if (!outlet) return;
    setPrices((current) => {
      if (Object.prototype.hasOwnProperty.call(current, outlet.id)) return current;
      return {
        ...current,
        [outlet.id]: draftMenuPrice.trim() || (pricing.suggestedSellingPrice != null ? String(pricing.suggestedSellingPrice) : ""),
      };
    });
  };

  const updateMenuItemPrice = async () => {
    if (!linkedMenuItem?.id) {
      toastService.error("Link a Menu Item before updating its selling price.");
      return;
    }
    const amount = Number(menuPriceText);
    if (!Number.isFinite(amount) || amount < 0) {
      toastService.error("Enter a valid menu price to update the Menu Item.");
      return;
    }
    setUpdatingMenuPrice(true);
    try {
      await saveMenuEngineering({
        action: "UPSERT_ITEM",
        item: {
          ...linkedMenuItem,
          id: linkedMenuItem.id,
          sellingPrice: amount,
          currency: header.currency.trim() || linkedMenuItem.currency || undefined,
          outletLocationId: menuOutletId || linkedMenuItem.outletLocationId || undefined,
          linkedRecipeId: recipe?.id ?? linkedMenuItem.linkedRecipeId,
        },
      });
      const envelope = await getMenuEngineering("item", { menuItemId: linkedMenuItem.id });
      setLinkedMenuItem(envelope.item ?? { ...linkedMenuItem, sellingPrice: amount });
      toastService.success(`Menu Item selling price updated to ${formatCost(amount, currency)}.`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not update the Menu Item price.");
    } finally {
      setUpdatingMenuPrice(false);
    }
  };

  const handleDelete = async () => {
    if (!recipe?.id) return;
    setDeleting(true);
    try {
      await deactivateRecipe(recipe.id);
      toastService.success("Recipe deleted (deactivated).");
      setConfirmDelete(false);
      onDeleted?.();
      onCancel();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not delete the recipe.");
    } finally {
      setDeleting(false);
    }
  };
  const materialRecs = useMemo(
    () => recommendMaterials(lines, approvedRecipes, catalogPool, 24),
    [lines, approvedRecipes, catalogPool],
  );
  const familyName = families.find((item) => item.id === header.familyId)?.name;
  const categoryName = categories.find((item) => item.id === header.categoryId)?.name;
  const menuRecommendations = useMemo(
    () => recommendMenuItems(header.name, familyName, categoryName, menuPool, 8),
    [header.name, familyName, categoryName, menuPool],
  );
  const outletRecommendations = useMemo(
    () => recommendOutlets(outlets, Object.keys(prices), classification?.mealPeriod),
    [outlets, prices, classification?.mealPeriod],
  );

  const commitLines = (next: IngredientLine[] | ((current: IngredientLine[]) => IngredientLine[])) => {
    setLines((current) => {
      const resolved = typeof next === "function" ? next(current) : next;
      return resolved;
    });
    setUndoStack((stack) => pushUndo(stack, snapshotOf(lines, header.servingQty)));
  };

  const commitServingQty = (value: string) => {
    setUndoStack((stack) => pushUndo(stack, snapshotOf(lines, header.servingQty)));
    setHeader((current) => ({ ...current, servingQty: value }));
  };

  const undo = () => {
    setUndoStack((stack) => {
      if (stack.length === 0) return stack;
      const previous = stack[stack.length - 1];
      setLines(previous.lines);
      setHeader((current) => ({ ...current, servingQty: previous.servingQty }));
      return stack.slice(0, -1);
    });
  };

  const cancelChanges = () => {
    setLines(baseline.lines);
    setHeader((current) => ({ ...current, servingQty: baseline.servingQty }));
    setUndoStack([]);
  };

  const addMaterial = (option: SilaIngredientOption) => {
    rememberMaterial(option.id);
    const line = materialLine(option);
    if (header.itemMode === "DIRECT") {
      commitLines([line]);
      return;
    }
    commitLines((current) => (current.some((x) => x.material?.id === line.material?.id) ? current : [...current, line]));
  };

  const addMaterials = (options: SilaIngredientOption[]) => {
    options.forEach((option) => rememberMaterial(option.id));
    if (header.itemMode === "DIRECT") {
      if (options[0]) commitLines([materialLine(options[0])]);
      return;
    }
    commitLines((current) => {
      const next = [...current];
      options.forEach((option) => {
        if (!next.some((x) => x.material?.id === option.id)) next.push(materialLine(option));
      });
      return next;
    });
  };

  const addSubRecipe = () => {
    const sub = subRecipes.find((x) => x.id === subRecipeId);
    if (!sub || lines.some((line) => line.subRecipe?.id === sub.id)) return;
    commitLines((current) => [...current, { key: nextKey(), subRecipe: sub, quantity: "1", uom: sub.servingUom }]);
    setSubRecipeId("");
  };

  const changeHeader = (change: Partial<HeaderValues>) => {
    if (change.servingQty != null && change.servingQty !== header.servingQty) {
      setUndoStack((stack) => pushUndo(stack, snapshotOf(lines, header.servingQty)));
    }
    if (change.name != null) setNameConfirmed(true);
    if (change.familyId != null || change.categoryId != null) setClassConfirmed(true);
    setHeader((current) => ({ ...current, ...change }));
    if (change.itemMode === "DIRECT") commitLines((current) => current.filter((line) => line.material).slice(0, 1));
    if (change.itemMode && change.itemMode !== "BATCH") commitLines((current) => current.filter((line) => !line.subRecipe));
  };

  const confirmName = (name: string) => {
    setNameConfirmed(true);
    setHeader((current) => ({ ...current, name }));
  };

  const confirmClassification = (suggestion: ClassificationSuggestion) => {
    setClassConfirmed(true);
    setHeader((current) => ({
      ...current,
      familyId: suggestion.familyId || current.familyId,
      categoryId: suggestion.categoryId || current.categoryId,
    }));
  };

  const validate = (): string | null => {
    if (!header.name.trim()) return "Enter the recipe name.";
    if (!(toNumber(header.servingQty) > 0) || !header.servingUom.trim()) return "Enter a serving quantity greater than zero and its unit.";
    if (lines.length === 0) return "Add at least one ingredient.";
    if (lines.some((line) => !(toNumber(line.quantity) > 0))) return "Enter a quantity greater than zero for every ingredient.";
    if (Object.values(prices).some((value) => value.trim() !== "" && !(toNumber(value) > 0))) {
      return "Menu prices must be greater than zero (leave empty when not sold at an outlet).";
    }
    const pos = header.posCode.trim();
    if (pos) {
      const conflict = approvedRecipes.find(
        (row) => row.id !== recipe?.id && (row.posCode || "").trim().toUpperCase() === pos.toUpperCase(),
      );
      if (conflict) {
        return `POS ID "${pos}" is already used by ${conflict.recipeCode || conflict.name}. Choose a free POS ID.`;
      }
    }
    if (header.posItem.trim().length > 200) {
      return "POS item name must be at most 200 characters.";
    }
    return null;
  };

  const buildRequest = (): SilaRecipeWrite => ({
    name: header.name.trim(),
    description: header.description.trim() || null,
    familyId: header.familyId || null,
    categoryId: header.categoryId || null,
    itemMode: header.itemMode,
    servingQty: toNumber(header.servingQty),
    servingUom: header.servingUom.trim(),
    sellingUom: header.sellingUom.trim() || "EA",
    posCode: header.posCode.trim() || null,
    posItem: header.posItem.trim() || null,
    currency: header.currency.trim() || null,
    ingredients: lines.map((line) => ({
      materialId: line.material?.id ?? null,
      subRecipeId: line.subRecipe?.id ?? null,
      quantity: toNumber(line.quantity),
      uom: line.uom || null,
    })),
    outletPrices: Object.entries(prices)
      .filter(([, value]) => value.trim() !== "")
      .map(([outletLocationId, value]) => ({ outletLocationId, menuPrice: toNumber(value) })),
  });

  const handleSave = async (submit: boolean) => {
    const problem = validate();
    if (problem) {
      toastService.error(problem);
      setReviewOpen(true);
      return;
    }
    setSaving(submit ? "submit" : "draft");
    let recipeId = recipe?.id ?? null;
    try {
      if (recipe) {
        await updateRecipe(recipe.id, buildRequest());
      } else {
        recipeId = await createRecipe(buildRequest());
      }
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not save the recipe.");
      setSaving("");
      return;
    }
    const newVersion = recipe && (recipe.status === "APPROVED" || recipe.status === "REJECTED");
    try {
      if (submit && recipeId) {
        await submitRecipe(recipeId);
        toastService.success("Recipe saved and sent for approval.");
      } else {
        toastService.success(newVersion ? `Saved as draft version ${(recipe?.version ?? 0) + 1}.` : "Recipe saved as draft.");
      }
    } catch (err: unknown) {
      toastService.error(`Saved as draft, but not sent for approval: ${err instanceof Error ? err.message : "the submit failed."}`);
    } finally {
      setSaving("");
      setReviewOpen(false);
    }
    if (recipeId) onSaved(recipeId);
  };

  const openReview = (submit: boolean) => {
    setPendingSubmit(submit);
    setReviewOpen(true);
  };

  const title = recipe ? `${recipe.recipeCode} ${header.name || recipe.name}` : (header.name.trim() || "Untitled draft");
  const coverKind = resolveStudioKind(header.name, recipe?.category, recipe?.familyName, ...lines.map((line) => line.material?.description));

  if (loading || loadError) {
    return (
      <div className="sila-me srec-page rstudio-page">
        <div className="rstudio-editor-top">
          <StudioCover compact kind="main" title="Recipe Studio" />
          <div>
            <p className="rstudio-kicker">Recipe Studio</p>
            <h1 className="rstudio-hero__title" style={{ fontSize: "1.45rem" }}>{title}</h1>
          </div>
        </div>
        {loading ? <Loader size={24} message="Loading Recipe Studio..." /> : <EmptyState variant="error" title="Couldn't open the editor" description={loadError ?? undefined} />}
        <button type="button" className="sila-btn sila-btn--secondary" onClick={onCancel}>Close</button>
      </div>
    );
  }

  return (
    <div className="sila-me srec-page rstudio-page rstudio-editor-shell">
      <div className="rstudio-editor-top">
        <StudioCover compact kind={coverKind} title={title} />
        <div className="rstudio-editor-top__main">
          <p className="rstudio-kicker">{recipe ? "Change Recipe" : "New Recipe"} · ingredient-first</p>
          <input
            className="rstudio-name-input"
            value={header.name}
            placeholder="Recipe name (optional while building — required on save)"
            maxLength={200}
            aria-label="Recipe name"
            onChange={(e) => changeHeader({ name: e.target.value })}
          />
          <div className="srec-badges">
            <span className={recipeBadgeClass(recipe?.status ?? "DRAFT")}>{recipeLabel(recipe?.status ?? "DRAFT")}</span>
            <span className={readinessBadgeClass(issues.length)}>{readinessLabel(issues.length)}</span>
            {recipe && <span className="sila-badge sila-badge--neutral">v{recipe.version}</span>}
            {!recipe && <span className="sila-badge sila-badge--neutral">Local draft</span>}
            {dirty && <span className="sila-badge sila-badge--warning">Unsaved changes</span>}
          </div>
        </div>
        <div className="rstudio-yield">
          <label className="sila-label" htmlFor="rstudio-yield-qty">Yield</label>
          <div className="rstudio-yield__row">
            <input
              id="rstudio-yield-qty"
              className="sila-input"
              type="number"
              min="0"
              step="any"
              value={header.servingQty}
              onChange={(e) => commitServingQty(e.target.value)}
            />
            <input
              className="sila-input"
              list="rstudio-yield-uoms"
              value={header.servingUom}
              aria-label="Yield UOM"
              onChange={(e) => changeHeader({ servingUom: e.target.value })}
            />
            <datalist id="rstudio-yield-uoms">
              {uoms.map((unit) => <option key={unit} value={unit} />)}
            </datalist>
          </div>
          <div className="rstudio-live-cost">
            <span className="sila-help">Recipe cost</span>
            <strong>{pricing.incomplete || totalCost == null ? "COST PENDING" : formatCost(totalCost, currency)}</strong>
            <span className="sila-help">Suggest sell</span>
            <strong>
              {pricing.incomplete || pricing.suggestedSellingPrice == null
                ? "COST PENDING"
                : formatCost(pricing.suggestedSellingPrice, currency)}
            </strong>
          </div>
        </div>
      </div>

      {recipe && recipe.activeVersion > 0 && (recipe.status === "APPROVED" || recipe.status === "REJECTED") && (
        <div className="sila-alert sila-alert--warning" role="status">
          Saving creates draft version {recipe.version + 1}. POS sales keep using version {recipe.activeVersion} until it is approved.
        </div>
      )}

      {recipe && recipe.versions.length > 0 && <span className="sila-help">Versions: {versionsSummary(recipe.versions)}</span>}

      <SilaRecipeReadinessPanel readiness={{ ready: issues.length === 0, costComplete, issues }} />

      {replaceLine ? (
        <SilaRecipeReplacePanel
          line={replaceLine}
          outletId={activeOutlet}
          currency={currency}
          onCancel={() => setReplaceKey(null)}
          onConfirm={(option, quantity, uom) => {
            rememberMaterial(option.id);
            commitLines((current) => current.map((line) => (line.key === replaceLine.key ? replaceMaterialOnLine(line, option, quantity, uom) : line)));
            setReplaceKey(null);
          }}
        />
      ) : (
        <div className="srec-builder srec-builder--studio">
          <div className="rstudio-my-recipe" aria-label="My Recipe">
            <SilaRecipeCanvas
              lines={lines}
              costs={costs}
              mode={header.itemMode}
              currency={currency}
              onReorder={(next) => commitLines(next)}
              onChange={(key, change) => commitLines((current) => current.map((line) => (line.key === key ? { ...line, ...change } : line)))}
              onRemove={(key) => commitLines((current) => current.filter((line) => line.key !== key))}
              onDuplicate={(key) => commitLines((current) => {
                const index = current.findIndex((line) => line.key === key);
                if (index < 0) return current;
                const copy = duplicateLine(current[index]);
                const next = [...current];
                next.splice(index + 1, 0, copy);
                return next;
              })}
              onReplace={setReplaceKey}
              onAddMaterial={addMaterial}
              onReplaceMaterial={(key, material) => {
                rememberMaterial(material.id);
                commitLines((current) => current.map((line) => (
                  line.key === key ? replaceMaterialOnLine(line, material, line.quantity, material.baseUom) : line
                )));
              }}
              onUpdatePrice={canRequestPrice ? setPriceMaterial : undefined}
            />

            {header.itemMode === "BATCH" && (
              <section className="sila-card">
                <div className="sila-card-body">
                  <div className="srec-filters srec-filters--flush">
                    <div className="sila-field srec-grow">
                      <label className="sila-label" htmlFor="srec-sub-recipe">Add an approved sub-recipe</label>
                      <select id="srec-sub-recipe" className="sila-select" value={subRecipeId} onChange={(e) => setSubRecipeId(e.target.value)}>
                        <option value="">Select a recipe</option>
                        {subRecipes.map((sub) => (
                          <option key={sub.id} value={sub.id}>{sub.recipeCode} {sub.name} ({formatQty(sub.servingQty)} {sub.servingUom})</option>
                        ))}
                      </select>
                    </div>
                    <button type="button" className="sila-btn sila-btn--secondary" onClick={addSubRecipe} disabled={!subRecipeId}>Add sub-recipe</button>
                  </div>
                </div>
              </section>
            )}

            <SilaRecipeSmartSummary
              header={header}
              families={families}
              categories={categories}
              uoms={uoms}
              recipeCode={recipe?.recipeCode}
              nameConfirmed={nameConfirmed}
              classConfirmed={classConfirmed}
              nameSuggestions={nameSuggestions}
              classification={classification}
              pricing={pricing}
              currency={currency}
              totalCost={totalCost}
              menuPrice={menuPriceText}
              menuOutlet={menuOutletName}
              linkedMenuItem={linkedMenuItem}
              menuMatches={menuMatches}
              menuSearch={menuSearch}
              menuRecommendations={menuRecommendations}
              canLinkMenu={Boolean(recipe?.id)}
              linkingMenu={linkingMenu}
              updatingMenuPrice={updatingMenuPrice}
              posOptions={posOptions}
              outlets={outlets}
              outletRecommendations={outletRecommendations}
              prices={prices}
              onHeaderChange={changeHeader}
              onConfirmName={confirmName}
              onConfirmClassification={confirmClassification}
              onTargetFoodCost={(percent) => {
                const next = Number.isFinite(percent) && percent > 0 && percent < 100 ? percent : 30;
                setTargetFoodCostPercent(next);
                saveTargetFoodCostPercent(next);
              }}
              onMenuSearch={setMenuSearch}
              onSearchMenuItems={searchMenuItems}
              onLinkMenuItem={linkMenuItem}
              onUpdateMenuItemPrice={updateMenuItemPrice}
              onMenuPrice={(value) => {
                if (menuOutletId) setPrices((current) => ({ ...current, [menuOutletId]: value }));
                else setDraftMenuPrice(value);
              }}
              onOutletPrice={(id, value) => setPrices((current) => ({ ...current, [id]: value }))}
              onRemoveOutlet={(id) => setPrices((current) => {
                const next = { ...current };
                delete next[id];
                return next;
              })}
              onAddOutlet={addOutletById}
              onToggleOutlet={(outlet) => setPrices((current) => {
                if (Object.prototype.hasOwnProperty.call(current, outlet.id)) {
                  const next = { ...current };
                  delete next[outlet.id];
                  return next;
                }
                return {
                  ...current,
                  [outlet.id]: draftMenuPrice.trim() || (pricing.suggestedSellingPrice != null ? String(pricing.suggestedSellingPrice) : ""),
                };
              })}
            />

            <div className="rstudio-cost-strip" aria-label="Live recipe cost">
              <span>
                <em>Cost basis</em>
                <strong>{activeOutletName ?? "Default prices"}</strong>
              </span>
              <span>
                <em>Total</em>
                <strong>{pricing.incomplete || totalCost == null ? "COST PENDING" : formatCost(totalCost, currency)}</strong>
              </span>
              <span>
                <em>/ portion</em>
                <strong>
                  {pricing.incomplete || pricing.costPerPortion == null ? "COST PENDING" : formatCost(pricing.costPerPortion, currency)}
                </strong>
              </span>
              <span>
                <em>Suggested sell @ {targetFoodCostPercent}%</em>
                <strong>
                  {pricing.incomplete || pricing.suggestedSellingPrice == null
                    ? "COST PENDING"
                    : formatCost(pricing.suggestedSellingPrice, currency)}
                </strong>
              </span>
              <span>
                <em>Outlets priced</em>
                <strong>{pricedOutlets}</strong>
              </span>
            </div>
          </div>

          <SilaRecipeMaterialLibrary
            addedIds={lines.filter((x) => x.material).map((x) => x.material!.id)}
            onAdd={addMaterial}
            onAddMany={addMaterials}
            recommended={materialRecs.items}
            recommendReason={materialRecs.reason}
          />
        </div>
      )}

      <span className="sila-help">
        Draft edits stay local until Save draft / Submit. Approved recipes create a new draft version on save.
        Ingredient order is persisted as Sequence on save. Preparation notes and ingredient groups are not in the recipe data model.
      </span>

      <div className="rstudio-sticky-bar" role="toolbar" aria-label="Recipe Studio actions">
        <button type="button" className="sila-btn sila-btn--ghost" onClick={undo} disabled={saving !== "" || deleting || undoStack.length === 0}>Undo</button>
        <button type="button" className="sila-btn sila-btn--ghost" onClick={cancelChanges} disabled={saving !== "" || deleting || !dirty}>Cancel changes</button>
        <button type="button" className="sila-btn sila-btn--secondary" onClick={onCancel} disabled={saving !== "" || deleting}>Close</button>
        {recipe?.id && recipe.status !== "INACTIVE" && (
          <button
            type="button"
            className="sila-btn sila-btn--danger"
            onClick={() => setConfirmDelete(true)}
            disabled={saving !== "" || deleting}
          >
            Delete recipe
          </button>
        )}
        <button type="button" className="sila-btn sila-btn--secondary" onClick={() => openReview(false)} disabled={saving !== "" || deleting}>
          {saving === "draft" ? "Saving..." : "Save draft"}
        </button>
        <button
          type="button"
          className="sila-btn sila-btn--primary"
          onClick={() => openReview(true)}
          disabled={saving !== "" || deleting}
          title={issues.length > 0 ? "Review readiness before submit." : undefined}
        >
          {saving === "submit" ? "Submitting..." : recipe?.activeVersion ? "Submit POS / recipe change" : "Submit for approval"}
        </button>
      </div>

      <Modal
        isOpen={confirmDelete}
        onClose={() => setConfirmDelete(false)}
        variant="danger"
        headerProps={{ heading: `Delete ${recipe?.recipeCode || "recipe"}` }}
        footerProps={{
          secondaryButton: { text: "Cancel", onClick: () => setConfirmDelete(false), disabled: deleting },
          primaryButton: {
            text: "Delete",
            loading: deleting,
            onClick: handleDelete,
          },
        }}
      >
        <p className="sila-modal-text">
          This deactivates the recipe. POS sales with POS ID {header.posCode.trim() || "—"} will no longer match it.
        </p>
      </Modal>

      {reviewOpen && (
        <div className="rstudio-review" role="dialog" aria-modal="true" aria-label="Save review">
          <div className="rstudio-review__panel">
            <h2>Review before {pendingSubmit ? "submit" : "save"}</h2>
            <ul className="srec-stack">
              <li><strong>Name:</strong> {header.name.trim() || "Missing"}</li>
              <li><strong>Family / Category:</strong> {familyName || "—"} / {categoryName || "—"}</li>
              <li><strong>Yield:</strong> {header.servingQty} {header.servingUom}</li>
              <li><strong>Ingredients:</strong> {lines.length}</li>
              <li><strong>Recipe cost:</strong> {pricing.incomplete || totalCost == null ? "COST PENDING" : formatCost(totalCost, currency)}</li>
              <li><strong>Suggested sell:</strong> {pricing.incomplete || pricing.suggestedSellingPrice == null ? "COST PENDING" : formatCost(pricing.suggestedSellingPrice, currency)}</li>
              <li><strong>Outlets with price:</strong> {pricedOutlets}</li>
              <li><strong>Menu Item:</strong> {linkedMenuItem?.name ?? "None linked"}</li>
            </ul>
            {issues.length > 0 && (
              <div className="sila-alert sila-alert--warning" role="status">
                Readiness: {issues.join("; ")}
              </div>
            )}
            <p className="sila-help">Mandatory fields are validated by existing save rules. Incomplete recipes are not persisted until validation passes.</p>
            <div className="rstudio-review__actions">
              <button type="button" className="sila-btn sila-btn--ghost" onClick={() => setReviewOpen(false)} disabled={saving !== ""}>Back</button>
              <button type="button" className="sila-btn sila-btn--primary" onClick={() => handleSave(pendingSubmit)} disabled={saving !== ""}>
                {pendingSubmit ? "Confirm submit" : "Confirm save draft"}
              </button>
            </div>
          </div>
        </div>
      )}

      {priceMaterial && (
        <SilaMaterialPricePanel
          material={asPanelMaterial(priceMaterial)}
          canRequest={canRequestPrice}
          outlets={outlets}
          outletPrices={priceMaterial.outletPrices}
          defaultOutletId={activeOutlet}
          onClose={() => setPriceMaterial(null)}
          onSubmitted={() => {
            const id = priceMaterial.id;
            setLines((current) =>
              current.map((line) => (line.material?.id === id ? { ...line, material: { ...line.material, priceStatus: "PENDING_APPROVAL" } } : line)),
            );
            setPriceMaterial(null);
          }}
        />
      )}
    </div>
  );
};

export default SilaRecipeEditor;
