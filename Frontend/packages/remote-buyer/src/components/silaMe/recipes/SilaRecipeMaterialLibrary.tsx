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

interface SilaRecipeMaterialLibraryProps {
  addedIds: string[];
  onAdd: (material: SilaIngredientOption) => void;
  onAddMany: (materials: SilaIngredientOption[]) => void;
  /** When set, Add replaces the target line instead of appending. */
  replaceTargetKey?: string | null;
  onReplacePick?: (material: SilaIngredientOption) => void;
}

const EMPTY_FACETS: SilaIngredientFacets = { materialGroups: [], categories: [], suppliers: [], materialTypes: [], supplierOptions: [] };
const RESULT_LIMIT = 30;

/** Left-panel Material Master library: search, filter, multi-select, drag source. */
const SilaRecipeMaterialLibrary: React.FC<SilaRecipeMaterialLibraryProps> = ({
  addedIds,
  onAdd,
  onAddMany,
  replaceTargetKey,
  onReplacePick,
}) => {
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

  useEffect(() => {
    getIngredientFacets().then(setFacets).catch(() => setFacets(EMPTY_FACETS));
  }, []);

  const term = search.trim();
  const active = term.length >= 2 || Boolean(materialGroup || category || materialType);

  useEffect(() => {
    if (!active) {
      setResults([]);
      setTotal(0);
      setSearchError(null);
      return undefined;
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
  }, [active, term, materialGroup, category, materialType]);

  const added = useMemo(() => new Set(addedIds.map((id) => id.toLowerCase())), [addedIds]);
  const selectedItems = results.filter((item) => selected[item.id]);
  const recentInResults = useMemo(() => {
    const byId = new Map(results.map((item) => [item.id.toLowerCase(), item]));
    return recentIds.map((id) => byId.get(id.toLowerCase())).filter((item): item is SilaIngredientOption => Boolean(item));
  }, [recentIds, results]);

  const pick = (material: SilaIngredientOption) => {
    rememberMaterial(material.id);
    if (replaceTargetKey && onReplacePick) {
      onReplacePick(material);
      return;
    }
    onAdd(material);
  };

  const toggle = (id: string) => setSelected((current) => ({ ...current, [id]: !current[id] }));

  const startDrag = (event: React.DragEvent, material: SilaIngredientOption) => {
    event.dataTransfer.setData("application/x-sila-material", JSON.stringify(material));
    event.dataTransfer.effectAllowed = "copy";
  };

  const renderCard = (material: SilaIngredientOption) => {
    const isAdded = added.has(material.id.toLowerCase());
    const kind = resolveStudioKind(material.materialGroup, material.category, material.description, material.materialCode);
    return (
      <article
        key={material.id}
        className={`srec-lib-card${selected[material.id] ? " is-selected" : ""}${isAdded ? " is-added" : ""}`}
        draggable
        onDragStart={(event) => startDrag(event, material)}
      >
        <label className="srec-lib-check">
          <input type="checkbox" checked={Boolean(selected[material.id])} onChange={() => toggle(material.id)} aria-label={`Select ${material.materialCode}`} />
        </label>
        <StudioCover compact kind={kind} title={material.description || material.materialCode} />
        <div className="srec-lib-body">
          <div className="srec-lib-title">
            <strong>{material.description}</strong>
            <span className={priceStatusBadgeClass(material.priceStatus)}>{priceStatusLabel(material.priceStatus)}</span>
          </div>
          <div className="srec-lib-desc">{material.materialCode}</div>
          <div className="srec-lib-meta">
            <span>{material.baseUom}</span>
            {material.materialGroup && <span>{material.materialGroup}</span>}
            <span>{unitPriceText(material.priceStatus, material.unitCost, material.currency, material.baseUom)}</span>
          </div>
          <div className="srec-sub">{conversionSummary(material) || "Base UOM only"}</div>
        </div>
        <button
          type="button"
          className="sila-btn sila-btn--primary sila-btn--sm"
          disabled={isAdded && !replaceTargetKey}
          onClick={() => pick(material)}
        >
          {replaceTargetKey ? "Use" : isAdded ? "Added" : "Add"}
        </button>
      </article>
    );
  };

  return (
    <section className="srec-builder-panel srec-lib" aria-label="Ingredient shopping catalog">
      <header className="srec-builder-panel__head">
        <h2 className="srec-builder-panel__title">Ingredient catalog</h2>
        <p className="sila-help">Grocery-style Material Master. Drag to the canvas or add selected.</p>
      </header>
      <div className="srec-lib-filters">
        <div className="sila-field srec-grow">
          <label className="sila-label" htmlFor="srec-lib-search">Search</label>
          <input
            id="srec-lib-search"
            className="sila-input"
            type="search"
            placeholder="Code or name"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </div>
        <div className="sila-field">
          <label className="sila-label" htmlFor="srec-lib-group">Group</label>
          <select id="srec-lib-group" className="sila-select" value={materialGroup} onChange={(event) => setMaterialGroup(event.target.value)}>
            <option value="">All</option>
            {facets.materialGroups.map((value) => <option key={value} value={value}>{value}</option>)}
          </select>
        </div>
        <div className="sila-field">
          <label className="sila-label" htmlFor="srec-lib-category">Category</label>
          <select id="srec-lib-category" className="sila-select" value={category} onChange={(event) => setCategory(event.target.value)}>
            <option value="">All</option>
            {facets.categories.map((value) => <option key={value} value={value}>{value}</option>)}
          </select>
        </div>
        <div className="sila-field">
          <label className="sila-label" htmlFor="srec-lib-type">Type</label>
          <select id="srec-lib-type" className="sila-select" value={materialType} onChange={(event) => setMaterialType(event.target.value)}>
            <option value="">All</option>
            {facets.materialTypes.map((value) => <option key={value} value={value}>{value}</option>)}
          </select>
        </div>
      </div>
      {selectedItems.length > 0 && (
        <div className="srec-lib-bulk">
          <span className="sila-help">{selectedItems.length} selected</span>
          <button
            type="button"
            className="sila-btn sila-btn--primary sila-btn--sm"
            onClick={() => {
              selectedItems.forEach((item) => rememberMaterial(item.id));
              onAddMany(selectedItems);
              setSelected({});
            }}
          >
            Add selected
          </button>
        </div>
      )}
      {!active ? (
        <span className="sila-help">Type at least two characters or choose a filter.</span>
      ) : searching ? (
        <Loader size={20} message="Searching..." />
      ) : searchError ? (
        <div className="sila-alert sila-alert--danger" role="alert">{searchError}</div>
      ) : results.length === 0 ? (
        <span className="sila-help">No materials matched.</span>
      ) : (
        <div className="srec-lib-list">
          {recentInResults.length > 0 && (
            <div className="srec-lib-section">
              <h3 className="srec-builder-panel__subtitle">Recent in results</h3>
              {recentInResults.map(renderCard)}
            </div>
          )}
          <div className="srec-lib-section">
            <h3 className="srec-builder-panel__subtitle">Materials ({Math.min(results.length, total)} of {total})</h3>
            {results.map(renderCard)}
          </div>
        </div>
      )}
    </section>
  );
};

export default SilaRecipeMaterialLibrary;
