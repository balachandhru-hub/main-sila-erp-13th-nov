import React from "react";
import SilaRecipeStudioPresentation from "./SilaRecipeStudioPresentation";
import "./SilaRecipeStudio.css";

export type RecipeWorkspaceMode = "new" | "edit";

interface SilaRecipeWorkspaceShellProps {
  mode: RecipeWorkspaceMode;
  /** Present for Edit; ignored for New. */
  recipeId?: string | null;
  onBackToCatalog: () => void;
  /** New Recipe only: after Save Draft create + verify, open Recipe Detail. */
  onDraftSaved?: (recipeId: string) => void;
}

/**
 * Recipe Studio workspace mount (Steps 1–6).
 * Owns the verified New/Edit route mount from Step 2 and renders the Step 6
 * presentation (catalog, create/update draft, revision, submit). Does not mount SilaRecipeEditor.
 */
const SilaRecipeWorkspaceShell: React.FC<SilaRecipeWorkspaceShellProps> = ({
  mode,
  recipeId,
  onBackToCatalog,
  onDraftSaved,
}) => {
  const isEdit = mode === "edit";
  const idText = (recipeId ?? "").trim();

  return (
    <div
      className="sila-me srec-page rstudio-page"
      data-testid="recipe-workspace-shell"
      data-mode={mode}
      data-recipe-id={isEdit ? idText || undefined : undefined}
    >
      <SilaRecipeStudioPresentation
        mode={mode}
        recipeId={isEdit ? idText : null}
        onBackToCatalog={onBackToCatalog}
        onDraftSaved={onDraftSaved}
      />
    </div>
  );
};

export default SilaRecipeWorkspaceShell;
