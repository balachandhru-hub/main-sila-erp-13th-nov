import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const EVENTS_BASE = "/api/v1/buyer/sila/culinary-events";
const MENUS_BASE = "/api/v1/buyer/sila/culinary-menus";

export interface SilaCulinaryDietaryAllocation {
  requirementType: string;
  guestCount: number;
  sessionId?: string | null;
  requiredMenuItems: string[];
  specialPrepNotes?: string | null;
  overlapsOtherGroups: boolean;
  verificationStatus: "PENDING" | "VERIFIED" | "FLAGGED" | string;
  reviewerName?: string | null;
}

export interface SilaCulinaryEventSession {
  id: string;
  eventId: string;
  sessionCode: string;
  dayNumber: number;
  sessionDateUtc: string;
  startTime: string;
  endTime: string;
  sessionName: string;
  venueName?: string | null;
  kitchenLocationId?: string | null;
  serviceStyle: string;
  guestCount: number;
  menuId?: string | null;
  budgetAllocation?: number | null;
  prepDeadlineUtc?: string | null;
  dietaryNotesJson?: string | null;
  sortOrder: number;
  estimatedSessionCost?: number | null;
}

export interface SilaCulinaryUpsertSessionRequest {
  sessionId?: string | null;
  dayNumber: number;
  sessionDateUtc: string;
  startTime: string;
  endTime: string;
  sessionName: string;
  venueName?: string | null;
  kitchenLocationId?: string | null;
  serviceStyle: string;
  guestCount: number;
  menuId?: string | null;
  budgetAllocation?: number | null;
  prepDeadlineUtc?: string | null;
  dietaryNotesJson?: string | null;
  sortOrder: number;
}

export interface SilaCulinaryMenuLine {
  id: string;
  menuId: string;
  courseName: string;
  sortOrder: number;
  itemRole: string;
  isLocked: boolean;
  recipeId?: string | null;
  recipeCode?: string | null;
  recipeVersion?: number | null;
  latestPublishedRecipeVersion?: number | null;
  recipeVersionOutdated: boolean;
  recipeDraftId?: string | null;
  dishName: string;
  customerDescription?: string | null;
  dishImageUrl?: string | null;
  lineStatus: string;
  serviceStyle: string;
  uptakePercent: number;
  servingFrequency: number;
  portionSize: number;
  portionUom: string;
  allocatedPortions?: number | null;
  manualPortionsOverride?: number | null;
  plannedPortions: number;
  costingSnapshotId?: string | null;
  unitCostPerPortion?: number | null;
  estimatedLineCost?: number | null;
  costVisible: boolean;
  dietaryTags: string[];
  verifiedAllergens: string[];
  allergenUnknown: boolean;
  allergenVerified: boolean;
  isBeverage: boolean;
  servingsPerGuest: number;
  servingVolumeMl?: number | null;
  containerSizeMl?: number | null;
  estimatedBeverageLiters?: number | null;
  liveStationName?: string | null;
  verifiedStationCapacityPerHour?: number | null;
  notes?: string | null;
}

export interface SilaCulinaryMenu {
  id: string;
  menuCode: string;
  eventId?: string | null;
  sessionId?: string | null;
  name: string;
  description?: string | null;
  menuCategory: string;
  cuisine?: string | null;
  propertyId?: string | null;
  outletLocationId?: string | null;
  serviceStyle: string;
  versionNumber: number;
  effectiveDateUtc: string;
  menuImageUrl?: string | null;
  status: string;
  isReusableTemplate: boolean;
  isScenario: boolean;
  scenarioName?: string | null;
  isSelectedScenario: boolean;
  currency: string;
  targetSellingPrice?: number | null;
  targetFoodCostPercent: number;
  totalFoodCost?: number | null;
  totalBeverageCost?: number | null;
  totalEstimatedCost?: number | null;
  costPerGuest?: number | null;
  costVisible: boolean;
  aiRecommendationNotes?: string | null;
  lines: SilaCulinaryMenuLine[];
}

