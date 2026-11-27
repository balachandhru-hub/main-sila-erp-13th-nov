import React, { useEffect, useRef, useState } from "react";
import { BookOpen, Maximize2, Minimize2, MoreVertical, PanelLeftClose, Pin, PinOff, Plus, Trash2, UtensilsCrossed } from "lucide-react";
import { StudioDrawer } from "./StudioWorkspacePrimitives";

export interface RecipeCardOverflowAction {
  id: string;
  label: string;
  danger?: boolean;
  onClick: () => void;
  testId?: string;
}

export interface RecipeCardProps {
  id?: string;
  title: string;
  code: string;
  statusLabel: string;
  statusClassName?: string;
  statusTone?: "success" | "warning" | "danger" | "error" | "info" | "neutral";
  media: React.ReactNode;
  metaParts?: string[];
  metaText?: string;
  versionText: string;
  ingredientCountText?: string;
  ingredientCount?: number;
  yieldText: string;
  costPerPortionText?: string;
  costLabel?: string;
  menuRefText?: string;
  posReference?: string;
  onView: () => void;
  onEdit?: () => void;
  overflowActions?: RecipeCardOverflowAction[];
  moreActions?: RecipeCardOverflowAction[];
  compact?: boolean;
}

const mapCardToneClass = (tone?: RecipeCardProps["statusTone"]): string => {
  switch (tone) {
    case "success":
      return "sila-studio-badge sila-studio-badge--success";
    case "warning":
      return "sila-studio-badge sila-studio-badge--warning";
    case "danger":
    case "error":
      return "sila-studio-badge sila-studio-badge--error";
    case "neutral":
      return "sila-studio-badge sila-studio-badge--neutral";
    default:
      return "sila-studio-badge sila-studio-badge--info";
  }
};

