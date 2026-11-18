/** Client-side recipe picture validation (mirrors SilaRecipePictureRules). */

export const RECIPE_PICTURE_MAX_BYTES = 5 * 1024 * 1024;
export const RECIPE_PICTURE_ACCEPT = "image/jpeg,image/png,image/webp,.jpg,.jpeg,.png,.webp";

const JPEG = [0xff, 0xd8, 0xff];
const PNG = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

const startsWith = (bytes: Uint8Array, signature: number[]): boolean =>
  bytes.length >= signature.length && signature.every((value, index) => bytes[index] === value);

const isWebp = (bytes: Uint8Array): boolean =>
  bytes.length >= 12
  && bytes[0] === 0x52 && bytes[1] === 0x49 && bytes[2] === 0x46 && bytes[3] === 0x46
  && bytes[8] === 0x57 && bytes[9] === 0x45 && bytes[10] === 0x42 && bytes[11] === 0x50;

export type RecipePictureKind = "JPEG" | "PNG" | "WEBP";

export const detectRecipePictureKind = (bytes: Uint8Array): RecipePictureKind | null => {
  if (startsWith(bytes, JPEG)) return "JPEG";
  if (startsWith(bytes, PNG)) return "PNG";
  if (isWebp(bytes)) return "WEBP";
  return null;
};

export const validateRecipePictureFile = async (file: File): Promise<{ ok: true; kind: RecipePictureKind } | { ok: false; error: string }> => {
  if (!file || file.size <= 0) return { ok: false, error: "Choose a JPEG, PNG or WebP image." };
  if (file.size > RECIPE_PICTURE_MAX_BYTES) return { ok: false, error: "Upload a recipe picture of at most 5 MB." };
  const buffer = new Uint8Array(await file.slice(0, 16).arrayBuffer());
  const kind = detectRecipePictureKind(buffer);
  if (!kind) return { ok: false, error: "Upload the picture as a JPEG, PNG or WebP image. SVG and executable files are not allowed." };
  // Decode to confirm the browser can render it.
  try {
    if (typeof createImageBitmap === "function") {
      const bitmap = await createImageBitmap(file);
      bitmap.close();
    }
  } catch {
    return { ok: false, error: "The image could not be decoded. Choose another JPEG, PNG or WebP file." };
  }
  return { ok: true, kind };
};
