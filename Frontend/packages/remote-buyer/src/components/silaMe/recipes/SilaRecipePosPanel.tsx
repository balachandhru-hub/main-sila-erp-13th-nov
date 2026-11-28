import React, { useCallback, useEffect, useMemo, useState } from "react";
import { AlertTriangle, CheckCircle2, ExternalLink, RefreshCw, Search } from "lucide-react";
import { EmptyState, Loader, Pagination } from "@vosox/shared-ui";
import { getPosTransactions, type SilaPosTransaction } from "../../../api/silaMe/silaPosApi";
import { formatCost } from "./recipeFormat";
import { StudioFilterBar } from "../studio";

export type SilaRecipePosPanelMode = "failed-pos" | "posted-consumption";

interface SilaRecipePosPanelProps {
  mode: SilaRecipePosPanelMode;
  onBackToRecipes: () => void;
  onOpenPosStudio?: () => void;
  onOpenRecipe?: (recipeId: string) => void;
}

const PAGE_SIZE = 25;
const POSTED_SCAN_LIMIT = 200;

const utcDay = (value?: string | null): string => {
  if (!value) return "";
  const time = new Date(value);
  return Number.isNaN(time.getTime()) ? "" : time.toISOString().slice(0, 10);
};

const dateText = (value?: string | null): string => {
  if (!value) return "—";
  const time = new Date(value);
  return Number.isNaN(time.getTime()) ? value : time.toLocaleDateString();
};

/**
 * Drill-down for the Failed POS Sales and Posted Consumption KPI boxes: the actual POS sale lines
 * behind the number, with the failure step/message or the inventory consumption transaction.
 */