export interface SilaCulinaryEvent {
  id: string;
  eventCode: string;
  name: string;
  description?: string | null;
  eventType: string;
  eventCategory?: string | null;
  customerReference?: string | null;
  customerName?: string | null;
  bookingReference?: string | null;
  externalEventReference?: string | null;
  eventCoordinator?: string | null;
  responsibleChef?: string | null;
  executiveChef?: string | null;
  banquetManager?: string | null;
  status: string;
  revisionNumber: number;
  approvedRevisionNumber?: number | null;
  propertyId?: string | null;
  companyCode?: string | null;
  plantCode?: string | null;
  venueName?: string | null;
  outletLocationId?: string | null;
  kitchenLocationId?: string | null;
  storageLocationCodes: string[];
  startDateUtc: string;
  endDateUtc: string;
  startTime: string;
  endTime: string;
  prepStartUtc?: string | null;
  menuConfirmationDeadlineUtc?: string | null;
  guestConfirmationDeadlineUtc?: string | null;
  mrpDeadlineUtc?: string | null;
  timeZoneId: string;
  guaranteedGuests: number;
  expectedGuests: number;
  maxCapacity?: number | null;
  additionalAllowancePercent: number;
  planningGuests: number;
  billableGuests: number;
  billablePolicy: string;
  adultsCount: number;
  childrenCount: number;
  vipCount: number;
  staffMealsCount: number;
  currency: string;
  netPricePerGuest?: number | null;
  fixedPackagePrice?: number | null;
  estimatedFoodRevenue?: number | null;
  estimatedBeverageRevenue?: number | null;
  otherPlannedRevenue?: number | null;
  totalEstimatedRevenue?: number | null;
  targetFoodCostPercent: number;
  targetBeverageCostPercent: number;
  combinedMenuBudget?: number | null;
  approvedBudget?: number | null;
  totalEstimatedMenuCost?: number | null;
  menuCostPercent?: number | null;
  remainingMenuBudget?: number | null;
  costVisible: boolean;
  serviceStyle: string;
  primaryMenuId?: string | null;
  sourceTemplateId?: string | null;
  wizardStepCompleted: number;
  dietaryRequirements: SilaCulinaryDietaryAllocation[];
  dietaryReviewVerified: boolean;
  dietaryReviewerName?: string | null;
  dietaryVerifiedOnUtc?: string | null;
  hasVenueConflict: boolean;
  conflictingEventCodes: string[];
  approvalReference?: string | null;
  submittedOnUtc?: string | null;
  approvedOnUtc?: string | null;
  approvalDecisionComment?: string | null;
  createdOn: string;
  updatedOn?: string | null;
  primaryMenu?: SilaCulinaryMenu | null;
  sessions: SilaCulinaryEventSession[];
  scenarios: SilaCulinaryMenu[];
}

export interface SilaCulinaryUpsertEventRequest {
  name: string;
  description?: string | null;
  eventType: string;
  eventCategory?: string | null;
  customerReference?: string | null;
  customerName?: string | null;
  bookingReference?: string | null;
  externalEventReference?: string | null;
  eventCoordinator?: string | null;
  responsibleChef?: string | null;
  executiveChef?: string | null;
  banquetManager?: string | null;
  propertyId?: string | null;
  companyCode?: string | null;
  plantCode?: string | null;
  venueName?: string | null;
  outletLocationId?: string | null;
  kitchenLocationId?: string | null;
  storageLocationCodes?: string[] | null;
  startDateUtc: string;
  endDateUtc: string;
  startTime: string;
  endTime: string;
  prepStartUtc?: string | null;
  menuConfirmationDeadlineUtc?: string | null;
  guestConfirmationDeadlineUtc?: string | null;
  mrpDeadlineUtc?: string | null;
  timeZoneId: string;
  guaranteedGuests: number;
  expectedGuests: number;
  maxCapacity?: number | null;
  additionalAllowancePercent: number;
  billablePolicy: string;
  adultsCount: number;
  childrenCount: number;
  vipCount: number;
  staffMealsCount: number;
  currency: string;
  netPricePerGuest: number;
  fixedPackagePrice?: number | null;
  targetFoodCostPercent: number;
  targetBeverageCostPercent: number;
  combinedMenuBudget?: number | null;
  approvedBudget?: number | null;
  serviceStyle: string;
  wizardStepCompleted: number;
  dietaryRequirements?: SilaCulinaryDietaryAllocation[] | null;
  changeReason?: string | null;
}

export interface SilaCulinaryCreateMenuRequest {
  eventId?: string | null;
  sessionId?: string | null;
  name: string;
  description?: string | null;
  menuCategory: string;
  cuisine?: string | null;
  propertyId?: string | null;
  outletLocationId?: string | null;
  serviceStyle: string;
  isReusableTemplate: boolean;
  currency: string;
  targetSellingPrice?: number | null;
  targetFoodCostPercent: number;
}

