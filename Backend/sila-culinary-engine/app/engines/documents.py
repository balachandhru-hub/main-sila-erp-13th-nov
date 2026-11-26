from __future__ import annotations

import base64
import io
import re
from decimal import Decimal
import openpyxl
from docx import Document as DocxDocument
from PIL import Image, UnidentifiedImageError
from pypdf import PdfReader
from app.schemas.recipe_ai import ExtractedDocumentResult

MAX_DOC_BYTES = 5 * 1024 * 1024
MAX_IMAGE_BYTES = 6 * 1024 * 1024
MAX_IMAGE_DIM = 4096

ALLOWED_IMAGE_FORMATS = {"JPEG": "image/jpeg", "PNG": "image/png", "WEBP": "image/webp"}

# Neutralize spreadsheet formula prefixes & common prompt-injection markers inside untrusted uploads
_FORMULA_PREFIX_RE = re.compile(r"^[=+\-@]+")
_INJECTION_PATTERNS = (
    re.compile(r"ignore\s+(all\s+)?previous\s+instructions", re.IGNORECASE),
    re.compile(r"system\s+prompt", re.IGNORECASE),
    re.compile(r"</?untrusted_recipe_document>", re.IGNORECASE),
    re.compile(r"execute\s+(sql|shell|command)", re.IGNORECASE),
)


def sanitize_untrusted_text(raw: str) -> tuple[str, list[str]]:
    warnings: list[str] = []
    text = raw.replace("\x00", "").strip()
    for pat in _INJECTION_PATTERNS:
        if pat.search(text):
            text = pat.sub("[REDACTED_INSTRUCTION_LIKE_TEXT]", text)
            if "Potential prompt-injection text was neutralized in the uploaded document." not in warnings:
                warnings.append("Potential prompt-injection text was neutralized in the uploaded document.")
    return text[:12000], warnings


def _clean_cell(val: object) -> str:
    if val is None:
        return ""
    s = str(val).strip()
    if _FORMULA_PREFIX_RE.match(s):
        # Strip leading spreadsheet formula triggers so exported/reused text cannot carry DDE/formula payloads
        return s.lstrip("=+-@").strip()
    return s


def extract_recipe_document(filename: str, content_bytes: bytes) -> ExtractedDocumentResult:
    if len(content_bytes) == 0:
        raise ValueError("Uploaded recipe document is empty.")
    if len(content_bytes) > MAX_DOC_BYTES:
        raise ValueError("Uploaded recipe document exceeds the 5 MB Phase 1A limit.")

    ext = filename.rsplit(".", 1)[-1].lower() if "." in filename else ""
    warnings: list[str] = []
    raw_lines: list[str] = []

    if ext == "txt":
        raw_lines = content_bytes.decode("utf-8", errors="replace").splitlines()
    elif ext == "pdf":
        reader = PdfReader(io.BytesIO(content_bytes))
        for page in reader.pages[:25]:
            raw_lines.extend((page.extract_text() or "").splitlines())
    elif ext == "docx":
        doc = DocxDocument(io.BytesIO(content_bytes))
        for para in doc.paragraphs:
            if para.text.strip():
                raw_lines.append(para.text)
        for table in doc.tables:
            for row in table.rows:
                cells = [_clean_cell(c.text) for c in row.cells]
                if any(cells):
                    raw_lines.append(" | ".join(cells))
    elif ext == "xlsx":
        wb = openpyxl.load_workbook(io.BytesIO(content_bytes), data_only=True, read_only=True)
        for sheet in wb.worksheets[:5]:
            raw_lines.append(f"[Sheet: {sheet.title}]")
            for row in sheet.iter_rows(values_only=True):
                cells = [_clean_cell(c) for c in row]
                if any(cells):
                    raw_lines.append(" | ".join(cells))
        wb.close()
    else:
        raise ValueError(f"Unsupported recipe document type '.{ext}'. Allowed: PDF, DOCX, XLSX, TXT.")

    joined, inj_warnings = sanitize_untrusted_text("\n".join(ln.strip() for ln in raw_lines if ln.strip()))
    warnings.extend(inj_warnings)

    lines = [ln for ln in joined.splitlines() if ln.strip()]
    if not lines:
        warnings.append("No readable text could be extracted from the uploaded file.")

    detected_title = lines[0][:140] if lines else None
    detected_servings: Decimal | None = None
    missing_qty: list[str] = []
    unclear_units: list[str] = []

    for ln in lines:
        srv_match = re.search(r"(?:servings?|portions?|yields?)\s*[:\-]?\s*(\d+(?:\.\d+)?)", ln, re.IGNORECASE)
        if srv_match and detected_servings is None:
            detected_servings = Decimal(srv_match.group(1))
        if re.search(r"\b(to taste|as needed|q\.?s\.?|pinch)\b", ln, re.IGNORECASE):
            unclear_units.append(ln[:120])
        elif re.match(r"^[-*•]\s*[A-Za-z]", ln) and not re.search(r"\d", ln):
            missing_qty.append(ln[:120])

    if detected_servings is None:
        warnings.append("Serving count was not explicitly detected in the document; verify before saving.")

    return ExtractedDocumentResult(
        filename=filename,
        file_type=ext.upper(),
        extracted_text=joined,
        line_count=len(lines),
        detected_title=detected_title,
        detected_servings=detected_servings,
        missing_quantities_detected=missing_qty[:20],
        unclear_units_detected=unclear_units[:20],
        warnings=warnings,
    )


def validate_and_prepare_dish_image(filename: str, content_bytes: bytes) -> tuple[str, str, int, int]:
    """
    Validates image bytes with Pillow, checks dimensions, strips EXIF metadata,
    and returns (base64_str, mime_type, width, height).
    """
    if len(content_bytes) == 0:
        raise ValueError("Uploaded dish image is empty.")
    if len(content_bytes) > MAX_IMAGE_BYTES:
        raise ValueError("Dish image exceeds 6 MB limit.")

    try:
        with Image.open(io.BytesIO(content_bytes)) as img:
            fmt = (img.format or "").upper()
            if fmt not in ALLOWED_IMAGE_FORMATS:
                raise ValueError(f"Unsupported image format '{fmt or filename}'. Allowed: JPEG, PNG, WebP.")
            w, h = img.size
            if w < 16 or h < 16 or w > MAX_IMAGE_DIM or h > MAX_IMAGE_DIM:
                raise ValueError(f"Image dimensions ({w}x{h}) must be between 16x16 and {MAX_IMAGE_DIM}x{MAX_IMAGE_DIM}.")
            clean = img.convert("RGB") if fmt == "JPEG" else img.copy()
            clean.thumbnail((1536, 1536))
            out_buf = io.BytesIO()
            clean.save(out_buf, format=fmt)
            encoded = base64.b64encode(out_buf.getvalue()).decode("ascii")
            return encoded, ALLOWED_IMAGE_FORMATS[fmt], clean.width, clean.height
    except UnidentifiedImageError as exc:
        raise ValueError("Uploaded file is not a valid JPEG, PNG, or WebP image.") from exc
