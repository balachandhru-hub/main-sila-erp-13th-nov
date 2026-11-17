/** Display copy for the temporary Recipe Studio workspace shell (Steps 1–2). */
export const recipeWorkspaceShellTitle = (mode: "new" | "edit"): string =>
  mode === "edit" ? "Edit Recipe — Workspace Ready" : "New Recipe — Workspace Ready";

export const recipeWorkspaceShellBody = (mode: "new" | "edit"): string =>
  mode === "edit" ? "Recipe editing workspace is connected." : "Recipe creation workspace is connected.";

export const RECIPE_WORKSPACE_BACK_LABEL = "Back to Recipe Catalog";
