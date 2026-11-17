-- SILA Test Buyer: link FL01 / FL10 outlets to plant 1060 and set on-hand 50
-- for materials 1000000 and 1000001 at both outlets.
-- Also upserts material plant + storage-location assignments and location stocking rows.
-- Idempotent. Does not post to SAP.

DECLARE @Buyer uniqueidentifier = '15ce77a3-df16-4474-a5da-f874551782d8';
DECLARE @Actor uniqueidentifier = '5c4474fc-5eac-410b-bff4-c04f51195ff6';
DECLARE @PropertyId uniqueidentifier = 'a5d48259-b0f0-441d-a529-5c7f0385b78e'; -- already referenced by FL locations
DECLARE @OutletFl01 uniqueidentifier = '7c3336f0-5296-411c-94af-4306398e6c0c';
DECLARE @OutletFl10 uniqueidentifier = 'de25e2be-bae7-49bf-82d9-c2f0dc5a5b7c';
DECLARE @Qty decimal(18, 4) = 50;
DECLARE @Plant nvarchar(20) = N'1060';

IF NOT EXISTS (SELECT 1 FROM buyersystem.buyer_business_profile WHERE id = @Buyer AND is_active = 1)
    THROW 50001, 'SILA Test Buyer is missing.', 1;

-- 1) Restore property / plant 1060 (locations already point at @PropertyId).
IF NOT EXISTS (SELECT 1 FROM buyersystem.buyer_property WHERE id = @PropertyId)
BEGIN
    INSERT INTO buyersystem.buyer_property
        (id, buyer_id, company_code, plant_code, property_name,
         master_approval_flow_id, wishlist_approval_flow_id,
         purchasing_group, purchasing_organization,
         date_created, date_updated, created_by, updated_by, is_active)
    VALUES
        (@PropertyId, @Buyer, @Plant, @Plant, N'FIVE Luxe Hotel LLC',
         NULL, NULL, N'001', N'1000',
         SYSUTCDATETIME(), SYSUTCDATETIME(), @Actor, @Actor, 1);
END
ELSE
BEGIN
    UPDATE buyersystem.buyer_property
    SET company_code = @Plant,
        plant_code = @Plant,
        property_name = COALESCE(NULLIF(property_name, N''), N'FIVE Luxe Hotel LLC'),
        is_active = 1,
        date_updated = SYSUTCDATETIME(),
        updated_by = @Actor
    WHERE id = @PropertyId AND buyer_id = @Buyer;
END

-- 2) Restore buyer outlets FL01 / FL10 and keep inventory_location.outlet_id aligned.
IF NOT EXISTS (SELECT 1 FROM buyersystem.buyer_outlet WHERE id = @OutletFl01)
BEGIN
    INSERT INTO buyersystem.buyer_outlet
        (id, buyer_id, outlet_name, outlet_code, description, property_id, storage_location,
         date_created, date_updated, created_by, updated_by, is_active)
    VALUES
        (@OutletFl01, @Buyer, N'FOC INV BEVERAGE', N'FL01', N'FOC INV BEVERAGE', @PropertyId, N'FL01',
         SYSUTCDATETIME(), SYSUTCDATETIME(), @Actor, @Actor, 1);
END
ELSE
BEGIN
    UPDATE buyersystem.buyer_outlet
    SET outlet_code = N'FL01',
        outlet_name = N'FOC INV BEVERAGE',
        storage_location = N'FL01',
        property_id = @PropertyId,
        is_active = 1,
        date_updated = SYSUTCDATETIME(),
        updated_by = @Actor
    WHERE id = @OutletFl01 AND buyer_id = @Buyer;
END

IF NOT EXISTS (SELECT 1 FROM buyersystem.buyer_outlet WHERE id = @OutletFl10)
BEGIN
    INSERT INTO buyersystem.buyer_outlet
        (id, buyer_id, outlet_name, outlet_code, description, property_id, storage_location,
         date_created, date_updated, created_by, updated_by, is_active)
    VALUES
        (@OutletFl10, @Buyer, N'GOOSE ISLAND BEV', N'FL10', N'GOOSE ISLAND BEV', @PropertyId, N'FL10',
         SYSUTCDATETIME(), SYSUTCDATETIME(), @Actor, @Actor, 1);
END
ELSE
BEGIN
    UPDATE buyersystem.buyer_outlet
    SET outlet_code = N'FL10',
        outlet_name = N'GOOSE ISLAND BEV',
        storage_location = N'FL10',
        property_id = @PropertyId,
        is_active = 1,
        date_updated = SYSUTCDATETIME(),
        updated_by = @Actor
    WHERE id = @OutletFl10 AND buyer_id = @Buyer;
END

-- 3) Point venue + outlets at plant 1060 property and storage-location codes.
UPDATE buyersystem.inventory_location
SET property_id = @PropertyId,
    storage_location_code = CASE location_code
        WHEN N'FL01' THEN N'FL01'
        WHEN N'FL10' THEN N'FL10'
        ELSE storage_location_code
    END,
    outlet_id = CASE location_code
        WHEN N'FL01' THEN @OutletFl01
        WHEN N'FL10' THEN @OutletFl10
        ELSE outlet_id
    END,
    is_active = 1,
    date_updated = SYSUTCDATETIME(),
    updated_by = @Actor
