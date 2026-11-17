import React from "react";
import "./SilaRecipeStudio.css";

export type RecipeWorkspaceMode = "new" | "edit";

interface SilaRecipeWorkspaceShellProps {
  mode: RecipeWorkspaceMode;
  /** Present for Edit; ignored for New. */
  recipeId?: string | null;
  onBackToCatalog: () => void;
}

/**
 * STEP 1 OF 7 — temporary Recipe Studio workspace shell.
 *
 * Deliberately not a permanent editor. New/Edit routes mount this instead of
 * SilaRecipeEditor while the create/edit UI is rebuilt. No Material Catalog,
 * Save Draft, Submit, or drag/drop wiring in this step.
 */
const SilaRecipeWorkspaceShell: React.FC<SilaRecipeWorkspaceShellProps> = ({
  mode,
  recipeId,
  onBackToCatalog,
}) => {
  const isEdit = mode === "edit";
  const title = isEdit ? "Edit Recipe — Workspace Ready" : "New Recipe — Workspace Ready";
  const subtitle = isEdit
    ? "Temporary Step 1 shell. Full create/edit Studio returns in later rebuild steps."
    : "Temporary Step 1 shell. Recipe creation Studio returns in later rebuild steps.";

  return (
    <div className="sila-me srec-page rstudio-page" data-testid="recipe-workspace-shell" data-mode={mode}>
      <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm rstudio-back" onClick={onBackToCatalog}>
        ← Back to Recipe Catalog
      </button>

      <header className="rstudio-detail-hero" style={{ minHeight: "12rem" }}>
        <div className="rstudio-detail-hero__copy">
          <p className="rstudio-kicker">SILA Recipe Studio · Step 1 of 7</p>
          <h1>{title}</h1>
          <p className="rstudio-detail-hero__meta">{subtitle}</p>
          {isEdit && (
            <p className="rstudio-detail-hero__desc" data-testid="recipe-workspace-edit-id">
              Recipe ID: <code>{recipeId?.trim() || "—"}</code>
            </p>
          )}
          <div className="srec-actions">
            <button type="button" className="sila-btn sila-btn--secondary" onClick={onBackToCatalog}>
              Back to Recipe Catalog
            </button>
          </div>
        </div>
      </header>

      <div className="sila-alert sila-alert--info" role="status">
        Create/Edit editor is isolated for rebuild. Catalog and detail remain available. No Save or Submit in this step.
      </div>
    </div>
  );
};

export default SilaRecipeWorkspaceShell;
