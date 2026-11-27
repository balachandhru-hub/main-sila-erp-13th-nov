from __future__ import annotations

import re
import unicodedata
from app.schemas.material_ai import ExtractedAttributes

# Specification-bearing tokens are NEVER stripped because removing them alters product identity (§10).
_SPEC_KEEP_WORDS = {
    "salted",
    "unsalted",
    "fresh",
    "frozen",
    "smoked",
    "dried",
    "canned",
    "tinned",
    "raw",
    "cooked",
    "whole",
    "boneless",
    "bone-in",
    "skinless",
    "skin-on",
    "ground",
    "minced",
    "grated",
    "shredded",
    "sliced",
    "paste",
    "puree",
    "powder",
    "flakes",
    "juice",
    "concentrate",
    "cooking",
    "whipping",
    "heavy",
    "light",
    "skim",
    "low-fat",
    "full-fat",
    "organic",
    "halal",
    "vegan",
    "gluten-free",
    "extra",
    "virgin",
    "refined",
    "brown",
    "white",
    "black",
    "red",
    "green",
    "yellow",
}

_ABBREVIATION_PATTERNS: list[tuple[str, str]] = [
    (r"\bb\s*/\s*l\b", "boneless"),
    (r"\bb/l\b", "boneless"),
    (r"\bs/l\b", "skinless"),
    (r"\bb\s*&\s*s\b", "boneless skinless"),
    (r"\be\.?v\.?o\.?o\.?\b", "extra virgin olive oil"),
    (r"\bgm\b", "g"),
    (r"\bgms\b", "g"),
    (r"\bltr\b", "l"),
    (r"\bw/\b", "with"),
    (r"\bunsalt\b", "unsalted"),
    (r"\bchese\b", "cheese"),
    (r"\bchiken\b", "chicken"),
    (r"\bchkn\b", "chicken"),
    (r"\btomto\b", "tomato"),
    (r"\bpurée\b", "puree"),
    (r"\bpurée\b", "puree"),
]

# Multilingual and regional culinary equivalence map (§10, §13)
CULINARY_SYNONYM_MAP: dict[str, list[str]] = {
    "aubergine": ["eggplant", "brinjal"],
    "eggplant": ["aubergine", "brinjal"],
    "coriander": ["cilantro", "dhania"],
    "cilantro": ["coriander", "coriander leaves"],
    "capsicum": ["bell pepper", "sweet pepper"],
    "bell pepper": ["capsicum"],
    "garbanzo": ["chickpea", "chickpeas", "chana"],
    "chickpea": ["garbanzo", "garbanzo beans"],
    "scallion": ["spring onion", "green onion"],
    "spring onion": ["scallion", "green onion"],
    "courgette": ["zucchini"],
    "zucchini": ["courgette"],
    "prawn": ["shrimp"],
    "shrimp": ["prawn"],
    "rocket": ["arugula"],
    "arugula": ["rocket"],
    "methi": ["fenugreek"],
    "kasuri methi": ["dried fenugreek leaves", "fenugreek leaves dried"],
    "poulet": ["chicken"],
    "beurre": ["butter"],
    "queso": ["cheese"],
    "parmesano": ["parmesan"],
    "saumon": ["salmon"],
    "creme": ["cream"],
    "crème": ["cream"],
    "lait": ["milk"],
    "huile": ["oil"],
    "beurre doux": ["unsalted butter"],
    "beurre sale": ["salted butter"],
    "sans os": ["boneless"],
}

_PLURAL_EXCEPTIONS = {
    "molasses",
    "hummus",
    "couscous",
    "asparagus",
    "citrus",
    "lemongrass",
    "watercress",
    "bass",
    "swiss",
    "parmesan",
}

_PREP_NOISE_WORDS = {
    "finely",
    "roughly",
    "freshly",
    "lightly",
    "thinly",
    "thickly",
    "peeled",
    "deveined",
    "trimmed",
    "washed",
    "rinsed",
    "pat-dried",
    "chopped",
    "diced",
    "julienned",
    "crushed",
    "toasted",
}

_SPECIES_MAP = {
    "chicken": "chicken",
    "salmon": "salmon",
    "beef": "beef",
    "lamb": "lamb",
    "mutton": "lamb",
    "veal": "veal",
    "duck": "duck",
    "turkey": "turkey",
    "prawn": "prawn",
    "shrimp": "prawn",
    "tuna": "tuna",
    "cod": "cod",
    "hammour": "hammour",
    "sea bass": "sea bass",
    "lobster": "lobster",
    "crab": "crab",
    "squid": "squid",
}

