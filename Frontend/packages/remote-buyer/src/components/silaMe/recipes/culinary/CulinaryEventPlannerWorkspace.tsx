import React, { useCallback, useEffect, useMemo, useState } from "react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { toastService } from "@vosox/shared-ui";
import {
  cancelCulinaryEvent,
  createCulinaryEvent,
  createCulinaryEventRevision,
  createCulinaryEventScenario,
  createEventFromTemplate,
  createMissingRecipeDraftFromMenuLine,
  decideCulinaryEventApproval,
  deleteCulinaryEventSession,
  downloadEventReportResult,
  duplicateCulinaryEvent,
  exportCulinaryEventReport,
  generateCulinaryAiMenu,
  getCulinaryCustomerMenuPreview,
  getCulinaryEvent,
  getCulinaryEventAllergenMatrix,
  getCulinaryEventMaterialPreview,
  getCulinaryEventPhase2BHandover,
  getCulinaryEventReadiness,
  getEventDashboard,
  listCulinaryEventRevisions,
  listCulinaryEvents,
  listCulinaryEventTemplates,
  optimizeCulinaryEventMenu,
  removeCulinaryMenuLine,
  saveCulinaryEventTemplate,
  searchPublishedCulinaryRecipes,
  selectCulinaryEventScenario,
  simulateCulinaryEvent,
  submitCulinaryEventForApproval,
  updateCulinaryEvent,
  upsertCulinaryEventSession,
  upsertCulinaryMenuLine,
  verifyCulinaryEventDietary,
  type SilaCulinaryAiMenuGenerateResult,
  type SilaCulinaryAllergenMatrixRow,
  type SilaCulinaryCustomerMenuPreview,
  type SilaCulinaryEvent,
  type SilaCulinaryEventDashboard,
  type SilaCulinaryEventReadiness,
  type SilaCulinaryEventRevision,
  type SilaCulinaryEventSimulationResult,
  type SilaCulinaryEventTemplate,
  type SilaCulinaryMaterialPreviewLine,
  type SilaCulinaryMenuOptimizeResult,
  type SilaCulinaryPhase2BHandover,
  type SilaCulinaryPublishedRecipeSearchItem,
  type SilaCulinaryUpsertEventRequest,
} from "../../../../api/silaMe/silaCulinaryEventMenuApi";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import "./CulinarySuite.css";

interface CulinaryEventPlannerWorkspaceProps {
  onOpenWorkspace: (target: CulinaryWorkspaceKind) => void;
}

type TopView = "dashboard" | "calendar" | "workspace";
type EventTab =
  | "wizard"
  | "sessions"
  | "guests"
  | "menu"
  | "ai"
  | "stock"
  | "simulate"
  | "customer"
  | "governance";

const EVENT_TYPES = [
  "Corporate Gala",
  "Wedding Banquet",
  "Conference",
  "Executive Dinner",
  "Buffet Brunch",
  "Ramadan Iftar",
  "Suhoor",
  "Cocktail Reception",
  "Outside Catering",
  "Custom Event",
];

const SERVICE_STYLES = [
  "BUFFET",
  "PLATED",
  "FAMILY_STYLE",
  "LIVE_STATION",
  "COCKTAIL_RECEPTION",
  "COFFEE_BREAK",
  "CUSTOM",
];

const WIZARD_STEPS = [
  "1. Event Details",
  "2. Property & Kitchen",
  "3. Sessions & Timing",
  "4. Guest Planning",
  "5. Service Style",
  "6. Menu Selection",
  "7. Dietary & Allergens",
  "8. Budget & Costing",
  "9. Readiness Check",
  "10. Submit Approval",
];

const REPORT_CODES = [
  { code: "EVENT_SUMMARY", label: "Event Summary" },
  { code: "MENU_COSTING", label: "Menu Costing" },
  { code: "GUEST_PLANNING", label: "Guest Planning" },
  { code: "ALLERGEN_MATRIX", label: "Allergen Matrix" },
  { code: "MATERIAL_PREVIEW", label: "Material Preview" },
  { code: "APPROVAL_PACKAGE", label: "Approval Package" },
];

