import React, { useEffect, useMemo, useState } from "react";
import { EmptyState, Loader } from "@vosox/shared-ui";
import { getPosTransactions, type SilaPosTransaction } from "../../../api/silaMe/silaPosApi";
import { errorText, posLabel } from "./posFormat";

interface SilaPosStudioExceptionsProps {
  onOpenApi: () => void;
  onOpenTracker: () => void;
}

type ExceptionGroup =
  | "POS Mapping Missing"
  | "Recipe Mapping Missing"
  | "Outlet Mapping Missing"
  | "Plant/SLoc Missing"
  | "Invalid UOM"
  | "Cost Pending"
  | "Duplicate Transaction"
  | "Insufficient Stock"
  | "SAP Posting Failure"
  | "SILA Reconciliation Failure"
  | "Other";

const classify = (tx: SilaPosTransaction): ExceptionGroup => {
  const text = `${tx.status || ""} ${tx.erpPostingStatus || ""} ${tx.failureMessage || ""} ${tx.failureCode || ""} ${tx.step1Message || ""} ${tx.step2Message || ""}`.toLowerCase();
  if (text.includes("duplicate")) return "Duplicate Transaction";
  if (text.includes("unmapped") || text.includes("pos code") || text.includes("item mapping") || text.includes("match_failed")) return "POS Mapping Missing";
  if (text.includes("recipe")) return "Recipe Mapping Missing";
  if (text.includes("outlet")) return "Outlet Mapping Missing";
  if (text.includes("plant") || text.includes("sloc") || text.includes("storage")) return "Plant/SLoc Missing";
  if (text.includes("uom")) return "Invalid UOM";
  if (text.includes("cost pending") || text.includes("unit cost")) return "Cost Pending";
  if (text.includes("stock") || text.includes("insufficient")) return "Insufficient Stock";
  if (text.includes("sap") || (text.includes("erp") && text.includes("fail")) || tx.failureCode === "ERP_FAILED") return "SAP Posting Failure";
  if (tx.erpPostingStatus === "UNKNOWN" || tx.failureCode === "ERP_UNKNOWN" || text.includes("reconcil")) return "SILA Reconciliation Failure";
  return "Other";
};

const actionFor = (group: ExceptionGroup): { action: string; role: string; retry: string } => {
  switch (group) {
    case "POS Mapping Missing":
    case "Recipe Mapping Missing":
      return { action: "Map POS code to recipe under Item mapping", role: "Cost controller", retry: "Retry after mapping" };
    case "Outlet Mapping Missing":
    case "Plant/SLoc Missing":
      return { action: "Complete outlet → plant/SLoc mapping", role: "Outlet admin", retry: "Retry after mapping" };
    case "Invalid UOM":
      return { action: "Align selling UOM with recipe", role: "Recipe owner", retry: "Re-upload or reprocess" };
    case "Cost Pending":
      return { action: "Maintain material unit cost", role: "Cost controller", retry: "Preview again after cost" };
    case "Duplicate Transaction":
      return { action: "Confirm original sale / void linkage", role: "Outlet operator", retry: "Not auto-retry" };
    case "Insufficient Stock":
      return { action: "Adjust stock or investigate variance", role: "Storekeeper", retry: "Retry when stock available" };
    case "SAP Posting Failure":
      return { action: "Inspect ERP posting result; do not duplicate post", role: "Integration admin", retry: "Eligible retry via tracker" };
    case "SILA Reconciliation Failure":
      return { action: "Reconcile ERP unknown under tracker / ERP postings", role: "Controller", retry: "Reconcile only" };
    default:
      return { action: "Open Transaction Tracker for lineage", role: "Operator", retry: "Case by case" };
  }
};