export interface SilaCulinaryUpsertMenuLineRequest {
  lineId?: string | null;
  courseName: string;
  sortOrder?: number | null;
  itemRole: string;
  isLocked: boolean;
  recipeId?: string | null;
  recipeVersion?: number | null;
  dishName?: string | null;
  customerDescription?: string | null;
  serviceStyle: string;
  uptakePercent: number;
  servingFrequency: number;
  portionSize: number;
  portionUom: string;
  allocatedPortions?: number | null;
  manualPortionsOverride?: number | null;
  isBeverage: boolean;
  servingsPerGuest: number;
  servingVolumeMl?: number | null;
  containerSizeMl?: number | null;
  liveStationName?: string | null;
  verifiedStationCapacityPerHour?: number | null;
  notes?: string | null;
  unitCostOverride?: number | null;
}

export interface SilaCulinaryPublishedRecipeSearchItem {
  recipeId: string;
  recipeCode: string;
  recipeVersion: number;
  name: string;
  category: string;
  cuisine?: string | null;
  imageUrl?: string | null;
  servingQty: number;
  servingUom: string;
  costPerPortion?: number | null;
  currency: string;
  prepTimeMinutes?: number | null;
  cookTimeMinutes?: number | null;
  dietaryTags: string[];
  verifiedAllergens: string[];
  unknownAllergens: boolean;
  propertyEligible: boolean;
  matchScore: number;
  matchReason: string;
}

export interface SilaCulinaryAiMenuGenerateRequest {
  naturalLanguageBrief?: string | null;
  eventTheme?: string | null;
  cuisinePreferences: string[];
  requiredCourses: string[];
  requiredDietaryTags: string[];
  excludedAllergens: string[];
  strategy: string;
  allowMissingRecipeSuggestions: boolean;
  applyToMenu: boolean;
}

export interface SilaCulinaryAiMenuGenerateResult {
  menuTitle: string;
  menuDescription: string;
  planningGuests: number;
  estimatedTotalCost?: number | null;
  currency: string;
  providerUsed: string;
  modelUsed: string;
  clarificationQuestions: string[];
  budgetWarnings: string[];
  dietaryCoverage: Record<string, number>;
  missingRecipeCount: number;
  explanation: string;
  proposedLines: SilaCulinaryMenuLine[];
}

export interface SilaCulinaryMenuOptimizeRequest {
  maxBudget?: number | null;
  requiredCourses?: Record<string, number> | null;
  requiredDietaryTags: string[];
  excludedAllergens: string[];
  strategy: string;
  applyChanges: boolean;
}

export interface SilaCulinaryMenuOptimizeResult {
  feasible: boolean;
  solver: string;
  beforeCost?: number | null;
  afterCost?: number | null;
  budget?: number | null;
  savings?: number | null;
  conflictingConstraints: string[];
  suggestedAdjustments: string[];
  explanation: string;
  optimizedLines: SilaCulinaryMenuLine[];
}

export interface SilaCulinaryAllergenMatrixRow {
  menuLineId: string;
  dishName: string;
  recipeCode?: string | null;
  recipeVersion?: number | null;
  verifiedAllergens: string[];
  subRecipeAllergens: string[];
  allergenUnknown: boolean;
  crossContactRisk: string;
  verificationStatus: string;
}

export interface SilaCulinaryMaterialPreviewLine {
  dishName: string;
  recipeCode?: string | null;
  recipeVersion?: number | null;
  ingredientName: string;
  materialId?: string | null;
  materialCode?: string | null;
  materialDescription?: string | null;
  requiredQty: number;
  recipeUom: string;
  baseQty?: number | null;
  baseUom?: string | null;
  onHandStock?: number | null;
  stockIndicator: string;
  stockTimestampUtc?: string | null;
  scalingIncomplete: boolean;
}

export interface SilaCulinaryEventSimulationRequest {
  guaranteedGuests?: number | null;
  expectedGuests?: number | null;
  additionalAllowancePercent?: number | null;
  netPricePerGuest?: number | null;
  targetFoodCostPercent?: number | null;
  menuBudget?: number | null;
  serviceStyle?: string | null;
  lineOverrides: Array<{
    lineId: string;
    uptakePercent?: number | null;
    portionSize?: number | null;
    unitCostOverride?: number | null;
  }>;
}

