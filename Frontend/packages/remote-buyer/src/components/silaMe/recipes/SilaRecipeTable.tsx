import React from "react";
import type { SilaRecipeListItem } from "../../../api/silaMe/silaRecipeApi";
import { formatDateTime } from "../../cart/lineFormat";
import { approvalEvent, costPerServing, formatCost, formatPercent, formatQty, readinessStatusBadgeClass, readinessText, recipeBadgeClass, recipeLabel } from "./recipeFormat";

interface SilaRecipeTableProps {
  rows: SilaRecipeListItem[];
  onOpen: (recipeId: string) => void;
  /** Label of the open button, e.g. "Open" or "Review". */
  openLabel?: string;
  /** Shows the approval event (CREATE / CHANGE) and version columns (recipe approvals). */
  showEvent?: boolean;
  onEdit?: (recipeId: string) => void;
  onDelete?: (recipe: SilaRecipeListItem) => void;
}

const Incomplete: React.FC = () => <span className="sila-me-flag">Incomplete</span>;

/**
 * Recipe master columns: POS code, title, serving, recipe id, family, category, menu price,
 * recipe cost, cost per serving, cost % and margin (or INCOMPLETE / NO MENU PRICE), status and readiness.
 */
const SilaRecipeTable: React.FC<SilaRecipeTableProps> = ({ rows, onOpen, openLabel = "Open", showEvent = false, onEdit, onDelete }) => (
  <div className="sila-studio-table-wrap">
    <table className="sila-studio-table">
      <thead>
        <tr>
          <th scope="col">POS code</th>
          <th scope="col">Title</th>
          <th scope="col">POS item</th>
          <th scope="col">Serving UOM</th>
          <th scope="col" className="srec-num">Serving qty</th>
          <th scope="col">Recipe ID</th>
          {showEvent && <th scope="col">Event</th>}
          {showEvent && <th scope="col">Level / approver</th>}
          <th scope="col">Family</th>
          <th scope="col">Category</th>
          <th scope="col">Mode</th>
          <th scope="col">Menu price</th>
          <th scope="col" className="srec-num">Recipe cost</th>
          <th scope="col" className="srec-num">Cost / serving</th>
          <th scope="col" className="srec-num">Cost %</th>
          <th scope="col" className="srec-num">Margin</th>
          <th scope="col">Status</th>
          <th scope="col">Readiness</th>
          <th scope="col">Updated</th>
          <th scope="col"><span className="sila-visually-hidden">Actions</span></th>
        </tr>
      </thead>
      <tbody>
        {rows.map((row) => (
          <tr key={row.id}>
            <td>{row.posCode || "—"}</td>
            <td className="sila-cell-strong">{row.name}</td>
            <td>{row.posItem || "—"}</td>
            <td>{row.servingUom}</td>
            <td className="srec-num">{formatQty(row.servingQty)}</td>
            <td>{row.recipeCode}</td>
            {showEvent && (
              <td>
                {row.approvalEvent || approvalEvent(row.activeVersion)}
                <span className="srec-sub">v{row.version}</span>
              </td>
            )}
            {showEvent && (
              <td>
                {row.approvalLevel != null ? `Level ${row.approvalLevel}${row.approvalLevelCount ? ` of ${row.approvalLevelCount}` : ""}` : "—"}
                {(row.approverName || row.approverRole) && (
                  <span className="srec-sub">{[row.approverName, row.approverRole].filter(Boolean).join(" · ")}</span>
                )}
              </td>
            )}
            <td>{row.familyName || "—"}</td>
            <td>{row.category || "—"}</td>
            <td>{recipeLabel(row.itemMode)}</td>
            <td>
              {row.menuPrice == null ? <span className="sila-me-flag">No menu price</span> : formatCost(row.menuPrice, row.currency)}
              {row.outletCount > 1 && <span className="srec-sub">{row.outletCount} outlets</span>}
            </td>
            <td className="srec-num">
              {row.costComplete || row.activeVersion > 0 ? formatCost(row.totalCost, row.currency) : <Incomplete />}
              {row.draftTotalCost != null && <span className="srec-sub">Draft {formatCost(row.draftTotalCost, row.currency)}</span>}
            </td>
            <td className="srec-num">
              {row.costComplete || row.activeVersion > 0
                ? formatCost(row.costPerServing ?? costPerServing(row.totalCost, row.servingQty), row.currency)
                : <Incomplete />}
            </td>
            <td className="srec-num">
              {!row.costComplete ? <Incomplete /> : row.menuPrice == null ? <span className="sila-me-flag">No menu price</span> : formatPercent(row.costPercent)}
            </td>
            <td className="srec-num">
              {!row.costComplete ? <Incomplete /> : row.menuPrice == null ? <span className="sila-me-flag">No menu price</span> : formatCost(row.marginAmount, row.currency)}
            </td>
            <td>
              <span className="srec-badges">
                <span className={recipeBadgeClass(row.status)}>{recipeLabel(row.status)}</span>
                <span className="srec-sub">
                  v{row.version}
                  {row.activeVersion > 0 && row.activeVersion !== row.version ? ` · selling v${row.activeVersion}` : ""}
                </span>
              </span>
            </td>
            <td>
              {row.status === "INACTIVE" ? (
                "—"
              ) : (
                <span className={readinessStatusBadgeClass(row.readinessStatus, row.issueCount)}>{readinessText(row.readinessStatus, row.issueCount)}</span>
              )}
            </td>
            <td>{formatDateTime(row.dateUpdated ?? row.submittedOn)}</td>
            <td>
              <div className="srec-actions">
                <button
                  type="button"
                  className="sila-btn sila-btn--secondary sila-btn--sm"
                  aria-label={`${openLabel} ${row.recipeCode}`}
                  onClick={() => onOpen(row.id)}
                >
                  {openLabel}
                </button>
                {onEdit && row.status !== "INACTIVE" && row.status !== "PENDING_APPROVAL" && (
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary sila-btn--sm"
                    aria-label={`Edit ${row.recipeCode}`}
                    onClick={() => onEdit(row.id)}
                  >
                    Edit
                  </button>
                )}
                {onDelete && row.status === "DRAFT" && (
                  <button
                    type="button"
                    className="sila-btn sila-btn--danger sila-btn--sm"
                    aria-label={`Delete Draft ${row.recipeCode}`}
                    onClick={() => onDelete(row)}
                  >
                    Delete Draft
                  </button>
                )}
                {onDelete && row.status === "INACTIVE" && (
                  <button
                    type="button"
                    className="sila-btn sila-btn--danger sila-btn--sm"
                    aria-label={`Delete permanently ${row.recipeCode}`}
                    onClick={() => onDelete(row)}
                    data-testid="recipe-table-delete-inactive"
                  >
                    Delete permanently
                  </button>
                )}
              </div>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  </div>
);

export default SilaRecipeTable;