/** Live exception groups from failed / ERP-unknown POS transactions. */
const SilaPosStudioExceptions: React.FC<SilaPosStudioExceptionsProps> = ({ onOpenApi, onOpenTracker }) => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [rows, setRows] = useState<SilaPosTransaction[]>([]);
  const [search, setSearch] = useState("");
  const [groupFilter, setGroupFilter] = useState<ExceptionGroup | "ALL">("ALL");

  useEffect(() => {
    let active = true;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const [failed, unknown] = await Promise.all([
          getPosTransactions({ status: "FAILED", index: 0, limit: 100 }),
          getPosTransactions({ postingStatus: "UNKNOWN", index: 0, limit: 100 }),
        ]);
        if (!active) return;
        const map = new Map<string, SilaPosTransaction>();
        for (const row of [...(failed.items || []), ...(unknown.items || [])]) {
          map.set(row.id, row);
        }
        setRows([...map.values()]);
      } catch (err: unknown) {
        if (!active) return;
        setError(errorText(err, "Could not load POS exceptions."));
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => { active = false; };
  }, []);

  const grouped = useMemo(() => {
    const counts = new Map<ExceptionGroup, number>();
    for (const row of rows) {
      const key = classify(row);
      counts.set(key, (counts.get(key) || 0) + 1);
    }
    return counts;
  }, [rows]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return rows.filter((row) => {
      const group = classify(row);
      if (groupFilter !== "ALL" && group !== groupFilter) return false;
      if (!q) return true;
      const hay = [
        row.sourceTransactionId,
        row.posCode,
        row.outletCode,
        row.failureMessage,
        row.failureCode,
        row.status,
        row.erpPostingStatus,
        group,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return hay.includes(q);
    });
  }, [rows, search, groupFilter]);

  if (loading) return <Loader size={20} message="Loading exceptions..." />;
  if (error) {
    return <EmptyState variant="error" title="Exceptions unavailable" description={error} />;
  }

  return (
    <section className="pstudio-panel pstudio-panel--amber" data-testid="pstudio-exceptions">
      <h2 className="pstudio-panel__title">Exceptions & attention required</h2>
      <p className="pstudio-panel__hint">
        Live failed / ERP-unknown POS lines. Resolve mapping through existing POS API Integration workflows — not a parallel mapping engine.
      </p>

      <div className="pstudio-kpis">
        {[...grouped.entries()].map(([group, count]) => (
          <button
            key={group}
            type="button"
            className="pstudio-kpi pstudio-kpi--amber"
            style={{ textAlign: "left", cursor: "pointer" }}
            onClick={() => setGroupFilter(groupFilter === group ? "ALL" : group)}
            aria-pressed={groupFilter === group}
          >
            <span>{group}</span>
            <strong>{count}</strong>
          </button>
        ))}
        {grouped.size === 0 && (
          <div className="pstudio-kpi pstudio-kpi--mint"><span>Open exceptions</span><strong>0</strong></div>
        )}
      </div>

      <div className="srec-actions" style={{ flexWrap: "wrap" }}>
        <input
          className="sila-input"
          style={{ minWidth: "14rem", maxWidth: "22rem" }}
          placeholder="Search exceptions…"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          aria-label="Search exceptions"
        />
        <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => setGroupFilter("ALL")}>Clear filter</button>
        <button type="button" className="sila-btn sila-btn--primary" onClick={onOpenTracker}>Open tracker</button>
        <button type="button" className="sila-btn sila-btn--secondary" onClick={onOpenApi}>Open mappings</button>
      </div>

      {filtered.length === 0 ? (
        <div className="pstudio-empty">No matching exceptions. Failed and ERP-unknown lines appear here when present.</div>
      ) : (
        <div className="sila-table-wrap">
          <table className="sila-table">
            <thead>
              <tr>
                <th scope="col">Group</th>
                <th scope="col">POS transaction</th>
                <th scope="col">Error reason</th>
                <th scope="col">Required action</th>
                <th scope="col">Role</th>
                <th scope="col">Retry</th>
                <th scope="col">Status</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((row) => {
                const group = classify(row);
                const meta = actionFor(group);
                return (
                  <tr key={row.id}>
                    <td>{group}</td>
                    <td>
                      {row.sourceTransactionId || row.id}
                      <span className="srec-sub">{row.posCode || "—"} · {row.outletCode || "—"}</span>
                    </td>
                    <td className="srec-message">{row.failureMessage || row.step2Message || row.step1Message || "—"}</td>
                    <td>{meta.action}</td>
                    <td>{meta.role}</td>
                    <td>{row.canReprocess ? meta.retry : "Not eligible"}</td>
                    <td>
                      <span className="sila-badge">{posLabel(row.status)}</span>
                      {row.erpPostingStatus && <span className="srec-sub">{posLabel(row.erpPostingStatus)}</span>}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
};

export default SilaPosStudioExceptions;
