/** Display copy shared by shell/presentation contracts and Step tests. */
export const recipeWorkspaceShellTitle = (mode: "new" | "edit"): string =>
  mode === "edit" ? "Edit Recipe" : "Create New Recipe";

export const recipeWorkspaceShellBody = (mode: "new" | "edit"): string =>
  mode === "edit"
    ? "Same Studio layout as Create. Recipe data loading connects in Step 6 — no fabricated recipe details."
    : "Blank local UI draft. No database row until Save Draft is connected in Step 5.";

export const RECIPE_WORKSPACE_BACK_LABEL = "Back to Recipe Catalog";
