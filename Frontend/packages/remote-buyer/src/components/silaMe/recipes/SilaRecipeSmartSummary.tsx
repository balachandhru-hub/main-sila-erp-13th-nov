import React, { useState } from "react";
import { EmptyState } from "@vosox/shared-ui";
import type { SilaRecipeMaster } from "../../../api/silaMe/silaRecipeToolsApi";
import type { MenuItemDetail } from "../../../api/silaMe/silaMenuEngineeringApi";
import type { SilaLocation } from "../../../api/silaMe/silaInventoryApi";
import type { HeaderValues } from "./recipeEditorModel";
import { formatCost } from "./recipeFormat";
import type { ClassificationSuggestion, NameSuggestion, PricingSuggestion } from "./recipeStudioSuggestions";

export interface PosIdOption {
  code: string;
  label: string;
  /** POS item description only (not the composite label). */
  description?: string;
  /** True when another recipe already owns this POS code — not selectable for save. */
  taken?: boolean;
}

interface SilaRecipeSmartSummaryProps {
  header: HeaderValues;
  families: SilaRecipeMaster[];
  categories: SilaRecipeMaster[];
  uoms: string[];
  recipeCode?: string | null;
  nameConfirmed: boolean;
  classConfirmed: boolean;
  nameSuggestions: NameSuggestion[];
  classification: ClassificationSuggestion | null;
  pricing: PricingSuggestion;
  currency: string;
  totalCost: number | null;
  menuPrice: string;
  menuOutlet?: string;
  linkedMenuItem: MenuItemDetail | null;
  menuMatches: MenuItemDetail[];
  menuSearch: string;
  menuRecommendations: MenuItemDetail[];
  canLinkMenu: boolean;
  linkingMenu: boolean;
  updatingMenuPrice: boolean;
  posOptions: PosIdOption[];
  outlets: SilaLocation[];
  outletRecommendations: SilaLocation[];
  prices: Record<string, string>;
  onHeaderChange: (change: Partial<HeaderValues>) => void;
  onConfirmName: (name: string) => void;
  onConfirmClassification: (suggestion: ClassificationSuggestion) => void;
  onTargetFoodCost: (percent: number) => void;
  onMenuSearch: (value: string) => void;
  onSearchMenuItems: () => void;
  onLinkMenuItem: (id: string) => void;
  onUpdateMenuItemPrice: () => void;
  onMenuPrice: (value: string) => void;
  onOutletPrice: (outletId: string, value: string) => void;
  onRemoveOutlet: (outletId: string) => void;
  onToggleOutlet: (outlet: SilaLocation) => void;
  onAddOutlet: (outletId: string) => void;
}