_MEAT_CUTS = {
    "breast",
    "thigh",
    "wing",
    "drumstick",
    "tenderloin",
    "sirloin",
    "ribeye",
    "striploin",
    "fillet",
    "loin",
    "shank",
    "rack",
    "brisket",
    "chop",
    "cutlet",
}

_CATEGORY_KEYWORDS: list[tuple[str, set[str]]] = [
    ("DAIRY", {"butter", "cream", "milk", "cheese", "parmesan", "mozzarella", "cheddar", "yogurt", "ghee", "mascarpone", "ricotta"}),
    ("PROTEIN_POULTRY", {"chicken", "duck", "turkey", "quail"}),
    ("PROTEIN_SEAFOOD", {"salmon", "prawn", "shrimp", "tuna", "cod", "hammour", "bass", "lobster", "crab", "fish", "seafood"}),
    ("PROTEIN_MEAT", {"beef", "lamb", "veal", "mutton", "steak", "sirloin", "ribeye", "tenderloin"}),
    ("PRODUCE", {"tomato", "onion", "garlic", "eggplant", "aubergine", "capsicum", "pepper", "zucchini", "courgette", "carrot", "potato", "spinach", "lettuce", "lemon", "lime", "mushroom"}),
    ("HERBS_SPICES", {"coriander", "cilantro", "dill", "parsley", "basil", "thyme", "rosemary", "cumin", "turmeric", "paprika", "cinnamon", "cardamom", "saffron", "methi", "fenugreek", "oregano", "chili"}),
    ("OILS_FATS", {"olive oil", "oil", "sunflower", "canola", "sesame oil", "truffle oil"}),
    ("DRY_GOODS", {"flour", "sugar", "rice", "pasta", "spaghetti", "penne", "lentil", "chickpea", "garbanzo", "oats", "starch"}),
]

_ALLERGEN_KEYWORDS: dict[str, set[str]] = {
    "DAIRY": {"butter", "cream", "milk", "cheese", "parmesan", "mozzarella", "cheddar", "yogurt", "ghee", "whey", "casein"},
    "FISH": {"salmon", "tuna", "cod", "hammour", "bass", "anchovy", "fish"},
    "SHELLFISH": {"prawn", "shrimp", "lobster", "crab", "squid", "mussel", "clam"},
    "GLUTEN": {"wheat", "flour", "pasta", "spaghetti", "penne", "bread", "couscous", "semolina", "barley"},
    "TREE_NUTS": {"almond", "walnut", "cashew", "pistachio", "hazelnut", "pecan", "pine nut"},
    "PEANUTS": {"peanut"},
    "SOY": {"soy", "tofu", "edamame", "miso", "tamari"},
    "EGG": {"egg", "mayonnaise", "aioli"},
    "SESAME": {"sesame", "tahini"},
}


def _strip_accents(text: str) -> str:
    nfkd = unicodedata.normalize("NFKD", text)
    return "".join(ch for ch in nfkd if not unicodedata.combining(ch))


def _singularize_token(token: str) -> str:
    if len(token) <= 3 or token in _PLURAL_EXCEPTIONS:
        return token
    if token.endswith("ies") and len(token) > 4:
        return token[:-3] + "y"
    if token.endswith(("tomatoes", "potatoes")):
        return token[:-2]
    if token.endswith("ves") and len(token) > 4:
        if token in {"leaves", "loaves", "halves"}:
            return token[:-3] + "f"
    if token.endswith("es") and len(token) > 4 and token[-3] in ("s", "x", "z", "h"):
        return token[:-2]
    if token.endswith("s") and not token.endswith(("ss", "us", "is")):
        return token[:-1]
    return token


def normalize_for_matching(text: str) -> str:
    """
    Deterministic Unicode + culinary normalization preserving specification-bearing tokens.
    """
    raw = unicodedata.normalize("NFKC", text or "").strip()
    lowered = _strip_accents(raw.lower())
    for pattern, repl in _ABBREVIATION_PATTERNS:
        lowered = re.sub(pattern, repl, lowered)
    # Remove parenthetical notes only if they don't carry fat % or spec keywords
    lowered = re.sub(r"[,;/()]+", " ", lowered)
    tokens = []
    for tok in lowered.split():
        clean_tok = tok.strip(".-_:")
        if not clean_tok:
            continue
        if clean_tok in _PREP_NOISE_WORDS:
            continue
        tokens.append(_singularize_token(clean_tok))
    return " ".join(tokens)