export interface SilaCulinaryEventSimulationResult {
  guaranteedGuests: number;
  planningGuests: number;
  billableGuests: number;
  currency: string;
  estimatedRevenue?: number | null;
  totalFoodCost?: number | null;
  totalBeverageCost?: number | null;
  totalMenuCost?: number | null;
  costPerGuaranteedGuest?: number | null;
  costPerPlannedGuest?: number | null;
  menuCostPercent?: number | null;
  menuBudget?: number | null;
  remainingMenuBudget?: number | null;
  budgetExceeded: boolean;
  platedAllocationValid: boolean;
  warnings: string[];
  simulatedLines: SilaCulinaryMenuLine[];
}

export interface SilaCulinaryEventReadinessItem {
  checkCode: string;
  label: string;
  status: "PASSED" | "WARNING" | "BLOCKED" | string;
  detail: string;
  recommendedAction?: string | null;
}

export interface SilaCulinaryEventReadiness {
  eventId: string;
  eventCode: string;
  readyForApproval: boolean;
  readyForMrp: boolean;
  passedCount: number;
  warningCount: number;
  blockedCount: number;
  checks: SilaCulinaryEventReadinessItem[];
}

export interface SilaCulinaryCustomerMenuPreview {
  eventName: string;
  propertyName?: string | null;
  venueName?: string | null;
  startDateUtc: string;
  menuTitle: string;
  serviceStyle: string;
  isProposedOnly: boolean;
  courses: Array<{
    courseName: string;
    dishes: Array<{
      dishName: string;
      description?: string | null;
      imageUrl?: string | null;
      dietaryTags: string[];
      allergenDisclosures: string[];
      allergenPendingVerification: boolean;
    }>;
  }>;
}

export interface SilaCulinaryEventTemplate {
  id: string;
  templateCode: string;
  name: string;
  description?: string | null;
  eventType: string;
  defaultServiceStyle: string;
  defaultDurationDays: number;
  defaultAllowancePercent: number;
  defaultTargetFoodCostPercent: number;
  createdOn: string;
}

export interface SilaCulinaryEventDashboard {
  upcomingEvents: number;
  eventsThisWeek: number;
  eventsThisMonth: number;
  totalPlannedGuests: number;
  draftEvents: number;
  awaitingApprovalEvents: number;
  approvedEvents: number;
  incompleteMenuEvents: number;
  aboveBudgetEvents: number;
  dietaryReviewPendingEvents: number;
  readyForMrpEvents: number;
  eventsByType: Record<string, number>;
  eventsByStatus: Record<string, number>;
  budgetVsCost: Array<{
    eventCode: string;
    eventName: string;
    budget?: number | null;
    estimatedCost?: number | null;
    planningGuests: number;
  }>;
}

export interface SilaCulinaryEventRevision {
  id: string;
  revisionNumber: number;
  statusAtRevision: string;
  isApprovedBaseline: boolean;
  changeReason?: string | null;
  guaranteedGuestsAtRevision: number;
  planningGuestsAtRevision: number;
  menuCostAtRevision?: number | null;
  approvalReference?: string | null;
  createdByName?: string | null;
  createdOn: string;
}

export interface SilaCulinaryEventReportResult {
  reportCode: string;
  fileName: string;
  contentType: string;
  base64Content: string;
  rowCount: number;
}

export interface SilaCulinaryPhase2BHandover {
  eventId: string;
  eventCode: string;
  eventName: string;
  eventRevision: number;
  approvedRevision?: number | null;
  propertyId?: string | null;
  plantCode?: string | null;
  venueName?: string | null;
  kitchenLocationId?: string | null;
  startDateUtc: string;
  endDateUtc: string;
  mrpDeadlineUtc?: string | null;
  guaranteedGuests: number;
  planningGuests: number;
  currency: string;
  eventMenuBudget?: number | null;
  totalEstimatedMenuCost?: number | null;
  approvalReference?: string | null;
  approvalStatus: string;
  baselineTimestampUtc?: string | null;
  dietaryVerified: boolean;
  mrpReady: boolean;
  outstandingWarnings: string[];
  sessions: SilaCulinaryEventSession[];
  pinnedMenuLines: SilaCulinaryMenuLine[];
  preliminaryMaterialRequirements: SilaCulinaryMaterialPreviewLine[];
}

