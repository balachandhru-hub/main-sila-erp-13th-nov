import React, { useMemo, useRef, useState } from 'react';
import {
  DEFAULT_CULINARY_FEATURE_FLAGS,
  getCulinaryAiStatus,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryAiApi';
import {
  cancelCulinaryGeneration,
  generateCulinaryRecipe,
  getCulinaryPlannerContext,
  getCulinaryRecipeDraft,
  listCulinaryRecipeDrafts,
  prepareCulinaryDishImage,
  refineCulinaryRecipe,
  saveCulinaryRecipeDraft,
  type CulinaryDishImagePrepared,
  type CulinaryRecipeDraftDetail,
  type CulinaryRefineResponse,
  type CulinaryStructuredRecipePayload,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryRecipesApi';
import {
  applyIngredientSubstitution,
  bulkAcceptExactMappings,
  confirmMaterialMapping,
  getMaterialWorkspace,
  matchDraftMaterials,
  prepareMaterialRequestDraft,
  rejectMaterialMapping,
  searchMaterialsForMapping,
  suggestIngredientSubstitutions,
  type SilaCulinaryMaterialMappingLine,
  type SilaCulinaryMaterialSearchItem,
  type SilaCulinaryMaterialWorkspace,
  type SilaCulinarySubstitutionResponse,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryMaterialsApi';
import {
  assignProductionRecipeImage,
  calculateDraftCosting,
  createMaterialRequest,
  getApprovalPackage,
  getRecipeReadiness,
  listMaterialRequests,
  submitMaterialRequest,
  submitRecipeForApproval,
  type SilaCulinaryApprovalPackage,
  type SilaCulinaryCostingResult,
  type SilaCulinaryMaterialRequest,
  type SilaCulinaryRecipeReadiness,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryLifecycleApi';
import {
  decideCulinaryEventApproval,
  getCulinaryEvent,
  getCulinaryEventMaterialPreview,
  getCulinaryEventReadiness,
  listCulinaryEvents,
  simulateCulinaryEvent,
  submitCulinaryEventForApproval,
  type SilaCulinaryEvent,
  type SilaCulinaryEventReadiness,
  type SilaCulinaryEventSimulationResult,
  type SilaCulinaryMaterialPreviewLine,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryEventMenuApi';
import {
  decideMrpApproval,
  getMrpRun,
  listMrpRuns,
  simulateMrpRun,
  submitMrpForApproval,
  type SilaCulinaryMrpRunDetail,
  type SilaCulinaryMrpRunSummary,
  type SilaCulinaryMrpSimulationResult,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryMrpApi';
import {
  createProcurementItoDraft,
  createProcurementPoDraft,
  decideProcurementPlanApproval,
  getProcurementPlan,
  listProcurementPlans,
  submitProcurementPlanForApproval,
  type SilaCulinaryProcurementPlanDetail,
  type SilaCulinaryProcurementPlanSummary,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryProcurementApi';
import {
  decideFinancialPlanApproval,
  getMobileFinancialSummary,
  listEventFinancialPlans,
  simulateEventFinancials,
  submitFinancialPlanForApproval,
  type SilaCulinaryEventFinancialPlanSummary,
  type SilaCulinaryMobileFinancialSummary,
  type SilaCulinarySimulateFinancialResult,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryFinancialApi';
import {
  decideCulinaryRecommendation,
  getMobileCulinaryOpsSummary,
  queryCulinaryCopilot,
  recordCulinaryWaste,
  type SilaCulinaryCopilotResponse,
  type SilaCulinaryMobileOpsSummary,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryAnalyticsApi';
import {
  confirmProductionPickingLine,
  executeProductionBatchAction,
  getMobileProductionSummary,
  getProductionBatchTraceability,
  getProductionKdsFeed,
  issueMaterialToProduction,
  listBatchTasks,
  listProductionExceptions,
  recordProductionQualityCheck,
  reportProductionException,
  triggerBuffetReplenishment,
  updateProductionTaskStatus,
  type SilaCulinaryMobileProductionSummary,
  type SilaCulinaryProductionBatch,
  type SilaCulinaryProductionBatchTraceability,
  type SilaCulinaryProductionException,
  type SilaCulinaryProductionPickingLine,
  type SilaCulinaryProductionTask,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryProductionApi';
import { scaleDraftLocally } from '../../../../remote-buyer/src/components/silaMe/recipes/culinary/culinaryRecipePlannerHelpers';
import {
  calculateRecipeScaling,
  computeMrpRow,
  DEMO_EVENT_MRP_ROWS,
  DEMO_SALMON_RECIPE,
} from '../../../../remote-buyer/src/components/silaMe/recipes/culinary/culinarySimulatorsMath';
import ScreenHeader from '../../components/ScreenHeader';
import { useOnline } from '../../components/StateViews';
import { useGo } from '../../navigation';
import { errorText, readStored, useLoad, writeStored } from '../../useLoad';
import './recipeStudio.css';

type MobileTab = 'generate' | 'draft' | 'materials' | 'lifecycle' | 'events' | 'mrp' | 'procurement' | 'pnl' | 'analytics' | 'production' | 'history' | 'demo';

interface MobileProdOfflineAction {
  id: string;
  idempotencyKey: string;
  kind: 'BATCH_STATUS' | 'TASK_UPDATE' | 'QUALITY_CHECK' | 'PICKING_CONFIRM' | 'BUFFET_REPLENISH' | 'EXCEPTION_REPORT';
  label: string;
  payload: Record<string, unknown>;
  createdAt: string;
  syncStatus: 'PENDING' | 'CONFLICT' | 'FAILED';
  error?: string;
}

const MOBILE_PROD_QUEUE_KEY = 'sila_mobile_prod_offline_queue_v1';

const loadOfflineProdQueue = (): MobileProdOfflineAction[] => {
  const raw = readStored(MOBILE_PROD_QUEUE_KEY);
  if (!raw) return [];
  try {
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
};

/**
 * Phase 1A + Phase 1B Mobile Culinary Intelligence — responsive chef AI recipe generation,
 * dish photograph capture/upload, serving scaling, quantity editing, basic refinement,
 * draft saving, draft history, and Material Intelligence mapping (§39, §43).
 * Never fetches or displays administrator AI model settings or credentials.
 */
const CulinarySuitePreviewScreen: React.FC = () => {
  const go = useGo();
  const statusLoad = useLoad(() => getCulinaryAiStatus(), 'mobile-culinary-ai-status');
  const contextLoad = useLoad(() => getCulinaryPlannerContext(), 'mobile-culinary-context');
  const draftsLoad = useLoad(() => listCulinaryRecipeDrafts(), 'mobile-culinary-drafts');

  const [tab, setTab] = useState<MobileTab>('generate');
  const [recipeName, setRecipeName] = useState('');
  const [cuisine, setCuisine] = useState('');
  const [servings, setServings] = useState(10);
  const [servingSize, setServingSize] = useState(180);
  const [dietaryText, setDietaryText] = useState('');
  const [instructionsText, setInstructionsText] = useState('');
  const [dishImage, setDishImage] = useState<CulinaryDishImagePrepared | null>(null);
  const [dishImageName, setDishImageName] = useState<string | null>(null);
  const [generating, setGenerating] = useState(false);
  const [statusMsg, setStatusMsg] = useState<string | null>(null);
  const [errMsg, setErrMsg] = useState<string | null>(null);
  const activeGenRef = useRef<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  const [activeDraft, setActiveDraft] = useState<CulinaryRecipeDraftDetail | null>(null);
  const [payload, setPayload] = useState<CulinaryStructuredRecipePayload | null>(null);
  const [targetServings, setTargetServings] = useState(10);
  const [targetServingSize, setTargetServingSize] = useState(180);
  const [refineText, setRefineText] = useState('');
  const [refining, setRefining] = useState(false);
  const [pendingRefine, setPendingRefine] = useState<CulinaryRefineResponse | null>(null);
  const [saving, setSaving] = useState(false);

  // Phase 1B Mobile Material Intelligence state
  const [matWs, setMatWs] = useState<SilaCulinaryMaterialWorkspace | null>(null);
  const [matLoading, setMatLoading] = useState(false);
  const [matMatching, setMatMatching] = useState(false);
  const [activeMatRef, setActiveMatRef] = useState<string | null>(null);
  const [matSearchQ, setMatSearchQ] = useState('');
  const [matSearchItems, setMatSearchItems] = useState<SilaCulinaryMaterialSearchItem[]>([]);
  const [matSubs, setMatSubs] = useState<SilaCulinarySubstitutionResponse | null>(null);
  const [reqDraftDesc, setReqDraftDesc] = useState('');
  const [reqDraftReason, setReqDraftReason] = useState('');

  // Phase 1C Mobile Costing, Requests & Lifecycle state
  const [mobileMrqs, setMobileMrqs] = useState<SilaCulinaryMaterialRequest[]>([]);
  const [mobileCosting, setMobileCosting] = useState<SilaCulinaryCostingResult | null>(null);
  const [mobileReadiness, setMobileReadiness] = useState<SilaCulinaryRecipeReadiness | null>(null);
  const [mobilePkg, setMobilePkg] = useState<SilaCulinaryApprovalPackage | null>(null);
  const [mobileSellingPrice, setMobileSellingPrice] = useState('65');
  const [mobileCostServings, setMobileCostServings] = useState('10');
  const [lifecycleLoading, setLifecycleLoading] = useState(false);

  // Phase 0 Demo fallback tab state
  const [demoServings, setDemoServings] = useState(10);
  const [guests, setGuests] = useState(500);

  // Phase 2A Mobile Event & Menu Planner state
  const [mobileEvents, setMobileEvents] = useState<SilaCulinaryEvent[]>([]);
  const [selectedMobileEvent, setSelectedMobileEvent] = useState<SilaCulinaryEvent | null>(null);
  const [mobileEventReadiness, setMobileEventReadiness] = useState<SilaCulinaryEventReadiness | null>(null);
  const [mobileEventStock, setMobileEventStock] = useState<SilaCulinaryMaterialPreviewLine[]>([]);
  const [mobileEventSimGuests, setMobileEventSimGuests] = useState(500);
  const [mobileEventSim, setMobileEventSim] = useState<SilaCulinaryEventSimulationResult | null>(null);
  const [eventsLoading, setEventsLoading] = useState(false);

  // Phase 2B Mobile Event MRP state (chef-friendly, cost-permission gated, zero procurement transactions)
  const [mobileMrpRuns, setMobileMrpRuns] = useState<SilaCulinaryMrpRunSummary[]>([]);
  const [activeMobileMrp, setActiveMobileMrp] = useState<SilaCulinaryMrpRunDetail | null>(null);
  const [mrpLoading, setMrpLoading] = useState(false);
  const [mrpMatQuery, setMrpMatQuery] = useState('');
  const [mrpGuestScalePct, setMrpGuestScalePct] = useState(100);
  const [mrpBufferPct, setMrpBufferPct] = useState(0);
  const [mrpSimResult, setMrpSimResult] = useState<SilaCulinaryMrpSimulationResult | null>(null);

  // Phase 2C Mobile Event Procurement state (chef-friendly, cost-permission gated, zero auto-postings)
  const [mobileProcPlans, setMobileProcPlans] = useState<SilaCulinaryProcurementPlanSummary[]>([]);
  const [activeMobileProc, setActiveMobileProc] = useState<SilaCulinaryProcurementPlanDetail | null>(null);
  const [procLoading, setProcLoading] = useState(false);
  const [procShortagesOnly, setProcShortagesOnly] = useState(false);

  // Phase 3A Mobile Chef Financial Summary state (§69: hides salaries, supplier terms, GL balances, admin AI secrets)
  const [mobileFinPlans, setMobileFinPlans] = useState<SilaCulinaryEventFinancialPlanSummary[]>([]);
  const [mobileFinSummary, setMobileFinSummary] = useState<SilaCulinaryMobileFinancialSummary | null>(null);
  const [mobileFinLoading, setMobileFinLoading] = useState(false);
  const [mobileFinGuests, setMobileFinGuests] = useState(500);
  const [mobileFinSim, setMobileFinSim] = useState<SilaCulinarySimulateFinancialResult | null>(null);

  // Phase 3B Mobile Operational Culinary Intelligence state (covers, waste/deviation sync, buffet replenishment, role-safe Copilot)
  const [opsSummary, setOpsSummary] = useState<SilaCulinaryMobileOpsSummary | null>(null);
  const [opsLoading, setOpsLoading] = useState(false);
  const [mobWasteIng, setMobWasteIng] = useState('');
  const [mobWasteCat, setMobWasteCat] = useState('PRODUCTION_WASTE');
  const [mobWasteQty, setMobWasteQty] = useState(1.5);
  const [mobCopilotQ, setMobCopilotQ] = useState('What should we prioritize for today prep and waste control?');
  const [mobCopilotAns, setMobCopilotAns] = useState<SilaCulinaryCopilotResponse | null>(null);

  // Phase 4A Mobile Kitchen Production Operations & Offline Queue state (§29)
  const isOnline = useOnline();
  const [prodSummary, setProdSummary] = useState<SilaCulinaryMobileProductionSummary | null>(null);
  const [prodKdsBatches, setProdKdsBatches] = useState<SilaCulinaryProductionBatch[]>([]);
  const [prodTasks, setProdTasks] = useState<SilaCulinaryProductionTask[]>([]);
  const [prodPicks, setProdPicks] = useState<SilaCulinaryProductionPickingLine[]>([]);
  const [prodExceptions, setProdExceptions] = useState<SilaCulinaryProductionException[]>([]);
  const [selectedBatchId, setSelectedBatchId] = useState('');
  const [selectedTrace, setSelectedTrace] = useState<SilaCulinaryProductionBatchTraceability | null>(null);
  const [prodLoading, setProdLoading] = useState(false);
  const [mobScanCode, setMobScanCode] = useState('');
  const [mobQcName, setMobQcName] = useState('Cooking Core Temp');
  const [mobQcExpected, setMobQcExpected] = useState('>= 75 C');
  const [mobQcValue, setMobQcValue] = useState('76');
  const [mobQcPassed, setMobQcPassed] = useState(true);
  const [mobQcNotes, setMobQcNotes] = useState('');
  const [mobBuffetRecipe, setMobBuffetRecipe] = useState('');
  const [mobBuffetPortions, setMobBuffetPortions] = useState(40);
  const [mobExcTitle, setMobExcTitle] = useState('');
  const [mobExcType, setMobExcType] = useState('INGREDIENT_SHORTAGE');
  const [offlineQueue, setOfflineQueue] = useState<MobileProdOfflineAction[]>(() => loadOfflineProdQueue());
  const [syncingQueue, setSyncingQueue] = useState(false);

  const persistOfflineQueue = (next: MobileProdOfflineAction[]) => {
    setOfflineQueue(next);
    writeStored(MOBILE_PROD_QUEUE_KEY, JSON.stringify(next));
  };

  const enqueueOfflineAction = (
    kind: MobileProdOfflineAction['kind'],
    label: string,
    payload: Record<string, unknown>,
  ) => {
    const action: MobileProdOfflineAction = {
      id: `q-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
      idempotencyKey: `MOB4A-${kind}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
      kind,
      label,
      payload,
      createdAt: new Date().toISOString(),
      syncStatus: 'PENDING',
    };
    const next = [action, ...offlineQueue];
    persistOfflineQueue(next);
    setStatusMsg(`Queued offline (${next.length} pending): ${label}. Sync when online.`);
  };

  const loadMobileProductionData = async (batchOverride?: string) => {
    setProdLoading(true);
    setErrMsg(null);
    try {
      const [summary, kds, excs] = await Promise.all([
        getMobileProductionSummary().catch(() => null),
        getProductionKdsFeed().catch(() => []),
        listProductionExceptions().catch(() => []),
      ]);
      setProdSummary(summary);
      setProdKdsBatches(kds.length > 0 ? kds : summary?.activeBatches ?? []);
      setProdPicks(summary?.pickingLines ?? []);
      setProdExceptions(excs.length > 0 ? excs : summary?.exceptions ?? []);
      const targetBatch = batchOverride || selectedBatchId || kds[0]?.id || summary?.activeBatches?.[0]?.id || '';
      if (targetBatch) {
        setSelectedBatchId(targetBatch);
        const [tasks, trace] = await Promise.all([
          listBatchTasks(targetBatch).catch(() => summary?.myTasks ?? []),
          getProductionBatchTraceability(targetBatch).catch(() => null),
        ]);
        setProdTasks(tasks);
        setSelectedTrace(trace);
      } else {
        setProdTasks(summary?.myTasks ?? []);
      }
    } catch (err) {
      setErrMsg(errorText(err, 'Unable to load kitchen production operations.'));
    } finally {
      setProdLoading(false);
    }
  };

  const handleOpenProductionTab = async () => {
    setTab('production');
    await loadMobileProductionData();
  };

  const handleSyncOfflineQueue = async () => {
    if (!isOnline) {
      setErrMsg('Device is offline. Connect to sync pending production actions.');
      return;
    }
    if (offlineQueue.length === 0) return;
    setSyncingQueue(true);
    setErrMsg(null);
    const remaining: MobileProdOfflineAction[] = [];
    let syncedCount = 0;
    for (const item of offlineQueue) {
      try {
        if (item.kind === 'BATCH_STATUS') {
          await executeProductionBatchAction(
            String(item.payload.batchId),
            item.payload.action as 'START_PREP' | 'START_COOKING' | 'PAUSE' | 'RESUME' | 'READY_FOR_QUALITY' | 'COMPLETE_BATCH',
            item.payload.actualOutputQty as number | undefined,
            `${String(item.payload.note || '')} [${item.idempotencyKey}]`.trim(),
          );
        } else if (item.kind === 'TASK_UPDATE') {
          await updateProductionTaskStatus(
            String(item.payload.taskId),
            String(item.payload.status),
            `Synced ${item.idempotencyKey}`,
          );
        } else if (item.kind === 'QUALITY_CHECK') {
          await recordProductionQualityCheck({
            productionBatchId: String(item.payload.productionBatchId),
            checkpointName: String(item.payload.checkpointName),
            expectedValue: String(item.payload.expectedValue || '>= 75 C'),
            actualValue: String(item.payload.actualValue),
            measurementUom: String(item.payload.measurementUom || 'C'),
            passed: Boolean(item.payload.passed),
            comments: `${String(item.payload.comments || '')} [${item.idempotencyKey}]`.trim(),
          });
        } else if (item.kind === 'PICKING_CONFIRM') {
          await confirmProductionPickingLine(
            String(item.payload.pickingLineId),
            Number(item.payload.pickedQty),
            item.payload.inventoryBatchNumber ? String(item.payload.inventoryBatchNumber) : undefined,
          );
        } else if (item.kind === 'BUFFET_REPLENISH') {
          await triggerBuffetReplenishment({
            kitchenId: String(item.payload.kitchenId || 'DEFAULT_KITCHEN'),
            itemName: String(item.payload.itemName),
            qty: Number(item.payload.qty),
          });
        } else if (item.kind === 'EXCEPTION_REPORT') {
          await reportProductionException({
            batchId: item.payload.batchId ? String(item.payload.batchId) : undefined,
            exceptionType: String(item.payload.exceptionType),
            severity: String(item.payload.severity || 'HIGH'),
            description: `${String(item.payload.description || '')} [${item.idempotencyKey}]`.trim(),
          });
        }
        syncedCount += 1;
      } catch (err) {
        const msg = errorText(err, 'Sync failed');
        remaining.push({
          ...item,
          syncStatus: msg.toLowerCase().includes('conflict') || msg.toLowerCase().includes('version') ? 'CONFLICT' : 'FAILED',
          error: msg,
        });
      }
    }
    persistOfflineQueue(remaining);
    setSyncingQueue(false);
    setStatusMsg(`Synced ${syncedCount} action(s). ${remaining.length} remaining.`);
    await loadMobileProductionData();
  };

  const handleMobileBatchAction = async (
    batch: SilaCulinaryProductionBatch,
    action: 'START_PREP' | 'START_COOKING' | 'PAUSE' | 'RESUME' | 'READY_FOR_QUALITY' | 'COMPLETE_BATCH',
  ) => {
    setErrMsg(null);
    const payloadData = {
      batchId: batch.id,
      action,
      actualOutputQty: action === 'COMPLETE_BATCH' ? batch.plannedBatchQty : undefined,
      note: `Mobile KDS -> ${action}`,
    };
    if (!isOnline) {
      enqueueOfflineAction('BATCH_STATUS', `${batch.batchNumber} -> ${action}`, payloadData);
      return;
    }
    try {
      await executeProductionBatchAction(batch.id, action, payloadData.actualOutputQty, payloadData.note);
      setStatusMsg(`Batch ${batch.batchNumber} updated (${action}).`);
      await loadMobileProductionData(batch.id);
    } catch (err) {
      setErrMsg(errorText(err));
    }
  };

  const handleOpenOpsTab = async () => {
    setTab('analytics');
    setOpsLoading(true);
    try {
      const summary = await getMobileCulinaryOpsSummary();
      setOpsSummary(summary);
    } catch (err) {
      setErrMsg(errorText(err));
    } finally {
      setOpsLoading(false);
    }
  };

  const handleOpenPnlTab = async () => {
    setTab('pnl');
    setMobileFinLoading(true);
    try {
      const [plansList, summaryData] = await Promise.all([
        listEventFinancialPlans({ includeSimulations: false }).catch(() => []),
        getMobileFinancialSummary().catch(() => null),
      ]);
      setMobileFinPlans(plansList);
      setMobileFinSummary(summaryData);
      if (summaryData?.plannedGuests) setMobileFinGuests(summaryData.plannedGuests);
      setMobileFinSim(null);
    } catch (err) {
      setErrMsg(errorText(err));
    } finally {
      setMobileFinLoading(false);
    }
  };

  const handleOpenProcurementTab = async () => {
    setTab('procurement');
    setProcLoading(true);
    try {
      const list = await listProcurementPlans({ includeSimulations: true });
      setMobileProcPlans(list);
      const target = activeMobileProc ? list.find((p) => p.id === activeMobileProc.summary.id) ?? list[0] : list[0];
      if (target) {
        setActiveMobileProc(await getProcurementPlan(target.id));
      }
    } catch (err) {
      setErrMsg(errorText(err));
    } finally {
      setProcLoading(false);
    }
  };

  const handleOpenMrpTab = async () => {
    setTab('mrp');
    setMrpLoading(true);
    try {
      const list = await listMrpRuns({ includeSimulations: true });
      setMobileMrpRuns(list);
      const target = activeMobileMrp ? list.find((r) => r.id === activeMobileMrp.id) ?? list[0] : list[0];
      if (target) {
        setActiveMobileMrp(await getMrpRun(target.id));
        setMrpSimResult(null);
      }
    } catch (err) {
      setErrMsg(errorText(err));
    } finally {
      setMrpLoading(false);
    }
  };

  const handleOpenEventsTab = async () => {
    setTab('events');
    setEventsLoading(true);
    try {
      const list = await listCulinaryEvents();
      setMobileEvents(list);
      const target = selectedMobileEvent ? list.find((e) => e.id === selectedMobileEvent.id) ?? list[0] : list[0];
      if (target) {
        const detail = await getCulinaryEvent(target.id);
        setSelectedMobileEvent(detail);
        setMobileEventSimGuests(detail.guaranteedGuests);
        const [rd, st] = await Promise.all([
          getCulinaryEventReadiness(detail.id),
          getCulinaryEventMaterialPreview(detail.id),
        ]);
        setMobileEventReadiness(rd);
        setMobileEventStock(st);
      }
    } catch (err) {
      setErrMsg(errorText(err));
    } finally {
      setEventsLoading(false);
    }
  };

  const flags = statusLoad.data?.featureFlags ?? DEFAULT_CULINARY_FEATURE_FLAGS;

  const demoScaling = useMemo(
    () =>
      calculateRecipeScaling({
        baseServings: DEMO_SALMON_RECIPE.baseServings,
        targetServings: demoServings,
        baseServingSizeGrams: DEMO_SALMON_RECIPE.baseServingSizeGrams,
        targetServingSizeGrams: DEMO_SALMON_RECIPE.baseServingSizeGrams,
        prepBufferPercent: 0,
        wasteAllowancePercent: 0,
        ingredients: DEMO_SALMON_RECIPE.ingredients,
      }),
    [demoServings],
  );

  const mrpRows = useMemo(
    () => DEMO_EVENT_MRP_ROWS.map((row) => computeMrpRow(guests, row)),
    [guests],
  );

  const scaledDraft = useMemo(() => {
    if (!payload) return null;
    return scaleDraftLocally({
      recipeName: payload.header.proposed_name,
      baseServings: payload.header.base_servings || 10,
      targetServings,
      baseServingSize: payload.header.serving_size || 180,
      targetServingSize,
      prepBufferPercent: 0,
      useNonlinearRules: true,
      ingredients: payload.ingredients,
    });
  }, [payload, targetServings, targetServingSize]);

  const hydrateDraft = (detail: CulinaryRecipeDraftDetail) => {
    setActiveDraft(detail);
    setPayload(detail.payload);
    setTargetServings(detail.payload.header.base_servings || 10);
    setTargetServingSize(detail.payload.header.serving_size || 180);
    setPendingRefine(null);
    setTab('draft');
  };

  const handlePhotoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setErrMsg(null);
    try {
      const prepared = await prepareCulinaryDishImage(file);
      setDishImage(prepared);
      setDishImageName(file.name);
      setStatusMsg(`Photo ready (${prepared.width}x${prepared.height}). Inferred ingredients will require chef review.`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Unable to prepare dish photo.'));
    } finally {
      e.target.value = '';
    }
  };

  const handleGenerate = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrMsg(null);
    setStatusMsg(null);
    if (!recipeName.trim() && !dishImage) {
      setErrMsg('Enter a recipe name or attach a dish photograph.');
      return;
    }

    const genRef = `MOB-${Date.now()}`;
    activeGenRef.current = genRef;
    const ctrl = new AbortController();
    abortRef.current = ctrl;
    setGenerating(true);

    try {
      const created = await generateCulinaryRecipe(
        {
          generationMode: dishImage ? 'DISH_IMAGE' : instructionsText.trim() ? 'DETAILED_INSTRUCTIONS' : 'DISH_NAME',
          recipeName: recipeName.trim() || 'Dish Inspiration Recipe',
          recipeDescription: instructionsText.trim(),
          cuisine: cuisine.trim(),
          servings: Math.max(1, servings),
          servingSize: Math.max(1, servingSize),
          servingUom: 'G',
          dietaryPreferences: dietaryText
            .split(',')
            .map((s) => s.trim())
            .filter(Boolean),
          locationId: contextLoad.data?.locations?.[0]?.locationId ?? null,
          imageBase64: dishImage?.base64 ?? null,
          imageMimeType: dishImage?.mime_type ?? null,
          generationReference: genRef,
        },
        ctrl.signal,
      );
      hydrateDraft(created);
      draftsLoad.reload();
      setStatusMsg(`Generated via ${created.provider} (${created.model}).`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Recipe generation failed.'));
    } finally {
      setGenerating(false);
      activeGenRef.current = null;
      abortRef.current = null;
    }
  };

  const handleCancelGenerate = async () => {
    abortRef.current?.abort();
    if (activeGenRef.current) {
      await cancelCulinaryGeneration(activeGenRef.current);
    }
    setGenerating(false);
    setStatusMsg('Generation cancelled.');
  };

  const handleRefine = async () => {
    if (!payload || !refineText.trim()) return;
    setErrMsg(null);
    setRefining(true);
    try {
      const res = await refineCulinaryRecipe(
        { instruction: refineText.trim(), currentPayload: payload },
        activeDraft?.id,
      );
      setPendingRefine(res);
      setRefineText('');
    } catch (caught) {
      setErrMsg(errorText(caught, 'Refinement failed.'));
    } finally {
      setRefining(false);
    }
  };

  const handleSaveDraft = async () => {
    if (!activeDraft || !payload) return;
    setErrMsg(null);
    setSaving(true);
    try {
      const saved = await saveCulinaryRecipeDraft(activeDraft.id, {
        expectedRevisionNumber: activeDraft.revisionNumber,
        status: 'CHEF_REVIEW',
        changeType: 'CHEF_EDIT',
        changeSummary: 'Saved from SILA Mobile Culinary AI Planner.',
        payload,
      });
      hydrateDraft(saved);
      draftsLoad.reload();
      setStatusMsg(`Saved ${saved.draftReference} (Rev ${saved.revisionNumber}).`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Failed to save draft.'));
    } finally {
      setSaving(false);
    }
  };

  const handleOpenDraft = async (draftId: string) => {
    setErrMsg(null);
    try {
      const detail = await getCulinaryRecipeDraft(draftId);
      hydrateDraft(detail);
      setMatWs(null);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Unable to open recipe draft.'));
    }
  };

  const loadMaterialWorkspace = async (draftId: string) => {
    setErrMsg(null);
    setMatLoading(true);
    try {
      const ws = await getMaterialWorkspace(draftId);
      setMatWs(ws);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Unable to load material mapping workspace.'));
    } finally {
      setMatLoading(false);
    }
  };

  const handleOpenMaterialsTab = async () => {
    setTab('materials');
    if (activeDraft && (!matWs || matWs.draftId !== activeDraft.id)) {
      await loadMaterialWorkspace(activeDraft.id);
    }
  };

  const loadMobileLifecycle = async (draftId: string, srvOverride?: number, priceOverride?: number) => {
    setErrMsg(null);
    setLifecycleLoading(true);
    try {
      const [mrqs, rd, pkg, cst] = await Promise.all([
        listMaterialRequests({}),
        getRecipeReadiness(draftId),
        getApprovalPackage(draftId),
        calculateDraftCosting(draftId, {
          servings: srvOverride,
          sellingPricePerPortion: priceOverride,
        }).catch(() => null),
      ]);
      setMobileMrqs(mrqs);
      setMobileReadiness(rd);
      setMobilePkg(pkg);
      setMobileCosting(cst);
      if (cst) {
        setMobileCostServings(String(cst.servings));
      }
    } catch (caught) {
      setErrMsg(errorText(caught, 'Unable to load Phase 1C costing & readiness.'));
    } finally {
      setLifecycleLoading(false);
    }
  };

  const handleOpenLifecycleTab = async () => {
    setTab('lifecycle');
    if (activeDraft) {
      await loadMobileLifecycle(activeDraft.id);
    } else {
      setLifecycleLoading(true);
      try {
        const mrqs = await listMaterialRequests({});
        setMobileMrqs(mrqs);
      } catch {
        // ignore
      } finally {
        setLifecycleLoading(false);
      }
    }
  };

  const handleMobileMatchAll = async () => {
    if (!activeDraft) return;
    setErrMsg(null);
    setMatMatching(true);
    try {
      const ws = await matchDraftMaterials(activeDraft.id, { unresolvedOnly: false });
      setMatWs(ws);
      setStatusMsg(`Matched: ${ws.kpis.exactMatches} exact, ${ws.kpis.suggestedMatches} suggested.`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Material matching failed.'));
    } finally {
      setMatMatching(false);
    }
  };

  const handleMobileBulkAccept = async () => {
    if (!activeDraft) return;
    setErrMsg(null);
    setMatMatching(true);
    try {
      const ws = await bulkAcceptExactMappings(activeDraft.id);
      setMatWs(ws);
      setStatusMsg(`Confirmed ${ws.kpis.confirmedMappings} exact mapping(s).`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Bulk accept failed.'));
    } finally {
      setMatMatching(false);
    }
  };

  const handleMobileLineConfirm = async (
    line: SilaCulinaryMaterialMappingLine,
    materialId: string | null,
    materialCode: string | null,
  ) => {
    if (!activeDraft) return;
    setErrMsg(null);
    try {
      await confirmMaterialMapping(activeDraft.id, line.ingredientTempRef, {
        expectedMappingRevision: line.mappingRevision,
        materialId,
        materialCode,
      });
      await loadMaterialWorkspace(activeDraft.id);
      setActiveMatRef(null);
      setStatusMsg(`Confirmed ${materialCode ?? 'material'} for ${line.originalDescription}.`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Confirm failed.'));
    }
  };

  const handleMobileLineReject = async (line: SilaCulinaryMaterialMappingLine) => {
    if (!activeDraft) return;
    setErrMsg(null);
    try {
      await rejectMaterialMapping(activeDraft.id, line.ingredientTempRef, {
        expectedMappingRevision: line.mappingRevision,
        reason: 'Rejected on mobile review',
      });
      await loadMaterialWorkspace(activeDraft.id);
      setStatusMsg(`Rejected candidate for ${line.originalDescription}.`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Reject failed.'));
    }
  };

  return (
    <>
      <ScreenHeader title="AI Recipe Planner" eyebrow="Phase 1A Mobile" back backTo="recipes" />
      <div className="sm-screen" data-testid="mobile-culinary-suite-preview">
        {!flags.culinarySuiteEnabled ? (
          <section className="sm-card">
            <p className="sm-meta">
              Culinary Intelligence is currently disabled by your administrator feature flags.
            </p>
          </section>
        ) : (
          <>
            <section className="sm-card">
              <p className="sm-eyebrow">CULINARY INTELLIGENCE · PHASE 1A</p>
              <p className="sm-meta" style={{ marginTop: '0.25rem' }}>
                Engine: <strong>{statusLoad.data?.operatingMode ?? 'LOCAL'}</strong> · Offline AI generation is not claimed unless a verified local backend runtime is connected.
              </p>
            </section>

            <div className="rs-tabs" role="tablist">
              <button type="button" className={`rs-tab ${tab === 'generate' ? 'rs-tab--on' : ''}`} onClick={() => setTab('generate')}>
                Generate
              </button>
              <button type="button" className={`rs-tab ${tab === 'draft' ? 'rs-tab--on' : ''}`} onClick={() => setTab('draft')}>
                Draft{activeDraft ? ` (R${activeDraft.revisionNumber})` : ''}
              </button>
              <button type="button" className={`rs-tab ${tab === 'materials' ? 'rs-tab--on' : ''}`} onClick={handleOpenMaterialsTab}>
                Materials{matWs ? ` (${matWs.kpis.confirmedMappings}/${matWs.kpis.totalIngredients})` : ''}
              </button>
              <button type="button" className={`rs-tab ${tab === 'lifecycle' ? 'rs-tab--on' : ''}`} onClick={handleOpenLifecycleTab}>
                Cost &amp; Publish
              </button>
              <button type="button" className={`rs-tab ${tab === 'events' ? 'rs-tab--on' : ''}`} onClick={handleOpenEventsTab}>
                Events{mobileEvents.length > 0 ? ` (${mobileEvents.length})` : ''}
              </button>
              <button type="button" className={`rs-tab ${tab === 'mrp' ? 'rs-tab--on' : ''}`} onClick={handleOpenMrpTab}>
                Event MRP{mobileMrpRuns.length > 0 ? ` (${mobileMrpRuns.length})` : ''}
              </button>
              <button type="button" className={`rs-tab ${tab === 'procurement' ? 'rs-tab--on' : ''}`} onClick={handleOpenProcurementTab}>
                Procurement{mobileProcPlans.length > 0 ? ` (${mobileProcPlans.length})` : ''}
              </button>
              <button type="button" className={`rs-tab ${tab === 'pnl' ? 'rs-tab--on' : ''}`} onClick={handleOpenPnlTab}>
                Event P&amp;L
              </button>
              <button type="button" className={`rs-tab ${tab === 'analytics' ? 'rs-tab--on' : ''}`} onClick={handleOpenOpsTab}>
                Daily Ops
              </button>
              {(flags.culinaryPhase4AEnabled ?? true) && (
                <button type="button" className={`rs-tab ${tab === 'production' ? 'rs-tab--on' : ''}`} onClick={handleOpenProductionTab}>
                  Production Ops{offlineQueue.length > 0 ? ` (${offlineQueue.length})` : ''}
                </button>
              )}
              <button type="button" className={`rs-tab ${tab === 'history' ? 'rs-tab--on' : ''}`} onClick={() => setTab('history')}>
                History ({draftsLoad.data?.length ?? 0})
              </button>
              <button type="button" className={`rs-tab ${tab === 'demo' ? 'rs-tab--on' : ''}`} onClick={() => setTab('demo')}>
                Demo
              </button>
            </div>

            {errMsg && (
              <section className="sm-card" style={{ borderLeft: '4px solid #dc2626' }} role="alert">
                <p style={{ margin: 0, color: '#991b1b', fontSize: '0.85rem' }}>{errMsg}</p>
              </section>
            )}
            {statusMsg && (
              <section className="sm-card" style={{ borderLeft: '4px solid #059669' }} role="status">
                <p style={{ margin: 0, color: '#065f46', fontSize: '0.85rem' }}>{statusMsg}</p>
              </section>
            )}

            {tab === 'generate' && (
              <form className="sm-card" onSubmit={handleGenerate}>
                <div className="sm-field">
                  <label htmlFor="m-p1a-name">Recipe Name</label>
                  <input id="m-p1a-name" placeholder="e.g. Butter Chicken" value={recipeName} onChange={(e) => setRecipeName(e.target.value)} />
                </div>
                <div className="sm-field" style={{ marginTop: '0.5rem' }}>
                  <label htmlFor="m-p1a-cuisine">Cuisine</label>
                  <input id="m-p1a-cuisine" placeholder="e.g. Indian" value={cuisine} onChange={(e) => setCuisine(e.target.value)} />
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem', marginTop: '0.5rem' }}>
                  <div className="sm-field">
                    <label htmlFor="m-p1a-srv">Servings</label>
                    <input id="m-p1a-srv" type="number" min={1} value={servings} onChange={(e) => setServings(Math.max(1, Number(e.target.value) || 1))} />
                  </div>
                  <div className="sm-field">
                    <label htmlFor="m-p1a-sz">Serving Size (g)</label>
                    <input id="m-p1a-sz" type="number" min={10} value={servingSize} onChange={(e) => setServingSize(Math.max(10, Number(e.target.value) || 100))} />
                  </div>
                </div>
                <div className="sm-field" style={{ marginTop: '0.5rem' }}>
                  <label htmlFor="m-p1a-diet">Dietary Preferences</label>
                  <input id="m-p1a-diet" placeholder="e.g. Halal, Gluten-free" value={dietaryText} onChange={(e) => setDietaryText(e.target.value)} />
                </div>
                <div className="sm-field" style={{ marginTop: '0.5rem' }}>
                  <label htmlFor="m-p1a-inst">Chef Instructions (Optional)</label>
                  <textarea id="m-p1a-inst" rows={2} placeholder="Spice level, buffet hold, equipment..." value={instructionsText} onChange={(e) => setInstructionsText(e.target.value)} />
                </div>
                <div className="sm-field" style={{ marginTop: '0.5rem' }}>
                  <label htmlFor="m-p1a-photo">Dish Photograph Inspiration</label>
                  <input id="m-p1a-photo" type="file" accept="image/jpeg,image/png,image/webp" capture="environment" onChange={handlePhotoUpload} />
                  {dishImage && (
                    <p className="sm-meta" style={{ marginTop: '0.25rem' }}>
                      Attached: {dishImageName} ({dishImage.width}x{dishImage.height})
                    </p>
                  )}
                </div>
                <div className="sm-actions" style={{ marginTop: '0.75rem' }}>
                  <button type="submit" className="sm-btn sm-btn--primary" disabled={generating}>
                    {generating ? 'Generating Recipe...' : 'Generate Recipe'}
                  </button>
                  {generating && (
                    <button type="button" className="sm-btn" onClick={handleCancelGenerate}>
                      Cancel
                    </button>
                  )}
                </div>
              </form>
            )}

            {tab === 'draft' && (
              <>
                {!payload ? (
                  <section className="sm-card">
                    <p className="sm-meta">No active recipe draft loaded. Generate a recipe or pick one from History.</p>
                  </section>
                ) : (
                  <>
                    <section className="sm-card">
                      <p className="sm-eyebrow">
                        {activeDraft?.draftReference} · {activeDraft?.provider}/{activeDraft?.model}
                      </p>
                      <h2 style={{ margin: '0.25rem 0' }}>{payload.header.proposed_name}</h2>
                      <p className="sm-meta">{payload.header.description}</p>
                      <p className="sm-meta" style={{ marginTop: '0.35rem' }}>
                        <strong>Allergens (Requires Verification):</strong>{' '}
                        {payload.header.potential_allergens.join(', ') || 'None flagged'}
                      </p>
                    </section>

                    <section className="sm-card">
                      <h3 style={{ margin: '0 0 0.4rem' }}>Portion Scaling ({scaledDraft?.linear_factor ?? 1}x)</h3>
                      <div className="sm-field">
                        <label htmlFor="m-p1a-scale-srv">Servings: <strong>{targetServings}</strong> (base {payload.header.base_servings})</label>
                        <input id="m-p1a-scale-srv" type="range" min={1} max={300} value={targetServings} onChange={(e) => setTargetServings(Number(e.target.value))} />
                      </div>
                      <div className="sm-field" style={{ marginTop: '0.4rem' }}>
                        <label htmlFor="m-p1a-scale-sz">Serving Size ({payload.header.serving_uom})</label>
                        <input id="m-p1a-scale-sz" type="number" min={1} value={targetServingSize} onChange={(e) => setTargetServingSize(Math.max(1, Number(e.target.value) || 1))} />
                      </div>
                    </section>

                    <section className="sm-card">
                      <h3 style={{ margin: '0 0 0.4rem' }}>Ingredients ({payload.ingredients.length})</h3>
                      <ul className="sm-list">
                        {payload.ingredients.map((ing, idx) => {
                          const scaled = scaledDraft?.ingredients.find((s) => s.temp_ref === ing.temp_ref);
                          return (
                            <li key={ing.temp_ref || idx} className="sm-row" style={{ padding: '0.4rem 0', gap: '0.5rem' }}>
                              <div style={{ flex: 1 }}>
                                <strong>{ing.description}</strong>
                                <div className="sm-meta">
                                  Scaled: {scaled?.scaled_raw_qty ?? ing.quantity} {ing.recipe_uom} · {ing.material_mapping_status}
                                </div>
                              </div>
                              <input
                                type="number"
                                step="0.01"
                                aria-label={`Base qty ${ing.description}`}
                                style={{ width: '4.5rem' }}
                                value={ing.quantity}
                                onChange={(e) => {
                                  const qty = Math.max(0.001, Number(e.target.value) || 0.001);
                                  setPayload((prev) => {
                                    if (!prev) return prev;
                                    const next = [...prev.ingredients];
                                    next[idx] = { ...next[idx], quantity: qty, provenance: 'CHEF_EDITED' };
                                    return { ...prev, ingredients: next };
                                  });
                                }}
                              />
                            </li>
                          );
                        })}
                      </ul>
                    </section>

                    <section className="sm-card">
                      <h3 style={{ margin: '0 0 0.4rem' }}>Preparation Steps ({payload.instructions.length})</h3>
                      <div className="rs-step">
                        {payload.instructions.map((s) => (
                          <div key={s.step_number}>
                            <span className="rs-step-num">{s.step_number}</span>
                            <strong>{s.step_title}:</strong> {s.instruction}
                          </div>
                        ))}
                      </div>
                    </section>

                    <section className="sm-card">
                      <h3 style={{ margin: '0 0 0.4rem' }}>AI Conversational Refinement</h3>
                      <div style={{ display: 'flex', gap: '0.4rem' }}>
                        <input
                          placeholder="e.g. Make less spicy or replace chicken with paneer"
                          value={refineText}
                          onChange={(e) => setRefineText(e.target.value)}
                        />
                        <button type="button" className="sm-btn sm-btn--primary" disabled={refining || !refineText.trim()} onClick={handleRefine}>
                          {refining ? '...' : 'Refine'}
                        </button>
                      </div>
                      {pendingRefine && (
                        <div style={{ marginTop: '0.6rem', padding: '0.5rem', background: '#eef2ff', borderRadius: '0.5rem' }}>
                          <p style={{ margin: 0, fontSize: '0.82rem' }}>
                            <strong>Proposed ({pendingRefine.diffSummary.length} changes):</strong> {pendingRefine.summaryMessage}
                          </p>
                          <div className="sm-actions" style={{ marginTop: '0.4rem' }}>
                            <button
                              type="button"
                              className="sm-btn sm-btn--primary"
                              onClick={() => {
                                setPayload(pendingRefine.proposedPayload);
                                setPendingRefine(null);
                              }}
                            >
                              Accept
                            </button>
                            <button type="button" className="sm-btn" onClick={() => setPendingRefine(null)}>
                              Reject
                            </button>
                          </div>
                        </div>
                      )}
                    </section>

                    <div className="rs-sticky">
                      <button type="button" className="sm-btn sm-btn--primary" disabled={saving} onClick={handleSaveDraft}>
                        {saving ? 'Saving...' : 'Save Draft'}
                      </button>
                      <button type="button" className="sm-btn" onClick={handleOpenMaterialsTab}>
                        Map Materials
                      </button>
                      <button type="button" className="sm-btn" onClick={() => go('recipes/catalog')}>
                        Catalog
                      </button>
                    </div>
                  </>
                )}
              </>
            )}

            {tab === 'materials' && (
              <>
                {!activeDraft ? (
                  <section className="sm-card">
                    <p className="sm-meta">Open or generate a recipe draft first to inspect Phase 1B material mappings.</p>
                  </section>
                ) : matLoading ? (
                  <section className="sm-card">
                    <p className="sm-meta">Loading Material Intelligence workspace...</p>
                  </section>
                ) : (
                  <>
                    <section className="sm-card" data-testid="mobile-p1b-materials-header">
                      <p className="sm-eyebrow">PHASE 1B MATERIAL INTELLIGENCE</p>
                      <h2 style={{ margin: '0.25rem 0' }}>{matWs?.recipeName ?? activeDraft.recipeName}</h2>
                      <p className="sm-meta">
                        Status: <strong>{matWs?.readinessStatus ?? 'MATERIAL_MAPPING_PENDING'}</strong> · Confirmed{' '}
                        <strong>{matWs?.kpis.confirmedMappings ?? 0}/{matWs?.kpis.totalIngredients ?? 0}</strong> · Missing{' '}
                        <strong>{matWs?.kpis.missingMaterials ?? 0}</strong>
                      </p>
                      <div className="sm-actions" style={{ marginTop: '0.5rem' }}>
                        <button type="button" className="sm-btn sm-btn--primary" disabled={matMatching} onClick={handleMobileMatchAll}>
                          {matMatching ? 'Matching...' : 'Match All'}
                        </button>
                        <button type="button" className="sm-btn" disabled={matMatching} onClick={handleMobileBulkAccept}>
                          Accept Exact
                        </button>
                        <button type="button" className="sm-btn" onClick={() => loadMaterialWorkspace(activeDraft.id)}>
                          Refresh
                        </button>
                      </div>
                    </section>

                    {(matWs?.lines ?? []).map((line) => {
                      const isExpanded = activeMatRef === line.ingredientTempRef;
                      return (
                        <section key={line.ingredientTempRef} className="sm-card" data-testid={`mobile-p1b-line-${line.ingredientTempRef}`}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '0.5rem' }}>
                            <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                              {line.materialImageUrl ? (
                                <img
                                  src={line.materialImageUrl}
                                  alt={line.materialDescription ?? line.originalDescription}
                                  style={{ width: 34, height: 34, borderRadius: 6, objectFit: 'cover' }}
                                />
                              ) : (
                                <span
                                  style={{
                                    width: 34,
                                    height: 34,
                                    borderRadius: 6,
                                    background: '#f1f5f9',
                                    color: '#475569',
                                    display: 'inline-flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    fontSize: 10,
                                    fontWeight: 700,
                                  }}
                                >
                                  MAT
                                </span>
                              )}
                              <div>
                                <strong>{line.originalDescription}</strong>
                                <div className="sm-meta">
                                  Need: {line.requiredQuantity} {line.recipeUom}
                                  {line.convertedQuantity != null && line.baseUom
                                    ? ` -> ${line.convertedQuantity} ${line.baseUom}`
                                    : line.uomCompatible
                                      ? ''
                                      : ' (UOM Conflict)'}
                                </div>
                              </div>
                            </div>
                            <span className="sm-meta" style={{ fontWeight: 700 }}>{line.mappingStatus}</span>
                          </div>

                          <div className="sm-meta" style={{ marginTop: '0.35rem' }}>
                            Material: <code>{line.materialCode ?? 'UNMATCHED'}</code>{' '}
                            {line.materialDescription ?? ''} · Stock:{' '}
                            <strong>
                              {line.stockStatus === 'UNKNOWN' || line.availableStock == null
                                ? 'Unknown'
                                : `${line.availableStock} ${line.stockUom ?? ''}`}
                            </strong>
                          </div>

                          <div className="sm-actions" style={{ marginTop: '0.45rem' }}>
                            {line.materialId && line.mappingStatus !== 'CONFIRMED' && line.uomCompatible && line.specificationCompatible && (
                              <button
                                type="button"
                                className="sm-btn sm-btn--primary"
                                onClick={() => handleMobileLineConfirm(line, line.materialId ?? null, line.materialCode ?? null)}
                              >
                                Confirm
                              </button>
                            )}
                            <button
                              type="button"
                              className="sm-btn"
                              onClick={() => {
                                if (isExpanded) {
                                  setActiveMatRef(null);
                                } else {
                                  setActiveMatRef(line.ingredientTempRef);
                                  setMatSearchQ(line.originalDescription);
                                  setMatSearchItems([]);
                                  setMatSubs(null);
                                  setReqDraftDesc(line.requestDraft?.proposedDescription ?? line.originalDescription.toUpperCase());
                                  setReqDraftReason(line.requestDraft?.reason ?? line.missingReason ?? '');
                                }
                              }}
                            >
                              {isExpanded ? 'Hide Details' : 'Options / Search'}
                            </button>
                            {line.materialId && (
                              <button type="button" className="sm-btn" onClick={() => handleMobileLineReject(line)}>
                                Reject
                              </button>
                            )}
                          </div>

                          {isExpanded && (
                            <div style={{ marginTop: '0.6rem', paddingTop: '0.6rem', borderTop: '1px solid #e2e8f0' }}>
                              {line.candidates.length > 0 && (
                                <div style={{ marginBottom: '0.5rem' }}>
                                  <strong style={{ fontSize: '0.8rem' }}>Candidates</strong>
                                  {line.candidates.map((c) => (
                                    <div key={c.materialId + c.materialCode} className="sm-row" style={{ padding: '0.3rem 0' }}>
                                      <div>
                                        <code>{c.materialCode}</code> {c.description}
                                        <div className="sm-meta">
                                          {c.matchMethod} · {(c.finalScore * 100).toFixed(0)}% · {c.baseUom}
                                        </div>
                                      </div>
                                      <button
                                        type="button"
                                        className="sm-btn"
                                        disabled={!c.materialActive || !c.plantEligible}
                                        onClick={() => handleMobileLineConfirm(line, c.materialId, c.materialCode)}
                                      >
                                        Select
                                      </button>
                                    </div>
                                  ))}
                                </div>
                              )}

                              <div className="sm-field">
                                <label htmlFor={`m-search-${line.ingredientTempRef}`}>Search Material Master</label>
                                <div style={{ display: 'flex', gap: '0.4rem' }}>
                                  <input
                                    id={`m-search-${line.ingredientTempRef}`}
                                    value={matSearchQ}
                                    onChange={(e) => setMatSearchQ(e.target.value)}
                                    placeholder="Code or description"
                                  />
                                  <button
                                    type="button"
                                    className="sm-btn"
                                    onClick={async () => {
                                      const items = await searchMaterialsForMapping(matSearchQ, matWs?.propertyId ?? null, 10);
                                      setMatSearchItems(items);
                                    }}
                                  >
                                    Find
                                  </button>
                                </div>
                              </div>
                              {matSearchItems.map((it) => (
                                <div key={it.materialId} className="sm-row" style={{ padding: '0.25rem 0' }}>
                                  <span><code>{it.materialCode}</code> {it.description}</span>
                                  <button
                                    type="button"
                                    className="sm-btn"
                                    disabled={!it.plantEligible}
                                    onClick={() => handleMobileLineConfirm(line, it.materialId, it.materialCode)}
                                  >
                                    Assign
                                  </button>
                                </div>
                              ))}

                              <div className="sm-actions" style={{ marginTop: '0.5rem' }}>
                                <button
                                  type="button"
                                  className="sm-btn"
                                  onClick={async () => {
                                    const res = await suggestIngredientSubstitutions(activeDraft.id, line.ingredientTempRef);
                                    setMatSubs(res);
                                  }}
                                >
                                  Review Substitutions
                                </button>
                              </div>
                              {matSubs && matSubs.temp_ref === line.ingredientTempRef && (
                                <div style={{ marginTop: '0.4rem' }}>
                                  {matSubs.suggestions.map((s) => (
                                    <div key={s.proposed_ingredient} className="sm-row" style={{ padding: '0.25rem 0' }}>
                                      <div>
                                        <strong>{s.proposed_ingredient}</strong> ({s.substitution_kind})
                                        <div className="sm-meta">{s.taste_impact}</div>
                                      </div>
                                      <button
                                        type="button"
                                        className="sm-btn"
                                        onClick={async () => {
                                          await applyIngredientSubstitution(activeDraft.id, line.ingredientTempRef, {
                                            expectedMappingRevision: line.mappingRevision,
                                            proposedIngredient: s.proposed_ingredient,
                                            proposedMaterialId: s.proposed_material_id ?? null,
                                            proposedMaterialCode: s.proposed_material_code ?? null,
                                            replacementRatio: s.suggested_replacement_ratio ?? 1,
                                          });
                                          await loadMaterialWorkspace(activeDraft.id);
                                          setStatusMsg(`Applied substitution ${s.proposed_ingredient} to draft.`);
                                        }}
                                      >
                                        Apply
                                      </button>
                                    </div>
                                  ))}
                                </div>
                              )}

                              <div style={{ marginTop: '0.5rem' }}>
                                <strong style={{ fontSize: '0.8rem' }}>Prepare Missing Material Request (Phase 1C)</strong>
                                <div className="sm-field" style={{ marginTop: '0.25rem' }}>
                                  <input
                                    placeholder="Proposed material description"
                                    value={reqDraftDesc}
                                    onChange={(e) => setReqDraftDesc(e.target.value)}
                                  />
                                </div>
                                <div className="sm-field" style={{ marginTop: '0.25rem' }}>
                                  <input
                                    placeholder="Reason / specification"
                                    value={reqDraftReason}
                                    onChange={(e) => setReqDraftReason(e.target.value)}
                                  />
                                </div>
                                <button
                                  type="button"
                                  className="sm-btn"
                                  style={{ marginTop: '0.3rem' }}
                                  disabled={!reqDraftDesc.trim()}
                                  onClick={async () => {
                                    await prepareMaterialRequestDraft(activeDraft.id, line.ingredientTempRef, {
                                      expectedMappingRevision: line.mappingRevision,
                                      requestType: line.recommendedRequestType ?? 'NEW_MATERIAL',
                                      proposedDescription: reqDraftDesc.trim(),
                                      proposedBaseUom: line.recipeUom,
                                      reason: reqDraftReason.trim() || 'Marked missing on mobile',
                                    });
                                    await loadMaterialWorkspace(activeDraft.id);
                                    setStatusMsg(`Prepared Phase 1C request draft for ${line.originalDescription}.`);
                                  }}
                                >
                                  Mark Missing &amp; Save Draft
                                </button>
                              </div>
                            </div>
                          )}
                        </section>
                      );
                    })}
                  </>
                )}
              </>
            )}

            {tab === 'lifecycle' && (
              <>
                {lifecycleLoading ? (
                  <section className="sm-card"><p className="sm-meta">Loading Phase 1C Costing, Requests &amp; Readiness...</p></section>
                ) : (
                  <>
                    {activeDraft && mobileReadiness && (
                      <section className="sm-card" data-testid="mobile-p1c-readiness">
                        <p className="sm-eyebrow">PHASE 1C · READINESS &amp; APPROVAL</p>
                        <h2 style={{ margin: '0.25rem 0' }}>{activeDraft.recipeName}</h2>
                        <p className="sm-meta">
                          Readiness: <strong>{mobileReadiness.passedCount}/{mobileReadiness.checks.length} passed</strong> ·
                          Blocking: <strong>{mobileReadiness.blockingCount}</strong> ·
                          Status: <strong>{mobilePkg?.lifecycle.lifecycleStatus ?? 'DRAFT'}</strong>
                        </p>
                        {mobilePkg?.lifecycle.productionRecipeCode && (
                          <p className="sm-meta">
                            Production Recipe ID: <code>{mobilePkg.lifecycle.productionRecipeCode}</code> (v{mobilePkg.lifecycle.productionRecipeVersion})
                          </p>
                        )}
                        <div className="sm-actions" style={{ marginTop: '0.5rem' }}>
                          {mobilePkg?.lifecycle.lifecycleStatus === 'DRAFT' && (
                            <button
                              type="button"
                              className="sm-btn sm-btn--primary"
                              onClick={async () => {
                                try {
                                  await submitRecipeForApproval(activeDraft.id, 'Submitted from SILA Mobile');
                                  setStatusMsg('Submitted recipe for approval.');
                                  await loadMobileLifecycle(activeDraft.id);
                                } catch (caught) {
                                  setErrMsg(errorText(caught, 'Submission failed.'));
                                }
                              }}
                            >
                              Submit for Approval
                            </button>
                          )}
                          {mobilePkg?.lifecycle.productionRecipeId && (
                            <label className="sm-btn" style={{ cursor: 'pointer' }}>
                              Capture Dish Photo
                              <input
                                type="file"
                                accept="image/jpeg,image/png,image/webp"
                                capture="environment"
                                hidden
                                onChange={async (ev) => {
                                  const f = ev.target.files?.[0];
                                  if (!f) return;
                                  try {
                                    await assignProductionRecipeImage(activeDraft.id, f, 'CHEF_PHOTOGRAPH');
                                    setStatusMsg('Assigned photograph to Production Recipe ID.');
                                    await loadMobileLifecycle(activeDraft.id);
                                  } catch (caught) {
                                    setErrMsg(errorText(caught, 'Photo upload failed.'));
                                  }
                                }}
                              />
                            </label>
                          )}
                          {mobilePkg?.lifecycle.lifecycleStatus === 'PUBLISHED' && (
                            <button type="button" className="sm-btn sm-btn--primary" onClick={() => go('recipes/catalog')}>
                              Open Published Recipe
                            </button>
                          )}
                        </div>
                      </section>
                    )}

                    {activeDraft && mobileCosting && (
                      <section className="sm-card" data-testid="mobile-p1c-costing">
                        <p className="sm-eyebrow">COSTING SUMMARY ({mobileCosting.currency})</p>
                        {mobileCosting.canViewCostDetail === false ? (
                          <p className="sm-meta">Cost detail visibility is restricted for your role.</p>
                        ) : (
                          <>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem', marginTop: '0.4rem' }}>
                          <div className="sm-field">
                            <label htmlFor="m-p1c-srv">Servings</label>
                            <input
                              id="m-p1c-srv"
                              type="number"
                              min={1}
                              value={mobileCostServings}
                              onChange={(e) => setMobileCostServings(e.target.value)}
                            />
                          </div>
                          <div className="sm-field">
                            <label htmlFor="m-p1c-price">Selling Price</label>
                            <input
                              id="m-p1c-price"
                              type="number"
                              step="0.5"
                              value={mobileSellingPrice}
                              onChange={(e) => setMobileSellingPrice(e.target.value)}
                            />
                          </div>
                        </div>
                        <button
                          type="button"
                          className="sm-btn"
                          style={{ marginTop: '0.4rem' }}
                          onClick={() =>
                            loadMobileLifecycle(
                              activeDraft.id,
                              Math.max(1, Number(mobileCostServings) || 10),
                              Number(mobileSellingPrice) || undefined,
                            )
                          }
                        >
                          Recalculate Cost
                        </button>
                        <div className="sm-meta" style={{ marginTop: '0.5rem', lineHeight: 1.7 }}>
                          <div>
                            Total Cost: <strong>{mobileCosting.currency} {mobileCosting.totalIngredientCost.toFixed(2)}</strong> ·
                            Per Portion: <strong>{mobileCosting.currency} {mobileCosting.ingredientCostPerPortion.toFixed(2)}</strong>
                          </div>
                          <div>
                            Food Cost %: <strong>{mobileCosting.foodCostPercent != null ? `${mobileCosting.foodCostPercent.toFixed(1)}%` : 'N/A'}</strong> ·
                            Completeness: <strong>{mobileCosting.costCompleteness}</strong>
                          </div>
                        </div>
                          </>
                        )}
                      </section>
                    )}

                    <section className="sm-card" data-testid="mobile-p1c-requests">
                      <p className="sm-eyebrow">MATERIAL REQUESTS ({mobileMrqs.length})</p>
                      {activeDraft && matWs && matWs.lines.some((l) => l.mappingStatus === 'MISSING' || l.mappingStatus === 'UNMATCHED') && (
                        <button
                          type="button"
                          className="sm-btn sm-btn--primary"
                          style={{ marginBottom: '0.5rem' }}
                          onClick={async () => {
                            const missing = matWs.lines.find((l) => l.mappingStatus === 'MISSING' || l.mappingStatus === 'UNMATCHED');
                            if (!missing) return;
                            try {
                              const created = await createMaterialRequest({
                                requestType: missing.recommendedRequestType ?? 'NEW_MATERIAL',
                                proposedDescription: missing.originalDescription.toUpperCase(),
                                baseUom: missing.recipeUom,
                                businessJustification: 'Requested from SILA Mobile',
                                links: [
                                  {
                                    draftId: activeDraft.id,
                                    ingredientTempRef: missing.ingredientTempRef,
                                    ingredientDescription: missing.originalDescription,
                                    requiredQuantity: missing.requiredQuantity,
                                    recipeUom: missing.recipeUom,
                                  },
                                ],
                              });
                              await submitMaterialRequest(created.id);
                              setStatusMsg(`Submitted ${created.requestReference} for approval.`);
                              await loadMobileLifecycle(activeDraft.id);
                            } catch (caught) {
                              setErrMsg(errorText(caught, 'Material request failed.'));
                            }
                          }}
                        >
                          Prepare &amp; Submit Missing Material Request
                        </button>
                      )}
                      {mobileMrqs.length === 0 ? (
                        <p className="sm-meta">No material requests for this tenant yet.</p>
                      ) : (
                        <ul className="sm-list">
                          {mobileMrqs.slice(0, 8).map((r) => (
                            <li key={r.id} className="sm-row" style={{ padding: '0.35rem 0' }}>
                              <div>
                                <code>{r.requestReference}</code> {r.proposedDescription}
                                <div className="sm-meta">{r.requestType} · {r.resultingMaterialCode ?? 'Pending'}</div>
                              </div>
                              <strong>{r.status}</strong>
                            </li>
                          ))}
                        </ul>
                      )}
                    </section>
                  </>
                )}
              </>
            )}

            {tab === 'events' && (
              <section className="sm-card">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                  <h2 style={{ margin: 0 }}>Event &amp; Menu Planner</h2>
                  <button type="button" className="sm-btn" onClick={handleOpenEventsTab} disabled={eventsLoading}>
                    {eventsLoading ? 'Loading...' : 'Refresh'}
                  </button>
                </div>
                {mobileEvents.length === 0 ? (
                  <p className="sm-meta">No events created yet. Create events in SILA Cloud Recipe Studio.</p>
                ) : (
                  <>
                    <div className="sm-field">
                      <label htmlFor="m-p2a-ev">Select Event</label>
                      <select
                        id="m-p2a-ev"
                        value={selectedMobileEvent?.id ?? ''}
                        onChange={async (e) => {
                          const detail = await getCulinaryEvent(e.target.value);
                          setSelectedMobileEvent(detail);
                          setMobileEventSimGuests(detail.guaranteedGuests);
                          const [rd, st] = await Promise.all([
                            getCulinaryEventReadiness(detail.id),
                            getCulinaryEventMaterialPreview(detail.id),
                          ]);
                          setMobileEventReadiness(rd);
                          setMobileEventStock(st);
                        }}
                      >
                        {mobileEvents.map((ev) => (
                          <option key={ev.id} value={ev.id}>
                            {ev.eventCode} — {ev.name} ({ev.status})
                          </option>
                        ))}
                      </select>
                    </div>
                    {selectedMobileEvent && (
                      <div style={{ marginTop: '0.6rem', display: 'grid', gap: '0.5rem' }}>
                        <p className="sm-meta" style={{ margin: 0 }}>
                          <strong>{selectedMobileEvent.startDateUtc.slice(0, 10)}</strong> · {selectedMobileEvent.venueName || 'Venue TBD'} ·{' '}
                          {selectedMobileEvent.planningGuests} planned guests · {selectedMobileEvent.serviceStyle}
                        </p>
                        {selectedMobileEvent.costVisible && (
                          <p className="sm-meta" style={{ margin: 0 }}>
                            Est. Menu Cost: <strong>{selectedMobileEvent.currency} {(selectedMobileEvent.totalEstimatedMenuCost ?? 0).toFixed(2)}</strong>
                          </p>
                        )}
                        <div className="sm-field">
                          <label htmlFor="m-p2a-sim">Simulate Guests: <strong>{mobileEventSimGuests}</strong></label>
                          <input
                            id="m-p2a-sim"
                            type="range"
                            min={20}
                            max={1500}
                            step={10}
                            value={mobileEventSimGuests}
                            onChange={(e) => setMobileEventSimGuests(Number(e.target.value))}
                          />
                          <button
                            type="button"
                            className="sm-btn"
                            style={{ marginTop: '0.35rem' }}
                            onClick={async () => {
                              const res = await simulateCulinaryEvent(selectedMobileEvent.id, {
                                guaranteedGuests: mobileEventSimGuests,
                                expectedGuests: mobileEventSimGuests,
                                lineOverrides: [],
                              });
                              setMobileEventSim(res);
                            }}
                          >
                            Simulate Event Cost &amp; Portions
                          </button>
                          {mobileEventSim && (
                            <p className="sm-meta" style={{ marginTop: '0.3rem' }}>
                              Planning Guests: <strong>{mobileEventSim.planningGuests}</strong>
                              {selectedMobileEvent.costVisible && mobileEventSim.totalMenuCost != null
                                ? ` · Est. Menu Cost: ${mobileEventSim.currency} ${mobileEventSim.totalMenuCost.toFixed(2)}`
                                : ''}
                            </p>
                          )}
                        </div>
                        {mobileEventReadiness && (
                          <p className="sm-meta" style={{ margin: 0 }}>
                            Readiness: {mobileEventReadiness.passedCount} pass · {mobileEventReadiness.warningCount} warn · {mobileEventReadiness.blockedCount} block
                          </p>
                        )}
                        {mobileEventStock.length > 0 && (
                          <ul className="sm-list">
                            {mobileEventStock.slice(0, 5).map((st, idx) => (
                              <li key={idx} className="sm-row" style={{ padding: '0.25rem 0' }}>
                                <span>{st.ingredientName}</span>
                                <span>{st.requiredQty} {st.recipeUom} ({st.stockIndicator})</span>
                              </li>
                            ))}
                          </ul>
                        )}
                        <div className="sm-actions">
                          <button
                            type="button"
                            className="sm-btn sm-btn--primary"
                            onClick={async () => {
                              const updated = await submitCulinaryEventForApproval(selectedMobileEvent.id);
                              setSelectedMobileEvent(updated);
                              setStatusMsg(`Submitted ${updated.eventCode} for approval.`);
                            }}
                          >
                            Submit Approval
                          </button>
                          <button
                            type="button"
                            className="sm-btn"
                            onClick={async () => {
                              const updated = await decideCulinaryEventApproval(selectedMobileEvent.id, 'APPROVE', 'Approved on mobile');
                              setSelectedMobileEvent(updated);
                              setStatusMsg(`Approved ${updated.eventCode}.`);
                            }}
                          >
                            Approve
                          </button>
                        </div>
                      </div>
                    )}
                  </>
                )}
              </section>
            )}

            {tab === 'mrp' && (
              <section className="sm-card" data-testid="mobile-p2b-mrp">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                  <h2 style={{ margin: 0 }}>Event MRP Studio</h2>
                  <button type="button" className="sm-btn" onClick={handleOpenMrpTab} disabled={mrpLoading}>
                    {mrpLoading ? 'Loading...' : 'Refresh'}
                  </button>
                </div>
                {mobileMrpRuns.length === 0 ? (
                  <p className="sm-meta">No Event MRP runs yet. Calculate an MRP baseline in SILA Cloud Recipe Studio or preview the Demo tab.</p>
                ) : (
                  <>
                    <div className="sm-field">
                      <label htmlFor="m-p2b-run">Select MRP Run</label>
                      <select
                        id="m-p2b-run"
                        value={activeMobileMrp?.id ?? ''}
                        onChange={async (e) => {
                          setActiveMobileMrp(await getMrpRun(e.target.value));
                          setMrpSimResult(null);
                        }}
                      >
                        {mobileMrpRuns.map((r) => (
                          <option key={r.id} value={r.id}>
                            {r.mrpCode} v{r.versionNumber} — {r.name} ({r.status})
                          </option>
                        ))}
                      </select>
                    </div>
                    {activeMobileMrp && (
                      <div style={{ marginTop: '0.6rem', display: 'grid', gap: '0.55rem' }}>
                        <p className="sm-meta" style={{ margin: 0 }}>
                          <strong>{activeMobileMrp.totalRecipes}</strong> recipes · <strong>{activeMobileMrp.totalUniqueMaterials}</strong> materials ·{' '}
                          <span style={{ color: activeMobileMrp.shortageCount > 0 ? '#b91c1c' : '#0f766e', fontWeight: 600 }}>
                            {activeMobileMrp.shortageCount} shortage(s)
                          </span>
                          {activeMobileMrp.missingMappingCount > 0 ? ` · ${activeMobileMrp.missingMappingCount} missing map` : ''}
                        </p>
                        {activeMobileMrp.costVisible && activeMobileMrp.estimatedRequirementValue != null && (
                          <p className="sm-meta" style={{ margin: 0 }}>
                            Est. Req. Value: <strong>{activeMobileMrp.currency} {activeMobileMrp.estimatedRequirementValue.toLocaleString()}</strong>
                          </p>
                        )}
                        <div className="sm-field">
                          <label htmlFor="m-p2b-search">Search Materials</label>
                          <input
                            id="m-p2b-search"
                            value={mrpMatQuery}
                            onChange={(e) => setMrpMatQuery(e.target.value)}
                            placeholder="Filter by material or recipe..."
                          />
                        </div>
                        <div className="sm-field">
                          <label htmlFor="m-p2b-scale">Simulate Guest Scale ({mrpGuestScalePct}%) &amp; Buffer ({mrpBufferPct}%)</label>
                          <input
                            id="m-p2b-scale"
                            type="range"
                            min={50}
                            max={180}
                            step={5}
                            value={mrpGuestScalePct}
                            onChange={(e) => setMrpGuestScalePct(Number(e.target.value))}
                          />
                          <input
                            type="range"
                            min={0}
                            max={20}
                            step={1}
                            value={mrpBufferPct}
                            onChange={(e) => setMrpBufferPct(Number(e.target.value))}
                          />
                          <button
                            type="button"
                            className="sm-btn"
                            onClick={async () => {
                              const res = await simulateMrpRun(activeMobileMrp.id, {
                                guestScaleFactor: mrpGuestScalePct / 100,
                                additionalAllowancePercent: 0,
                                bufferPercent: mrpBufferPct,
                                yieldOverridePercent: 0,
                                priceIncreasePercent: 0,
                                preparedStockMode: 'ADJUSTED_SCENARIO',
                                lineOverrides: [],
                              });
                              setMrpSimResult(res);
                            }}
                          >
                            Simulate MRP
                          </button>
                          {mrpSimResult && (
                            <p className="sm-meta" style={{ marginTop: '0.3rem' }}>
                              Simulated Guests: <strong>{mrpSimResult.plannedGuests}</strong> · Shortages: <strong>{mrpSimResult.shortageCount}</strong>
                            </p>
                          )}
                        </div>
                        <ul className="sm-list">
                          {(mrpSimResult?.simulatedLines ?? activeMobileMrp.lines)
                            .filter((l) => {
                              const q = mrpMatQuery.trim().toLowerCase();
                              if (!q) return true;
                              return (
                                l.materialDescription.toLowerCase().includes(q) ||
                                (l.materialCode ?? '').toLowerCase().includes(q) ||
                                l.contributingRecipes.some((r) => r.toLowerCase().includes(q))
                              );
                            })
                            .slice(0, 15)
                            .map((l) => (
                              <li key={l.id} className="sm-row" style={{ padding: '0.4rem 0' }}>
                                <div>
                                  <strong>{l.materialDescription}</strong>
                                  <div className="sm-meta">
                                    {l.contributingRecipes.slice(0, 2).join(', ')} · Due {l.requiredDateBucket}
                                  </div>
                                </div>
                                <div style={{ textAlign: 'right' }}>
                                  <strong>{l.grossQty} {l.baseUom}</strong>
                                  <div className="sm-meta" style={{ color: (l.indicativeCurrentShortageQty ?? 0) > 0 ? '#b91c1c' : '#0f766e' }}>
                                    {!l.isResolvedMaterial
                                      ? 'Missing Map'
                                      : l.eligibleStockQty == null
                                        ? 'Stock Unknown'
                                        : (l.indicativeCurrentShortageQty ?? 0) > 0
                                          ? `Short ${l.indicativeCurrentShortageQty} ${l.baseUom}`
                                          : 'Covered'}
                                  </div>
                                </div>
                              </li>
                            ))}
                        </ul>
                        <div className="sm-actions">
                          {activeMobileMrp.status === 'CALCULATED' && activeMobileMrp.readiness?.canSubmitForApproval && (
                            <button
                              type="button"
                              className="sm-btn sm-btn--primary"
                              onClick={async () => {
                                const updated = await submitMrpForApproval(activeMobileMrp.id);
                                setActiveMobileMrp(updated);
                                setStatusMsg(`Submitted ${updated.mrpCode} for approval.`);
                              }}
                            >
                              Submit MRP Approval
                            </button>
                          )}
                          {activeMobileMrp.status === 'PENDING_APPROVAL' && (
                            <button
                              type="button"
                              className="sm-btn sm-btn--primary"
                              onClick={async () => {
                                const updated = await decideMrpApproval(activeMobileMrp.id, 'APPROVE', 'Approved via Mobile');
                                setActiveMobileMrp(updated);
                                setStatusMsg(`Approved ${updated.mrpCode}.`);
                              }}
                            >
                              Approve MRP
                            </button>
                          )}
                        </div>
                      </div>
                    )}
                  </>
                )}
              </section>
            )}

            {tab === 'procurement' && (
              <section className="sm-card" data-testid="mobile-p2c-procurement">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                  <h2 style={{ margin: 0 }}>Event Material Readiness &amp; Procurement</h2>
                  <button type="button" className="sm-btn" onClick={handleOpenProcurementTab}>Refresh</button>
                </div>
                {procLoading ? (
                  <p className="sm-meta">Loading procurement readiness...</p>
                ) : mobileProcPlans.length === 0 ? (
                  <p className="sm-meta">No event procurement plans yet. Create one from an approved Event MRP in Cloud Recipe Studio.</p>
                ) : (
                  <>
                    <div className="sm-field" style={{ marginBottom: '0.5rem' }}>
                      <label htmlFor="m-p2c-plan">Procurement Plan</label>
                      <select
                        id="m-p2c-plan"
                        value={activeMobileProc?.summary.id ?? ''}
                        onChange={async (e) => {
                          if (!e.target.value) return;
                          setProcLoading(true);
                          try {
                            setActiveMobileProc(await getProcurementPlan(e.target.value));
                          } finally {
                            setProcLoading(false);
                          }
                        }}
                      >
                        {mobileProcPlans.map((p) => (
                          <option key={p.id} value={p.id}>
                            {p.planCode} · {p.name} ({p.status})
                          </option>
                        ))}
                      </select>
                    </div>
                    {activeMobileProc && (
                      <div style={{ display: 'grid', gap: '0.55rem' }}>
                        <div className="sm-meta">
                          <strong>{activeMobileProc.summary.primaryEventName || activeMobileProc.summary.name}</strong> ·{' '}
                          Physical Ready: <strong>{activeMobileProc.summary.physicalReadinessPercent}%</strong> ·{' '}
                          Projected Ready: <strong>{activeMobileProc.summary.projectedReadinessPercent}%</strong> ·{' '}
                          Shortages: <strong>{activeMobileProc.summary.criticalShortageCount}</strong>
                        </div>
                        <label style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.82rem' }}>
                          <input type="checkbox" checked={procShortagesOnly} onChange={(e) => setProcShortagesOnly(e.target.checked)} />
                          Show shortage &amp; delivery-risk items only
                        </label>
                        <ul className="sm-list">
                          {activeMobileProc.lines
                            .filter((l) => !procShortagesOnly || l.netUncoveredQty > 0 || !l.isDeliveryFeasible)
                            .map((l) => (
                              <li key={l.id} style={{ padding: '0.45rem 0', borderBottom: '1px solid #e2e8f0' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', gap: '0.5rem' }}>
                                  <div>
                                    <strong>{l.materialDescription}</strong>
                                    <div className="sm-meta">
                                      Req {l.grossRequirementQty} {l.baseUom} · Res {l.activeEventReservationQty} · Inc PO {l.incomingPoAllocatedQty} · Prop ITO {l.proposedItoQty} · Gap {l.netUncoveredQty}
                                    </div>
                                    <div className="sm-meta">
                                      Status: <strong>{l.readinessStatus}</strong>
                                      {activeMobileProc.costsVisible && l.recommendedSupplierName
                                        ? ` · Supplier: ${l.recommendedSupplierName}`
                                        : ''}
                                    </div>
                                  </div>
                                  <div className="sm-actions">
                                    {l.proposedItoQty > 0 && l.itoCandidates[0]?.samePlant311Eligible && l.destinationLocationId && (
                                      <button
                                        type="button"
                                        className="sm-btn"
                                        onClick={async () => {
                                          await createProcurementItoDraft(activeMobileProc.summary.id, {
                                            procurementLineId: l.id,
                                            fromLocationId: l.itoCandidates[0].sourceLocationId,
                                            toLocationId: l.destinationLocationId!,
                                            quantity: l.proposedItoQty,
                                            idempotencyKey: `mob-ito-${l.id}`,
                                          });
                                          setActiveMobileProc(await getProcurementPlan(activeMobileProc.summary.id));
                                          setStatusMsg(`ITO draft requested for ${l.materialDescription}.`);
                                        }}
                                      >
                                        ITO Draft
                                      </button>
                                    )}
                                    {l.recommendedPurchaseQty > 0 && l.recommendedSupplierId && (
                                      <button
                                        type="button"
                                        className="sm-btn"
                                        onClick={async () => {
                                          await createProcurementPoDraft(activeMobileProc.summary.id, {
                                            procurementLineIds: [l.id],
                                            supplierId: l.recommendedSupplierId!,
                                            idempotencyKey: `mob-po-${l.id}`,
                                          });
                                          setActiveMobileProc(await getProcurementPlan(activeMobileProc.summary.id));
                                          setStatusMsg(`PO draft created for ${l.materialDescription} (pending approval).`);
                                        }}
                                      >
                                        PO Draft
                                      </button>
                                    )}
                                  </div>
                                </div>
                              </li>
                            ))}
                        </ul>
                        <div className="sm-actions">
                          {activeMobileProc.summary.status === 'DRAFT' && (
                            <button
                              type="button"
                              className="sm-btn sm-btn--primary"
                              onClick={async () => {
                                const updated = await submitProcurementPlanForApproval(activeMobileProc.summary.id);
                                setActiveMobileProc(updated);
                                setStatusMsg(`Submitted ${updated.summary.planCode} for approval.`);
                              }}
                            >
                              Submit Procurement Approval
                            </button>
                          )}
                          {activeMobileProc.summary.status === 'PENDING_APPROVAL' && (
                            <button
                              type="button"
                              className="sm-btn sm-btn--primary"
                              onClick={async () => {
                                const updated = await decideProcurementPlanApproval(activeMobileProc.summary.id, 'APPROVE', 'Approved via Mobile');
                                setActiveMobileProc(updated);
                                setStatusMsg(`Approved ${updated.summary.planCode}.`);
                              }}
                            >
                              Approve Plan
                            </button>
                          )}
                        </div>
                      </div>
                    )}
                  </>
                )}
              </section>
            )}

            {tab === 'pnl' && (
              <section className="sm-card">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                  <h2 style={{ margin: 0 }}>Event Food Cost &amp; P&amp;L Summary</h2>
                  <button type="button" className="sm-btn" onClick={handleOpenPnlTab} disabled={mobileFinLoading}>
                    {mobileFinLoading ? 'Loading...' : 'Refresh'}
                  </button>
                </div>
                {mobileFinPlans.length > 0 && (
                  <div className="sm-field" style={{ marginBottom: '0.6rem' }}>
                    <label htmlFor="m-p3a-plan">Event Financial Plan</label>
                    <select
                      id="m-p3a-plan"
                      value={mobileFinSummary?.financialPlanId ?? ''}
                      onChange={async (e) => {
                        const id = e.target.value;
                        if (!id) return;
                        setMobileFinLoading(true);
                        try {
                          const s = await getMobileFinancialSummary(id);
                          setMobileFinSummary(s);
                          setMobileFinGuests(s.plannedGuests);
                          setMobileFinSim(null);
                        } catch (err) {
                          setErrMsg(errorText(err));
                        } finally {
                          setMobileFinLoading(false);
                        }
                      }}
                    >
                      {mobileFinPlans.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.planCode} · {p.name} ({p.status})
                        </option>
                      ))}
                    </select>
                  </div>
                )}
                {mobileFinSummary && mobileFinSummary.financialPlanId ? (
                  <>
                    <div className="sm-meta" style={{ marginBottom: '0.5rem' }}>
                      <strong>{mobileFinSummary.eventName}</strong> · {mobileFinSummary.plannedGuests} guests · Readiness:{' '}
                      <strong>{mobileFinSummary.financialReadiness}</strong> ({mobileFinSummary.openExceptionsCount} open alerts)
                    </div>
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem', marginBottom: '0.75rem' }}>
                      <div className="sm-card" style={{ padding: '0.55rem' }}>
                        <div className="sm-meta">Food Cost</div>
                        <strong>{mobileFinSummary.reportingCurrency} {mobileFinSummary.foodCost.toFixed(2)}</strong>
                      </div>
                      <div className="sm-card" style={{ padding: '0.55rem' }}>
                        <div className="sm-meta">Beverage Cost</div>
                        <strong>{mobileFinSummary.reportingCurrency} {mobileFinSummary.beverageCost.toFixed(2)}</strong>
                      </div>
                      <div className="sm-card" style={{ padding: '0.55rem' }}>
                        <div className="sm-meta">Approved Budget</div>
                        <strong>{mobileFinSummary.reportingCurrency} {mobileFinSummary.approvedBudget.toFixed(2)}</strong>
                      </div>
                      <div className="sm-card" style={{ padding: '0.55rem' }}>
                        <div className="sm-meta">Budget Utilization</div>
                        <strong>{mobileFinSummary.budgetUtilizationPercent.toFixed(1)}%</strong>
                      </div>
                      {mobileFinSummary.eventContribution != null && (
                        <div className="sm-card" style={{ padding: '0.55rem', gridColumn: '1 / -1' }}>
                          <div className="sm-meta">Event Contribution (Authorized View)</div>
                          <strong>
                            {mobileFinSummary.reportingCurrency} {mobileFinSummary.eventContribution.toFixed(2)} (
                            {(mobileFinSummary.contributionMarginPercent ?? 0).toFixed(2)}%)
                          </strong>
                        </div>
                      )}
                    </div>
                    <div className="sm-field" style={{ marginBottom: '0.6rem' }}>
                      <label htmlFor="m-p3a-guests">Guest Count Simulation: <strong>{mobileFinGuests}</strong></label>
                      <input
                        id="m-p3a-guests"
                        type="range"
                        min={50}
                        max={1200}
                        step={10}
                        value={mobileFinGuests}
                        onChange={(e) => setMobileFinGuests(Number(e.target.value))}
                      />
                    </div>
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
                      <button
                        type="button"
                        className="sm-btn"
                        disabled={mobileFinLoading}
                        onClick={async () => {
                          if (!mobileFinSummary.financialPlanId) return;
                          setMobileFinLoading(true);
                          try {
                            const res = await simulateEventFinancials(mobileFinSummary.financialPlanId, {
                              plannedGuests: mobileFinGuests,
                              guaranteedGuests: mobileFinGuests,
                              sellingPricePerGuest: 350,
                              fixedEventRevenue: 0,
                              additionalGuestRevenue: 0,
                              discountPercent: 0,
                              foodCostPerGuest: mobileFinSummary.plannedGuests > 0 ? mobileFinSummary.foodCost / mobileFinSummary.plannedGuests : 90,
                              beverageCostPerGuest: mobileFinSummary.plannedGuests > 0 ? mobileFinSummary.beverageCost / mobileFinSummary.plannedGuests : 25,
                              ingredientPriceChangePercent: 0,
                              manpowerCost: 15000,
                              equipmentCost: 5000,
                              decorationCost: 10000,
                              externalServiceCost: 0,
                              otherDirectCost: 5000,
                              allocatedOverhead: 0,
                              targetContributionMarginPercent: 40,
                              saveScenario: false,
                            });
                            setMobileFinSim(res);
                          } catch (err) {
                            setErrMsg(errorText(err));
                          } finally {
                            setMobileFinLoading(false);
                          }
                        }}
                      >
                        Simulate Guests
                      </button>
                      <button
                        type="button"
                        className="sm-btn"
                        disabled={mobileFinLoading}
                        onClick={async () => {
                          if (!mobileFinSummary.financialPlanId) return;
                          setMobileFinLoading(true);
                          try {
                            await submitFinancialPlanForApproval(mobileFinSummary.financialPlanId);
                            setStatusMsg('Submitted event financial plan for approval.');
                            await handleOpenPnlTab();
                          } catch (err) {
                            setErrMsg(errorText(err));
                          } finally {
                            setMobileFinLoading(false);
                          }
                        }}
                      >
                        Submit Approval
                      </button>
                      <button
                        type="button"
                        className="sm-btn"
                        disabled={mobileFinLoading}
                        onClick={async () => {
                          if (!mobileFinSummary.financialPlanId) return;
                          setMobileFinLoading(true);
                          try {
                            await decideFinancialPlanApproval(mobileFinSummary.financialPlanId, 'APPROVE', 'Approved from Mobile');
                            setStatusMsg('Approved event financial plan.');
                            await handleOpenPnlTab();
                          } catch (err) {
                            setErrMsg(errorText(err));
                          } finally {
                            setMobileFinLoading(false);
                          }
                        }}
                      >
                        Approve
                      </button>
                    </div>
                    {mobileFinSim && (
                      <div className="sm-meta" style={{ marginTop: '0.6rem' }}>
                        Simulated ({mobileFinSim.plannedGuests} guests): Est. Total Cost {mobileFinSim.reportingCurrency}{' '}
                        {mobileFinSim.totalEventCost.toFixed(2)} · Break-Even {mobileFinSim.breakEvenGuests ?? 'N/A'} guests
                      </div>
                    )}
                  </>
                ) : (
                  <p className="sm-meta">No event financial plan found yet. Create a plan in Cloud Event P&amp;L Studio.</p>
                )}
              </section>
            )}

            {tab === 'analytics' && (
              <section className="sm-card" data-testid="mobile-culinary-ops">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.6rem' }}>
                  <h2 style={{ margin: 0 }}>Daily Culinary Ops (Phase 3B)</h2>
                  <button type="button" className="sm-btn" disabled={opsLoading} onClick={handleOpenOpsTab}>
                    {opsLoading ? 'Syncing...' : 'Sync'}
                  </button>
                </div>
                {opsSummary && (
                  <>
                    <div className="sm-grid" style={{ gap: '0.5rem', marginBottom: '0.75rem' }}>
                      <div className="sm-kpi">
                        <span>Expected Covers</span>
                        <strong>{opsSummary.dailyExpectedCovers}</strong>
                      </div>
                      <div className="sm-kpi">
                        <span>Shortage / Waste Alerts</span>
                        <strong>{opsSummary.activeShortageAlerts} / {opsSummary.activeWasteAlerts}</strong>
                      </div>
                      <div className="sm-kpi">
                        <span>Approval Tasks</span>
                        <strong>{opsSummary.pendingApprovalTasks}</strong>
                      </div>
                      {opsSummary.canViewCosts && (
                        <div className="sm-kpi">
                          <span>Daily Food Cost %</span>
                          <strong>{opsSummary.dailyFoodCostPercent ?? '—'}% (Target {opsSummary.targetFoodCostPercent}%)</strong>
                        </div>
                      )}
                    </div>
                    <div className="sm-meta" style={{ marginBottom: '0.6rem' }}>
                      <strong>Production &amp; Buffet Guidance:</strong>
                      <ul style={{ margin: '0.25rem 0', paddingLeft: '1.1rem' }}>
                        {opsSummary.productionRecommendations.map((x) => <li key={x}>{x}</li>)}
                        {opsSummary.buffetReplenishmentNotes.map((x) => <li key={x}>{x}</li>)}
                        {opsSummary.eventProductionSummaries.map((x) => <li key={x}>{x}</li>)}
                      </ul>
                    </div>

                    <div style={{ borderTop: '1px solid #e2e8f0', paddingTop: '0.6rem', marginTop: '0.6rem' }}>
                      <strong style={{ fontSize: '0.85rem' }}>Record Kitchen Waste / Deviation (Approval-Gated)</strong>
                      <div className="sm-grid" style={{ gap: '0.4rem', marginTop: '0.4rem' }}>
                        <input className="sm-input" placeholder="Ingredient" value={mobWasteIng} onChange={(e) => setMobWasteIng(e.target.value)} />
                        <select className="sm-input" value={mobWasteCat} onChange={(e) => setMobWasteCat(e.target.value)}>
                          <option value="PRODUCTION_WASTE">Production Waste</option>
                          <option value="OVERPRODUCTION">Overproduction</option>
                          <option value="BUFFET_LEFTOVERS">Buffet Leftovers</option>
                          <option value="SPOILAGE">Spoilage</option>
                          <option value="YIELD_LOSS">Yield Loss / Deviation</option>
                        </select>
                        <input className="sm-input" type="number" value={mobWasteQty} onChange={(e) => setMobWasteQty(Number(e.target.value))} />
                        <button
                          type="button"
                          className="sm-btn sm-btn--primary"
                          onClick={async () => {
                            if (!mobWasteIng.trim()) return;
                            try {
                              await recordCulinaryWaste({
                                mealPeriod: 'All Day',
                                wasteCategory: mobWasteCat,
                                ingredientName: mobWasteIng,
                                quantity: mobWasteQty,
                                uom: 'KG',
                                estimatedCost: 0,
                                coversServed: Math.max(1, Math.round(opsSummary.dailyExpectedCovers)),
                                sourceChannel: 'MOBILE',
                                idempotencyKey: `MOB-${Date.now()}-${mobWasteIng.trim()}`,
                              });
                              setStatusMsg(`Logged ${mobWasteCat} for ${mobWasteIng} (pending approval).`);
                              setMobWasteIng('');
                            } catch (err) {
                              setErrMsg(errorText(err));
                            }
                          }}
                        >
                          Submit Waste Log
                        </button>
                      </div>
                    </div>

                    {opsSummary.chefRecommendations.length > 0 && (
                      <div style={{ borderTop: '1px solid #e2e8f0', paddingTop: '0.6rem', marginTop: '0.6rem' }}>
                        <strong style={{ fontSize: '0.85rem' }}>Chef Recommendations</strong>
                        {opsSummary.chefRecommendations.map((r) => (
                          <div key={r.id} className="sm-row" style={{ padding: '0.35rem 0' }}>
                            <div>
                              <strong style={{ fontSize: '0.8rem' }}>{r.title}</strong>
                              <div className="sm-meta">{r.proposedAction} ({r.status})</div>
                            </div>
                            {opsSummary.canViewCosts && r.status === 'NEW' && (
                              <button
                                type="button"
                                className="sm-btn"
                                onClick={async () => {
                                  await decideCulinaryRecommendation(r.id, { status: 'APPROVED', comment: 'Approved on Mobile' });
                                  await handleOpenOpsTab();
                                }}
                              >
                                Approve
                              </button>
                            )}
                          </div>
                        ))}
                      </div>
                    )}

                    <div style={{ borderTop: '1px solid #e2e8f0', paddingTop: '0.6rem', marginTop: '0.6rem' }}>
                      <strong style={{ fontSize: '0.85rem' }}>Mobile Culinary Copilot</strong>
                      <div style={{ display: 'flex', gap: '0.4rem', marginTop: '0.35rem' }}>
                        <input className="sm-input" style={{ flex: 1 }} value={mobCopilotQ} onChange={(e) => setMobCopilotQ(e.target.value)} />
                        <button
                          type="button"
                          className="sm-btn"
                          onClick={async () => {
                            const ans = await queryCulinaryCopilot({ question: mobCopilotQ, contextModule: 'MOBILE_DAILY_OPS' });
                            setMobCopilotAns(ans);
                          }}
                        >
                          Ask
                        </button>
                      </div>
                      {mobCopilotAns && (
                        <p className="sm-meta" style={{ marginTop: '0.4rem' }}>
                          {mobCopilotAns.directAnswer} — <em>{mobCopilotAns.recommendedAction}</em>
                        </p>
                      )}
                    </div>
                  </>
                )}
              </section>
            )}

            {tab === 'production' && (
              <div data-testid="mobile-culinary-production-ops" style={{ display: 'grid', gap: '0.75rem' }}>
                <section className="sm-card">
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
                    <div>
                      <p className="sm-eyebrow">PHASE 4A · SILA STORE KITCHEN OPS</p>
                      <h2 style={{ margin: '0.15rem 0' }}>Kitchen Production &amp; Execution</h2>
                    </div>
                    <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
                      <span
                        className="sm-meta"
                        style={{
                          padding: '0.35rem 0.65rem',
                          borderRadius: 999,
                          fontWeight: 700,
                          background: isOnline ? '#dcfce7' : '#fee2e2',
                          color: isOnline ? '#166534' : '#991b1b',
                        }}
                      >
                        {isOnline ? 'ONLINE' : 'OFFLINE QUEUE MODE'}
                      </span>
                      <button
                        type="button"
                        className="sm-btn sm-btn--primary"
                        style={{ minHeight: 44 }}
                        disabled={prodLoading}
                        onClick={() => loadMobileProductionData()}
                      >
                        {prodLoading ? 'Refreshing...' : 'Refresh'}
                      </button>
                    </div>
                  </div>

                  {prodSummary && (
                    <div
                      className="sm-grid"
                      style={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(auto-fit, minmax(120px, 1fr))',
                        gap: '0.5rem',
                        marginTop: '0.65rem',
                      }}
                    >
                      <div className="sm-kpi">
                        <span>My Tasks</span>
                        <strong>{prodSummary.myTasksCount}</strong>
                      </div>
                      <div className="sm-kpi">
                        <span>Active Batches</span>
                        <strong>{prodSummary.activeBatchesCount}</strong>
                      </div>
                      <div className="sm-kpi">
                        <span>Picking Pending</span>
                        <strong>{prodSummary.pendingPickingCount}</strong>
                      </div>
                      <div className="sm-kpi">
                        <span>Quality Pending</span>
                        <strong>{prodSummary.pendingQualityChecksCount}</strong>
                      </div>
                      <div className="sm-kpi">
                        <span>Exceptions</span>
                        <strong style={{ color: prodSummary.openExceptionsCount > 0 ? '#dc2626' : '#0f172a' }}>
                          {prodSummary.openExceptionsCount}
                        </strong>
                      </div>
                    </div>
                  )}
                </section>

                <section className="sm-card" data-testid="mobile-prod-offline-queue">
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
                    <div>
                      <strong>Offline Pending-Action Queue ({offlineQueue.length})</strong>
                      <div className="sm-meta">
                        Idempotent offline buffer for status, task, QC &amp; picking updates. Final stock issue postings strictly require verified online sync.
                      </div>
                    </div>
                    <button
                      type="button"
                      className="sm-btn sm-btn--primary"
                      style={{ minHeight: 44 }}
                      disabled={!isOnline || syncingQueue || offlineQueue.length === 0}
                      onClick={handleSyncOfflineQueue}
                    >
                      {syncingQueue ? 'Syncing...' : `Sync Queue (${offlineQueue.length})`}
                    </button>
                  </div>
                  {offlineQueue.length > 0 && (
                    <ul className="sm-list" style={{ marginTop: '0.5rem' }}>
                      {offlineQueue.map((q) => (
                        <li key={q.id} className="sm-row" style={{ padding: '0.4rem 0', gap: '0.5rem' }}>
                          <div style={{ flex: 1 }}>
                            <strong>{q.label}</strong>
                            <div className="sm-meta">
                              {q.kind} · {q.syncStatus} · <code>{q.idempotencyKey}</code>
                              {q.error ? ` · ${q.error}` : ''}
                            </div>
                          </div>
                          <button
                            type="button"
                            className="sm-btn"
                            style={{ minHeight: 40 }}
                            onClick={() => persistOfflineQueue(offlineQueue.filter((item) => item.id !== q.id))}
                          >
                            Drop
                          </button>
                        </li>
                      ))}
                    </ul>
                  )}
                </section>

                <section className="sm-card" data-testid="mobile-prod-kds-batches">
                  <h3 style={{ margin: '0 0 0.5rem' }}>Digital Production Sheet &amp; KDS Batches ({prodKdsBatches.length})</h3>
                  {prodKdsBatches.length === 0 ? (
                    <p className="sm-meta">No active KDS batches found. Release a production order in Cloud Production Studio.</p>
                  ) : (
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))', gap: '0.6rem' }}>
                      {prodKdsBatches.map((batch) => {
                        const isDelayed = new Date(batch.plannedFinishUtc).getTime() < Date.now() && batch.batchStatus !== 'PRODUCTION_COMPLETED';
                        return (
                          <div
                            key={batch.id}
                            style={{
                              border: selectedBatchId === batch.id ? '2px solid #2563eb' : '1px solid #cbd5e1',
                              borderLeft: isDelayed || batch.batchStatus === 'ON_HOLD' ? '5px solid #dc2626' : '5px solid #059669',
                              borderRadius: '0.65rem',
                              padding: '0.65rem',
                              background: '#fff',
                            }}
                          >
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '0.4rem' }}>
                              <div>
                                <strong style={{ fontSize: '0.95rem' }}>{batch.batchNumber}</strong>
                                <div className="sm-meta">
                                  {batch.batchType} · Chef: {batch.assignedChefName || 'Unassigned'} · Kitchen: {batch.kitchenId || 'Main'}
                                </div>
                              </div>
                              <span
                                style={{
                                  fontSize: '0.75rem',
                                  fontWeight: 700,
                                  padding: '0.2rem 0.5rem',
                                  borderRadius: 999,
                                  background: batch.batchStatus === 'ON_HOLD' ? '#fee2e2' : '#e0f2fe',
                                  color: batch.batchStatus === 'ON_HOLD' ? '#991b1b' : '#075985',
                                }}
                              >
                                {batch.batchStatus}
                              </span>
                            </div>
                            <div className="sm-meta" style={{ marginTop: '0.35rem' }}>
                              Qty: <strong>{batch.plannedBatchQty} {batch.productionUom}</strong> · Quality: <strong>{batch.qualityStatus}</strong>
                              {batch.allergenSummary ? ` · Allergens: ${batch.allergenSummary}` : ''}
                              {isDelayed ? ' · DELAYED' : ''}
                            </div>
                            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.4rem', marginTop: '0.55rem' }}>
                              <button
                                type="button"
                                className="sm-btn"
                                style={{ minHeight: 44 }}
                                onClick={async () => {
                                  setSelectedBatchId(batch.id);
                                  const [tasks, tr] = await Promise.all([
                                    listBatchTasks(batch.id).catch(() => []),
                                    getProductionBatchTraceability(batch.id).catch(() => null),
                                  ]);
                                  setProdTasks(tasks);
                                  setSelectedTrace(tr);
                                }}
                              >
                                Sheet &amp; QR
                              </button>
                              <button
                                type="button"
                                className="sm-btn"
                                style={{ minHeight: 44 }}
                                onClick={() => handleMobileBatchAction(batch, 'START_PREP')}
                              >
                                Start Prep
                              </button>
                              <button
                                type="button"
                                className="sm-btn"
                                style={{ minHeight: 44 }}
                                onClick={() => handleMobileBatchAction(batch, 'START_COOKING')}
                              >
                                Cook
                              </button>
                              <button
                                type="button"
                                className="sm-btn sm-btn--primary"
                                style={{ minHeight: 44 }}
                                onClick={() => handleMobileBatchAction(batch, 'COMPLETE_BATCH')}
                              >
                                Complete
                              </button>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </section>

                {selectedTrace && (
                  <section className="sm-card" data-testid="mobile-prod-qr-traceability">
                    <h3 style={{ margin: '0 0 0.4rem' }}>Batch Traceability &amp; QR Label ({selectedTrace.batchNumber})</h3>
                    <div className="sm-meta">
                      <strong>QR Payload:</strong> <code>{selectedTrace.qrPayloadJson}</code>
                    </div>
                    <div className="sm-meta" style={{ marginTop: '0.25rem' }}>
                      Recipe: <strong>{selectedTrace.recipeName}</strong> (v{selectedTrace.recipeVersion})
                    </div>
                    <div className="sm-meta" style={{ marginTop: '0.25rem' }}>
                      Backward: {selectedTrace.backwardTraceSummary}
                    </div>
                    <div className="sm-meta" style={{ marginTop: '0.25rem' }}>
                      Forward: {selectedTrace.forwardTraceSummary} · Dispatches: {selectedTrace.outletDispatches.length} · QC Results: {selectedTrace.qualityResults.length}
                    </div>
                  </section>
                )}

                <section className="sm-card" data-testid="mobile-prod-tasks">
                  <h3 style={{ margin: '0 0 0.45rem' }}>Sequenced Production Tasks ({prodTasks.length})</h3>
                  {prodTasks.slice(0, 8).map((t) => (
                    <div key={t.id} className="sm-row" style={{ padding: '0.45rem 0', gap: '0.5rem', flexWrap: 'wrap' }}>
                      <div style={{ flex: 1, minWidth: 180 }}>
                        <strong>#{t.sequenceOrder} {t.taskType}</strong>
                        <div className="sm-meta">{t.instructions} · {t.status} · {t.plannedDurationMinutes} min</div>
                      </div>
                      <div style={{ display: 'flex', gap: '0.4rem' }}>
                        {t.status === 'PENDING' && (
                          <button
                            type="button"
                            className="sm-btn"
                            style={{ minHeight: 44 }}
                            onClick={async () => {
                              if (!isOnline) {
                                enqueueOfflineAction('TASK_UPDATE', `Start task #${t.sequenceOrder} ${t.taskType}`, { taskId: t.id, status: 'IN_PROGRESS' });
                                return;
                              }
                              try {
                                await updateProductionTaskStatus(t.id, 'IN_PROGRESS');
                                await loadMobileProductionData();
                              } catch (err) {
                                setErrMsg(errorText(err));
                              }
                            }}
                          >
                            Start
                          </button>
                        )}
                        {t.status !== 'COMPLETED' && (
                          <button
                            type="button"
                            className="sm-btn sm-btn--primary"
                            style={{ minHeight: 44 }}
                            onClick={async () => {
                              if (!isOnline) {
                                enqueueOfflineAction('TASK_UPDATE', `Complete task #${t.sequenceOrder} ${t.taskType}`, { taskId: t.id, status: 'COMPLETED' });
                                return;
                              }
                              try {
                                await updateProductionTaskStatus(t.id, 'COMPLETED');
                                await loadMobileProductionData();
                              } catch (err) {
                                setErrMsg(errorText(err));
                              }
                            }}
                          >
                            Complete
                          </button>
                        )}
                      </div>
                    </div>
                  ))}
                </section>

                <section className="sm-card" data-testid="mobile-prod-picking-issue">
                  <h3 style={{ margin: '0 0 0.45rem' }}>FEFO Picking &amp; Material Issue ({prodPicks.length})</h3>
                  <div style={{ display: 'flex', gap: '0.4rem', marginBottom: '0.5rem', flexWrap: 'wrap' }}>
                    <input
                      className="sm-input"
                      style={{ flex: 1, minWidth: 180, minHeight: 44 }}
                      placeholder="Scan Barcode / QR / Batch #"
                      value={mobScanCode}
                      onChange={(e) => setMobScanCode(e.target.value)}
                    />
                  </div>
                  {!isOnline && (
                    <p className="sm-meta" style={{ color: '#991b1b', fontWeight: 600 }}>
                      Offline guard active: picking can be queued, but final Material Issue stock posting requires a verified online connection.
                    </p>
                  )}
                  {prodPicks.slice(0, 8).map((p) => (
                    <div key={p.id} className="sm-row" style={{ padding: '0.45rem 0', gap: '0.5rem', flexWrap: 'wrap' }}>
                      <div style={{ flex: 1, minWidth: 180 }}>
                        <strong>{p.materialName}</strong>
                        <div className="sm-meta">
                          Strategy: {p.pickingStrategy} · Batch: <code>{p.inventoryBatchNumber || 'N/A'}</code> · Req: {p.requiredQty}{' '}
                          {p.inventoryUom} · Picked: {p.pickedQty} · {p.status}
                        </div>
                      </div>
                      <div style={{ display: 'flex', gap: '0.4rem' }}>
                        {p.status !== 'CONFIRMED' && (
                          <button
                            type="button"
                            className="sm-btn"
                            style={{ minHeight: 44 }}
                            onClick={async () => {
                              const batchNum = mobScanCode || p.inventoryBatchNumber || undefined;
                              if (!isOnline) {
                                enqueueOfflineAction('PICKING_CONFIRM', `Pick ${p.materialName}`, {
                                  pickingLineId: p.id,
                                  pickedQty: p.requiredQty,
                                  inventoryBatchNumber: batchNum,
                                });
                                return;
                              }
                              try {
                                await confirmProductionPickingLine(p.id, p.requiredQty, batchNum);
                                await loadMobileProductionData();
                              } catch (err) {
                                setErrMsg(errorText(err));
                              }
                            }}
                          >
                            Confirm Pick
                          </button>
                        )}
                        <button
                          type="button"
                          className="sm-btn sm-btn--primary"
                          style={{ minHeight: 44 }}
                          disabled={!isOnline}
                          onClick={async () => {
                            try {
                              await issueMaterialToProduction({
                                productionOrderId: p.productionOrderId,
                                productionBatchId: p.productionBatchId ?? undefined,
                                fromStoreLocationId: p.storeLocationId || 'MAIN-STORE',
                                toKitchenLocationId: prodKdsBatches.find((b) => b.id === (p.productionBatchId || selectedBatchId))?.kitchenId || 'MAIN-KITCHEN',
                                materialCode: p.materialCode,
                                issueQty: p.pickedQty > 0 ? p.pickedQty : p.requiredQty,
                                uom: p.inventoryUom,
                                idempotencyKey: `MOB-ISS-${p.id}`,
                                comment: `Mobile issue (${p.inventoryBatchNumber || 'FEFO'})`,
                              });
                              setStatusMsg(`Issued ${p.materialName} to production.`);
                              await loadMobileProductionData();
                            } catch (err) {
                              setErrMsg(errorText(err));
                            }
                          }}
                        >
                          Issue Stock
                        </button>
                      </div>
                    </div>
                  ))}
                </section>

                <section className="sm-card" data-testid="mobile-prod-quality-check">
                  <h3 style={{ margin: '0 0 0.45rem' }}>Food Safety &amp; Quality Checkpoint</h3>
                  <div className="sm-grid" style={{ display: 'grid', gap: '0.45rem' }}>
                    <input
                      className="sm-input"
                      style={{ minHeight: 44 }}
                      placeholder="Checkpoint name (e.g. Cooking Core Temp)"
                      value={mobQcName}
                      onChange={(e) => setMobQcName(e.target.value)}
                    />
                    <input
                      className="sm-input"
                      style={{ minHeight: 44 }}
                      placeholder="Expected threshold (e.g. >= 75 C)"
                      value={mobQcExpected}
                      onChange={(e) => setMobQcExpected(e.target.value)}
                    />
                    <input
                      className="sm-input"
                      style={{ minHeight: 44 }}
                      placeholder="Measured value / Temp (°C)"
                      value={mobQcValue}
                      onChange={(e) => setMobQcValue(e.target.value)}
                    />
                    <div style={{ display: 'flex', gap: '0.4rem' }}>
                      <button
                        type="button"
                        className={`sm-btn ${mobQcPassed ? 'sm-btn--primary' : ''}`}
                        style={{ flex: 1, minHeight: 44 }}
                        onClick={() => setMobQcPassed(true)}
                      >
                        PASS
                      </button>
                      <button
                        type="button"
                        className={`sm-btn ${!mobQcPassed ? 'sm-btn--primary' : ''}`}
                        style={{ flex: 1, minHeight: 44, background: !mobQcPassed ? '#dc2626' : undefined, color: !mobQcPassed ? '#fff' : undefined }}
                        onClick={() => setMobQcPassed(false)}
                      >
                        FAIL (Hold Batch)
                      </button>
                    </div>
                    <input
                      className="sm-input"
                      style={{ minHeight: 44 }}
                      placeholder="Inspector comments / evidence note"
                      value={mobQcNotes}
                      onChange={(e) => setMobQcNotes(e.target.value)}
                    />
                    <button
                      type="button"
                      className="sm-btn sm-btn--primary"
                      style={{ minHeight: 44 }}
                      disabled={!selectedBatchId || !mobQcName.trim()}
                      onClick={async () => {
                        const qcPayload = {
                          productionBatchId: selectedBatchId,
                          checkpointName: mobQcName.trim(),
                          expectedValue: mobQcExpected.trim() || '>= 75 C',
                          actualValue: mobQcValue,
                          measurementUom: 'C',
                          passed: mobQcPassed,
                          comments: mobQcNotes,
                        };
                        if (!isOnline) {
                          enqueueOfflineAction('QUALITY_CHECK', `QC ${mobQcPassed ? 'PASS' : 'FAIL'} (${mobQcValue})`, qcPayload);
                          return;
                        }
                        try {
                          const res = await recordProductionQualityCheck(qcPayload);
                          setStatusMsg(
                            !res.passed
                              ? 'Mandatory quality checkpoint FAILED — batch placed ON HOLD for supervisor corrective action.'
                              : 'Quality checkpoint recorded as PASS.',
                          );
                          setMobQcNotes('');
                          await loadMobileProductionData(selectedBatchId);
                        } catch (err) {
                          setErrMsg(errorText(err));
                        }
                      }}
                    >
                      Submit Quality Check
                    </button>
                  </div>
                </section>

                <section className="sm-card" data-testid="mobile-prod-buffet-exceptions">
                  <h3 style={{ margin: '0 0 0.45rem' }}>Buffet Replenishment &amp; Exceptions ({prodExceptions.length})</h3>
                  <div style={{ display: 'grid', gap: '0.45rem', marginBottom: '0.65rem' }}>
                    <input
                      className="sm-input"
                      style={{ minHeight: 44 }}
                      placeholder="Buffet item / recipe name"
                      value={mobBuffetRecipe}
                      onChange={(e) => setMobBuffetRecipe(e.target.value)}
                    />
                    <div style={{ display: 'flex', gap: '0.4rem' }}>
                      <input
                        className="sm-input"
                        type="number"
                        style={{ width: 100, minHeight: 44 }}
                        value={mobBuffetPortions}
                        onChange={(e) => setMobBuffetPortions(Number(e.target.value))}
                      />
                      <button
                        type="button"
                        className="sm-btn sm-btn--primary"
                        style={{ flex: 1, minHeight: 44 }}
                        disabled={!mobBuffetRecipe.trim()}
                        onClick={async () => {
                          const kitchenId = prodKdsBatches[0]?.kitchenId || 'MAIN-KITCHEN';
                          const payloadData = {
                            kitchenId,
                            itemName: mobBuffetRecipe.trim(),
                            qty: Math.max(1, mobBuffetPortions),
                          };
                          if (!isOnline) {
                            enqueueOfflineAction('BUFFET_REPLENISH', `Buffet replenish ${payloadData.itemName}`, payloadData);
                            setMobBuffetRecipe('');
                            return;
                          }
                          try {
                            await triggerBuffetReplenishment(payloadData);
                            setStatusMsg(`Requested replenishment for ${payloadData.itemName}.`);
                            setMobBuffetRecipe('');
                            await loadMobileProductionData();
                          } catch (err) {
                            setErrMsg(errorText(err));
                          }
                        }}
                      >
                        Request Replenishment
                      </button>
                    </div>
                  </div>

                  <div style={{ borderTop: '1px solid #e2e8f0', paddingTop: '0.6rem', display: 'grid', gap: '0.45rem' }}>
                    <select className="sm-input" style={{ minHeight: 44 }} value={mobExcType} onChange={(e) => setMobExcType(e.target.value)}>
                      <option value="INGREDIENT_SHORTAGE">Ingredient Unavailable / Shortage</option>
                      <option value="EQUIPMENT_BREAKDOWN">Equipment Breakdown</option>
                      <option value="STAFF_UNAVAILABLE">Staff Unavailable</option>
                      <option value="QUALITY_FAIL">Quality Checkpoint Failure</option>
                      <option value="PRODUCTION_DELAY">Production Delay</option>
                    </select>
                    <input
                      className="sm-input"
                      style={{ minHeight: 44 }}
                      placeholder="Exception summary"
                      value={mobExcTitle}
                      onChange={(e) => setMobExcTitle(e.target.value)}
                    />
                    <button
                      type="button"
                      className="sm-btn"
                      style={{ minHeight: 44 }}
                      disabled={!mobExcTitle.trim()}
                      onClick={async () => {
                        const excPayload = {
                          batchId: selectedBatchId || undefined,
                          exceptionType: mobExcType,
                          severity: 'HIGH',
                          description: mobExcTitle.trim(),
                        };
                        if (!isOnline) {
                          enqueueOfflineAction('EXCEPTION_REPORT', excPayload.description, excPayload);
                          setMobExcTitle('');
                          return;
                        }
                        try {
                          await reportProductionException(excPayload);
                          setStatusMsg(`Reported exception: ${excPayload.description}`);
                          setMobExcTitle('');
                          await loadMobileProductionData();
                        } catch (err) {
                          setErrMsg(errorText(err));
                        }
                      }}
                    >
                      Report Production Exception
                    </button>
                  </div>
                </section>
              </div>
            )}

            {tab === 'history' && (
              <section className="sm-card">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                  <h2 style={{ margin: 0 }}>Saved AI Drafts</h2>
                  <button type="button" className="sm-btn" onClick={() => draftsLoad.reload()}>Refresh</button>
                </div>
                {(draftsLoad.data ?? []).length === 0 ? (
                  <p className="sm-meta">No AI recipe drafts saved yet.</p>
                ) : (
                  <ul className="sm-list">
                    {(draftsLoad.data ?? []).map((d) => (
                      <li key={d.id} className="sm-row" style={{ padding: '0.5rem 0' }}>
                        <div>
                          <strong>{d.recipeName}</strong>
                          <div className="sm-meta">
                            {d.draftReference} · Rev {d.revisionNumber} · {d.baseServings} srv · {d.status}
                          </div>
                        </div>
                        <button type="button" className="sm-btn" onClick={() => handleOpenDraft(d.id)}>
                          Open
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            )}

            {tab === 'demo' && (
              <>
                <section className="sm-section">
                  <h2>Interactive Portion Scaling (Demo)</h2>
                  <div className="sm-card">
                    <p style={{ fontWeight: 600, margin: 0 }}>{DEMO_SALMON_RECIPE.title}</p>
                    <div className="sm-field" style={{ marginTop: '0.6rem' }}>
                      <label htmlFor="m-cs2-servings">Portions: <strong>{demoServings}</strong></label>
                      <input
                        id="m-cs2-servings"
                        type="range"
                        min={1}
                        max={250}
                        value={demoServings}
                        onChange={(e) => setDemoServings(Number(e.target.value))}
                      />
                    </div>
                    <p className="sm-meta" style={{ marginTop: '0.4rem' }}>
                      Total Demo Cost: <strong>AED {demoScaling.scaledTotalCostAed.toFixed(2)}</strong> (AED {demoScaling.scaledCostPerServingAed.toFixed(2)} / serving)
                    </p>
                    <ul className="sm-list" style={{ marginTop: '0.5rem' }}>
                      {demoScaling.items.slice(0, 4).map((it) => (
                        <li key={it.id} className="sm-row" style={{ padding: '0.35rem 0' }}>
                          <span>{it.name}</span>
                          <span><strong>{it.grossQuantityWithBuffers.toFixed(2)} {it.uom}</strong></span>
                        </li>
                      ))}
                    </ul>
                  </div>
                </section>

                <section className="sm-section">
                  <h2>Event Shortage Preview (EVT-2027-0001 Demo)</h2>
                  <div className="sm-card">
                    <div className="sm-field">
                      <label htmlFor="m-cs2-guests">Guests: <strong>{guests}</strong></label>
                      <input
                        id="m-cs2-guests"
                        type="range"
                        min={50}
                        max={1000}
                        step={10}
                        value={guests}
                        onChange={(e) => setGuests(Number(e.target.value))}
                      />
                    </div>
                    <ul className="sm-list" style={{ marginTop: '0.5rem' }}>
                      {mrpRows.map((r) => (
                        <li key={r.id} className="sm-row" style={{ padding: '0.35rem 0' }}>
                          <span>{r.materialName}</span>
                          <span>
                            {r.materialExists
                              ? r.shortageQty > 0
                                ? `Shortage ${r.shortageQty.toFixed(1)} ${r.baseUom}`
                                : 'Covered'
                              : 'Material Req.'}
                          </span>
                        </li>
                      ))}
                    </ul>
                  </div>
                </section>
              </>
            )}

            <div className="sm-actions">
              <button type="button" className="sm-btn" onClick={() => go('recipes/catalog')}>
                Existing Recipe Catalog
              </button>
              <button type="button" className="sm-btn" onClick={() => go('recipes/approvals')}>
                Existing Approvals
              </button>
            </div>
          </>
        )}
      </div>
    </>
  );
};

export default CulinarySuitePreviewScreen;
