import React, { useEffect, useState } from 'react';
import { getRecipePicture } from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';

/** Loads an authenticated recipe picture blob into an <img>. */
const RecipePicture: React.FC<{ recipeId: string; hasPicture?: boolean; className?: string; preferDraft?: boolean }> = ({
  recipeId,
  hasPicture,
  className,
  preferDraft = false,
}) => {
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    let objectUrl: string | null = null;
    if (!hasPicture) {
      setUrl(null);
      return undefined;
    }
    getRecipePicture(recipeId, preferDraft)
      .then((blob) => {
        if (!active) return;
        objectUrl = URL.createObjectURL(blob);
        setUrl(objectUrl);
      })
      .catch(() => {
        if (active) setUrl(null);
      });
    return () => {
      active = false;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [recipeId, hasPicture, preferDraft]);

  if (!url) return <div className={className} aria-hidden="true" />;
  return <img className={className} src={url} alt="" />;
};

export default RecipePicture;
