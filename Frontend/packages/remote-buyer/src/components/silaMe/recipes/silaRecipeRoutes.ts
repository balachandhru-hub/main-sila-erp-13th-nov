/**
 * Canonical Recipe Studio URLs under the buyer dashboard's first-segment routing
 * (`useRouteNav` maps `silaRecipes` -> `/sila-recipes`). Nested segments after that
 * are owned by SilaRecipes so Overview, Recipe Master, New/Edit/Detail, and Culinary Intelligence
 * workspaces survive refresh, deep links, and browser back/forward navigation.
 *
 * Contract:
 * - /sila-recipes (default Overview landing screen)
 * - /sila-recipes/overview
 * - /sila-recipes/master (Recipe Master catalog)
 * - /sila-recipes/new
 * - /sila-recipes/legacy-upload
 * - /sila-recipes/:recipeId
 * - /sila-recipes/:recipeId/edit
 * - /sila-recipes/{ai-planner|materials|material-requests|events|mrp|procurement|production|food-cost|event-pnl|analytics|reports|ai-setup}
 * - /sila-recipes/menus (legacy ME Studio path — safely redirects to Menu Engineering -> ME Studio)
 */

export const SILA_RECIPES_BASE = "/sila-recipes";

export type CulinaryWorkspaceKind =
  | "overview"
  | "ai-planner"
  | "materials"
  | "material-requests"
  | "menus"
  | "events"
  | "mrp"
  | "procurement"
  | "production"
  | "food-cost"
  | "event-pnl"
  | "analytics"
  | "reports"
  | "ai-setup";

export type SilaRecipeRoute =
  | { kind: "list" }
  | { kind: "new" }
  | { kind: "legacy-upload" }
  | { kind: "detail"; recipeId: string }
  | { kind: "edit"; recipeId: string }
  | { kind: CulinaryWorkspaceKind };

const CULINARY_SEGMENTS: ReadonlySet<string> = new Set<string>([
  "overview",
  "ai-planner",
  "materials",
  "material-requests",
  "menus",
  "events",
  "mrp",
  "procurement",
  "production",
  "food-cost",
  "event-pnl",
  "analytics",
  "reports",
  "ai-setup",
]);

const GUID_RE =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export const isRecipeId = (value: string): boolean => GUID_RE.test(value.trim());

/** Parse pathname into a Recipe Studio route; `/sila-recipes` defaults to Overview. */
export const parseSilaRecipePath = (pathname: string): SilaRecipeRoute => {
  const clean = pathname.split("?")[0]?.split("#")[0] ?? pathname;
  const parts = clean.split("/").filter(Boolean);
  const baseIdx = parts.findIndex((part) => part.toLowerCase() === "sila-recipes");
  if (baseIdx < 0) return { kind: "overview" };

  const second = parts[baseIdx + 1]?.trim() ?? "";
  const third = parts[baseIdx + 2]?.trim().toLowerCase() ?? "";

  if (!second) return { kind: "overview" };
  const lowerSecond = second.toLowerCase();
  if (lowerSecond === "master" || lowerSecond === "list") return { kind: "list" };
  if (lowerSecond === "new") return { kind: "new" };
  if (lowerSecond === "legacy-upload") return { kind: "legacy-upload" };
  if (CULINARY_SEGMENTS.has(lowerSecond)) {
    return { kind: lowerSecond as CulinaryWorkspaceKind };
  }

  if (!isRecipeId(second)) return { kind: "overview" };
  if (third === "edit") return { kind: "edit", recipeId: second };
  if (!third) return { kind: "detail", recipeId: second };
  return { kind: "overview" };
};

export const silaRecipeOverviewPath = (): string => SILA_RECIPES_BASE;

export type SilaRecipeCatalogQuery = {
  /** KPI drill-down panel: failed POS sale lines or today's posted consumption. */
  view?: "failed-pos" | "posted-consumption";
  /** DRAFT | PENDING_APPROVAL | APPROVED | INACTIVE | ACTIVE (selling). */
  status?: string;
};

export const silaRecipeCatalogPath = (query?: SilaRecipeCatalogQuery): string => {
  const base = `${SILA_RECIPES_BASE}/master`;
  const params = new URLSearchParams();
  if (query?.view) params.set("view", query.view);
  if (query?.status) params.set("status", query.status);
  const text = params.toString();
  return text ? `${base}?${text}` : base;
};

export const silaRecipeNewPath = (): string => `${SILA_RECIPES_BASE}/new`;

export const silaRecipeLegacyUploadPath = (): string => `${SILA_RECIPES_BASE}/legacy-upload`;

export const silaRecipeCulinaryPath = (kind: CulinaryWorkspaceKind): string =>
  kind === "overview" ? SILA_RECIPES_BASE : `${SILA_RECIPES_BASE}/${kind}`;

export const silaRecipeDetailPath = (recipeId: string): string =>
  `${SILA_RECIPES_BASE}/${encodeURIComponent(recipeId)}`;

export const silaRecipeEditPath = (recipeId: string): string =>
  `${SILA_RECIPES_BASE}/${encodeURIComponent(recipeId)}/edit`;
