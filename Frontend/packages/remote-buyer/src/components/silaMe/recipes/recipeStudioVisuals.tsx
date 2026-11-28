import React from "react";

/** Visual family used for recipe/material category illustrations (no external images). */
export type StudioVisualKind =
  | "meat"
  | "seafood"
  | "vegetables"
  | "fruits"
  | "dairy"
  | "bakery"
  | "spices"
  | "beverages"
  | "grains"
  | "dessert"
  | "sauce"
  | "breakfast"
  | "main"
  | "starter"
  | "other";

const KIND_META: Record<StudioVisualKind, { label: string; hue: string; accent: string }> = {
  meat: { label: "Meat", hue: "#ffe8e4", accent: "#c45c48" },
  seafood: { label: "Seafood", hue: "#e4f4ff", accent: "#2f7fb8" },
  vegetables: { label: "Vegetables", hue: "#e7f8ec", accent: "#3d9a5f" },
  fruits: { label: "Fruits", hue: "#fff0e0", accent: "#e08a2c" },
  dairy: { label: "Dairy", hue: "#f3f0ff", accent: "#6b5bd6" },
  bakery: { label: "Bakery", hue: "#fff4df", accent: "#c4882a" },
  spices: { label: "Spices", hue: "#fff1e8", accent: "#d46b2c" },
  beverages: { label: "Beverages", hue: "#e8f7ff", accent: "#1f8fbf" },
  grains: { label: "Grains", hue: "#f7f1e4", accent: "#9a7a3c" },
  dessert: { label: "Desserts", hue: "#ffeaf4", accent: "#c44f86" },
  sauce: { label: "Sauces", hue: "#fff0ea", accent: "#c45f3a" },
  breakfast: { label: "Breakfast", hue: "#fff8e6", accent: "#d4a017" },
  main: { label: "Main Course", hue: "#eef6ff", accent: "#3b7cc9" },
  starter: { label: "Starters", hue: "#eefbf4", accent: "#2f9e6d" },
  other: { label: "Other", hue: "#eef3f8", accent: "#5b7a99" },
};

const RULES: Array<{ kind: StudioVisualKind; match: RegExp }> = [
  { kind: "meat", match: /\b(meat|beef|chicken|lamb|pork|poultry|turkey|mutton|veal|bacon|sausage)\b/i },
  { kind: "seafood", match: /\b(fish|seafood|shrimp|prawn|salmon|tuna|crab|lobster|oyster|mussel|squid)\b/i },
  { kind: "vegetables", match: /\b(veg|vegetable|lettuce|tomato|onion|potato|carrot|spinach|herb|salad|produce)\b/i },
  { kind: "fruits", match: /\b(fruit|apple|banana|berry|lemon|orange|mango|avocado|citrus)\b/i },
  { kind: "dairy", match: /\b(dairy|milk|cream|cheese|butter|yogurt|yoghurt|egg)\b/i },
  { kind: "bakery", match: /\b(bakery|bread|bun|pastry|flour|dough|croissant|baguette)\b/i },
  { kind: "spices", match: /\b(spice|seasoning|pepper|salt|cumin|paprika|chili|chilli|herb)\b/i },
  { kind: "beverages", match: /\b(beverage|drink|juice|coffee|tea|soda|water|wine|beer|spirit|liquor)\b/i },
  { kind: "grains", match: /\b(grain|rice|pasta|noodle|cereal|wheat|oat|corn|pulse|lentil)\b/i },
  { kind: "dessert", match: /\b(dessert|sweet|cake|ice.?cream|chocolate|pudding|pastry)\b/i },
  { kind: "sauce", match: /\b(sauce|dressing|marinade|gravy|condiment|dip)\b/i },
  { kind: "breakfast", match: /\b(breakfast|brunch)\b/i },
  { kind: "starter", match: /\b(starter|appetizer|appetiser|soup)\b/i },
  { kind: "main", match: /\b(main|entree|entrée|course)\b/i },
];

export const resolveStudioKind = (...parts: Array<string | null | undefined>): StudioVisualKind => {
  const text = parts.filter(Boolean).join(" ");
  if (!text.trim()) return "other";
  for (const rule of RULES) {
    if (rule.match.test(text)) return rule.kind;
  }
  return "other";
};

export const studioKindMeta = (kind: StudioVisualKind) => KIND_META[kind];

/** Deterministic pastel cover for a recipe/material without inventing photography. */
export const StudioCover: React.FC<{
  kind?: StudioVisualKind;
  title?: string;
  subtitle?: string;
  className?: string;
  compact?: boolean;
}> = ({ kind = "other", title, subtitle, className = "", compact = false }) => {
  const meta = KIND_META[kind];
  const initials = (title ?? meta.label).trim().slice(0, 2).toUpperCase() || "RS";
  return (
    <div
      className={`rstudio-cover ${compact ? "is-compact" : ""} ${className}`.trim()}
      style={{ background: `linear-gradient(145deg, ${meta.hue} 0%, #ffffff 70%)` }}
      data-kind={kind}
      aria-hidden={!title}
    >
      <svg className="rstudio-cover__art" viewBox="0 0 80 80" role="img" aria-label={meta.label}>
        <circle cx="40" cy="40" r="28" fill={meta.hue} stroke={meta.accent} strokeWidth="2" opacity="0.95" />
        <circle cx="40" cy="40" r="16" fill={meta.accent} opacity="0.18" />
        <text x="40" y="46" textAnchor="middle" fontSize="16" fontWeight="700" fill={meta.accent}>{initials}</text>
      </svg>
      {!compact && (
        <div className="rstudio-cover__caption">
          <span>{meta.label}</span>
          {subtitle && <em>{subtitle}</em>}
        </div>
      )}
    </div>
  );
};

export const VIEW_PREF_KEY = "sila.recipeStudio.viewMode";

export type StudioViewMode = "grid" | "compact" | "list";

export const loadStudioViewMode = (): StudioViewMode => {
  try {
    const value = localStorage.getItem(VIEW_PREF_KEY);
    return value === "list" || value === "compact" ? value : "grid";
  } catch {
    return "grid";
  }
};

export const saveStudioViewMode = (mode: StudioViewMode): void => {
  try {
    localStorage.setItem(VIEW_PREF_KEY, mode);
  } catch {
    /* ignore */
  }
};