const SilaRecipePosPanel: React.FC<SilaRecipePosPanelProps> = ({
  mode,
  onBackToRecipes,
  onOpenPosStudio,
  onOpenRecipe,
}) => {
  const failedMode = mode === "failed-pos";
  const [rows, setRows] = useState<SilaPosTransaction[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setPage(1);
    setSearch("");
    setAppliedSearch("");
  }, [mode]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setAppliedSearch(search.trim());
      setPage(1);
    }, 300);
    return () => window.clearTimeout(timer);
  }, [search]);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      if (failedMode) {
        const result = await getPosTransactions({
          status: "FAILED",
          search: appliedSearch || undefined,
          index: page - 1,
          limit: PAGE_SIZE,
        });
        setRows(result.items);
        setTotal(result.total);
      } else {
        const result = await getPosTransactions({
          status: "POSTED",
          search: appliedSearch || undefined,
          index: 0,
          limit: POSTED_SCAN_LIMIT,
        });
        const today = new Date().toISOString().slice(0, 10);
        const todays = result.items.filter(
          (row) => Boolean(row.consumptionTransactionNumber) && utcDay(row.postedOn ?? row.dateCreated) === today,
        );
        setRows(todays);
        setTotal(todays.length);
      }
    } catch (err: unknown) {
      setRows([]);
      setTotal(0);
      setError(err instanceof Error ? err.message : "Could not load the POS transactions.");
    } finally {
      setLoading(false);
    }
  }, [failedMode, appliedSearch, page]);

  useEffect(() => {
    void load();
  }, [load]);

  const visibleRows = useMemo(
    () => (failedMode ? rows : rows.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)),
    [failedMode, rows, page],
  );
  const hasNext = page * PAGE_SIZE < total;

  return (
    <section className="srec-pos-panel" data-testid={failedMode ? "recipe-failed-pos-panel" : "recipe-posted-consumption-panel"}>
      <StudioFilterBar
        ariaLabel={failedMode ? "Failed POS sales filters" : "Posted consumption filters"}
        testId="recipe-pos-filter-toolbar"
        left={
          <>
            <div className="sila-studio-search" style={{ minWidth: "240px", flex: "1 1 280px" }}>
              <Search size={14} strokeWidth={1.9} className="sila-studio-search__icon" aria-hidden="true" />
              <input
                className="sila-studio-input"
                type="search"
                aria-label="Search POS transactions"
                placeholder="Search POS code, transaction, recipe..."
                value={search}
                onChange={(event) => setSearch(event.target.value)}
              />
            </div>
            <span className={`sila-studio-badge ${failedMode ? "sila-studio-badge--error" : "sila-studio-badge--success"}`}>
              {failedMode ? <AlertTriangle size={12} aria-hidden="true" /> : <CheckCircle2 size={12} aria-hidden="true" />}
              <span>
                {failedMode ? `${total} failed POS sale line${total === 1 ? "" : "s"}` : `${total} consumption posting${total === 1 ? "" : "s"} today`}
              </span>
            </span>
          </>
        }
        right={
          <div style={{ display: "inline-flex", gap: "0.4rem", flexWrap: "wrap" }}>
            <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={() => void load()} disabled={loading}>
              <RefreshCw size={14} strokeWidth={1.9} aria-hidden="true" />
              <span>Refresh</span>
            </button>
            {onOpenPosStudio && (
              <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={onOpenPosStudio}>
                <ExternalLink size={14} strokeWidth={1.9} aria-hidden="true" />
                <span>{failedMode ? "Resolve in POS Studio" : "Open POS Studio"}</span>
              </button>
            )}
            <button type="button" className="sila-studio-btn sila-studio-btn--primary" onClick={onBackToRecipes}>
              <span>Back to Recipes</span>
            </button>
          </div>
        }
      />

      {loading ? (
        <Loader size={24} message={failedMode ? "Loading failed POS sales..." : "Loading posted consumption..."} />
      ) : error ? (
        <EmptyState
          variant="error"
          title="Couldn't load POS transactions"
          description={error}
          action={
            <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={() => void load()}>
              Try again
            </button>
          }
        />
      ) : visibleRows.length === 0 ? (
        <EmptyState
          title={failedMode ? "No failed POS sales" : "No consumption posted today"}
          description={
            failedMode
              ? "Every POS sale line has matched a recipe, deducted stock and posted successfully."
              : "No recipe consumption has been posted from POS sales today."
          }
        />
      ) : (
        <>
          <section className="sila-studio-card">
            <div className="sila-studio-table-wrap">
              <table className="sila-studio-table" aria-label={failedMode ? "Failed POS sale lines" : "Posted consumption"}>
                <thead>
                  <tr>
                    <th scope="col">Business date</th>
                    <th scope="col">Sale / line</th>
                    <th scope="col">Outlet</th>
                    <th scope="col">POS code</th>
                    <th scope="col">Recipe</th>
                    <th scope="col" className="is-num">Qty</th>
                    <th scope="col" className="is-num">Amount</th>
                    {failedMode ? (
                      <>
                        <th scope="col">Failed step</th>
                        <th scope="col">Reason</th>
                      </>
                    ) : (
                      <>
                        <th scope="col">Consumption txn</th>
                        <th scope="col">ERP posting</th>
                      </>
                    )}
                  </tr>
                </thead>
                <tbody>
                  {visibleRows.map((row) => (
                    <tr key={row.id}>
                      <td>{dateText(row.businessDate)}</td>
                      <td>
                        {row.sourceTransactionId}/{row.lineNumber}
                      </td>
                      <td>{row.outletLocationName || row.outletCode || "—"}</td>
                      <td>{row.posCode || "—"}</td>
                      <td>
                        {row.recipeId && onOpenRecipe ? (
                          <button type="button" className="sila-studio-link" onClick={() => onOpenRecipe(row.recipeId as string)}>
                            {row.recipeName || row.recipeCode || row.menuItem || "Open recipe"}
                          </button>
                        ) : (
                          row.recipeName || row.menuItem || "Unmapped"
                        )}
                      </td>
                      <td className="is-num">{row.quantitySold}</td>
                      <td className="is-num">{row.amount == null ? "—" : formatCost(row.amount, row.currency)}</td>
                      {failedMode ? (
                        <>
                          <td>
                            <span className="sila-studio-badge sila-studio-badge--error">
                              {(row.failureCode || row.failedStep || "FAILED").replace(/_/g, " ")}
                            </span>
                          </td>
                          <td>{row.failureMessage || row.step1Message || row.step2Message || "—"}</td>
                        </>
                      ) : (
                        <>
                          <td>{row.consumptionTransactionNumber || "—"}</td>
                          <td>{row.erpPostingStatus || row.step2Status || "—"}</td>
                        </>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
          <Pagination
            page={page}
            hasNext={hasNext}
            onPrevious={() => setPage((current) => Math.max(1, current - 1))}
            onNext={() => setPage((current) => current + 1)}
            disabled={loading}
          />
        </>
      )}
    </section>
  );
};

export default SilaRecipePosPanel;