export async function getEventDashboard(): Promise<SilaCulinaryEventDashboard> {
  try {
    const res = await axiosInstance.get<SilaCulinaryEventDashboard>(`${EVENTS_BASE}/dashboard`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function listCulinaryEvents(params?: {
  status?: string;
  propertyId?: string;
  fromUtc?: string;
  toUtc?: string;
  search?: string;
}): Promise<SilaCulinaryEvent[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryEvent[]>(EVENTS_BASE, { params });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function getCulinaryEvent(eventId: string): Promise<SilaCulinaryEvent> {
  try {
    const res = await axiosInstance.get<SilaCulinaryEvent>(`${EVENTS_BASE}/${eventId}`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function createCulinaryEvent(payload: SilaCulinaryUpsertEventRequest): Promise<SilaCulinaryEvent> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEvent>(EVENTS_BASE, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function updateCulinaryEvent(eventId: string, payload: SilaCulinaryUpsertEventRequest): Promise<SilaCulinaryEvent> {
  try {
    const res = await axiosInstance.put<SilaCulinaryEvent>(`${EVENTS_BASE}/${eventId}`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function duplicateCulinaryEvent(eventId: string): Promise<SilaCulinaryEvent> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEvent>(`${EVENTS_BASE}/${eventId}/duplicate`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function cancelCulinaryEvent(eventId: string, reason?: string): Promise<SilaCulinaryEvent> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEvent>(`${EVENTS_BASE}/${eventId}/cancel`, { reason });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function listCulinaryEventTemplates(): Promise<SilaCulinaryEventTemplate[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryEventTemplate[]>(`${EVENTS_BASE}/templates`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function saveCulinaryEventTemplate(eventId: string, name: string, description?: string): Promise<SilaCulinaryEventTemplate> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventTemplate>(`${EVENTS_BASE}/${eventId}/save-template`, { name, description });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function createEventFromTemplate(templateId: string, overrides: SilaCulinaryUpsertEventRequest): Promise<SilaCulinaryEvent> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEvent>(`${EVENTS_BASE}/templates/${templateId}/create-event`, overrides);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function upsertCulinaryEventSession(eventId: string, payload: SilaCulinaryUpsertSessionRequest): Promise<SilaCulinaryEventSession> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventSession>(`${EVENTS_BASE}/${eventId}/sessions`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function deleteCulinaryEventSession(eventId: string, sessionId: string): Promise<void> {
  try {
    await axiosInstance.delete(`${EVENTS_BASE}/${eventId}/sessions/${sessionId}`);
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function listCulinaryMenus(params?: { eventId?: string; reusableOnly?: boolean }): Promise<SilaCulinaryMenu[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryMenu[]>(MENUS_BASE, { params });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function getCulinaryMenu(menuId: string): Promise<SilaCulinaryMenu> {
  try {
    const res = await axiosInstance.get<SilaCulinaryMenu>(`${MENUS_BASE}/${menuId}`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function createCulinaryMenu(payload: SilaCulinaryCreateMenuRequest): Promise<SilaCulinaryMenu> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMenu>(MENUS_BASE, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function upsertCulinaryMenuLine(menuId: string, payload: SilaCulinaryUpsertMenuLineRequest): Promise<SilaCulinaryMenu> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMenu>(`${MENUS_BASE}/${menuId}/lines`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function removeCulinaryMenuLine(menuId: string, lineId: string): Promise<SilaCulinaryMenu> {
  try {
    const res = await axiosInstance.delete<SilaCulinaryMenu>(`${MENUS_BASE}/${menuId}/lines/${lineId}`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function reorderCulinaryMenuLines(menuId: string, orderedLineIds: string[]): Promise<SilaCulinaryMenu> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMenu>(`${MENUS_BASE}/${menuId}/reorder`, { orderedLineIds });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function searchPublishedCulinaryRecipes(params?: {
  query?: string;
  category?: string;
  cuisine?: string;
  dietary?: string;
  excludeAllergens?: string;
  maxCost?: number;
  propertyId?: string;
}): Promise<SilaCulinaryPublishedRecipeSearchItem[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryPublishedRecipeSearchItem[]>(`${MENUS_BASE}/published-recipes`, { params });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function createMissingRecipeDraftFromMenuLine(menuId: string, lineId: string): Promise<{ draftId: string }> {
  try {
    const res = await axiosInstance.post<{ draftId: string }>(`${MENUS_BASE}/${menuId}/lines/${lineId}/create-recipe-draft`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function syncPublishedRecipeToMenuLine(menuId: string, lineId: string): Promise<SilaCulinaryMenu> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMenu>(`${MENUS_BASE}/${menuId}/lines/${lineId}/sync-published`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function generateCulinaryAiMenu(eventId: string, payload: SilaCulinaryAiMenuGenerateRequest): Promise<SilaCulinaryAiMenuGenerateResult> {
  try {
    const res = await axiosInstance.post<SilaCulinaryAiMenuGenerateResult>(`${EVENTS_BASE}/${eventId}/ai-generate-menu`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function optimizeCulinaryEventMenu(eventId: string, payload: SilaCulinaryMenuOptimizeRequest): Promise<SilaCulinaryMenuOptimizeResult> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMenuOptimizeResult>(`${EVENTS_BASE}/${eventId}/optimize-menu`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function verifyCulinaryEventDietary(eventId: string, verified: boolean): Promise<SilaCulinaryEvent> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEvent>(`${EVENTS_BASE}/${eventId}/dietary-verify`, { verified });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function getCulinaryEventAllergenMatrix(eventId: string): Promise<SilaCulinaryAllergenMatrixRow[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryAllergenMatrixRow[]>(`${EVENTS_BASE}/${eventId}/allergen-matrix`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function getCulinaryEventMaterialPreview(eventId: string): Promise<SilaCulinaryMaterialPreviewLine[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryMaterialPreviewLine[]>(`${EVENTS_BASE}/${eventId}/material-preview`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function simulateCulinaryEvent(eventId: string, payload: SilaCulinaryEventSimulationRequest): Promise<SilaCulinaryEventSimulationResult> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventSimulationResult>(`${EVENTS_BASE}/${eventId}/simulate`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function createCulinaryEventScenario(eventId: string, scenarioName: string): Promise<SilaCulinaryMenu> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMenu>(`${EVENTS_BASE}/${eventId}/scenarios`, { scenarioName });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function selectCulinaryEventScenario(eventId: string, scenarioMenuId: string): Promise<SilaCulinaryEvent> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEvent>(`${EVENTS_BASE}/${eventId}/scenarios/${scenarioMenuId}/select`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function getCulinaryCustomerMenuPreview(eventId: string): Promise<SilaCulinaryCustomerMenuPreview> {
  try {
    const res = await axiosInstance.get<SilaCulinaryCustomerMenuPreview>(`${EVENTS_BASE}/${eventId}/customer-menu`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function getCulinaryEventReadiness(eventId: string): Promise<SilaCulinaryEventReadiness> {
  try {
    const res = await axiosInstance.get<SilaCulinaryEventReadiness>(`${EVENTS_BASE}/${eventId}/readiness`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function submitCulinaryEventForApproval(eventId: string): Promise<SilaCulinaryEvent> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEvent>(`${EVENTS_BASE}/${eventId}/submit`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function decideCulinaryEventApproval(eventId: string, decision: "APPROVE" | "REJECT" | "RETURN", comment?: string): Promise<SilaCulinaryEvent> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEvent>(`${EVENTS_BASE}/${eventId}/decide`, { decision, comment });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function createCulinaryEventRevision(eventId: string, reason: string): Promise<SilaCulinaryEvent> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEvent>(`${EVENTS_BASE}/${eventId}/revisions`, { reason });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function listCulinaryEventRevisions(eventId: string): Promise<SilaCulinaryEventRevision[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryEventRevision[]>(`${EVENTS_BASE}/${eventId}/revisions`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function exportCulinaryEventReport(eventId: string, reportCode: string): Promise<SilaCulinaryEventReportResult> {
  try {
    const res = await axiosInstance.get<SilaCulinaryEventReportResult>(`${EVENTS_BASE}/${eventId}/reports/${encodeURIComponent(reportCode)}`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export async function getCulinaryEventPhase2BHandover(eventId: string): Promise<SilaCulinaryPhase2BHandover> {
  try {
    const res = await axiosInstance.get<SilaCulinaryPhase2BHandover>(`${EVENTS_BASE}/${eventId}/phase2b-handover`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary Event & Menu request failed."));
  }
}

export function downloadEventReportResult(report: SilaCulinaryEventReportResult): void {
  const binary = atob(report.base64Content);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i);
  }
  const blob = new Blob([bytes], { type: report.contentType || "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = report.fileName || `${report.reportCode}.csv`;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(url);
}