def extract_ingredient_attributes(
    description: str,
    category_hint: str | None = None,
    brand_hint: str | None = None,
    form_hint: str | None = None,
) -> ExtractedAttributes:
    orig = " ".join((description or "").split()).strip()
    norm = normalize_for_matching(orig)
    norm_tokens = norm.split()
    lowered_orig = _strip_accents(orig.lower())

    # Fat percentage & concentration
    fat_match = re.search(r"\b(\d{1,2}(?:\.\d+)?)\s*%\s*(?:fat|bf|butterfat)?\b", lowered_orig)
    fat_pct = f"{fat_match.group(1)}%" if fat_match else None
    conc_match = re.search(r"\b(\d{1,2}(?:\.\d+)?)\s*%\s*(?:conc|concentration|brix|acidity)?\b", lowered_orig)
    concentration = f"{conc_match.group(1)}%" if (conc_match and not fat_match) else None

    # Pack size / weight / volume expressions (e.g. 500G, 1KG, 5L, 12x500ML)
    pack_match = re.search(r"\b(\d+(?:\.\d+)?\s*[xX×]\s*\d+(?:\.\d+)?\s*(?:kg|g|l|ml|pcs|ea|btl))\b", lowered_orig)
    weight_match = re.search(r"\b(\d+(?:\.\d+)?\s*(?:kg|g|gm|gms))\b", lowered_orig)
    vol_match = re.search(r"\b(\d+(?:\.\d+)?\s*(?:l|ltr|ml))\b", lowered_orig)

    # Salted / Unsalted
    salted_state = "UNKNOWN"
    if re.search(r"\bunsalted\b|\bsalt[- ]free\b|\bno added salt\b", norm):
        salted_state = "UNSALTED"
    elif re.search(r"\bsalted\b", norm):
        salted_state = "SALTED"

    # Fresh / Frozen / Smoked / Dried / Canned
    fresh_state = "UNKNOWN"
    if re.search(r"\bfrozen\b|\biqf\b", norm):
        fresh_state = "FROZEN"
    elif re.search(r"\bsmoked\b", norm):
        fresh_state = "SMOKED"
    elif re.search(r"\bdried\b|\bdehydrated\b", norm):
        fresh_state = "DRIED"
    elif re.search(r"\bcanned\b|\btinned\b", norm):
        fresh_state = "CANNED"
    elif re.search(r"\bfresh\b", norm):
        fresh_state = "FRESH"

    # Raw / Cooked
    raw_cooked = "UNKNOWN"
    if re.search(r"\bcooked\b|\broasted\b|\bgrilled\b|\bboiled\b", norm):
        raw_cooked = "COOKED"
    elif re.search(r"\braw\b", norm):
        raw_cooked = "RAW"

    # Whole / Processed form
    whole_proc = "UNKNOWN"
    for candidate_form in ("boneless", "bone-in", "fillet", "ground", "minced", "grated", "sliced", "paste", "puree", "powder", "whole"):
        if candidate_form in norm or candidate_form in lowered_orig:
            whole_proc = candidate_form.upper()
            break

    species = next((sp for key, sp in _SPECIES_MAP.items() if re.search(rf"\b{re.escape(key)}\b", norm)), None)
    meat_cut = next((cut for cut in _MEAT_CUTS if re.search(rf"\b{re.escape(cut)}\b", norm)), None)

    inferred_cat = category_hint.upper() if category_hint else None
    if not inferred_cat:
        for cat, kws in _CATEGORY_KEYWORDS:
            if any(re.search(rf"\b{re.escape(kw)}\b", norm) for kw in kws):
                inferred_cat = cat
                break

    dietary: list[str] = []
    for d in ("vegan", "vegetarian", "halal", "organic", "gluten-free", "dairy-free", "nut-free", "sugar-free"):
        if d in lowered_orig or d.replace("-", " ") in norm:
            dietary.append(d.upper().replace("-", "_"))

    allergens: list[str] = []
    for al, kws in _ALLERGEN_KEYWORDS.items():
        if any(re.search(rf"\b{re.escape(kw)}\b", norm) for kw in kws):
            allergens.append(al)

    spec_tokens = [t for t in norm_tokens if t in _SPEC_KEEP_WORDS]
    if fat_pct:
        spec_tokens.append(fat_pct)

    quality_specs: list[str] = []
    if re.search(r"\bextra virgin\b", norm):
        quality_specs.append("EXTRA_VIRGIN")
    if re.search(r"\bgrade a\b|\bpremium\b", norm):
        quality_specs.append("GRADE_A")
    if re.search(r"\bcooking\b", norm):
        quality_specs.append("COOKING_GRADE")
    if re.search(r"\bwhipping\b", norm):
        quality_specs.append("WHIPPING_GRADE")

    # Core culinary identity: strip pack size & storage modifiers while keeping core nouns + form
    identity_tokens = [
        t
        for t in norm_tokens
        if t not in {"fresh", "frozen", "raw", "cooked", "whole"}
        and not re.fullmatch(r"\d+(?:\.\d+)?(?:kg|g|l|ml|%)", t)
    ]
    identity = " ".join(identity_tokens) if identity_tokens else norm

    return ExtractedAttributes(
        original_description=orig,
        normalized_description=norm,
        canonical_tokens=sorted(set(norm_tokens)),
        ingredient_identity=identity,
        product_category=inferred_cat,
        ingredient_form=form_hint or (whole_proc if whole_proc != "UNKNOWN" else None),
        species=species,
        meat_cut=meat_cut,
        brand=brand_hint.strip() if brand_hint else None,
        fat_percentage=fat_pct,
        concentration=concentration,
        fresh_or_frozen=fresh_state,  # type: ignore[arg-type]
        raw_or_cooked=raw_cooked,  # type: ignore[arg-type]
        whole_or_processed=whole_proc,
        salted_or_unsalted=salted_state,  # type: ignore[arg-type]
        pack_size_expr=pack_match.group(1).upper() if pack_match else None,
        net_weight_expr=weight_match.group(1).upper() if weight_match else None,
        volume_expr=vol_match.group(1).upper() if vol_match else None,
        food_grade="FOOD_GRADE" if "food grade" in norm else None,
        dietary_flags=sorted(set(dietary)),
        allergen_flags=sorted(set(allergens)),
        quality_specification=quality_specs,
        specification_tokens=sorted(set(spec_tokens)),
    )


