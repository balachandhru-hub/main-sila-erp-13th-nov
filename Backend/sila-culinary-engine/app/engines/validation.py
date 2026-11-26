from __future__ import annotations

import re
from decimal import Decimal
from app.engines.normalization import normalize_ingredient_text, normalize_uom
from app.schemas.recipe_ai import (
    IngredientLineContract,
    StructuredRecipePayload,
    ValidationWarning,
)

# Heuristic allergen & dietary keyword detectors (assist chef review; never replace verified supplier specs)
_ALLERGEN_KEYWORDS: dict[str, tuple[str, ...]] = {
    "Dairy / Milk": ("butter", "cream", "milk", "cheese", "paneer", "yogurt", "ghee", "whey"),
    "Fish": ("salmon", "cod", "tuna", "sea bass", "snapper", "fish"),
    "Crustacean Shellfish": ("shrimp", "prawn", "crab", "lobster"),
    "Tree Nuts": ("cashew", "almond", "walnut", "pistachio", "hazelnut", "pecan"),
    "Peanuts": ("peanut", "groundnut"),
    "Gluten / Wheat": ("wheat", "flour", "maida", "bread", "pasta", "soy sauce", "semolina"),
    "Eggs": ("egg", "mayonnaise", "aioli"),
    "Soy": ("soy", "tofu", "edamame", "miso"),
    "Sesame": ("sesame", "tahini"),
}

_NON_VEGAN_TOKENS = (
    "chicken", "beef", "lamb", "mutton", "pork", "fish", "salmon", "shrimp", "prawn",
    "butter", "cream", "milk", "cheese", "paneer", "yogurt", "ghee", "egg", "honey",
)
_NON_VEG_TOKENS = ("chicken", "beef", "lamb", "mutton", "pork", "fish", "salmon", "shrimp", "prawn", "bacon", "anchovy")
_GLUTEN_TOKENS = ("wheat", "flour", "maida", "barley", "rye", "bread", "pasta", "couscous", "semolina")
_DAIRY_TOKENS = ("butter", "cream", "milk", "cheese", "paneer", "yogurt", "ghee", "whey")
_NUT_TOKENS = ("cashew", "almond", "walnut", "pistachio", "hazelnut", "pecan", "peanut")