/** Expandable smart summary: suggestions, classification, menu/outlet, pricing — not a mandatory header form. */
const SilaRecipeSmartSummary: React.FC<SilaRecipeSmartSummaryProps> = ({
  header,
  families,
  categories,
  uoms,
  recipeCode,
  nameConfirmed,
  classConfirmed,
  nameSuggestions,
  classification,
  pricing,
  currency,
  totalCost,
  menuPrice,
  menuOutlet,
  linkedMenuItem,
  menuMatches,
  menuSearch,
  menuRecommendations,
  canLinkMenu,
  linkingMenu,
  updatingMenuPrice,
  posOptions,
  outlets,
  outletRecommendations,
  prices,
  onHeaderChange,
  onConfirmName,
  onConfirmClassification,
  onTargetFoodCost,
  onMenuSearch,
  onSearchMenuItems,
  onLinkMenuItem,
  onUpdateMenuItemPrice,
  onMenuPrice,
  onOutletPrice,
  onRemoveOutlet,
  onToggleOutlet,
  onAddOutlet,
}) => {
  const [open, setOpen] = useState(true);
  const [outletPick, setOutletPick] = useState("");
  const assignedIds = Object.keys(prices);
  const availableOutlets = outlets.filter((outlet) => !Object.prototype.hasOwnProperty.call(prices, outlet.id));

  return (
    <section className="srec-builder-panel rstudio-smart" aria-label="Smart Recipe Summary">
      <header className="srec-builder-panel__head">
        <div>
          <h2 className="srec-builder-panel__title">Smart Recipe Summary</h2>
          <p className="sila-help">Suggested · Calculated · Confirmed — override before Save.</p>
        </div>
        <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => setOpen((v) => !v)}>
          {open ? "Collapse" : "Expand"}
        </button>
      </header>

      {!open ? (
        <div className="rstudio-smart-chips">
          <span className="rstudio-chip">{header.name.trim() || "Untitled draft"}</span>
          <span className="rstudio-chip">{families.find((f) => f.id === header.familyId)?.name || "Family?"}</span>
          <span className="rstudio-chip">{categories.find((c) => c.id === header.categoryId)?.name || "Category?"}</span>
          <span className="rstudio-chip">
            {pricing.incomplete || pricing.suggestedSellingPrice == null
              ? "Price COST PENDING"
              : `Suggest ${formatCost(pricing.suggestedSellingPrice, currency)}`}
          </span>
        </div>
      ) : (
        <div className="rstudio-smart-body">
          <div className="rstudio-smart-block">
            <h3 className="srec-builder-panel__subtitle">Recipe identity {recipeCode ? `· ${recipeCode}` : "· unsaved draft"}</h3>
            <div className="sila-form-grid">
              <div className="sila-field">
                <label className="sila-label" htmlFor="rstudio-name">Name</label>
                <input
                  id="rstudio-name"
                  className="sila-input"
                  maxLength={200}
                  value={header.name}
                  placeholder="Name before save (optional while building)"
                  onChange={(e) => onHeaderChange({ name: e.target.value })}
                />
                {nameConfirmed && <span className="sila-help">Confirmed by chef</span>}
              </div>
              <div className="sila-field">
                <label className="sila-label" htmlFor="rstudio-mode">Item mode</label>
                <select
                  id="rstudio-mode"
                  className="sila-select"
                  value={header.itemMode}
                  onChange={(e) => onHeaderChange({ itemMode: e.target.value as HeaderValues["itemMode"] })}
                >
                  <option value="RECIPE">Recipe</option>
                  <option value="DIRECT">Direct</option>
                  <option value="BATCH">Batch</option>
                </select>
              </div>
              <div className="sila-field">
                <label className="sila-label" htmlFor="rstudio-family">Family</label>
                <select id="rstudio-family" className="sila-select" value={header.familyId} onChange={(e) => onHeaderChange({ familyId: e.target.value })}>
                  <option value="">Not set</option>
                  {families.map((family) => (
                    <option key={family.id} value={family.id}>{family.code} · {family.name}</option>
                  ))}
                </select>
              </div>
              <div className="sila-field">
                <label className="sila-label" htmlFor="rstudio-category">Category</label>
                <select id="rstudio-category" className="sila-select" value={header.categoryId} onChange={(e) => onHeaderChange({ categoryId: e.target.value })}>
                  <option value="">Not set</option>
                  {categories.map((category) => (
                    <option key={category.id} value={category.id}>{category.code} · {category.name}</option>
                  ))}
                </select>
              </div>
              <div className="sila-field">
                <label className="sila-label" htmlFor="rstudio-currency">Currency</label>
                <input id="rstudio-currency" className="sila-input" maxLength={10} value={header.currency} onChange={(e) => onHeaderChange({ currency: e.target.value })} />
              </div>
              <div className="sila-field">
                <label className="sila-label" htmlFor="rstudio-selling-uom">Selling UOM</label>
                <input
                  id="rstudio-selling-uom"
                  className="sila-input"
                  list="rstudio-uom-list"
                  value={header.sellingUom}
                  onChange={(e) => onHeaderChange({ sellingUom: e.target.value })}
                />
                <datalist id="rstudio-uom-list">
                  {uoms.map((unit) => <option key={unit} value={unit} />)}
                </datalist>
              </div>
              <div className="sila-field">
                <label className="sila-label" htmlFor="rstudio-pos-pick">POS ID</label>
                <select
                  id="rstudio-pos-pick"
                  className="sila-select"
                  value={posOptions.some((row) => row.code === header.posCode && !row.taken) ? header.posCode : ""}
                  onChange={(e) => {
                    const code = e.target.value;
                    if (!code) {
                      onHeaderChange({ posCode: "", posItem: "" });
                      return;
                    }
                    const match = posOptions.find((row) => row.code === code);
                    if (match?.taken) return;
                    onHeaderChange({
                      posCode: code,
                      posItem: (match?.description ?? "").slice(0, 200),
                    });
                  }}
                >
                  <option value="">Choose POS ID / enter below</option>
                  {posOptions.filter((row) => !row.taken).map((row) => (
                    <option key={row.code} value={row.code}>{row.label}</option>
                  ))}
                </select>
              </div>
              <div className="sila-field">
                <label className="sila-label" htmlFor="rstudio-pos-code">POS ID (manual)</label>
                <input
                  id="rstudio-pos-code"
                  className="sila-input"
                  maxLength={50}
                  list="rstudio-pos-list"
                  value={header.posCode}
                  placeholder="Unique POS item code"
                  onChange={(e) => onHeaderChange({ posCode: e.target.value })}
                />
                <datalist id="rstudio-pos-list">
                  {posOptions.filter((row) => !row.taken).map((row) => (
                    <option key={row.code} value={row.code}>{row.description || row.label}</option>
                  ))}
                </datalist>
              </div>
              <div className="sila-field">
                <label className="sila-label" htmlFor="rstudio-pos-item">POS item name</label>
                <input
                  id="rstudio-pos-item"
                  className="sila-input"
                  maxLength={200}
                  value={header.posItem}
                  placeholder="Optional POS description"
                  onChange={(e) => onHeaderChange({ posItem: e.target.value.slice(0, 200) })}
                />
              </div>
            </div>
            {nameSuggestions.length > 0 && !nameConfirmed && (
              <div className="rstudio-suggest-row">
                <span className="sila-help">Suggested names</span>
                {nameSuggestions.map((item) => (
                  <button key={item.name} type="button" className="rstudio-chip" title={item.reason} onClick={() => onConfirmName(item.name)}>
                    {item.name}
                  </button>
                ))}
              </div>
            )}
            {classification && (
              <div className="rstudio-suggest-row">
                <span className="sila-help">
                  {classConfirmed ? "New suggestion (confirmed values kept until you apply): " : ""}
                  {classification.reason}
                </span>
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onConfirmClassification(classification)}>
                  Apply {classification.familyName} / {classification.categoryName}
                  {classification.mealPeriod ? ` · ${classification.mealPeriod}` : ""}
                </button>
              </div>
            )}
          </div>

          <div className="rstudio-smart-block">
            <h3 className="srec-builder-panel__subtitle">Pricing intelligence</h3>
            <div className="rstudio-pricing-grid">
              <div>
                <span className="sila-help">Recipe cost</span>
                <strong>{pricing.incomplete || totalCost == null ? "COST PENDING" : formatCost(totalCost, currency)}</strong>
              </div>
              <div>
                <span className="sila-help">Cost / portion</span>
                <strong>{pricing.incomplete || pricing.costPerPortion == null ? "COST PENDING" : formatCost(pricing.costPerPortion, currency)}</strong>
              </div>
              <div>
                <span className="sila-help">Suggested sell</span>
                <strong>{pricing.incomplete || pricing.suggestedSellingPrice == null ? "COST PENDING" : formatCost(pricing.suggestedSellingPrice, currency)}</strong>
              </div>
              <div>
                <span className="sila-help">Target food cost %</span>
                <input
                  className="sila-input"
                  type="number"
                  min={1}
                  max={99}
                  step="any"
                  value={pricing.targetFoodCostPercent}
                  onChange={(e) => onTargetFoodCost(Number(e.target.value))}
                  aria-label="Target food cost percent"
                />
              </div>
              <div>
                <span className="sila-help">Actual food cost %</span>
                <strong>{pricing.actualFoodCostPercent == null ? "—" : `${pricing.actualFoodCostPercent}%`}</strong>
              </div>
              <div>
                <span className="sila-help">Menu price {menuOutlet ? `(${menuOutlet})` : "(draft)"}</span>
                <input className="sila-input" value={menuPrice} onChange={(e) => onMenuPrice(e.target.value)} placeholder="Enter menu price" />
              </div>
            </div>
            <div className="rstudio-suggest-row">
              <button
                type="button"
                className="sila-btn sila-btn--secondary sila-btn--sm"
                disabled={!linkedMenuItem?.id || !menuPrice.trim() || updatingMenuPrice || linkingMenu}
                onClick={onUpdateMenuItemPrice}
                title={linkedMenuItem ? "Push this menu price to the linked Menu Item selling price" : "Link a Menu Item first"}
              >
                {updatingMenuPrice ? "Updating menu price..." : "Update Menu Item price"}
              </button>
              <span className="sila-help">
                Outlet menu prices save with the recipe. Use Update Menu Item price to sync the commercial selling price.
              </span>
            </div>
          </div>

          <div className="rstudio-smart-block">
            <h3 className="srec-builder-panel__subtitle">Menu Item linkage</h3>
            <p className="sila-help">Menu Item = commercial/planning. Recipe = production/ingredients.</p>
            {linkedMenuItem ? (
              <p>
                Linked to <strong>{linkedMenuItem.name}</strong> ({linkedMenuItem.menuItemCode}) · {linkedMenuItem.recipeLinkStatus}
                {linkedMenuItem.sellingPrice != null && <> · sell {formatCost(linkedMenuItem.sellingPrice, currency)}</>}
              </p>
            ) : (
              <span className="sila-help">No Menu Item linked — allowed until save rules require otherwise.</span>
            )}
            {!canLinkMenu && <span className="sila-help">Save the recipe first, then link a Menu Item.</span>}
            {menuRecommendations.length > 0 && (
              <div className="rstudio-suggest-row">
                <span className="sila-help">Recommended Menu Items</span>
                {menuRecommendations.map((item) => (
                  <button
                    key={item.id ?? item.menuItemCode}
                    type="button"
                    className="rstudio-chip"
                    disabled={!canLinkMenu || linkingMenu || !item.id}
                    onClick={() => item.id && onLinkMenuItem(item.id)}
                  >
                    {item.name}
                  </button>
                ))}
              </div>
            )}
            <div className="srec-filters srec-filters--flush">
              <div className="sila-field srec-grow">
                <label className="sila-label" htmlFor="rstudio-menu-search">Search Menu Items</label>
                <input
                  id="rstudio-menu-search"
                  className="sila-input"
                  value={menuSearch}
                  placeholder="Name or code"
                  disabled={!canLinkMenu}
                  onChange={(e) => onMenuSearch(e.target.value)}
                />
              </div>
              <button type="button" className="sila-btn sila-btn--secondary" disabled={!canLinkMenu || linkingMenu} onClick={onSearchMenuItems}>Search</button>
            </div>
            {menuMatches.length === 0 ? (
              <EmptyState title="No Menu Items in this search." description="Create in Menu Engineering, then link here." />
            ) : (
              <ul className="srec-stack">
                {menuMatches.map((row) => (
                  <li key={row.id ?? row.menuItemCode} className="srec-filters srec-filters--flush">
                    <span>
                      <strong>{row.name}</strong>
                      <span className="sila-help"> {row.menuItemCode} · {row.recipeLinkStatus}</span>
                    </span>
                    <button
                      type="button"
                      className="sila-btn sila-btn--primary sila-btn--sm"
                      disabled={!canLinkMenu || linkingMenu || !row.id}
                      onClick={() => row.id && onLinkMenuItem(row.id)}
                    >
                      Link
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>

          <div className="rstudio-smart-block">
            <h3 className="srec-builder-panel__subtitle">Outlets &amp; menu prices</h3>
            <p className="sila-help">Choose outlets from the dropdown (multi-select by adding one at a time). Set or update each outlet menu price, then Save.</p>
            <div className="srec-filters srec-filters--flush">
              <div className="sila-field srec-grow">
                <label className="sila-label" htmlFor="rstudio-outlet-select">Add outlet</label>
                <select
                  id="rstudio-outlet-select"
                  className="sila-select"
                  value={outletPick}
                  onChange={(e) => setOutletPick(e.target.value)}
                  disabled={availableOutlets.length === 0}
                >
                  <option value="">
                    {availableOutlets.length === 0
                      ? (outlets.length === 0 ? "No outlet locations configured" : "All outlets already selected")
                      : "Select an outlet…"}
                  </option>
                  {availableOutlets.map((outlet) => (
                    <option key={outlet.id} value={outlet.id}>
                      {outlet.locationName}
                      {outlet.locationCode ? ` (${outlet.locationCode})` : ""}
                      {outlet.propertyName ? ` · ${outlet.propertyName}` : ""}
                    </option>
                  ))}
                </select>
              </div>
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                disabled={!outletPick}
                onClick={() => {
                  if (!outletPick) return;
                  onAddOutlet(outletPick);
                  setOutletPick("");
                }}
              >
                Add outlet
              </button>
            </div>
            {outletRecommendations.length > 0 && (
              <div className="rstudio-suggest-row">
                <span className="sila-help">Suggested</span>
                {outletRecommendations.map((outlet) => {
                  const assigned = Object.prototype.hasOwnProperty.call(prices, outlet.id);
                  return (
                    <button
                      key={outlet.id}
                      type="button"
                      className={`rstudio-chip${assigned ? " is-active" : ""}`}
                      onClick={() => onToggleOutlet(outlet)}
                    >
                      {outlet.locationName}
                    </button>
                  );
                })}
              </div>
            )}
            {assignedIds.length === 0 ? (
              <EmptyState title="No outlets selected" description="Pick outlets from the dropdown to set menu prices." />
            ) : (
              <div className="sila-form-grid">
                {assignedIds.map((id) => {
                  const outlet = outlets.find((row) => row.id === id);
                  return (
                    <div key={id} className="sila-field">
                      <label className="sila-label" htmlFor={`rstudio-outlet-${id}`}>
                        {outlet?.locationName ?? id}
                        {outlet?.locationCode ? ` · ${outlet.locationCode}` : ""}
                      </label>
                      <div className="srec-filters srec-filters--flush">
                        <input
                          id={`rstudio-outlet-${id}`}
                          className="sila-input"
                          value={prices[id]}
                          placeholder="Menu price"
                          inputMode="decimal"
                          onChange={(e) => onOutletPrice(id, e.target.value)}
                        />
                        <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => onRemoveOutlet(id)}>Remove</button>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      )}
    </section>
  );
};

export default SilaRecipeSmartSummary;
