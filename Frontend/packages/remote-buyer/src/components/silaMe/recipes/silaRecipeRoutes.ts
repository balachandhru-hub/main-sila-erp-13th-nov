/**
 * Canonical Recipe Studio URLs under the buyer dashboard's first-segment routing
 * (`useRouteNav` maps `silaRecipes` → `/sila-recipes`). Nested segments after that
 * are owned by SilaRecipes so New/Edit/Detail survive refresh and deep links.
 *
 * Contract:
 * - /sila-recipes
 * - /sila-recipes/new
 * - /sila-recipes/:recipeId
 * - /sila-recipes/:recipeId/edit
 */

export const SILA_RECIPES_BASE = "/sila-recipes";

export type SilaRecipeRoute =
  | { kind: "list" }
  | { kind: "new" }
  | { kind: "detail"; recipeId: string }
  | { kind: "edit"; recipeId: string };

const GUID_RE =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export const isRecipeId = (value: string): boolean => GUID_RE.test(value.trim());

/** Parse pathname into a Recipe Studio route; unknown shapes fall back to catalog. */
export const parseSilaRecipePath = (pathname: string): SilaRecipeRoute => {
  const parts = pathname.split("/").filter(Boolean);
  if (parts[0] !== "sila-recipes") return { kind: "list" };

  const second = parts[1]?.trim() ?? "";
  const third = parts[2]?.trim().toLowerCase() ?? "";

  if (!second) return { kind: "list" };
  if (second.toLowerCase() === "new") return { kind: "new" };

  if (!isRecipeId(second)) return { kind: "list" };
  if (third === "edit") return { kind: "edit", recipeId: second };
  if (!third) return { kind: "detail", recipeId: second };
  return { kind: "list" };
};

export const silaRecipeCatalogPath = (): string => SILA_RECIPES_BASE;

export const silaRecipeNewPath = (): string => `${SILA_RECIPES_BASE}/new`;

export const silaRecipeDetailPath = (recipeId: string): string =>
  `${SILA_RECIPES_BASE}/${encodeURIComponent(recipeId)}`;

export const silaRecipeEditPath = (recipeId: string): string =>
  `${SILA_RECIPES_BASE}/${encodeURIComponent(recipeId)}/edit`;
