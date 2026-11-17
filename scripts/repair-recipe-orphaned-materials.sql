-- Remap recipe ingredients whose MaterialId no longer exists in item_buyer_master
-- to the current active Item Master row with the same material_code (item_code).
-- Also copy UOM conversions onto the surviving material when missing.
-- Safe to re-run.

SET NOCOUNT ON;

;WITH orphan AS (
    SELECT
        i.id AS ingredient_id,
        i.material_id AS old_material_id,
        i.item_code,
        r.buyer_id,
        r.recipe_code
    FROM buyersystem.recipe_ingredient i
    INNER JOIN buyersystem.recipe r ON r.id = i.recipe_id
    LEFT JOIN buyersystem.item_buyer_master m ON m.id = i.material_id
    WHERE i.is_active = 1
      AND i.material_id IS NOT NULL
      AND m.id IS NULL
      AND NULLIF(LTRIM(RTRIM(i.item_code)), '') IS NOT NULL
),
target AS (
    SELECT
        o.ingredient_id,
        o.old_material_id,
        o.item_code,
        o.recipe_code,
        cur.id AS new_material_id,
        ROW_NUMBER() OVER (
            PARTITION BY o.ingredient_id
            ORDER BY cur.date_updated DESC, cur.date_created DESC
        ) AS rn
    FROM orphan o
    INNER JOIN buyersystem.item_buyer_master cur
        ON cur.buyer_id = o.buyer_id
       AND cur.is_active = 1
       AND UPPER(LTRIM(RTRIM(cur.material_code))) = UPPER(LTRIM(RTRIM(o.item_code)))
)
SELECT recipe_code, item_code, old_material_id, new_material_id
INTO #remap
FROM target
WHERE rn = 1;

PRINT 'Remap rows:';
SELECT * FROM #remap;

UPDATE i
SET i.material_id = r.new_material_id,
    i.date_updated = SYSUTCDATETIME()
FROM buyersystem.recipe_ingredient i
INNER JOIN #remap r ON r.ingredient_id = i.id;

-- Copy UOM conversions from orphaned material ids when the new material has none.
INSERT INTO buyersystem.material_uom_conversion (
    id, buyer_id, material_id, from_uom, to_uom, factor,
    date_created, date_updated, created_by, updated_by, is_active
)
SELECT
    NEWID(),
    oldc.buyer_id,
    r.new_material_id,
    oldc.from_uom,
    oldc.to_uom,
    oldc.factor,
    SYSUTCDATETIME(),
    SYSUTCDATETIME(),
    oldc.created_by,
    oldc.updated_by,
    1
FROM #remap r
INNER JOIN buyersystem.material_uom_conversion oldc
    ON oldc.material_id = r.old_material_id AND oldc.is_active = 1
WHERE NOT EXISTS (
    SELECT 1
    FROM buyersystem.material_uom_conversion existing
    WHERE existing.material_id = r.new_material_id
      AND existing.is_active = 1
      AND UPPER(existing.from_uom) = UPPER(oldc.from_uom)
      AND UPPER(existing.to_uom) = UPPER(oldc.to_uom)
);

DROP TABLE #remap;
