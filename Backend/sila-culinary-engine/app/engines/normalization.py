from __future__ import annotations

import re
from decimal import Decimal, ROUND_HALF_UP
from pint import UnitRegistry, errors as pint_errors

_UREG = UnitRegistry()

CANONICAL_UOM_ALIASES: dict[str, str] = {
    "KG": "KG",
    "KGS": "KG",
    "KILOGRAM": "KG",
    "KILOGRAMS": "KG",
    "G": "G",
    "GM": "G",
    "GMS": "G",
    "GRAM": "G",
    "GRAMS": "G",
    "L": "L",
    "LT": "L",
    "LTR": "L",
    "LITER": "L",
    "LITERS": "L",
    "LITRE": "L",
    "LITRES": "L",
    "ML": "ML",
    "MILLILITER": "ML",
    "MILLILITERS": "ML",
    "MILLILITRE": "ML",
    "EA": "EA",
    "EACH": "EA",
    "PC": "PCS",
    "PCS": "PCS",
    "PIECE": "PCS",
    "PIECES": "PCS",
    "UNIT": "EA",
    "UNITS": "EA",
    "TBSP": "TBSP",
    "TABLESPOON": "TBSP",
    "TABLESPOONS": "TBSP",
    "TSP": "TSP",
    "TEASPOON": "TSP",
    "TEASPOONS": "TSP",
    "CUP": "CUP",
    "CUPS": "CUP",
    "PINCH": "PINCH",
    "BUNCH": "BUNCH",
    "CLOVE": "CLOVE",
    "CLOVES": "CLOVE",
}

MASS_UOMS = {"KG", "G"}
VOLUME_UOMS = {"L", "ML", "TBSP", "TSP", "CUP"}
COUNT_UOMS = {"EA", "PCS", "CLOVE", "BUNCH"}
UNRESOLVED_UOMS = {"PINCH"}

_PINT_MAP = {
    "KG": "kilogram",
    "G": "gram",
    "L": "liter",
    "ML": "milliliter",
    "TBSP": "tablespoon",
    "TSP": "teaspoon",
    "CUP": "cup",
}

_PREP_DESCRIPTORS = (
    "finely chopped",
    "roughly chopped",
    "chopped",
    "diced",
    "minced",
    "sliced",
    "julienned",
    "grated",
    "crushed",
    "peeled",
    "deveined",
    "trimmed",
    "washed",
    "blanched",
    "toasted",
)

_FORM_TOKENS = (
    "fresh",
    "frozen",
    "dried",
    "powder",
    "paste",
    "purée",
    "puree",
    "boneless",
    "bone-in",
    "skinless",
    "salted",
    "unsalted",
    "raw",
    "cooked",
    "whole",
    "sliced",
    "ground",
)

_ABBREVIATIONS: dict[str, str] = {
    r"\btbsp\b": "tablespoon",
    r"\btsp\b": "teaspoon",
    r"\bgm\b": "g",
    r"\bgms\b": "g",
    r"\bpcs\b": "pieces",
    r"\bw/\b": "with",
}


def normalize_uom(raw_uom: str | None) -> tuple[str, bool]:
    if not raw_uom or not raw_uom.strip():
        return ("UNRESOLVED", False)
    cleaned = re.sub(r"[^A-Za-z0-9]", "", raw_uom.strip()).upper()
    canonical = CANONICAL_UOM_ALIASES.get(cleaned)
    if canonical is None:
        return (raw_uom.strip().upper(), False)
    if canonical in UNRESOLVED_UOMS:
        return (canonical, False)
    return (canonical, True)


def convert_physical_quantity(
    qty: Decimal,
    from_uom: str,
    to_uom: str,
    density_g_per_ml: Decimal | None = None,
) -> Decimal:
    """
    Deterministic Pint-backed dimensional conversion (§18).
    Rejects mass <-> volume conversion unless verified density_g_per_ml is supplied.
    Rejects count <-> mass/volume conversions.
    """
    src, src_ok = normalize_uom(from_uom)
    dst, dst_ok = normalize_uom(to_uom)
    if not src_ok or not dst_ok:
        raise ValueError(f"Unresolved unit conversion from '{from_uom}' to '{to_uom}'.")
    if src == dst:
        return qty

    if (src in COUNT_UOMS) != (dst in COUNT_UOMS) or (src in COUNT_UOMS and dst in COUNT_UOMS and src != dst):
        raise ValueError(f"Cannot convert count unit '{src}' to '{dst}' without a validated piece weight factor.")

    src_is_mass = src in MASS_UOMS
    dst_is_mass = dst in MASS_UOMS
    src_is_vol = src in VOLUME_UOMS
    dst_is_vol = dst in VOLUME_UOMS

    if (src_is_mass and dst_is_vol) or (src_is_vol and dst_is_mass):
        if density_g_per_ml is None or density_g_per_ml <= 0:
            raise ValueError(
                f"Cannot convert between mass ({src}) and volume ({dst}) without an ingredient-specific density."
            )
        if src_is_mass:
            grams = convert_physical_quantity(qty, src, "G")
            ml = grams / density_g_per_ml
            return convert_physical_quantity(ml, "ML", dst)
        ml = convert_physical_quantity(qty, src, "ML")
        grams = ml * density_g_per_ml
        return convert_physical_quantity(grams, "G", dst)

    try:
        q = float(qty) * _UREG(_PINT_MAP[src])
        converted = q.to(_PINT_MAP[dst]).magnitude
        return Decimal(str(converted)).quantize(Decimal("0.0001"), rounding=ROUND_HALF_UP)
    except (KeyError, pint_errors.DimensionalityError, pint_errors.UndefinedUnitError) as exc:
        raise ValueError(f"Unsupported unit conversion {src} -> {dst}: {exc}") from exc


def normalize_ingredient_text(description: str) -> dict[str, str]:
    """
    Bounded culinary text normalization (§17).
    Preserves culinary specification distinctions (fresh vs cooking cream, boneless vs bone-in, salted vs unsalted).
    """
    original = " ".join((description or "").split()).strip()
    lowered = original.lower()
    for pattern, repl in _ABBREVIATIONS.items():
        lowered = re.sub(pattern, repl, lowered)

    prep_found: list[str] = []
    cleaned = lowered
    for prep in _PREP_DESCRIPTORS:
        if re.search(rf"\b{re.escape(prep)}\b", cleaned):
            prep_found.append(prep)
            cleaned = re.sub(rf"\b{re.escape(prep)}\b", " ", cleaned)

    forms_found: list[str] = []
    for form in _FORM_TOKENS:
        if re.search(rf"\b{re.escape(form)}\b", cleaned):
            forms_found.append(form)

    # Collapse punctuation/extra spaces while keeping culinary nouns and distinguishing modifiers intact
    cleaned = re.sub(r"[,;()]+", " ", cleaned)
    cleaned = " ".join(cleaned.split()).strip()

    return {
        "original_description": original,
        "normalized_description": cleaned.title() if cleaned else original,
        "ingredient_form": ", ".join(forms_found),
        "preparation_descriptor": ", ".join(prep_found),
    }