WHERE buyer_id = @Buyer
  AND location_code IN (N'1060', N'FL01', N'FL10');

IF NOT EXISTS (
    SELECT 1 FROM buyersystem.inventory_location
    WHERE buyer_id = @Buyer AND location_code = N'FL01' AND property_id = @PropertyId AND is_active = 1
)
    THROW 50002, 'FL01 is not linked to plant 1060 after update.', 1;

IF NOT EXISTS (
    SELECT 1 FROM buyersystem.inventory_location
    WHERE buyer_id = @Buyer AND location_code = N'FL10' AND property_id = @PropertyId AND is_active = 1
)
    THROW 50003, 'FL10 is not linked to plant 1060 after update.', 1;

-- 4) Materials (existing SILA codes for company 1060).
DECLARE @Want TABLE (material_code nvarchar(50) NOT NULL PRIMARY KEY);
INSERT INTO @Want (material_code) VALUES (N'1000000'), (N'1000001');

IF EXISTS (
    SELECT 1 FROM @Want w
    WHERE NOT EXISTS (
        SELECT 1 FROM buyersystem.item_buyer_master m
        WHERE m.buyer_id = @Buyer AND m.material_code = w.material_code AND m.is_active = 1
    )
)
    THROW 50004, 'Material 1000000 or 1000001 is missing for SILA Test Buyer.', 1;

DECLARE @Pick TABLE (
    material_code nvarchar(50) NOT NULL PRIMARY KEY,
    material_id uniqueidentifier NOT NULL,
    base_uom nvarchar(20) NOT NULL,
    unit_cost decimal(18, 4) NULL
);

INSERT INTO @Pick (material_code, material_id, base_uom, unit_cost)
SELECT material_code, id, base_uom, unit_cost
FROM (
    SELECT
        w.material_code,
        m.id,
        UPPER(LTRIM(RTRIM(CASE
            WHEN m.base_unit_of_measure IS NULL OR LTRIM(RTRIM(m.base_unit_of_measure)) = N'' THEN N'EA'
            ELSE m.base_unit_of_measure
        END))) AS base_uom,
        m.unit_cost,
        ROW_NUMBER() OVER (
            PARTITION BY w.material_code
            ORDER BY CASE WHEN m.company_code = @Plant THEN 0 ELSE 1 END, m.date_created
        ) AS rn
    FROM @Want w
    JOIN buyersystem.item_buyer_master m
        ON m.buyer_id = @Buyer AND m.material_code = w.material_code AND m.is_active = 1
) ranked
WHERE rn = 1;

-- 5) Plant assignments (1060) + storage-location assignments (FL01, FL10).
MERGE buyersystem.material_plant_assignment AS target
USING @Pick AS source
ON target.buyer_id = @Buyer AND target.material_id = source.material_id AND target.plant_code = @Plant
WHEN MATCHED THEN
    UPDATE SET
        is_active = 1,
        plant_status = COALESCE(target.plant_status, N'ACTIVE'),
        source_system = COALESCE(target.source_system, N'MANUAL'),
        last_synced_on = SYSUTCDATETIME(),
        date_updated = SYSUTCDATETIME(),
        updated_by = @Actor
WHEN NOT MATCHED THEN
    INSERT (
        id, buyer_id, material_id, plant_code, plant_status,
        last_synced_on, source_system,
        date_created, date_updated, created_by, updated_by, is_active
    )
    VALUES (
        NEWID(), @Buyer, source.material_id, @Plant, N'ACTIVE',
        SYSUTCDATETIME(), N'MANUAL',
        SYSUTCDATETIME(), SYSUTCDATETIME(), @Actor, @Actor, 1
    );

DECLARE @Sloc TABLE (storage_location_code nvarchar(50) NOT NULL, location_id uniqueidentifier NOT NULL);
INSERT INTO @Sloc (storage_location_code, location_id)
SELECT l.location_code, l.id
FROM buyersystem.inventory_location l
WHERE l.buyer_id = @Buyer AND l.location_code IN (N'FL01', N'FL10') AND l.is_active = 1;

MERGE buyersystem.material_storage_location_assignment AS target
USING (
    SELECT p.material_id, s.storage_location_code, s.location_id
    FROM @Pick p
    CROSS JOIN @Sloc s
) AS source
ON target.buyer_id = @Buyer
   AND target.material_id = source.material_id
   AND target.plant_code = @Plant
   AND target.storage_location_code = source.storage_location_code
WHEN MATCHED THEN
    UPDATE SET
        inventory_location_id = source.location_id,
        assignment_status = N'MAPPED',
        source_system = COALESCE(target.source_system, N'MANUAL'),
        last_synced_on = SYSUTCDATETIME(),
        is_active = 1,
        date_updated = SYSUTCDATETIME(),
        updated_by = @Actor
