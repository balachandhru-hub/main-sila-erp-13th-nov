import React, { useMemo, useState } from "react";
import {
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  arrayMove,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import type { SilaIngredientOption } from "../../../api/silaMe/silaRecipeToolsApi";
import type { SilaRecipeMode } from "../../../api/silaMe/silaRecipeApi";
import type { IngredientLine, IngredientMaterial, LineCost } from "./recipeEditorModel";
import { formatCost, formatQty, materialUoms, priceStatusBadgeClass, priceStatusLabel, unitPriceText } from "./recipeFormat";
import { lineLabel } from "./recipeBuilderState";
import { resolveStudioKind, StudioCover } from "./recipeStudioVisuals";

interface DropConflict {
  material: SilaIngredientOption;
  targetKey: string;
}

interface SilaRecipeCanvasProps {
  lines: IngredientLine[];
  costs: LineCost[];
  mode: SilaRecipeMode;
  currency: string;
  onReorder: (lines: IngredientLine[]) => void;
  onChange: (key: string, change: Partial<IngredientLine>) => void;
  onRemove: (key: string) => void;
  onDuplicate: (key: string) => void;
  onReplace: (key: string) => void;
  onAddMaterial: (material: SilaIngredientOption) => void;
  onReplaceMaterial: (key: string, material: SilaIngredientOption) => void;
  onUpdatePrice?: (material: IngredientMaterial) => void;
  /** Optional accessible Up/Down reorder (mobile/PWA; drag still works on desktop). */
  onMove?: (key: string, direction: -1 | 1) => void;
}

interface SortableRowProps {
  line: IngredientLine;
  cost: LineCost;
  currency: string;
  canMoveUp: boolean;
  canMoveDown: boolean;
  onChange: (key: string, change: Partial<IngredientLine>) => void;
  onRemove: (key: string) => void;
  onDuplicate: (key: string) => void;
  onReplace: (key: string) => void;
  onUpdatePrice?: (material: IngredientMaterial) => void;
  onMove?: (key: string, direction: -1 | 1) => void;
}

const SortableRow: React.FC<SortableRowProps> = ({
  line, cost, currency, canMoveUp, canMoveDown, onChange, onRemove, onDuplicate, onReplace, onUpdatePrice, onMove,
}) => {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: line.key });
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };
  const code = line.material?.materialCode ?? line.subRecipe?.recipeCode ?? "";
  const name = line.material?.description ?? line.subRecipe?.name ?? "";
  const kind = resolveStudioKind(name, code, line.uom);

  return (
    <article
      ref={setNodeRef}
      style={style}
      className={`srec-canvas-card${isDragging ? " is-dragging" : ""}${cost.cost == null ? " is-warn" : ""}`}
      data-line-key={line.key}
    >
      <button type="button" className="srec-canvas-handle" aria-label={`Reorder ${code}`} {...attributes} {...listeners}>
        ⋮⋮
      </button>
      <StudioCover compact kind={kind} title={name || code} />
      <div className="srec-canvas-main">
        <div className="srec-lib-title">
          <strong>{name}</strong>
          {line.material && <span className={priceStatusBadgeClass(cost.priceStatus)}>{priceStatusLabel(cost.priceStatus)}</span>}
          {line.subRecipe && <span className="srec-sub">Sub-recipe</span>}
        </div>
        <div className="srec-lib-desc">{code}</div>
        <div className="srec-canvas-edit">
          <label className="sila-field">
            <span className="sila-label">Qty</span>
            <input
              className="sila-input srec-cell-input"
              type="number"
              min="0"
              step="any"
              value={line.quantity}
              onChange={(event) => onChange(line.key, { quantity: event.target.value })}
            />
          </label>
          <label className="sila-field">
            <span className="sila-label">UOM</span>
            {line.material ? (
              <select
                className="sila-select srec-cell-input"
                value={line.uom}
                onChange={(event) => onChange(line.key, { uom: event.target.value })}
              >
                {Array.from(new Set([line.uom.toUpperCase(), ...materialUoms(line.material)])).map((unit) => (
                  <option key={unit} value={unit}>{unit}</option>
                ))}
              </select>
            ) : (
              <span className="sila-help">{line.uom}</span>
            )}
          </label>
        </div>
        <div className="srec-lib-meta">
          <span>
            {line.material
              ? (cost.cost == null ? "COST PENDING" : `${unitPriceText(cost.priceStatus, cost.unitCost, cost.currency, line.material.baseUom)}`)
              : `${formatCost(cost.unitCost)} / ${cost.baseUom}`}
          </span>
          <span>Ext. {cost.cost == null ? "COST PENDING" : formatCost(cost.cost, currency)}</span>
          {cost.baseQuantity != null && <span>Inv. {formatQty(cost.baseQuantity)} {cost.baseUom}</span>}
        </div>
        {cost.issue && <div className="sila-me-flag srec-negative">{cost.issue}</div>}
      </div>
      <div className="srec-canvas-actions">
        {onMove && (
          <>
            <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" disabled={!canMoveUp} onClick={() => onMove(line.key, -1)} aria-label={`Move ${code} up`}>
              Up
            </button>
            <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" disabled={!canMoveDown} onClick={() => onMove(line.key, 1)} aria-label={`Move ${code} down`}>
              Down
            </button>
          </>
        )}
        {line.material && (
          <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => onReplace(line.key)}>Replace</button>
        )}
        <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => onDuplicate(line.key)}>Duplicate</button>
        {line.material && onUpdatePrice && line.material.priceStatus !== "PENDING_APPROVAL" && (
          <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => line.material && onUpdatePrice(line.material)}>Price</button>
        )}
        <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => onRemove(line.key)}>Remove</button>
      </div>
    </article>
  );
};

