import React, { useEffect, useMemo, useState } from "react";
import { Loader } from "@vosox/shared-ui";
import {
  getIngredientFacets,
  searchIngredients,
  type SilaIngredientFacets,
  type SilaIngredientOption,
} from "../../../api/silaMe/silaRecipeToolsApi";
import { conversionSummary, priceStatusBadgeClass, priceStatusLabel, unitPriceText } from "./recipeFormat";
import { loadRecentMaterials, rememberMaterial } from "./recipeBuilderState";
import { resolveStudioKind, StudioCover } from "./recipeStudioVisuals";

type CatalogTab = "recommended" | "all" | "recent";

interface SilaRecipeMaterialLibraryProps {
  addedIds: string[];
  onAdd: (material: SilaIngredientOption) => void;
  onAddMany: (materials: SilaIngredientOption[]) => void;
  /** When set, Add replaces the target line instead of appending. */
  replaceTargetKey?: string | null;
  onReplacePick?: (material: SilaIngredientOption) => void;
  /** Rule-based / recent recommendations from the editor. */
  recommended?: SilaIngredientOption[];
  recommendReason?: string;
  /** Authorized outlet/store location for location-specific ledger stock (optional). */
  outletLocationId?: string | null;
  /** When true, add/replace/drag into the recipe is disabled. */
  locked?: boolean;
}

const EMPTY_FACETS: SilaIngredientFacets = { materialGroups: [], categories: [], suppliers: [], materialTypes: [], supplierOptions: [] };
const RESULT_LIMIT = 30;