def expand_culinary_synonyms(norm_text: str) -> list[str]:
    variants = [norm_text]
    for phrase, syns in CULINARY_SYNONYM_MAP.items():
        if re.search(rf"\b{re.escape(phrase)}\b", norm_text):
            for syn in syns:
                variants.append(re.sub(rf"\b{re.escape(phrase)}\b", syn, norm_text))
    # Deduplicate preserving order
    seen: set[str] = set()
    out: list[str] = []
    for v in variants:
        if v not in seen:
            seen.add(v)
            out.append(v)
    return out


def check_specification_compatibility(
    query: ExtractedAttributes,
    candidate: ExtractedAttributes,
    required_allergen_free: list[str] | None = None,
    required_dietary: list[str] | None = None,
    material_brand: str | None = None,
    material_allergens: list[str] | None = None,
    material_dietary: list[str] | None = None,
) -> tuple[bool, float, list[str], list[str]]:
    """
    Evaluates mandatory specification compatibility (§10, §11, §16, §17).
    Returns (compatible, attribute_score_0_to_1, hard_conflicts, soft_warnings).
    Mandatory conflicts are returned as hard_conflicts so a lower price or higher
    generic similarity can NEVER promote an incompatible material (e.g., salted vs unsalted).
    """
    conflicts: list[str] = []
    warnings: list[str] = []
    score = 0.5  # neutral baseline

    # 1. Salted vs Unsalted (CRITICAL §10 / §16)
    if query.salted_or_unsalted != "UNKNOWN" and candidate.salted_or_unsalted != "UNKNOWN":
        if query.salted_or_unsalted != candidate.salted_or_unsalted:
            conflicts.append(
                f"Salt specification conflict: required {query.salted_or_unsalted} vs material {candidate.salted_or_unsalted}."
            )
        else:
            score += 0.15
    elif query.salted_or_unsalted == "UNSALTED" and candidate.salted_or_unsalted == "UNKNOWN":
        warnings.append("Material does not explicitly state UNSALTED; verify packaging.")

    # 2. Fresh vs Frozen / Smoked / Dried / Canned
    if query.fresh_or_frozen != "UNKNOWN" and candidate.fresh_or_frozen != "UNKNOWN":
        if query.fresh_or_frozen != candidate.fresh_or_frozen:
            conflicts.append(
                f"Storage/state conflict: required {query.fresh_or_frozen} vs material {candidate.fresh_or_frozen}."
            )
        else:
            score += 0.12

    # 3. Whole vs Boneless / Bone-in / Paste / Puree / Powder
    incompatible_forms = {
        ("BONELESS", "BONE-IN"),
        ("BONE-IN", "BONELESS"),
        ("BONELESS", "WHOLE"),
        ("PASTE", "PUREE"),
        ("PUREE", "PASTE"),
        ("PASTE", "POWDER"),
        ("POWDER", "PASTE"),
        ("PASTE", "FRESH"),
        ("POWDER", "FRESH"),
    }
    if query.whole_or_processed != "UNKNOWN" and candidate.whole_or_processed != "UNKNOWN":
        if (query.whole_or_processed, candidate.whole_or_processed) in incompatible_forms:
            conflicts.append(
                f"Ingredient form conflict: required {query.whole_or_processed} vs material {candidate.whole_or_processed}."
            )
        elif query.whole_or_processed == candidate.whole_or_processed:
            score += 0.12

    # 4. Species & meat cut check
    if query.species and candidate.species and query.species != candidate.species:
        conflicts.append(f"Species conflict: required {query.species} vs material {candidate.species}.")
    elif query.species and candidate.species and query.species == candidate.species:
        score += 0.10

    if query.meat_cut and candidate.meat_cut and query.meat_cut != candidate.meat_cut:
        conflicts.append(f"Cut conflict: required {query.meat_cut} vs material {candidate.meat_cut}.")
    elif query.meat_cut and candidate.meat_cut and query.meat_cut == candidate.meat_cut:
        score += 0.08

    # 5. Brand preference check when explicitly specified
    cand_brand = (material_brand or candidate.brand or "").strip().lower()
    if query.brand:
        q_brand = query.brand.strip().lower()
        if cand_brand and q_brand not in cand_brand and cand_brand not in q_brand:
            conflicts.append(f"Brand mismatch: requested '{query.brand}' vs material '{material_brand or candidate.brand}'.")
        elif cand_brand:
            score += 0.08

    # 6. Fat % check (e.g. 82% butter vs 35% cream)
    if query.fat_percentage and candidate.fat_percentage and query.fat_percentage != candidate.fat_percentage:
        warnings.append(f"Fat percentage differs: requested {query.fat_percentage} vs material {candidate.fat_percentage}.")
    elif query.fat_percentage and candidate.fat_percentage and query.fat_percentage == candidate.fat_percentage:
        score += 0.08

    # 7. Cream culinary distinction (fresh dairy cream vs cooking cream vs whipping cream §18)
    if "COOKING_GRADE" in candidate.quality_specification and "COOKING_GRADE" not in query.quality_specification:
        if "cream" in query.normalized_description:
            warnings.append("Cooking cream and fresh dairy cream may have different culinary characteristics.")
    if "WHIPPING_GRADE" in candidate.quality_specification and "WHIPPING_GRADE" not in query.quality_specification:
        if "cream" in query.normalized_description:
            warnings.append("Whipping cream specification differs from generic fresh cream.")

    # 8. Allergen & dietary enforcement
    cand_allergens = set(a.upper() for a in (material_allergens or [])) | set(candidate.allergen_flags)
    for req_free in (required_allergen_free or []):
        if req_free.strip().upper() in cand_allergens:
            conflicts.append(f"Allergen conflict: recipe requires {req_free.upper()}-free, candidate contains {req_free.upper()}.")

    cand_dietary = set(d.upper() for d in (material_dietary or [])) | set(candidate.dietary_flags)
    for req_diet in (required_dietary or []):
        rd = req_diet.strip().upper()
        if rd == "DAIRY_FREE" and "DAIRY" in cand_allergens:
            conflicts.append("Dietary conflict: recipe requires DAIRY_FREE, candidate contains DAIRY.")
        elif rd == "GLUTEN_FREE" and "GLUTEN" in cand_allergens:
            conflicts.append("Dietary conflict: recipe requires GLUTEN_FREE, candidate contains GLUTEN.")
        elif rd == "NUT_FREE" and ("TREE_NUTS" in cand_allergens or "PEANUTS" in cand_allergens):
            conflicts.append("Dietary conflict: recipe requires NUT_FREE, candidate contains nuts.")
        elif cand_dietary and rd not in cand_dietary and rd in {"HALAL", "VEGAN", "ORGANIC"}:
            warnings.append(f"Material does not explicitly list {rd} certification.")

    # 9. Category compatibility boost
    if query.product_category and candidate.product_category:
        if query.product_category == candidate.product_category:
            score += 0.10
        else:
            warnings.append(f"Category differs ({query.product_category} vs {candidate.product_category}).")

    compatible = len(conflicts) == 0
    if not compatible:
        score = max(0.0, score - 0.5)
    return compatible, min(1.0, max(0.0, round(score, 4))), conflicts, warnings
