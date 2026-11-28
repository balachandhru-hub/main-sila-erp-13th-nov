import React, { useCallback, useEffect, useState } from "react";
import { EmptyState, Loader, toastService } from "@vosox/shared-ui";
import {
  listCulinaryRecipeDrafts,
  type CulinaryRecipeDraftSummary,
} from "../../../../api/silaMe/silaCulinaryRecipesApi";
import {
  getPhase1CMaterialHandover,
  type SilaCulinaryPhase1CHandover,
} from "../../../../api/silaMe/silaCulinaryMaterialsApi";
import {
  assistMaterialRequest,
  bulkPrepareMaterialRequests,
  createMaterialRequest,
  decideMaterialRequest,
  executeMaterialCreation,
  exportMaterialRequestsReport,
  getMaterialRequestDashboard,
  listMaterialRequests,
  resubmitMaterialRequest,
  submitMaterialRequest,
  updateMaterialRequest,
  verifyManualErpMaterial,
  type MaterialRequestType,
  type SilaCulinaryMaterialRequest,
  type SilaCulinaryMaterialRequestAssistResult,
  type SilaCulinaryMaterialRequestDashboard,
} from "../../../../api/silaMe/silaCulinaryLifecycleApi";
import { triggerBlobDownload } from "./culinaryRecipePlannerHelpers";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import { CulinaryKpis } from "./CulinaryWorkspaceKit";
import "./CulinarySuite.css";

interface Props {
  selectedDraftId?: string | null;
  onSelectDraftId?: (draftId: string | null) => void;
  onOpenWorkspace: (target: CulinaryWorkspaceKind) => void;
}

const REQUEST_TYPES: { value: MaterialRequestType; label: string }[] = [
  { value: "NEW_MATERIAL", label: "New Material" },
  { value: "MATERIAL_EXTENSION", label: "Material Extension (Plant/Org)" },
  { value: "MATERIAL_REACTIVATION", label: "Material Reactivation" },
  { value: "UOM_MAINTENANCE", label: "UOM Maintenance" },
  { value: "SPECIFICATION_CORRECTION", label: "Specification Correction" },
  { value: "CLASSIFICATION_MAINTENANCE", label: "Classification Maintenance" },
  { value: "MANUAL_REVIEW", label: "Manual Master Data Review" },
];

