/**
 * Canonical Recipe Studio URLs under the buyer dashboard's first-segment routing
 * (`useRouteNav` maps `silaRecipes` → `/sila-recipes`). Nested segments after that
 * are owned by SilaRecipes so New/Edit/Detail and Recipe Studio 2.0 workspaces survive refresh and deep links.
 *
 * Contract:
 * - /sila-recipes
 * - /sila-recipes/new
 * - /sila-recipes/legacy-upload
 * - /sila-recipes/:recipeId
 * - /sila-recipes/:recipeId/edit
 * - /sila-recipes/{overview|ai-planner|materials|material-requests|menus|events|mrp|procurement|production|food-cost|event-pnl|analytics|reports|ai-setup}
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

/** Parse pathname into a Recipe Studio route; unknown shapes fall back to catalog. */
export const parseSilaRecipePath = (pathname: string): SilaRecipeRoute => {
  // Ignore query/hash; tolerate trailing slash and accidental double segments.
  const clean = pathname.split("?")[0]?.split("#")[0] ?? pathname;
  const parts = clean.split("/").filter(Boolean);
  const baseIdx = parts.findIndex((part) => part.toLowerCase() === "sila-recipes");
  if (baseIdx < 0) return { kind: "list" };

  const second = parts[baseIdx + 1]?.trim() ?? "";
  const third = parts[baseIdx + 2]?.trim().toLowerCase() ?? "";

  if (!second) return { kind: "list" };
  const lowerSecond = second.toLowerCase();
  if (lowerSecond === "new") return { kind: "new" };
  if (lowerSecond === "legacy-upload") return { kind: "legacy-upload" };
  if (CULINARY_SEGMENTS.has(lowerSecond)) {
    return { kind: lowerSecond as CulinaryWorkspaceKind };
  }

  if (!isRecipeId(second)) return { kind: "list" };
  if (third === "edit") return { kind: "edit", recipeId: second };
  if (!third) return { kind: "detail", recipeId: second };
  return { kind: "list" };
};

export const silaRecipeCatalogPath = (): string => SILA_RECIPES_BASE;

export const silaRecipeNewPath = (): string => `${SILA_RECIPES_BASE}/new`;

export const silaRecipeLegacyUploadPath = (): string => `${SILA_RECIPES_BASE}/legacy-upload`;

export const silaRecipeCulinaryPath = (kind: CulinaryWorkspaceKind): string =>
  `${SILA_RECIPES_BASE}/${kind}`;

export const silaRecipeDetailPath = (recipeId: string): string =>
  `${SILA_RECIPES_BASE}/${encodeURIComponent(recipeId)}`;

export const silaRecipeEditPath = (recipeId: string): string =>
  `${SILA_RECIPES_BASE}/${encodeURIComponent(recipeId)}/edit`;
