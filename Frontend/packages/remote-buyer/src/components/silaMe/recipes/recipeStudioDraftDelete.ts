/** Delete Draft is allowed only for an existing persisted DRAFT (server enforces the same). */
export const canDeleteDraftRecipe = (status?: string | null): boolean =>
  (status || "").toUpperCase() === "DRAFT";

export const DELETE_DRAFT_TITLE = "Delete Draft Recipe?";

export const DELETE_DRAFT_BODY =
  "Are you sure you want to delete this draft recipe? This action cannot be undone. Approved versions, if any, will remain unchanged.";

export const DELETE_DRAFT_SUCCESS = "Draft recipe deleted successfully";

export const DELETE_DRAFT_CONFIRM_LABEL = "Delete Draft";

export const DISCARD_CHANGES_LABEL = "Discard Changes";
