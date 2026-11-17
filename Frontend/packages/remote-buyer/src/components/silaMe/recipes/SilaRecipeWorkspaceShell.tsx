import React from "react";
import SilaRecipeStudioPresentation from "./SilaRecipeStudioPresentation";
import "./SilaRecipeStudio.css";

export type RecipeWorkspaceMode = "new" | "edit";

interface SilaRecipeWorkspaceShellProps {
  mode: RecipeWorkspaceMode;
  /** Present for Edit; ignored for New. */
  recipeId?: string | null;
  onBackToCatalog: () => void;
}

/**
 * Recipe Studio workspace mount (Steps 1–4).
 * Owns the verified New/Edit route mount from Step 2 and renders the Step 4
 * presentation (live Material Catalog + local ingredient draft). Does not mount
 * SilaRecipeEditor. No Recipe create/update/get APIs.
 */
const SilaRecipeWorkspaceShell: React.FC<SilaRecipeWorkspaceShellProps> = ({
  mode,
  recipeId,
  onBackToCatalog,
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
      />
    </div>
  );
};

export default SilaRecipeWorkspaceShell;
