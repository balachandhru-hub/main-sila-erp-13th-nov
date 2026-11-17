import React from "react";
import {
  RECIPE_WORKSPACE_BACK_LABEL,
  recipeWorkspaceShellBody,
  recipeWorkspaceShellTitle,
} from "./recipeWorkspaceShellCopy";
import "./SilaRecipeStudio.css";

export type RecipeWorkspaceMode = "new" | "edit";

interface SilaRecipeWorkspaceShellProps {
  mode: RecipeWorkspaceMode;
  /** Present for Edit; ignored for New. */
  recipeId?: string | null;
  onBackToCatalog: () => void;
}

/**
 * Temporary Recipe Studio workspace shell (Steps 1–2 of 7).
 *
 * Deliberately not a permanent editor. New/Edit routes mount this instead of
 * SilaRecipeEditor while create/edit UI is rebuilt. No Material Catalog,
 * Save Draft, Submit, or drag/drop wiring yet.
 */
const SilaRecipeWorkspaceShell: React.FC<SilaRecipeWorkspaceShellProps> = ({
  mode,
  recipeId,
  onBackToCatalog,
}) => {
  const isEdit = mode === "edit";
  const title = recipeWorkspaceShellTitle(mode);
  const subtitle = recipeWorkspaceShellBody(mode);
  const idText = (recipeId ?? "").trim();

  return (
    <div
      className="sila-me srec-page rstudio-page"
      data-testid="recipe-workspace-shell"
      data-mode={mode}
      data-recipe-id={isEdit ? idText || undefined : undefined}
    >
      <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm rstudio-back" onClick={onBackToCatalog}>
        ← {RECIPE_WORKSPACE_BACK_LABEL}
      </button>

      <header className="rstudio-detail-hero" style={{ minHeight: "12rem" }}>
        <div className="rstudio-detail-hero__copy">
          <p className="rstudio-kicker">SILA Recipe Studio · Step 2 of 7</p>
          <h1>{title}</h1>
          <p className="rstudio-detail-hero__meta">{subtitle}</p>
          {isEdit && (
            <p className="rstudio-detail-hero__desc" data-testid="recipe-workspace-edit-id">
              Recipe ID: <code>{idText || "—"}</code>
            </p>
          )}
          <div className="srec-actions">
            <button type="button" className="sila-btn sila-btn--secondary" onClick={onBackToCatalog}>
              {RECIPE_WORKSPACE_BACK_LABEL}
            </button>
          </div>
        </div>
      </header>

      <div className="sila-alert sila-alert--info" role="status">
        Create/Edit editor remains isolated for rebuild. Catalog and detail stay available. No Save or Submit in this step.
      </div>
    </div>
  );
};

export default SilaRecipeWorkspaceShell;