/** Right-panel Recommended Material Catalog: grocery-style cards with Recommended / All / Recent. */
const SilaRecipeMaterialLibrary: React.FC<SilaRecipeMaterialLibraryProps> = ({
  addedIds,
  onAdd,
  onAddMany,
  replaceTargetKey,
  onReplacePick,
  recommended = [],
  recommendReason = "Browse Material Master to begin.",
  outletLocationId = null,
  locked = false,
}) => {
  const [tab, setTab] = useState<CatalogTab>("recommended");
  const [facets, setFacets] = useState<SilaIngredientFacets>(EMPTY_FACETS);
  const [search, setSearch] = useState("");
  const [materialGroup, setMaterialGroup] = useState("");
  const [category, setCategory] = useState("");
  const [materialType, setMaterialType] = useState("");
  const [results, setResults] = useState<SilaIngredientOption[]>([]);
  const [total, setTotal] = useState(0);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [selected, setSelected] = useState<Record<string, boolean>>({});
  const [recentIds] = useState(() => loadRecentMaterials());
  const [recentItems, setRecentItems] = useState<SilaIngredientOption[]>([]);

  useEffect(() => {
    getIngredientFacets().then(setFacets).catch(() => setFacets(EMPTY_FACETS));
  }, []);

  // Resolve recent material IDs via server search (do not invent catalog rows).
  useEffect(() => {
    if (tab !== "recent" || recentIds.length === 0) {
      setRecentItems([]);
      return undefined;
    }
    let current = true;
    searchIngredients({ search: "", limit: RESULT_LIMIT, outletLocationId: outletLocationId || undefined })
      .then((page) => {
        if (!current) return;
        const byId = new Map(page.items.map((item) => [item.id.toLowerCase(), item]));
        // Also merge recommended + all results already loaded for better hit rate
        recommended.forEach((item) => byId.set(item.id.toLowerCase(), item));
        results.forEach((item) => byId.set(item.id.toLowerCase(), item));
        const matched = recentIds.map((id) => byId.get(id.toLowerCase())).filter((item): item is SilaIngredientOption => Boolean(item));
        setRecentItems(matched);
      })
      .catch(() => {
        if (current) setRecentItems([]);
      });
    return () => {
      current = false;
    };
  }, [tab, recentIds, recommended, results, outletLocationId]);

  const term = search.trim();
  const filtersOn = Boolean(materialGroup || category || materialType);
  const active = tab === "all" && (term.length >= 2 || filtersOn);

  useEffect(() => {
    if (tab !== "all") return undefined;
    if (!active) {
      // Soft browse first page when opening All without a filter so the catalog is never empty.
      let current = true;
      setSearching(true);
      searchIngredients({ search: "", limit: RESULT_LIMIT, outletLocationId: outletLocationId || undefined })
        .then((page) => {
          if (!current) return;
          setResults(page.items);
          setTotal(page.total);
          setSearchError(null);
        })
        .catch((err: unknown) => {
          if (current) setSearchError(err instanceof Error ? err.message : "Could not search the materials.");
        })
        .finally(() => {
          if (current) setSearching(false);
        });
      return () => {
        current = false;
      };
    }
    let current = true;
    const timer = window.setTimeout(async () => {
      setSearching(true);
      setSearchError(null);
      try {
        const page = await searchIngredients({
          search: term,
          materialGroup,
          category,
          materialType,
          outletLocationId: outletLocationId || undefined,
          limit: RESULT_LIMIT,
        });
        if (current) {
          setResults(page.items);
          setTotal(page.total);
        }
      } catch (err: unknown) {
        if (current) setSearchError(err instanceof Error ? err.message : "Could not search the materials.");
      } finally {
        if (current) setSearching(false);
      }
    }, 250);
    return () => {
      current = false;
      window.clearTimeout(timer);
    };
  }, [tab, active, term, materialGroup, category, materialType, outletLocationId]);

  const added = useMemo(() => new Set(addedIds.map((id) => id.toLowerCase())), [addedIds]);

  const displayItems = tab === "recommended" ? recommended : tab === "recent" ? recentItems : results;
  const selectedItems = displayItems.filter((item) => selected[item.id]);

  const pick = (material: SilaIngredientOption) => {
    if (locked) return;
    rememberMaterial(material.id);
    if (replaceTargetKey && onReplacePick) {
      onReplacePick(material);
      return;
    }
    onAdd(material);
  };

  const toggle = (id: string) => setSelected((current) => ({ ...current, [id]: !current[id] }));

  const startDrag = (event: React.DragEvent, material: SilaIngredientOption) => {
    if (locked) {
      event.preventDefault();
      return;
    }
    event.dataTransfer.setData("application/x-sila-material", JSON.stringify(material));
    event.dataTransfer.effectAllowed = "copy";
  };

  const renderCard = (material: SilaIngredientOption) => {
    const isAdded = added.has(material.id.toLowerCase());
    const kind = resolveStudioKind(material.materialGroup, material.category, material.description, material.materialCode);
    const familyLabel = material.materialGroup?.trim() || "Not Classified";
    const categoryLabel = material.category?.trim() || "Not Classified";
    return (
      <article
        key={material.id}
        className={`srec-lib-card${selected[material.id] ? " is-selected" : ""}${isAdded ? " is-added" : ""}`}
      >
        <label className="srec-lib-check">
          <input type="checkbox" checked={Boolean(selected[material.id])} onChange={() => toggle(material.id)} aria-label={`Select ${material.materialCode}`} />
        </label>
        <span
          className="srec-lib-drag"
          title="Drag to recipe"
          draggable={!locked}
          onDragStart={(event) => startDrag(event, material)}
          aria-label={`Drag ${material.materialCode} to recipe`}
        >
          ⠿
        </span>
        <StudioCover compact kind={kind} title={material.description || material.materialCode} />
        <div className="srec-lib-body">
          <div className="srec-lib-title">
            <strong>{material.description}</strong>
            <span className={priceStatusBadgeClass(material.priceStatus)}>{priceStatusLabel(material.priceStatus)}</span>
          </div>
          <div className="srec-lib-desc">{material.materialCode}</div>
          <div className="srec-lib-chips">
            <span className={`rstudio-chip is-${kind}`}>{familyLabel}</span>
            <span className="rstudio-chip">{categoryLabel}</span>
          </div>
          <div className="srec-lib-meta">
            <span>{material.baseUom}</span>
            <span>{unitPriceText(material.priceStatus, material.unitCost, material.currency, material.baseUom)}</span>
          </div>
          <div className="srec-sub">
            {outletLocationId
              ? material.stockAvailable == null
                ? "Stock UNKNOWN"
                : material.stockAvailable <= 0
                  ? `Out of stock (${material.stockFreshness || "—"})`
                  : `Avail ${material.stockAvailable} ${material.baseUom} · ${material.stockFreshness || "—"}`
              : "Stock not determined (select outlet)"}
          </div>
          <div className="srec-sub">{conversionSummary(material) || "Base UOM only"}</div>
        </div>
        <button
          type="button"
          className="sila-btn sila-btn--primary sila-btn--sm"
          disabled={locked || (isAdded && !replaceTargetKey)}
          draggable={false}
          onMouseDown={(event) => event.stopPropagation()}
          onPointerDown={(event) => event.stopPropagation()}
          onClick={(event) => {
            event.preventDefault();
            event.stopPropagation();
            pick(material);
          }}
          data-testid="recipe-catalog-add"
          aria-label={replaceTargetKey ? `Use ${material.materialCode}` : isAdded ? `${material.materialCode} already added` : `Add ingredient ${material.materialCode}`}
        >
          {locked ? "Read-only" : replaceTargetKey ? "Use material" : isAdded ? "Added" : "Add Ingredient"}
        </button>
      </article>
    );
  };

  return (
    <section className="srec-builder-panel srec-lib rstudio-catalog" aria-label="Recommended Material Catalog">
      <header className="srec-builder-panel__head">
        <div>
          <h2 className="srec-builder-panel__title">Recommended Material Catalog</h2>
          <p className="sila-help">Drag into My Recipe or multi-select and Add.</p>
        </div>
      </header>

      <div className="rstudio-catalog-tabs" role="tablist" aria-label="Catalog views">
        {([
          ["recommended", "Recommended"],
          ["all", "All Materials"],
          ["recent", "Recently Used"],
        ] as const).map(([id, label]) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={tab === id}
            className={`rstudio-chip${tab === id ? " is-active" : ""}`}
            onClick={() => setTab(id)}
          >
            {label}
          </button>
        ))}
      </div>

      {tab === "recommended" && <p className="sila-help rstudio-catalog-reason">{recommendReason}</p>}

      {(tab === "all" || tab === "recommended") && (
        <div className="srec-lib-filters">
          <div className="sila-field srec-grow">
            <label className="sila-label" htmlFor="srec-lib-search">Search</label>
            <input
              id="srec-lib-search"
              className="sila-input"
              type="search"
              placeholder="Code or name"
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                if (tab !== "all") setTab("all");
              }}
            />
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="srec-lib-group">Family</label>
            <select
              id="srec-lib-group"
              className="sila-select"
              value={materialGroup}
              onChange={(event) => {
                setMaterialGroup(event.target.value);
                setTab("all");
              }}
            >
              <option value="">All</option>
              {facets.materialGroups.map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="srec-lib-category">Category</label>
            <select
              id="srec-lib-category"
              className="sila-select"
              value={category}
              onChange={(event) => {
                setCategory(event.target.value);
                setTab("all");
              }}
            >
              <option value="">All</option>
              {facets.categories.map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="srec-lib-type">Type</label>
            <select
              id="srec-lib-type"
              className="sila-select"
              value={materialType}
              onChange={(event) => {
                setMaterialType(event.target.value);
                setTab("all");
              }}
            >
              <option value="">All</option>
              {facets.materialTypes.map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </div>
        </div>
      )}

      {selectedItems.length > 0 && (
        <div className="srec-lib-bulk">
          <span className="sila-help">{selectedItems.length} selected</span>
          <button
            type="button"
            className="sila-btn sila-btn--primary sila-btn--sm"
            disabled={locked}
            onClick={() => {
              if (locked) return;
              selectedItems.forEach((item) => rememberMaterial(item.id));
              onAddMany(selectedItems);
              setSelected({});
            }}
          >
            Add selected
          </button>
        </div>
      )}

      {tab === "all" && searching ? (
        <Loader size={20} message="Searching..." />
      ) : tab === "all" && searchError ? (
        <div className="sila-alert sila-alert--danger" role="alert">{searchError}</div>
      ) : displayItems.length === 0 ? (
        <span className="sila-help">
          {tab === "recent"
            ? "No recently used materials in this session yet."
            : tab === "recommended"
              ? "No recommendations yet — open All Materials or add ingredients to refresh suggestions."
              : "No materials matched."}
        </span>
      ) : (
        <div className="srec-lib-list rstudio-catalog-grid">
          {tab === "all" && (
            <h3 className="srec-builder-panel__subtitle">Materials ({Math.min(results.length, total)} of {total})</h3>
          )}
          {displayItems.map(renderCard)}
        </div>
      )}
    </section>
  );
};

export default SilaRecipeMaterialLibrary;