WHEN NOT MATCHED THEN
    INSERT (
        id, buyer_id, material_id, plant_code, storage_location_code, inventory_location_id,
        assignment_status, last_synced_on, source_system,
        date_created, date_updated, created_by, updated_by, is_active
    )
    VALUES (
        NEWID(), @Buyer, source.material_id, @Plant, source.storage_location_code, source.location_id,
        N'MAPPED', SYSUTCDATETIME(), N'MANUAL',
        SYSUTCDATETIME(), SYSUTCDATETIME(), @Actor, @Actor, 1
    );

-- 6) Targets: both outlets × both materials.
DECLARE @Targets TABLE (
    location_id uniqueidentifier NOT NULL,
    location_code nvarchar(50) NOT NULL,
    material_id uniqueidentifier NOT NULL,
    material_code nvarchar(50) NOT NULL,
    base_uom nvarchar(20) NOT NULL,
    unit_cost decimal(18, 4) NULL
);

INSERT INTO @Targets (location_id, location_code, material_id, material_code, base_uom, unit_cost)
SELECT s.location_id, s.storage_location_code, p.material_id, p.material_code, p.base_uom, p.unit_cost
FROM @Pick p
CROSS JOIN @Sloc s;

-- 7) Location stocking (inventory_location_material) — required for outlet material lists / transfers.
MERGE buyersystem.inventory_location_material AS target
USING @Targets AS source
ON target.location_id = source.location_id AND target.material_id = source.material_id
WHEN MATCHED THEN
    UPDATE SET
        is_active = 1,
        stocking_type = COALESCE(NULLIF(target.stocking_type, N''), N'REGULAR'),
        date_updated = SYSUTCDATETIME(),
        updated_by = @Actor
WHEN NOT MATCHED THEN
    INSERT (
        id, location_id, material_id, minimum_stock, par_level, reorder_point,
        maximum_stock, safety_stock, stocking_type,
        date_created, date_updated, created_by, updated_by, is_active
    )
    VALUES (
        NEWID(), source.location_id, source.material_id, NULL, NULL, NULL,
        NULL, NULL, N'REGULAR',
        SYSUTCDATETIME(), SYSUTCDATETIME(), @Actor, @Actor, 1
    );

-- 8) On-hand qty 50 at both FL01 and FL10.
MERGE buyersystem.inventory_balance AS target
USING @Targets AS source
ON target.location_id = source.location_id AND target.material_id = source.material_id
WHEN MATCHED THEN
    UPDATE SET
        on_hand_qty = @Qty,
        base_uom = source.base_uom,
        unit_cost = COALESCE(source.unit_cost, target.unit_cost),
        last_movement_on = SYSUTCDATETIME(),
        is_active = 1,
        date_updated = SYSUTCDATETIME(),
        updated_by = @Actor
WHEN NOT MATCHED THEN
    INSERT (
        id, buyer_id, location_id, material_id, on_hand_qty, in_transit_qty,
        base_uom, unit_cost, last_movement_on, date_created, date_updated,
        created_by, updated_by, is_active
    )
    VALUES (
        NEWID(), @Buyer, source.location_id, source.material_id, @Qty, 0,
        source.base_uom, source.unit_cost, SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME(),
        @Actor, @Actor, 1
    );

DECLARE @Start int = (
    SELECT ISNULL(MAX(TRY_CONVERT(int, SUBSTRING(transaction_number, 3, 6))), 0)
    FROM buyersystem.inventory_transaction
    WHERE buyer_id = @Buyer
      AND transaction_number LIKE 'IT[0-9][0-9][0-9][0-9][0-9][0-9]'
);

INSERT INTO buyersystem.inventory_transaction (
    id, buyer_id, transaction_number, transaction_type, location_id, material_id,
    direction, quantity, base_uom, entered_quantity, entered_uom, unit_cost, value,
    reference_type, reference_id, reference_number, reason, business_date,
    date_created, date_updated, created_by, updated_by, is_active
)
SELECT
    NEWID(),
    @Buyer,
    N'IT' + RIGHT(N'000000' + CAST(@Start + ROW_NUMBER() OVER (ORDER BY t.location_code, t.material_code) AS varchar(6)), 6),
    N'OPENING_STOCK',
    t.location_id,
    t.material_id,
    N'IN',
    @Qty,
    t.base_uom,
    @Qty,
    t.base_uom,
    t.unit_cost,
    t.unit_cost * @Qty,
    N'ADJUSTMENT',
    b.id,
    t.location_code + N'-' + t.material_code,
    N'Opening stock set to 50 at ' + t.location_code + N' (plant 1060)',
    CAST(SYSUTCDATETIME() AS date),
    SYSUTCDATETIME(),
    SYSUTCDATETIME(),
    @Actor,
    @Actor,
    1
FROM @Targets t
JOIN buyersystem.inventory_balance b
    ON b.location_id = t.location_id AND b.material_id = t.material_id
WHERE NOT EXISTS (
    SELECT 1
    FROM buyersystem.inventory_transaction existing
    WHERE existing.buyer_id = @Buyer
      AND existing.location_id = t.location_id
      AND existing.material_id = t.material_id
      AND existing.transaction_type = N'OPENING_STOCK'
      AND existing.reference_number = t.location_code + N'-' + t.material_code
      AND existing.is_active = 1
);