def enrich_and_validate_recipe(payload: StructuredRecipePayload) -> tuple[StructuredRecipePayload, list[ValidationWarning]]:
    """
    Validates & normalizes a generated or edited StructuredRecipePayload (§16, §17, §27, §44).
    - Preserves original descriptions while adding normalized_description & normalized_uom.
    - Forces Phase 1B handover defaults (material_mapping_status = PENDING_MATERIAL_MAPPING, material_reference = None).
    - Flags unresolved units, duplicate ingredients, sub-recipe circular dependencies, dietary/allergen conflicts,
      and requires explicit chef verification.
    """
    warnings: list[ValidationWarning] = []

    if not payload.header.proposed_name.strip():
        warnings.append(ValidationWarning(code="MISSING_NAME", severity="ERROR", field="header.proposed_name", message="Recipe name is required."))

    if payload.header.base_servings <= Decimal("0"):
        warnings.append(ValidationWarning(code="INVALID_SERVINGS", severity="ERROR", field="header.base_servings", message="Base servings must be positive."))

    if payload.header.serving_size <= Decimal("0"):
        warnings.append(ValidationWarning(code="INVALID_SERVING_SIZE", severity="ERROR", field="header.serving_size", message="Serving size must be positive."))

    # Never mark AI output as certified nutrition or allergen-free without verified source
    payload.header.chef_verification_required = True
    if not payload.header.nutrition_status:
        payload.header.nutrition_status = "UNAVAILABLE_OR_UNVERIFIED"

    seen_norm: dict[str, str] = {}
    detected_allergens: set[str] = set(payload.header.potential_allergens)

    def _process_lines(lines: list[IngredientLineContract], prefix: str) -> None:
        for idx, line in enumerate(lines):
            if not line.temp_ref.strip():
                line.temp_ref = f"{prefix}-{idx + 1}"
            norm = normalize_ingredient_text(line.description)
            line.normalized_description = norm["normalized_description"]
            if not line.ingredient_form and norm["ingredient_form"]:
                line.ingredient_form = norm["ingredient_form"]

            norm_uom, uom_ok = normalize_uom(line.recipe_uom)
            line.normalized_uom = norm_uom
            line.uom_resolved = uom_ok
            if not uom_ok:
                warnings.append(
                    ValidationWarning(
                        code="UNRESOLVED_UOM",
                        severity="WARNING",
                        field=f"ingredients[{line.temp_ref}].recipe_uom",
                        message=f"Ingredient '{line.description}' uses unit '{line.recipe_uom}' requiring chef confirmation.",
                    )
                )

            if line.quantity <= Decimal("0"):
                warnings.append(
                    ValidationWarning(
                        code="MISSING_OR_ZERO_QUANTITY",
                        severity="WARNING",
                        field=f"ingredients[{line.temp_ref}].quantity",
                        message=f"Ingredient '{line.description}' has zero quantity; confirm before Phase 1B mapping.",
                    )
                )

            if line.yield_percent <= Decimal("0") or line.yield_percent > Decimal("100"):
                warnings.append(
                    ValidationWarning(
                        code="INVALID_YIELD",
                        severity="ERROR",
                        field=f"ingredients[{line.temp_ref}].yield_percent",
                        message=f"Yield percentage for '{line.description}' must be between 1 and 100.",
                    )
                )

            # Enforce Phase 1B handover contract: never allow invented material codes in Phase 1A
            line.material_mapping_status = "PENDING_MATERIAL_MAPPING"
            line.material_reference = None

            key = f"{line.normalized_description.lower()}::{line.normalized_uom}"
            if prefix == "ING":
                if key in seen_norm:
                    warnings.append(
                        ValidationWarning(
                            code="DUPLICATE_INGREDIENT",
                            severity="WARNING",
                            field=f"ingredients[{line.temp_ref}]",
                            message=f"Possible duplicate ingredient '{line.description}' (matches {seen_norm[key]}).",
                        )
                    )
                else:
                    seen_norm[key] = line.temp_ref

            lowered_desc = line.description.lower()
            for allergen_name, tokens in _ALLERGEN_KEYWORDS.items():
                if any(re.search(rf"\b{re.escape(tok)}\b", lowered_desc) for tok in tokens):
                    detected_allergens.add(allergen_name)

            # Flag non-linear spice/seasoning items if still marked LINEAR
            if line.scaling_method == "LINEAR" and any(
                k in lowered_desc for k in ("salt", "masala", "chilli", "pepper", "baking powder", "yeast", "kasuri", "spice")
            ):
                warnings.append(
                    ValidationWarning(
                        code="NONLINEAR_SCALING_CANDIDATE",
                        severity="INFO",
                        field=f"ingredients[{line.temp_ref}].scaling_method",
                        message=f"'{line.description}' is a spice/seasoning/leavening agent; review scaling method when scaling batches.",
                    )
                )

    _process_lines(payload.ingredients, "ING")

    # Sub-recipe cycle detection & nested validation
    sub_names_seen: set[str] = set()
    parent_name_norm = payload.header.proposed_name.strip().lower()
    for s_idx, sub in enumerate(payload.sub_recipes):
        sub_norm = sub.sub_recipe_name.strip().lower()
        if sub_norm == parent_name_norm or sub_norm in sub_names_seen:
            warnings.append(
                ValidationWarning(
                    code="CIRCULAR_SUB_RECIPE",
                    severity="ERROR",
                    field=f"sub_recipes[{sub.sub_recipe_ref}]",
                    message=f"Circular or duplicate sub-recipe reference detected: '{sub.sub_recipe_name}'.",
                )
            )
        sub_names_seen.add(sub_norm)
        _process_lines(sub.ingredients, f"SUB{s_idx + 1}")

    payload.header.potential_allergens = sorted(detected_allergens)

    # Instruction step reference check
    step_nums = {st.step_number for st in payload.instructions}
    for st in payload.instructions:
        if st.dependency_step is not None and (st.dependency_step not in step_nums or st.dependency_step >= st.step_number):
            warnings.append(
                ValidationWarning(
                    code="INVALID_STEP_DEPENDENCY",
                    severity="WARNING",
                    field=f"instructions[{st.step_number}]",
                    message=f"Step {st.step_number} references dependency step {st.dependency_step} which does not precede it.",
                )
            )

    # Dietary tag contradiction checks
    all_ing_text = " ".join(
        [i.description.lower() for i in payload.ingredients]
        + [si.description.lower() for sr in payload.sub_recipes for si in sr.ingredients]
    )
    tags_lower = {t.strip().lower() for t in payload.header.dietary_tags}
    if "vegan" in tags_lower and any(re.search(rf"\b{re.escape(tok)}\b", all_ing_text) for tok in _NON_VEGAN_TOKENS):
        warnings.append(
            ValidationWarning(
                code="DIETARY_CONTRADICTION",
                severity="WARNING",
                field="header.dietary_tags",
                message="Recipe is tagged 'Vegan' but contains animal-derived ingredients; verify before saving.",
            )
        )
    if "vegetarian" in tags_lower and any(re.search(rf"\b{re.escape(tok)}\b", all_ing_text) for tok in _NON_VEG_TOKENS):
        warnings.append(
            ValidationWarning(
                code="DIETARY_CONTRADICTION",
                severity="WARNING",
                field="header.dietary_tags",
                message="Recipe is tagged 'Vegetarian' but contains meat/poultry/seafood.",
            )
        )
    if ("gluten-free" in tags_lower or "gluten free" in tags_lower) and any(
        re.search(rf"\b{re.escape(tok)}\b", all_ing_text) for tok in _GLUTEN_TOKENS
    ):
        warnings.append(
            ValidationWarning(
                code="DIETARY_CONTRADICTION",
                severity="WARNING",
                field="header.dietary_tags",
                message="Recipe is tagged 'Gluten-Free' but lists gluten-containing ingredients.",
            )
        )
    if ("dairy-free" in tags_lower or "dairy free" in tags_lower) and any(
        re.search(rf"\b{re.escape(tok)}\b", all_ing_text) for tok in _DAIRY_TOKENS
    ):
        warnings.append(
            ValidationWarning(
                code="DIETARY_CONTRADICTION",
                severity="WARNING",
                field="header.dietary_tags",
                message="Recipe is tagged 'Dairy-Free' but lists dairy ingredients.",
            )
        )
    if ("nut-free" in tags_lower or "nut free" in tags_lower) and any(
        re.search(rf"\b{re.escape(tok)}\b", all_ing_text) for tok in _NUT_TOKENS
    ):
        warnings.append(
            ValidationWarning(
                code="DIETARY_CONTRADICTION",
                severity="WARNING",
                field="header.dietary_tags",
                message="Recipe is tagged 'Nut-Free' but lists tree nuts or peanuts.",
            )
        )

    if any(t in ("halal", "allergen-free", "nut-free", "gluten-free") for t in tags_lower):
        warnings.append(
            ValidationWarning(
                code="CHEF_CERTIFICATION_NOTICE",
                severity="INFO",
                field="header.dietary_tags",
                message="Dietary & allergen indicators are AI-assisted and do not constitute certified compliance without verified supplier specifications and kitchen controls.",
            )
        )

    return payload, warnings
