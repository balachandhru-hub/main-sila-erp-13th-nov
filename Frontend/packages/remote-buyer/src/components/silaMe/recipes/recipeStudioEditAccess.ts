const canStartRevision = (status?: string | null): boolean => {
  const value = (status || "").toUpperCase();
  return value === "APPROVED" || value === "REJECTED";
};

export interface StudioDescriptionNotes {
  cuisine: string;
  mealPeriod: string;
  prepTime: string;
  menuItem: string;
  outletNote: string;
}

/** Recover local presentation notes previously packed into description on Save Draft. */
export const parseStudioDescriptionNotes = (description?: string | null): StudioDescriptionNotes => {
  const text = (description ?? "").trim();
  const pick = (label: string): string => {
    const match = text.match(new RegExp(`${label}:\\s*([^·]+)`, "i"));
    return match?.[1]?.trim() ?? "";
  };
  return {
    cuisine: pick("Cuisine"),
    mealPeriod: pick("Meal period"),
    prepTime: pick("Prep"),
    menuItem: pick("Menu item note"),
    outletNote: pick("Outlet note"),
  };
};

/**
 * POS code / POS item for display and Studio outlet fields.
 * When header POS is empty, fall back to the Linked menu item note (they are the same identity).
 */
export const resolveRecipePos = (recipe: {
  posCode?: string | null;
  posItem?: string | null;
  description?: string | null;
}): { posCode: string; posItem: string; menuItem: string } => {
  const notes = parseStudioDescriptionNotes(recipe.description);
  const menuItem = notes.menuItem.trim();
  const posCode = (recipe.posCode ?? "").trim() || menuItem;
  const posItem = (recipe.posItem ?? "").trim() || menuItem || posCode;
  return {
    posCode,
    posItem,
    menuItem: menuItem || posItem || posCode,
  };
};

export type StudioEditAccess =
  | { kind: "editable" }
  | { kind: "revision-required"; reason: string }
  | { kind: "readonly"; reason: string };

/** Map backend status to Studio edit access (explicit revision for approved/rejected). */
export const studioEditAccess = (status?: string | null): StudioEditAccess => {
  const value = (status || "").toUpperCase();
  if (value === "DRAFT") return { kind: "editable" };
  if (value === "PENDING_APPROVAL") {
    return { kind: "readonly", reason: "This recipe is pending approval and cannot be edited until it is approved or rejected." };
  }
  if (value === "INACTIVE") {
    return { kind: "readonly", reason: "This recipe is inactive and cannot be edited." };
  }
  if (canStartRevision(value)) {
    return {
      kind: "revision-required",
      reason: value === "APPROVED"
        ? "This approved recipe is read-only. Create a Draft revision before making changes. POS keeps selling the active approved version until the revision is approved."
        : "This rejected recipe is read-only until you create a new Draft revision.",
    };
  }
  return { kind: "readonly", reason: `Status ${value || "unknown"} does not allow editing in Recipe Studio.` };
};
