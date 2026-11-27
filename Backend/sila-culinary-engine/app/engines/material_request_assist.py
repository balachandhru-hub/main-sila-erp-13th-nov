from __future__ import annotations

from rapidfuzz import fuzz

from app.engines.ingredient_attributes import extract_ingredient_attributes, normalize_for_matching
from app.schemas.costing_ai import MaterialRequestAssistRequest, MaterialRequestAssistResponse


_CATEGORY_DEFAULTS = {
    "PRODUCE": ("HERBS_PRODUCE", "KG", "Chilled 2-5C dry ventilated", []),
    "HERBS_SPICES": ("SPICES_SEASONING", "KG", "Ambient cool dry <25C", []),
    "DAIRY": ("DAIRY_CHILLED", "KG", "Chilled 0-4C", ["Dairy"]),
    "PROTEIN": ("MEAT_POULTRY", "KG", "Chilled 0-4C or Frozen -18C", []),
    "MEAT": ("MEAT_POULTRY", "KG", "Chilled 0-4C or Frozen -18C", []),
    "SEAFOOD": ("SEAFOOD", "KG", "Chilled 0-2C on ice", ["Fish/Shellfish"]),
    "SPICE": ("SPICES_SEASONING", "KG", "Ambient cool dry <25C", []),
    "OIL_FAT": ("OILS_FATS", "L", "Ambient cool dark <25C", []),
    "DRY_GOODS": ("DRY_GOODS", "KG", "Ambient dry storage", []),
}


def assist_material_request(req: MaterialRequestAssistRequest) -> MaterialRequestAssistResponse:
    """
    Deterministic + attribute-driven Material Request preparation helper (§8 & §10):
    - Standardizes proposed description and classification without inventing ERP material codes or accounting classes.
    - Scans existing tenant materials for potential duplicates, inactive records, or plant-extension candidates.
    """
    attrs = extract_ingredient_attributes(req.ingredient_description)
    norm = attrs.normalized_description or normalize_for_matching(req.ingredient_description)
    cat_key = (attrs.product_category or "CULINARY").upper()
    group, default_uom, storage, default_allergens = _CATEGORY_DEFAULTS.get(
        cat_key, ("CULINARY_INGREDIENTS", "KG", "Ambient / Check supplier spec", [])
    )

    duplicates: list[dict] = []
    rec_type = "NEW_MATERIAL"
    for mat in req.existing_materials:
        mat_norm = normalize_for_matching(mat.description)
        score = fuzz.token_set_ratio(norm, mat_norm)
        if score >= 75:
            reason = "SIMILAR_ACTIVE_MATERIAL"
            if not mat.is_active:
                reason = "INACTIVE_MATERIAL_FOUND"
                rec_type = "MATERIAL_REACTIVATION"
            elif not mat.plant_eligible:
                reason = "NOT_EXTENDED_TO_PLANT"
                rec_type = "MATERIAL_EXTENSION"
            duplicates.append(
                {
                    "material_id": mat.material_id,
                    "material_code": mat.material_code,
                    "description": mat.description,
                    "similarity": round(score / 100.0, 3),
                    "is_active": mat.is_active,
                    "plant_eligible": mat.plant_eligible,
                    "reason": reason,
                }
            )

    duplicates.sort(key=lambda d: d["similarity"], reverse=True)
    base_uom = (req.recipe_uom or default_uom).upper()
    if base_uom in ("G", "GRAM", "GRAMS"):
        base_uom = "KG"
    elif base_uom in ("ML", "MILLILITER"):
        base_uom = "L"

    proposed_desc = norm.upper()[:120]
    spec_parts = [f"Category: {cat_key}"]
    if attrs.salted_or_unsalted != "UNKNOWN":
        spec_parts.append(f"Salt: {attrs.salted_or_unsalted}")
    if attrs.fresh_or_frozen != "UNKNOWN":
        spec_parts.append(f"State: {attrs.fresh_or_frozen}")
    if attrs.ingredient_form:
        spec_parts.append(f"Form: {attrs.ingredient_form}")
    if attrs.fat_percentage:
        spec_parts.append(f"Fat: {attrs.fat_percentage}")

    merged_allergens = sorted(set(default_allergens + list(attrs.allergen_flags or [])))

    justification = (
        f"Required for recipe '{req.recipe_name}'."
        if req.recipe_name
        else "Required for AI Recipe Planner culinary production."
    )
    if duplicates:
        justification += f" {len(duplicates)} potential existing candidate(s) flagged for Master Data duplicate/extension verification."

    return MaterialRequestAssistResponse(
        recommended_request_type=rec_type,
        proposed_description=proposed_desc,
        proposed_long_description=f"{proposed_desc} ({'; '.join(spec_parts)})",
        ingredient_category=cat_key,
        proposed_material_group=group,
        proposed_base_uom=base_uom,
        product_specification="; ".join(spec_parts),
        storage_requirement=storage,
        potential_allergens=merged_allergens,
        business_justification=justification,
        potential_duplicates=duplicates[:5],
        provider_used="LOCAL_DETERMINISTIC",
        ai_suggested=True,
    )