/** Center recipe canvas: sortable ingredient cards with library drop zone. */
const SilaRecipeCanvas: React.FC<SilaRecipeCanvasProps> = ({
  lines,
  costs,
  mode,
  currency,
  onReorder,
  onChange,
  onRemove,
  onDuplicate,
  onReplace,
  onAddMaterial,
  onReplaceMaterial,
  onUpdatePrice,
  onMove,
}) => {
  const [activeId, setActiveId] = useState<string | null>(null);
  const [dropActive, setDropActive] = useState(false);
  const [conflict, setConflict] = useState<DropConflict | null>(null);
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );
  const ids = useMemo(() => lines.map((line) => line.key), [lines]);
  const activeLine = lines.find((line) => line.key === activeId);

  const onDragStart = (event: DragStartEvent) => setActiveId(String(event.active.id));
  const onDragEnd = (event: DragEndEvent) => {
    setActiveId(null);
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    const oldIndex = lines.findIndex((line) => line.key === active.id);
    const newIndex = lines.findIndex((line) => line.key === over.id);
    if (oldIndex < 0 || newIndex < 0) return;
    onReorder(arrayMove(lines, oldIndex, newIndex));
  };

  const parseMaterial = (event: React.DragEvent): SilaIngredientOption | null => {
    try {
      const raw = event.dataTransfer.getData("application/x-sila-material");
      return raw ? (JSON.parse(raw) as SilaIngredientOption) : null;
    } catch {
      return null;
    }
  };

  const handleLibraryDrop = (event: React.DragEvent, targetKey?: string) => {
    event.preventDefault();
    setDropActive(false);
    const material = parseMaterial(event);
    if (!material) return;
    if (targetKey) {
      setConflict({ material, targetKey });
      return;
    }
    onAddMaterial(material);
  };

  return (
    <section className="srec-builder-panel srec-canvas" aria-label="Recipe canvas">
      <header className="srec-builder-panel__head">
        <h2 className="srec-builder-panel__title">
          {mode === "DIRECT" ? "My Recipe · Direct" : "My Recipe"}
        </h2>
        <p className="sila-help">{lines.length} ingredient(s). Drag from the catalog, reorder on the canvas, or use Add.</p>
      </header>

      <div
        className={`srec-canvas-drop${dropActive ? " is-active" : ""}`}
        onDragOver={(event) => {
          if (event.dataTransfer.types.includes("application/x-sila-material")) {
            event.preventDefault();
            setDropActive(true);
          }
        }}
        onDragLeave={() => setDropActive(false)}
        onDrop={(event) => handleLibraryDrop(event)}
      >
        Drag ingredients here or browse the ingredient catalog
      </div>

      {conflict && (
        <div className="sila-alert sila-alert--warning srec-drop-confirm" role="dialog" aria-label="Drop action">
          <p>
            Dropped <strong>{conflict.material.materialCode}</strong> on{" "}
            <strong>{lineLabel(lines.find((line) => line.key === conflict.targetKey) ?? { key: "", quantity: "", uom: "" })}</strong>.
          </p>
          <div className="srec-actions">
            <button type="button" className="sila-btn sila-btn--primary sila-btn--sm" onClick={() => { onReplaceMaterial(conflict.targetKey, conflict.material); setConflict(null); }}>
              Replace existing
            </button>
            <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => { onAddMaterial(conflict.material); setConflict(null); }}>
              Add as another
            </button>
            <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => setConflict(null)}>Cancel</button>
          </div>
        </div>
      )}

      {lines.length === 0 ? (
        <div className="srec-canvas-empty">No ingredients yet. Search the library and add, or drop a material here.</div>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragStart={onDragStart} onDragEnd={onDragEnd}>
          <SortableContext items={ids} strategy={verticalListSortingStrategy}>
            <div className="srec-canvas-list">
              {lines.map((line, index) => (
                <div
                  key={line.key}
                  onDragOver={(event) => {
                    if (event.dataTransfer.types.includes("application/x-sila-material")) event.preventDefault();
                  }}
                  onDrop={(event) => handleLibraryDrop(event, line.key)}
                >
                  <SortableRow
                    line={line}
                    cost={costs[index]}
                    currency={currency}
                    canMoveUp={index > 0}
                    canMoveDown={index < lines.length - 1}
                    onChange={onChange}
                    onRemove={onRemove}
                    onDuplicate={onDuplicate}
                    onReplace={onReplace}
                    onUpdatePrice={onUpdatePrice}
                    onMove={onMove}
                  />
                </div>
              ))}
            </div>
          </SortableContext>
          <DragOverlay>
            {activeLine ? (
              <div className="srec-canvas-card is-overlay">
                <strong>{lineLabel(activeLine)}</strong>
              </div>
            ) : null}
          </DragOverlay>
        </DndContext>
      )}
    </section>
  );
};

export default SilaRecipeCanvas;