const CulinaryMaterialRequestsWorkspace: React.FC<Props> = ({
  selectedDraftId,
  onSelectDraftId,
  onOpenWorkspace,
}) => {
  const [requests, setRequests] = useState<SilaCulinaryMaterialRequest[]>([]);
  const [dashboard, setDashboard] = useState<SilaCulinaryMaterialRequestDashboard | null>(null);
  const [drafts, setDrafts] = useState<CulinaryRecipeDraftSummary[]>([]);
  const [handover, setHandover] = useState<SilaCulinaryPhase1CHandover | null>(null);
  const [selectedReq, setSelectedReq] = useState<SilaCulinaryMaterialRequest | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const [statusFilter, setStatusFilter] = useState("ALL");
  const [typeFilter, setTypeFilter] = useState("ALL");
  const showRequests = (status: string) => {
    setStatusFilter(status);
    setTypeFilter("ALL");
    setSearch("");
    window.setTimeout(() => document.getElementById("mrq-request-list")?.scrollIntoView({ behavior: "smooth", block: "start" }), 50);
  };
  const [search, setSearch] = useState("");
  const [activeDraftId, setActiveDraftId] = useState<string>(selectedDraftId ?? "");

  // Create/Edit form state
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [requestType, setRequestType] = useState<string>("NEW_MATERIAL");
  const [proposedDescription, setProposedDescription] = useState("");
  const [materialGroup, setMaterialGroup] = useState("CULINARY");
  const [baseUom, setBaseUom] = useState("KG");
  const [purchasingUom, setPurchasingUom] = useState("CASE");
  const [convNum, setConvNum] = useState<string>("");
  const [convDen, setConvDen] = useState<string>("1");
  const [productSpec, setProductSpec] = useState("");
  const [storageReq, setStorageReq] = useState("");
  const [allergens, setAllergens] = useState("");
  const [justification, setJustification] = useState("");
  const [targetMatCode, setTargetMatCode] = useState("");
  const [assistResult, setAssistResult] = useState<SilaCulinaryMaterialRequestAssistResult | null>(null);

  // Action modal/inline inputs
  const [decisionComment, setDecisionComment] = useState("");
  const [erpVerifyCode, setErpVerifyCode] = useState("");

  const loadAll = useCallback(async () => {
    setLoading(true);
    try {
      const [reqs, dash, drs] = await Promise.all([
        listMaterialRequests({
          status: statusFilter === "ALL" ? undefined : statusFilter,
          requestType: typeFilter === "ALL" ? undefined : typeFilter,
          search: search.trim() || undefined,
        }),
        getMaterialRequestDashboard(),
        listCulinaryRecipeDrafts(),
      ]);
      setRequests(reqs);
      setDashboard(dash);
      setDrafts(drs);
      if (selectedReq) {
        const updated = reqs.find((r) => r.id === selectedReq.id) ?? null;
        setSelectedReq(updated);
      } else if (reqs.length > 0) {
        setSelectedReq(reqs[0]);
      }
      if (activeDraftId) {
        try {
          const ho = await getPhase1CMaterialHandover(activeDraftId);
          setHandover(ho);
        } catch {
          setHandover(null);
        }
      } else {
        setHandover(null);
      }
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to load material requests.");
    } finally {
      setLoading(false);
    }
  }, [statusFilter, typeFilter, search, activeDraftId]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  const handleSelectDraft = (id: string) => {
    setActiveDraftId(id);
    onSelectDraftId?.(id || null);
  };

  const handleBulkPrepareFromPhase1B = async () => {
    if (!handover || handover.materialRequestDrafts.length === 0) {
      toastService.info("No missing material items from Phase 1B handover for this draft.");
      return;
    }
    setBusy(true);
    try {
      const created = await bulkPrepareMaterialRequests({
        consolidateEquivalent: true,
        items: handover.materialRequestDrafts.map((m) => ({
          draftId: handover.draftId,
          ingredientTempRef: m.ingredientTempRef,
          ingredientDescription: m.proposedDescription,
          requiredQuantity: m.requiredQuantity,
          recipeUom: m.recipeUom,
        })),
      });
      toastService.success(`Prepared ${created.length} consolidated material request(s) from Phase 1B.`);
      await loadAll();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Bulk preparation failed.");
    } finally {
      setBusy(false);
    }
  };

  const handleAiAssist = async () => {
    if (!proposedDescription.trim()) {
      toastService.warning("Enter an ingredient description first.");
      return;
    }
    setBusy(true);
    try {
      const res = await assistMaterialRequest({
        ingredientDescription: proposedDescription.trim(),
        recipeUom: baseUom,
        draftId: activeDraftId || undefined,
      });
      setAssistResult(res);
      setRequestType(res.recommendedRequestType);
      setProposedDescription(res.proposedDescription);
      setMaterialGroup(res.proposedMaterialGroup || "CULINARY");
      setBaseUom(res.proposedBaseUom || baseUom);
      setProductSpec(res.productSpecification);
      setStorageReq(res.storageRequirement);
      setAllergens(res.potentialAllergens.join(", "));
      setJustification(res.businessJustification);
      toastService.success(`AI Assist suggested ${res.recommendedRequestType} (${res.potentialDuplicates.length} duplicate candidate(s)).`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "AI Assist failed.");
    } finally {
      setBusy(false);
    }
  };

  const handleSaveRequest = async (e: React.FormEvent) => {
    e.preventDefault();
    setBusy(true);
    try {
      const payload = {
        requestType,
        proposedDescription: proposedDescription.trim(),
        materialGroup: materialGroup.trim() || undefined,
        baseUom: baseUom.trim() || "KG",
        purchasingUom: purchasingUom.trim() || undefined,
        conversionNumerator: convNum ? Number(convNum) : undefined,
        conversionDenominator: convDen ? Number(convDen) : undefined,
        productSpecification: productSpec.trim() || undefined,
        storageRequirements: storageReq.trim() || undefined,
        potentialAllergens: allergens.trim() || undefined,
        businessJustification: justification.trim() || undefined,
        targetMaterialCode: targetMatCode.trim() || undefined,
        dataOrigin: assistResult ? "AI_SUGGESTED" : "CHEF_ENTERED",
        allowDuplicateOverride: false,
      };
      const saved = editingId
        ? await updateMaterialRequest(editingId, payload)
        : await createMaterialRequest(payload);
      toastService.success(`Saved Material Request ${saved.requestReference}.`);
      setShowForm(false);
      setEditingId(null);
      setSelectedReq(saved);
      await loadAll();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to save material request.");
    } finally {
      setBusy(false);
    }
  };

  const runAction = async (fn: () => Promise<SilaCulinaryMaterialRequest>, okMsg: string) => {
    setBusy(true);
    try {
      const updated = await fn();
      setSelectedReq(updated);
      toastService.success(okMsg);
      await loadAll();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Action failed.");
    } finally {
      setBusy(false);
    }
  };

  const handleExport = async (format: "XLSX" | "CSV" | "PDF") => {
    try {
      const { blob, fileName } = await exportMaterialRequestsReport(
        format,
        statusFilter === "ALL" ? undefined : statusFilter
      );
      triggerBlobDownload(blob, fileName);
      toastService.success(`Exported ${fileName}`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Export failed.");
    }
  };

  return (
    <div className="cs2-shell" data-testid="culinary-material-requests-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Material Creation &amp; Master Data Requests</h2>
          <p className="cs2-subtitle">
            Controlled material creation, plant extensions, reactivations, UOM conversions, duplicate screening,
            master data approvals, ERP/standalone synchronization, and automatic recipe draft reconnection.
          </p>
        </div>
        <div className="cs2-actions-row">
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onOpenWorkspace("materials")}>
            Material Intelligence
          </button>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onOpenWorkspace("food-cost")}>
            Food Cost Studio
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--sm sila-btn--primary"
            onClick={() => {
              setEditingId(null);
              setAssistResult(null);
              setProposedDescription("");
              setShowForm(true);
            }}
          >
            + New Material Request
          </button>
        </div>
      </div>

      {/* Dashboard KPI Strip */}
      {dashboard && (
        <CulinaryKpis
          ariaLabel="Material request metrics"
          testId="mrq-dashboard-kpis"
          columns={6}
          items={[
            { id: "mrq-total", label: "Total Requests", value: dashboard.totalRequests, hint: dashboard.isErpIntegratedTenant ? "ERP Integrated (Manual SAP Creation)" : "Standalone SILA Master", tone: "blue", drillLabel: "Show every material request", selected: statusFilter === "ALL", onClick: () => showRequests("ALL") },
            { id: "mrq-draft", label: "Draft Requests", value: dashboard.draftRequests, tone: "lavender", drillLabel: "Show draft requests", selected: statusFilter === "DRAFT", onClick: () => showRequests("DRAFT") },
            { id: "mrq-pending", label: "Pending Approval", value: dashboard.pendingApprovalRequests, tone: "amber", drillLabel: "Show requests pending approval", selected: statusFilter === "PENDING_APPROVAL", onClick: () => showRequests("PENDING_APPROVAL") },
            { id: "mrq-returned", label: "Returned", value: dashboard.returnedRequests, tone: "rose", drillLabel: "Show requests returned for correction", selected: statusFilter === "RETURNED_FOR_CORRECTION", onClick: () => showRequests("RETURNED_FOR_CORRECTION") },
            { id: "mrq-approved", label: "Approved", value: dashboard.approvedRequests, tone: "sky", drillLabel: "Show approved requests", selected: statusFilter === "APPROVED", onClick: () => showRequests("APPROVED") },
            { id: "mrq-creation", label: "Creation / Sync Pending", value: dashboard.creationPendingRequests + dashboard.synchronizationPendingRequests, tone: "indigo", drillLabel: "Show requests pending creation in the ERP", selected: statusFilter === "CREATION_PENDING", onClick: () => showRequests("CREATION_PENDING") },
            { id: "mrq-resolved", label: "Available / Resolved", value: dashboard.resolvedRequests, hint: dashboard.averageResolutionHours != null ? `Avg ${dashboard.averageResolutionHours}h` : undefined, tone: "green", drillLabel: "Show requests whose material is now available", selected: statusFilter === "MATERIAL_AVAILABLE", onClick: () => showRequests("MATERIAL_AVAILABLE") },
            { id: "mrq-blocked", label: "Blocked Recipe Drafts", value: dashboard.recipesBlockedByMissingMaterials, tone: "rose", drillLabel: "Show recipe drafts waiting for materials", onClick: () => document.getElementById("mrq-handover")?.scrollIntoView({ behavior: "smooth", block: "start" }) },
          ]}
        />
      )}

      {/* Phase 1B Handover Consolidation Strip */}
      <section className="cs2-card" id="mrq-handover">
        <div className="cs2-card__head">
          <h3 className="cs2-card__title">Connect Waiting AI Recipe Drafts (Material Mapping → Requests)</h3>
          <div className="cs2-actions-row">
            <select
              className="sila-select"
              value={activeDraftId}
              onChange={(e) => handleSelectDraft(e.target.value)}
              aria-label="Select Recipe Draft for Material Handover"
            >
              <option value="">All recipe drafts</option>
              {drafts.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.draftReference} — {d.recipeName}
                </option>
              ))}
            </select>
            {handover && handover.materialRequestDrafts.length > 0 && (
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--primary"
                disabled={busy}
                onClick={handleBulkPrepareFromPhase1B}
              >
                Bulk Prepare ({handover.materialRequestDrafts.length} Missing)
              </button>
            )}
          </div>
        </div>
        {handover && (
          <div className="cs2-empty-note">
            Draft <strong>{handover.draftReference}</strong> ({handover.recipeName}) · Missing material items:{" "}
            <strong>{handover.materialRequestDrafts.length}</strong> · Outstanding issues:{" "}
            <strong>{handover.outstandingValidationIssues.length}</strong>
          </div>
        )}
      </section>

      {/* New / Edit Material Request Modal Form */}
      {showForm && (
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">{editingId ? "Edit Material Request" : "Prepare Material Request"}</h3>
            <div className="cs2-actions-row">
              <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={busy} onClick={handleAiAssist}>
                AI Assist &amp; Duplicate Screen
              </button>
              <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => setShowForm(false)}>
                Close
              </button>
            </div>
          </div>
          <form onSubmit={handleSaveRequest} className="cs2-form-grid">
            <div className="sila-field">
              <label className="sila-label">Request Type</label>
              <select className="sila-select" value={requestType} onChange={(e) => setRequestType(e.target.value)}>
                {REQUEST_TYPES.map((t) => (
                  <option key={t.value} value={t.value}>{t.label}</option>
                ))}
              </select>
            </div>
            <div className="sila-field">
              <label className="sila-label">Proposed Material Description *</label>
              <input className="sila-input" required value={proposedDescription} onChange={(e) => setProposedDescription(e.target.value)} placeholder="e.g. KASURI METHI DRIED LEAVES" />
            </div>
            <div className="sila-field">
              <label className="sila-label">Material Group</label>
              <input className="sila-input" value={materialGroup} onChange={(e) => setMaterialGroup(e.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Base UOM *</label>
              <input className="sila-input" required value={baseUom} onChange={(e) => setBaseUom(e.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Purchasing / Alternate UOM</label>
              <input className="sila-input" value={purchasingUom} onChange={(e) => setPurchasingUom(e.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Conversion (Num / Den)</label>
              <div style={{ display: "flex", gap: 6 }}>
                <input className="sila-input" type="number" step="0.001" placeholder="Num" value={convNum} onChange={(e) => setConvNum(e.target.value)} />
                <input className="sila-input" type="number" step="0.001" placeholder="Den" value={convDen} onChange={(e) => setConvDen(e.target.value)} />
              </div>
            </div>
            <div className="sila-field">
              <label className="sila-label">Product Specification</label>
              <input className="sila-input" value={productSpec} onChange={(e) => setProductSpec(e.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Storage &amp; Allergens</label>
              <input className="sila-input" value={storageReq} onChange={(e) => setStorageReq(e.target.value)} placeholder="Storage requirement" />
            </div>
            <div className="sila-field">
              <label className="sila-label">Target Material Code (Extension/Reactivation/UOM)</label>
              <input className="sila-input" value={targetMatCode} onChange={(e) => setTargetMatCode(e.target.value)} />
            </div>
            <div className="sila-field" style={{ gridColumn: "1 / -1" }}>
              <label className="sila-label">Business Justification</label>
              <input className="sila-input" value={justification} onChange={(e) => setJustification(e.target.value)} />
            </div>
            {assistResult && assistResult.potentialDuplicates.length > 0 && (
              <div style={{ gridColumn: "1 / -1" }} className="cs2-banner">
                <div>
                  <strong>Duplicate Screening Evidence ({assistResult.potentialDuplicates.length} candidate(s)):</strong>{" "}
                  {assistResult.potentialDuplicates.map((d) => `${d.materialCode} ${d.description} (${Math.round(d.similarity * 100)}%)`).join(" · ")}
                </div>
              </div>
            )}
            <div style={{ gridColumn: "1 / -1", display: "flex", gap: 8 }}>
              <button type="submit" className="sila-btn sila-btn--sm sila-btn--primary" disabled={busy}>
                Save Material Request
              </button>
            </div>
          </form>
        </section>
      )}

      {/* Filters & Export */}
      <section className="cs2-card" id="mrq-request-list">
        <div className="cs2-card__head">
          <div className="cs2-actions-row">
            <select className="sila-select" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="ALL">All Statuses</option>
              <option value="DRAFT">Draft</option>
              <option value="PENDING_APPROVAL">Pending Approval</option>
              <option value="RETURNED_FOR_CORRECTION">Returned</option>
              <option value="APPROVED">Approved</option>
              <option value="CREATION_PENDING">Creation Pending (ERP)</option>
              <option value="MATERIAL_AVAILABLE">Material Available</option>
              <option value="REJECTED">Rejected</option>
            </select>
            <select className="sila-select" value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
              <option value="ALL">All Request Types</option>
              {REQUEST_TYPES.map((t) => (
                <option key={t.value} value={t.value}>{t.label}</option>
              ))}
            </select>
            <input
              className="sila-input"
              placeholder="Search ref / description / material code"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div className="cs2-actions-row">
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => handleExport("XLSX")}>Export Excel</button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => handleExport("CSV")}>CSV</button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => handleExport("PDF")}>PDF</button>
          </div>
        </div>

        {loading ? (
          <Loader size={22} message="Loading material requests..." />
        ) : requests.length === 0 ? (
          <EmptyState
            title="No material requests found"
            description="Prepare a material request manually or consolidate missing ingredients from an AI recipe draft."
          />
        ) : (
          <div className="cs2-grid-2">
            <div>
              <table className="sila-table" style={{ width: "100%" }}>
                <thead>
                  <tr>
                    <th>Ref</th>
                    <th>Description</th>
                    <th>Type</th>
                    <th>Status</th>
                    <th>Recipes</th>
                  </tr>
                </thead>
                <tbody>
                  {requests.map((r) => (
                    <tr
                      key={r.id}
                      onClick={() => setSelectedReq(r)}
                      style={{ cursor: "pointer", background: selectedReq?.id === r.id ? "#eaf5ff" : undefined }}
                    >
                      <td><code>{r.requestReference}</code></td>
                      <td>
                        <strong>{r.proposedDescription}</strong>
                        <div className="cs2-empty-note">{r.baseUom} · {r.resultingMaterialCode ?? "Pending"}</div>
                      </td>
                      <td>{r.requestType}</td>
                      <td><span className="cs2-badge cs2-badge--indigo">{r.status}</span></td>
                      <td>{r.links.length}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Selected Request Detail & Controlled Actions */}
            {selectedReq && (
              <div style={{ border: "1px solid #e2e8f0", borderRadius: 10, padding: 14 }}>
                <div className="cs2-card__head">
                  <div>
                    <h4 style={{ margin: 0 }}>{selectedReq.requestReference} — {selectedReq.proposedDescription}</h4>
                    <div className="cs2-empty-note">
                      Rev {selectedReq.revisionNumber} · Origin: {selectedReq.dataOrigin} · Execution: {selectedReq.executionStatus}
                    </div>
                  </div>
                  <span className="cs2-badge cs2-badge--live">{selectedReq.status}</span>
                </div>

                <div style={{ fontSize: 13, lineHeight: 1.7, marginBottom: 10 }}>
                  <div><strong>Type:</strong> {selectedReq.requestType} · <strong>Base UOM:</strong> {selectedReq.baseUom}</div>
                  <div><strong>Spec:</strong> {selectedReq.productSpecification || "—"}</div>
                  <div><strong>Justification:</strong> {selectedReq.businessJustification || "—"}</div>
                  <div><strong>Resulting Material:</strong> <code>{selectedReq.resultingMaterialCode || "Not yet assigned"}</code></div>
                </div>

                {selectedReq.links.length > 0 && (
                  <div style={{ marginBottom: 10 }}>
                    <strong style={{ fontSize: 12 }}>Linked Recipe Drafts ({selectedReq.links.length}):</strong>
                    <ul style={{ margin: "4px 0 0", paddingLeft: 16, fontSize: 12 }}>
                      {selectedReq.links.map((l) => (
                        <li key={l.id}>
                          <code>{l.draftReference}</code> {l.recipeName} — {l.requiredQuantity} {l.recipeUom} (
                          {l.reconnected ? "Reconnected for chef confirmation" : "Waiting"})
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {selectedReq.duplicateCandidates.length > 0 && (
                  <div className="cs2-banner" style={{ marginBottom: 10, fontSize: 12 }}>
                    <div>
                      <strong>Duplicate Screening:</strong>{" "}
                      {selectedReq.duplicateCandidates.map((d) => `${d.materialCode} (${d.reason})`).join(", ")}
                    </div>
                  </div>
                )}

                <div style={{ display: "flex", flexWrap: "wrap", gap: 6, marginBottom: 10 }}>
                  {(selectedReq.status === "DRAFT" || selectedReq.status === "RETURNED_FOR_CORRECTION") && (
                    <>
                      <button
                        type="button"
                        className="sila-btn sila-btn--sm sila-btn--primary"
                        disabled={busy}
                        onClick={() =>
                          selectedReq.status === "RETURNED_FOR_CORRECTION"
                            ? runAction(() => resubmitMaterialRequest(selectedReq.id), "Resubmitted for approval.")
                            : runAction(() => submitMaterialRequest(selectedReq.id), "Submitted for Master Data approval.")
                        }
                      >
                        {selectedReq.status === "RETURNED_FOR_CORRECTION" ? "Resubmit" : "Submit for Approval"}
                      </button>
                      <button
                        type="button"
                        className="sila-btn sila-btn--sm sila-btn--secondary"
                        onClick={() => {
                          setEditingId(selectedReq.id);
                          setRequestType(selectedReq.requestType);
                          setProposedDescription(selectedReq.proposedDescription);
                          setBaseUom(selectedReq.baseUom);
                          setProductSpec(selectedReq.productSpecification ?? "");
                          setJustification(selectedReq.businessJustification ?? "");
                          setShowForm(true);
                        }}
                      >
                        Edit
                      </button>
                    </>
                  )}

                  {selectedReq.status === "PENDING_APPROVAL" && (
                    <div style={{ width: "100%", display: "flex", flexDirection: "column", gap: 6 }}>
                      <input
                        className="sila-input"
                        placeholder="Reviewer comment / return reason"
                        value={decisionComment}
                        onChange={(e) => setDecisionComment(e.target.value)}
                      />
                      <div style={{ display: "flex", gap: 6 }}>
                        <button
                          type="button"
                          className="sila-btn sila-btn--sm sila-btn--primary"
                          disabled={busy}
                          onClick={() => runAction(() => decideMaterialRequest(selectedReq.id, "APPROVE", decisionComment), "Request approved.")}
                        >
                          Approve
                        </button>
                        <button
                          type="button"
                          className="sila-btn sila-btn--sm sila-btn--secondary"
                          disabled={busy}
                          onClick={() => runAction(() => decideMaterialRequest(selectedReq.id, "RETURN", decisionComment), "Returned for correction.")}
                        >
                          Return
                        </button>
                        <button
                          type="button"
                          className="sila-btn sila-btn--sm sila-btn--secondary"
                          disabled={busy}
                          onClick={() => runAction(() => decideMaterialRequest(selectedReq.id, "REJECT", decisionComment), "Request rejected.")}
                        >
                          Reject
                        </button>
                      </div>
                    </div>
                  )}

                  {selectedReq.status === "APPROVED" && (
                    <button
                      type="button"
                      className="sila-btn sila-btn--sm sila-btn--primary"
                      disabled={busy}
                      onClick={() =>
                        runAction(
                          () => executeMaterialCreation(selectedReq.id, `${selectedReq.id}-rev-${selectedReq.revisionNumber}`),
                          "Material execution processed."
                        )
                      }
                    >
                      {selectedReq.isErpIntegratedTenant && !selectedReq.hasErpCreationApi
                        ? "Send to Manual ERP Creation Queue"
                        : "Execute Standalone Material Creation"}
                    </button>
                  )}

                  {selectedReq.status === "CREATION_PENDING" && (
                    <div style={{ width: "100%", display: "flex", gap: 6 }}>
                      <input
                        className="sila-input"
                        placeholder="Enter authoritative SAP Material Code (e.g. 100042)"
                        value={erpVerifyCode}
                        onChange={(e) => setErpVerifyCode(e.target.value)}
                      />
                      <button
                        type="button"
                        className="sila-btn sila-btn--sm sila-btn--primary"
                        disabled={busy}
                        onClick={() =>
                          runAction(
                            () => verifyManualErpMaterial(selectedReq.id, erpVerifyCode, "Verified via Master Data"),
                            "Verified ERP material & reconnected linked drafts."
                          )
                        }
                      >
                        Verify &amp; Reconnect
                      </button>
                    </div>
                  )}
                </div>

                {selectedReq.history.length > 0 && (
                  <div style={{ borderTop: "1px solid #e2e8f0", paddingTop: 8 }}>
                    <strong style={{ fontSize: 12 }}>Audit History:</strong>
                    <ul style={{ margin: "4px 0 0", paddingLeft: 16, fontSize: 12, maxHeight: 140, overflowY: "auto" }}>
                      {selectedReq.history.map((h) => (
                        <li key={h.id}>
                          <strong>{h.action}</strong> ({h.newStatus}) by {h.actorName || "System"} — {h.comment || ""}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            )}
          </div>
        )}
      </section>
    </div>
  );
};

export default CulinaryMaterialRequestsWorkspace;
