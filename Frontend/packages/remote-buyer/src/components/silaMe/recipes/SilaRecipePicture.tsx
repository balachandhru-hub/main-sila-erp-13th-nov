import React, { useEffect, useRef, useState } from "react";
import { getRecipePicture } from "../../../api/silaMe/silaRecipeApi";
import { resolveStudioKind, StudioCover } from "./recipeStudioVisuals";
import { RECIPE_PICTURE_ACCEPT, validateRecipePictureFile } from "./recipeStudioPicture";

interface SilaRecipePictureProps {
  /** Persisted recipe id when available. */
  recipeId?: string | null;
  /** When true, load the draft-slot picture for editors. */
  preferDraft?: boolean;
  /** Server says a picture exists (optional; fetch still attempted when recipeId is set). */
  hasPicture?: boolean;
  /** Local object URL / data URL for unsaved New Recipe preview. */
  localPreviewUrl?: string | null;
  /** Category/family/name used for the neutral placeholder illustration. */
  placeholderTitle?: string;
  placeholderCategory?: string | null;
  placeholderFamily?: string | null;
  locked?: boolean;
  /** Compact catalog/detail media frame. */
  variant?: "studio" | "catalog" | "detail";
  onChooseFile?: (file: File) => void | Promise<void>;
  onRemove?: () => void | Promise<void>;
  busy?: boolean;
  error?: string | null;
}

/**
 * Authenticated recipe picture frame with choose/replace/remove for Studio,
 * and catalog/detail display with category illustration fallback (never fabricated dish photos).
 */
const SilaRecipePicture: React.FC<SilaRecipePictureProps> = ({
  recipeId,
  preferDraft = false,
  hasPicture = false,
  localPreviewUrl = null,
  placeholderTitle = "Recipe",
  placeholderCategory,
  placeholderFamily,
  locked = false,
  variant = "studio",
  onChooseFile,
  onRemove,
  busy = false,
  error = null,
}) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const [remoteUrl, setRemoteUrl] = useState<string | null>(null);
  const [loadFailed, setLoadFailed] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    let objectUrl: string | null = null;
    setLoadFailed(false);
    setRemoteUrl(null);
    if (!recipeId || localPreviewUrl) return undefined;
    if (hasPicture === false) return undefined;
    getRecipePicture(recipeId, preferDraft)
      .then((blob) => {
        if (!active) return;
        objectUrl = URL.createObjectURL(blob);
        setRemoteUrl(objectUrl);
      })
      .catch(() => {
        if (active) setLoadFailed(true);
      });
    return () => {
      active = false;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [recipeId, preferDraft, hasPicture, localPreviewUrl]);

  const preview = localPreviewUrl || remoteUrl;
  const kind = resolveStudioKind(placeholderCategory, placeholderFamily, placeholderTitle);
  const showControls = variant === "studio" && !locked && Boolean(onChooseFile);
  const message = error || localError;

  const pick = async (file: File | undefined) => {
    setLocalError(null);
    if (!file || !onChooseFile) return;
    const check = await validateRecipePictureFile(file);
    if (!check.ok) {
      setLocalError(check.error);
      return;
    }
    await onChooseFile(file);
  };

  return (
    <div className={`rstudio-picture rstudio-picture--${variant}`} data-testid="recipe-picture">
      <div className="rstudio-picture__frame" aria-label="Recipe picture">
        {preview ? (
          <img src={preview} alt={placeholderTitle} className="rstudio-picture__img" />
        ) : (
          <StudioCover
            kind={kind}
            title={placeholderTitle}
            subtitle={placeholderCategory || placeholderFamily || (loadFailed ? "No picture" : "Add a picture")}
          />
        )}
      </div>
      {showControls && (
        <div className="rstudio-picture__actions">
          <input
            ref={inputRef}
            type="file"
            accept={RECIPE_PICTURE_ACCEPT}
            capture="environment"
            hidden
            onChange={(event) => {
              const file = event.target.files?.[0];
              event.target.value = "";
              void pick(file);
            }}
          />
          <button
            type="button"
            className="sila-btn sila-btn--secondary sila-btn--sm"
            disabled={busy}
            data-testid="recipe-picture-choose"
            onClick={() => inputRef.current?.click()}
          >
            {busy ? "Saving…" : preview ? "Replace picture" : "Choose picture"}
          </button>
          {preview && onRemove && (
            <button
              type="button"
              className="sila-btn sila-btn--ghost sila-btn--sm"
              disabled={busy}
              data-testid="recipe-picture-remove"
              onClick={() => { void onRemove(); }}
            >
              Remove
            </button>
          )}
        </div>
      )}
      {message && (
        <p className="rstudio-picture__error" role="alert" data-testid="recipe-picture-error">{message}</p>
      )}
    </div>
  );
};

export default SilaRecipePicture;
