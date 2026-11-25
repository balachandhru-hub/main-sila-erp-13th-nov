/** Permanent delete is allowed only for an already inactive (archived) recipe. */
export const canDeleteInactiveRecipe = (status?: string | null): boolean =>
  (status || "").toUpperCase() === "INACTIVE";

export const DELETE_INACTIVE_TITLE = "Delete inactive recipe permanently?";

export const DELETE_INACTIVE_BODY =
  "This permanently removes the inactive recipe and its ingredients and outlet prices. This cannot be undone.";

export const DELETE_INACTIVE_SUCCESS = "Inactive recipe permanently deleted";

export const DELETE_INACTIVE_CONFIRM_LABEL = "Delete permanently";