const CulinaryEventPlannerWorkspace: React.FC<CulinaryEventPlannerWorkspaceProps> = ({
  onOpenWorkspace,
}) => {
  const [topView, setTopView] = useState<TopView>("dashboard");
  const [calendarMode, setCalendarMode] = useState<"month" | "week" | "agenda">("agenda");
  const [eventTab, setEventTab] = useState<EventTab>("wizard");

  const [dashboard, setDashboard] = useState<SilaCulinaryEventDashboard | null>(null);
  const [events, setEvents] = useState<SilaCulinaryEvent[]>([]);
  const [templates, setTemplates] = useState<SilaCulinaryEventTemplate[]>([]);
  const [selectedEvent, setSelectedEvent] = useState<SilaCulinaryEvent | null>(null);
  const [statusFilter, setStatusFilter] = useState("");
  const [searchFilter, setSearchFilter] = useState("");
  const [loading, setLoading] = useState(false);

  // Create / Edit Wizard State
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [form, setForm] = useState<SilaCulinaryUpsertEventRequest>({
    name: "",
    eventType: "Corporate Gala",
    venueName: "Grand Ballroom",
    startDateUtc: new Date(Date.now() + 14 * 86400000).toISOString().slice(0, 10),
    endDateUtc: new Date(Date.now() + 14 * 86400000).toISOString().slice(0, 10),
    startTime: "18:00",
    endTime: "23:00",
    timeZoneId: "Asia/Dubai",
    guaranteedGuests: 500,
    expectedGuests: 500,
    additionalAllowancePercent: 5,
    billablePolicy: "GUARANTEED_ONLY",
    adultsCount: 500,
    childrenCount: 0,
    vipCount: 20,
    staffMealsCount: 15,
    currency: "AED",
    netPricePerGuest: 350,
    targetFoodCostPercent: 28,
    targetBeverageCostPercent: 22,
    serviceStyle: "BUFFET",
    wizardStepCompleted: 1,
    dietaryRequirements: [],
  });
  const [changeReason, setChangeReason] = useState("");

  // Session quick add
  const [sessName, setSessName] = useState("Gala Dinner");
  const [sessDay, setSessDay] = useState("1");
  const [sessStart, setSessStart] = useState("19:00");
  const [sessEnd, setSessEnd] = useState("22:30");
  const [sessGuests, setSessGuests] = useState("500");
  const [sessStyle, setSessStyle] = useState("BUFFET");

  // Menu search
  const [recipeQ, setRecipeQ] = useState("");
  const [recipeCourse, setRecipeCourse] = useState("Main Courses");
  const [publishedRecipes, setPublishedRecipes] = useState<SilaCulinaryPublishedRecipeSearchItem[]>([]);

  // AI Generation & MILP Optimization
  const [aiBrief, setAiBrief] = useState("International corporate gala buffet for 500 guests with halal main courses and gluten-free dessert option");
  const [aiStrategy, setAiStrategy] = useState("BALANCED");
  const [aiCourses, setAiCourses] = useState("Starters, Main Courses, Desserts, Cold Beverages");
  const [aiDietary, setAiDietary] = useState("HALAL");
  const [aiExclAllergens, setAiExclAllergens] = useState("");
  const [aiGenResult, setAiGenResult] = useState<SilaCulinaryAiMenuGenerateResult | null>(null);
  const [optMaxBudget, setOptMaxBudget] = useState("");
  const [optResult, setOptResult] = useState<SilaCulinaryMenuOptimizeResult | null>(null);
  const [aiBusy, setAiBusy] = useState(false);

  // Allergen, Material Preview, Simulation, Customer Preview, Readiness, Revisions, Handover
  const [allergens, setAllergens] = useState<SilaCulinaryAllergenMatrixRow[]>([]);
  const [materialPreview, setMaterialPreview] = useState<SilaCulinaryMaterialPreviewLine[]>([]);
  const [simGuests, setSimGuests] = useState(500);
  const [simAllowance, setSimAllowance] = useState(5);
  const [simPrice, setSimPrice] = useState(350);
  const [simResult, setSimResult] = useState<SilaCulinaryEventSimulationResult | null>(null);
  const [scenarioName, setScenarioName] = useState("");
  const [customerPreview, setCustomerPreview] = useState<SilaCulinaryCustomerMenuPreview | null>(null);
  const [readiness, setReadiness] = useState<SilaCulinaryEventReadiness | null>(null);
  const [revisions, setRevisions] = useState<SilaCulinaryEventRevision[]>([]);
  const [handover, setHandover] = useState<SilaCulinaryPhase2BHandover | null>(null);
  const [decisionComment, setDecisionComment] = useState("");
  const [templateName, setTemplateName] = useState("");

  const syncFormFromEvent = (ev: SilaCulinaryEvent) => {
    setForm({
      name: ev.name,
      description: ev.description,
      eventType: ev.eventType,
      eventCategory: ev.eventCategory,
      customerName: ev.customerName,
      bookingReference: ev.bookingReference,
      eventCoordinator: ev.eventCoordinator,
      responsibleChef: ev.responsibleChef,
      executiveChef: ev.executiveChef,
      banquetManager: ev.banquetManager,
      propertyId: ev.propertyId,
      companyCode: ev.companyCode,
      plantCode: ev.plantCode,
      venueName: ev.venueName,
      outletLocationId: ev.outletLocationId,
      kitchenLocationId: ev.kitchenLocationId,
      storageLocationCodes: ev.storageLocationCodes,
      startDateUtc: ev.startDateUtc.slice(0, 10),
      endDateUtc: ev.endDateUtc.slice(0, 10),
      startTime: ev.startTime,
      endTime: ev.endTime,
      timeZoneId: ev.timeZoneId || "Asia/Dubai",
      guaranteedGuests: ev.guaranteedGuests,
      expectedGuests: ev.expectedGuests,
      maxCapacity: ev.maxCapacity,
      additionalAllowancePercent: ev.additionalAllowancePercent,
      billablePolicy: ev.billablePolicy,
      adultsCount: ev.adultsCount,
      childrenCount: ev.childrenCount,
      vipCount: ev.vipCount,
      staffMealsCount: ev.staffMealsCount,
      currency: ev.currency || "AED",
      netPricePerGuest: ev.netPricePerGuest ?? 350,
      fixedPackagePrice: ev.fixedPackagePrice,
      targetFoodCostPercent: ev.targetFoodCostPercent,
      targetBeverageCostPercent: ev.targetBeverageCostPercent,
      combinedMenuBudget: ev.combinedMenuBudget,
      approvedBudget: ev.approvedBudget,
      serviceStyle: ev.serviceStyle,
      wizardStepCompleted: ev.wizardStepCompleted,
      dietaryRequirements: ev.dietaryRequirements ?? [],
    });
    setSimGuests(ev.guaranteedGuests);
    setSimAllowance(ev.additionalAllowancePercent);
    setSimPrice(ev.netPricePerGuest ?? 350);
  };

  const refreshAll = useCallback(async (selectId?: string) => {
    setLoading(true);
    try {
      const [dash, evList, tplList] = await Promise.all([
        getEventDashboard(),
        listCulinaryEvents({ status: statusFilter || undefined, search: searchFilter || undefined }),
        listCulinaryEventTemplates(),
      ]);
      setDashboard(dash);
      setEvents(evList);
      setTemplates(tplList);

      const targetId = selectId || selectedEvent?.id || evList[0]?.id;
      if (targetId) {
        const detail = await getCulinaryEvent(targetId);
        setSelectedEvent(detail);
        syncFormFromEvent(detail);
      }
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to load events.");
    } finally {
      setLoading(false);
    }
  }, [statusFilter, searchFilter, selectedEvent?.id]);

  useEffect(() => {
    refreshAll();
  }, []);

  const openEvent = async (eventId: string) => {
    try {
      const detail = await getCulinaryEvent(eventId);
      setSelectedEvent(detail);
      syncFormFromEvent(detail);
      setTopView("workspace");
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to open event.");
    }
  };

  const loadTabData = async (tab: EventTab, ev: SilaCulinaryEvent) => {
    try {
      if (tab === "menu") {
        const items = await searchPublishedCulinaryRecipes({ query: recipeQ || undefined });
        setPublishedRecipes(items);
      } else if (tab === "guests") {
        setAllergens(await getCulinaryEventAllergenMatrix(ev.id));
      } else if (tab === "stock") {
        setMaterialPreview(await getCulinaryEventMaterialPreview(ev.id));
      } else if (tab === "customer") {
        setCustomerPreview(await getCulinaryCustomerMenuPreview(ev.id));
      } else if (tab === "governance") {
        const [rd, revs, ho] = await Promise.all([
          getCulinaryEventReadiness(ev.id),
          listCulinaryEventRevisions(ev.id),
          getCulinaryEventPhase2BHandover(ev.id),
        ]);
        setReadiness(rd);
        setRevisions(revs);
        setHandover(ho);
      }
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to load tab details.");
    }
  };

  const handleTabSelect = (tab: EventTab) => {
    setEventTab(tab);
    if (selectedEvent) {
      loadTabData(tab, selectedEvent);
    }
  };

  const handleCreateEvent = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const created = await createCulinaryEvent({
        ...form,
        startDateUtc: new Date(form.startDateUtc).toISOString(),
        endDateUtc: new Date(form.endDateUtc).toISOString(),
      });
      toastService.success(`Event ${created.eventCode} created.`);
      setShowCreateModal(false);
      await refreshAll(created.id);
      setTopView("workspace");
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to create event.");
    }
  };

  const handleSaveEvent = async (nextStep?: number) => {
    if (!selectedEvent) return;
    try {
      const updated = await updateCulinaryEvent(selectedEvent.id, {
        ...form,
        wizardStepCompleted: nextStep ?? form.wizardStepCompleted,
        changeReason: changeReason || undefined,
        startDateUtc: new Date(form.startDateUtc).toISOString(),
        endDateUtc: new Date(form.endDateUtc).toISOString(),
      });
      setSelectedEvent(updated);
      syncFormFromEvent(updated);
      setChangeReason("");
      toastService.success(`Saved ${updated.eventCode} (rev ${updated.revisionNumber}).`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to update event.");
    }
  };

  const handleRunSimulation = async () => {
    if (!selectedEvent) return;
    try {
      const res = await simulateCulinaryEvent(selectedEvent.id, {
        guaranteedGuests: simGuests,
        expectedGuests: simGuests,
        additionalAllowancePercent: simAllowance,
        netPricePerGuest: simPrice,
        lineOverrides: [],
      });
      setSimResult(res);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Simulation failed.");
    }
  };

  const calendarGroups = useMemo(() => {
    const sorted = [...events].sort((a, b) => a.startDateUtc.localeCompare(b.startDateUtc));
    if (calendarMode === "week") {
      const cutoff = Date.now() + 7 * 86400000;
      return sorted.filter((ev) => new Date(ev.startDateUtc).getTime() <= cutoff);
    }
    return sorted;
  }, [events, calendarMode]);

  return (
    <div className="cs2-shell" data-testid="culinary-events-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">AI Event Planner</h2>
          <p className="cs2-subtitle">
            Hospitality Event Lifecycle: Calendar, Multi-Day Sessions, Guest Uptake, Published Recipe Selection,
            AI Event Planning, Optimization, Dietary &amp; Allergen Review, Read-Only Stock Preview, Approvals &amp; Material Requirements Handover.
          </p>
        </div>
        <div className="cs2-actions-row">
          <button type="button" className={`sila-btn sila-btn--sm ${topView === "dashboard" ? "sila-btn--primary" : "sila-btn--secondary"}`} onClick={() => setTopView("dashboard")}>
            Dashboard
          </button>
          <button type="button" className={`sila-btn sila-btn--sm ${topView === "calendar" ? "sila-btn--primary" : "sila-btn--secondary"}`} onClick={() => setTopView("calendar")}>
            Event Calendar
          </button>
          <button type="button" className={`sila-btn sila-btn--sm ${topView === "workspace" ? "sila-btn--primary" : "sila-btn--secondary"}`} onClick={() => setTopView("workspace")}>
            Event Workspace
          </button>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => setShowCreateModal((v) => !v)} data-testid="new-event-btn">
            {showCreateModal ? "Close New Event" : "+ New Event"}
          </button>
        </div>
      </div>

      {showCreateModal && (
        <form className="cs2-card" onSubmit={handleCreateEvent} data-testid="event-create-form">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Create New Event</h3>
            {templates.length > 0 && (
              <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
                <span style={{ fontSize: 12, color: "#64748b" }}>Quick Template:</span>
                {templates.slice(0, 3).map((tpl) => (
                  <button
                    key={tpl.id}
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--secondary"
                    onClick={async () => {
                      const created = await createEventFromTemplate(tpl.id, {
                        ...form,
                        name: form.name || `${tpl.name} Event`,
                        startDateUtc: new Date(form.startDateUtc).toISOString(),
                        endDateUtc: new Date(form.endDateUtc).toISOString(),
                      });
                      setShowCreateModal(false);
                      await refreshAll(created.id);
                      setTopView("workspace");
                      toastService.success(`Created ${created.eventCode} from template ${tpl.name}.`);
                    }}
                  >
                    {tpl.name}
                  </button>
                ))}
              </div>
            )}
          </div>
          <div className="cs2-form-grid">
            <div className="sila-field">
              <label className="sila-label">Event Name *</label>
              <input className="sila-input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} placeholder="Grand Corporate Gala Dinner" required />
            </div>
            <div className="sila-field">
              <label className="sila-label">Event Type</label>
              <select className="sila-select" value={form.eventType} onChange={(e) => setForm({ ...form, eventType: e.target.value })}>
                {EVENT_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
              </select>
            </div>
            <div className="sila-field">
              <label className="sila-label">Venue</label>
              <input className="sila-input" value={form.venueName ?? ""} onChange={(e) => setForm({ ...form, venueName: e.target.value })} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Service Style</label>
              <select className="sila-select" value={form.serviceStyle} onChange={(e) => setForm({ ...form, serviceStyle: e.target.value })}>
                {SERVICE_STYLES.map((s) => <option key={s} value={s}>{s}</option>)}
              </select>
            </div>
            <div className="sila-field">
              <label className="sila-label">Start Date</label>
              <input type="date" className="sila-input" value={form.startDateUtc} onChange={(e) => setForm({ ...form, startDateUtc: e.target.value, endDateUtc: e.target.value })} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Guaranteed Guests</label>
              <input type="number" className="sila-input" value={form.guaranteedGuests} onChange={(e) => setForm({ ...form, guaranteedGuests: Number(e.target.value) || 0, expectedGuests: Number(e.target.value) || 0 })} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Allowance %</label>
              <input type="number" className="sila-input" value={form.additionalAllowancePercent} onChange={(e) => setForm({ ...form, additionalAllowancePercent: Number(e.target.value) || 0 })} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Net Price / Guest (AED)</label>
              <input type="number" className="sila-input" value={form.netPricePerGuest} onChange={(e) => setForm({ ...form, netPricePerGuest: Number(e.target.value) || 0 })} />
            </div>
          </div>
          <div style={{ marginTop: 10, display: "flex", justifyContent: "flex-end", gap: 8 }}>
            <button type="submit" className="sila-btn sila-btn--sm sila-btn--primary">Create Event</button>
          </div>
        </form>
      )}

      {topView === "dashboard" && (
        <>
          <div className="cs2-kpi-grid">
            <div className="cs2-kpi cs2-kpi--accent">
              <span className="cs2-kpi__label">Upcoming Events</span>
              <span className="cs2-kpi__value">{dashboard?.upcomingEvents ?? 0}</span>
              <span className="cs2-kpi__hint">This week: {dashboard?.eventsThisWeek ?? 0} · Month: {dashboard?.eventsThisMonth ?? 0}</span>
            </div>
            <div className="cs2-kpi cs2-kpi--indigo">
              <span className="cs2-kpi__label">Total Planned Guests</span>
              <span className="cs2-kpi__value">{(dashboard?.totalPlannedGuests ?? 0).toLocaleString()}</span>
              <span className="cs2-kpi__hint">Draft: {dashboard?.draftEvents ?? 0} · Pending: {dashboard?.awaitingApprovalEvents ?? 0}</span>
            </div>
            <div className="cs2-kpi cs2-kpi--emerald">
              <span className="cs2-kpi__label">Approved &amp; MRP Ready</span>
              <span className="cs2-kpi__value">{dashboard?.approvedEvents ?? 0} / {dashboard?.readyForMrpEvents ?? 0}</span>
              <span className="cs2-kpi__hint">Ready for Event MRP</span>
            </div>
            <div className="cs2-kpi cs2-kpi--warn">
              <span className="cs2-kpi__label">Attention Required</span>
              <span className="cs2-kpi__value">{(dashboard?.aboveBudgetEvents ?? 0) + (dashboard?.dietaryReviewPendingEvents ?? 0)}</span>
              <span className="cs2-kpi__hint">Over budget: {dashboard?.aboveBudgetEvents ?? 0} · Dietary pending: {dashboard?.dietaryReviewPendingEvents ?? 0}</span>
            </div>
          </div>

          <div className="cs2-grid-2">
            <section className="cs2-card">
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">Events Directory</h3>
                <div style={{ display: "flex", gap: 6 }}>
                  <input className="sila-input" style={{ width: 170 }} placeholder="Search code/name..." value={searchFilter} onChange={(e) => setSearchFilter(e.target.value)} />
                  <select className="sila-select" style={{ width: 130 }} value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                    <option value="">All Statuses</option>
                    <option value="DRAFT">Draft</option>
                    <option value="PENDING_APPROVAL">Pending</option>
                    <option value="APPROVED">Approved</option>
                    <option value="CANCELLED">Cancelled</option>
                  </select>
                  <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => refreshAll()} disabled={loading}>Filter</button>
                </div>
              </div>
              <div style={{ display: "grid", gap: 8, maxHeight: 340, overflowY: "auto" }}>
                {events.length === 0 ? (
                  <div style={{ color: "#64748b", fontSize: 13 }}>No events found. Click "+ New Event" to start planning.</div>
                ) : (
                  events.map((ev) => (
                    <div key={ev.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: 10, border: "1px solid #e2e8f0", borderRadius: 10 }}>
                      <div>
                        <div style={{ fontWeight: 600 }}>
                          <code>{ev.eventCode}</code> · {ev.name}
                        </div>
                        <div style={{ fontSize: 12, color: "#475569" }}>
                          {ev.startDateUtc.slice(0, 10)} · {ev.venueName || "Venue TBD"} · {ev.planningGuests} planned guests · {ev.serviceStyle}
                        </div>
                      </div>
                      <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
                        {ev.hasVenueConflict && <span className="cs2-badge cs2-badge--warn">Venue Conflict</span>}
                        <span className={`cs2-badge ${ev.status === "APPROVED" ? "cs2-badge--live" : "cs2-badge--indigo"}`}>{ev.status}</span>
                        <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => openEvent(ev.id)}>Open</button>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </section>

            <section className="cs2-card">
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">Budget vs Estimated Menu Cost</h3>
              </div>
              <div style={{ width: "100%", height: 320 }}>
                {dashboard && dashboard.budgetVsCost.length > 0 ? (
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={dashboard.budgetVsCost}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                      <XAxis dataKey="eventCode" />
                      <YAxis />
                      <Tooltip />
                      <Legend />
                      <Bar dataKey="budget" name="Budget" fill="#087bb5" radius={[4, 4, 0, 0]} />
                      <Bar dataKey="estimatedCost" name="Est. Menu Cost" fill="#0ea5e9" radius={[4, 4, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                ) : (
                  <div style={{ color: "#64748b", fontSize: 13 }}>Create events with menus to view live Budget vs Cost comparison.</div>
                )}
              </div>
            </section>
          </div>
        </>
      )}

      {topView === "calendar" && (
        <section className="cs2-card" data-testid="event-calendar-view">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Event Calendar &amp; Venue Conflict Monitor</h3>
            <div style={{ display: "flex", gap: 6 }}>
              {(["month", "week", "agenda"] as const).map((m) => (
                <button key={m} type="button" className={`sila-btn sila-btn--sm ${calendarMode === m ? "sila-btn--primary" : "sila-btn--secondary"}`} onClick={() => setCalendarMode(m)}>
                  {m.toUpperCase()}
                </button>
              ))}
            </div>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            {calendarGroups.map((ev) => (
              <div key={ev.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: 12, border: ev.hasVenueConflict ? "1px solid #f59e0b" : "1px solid #e2e8f0", borderRadius: 10 }}>
                <div>
                  <div style={{ fontWeight: 600 }}>
                    {ev.startDateUtc.slice(0, 10)} ({ev.startTime}–{ev.endTime} {ev.timeZoneId}) · {ev.name} (<code>{ev.eventCode}</code>)
                  </div>
                  <div style={{ fontSize: 12, color: "#475569" }}>
                    Venue: {ev.venueName || "Unassigned"} · Guests: {ev.planningGuests} · Sessions: {ev.sessions.length}
                    {ev.hasVenueConflict && ` · Conflicts with: ${ev.conflictingEventCodes.join(", ")}`}
                  </div>
                </div>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => openEvent(ev.id)}>Open</button>
              </div>
            ))}
          </div>
        </section>
      )}

      {topView === "workspace" && (
        <>
          {!selectedEvent ? (
            <div className="cs2-card">Select an event from the Dashboard or click "+ New Event".</div>
          ) : (
            <>
              <div className="cs2-banner cs2-banner--phase" role="status">
                <div>
                  <strong>
                    {selectedEvent.eventCode} — {selectedEvent.name} (Rev {selectedEvent.revisionNumber}
                    {selectedEvent.approvedRevisionNumber ? ` · Approved Baseline v${selectedEvent.approvedRevisionNumber}` : ""})
                  </strong>
                  <div>
                    {selectedEvent.startDateUtc.slice(0, 10)} · {selectedEvent.venueName || "Venue TBD"} · Guaranteed {selectedEvent.guaranteedGuests} → Planning {selectedEvent.planningGuests} guests ·{" "}
                    {selectedEvent.costVisible
                      ? `Est. Cost: ${selectedEvent.currency} ${(selectedEvent.totalEstimatedMenuCost ?? 0).toFixed(2)} (${(selectedEvent.menuCostPercent ?? 0).toFixed(1)}%)`
                      : "Cost restricted"}
                  </div>
                </div>
                <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
                  <span className={`cs2-badge ${selectedEvent.status === "APPROVED" ? "cs2-badge--live" : "cs2-badge--indigo"}`}>{selectedEvent.status}</span>
                  <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => {
                    const dup = await duplicateCulinaryEvent(selectedEvent.id);
                    toastService.success(`Duplicated as ${dup.eventCode}.`);
                    await refreshAll(dup.id);
                  }}>Duplicate</button>
                  <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onOpenWorkspace("mrp")}>
                    Open MRP Studio
                  </button>
                </div>
              </div>

              <div className="cs2-nav-tabs" role="tablist">
                {([
                  ["wizard", "10-Step Wizard"],
                  ["sessions", `Sessions (${selectedEvent.sessions.length})`],
                  ["guests", "Guests & Dietary"],
                  ["menu", `Menu (${selectedEvent.primaryMenu?.lines.length ?? 0})`],
                  ["ai", "AI & MILP Optimizer"],
                  ["stock", "Stock Preview"],
                  ["simulate", "Cost & Scenarios"],
                  ["customer", "Customer Menu"],
                  ["governance", "Readiness, Approvals & 2B"],
                ] as const).map(([id, label]) => (
                  <button
                    key={id}
                    type="button"
                    role="tab"
                    aria-selected={eventTab === id}
                    className={`cs2-nav-tab${eventTab === id ? " is-active" : ""}`}
                    onClick={() => handleTabSelect(id)}
                  >
                    {label}
                  </button>
                ))}
              </div>

              {eventTab === "wizard" && (
                <section className="cs2-card">
                  <div className="cs2-card__head">
                    <h3 className="cs2-card__title">Guided Event Setup Wizard (Step {form.wizardStepCompleted} / 10)</h3>
                    <div style={{ display: "flex", gap: 4, flexWrap: "wrap" }}>
                      {WIZARD_STEPS.map((label, idx) => (
                        <button
                          key={label}
                          type="button"
                          className={`sila-btn sila-btn--sm ${form.wizardStepCompleted === idx + 1 ? "sila-btn--primary" : "sila-btn--secondary"}`}
                          onClick={() => handleSaveEvent(idx + 1)}
                        >
                          {label}
                        </button>
                      ))}
                    </div>
                  </div>
                  <div className="cs2-form-grid">
                    <div className="sila-field">
                      <label className="sila-label">Event Name</label>
                      <input className="sila-input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
                    </div>
                    <div className="sila-field">
                      <label className="sila-label">Venue</label>
                      <input className="sila-input" value={form.venueName ?? ""} onChange={(e) => setForm({ ...form, venueName: e.target.value })} />
                    </div>
                    <div className="sila-field">
                      <label className="sila-label">Plant Code</label>
                      <input className="sila-input" value={form.plantCode ?? ""} onChange={(e) => setForm({ ...form, plantCode: e.target.value })} placeholder="e.g. 1060" />
                    </div>
                    <div className="sila-field">
                      <label className="sila-label">Responsible Chef</label>
                      <input className="sila-input" value={form.responsibleChef ?? ""} onChange={(e) => setForm({ ...form, responsibleChef: e.target.value })} />
                    </div>
                    <div className="sila-field">
                      <label className="sila-label">Target Food Cost %</label>
                      <input type="number" className="sila-input" value={form.targetFoodCostPercent} onChange={(e) => setForm({ ...form, targetFoodCostPercent: Number(e.target.value) || 28 })} />
                    </div>
                    <div className="sila-field">
                      <label className="sila-label">Combined Menu Budget ({form.currency})</label>
                      <input type="number" className="sila-input" value={form.combinedMenuBudget ?? ""} onChange={(e) => setForm({ ...form, combinedMenuBudget: e.target.value ? Number(e.target.value) : undefined })} />
                    </div>
                  </div>
                  <div style={{ marginTop: 12, display: "flex", gap: 8, justifyContent: "space-between", flexWrap: "wrap" }}>
                    <div style={{ display: "flex", gap: 6 }}>
                      <input className="sila-input" placeholder="Save as template name..." value={templateName} onChange={(e) => setTemplateName(e.target.value)} />
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => {
                        if (!templateName.trim()) return;
                        await saveCulinaryEventTemplate(selectedEvent.id, templateName.trim());
                        setTemplateName("");
                        toastService.success("Saved event template.");
                      }}>Save Template</button>
                    </div>
                    <div style={{ display: "flex", gap: 6 }}>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => handleSaveEvent()}>Save Changes</button>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--danger" onClick={async () => {
                        const res = await cancelCulinaryEvent(selectedEvent.id, "Cancelled from Event Planner");
                        setSelectedEvent(res);
                        toastService.info(`Event ${res.eventCode} cancelled.`);
                      }}>Cancel Event</button>
                    </div>
                  </div>
                </section>
              )}

              {eventTab === "sessions" && (
                <section className="cs2-card">
                  <div className="cs2-card__head">
                    <h3 className="cs2-card__title">Multi-Day &amp; Multi-Session Schedule</h3>
                  </div>
                  <div className="cs2-form-grid" style={{ marginBottom: 12 }}>
                    <div className="sila-field"><label className="sila-label">Session Name</label><input className="sila-input" value={sessName} onChange={(e) => setSessName(e.target.value)} /></div>
                    <div className="sila-field"><label className="sila-label">Day #</label><input type="number" className="sila-input" value={sessDay} onChange={(e) => setSessDay(e.target.value)} /></div>
                    <div className="sila-field"><label className="sila-label">Start</label><input className="sila-input" value={sessStart} onChange={(e) => setSessStart(e.target.value)} /></div>
                    <div className="sila-field"><label className="sila-label">End</label><input className="sila-input" value={sessEnd} onChange={(e) => setSessEnd(e.target.value)} /></div>
                    <div className="sila-field"><label className="sila-label">Guests</label><input type="number" className="sila-input" value={sessGuests} onChange={(e) => setSessGuests(e.target.value)} /></div>
                    <div className="sila-field"><label className="sila-label">Service Style</label>
                      <select className="sila-select" value={sessStyle} onChange={(e) => setSessStyle(e.target.value)}>
                        {SERVICE_STYLES.map((s) => <option key={s} value={s}>{s}</option>)}
                      </select>
                    </div>
                    <div className="sila-field" style={{ display: "flex", alignItems: "flex-end" }}>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={async () => {
                        await upsertCulinaryEventSession(selectedEvent.id, {
                          dayNumber: Number(sessDay) || 1,
                          sessionDateUtc: selectedEvent.startDateUtc,
                          startTime: sessStart,
                          endTime: sessEnd,
                          sessionName: sessName,
                          venueName: selectedEvent.venueName,
                          serviceStyle: sessStyle,
                          guestCount: Number(sessGuests) || selectedEvent.guaranteedGuests,
                          sortOrder: selectedEvent.sessions.length + 1,
                        });
                        await openEvent(selectedEvent.id);
                        toastService.success("Session saved.");
                      }}>+ Add Session</button>
                    </div>
                  </div>
                  <div style={{ display: "grid", gap: 8 }}>
                    {selectedEvent.sessions.map((s) => (
                      <div key={s.id} style={{ display: "flex", justifyContent: "space-between", padding: 10, border: "1px solid #e2e8f0", borderRadius: 8 }}>
                        <div>
                          <strong>Day {s.dayNumber} · {s.sessionName}</strong> (<code>{s.sessionCode}</code>) · {s.startTime}–{s.endTime} · {s.serviceStyle} · {s.guestCount} guests
                        </div>
                        <button type="button" className="sila-btn sila-btn--sm sila-btn--danger" onClick={async () => {
                          await deleteCulinaryEventSession(selectedEvent.id, s.id);
                          await openEvent(selectedEvent.id);
                        }}>Delete</button>
                      </div>
                    ))}
                  </div>
                </section>
              )}

              {eventTab === "guests" && (
                <section className="cs2-card">
                  <div className="cs2-card__head">
                    <h3 className="cs2-card__title">Guest Planning &amp; Allergen Matrix</h3>
                    <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={async () => {
                      const updated = await verifyCulinaryEventDietary(selectedEvent.id, !selectedEvent.dietaryReviewVerified);
                      setSelectedEvent(updated);
                      toastService.success(updated.dietaryReviewVerified ? "Dietary review verified." : "Dietary verification cleared.");
                    }}>
                      {selectedEvent.dietaryReviewVerified ? "Dietary Verified (Click to Undo)" : "Verify Dietary & Allergens"}
                    </button>
                  </div>
                  <div className="cs2-form-grid" style={{ marginBottom: 14 }}>
                    <div className="sila-field"><label className="sila-label">Guaranteed Guests</label><input type="number" className="sila-input" value={form.guaranteedGuests} onChange={(e) => setForm({ ...form, guaranteedGuests: Number(e.target.value) || 0 })} /></div>
                    <div className="sila-field"><label className="sila-label">Allowance %</label><input type="number" className="sila-input" value={form.additionalAllowancePercent} onChange={(e) => setForm({ ...form, additionalAllowancePercent: Number(e.target.value) || 0 })} /></div>
                    <div className="sila-field"><label className="sila-label">Adults</label><input type="number" className="sila-input" value={form.adultsCount} onChange={(e) => setForm({ ...form, adultsCount: Number(e.target.value) || 0 })} /></div>
                    <div className="sila-field"><label className="sila-label">Children</label><input type="number" className="sila-input" value={form.childrenCount} onChange={(e) => setForm({ ...form, childrenCount: Number(e.target.value) || 0 })} /></div>
                    <div className="sila-field"><label className="sila-label">VIP</label><input type="number" className="sila-input" value={form.vipCount} onChange={(e) => setForm({ ...form, vipCount: Number(e.target.value) || 0 })} /></div>
                    <div className="sila-field"><label className="sila-label">Staff Meals</label><input type="number" className="sila-input" value={form.staffMealsCount} onChange={(e) => setForm({ ...form, staffMealsCount: Number(e.target.value) || 0 })} /></div>
                    <div className="sila-field" style={{ display: "flex", alignItems: "flex-end" }}>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => handleSaveEvent(4)}>Save Guest Plan</button>
                    </div>
                  </div>
                  <table className="sila-table" style={{ width: "100%" }}>
                    <thead>
                      <tr><th>Dish</th><th>Recipe</th><th>Allergens</th><th>Unknown?</th><th>Status</th></tr>
                    </thead>
                    <tbody>
                      {allergens.map((a) => (
                        <tr key={a.menuLineId}>
                          <td>{a.dishName}</td>
                          <td>{a.recipeCode ? `${a.recipeCode} v${a.recipeVersion}` : "No Recipe"}</td>
                          <td>{a.verifiedAllergens.join(", ") || "None declared"}</td>
                          <td>{a.allergenUnknown ? "Yes" : "No"}</td>
                          <td>{a.verificationStatus}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </section>
              )}

              {eventTab === "menu" && (
                <div className="cs2-grid-2">
                  <section className="cs2-card">
                    <div className="cs2-card__head"><h3 className="cs2-card__title">Add Published Recipes to Event Menu</h3></div>
                    <div style={{ display: "flex", gap: 6, marginBottom: 10 }}>
                      <input className="sila-input" value={recipeQ} onChange={(e) => setRecipeQ(e.target.value)} placeholder="Search published recipes..." />
                      <input className="sila-input" style={{ width: 150 }} value={recipeCourse} onChange={(e) => setRecipeCourse(e.target.value)} placeholder="Course" />
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={async () => setPublishedRecipes(await searchPublishedCulinaryRecipes({ query: recipeQ || undefined }))}>
                        Search
                      </button>
                    </div>
                    <div style={{ display: "grid", gap: 6, maxHeight: 320, overflowY: "auto" }}>
                      {publishedRecipes.map((r) => (
                        <div key={`${r.recipeId}-${r.recipeVersion}`} style={{ display: "flex", justifyContent: "space-between", padding: 8, border: "1px solid #e2e8f0", borderRadius: 8 }}>
                          <div>
                            <strong>{r.name}</strong> <code>{r.recipeCode} v{r.recipeVersion}</code>
                            <div style={{ fontSize: 12, color: "#64748b" }}>{r.category} · {r.costPerPortion != null ? `${r.currency} ${r.costPerPortion.toFixed(2)}` : "Cost unavailable"}</div>
                          </div>
                          <button
                            type="button"
                            className="sila-btn sila-btn--sm sila-btn--primary"
                            disabled={!selectedEvent.primaryMenuId}
                            onClick={async () => {
                              if (!selectedEvent.primaryMenuId) return;
                              await upsertCulinaryMenuLine(selectedEvent.primaryMenuId, {
                                courseName: recipeCourse,
                                itemRole: "MANDATORY",
                                isLocked: false,
                                recipeId: r.recipeId,
                                recipeVersion: r.recipeVersion,
                                dishName: r.name,
                                serviceStyle: selectedEvent.serviceStyle,
                                uptakePercent: selectedEvent.serviceStyle === "BUFFET" ? 60 : 100,
                                servingFrequency: 1,
                                portionSize: 180,
                                portionUom: "G",
                                isBeverage: r.category.toLowerCase().includes("beverage"),
                                servingsPerGuest: 1,
                              });
                              await openEvent(selectedEvent.id);
                            }}
                          >
                            + Add
                          </button>
                        </div>
                      ))}
                    </div>
                  </section>

                  <section className="cs2-card">
                    <div className="cs2-card__head"><h3 className="cs2-card__title">Active Event Menu Lines</h3></div>
                    <div style={{ display: "grid", gap: 8 }}>
                      {(selectedEvent.primaryMenu?.lines ?? []).map((line) => (
                        <div key={line.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: 8, background: "#f8fafc", borderRadius: 8 }}>
                          <div>
                            <strong>[{line.courseName}] {line.dishName}</strong>
                            <div style={{ fontSize: 12, color: "#475569" }}>
                              {line.plannedPortions} portions · {line.uptakePercent}% uptake · {line.portionSize}{line.portionUom}
                              {line.costVisible && line.estimatedLineCost != null ? ` · ${selectedEvent.currency} ${line.estimatedLineCost.toFixed(2)}` : ""}
                            </div>
                          </div>
                          <div style={{ display: "flex", gap: 4 }}>
                            {!line.recipeId && !line.recipeDraftId && selectedEvent.primaryMenuId && (
                              <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => {
                                await createMissingRecipeDraftFromMenuLine(selectedEvent.primaryMenuId!, line.id);
                                await openEvent(selectedEvent.id);
                                toastService.success("Recipe Draft created.");
                              }}>Create Draft</button>
                            )}
                            {selectedEvent.primaryMenuId && (
                              <button type="button" className="sila-btn sila-btn--sm sila-btn--danger" onClick={async () => {
                                await removeCulinaryMenuLine(selectedEvent.primaryMenuId!, line.id);
                                await openEvent(selectedEvent.id);
                              }}>Remove</button>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  </section>
                </div>
              )}

              {eventTab === "ai" && (
                <div className="cs2-grid-2">
                  <section className="cs2-card">
                    <div className="cs2-card__head"><h3 className="cs2-card__title">AI Event Menu Generator</h3></div>
                    <div className="sila-field"><label className="sila-label">Event Culinary Brief</label><textarea className="sila-input" rows={3} value={aiBrief} onChange={(e) => setAiBrief(e.target.value)} /></div>
                    <div className="cs2-form-grid" style={{ marginTop: 8 }}>
                      <div className="sila-field"><label className="sila-label">Required Courses (comma-sep)</label><input className="sila-input" value={aiCourses} onChange={(e) => setAiCourses(e.target.value)} /></div>
                      <div className="sila-field"><label className="sila-label">Dietary Tags</label><input className="sila-input" value={aiDietary} onChange={(e) => setAiDietary(e.target.value)} /></div>
                      <div className="sila-field"><label className="sila-label">Exclude Allergens</label><input className="sila-input" value={aiExclAllergens} onChange={(e) => setAiExclAllergens(e.target.value)} /></div>
                      <div className="sila-field"><label className="sila-label">Strategy</label>
                        <select className="sila-select" value={aiStrategy} onChange={(e) => setAiStrategy(e.target.value)}>
                          <option value="BALANCED">Balanced</option>
                          <option value="LOWEST_COST">Lowest Cost</option>
                          <option value="HIGHEST_MARGIN">Highest Margin</option>
                        </select>
                      </div>
                    </div>
                    <div style={{ marginTop: 10, display: "flex", gap: 8 }}>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={aiBusy} onClick={async () => {
                        setAiBusy(true);
                        try {
                          const res = await generateCulinaryAiMenu(selectedEvent.id, {
                            naturalLanguageBrief: aiBrief,
                            cuisinePreferences: [],
                            requiredCourses: aiCourses.split(",").map((s) => s.trim()).filter(Boolean),
                            requiredDietaryTags: aiDietary.split(",").map((s) => s.trim()).filter(Boolean),
                            excludedAllergens: aiExclAllergens.split(",").map((s) => s.trim()).filter(Boolean),
                            strategy: aiStrategy,
                            allowMissingRecipeSuggestions: true,
                            applyToMenu: false,
                          });
                          setAiGenResult(res);
                        } finally { setAiBusy(false); }
                      }}>Preview AI Menu</button>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" disabled={aiBusy} onClick={async () => {
                        setAiBusy(true);
                        try {
                          const res = await generateCulinaryAiMenu(selectedEvent.id, {
                            naturalLanguageBrief: aiBrief,
                            cuisinePreferences: [],
                            requiredCourses: aiCourses.split(",").map((s) => s.trim()).filter(Boolean),
                            requiredDietaryTags: aiDietary.split(",").map((s) => s.trim()).filter(Boolean),
                            excludedAllergens: aiExclAllergens.split(",").map((s) => s.trim()).filter(Boolean),
                            strategy: aiStrategy,
                            allowMissingRecipeSuggestions: true,
                            applyToMenu: true,
                          });
                          setAiGenResult(res);
                          await openEvent(selectedEvent.id);
                          toastService.success("AI menu applied to event.");
                        } finally { setAiBusy(false); }
                      }}>Generate &amp; Apply</button>
                    </div>
                    {aiGenResult && (
                      <div style={{ marginTop: 12, fontSize: 13 }}>
                        <div><strong>{aiGenResult.menuTitle}</strong> ({aiGenResult.providerUsed})</div>
                        <div>{aiGenResult.explanation}</div>
                        <ul>{aiGenResult.proposedLines.map((l, idx) => <li key={idx}>[{l.courseName}] {l.dishName} ({l.lineStatus})</li>)}</ul>
                      </div>
                    )}
                  </section>

                  <section className="cs2-card">
                    <div className="cs2-card__head"><h3 className="cs2-card__title">MILP Constraint Optimizer (SciPy HiGHS)</h3></div>
                    <div className="sila-field">
                      <label className="sila-label">Max Budget ({selectedEvent.currency})</label>
                      <input type="number" className="sila-input" value={optMaxBudget} onChange={(e) => setOptMaxBudget(e.target.value)} placeholder="Leave blank to use event budget" />
                    </div>
                    <div style={{ marginTop: 10, display: "flex", gap: 8 }}>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" disabled={aiBusy} onClick={async () => {
                        setAiBusy(true);
                        try {
                          const res = await optimizeCulinaryEventMenu(selectedEvent.id, {
                            maxBudget: optMaxBudget ? Number(optMaxBudget) : undefined,
                            requiredDietaryTags: [],
                            excludedAllergens: aiExclAllergens.split(",").map((s) => s.trim()).filter(Boolean),
                            strategy: "LOWEST_COST",
                            applyChanges: false,
                          });
                          setOptResult(res);
                        } finally { setAiBusy(false); }
                      }}>Run MILP Optimization</button>
                    </div>
                    {optResult && (
                      <div style={{ marginTop: 12, fontSize: 13 }}>
                        <div>Feasible: <strong>{optResult.feasible ? "YES" : "NO"}</strong> ({optResult.solver})</div>
                        <div>Before: {optResult.beforeCost ?? "—"} → After: {optResult.afterCost ?? "—"} (Savings: {optResult.savings ?? 0})</div>
                        <div>{optResult.explanation}</div>
                        {optResult.conflictingConstraints.length > 0 && (
                          <div style={{ color: "#b91c1c" }}>Conflicts: {optResult.conflictingConstraints.join("; ")}</div>
                        )}
                      </div>
                    )}
                  </section>
                </div>
              )}

              {eventTab === "stock" && (
                <section className="cs2-card">
                  <div className="cs2-card__head">
                    <h3 className="cs2-card__title">Preliminary Read-Only Material &amp; Stock Preview</h3>
                    <span className="cs2-badge cs2-badge--live">No Stock Reservations · Read Only</span>
                  </div>
                  <table className="sila-table" style={{ width: "100%" }}>
                    <thead>
                      <tr><th>Dish</th><th>Ingredient</th><th>Material</th><th>Required</th><th>On-Hand</th><th>Indicator</th></tr>
                    </thead>
                    <tbody>
                      {materialPreview.map((m, idx) => (
                        <tr key={idx}>
                          <td>{m.dishName}</td>
                          <td>{m.ingredientName}</td>
                          <td>{m.materialCode ? `${m.materialCode} — ${m.materialDescription}` : "Unmapped"}</td>
                          <td>{m.requiredQty} {m.recipeUom}</td>
                          <td>{m.onHandStock != null ? `${m.onHandStock} ${m.baseUom || ""}` : "Unknown"}</td>
                          <td><span className={`cs2-badge ${m.stockIndicator === "SUFFICIENT" ? "cs2-badge--live" : "cs2-badge--warn"}`}>{m.stockIndicator}</span></td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </section>
              )}

              {eventTab === "simulate" && (
                <section className="cs2-card">
                  <div className="cs2-card__head"><h3 className="cs2-card__title">Interactive Guest, Cost &amp; Scenario Simulator</h3></div>
                  <div className="cs2-form-grid">
                    <div className="sila-field"><label className="sila-label">Simulated Guaranteed Guests</label><input type="number" className="sila-input" value={simGuests} onChange={(e) => setSimGuests(Number(e.target.value) || 0)} /></div>
                    <div className="sila-field"><label className="sila-label">Simulated Allowance %</label><input type="number" className="sila-input" value={simAllowance} onChange={(e) => setSimAllowance(Number(e.target.value) || 0)} /></div>
                    <div className="sila-field"><label className="sila-label">Simulated Net Price / Guest</label><input type="number" className="sila-input" value={simPrice} onChange={(e) => setSimPrice(Number(e.target.value) || 0)} /></div>
                    <div className="sila-field" style={{ display: "flex", alignItems: "flex-end" }}>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={handleRunSimulation}>Run Simulation</button>
                    </div>
                  </div>
                  {simResult && (
                    <div className="cs2-kpi-grid" style={{ marginTop: 12 }}>
                      <div className="cs2-kpi"><span className="cs2-kpi__label">Planning Guests</span><span className="cs2-kpi__value">{simResult.planningGuests}</span></div>
                      <div className="cs2-kpi"><span className="cs2-kpi__label">Est. Revenue</span><span className="cs2-kpi__value">{simResult.currency} {(simResult.estimatedRevenue ?? 0).toLocaleString()}</span></div>
                      <div className="cs2-kpi"><span className="cs2-kpi__label">Total Menu Cost</span><span className="cs2-kpi__value">{simResult.currency} {(simResult.totalMenuCost ?? 0).toLocaleString()}</span></div>
                      <div className="cs2-kpi"><span className="cs2-kpi__label">Menu Cost %</span><span className="cs2-kpi__value">{(simResult.menuCostPercent ?? 0).toFixed(1)}%</span></div>
                    </div>
                  )}
                  <hr style={{ margin: "14px 0", borderColor: "#e2e8f0" }} />
                  <div style={{ display: "flex", gap: 8, marginBottom: 10 }}>
                    <input className="sila-input" placeholder="Scenario name (e.g. Premium Menu)" value={scenarioName} onChange={(e) => setScenarioName(e.target.value)} />
                    <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => {
                      if (!scenarioName.trim()) return;
                      await createCulinaryEventScenario(selectedEvent.id, scenarioName.trim());
                      setScenarioName("");
                      await openEvent(selectedEvent.id);
                    }}>Save Current Menu as Scenario</button>
                  </div>
                  <div style={{ display: "grid", gap: 6 }}>
                    {selectedEvent.scenarios.map((sc) => (
                      <div key={sc.id} style={{ display: "flex", justifyContent: "space-between", padding: 8, border: "1px solid #e2e8f0", borderRadius: 8 }}>
                        <div><strong>{sc.scenarioName || sc.name}</strong> · {sc.lines.length} dishes · Est. Cost: {sc.totalEstimatedCost ?? "—"}</div>
                        <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={async () => {
                          const updated = await selectCulinaryEventScenario(selectedEvent.id, sc.id);
                          setSelectedEvent(updated);
                          toastService.success("Scenario promoted to primary event menu.");
                        }}>Select Scenario</button>
                      </div>
                    ))}
                  </div>
                </section>
              )}

              {eventTab === "customer" && customerPreview && (
                <section className="cs2-card">
                  <div className="cs2-card__head">
                    <div>
                      <h3 className="cs2-card__title">{customerPreview.eventName} — {customerPreview.menuTitle}</h3>
                      <div style={{ fontSize: 12, color: "#475569" }}>{customerPreview.venueName} · {customerPreview.startDateUtc.slice(0, 10)} · {customerPreview.serviceStyle}</div>
                    </div>
                    <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => window.print()}>Print Customer Menu</button>
                  </div>
                  {customerPreview.courses.map((c) => (
                    <div key={c.courseName} style={{ marginBottom: 12 }}>
                      <h4 style={{ margin: "6px 0" }}>{c.courseName}</h4>
                      <ul style={{ margin: 0, paddingLeft: 18 }}>
                        {c.dishes.map((d, idx) => (
                          <li key={idx}>
                            <strong>{d.dishName}</strong> {d.dietaryTags.length > 0 ? `(${d.dietaryTags.join(", ")})` : ""}
                            {d.allergenDisclosures.length > 0 ? ` — Contains: ${d.allergenDisclosures.join(", ")}` : ""}
                            {d.allergenPendingVerification ? " [Allergen verification pending]" : ""}
                          </li>
                        ))}
                      </ul>
                    </div>
                  ))}
                </section>
              )}

              {eventTab === "governance" && (
                <div className="cs2-grid-2">
                  <section className="cs2-card">
                    <div className="cs2-card__head">
                      <h3 className="cs2-card__title">Readiness &amp; Approval Workflow</h3>
                      {readiness && (
                        <span className={`cs2-badge ${readiness.readyForApproval ? "cs2-badge--live" : "cs2-badge--warn"}`}>
                          {readiness.passedCount} Pass · {readiness.warningCount} Warn · {readiness.blockedCount} Block
                        </span>
                      )}
                    </div>
                    {readiness?.checks.map((chk) => (
                      <div key={chk.checkCode} style={{ padding: 6, borderBottom: "1px solid #f1f5f9", fontSize: 13 }}>
                        <strong>[{chk.status}] {chk.label}:</strong> {chk.detail}
                      </div>
                    ))}
                    <div style={{ marginTop: 12, display: "flex", gap: 6, flexWrap: "wrap" }}>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={async () => {
                        const updated = await submitCulinaryEventForApproval(selectedEvent.id);
                        setSelectedEvent(updated);
                        await loadTabData("governance", updated);
                        toastService.success("Submitted event for approval.");
                      }}>Submit for Approval</button>
                      <input className="sila-input" style={{ width: 180 }} placeholder="Approval comment..." value={decisionComment} onChange={(e) => setDecisionComment(e.target.value)} />
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={async () => {
                        const updated = await decideCulinaryEventApproval(selectedEvent.id, "APPROVE", decisionComment || "Approved");
                        setSelectedEvent(updated);
                        await loadTabData("governance", updated);
                        toastService.success("Event approved.");
                      }}>Approve</button>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => {
                        const updated = await decideCulinaryEventApproval(selectedEvent.id, "RETURN", decisionComment || "Returned for revision");
                        setSelectedEvent(updated);
                        await loadTabData("governance", updated);
                      }}>Return</button>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => {
                        const updated = await createCulinaryEventRevision(selectedEvent.id, changeReason || "Post-approval revision");
                        setSelectedEvent(updated);
                        await loadTabData("governance", updated);
                        toastService.success(`Revision ${updated.revisionNumber} opened.`);
                      }}>New Revision</button>
                    </div>
                    <h4 style={{ marginTop: 14, marginBottom: 6 }}>Revision History</h4>
                    <div style={{ fontSize: 12, display: "grid", gap: 4 }}>
                      {revisions.map((r) => (
                        <div key={r.id}>
                          Rev {r.revisionNumber} · {r.statusAtRevision} · {r.planningGuestsAtRevision} guests {r.isApprovedBaseline ? "· APPROVED BASELINE" : ""} {r.changeReason ? `— ${r.changeReason}` : ""}
                        </div>
                      ))}
                    </div>
                  </section>

                  <section className="cs2-card">
                    <div className="cs2-card__head">
                      <h3 className="cs2-card__title">Event Reports &amp; MRP Handover</h3>
                      {handover && (
                        <span className={`cs2-badge ${handover.mrpReady ? "cs2-badge--live" : "cs2-badge--warn"}`}>
                          {handover.mrpReady ? "MRP Ready" : "Review Warnings"}
                        </span>
                      )}
                    </div>
                    <div style={{ display: "flex", gap: 6, flexWrap: "wrap", marginBottom: 12 }}>
                      {REPORT_CODES.map((rc) => (
                        <button key={rc.code} type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => {
                          const rep = await exportCulinaryEventReport(selectedEvent.id, rc.code);
                          downloadEventReportResult(rep);
                          toastService.success(`Exported ${rep.fileName}`);
                        }}>
                          {rc.label}
                        </button>
                      ))}
                    </div>
                    {handover && (
                      <div style={{ fontSize: 12, background: "#f8fafc", padding: 10, borderRadius: 8 }}>
                        <div><strong>Handover Contract:</strong> {handover.eventCode} (Rev {handover.eventRevision}, Approved Rev {handover.approvedRevision ?? "—"})</div>
                        <div><strong>Sessions:</strong> {handover.sessions.length} · <strong>Pinned Menu Lines:</strong> {handover.pinnedMenuLines.length} · <strong>Preliminary Materials:</strong> {handover.preliminaryMaterialRequirements.length}</div>
                        {handover.outstandingWarnings.length > 0 && (
                          <div style={{ color: "#b45309", marginTop: 4 }}>Warnings: {handover.outstandingWarnings.join(" | ")}</div>
                        )}
                        <div style={{ marginTop: 8 }}>
                          <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => onOpenWorkspace("mrp")}>
                            Launch Event MRP
                          </button>
                        </div>
                      </div>
                    )}
                  </section>
                </div>
              )}
            </>
          )}
        </>
      )}
    </div>
  );
};

export default CulinaryEventPlannerWorkspace;