export const RecipeCard: React.FC<RecipeCardProps> = ({
  title,
  code,
  statusLabel,
  statusClassName,
  statusTone,
  media,
  metaParts,
  metaText,
  versionText,
  ingredientCountText,
  ingredientCount,
  yieldText,
  costPerPortionText,
  costLabel,
  menuRefText,
  posReference,
  onView,
  onEdit,
  overflowActions,
  moreActions,
  compact = false,
}) => {
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement | null>(null);
  const resolvedStatusClass = statusClassName ?? mapCardToneClass(statusTone);
  const resolvedMeta = metaText ?? (metaParts ? [code, ...metaParts.filter(Boolean)].join(" · ") : code);
  const resolvedIngredients =
    ingredientCountText ?? (ingredientCount !== undefined ? `${ingredientCount} ingr` : "");
  const resolvedCost = costPerPortionText ?? costLabel ?? "COST PENDING";
  const resolvedPos = menuRefText ?? posReference ?? "—";
  const resolvedOverflow = overflowActions ?? moreActions ?? [];

  useEffect(() => {
    if (!menuOpen) return;
    const onDocClick = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setMenuOpen(false);
      }
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setMenuOpen(false);
    };
    document.addEventListener("mousedown", onDocClick);
    window.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDocClick);
      window.removeEventListener("keydown", onKey);
    };
  }, [menuOpen]);

  return (
    <article className="sila-studio-item-card" role="listitem">
      <button
        type="button"
        className="sila-studio-item-card__media"
        onClick={onView}
        aria-label={`View ${title}`}
      >
        {media}
        <span className={`sila-studio-item-card__status ${resolvedStatusClass}`}>{statusLabel}</span>
      </button>
      <div className="sila-studio-item-card__body" style={compact ? { padding: "0.55rem 0.65rem" } : undefined}>
        <h3 className="sila-studio-item-card__title">
          <button type="button" className="sila-studio-item-card__title-btn" onClick={onView}>
            {title}
          </button>
        </h3>
        <p className="sila-studio-item-card__meta">
          {resolvedMeta}
        </p>
        {!compact && (
          <div className="sila-studio-item-card__stats">
            <span>{versionText}</span>
            {resolvedIngredients && <span>{resolvedIngredients}</span>}
            <span>{yieldText}</span>
          </div>
        )}
        <div className="sila-studio-item-card__metrics">
          <div className="sila-studio-item-card__metric">
            <em>Cost / portion</em>
            <strong>{resolvedCost}</strong>
          </div>
          <div className="sila-studio-item-card__metric">
            <em>Menu / POS</em>
            <strong>{resolvedPos}</strong>
          </div>
        </div>
        <div className="sila-studio-item-card__actions">
          <button type="button" className="sila-studio-btn sila-studio-btn--sm" onClick={onView}>
            View
          </button>
          {onEdit && (
            <button
              type="button"
              className="sila-studio-btn sila-studio-btn--primary sila-studio-btn--sm"
              onClick={onEdit}
            >
              Edit
            </button>
          )}
          {resolvedOverflow.length > 0 && (
            <div className="sila-studio-overflow" ref={menuRef}>
              <button
                type="button"
                className="sila-studio-btn sila-studio-btn--sm"
                aria-label={`More actions for ${title}`}
                aria-expanded={menuOpen}
                aria-haspopup="menu"
                title="More Actions"
                onClick={() => setMenuOpen((o) => !o)}
              >
                <MoreVertical size={14} aria-hidden="true" />
              </button>
              {menuOpen && (
                <div className="sila-studio-overflow__menu" role="menu">
                  {resolvedOverflow.map((act) => (
                    <button
                      key={act.id}
                      type="button"
                      role="menuitem"
                      data-testid={act.testId}
                      className={`sila-studio-overflow__item ${act.danger ? "sila-studio-overflow__item--danger" : ""}`}
                      onClick={() => {
                        setMenuOpen(false);
                        act.onClick();
                      }}
                    >
                      {act.danger && <Trash2 size={13} aria-hidden="true" />}
                      <span>{act.label}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </article>
  );
};

export interface RecipeGridProps {
  children: React.ReactNode;
  variant?: "standard" | "compact";
  ariaLabel?: string;
}

export const RecipeGrid: React.FC<RecipeGridProps> = ({
  children,
  variant = "standard",
  ariaLabel = "Recipe catalogue",
}) => (
  <div
    className={`sila-studio-card-grid ${variant === "compact" ? "sila-studio-card-grid--compact" : ""}`}
    role="list"
    aria-label={ariaLabel}
  >
    {children}
  </div>
);

export interface MenuItemCardProps {
  name: string;
  code: string;
  linkedRecipeName?: string | null;
  category?: string | null;
  cuisine?: string | null;
  sellingPriceText: string;
  foodCostText?: string;
  foodCostPctText?: string;
  contributionMarginText?: string;
  popularityText?: string;
  posMappingStatus?: string;
  availabilityLabel?: string;
  classification?: string | null;
  imageUrl?: string | null;
  actions?: React.ReactNode;
}

const classificationTone = (cls?: string | null): string => {
  switch ((cls ?? "").toUpperCase()) {
    case "STAR":
      return "sila-studio-badge sila-studio-badge--success";
    case "PLOWHORSE":
      return "sila-studio-badge sila-studio-badge--info";
    case "PUZZLE":
      return "sila-studio-badge sila-studio-badge--warning";
    case "DOG":
      return "sila-studio-badge sila-studio-badge--error";
    default:
      return "sila-studio-badge sila-studio-badge--neutral";
  }
};

export const MenuItemCard: React.FC<MenuItemCardProps> = ({
  name,
  code,
  linkedRecipeName,
  category,
  cuisine,
  sellingPriceText,
  foodCostText,
  foodCostPctText,
  contributionMarginText,
  popularityText,
  posMappingStatus,
  availabilityLabel,
  classification,
  imageUrl,
  actions,
}) => (
  <article className="sila-studio-item-card" role="listitem">
    <div className="sila-studio-item-card__media" style={{ cursor: "default" }}>
      {imageUrl ? (
        <img src={imageUrl} alt={name} loading="lazy" />
      ) : (
        <div
          style={{
            width: "100%",
            height: "100%",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            gap: "0.35rem",
            background: "#f5faff",
            color: "#087BB5",
          }}
        >
          <UtensilsCrossed size={24} strokeWidth={1.6} aria-hidden="true" />
          <span style={{ fontSize: "0.7rem", fontWeight: 600, color: "#64748b" }}>
            {category || "Menu Item"}
          </span>
        </div>
      )}
      {classification && (
        <span className={`sila-studio-item-card__status ${classificationTone(classification)}`}>
          {classification}
        </span>
      )}
    </div>
    <div className="sila-studio-item-card__body">
      <h3 className="sila-studio-item-card__title">{name}</h3>
      <p className="sila-studio-item-card__meta">
        {[code, category, cuisine].filter(Boolean).join(" · ")}
      </p>
      <div className="sila-studio-item-card__stats">
        <span>Recipe: {linkedRecipeName || "Unlinked"}</span>
        {posMappingStatus && <span>POS: {posMappingStatus}</span>}
        {availabilityLabel && <span>{availabilityLabel}</span>}
        {popularityText && <span>Pop: {popularityText}</span>}
      </div>
      <div className="sila-studio-item-card__metrics">
        <div className="sila-studio-item-card__metric">
          <em>Selling Price</em>
          <strong>{sellingPriceText}</strong>
        </div>
        {foodCostText !== undefined && (
          <div className="sila-studio-item-card__metric">
            <em>Food Cost{foodCostPctText ? ` (${foodCostPctText})` : ""}</em>
            <strong>{foodCostText}</strong>
          </div>
        )}
        {contributionMarginText !== undefined && (
          <div className="sila-studio-item-card__metric">
            <em>Contribution Margin</em>
            <strong>{contributionMarginText}</strong>
          </div>
        )}
      </div>
      {actions && <div className="sila-studio-item-card__actions">{actions}</div>}
    </div>
  </article>
);

export interface MenuCanvasProps {
  title: string;
  subtitle?: string;
  headerActions?: React.ReactNode;
  children: React.ReactNode;
}

export const MenuCanvas: React.FC<MenuCanvasProps> = ({
  title,
  subtitle,
  headerActions,
  children,
}) => (
  <section className="sila-studio-panel" aria-label={title}>
    <div className="sila-studio-panel__header">
      <div>
        <h2 className="sila-studio-panel__title">{title}</h2>
        {subtitle && <p className="sila-studio-panel__subtitle">{subtitle}</p>}
      </div>
      {headerActions && (
        <div style={{ display: "inline-flex", alignItems: "center", gap: "0.4rem", flexWrap: "wrap" }}>
          {headerActions}
        </div>
      )}
    </div>
    {children}
  </section>
);

export interface DishLibraryDrawerProps {
  isOpen: boolean;
  pinned: boolean;
  onTogglePin: () => void;
  onClose: () => void;
  title?: string;
  children: React.ReactNode;
}

export const DishLibraryDrawer: React.FC<DishLibraryDrawerProps> = ({
  isOpen,
  pinned,
  onTogglePin,
  onClose,
  title = "Dish & Approved Recipe Library",
  children,
}) => {
  const [expanded, setExpanded] = useState(false);

  if (!isOpen) return null;

  if (pinned) {
    return (
      <aside className="sila-studio-panel" aria-label={title} data-testid="dish-library-pinned">
        <div className="sila-studio-panel__header">
          <h2 className="sila-studio-panel__title" style={{ display: "inline-flex", alignItems: "center", gap: "0.4rem" }}>
            <BookOpen size={15} color="#087BB5" aria-hidden="true" />
            <span>{title}</span>
          </h2>
          <div className="sila-studio-drawer__controls">
            <button
              type="button"
              className="sila-studio-btn sila-studio-btn--sm"
              onClick={onTogglePin}
              title="Unpin to floating drawer"
              aria-label="Unpin dish library"
            >
              <PinOff size={13} aria-hidden="true" />
            </button>
            <button
              type="button"
              className="sila-studio-btn sila-studio-btn--sm"
              onClick={onClose}
              title="Close dish library"
              aria-label="Close dish library"
            >
              <PanelLeftClose size={13} aria-hidden="true" />
            </button>
          </div>
        </div>
        {children}
      </aside>
    );
  }

  return (
    <StudioDrawer
      isOpen={isOpen}
      onClose={onClose}
      title={title}
      icon={<BookOpen size={16} color="#087BB5" aria-hidden="true" />}
      side="left"
      width={expanded ? 540 : 380}
      testId="dish-library-drawer"
      headerControls={
        <>
          <button
            type="button"
            className="sila-studio-btn sila-studio-btn--sm"
            onClick={() => setExpanded((e) => !e)}
            title={expanded ? "Compact width" : "Expand drawer"}
            aria-label={expanded ? "Compact drawer width" : "Expand drawer width"}
          >
            {expanded ? <Minimize2 size={13} aria-hidden="true" /> : <Maximize2 size={13} aria-hidden="true" />}
          </button>
          <button
            type="button"
            className="sila-studio-btn sila-studio-btn--sm"
            onClick={onTogglePin}
            title="Pin alongside canvas"
            aria-label="Pin dish library alongside canvas"
          >
            <Pin size={13} aria-hidden="true" />
          </button>
        </>
      }
    >
      {children}
    </StudioDrawer>
  );
};

export const DishLibraryAddIcon = Plus;
