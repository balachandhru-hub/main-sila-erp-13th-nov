-- SILA ME GET_MATERIAL API config for SILA Test Buyer (Material Master pull from S/4).
-- Source recovery: application handlers already live under PullSilaMaterialsFromErp /
-- SilaMaterialErpSync / ResolveIntegration (entity then ALL). This script restores the
-- missing tenant Integration row that those handlers resolve.
--
-- SAP service (standard Product API; no credentials invented):
--   API_PRODUCT_SRV / A_Product
-- Related entity sets (not required for the header upsert; map later if needed):
--   A_ProductDescription, A_ProductUnitsOfMeasure, A_ProductPlant
--
-- Credentials: copied from the existing ACTIVE FIVE S4 - GET PO row (same BASIC user).
-- Idempotent: an existing ACTIVE GET_MATERIAL for this buyer is left as-is.
--
-- ResolveIntegration: ACTIVE GET_MATERIAL for EntityCode ALL covers company code 1060
-- (and any other code) when no company-specific row exists.
--
-- DO NOT run against a live database without confirming the target DB and obtaining
-- authorization. This script does not call SAP; it only inserts configuration + mappings.

DECLARE @Buyer uniqueidentifier = '15ce77a3-df16-4474-a5da-f874551782d8';
DECLARE @Actor uniqueidentifier = '5c4474fc-5eac-410b-bff4-c04f51195ff6';
DECLARE @SourcePo uniqueidentifier = '21f72653-7a88-470a-b4d5-c396178de92b';
DECLARE @MaterialId uniqueidentifier = 'e7a1c2d3-4b5e-6f70-8192-a3b4c5d6e7f8';
DECLARE @Now datetime2 = SYSUTCDATETIME();

DECLARE @S4User nvarchar(250);
DECLARE @S4Password nvarchar(max);
DECLARE @S4Base nvarchar(500);

SELECT @S4User = username,
       @S4Password = protected_password,
       @S4Base = base_url
FROM buyersystem.api_integration_configuration
WHERE id = @SourcePo
  AND buyer_id = @Buyer
  AND is_active = 1;

IF @S4User IS NULL OR @S4Base IS NULL
BEGIN
    THROW 50001, 'Source S4 GET_PO integration 21f72653-7a88-470a-b4d5-c396178de92b not found; cannot copy credentials.', 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM buyersystem.api_integration_configuration
    WHERE buyer_id = @Buyer
      AND process_type = N'GET_MATERIAL'
      AND status = N'ACTIVE'
      AND is_active = 1
)
BEGIN
    INSERT INTO buyersystem.api_integration_configuration
    (
        id, buyer_id, entity_code, name, process_type, protocol, system_name,
        base_url, resource_path, authentication_type, username, protected_password,
        http_method, payload_format, request_body, headers_json,
        timeout_seconds, retry_count, page_size, is_running, status,
        date_created, date_updated, created_by, updated_by, is_active
    )
    VALUES
    (
        @MaterialId, @Buyer, N'ALL', N'FIVE S4 - GET MATERIAL', N'GET_MATERIAL', N'ODATA_V2', N'SAP S/4',
        @S4Base,
        N'sap/opu/odata/sap/API_PRODUCT_SRV/A_Product?$expand=to_Description',
        N'BASIC', @S4User, @S4Password,
        N'GET', N'JSON', NULL,
        N'{"Accept":"application/json","DataServiceVersion":"2.0","MaxDataServiceVersion":"2.0"}',
        60, 2, 100, 0, N'ACTIVE',
        @Now, @Now, @Actor, @Actor, 1
    );
END;

-- Prefer the fixed id when we just inserted it; otherwise the existing ACTIVE row.
DECLARE @Config uniqueidentifier =
(
    SELECT TOP (1) id
    FROM buyersystem.api_integration_configuration
    WHERE buyer_id = @Buyer
      AND process_type = N'GET_MATERIAL'
      AND status = N'ACTIVE'
      AND is_active = 1
    ORDER BY CASE WHEN id = @MaterialId THEN 0 ELSE 1 END, date_created
);

IF @Config IS NULL
BEGIN
    THROW 50002, 'No ACTIVE GET_MATERIAL configuration available after insert.', 1;
END;

-- Ensure the recovered SAP Product path (with description expand) on the chosen ACTIVE row
-- when still blank, a placeholder, or missing to_Description expand.
UPDATE buyersystem.api_integration_configuration
SET resource_path = N'sap/opu/odata/sap/API_PRODUCT_SRV/A_Product?$expand=to_Description',
    protocol = N'ODATA_V2',
    system_name = N'SAP S/4',
    http_method = N'GET',
    date_updated = @Now,
    updated_by = @Actor
WHERE id = @Config
  AND (
        resource_path IS NULL
     OR LTRIM(RTRIM(resource_path)) = N''
     OR resource_path LIKE N'%example.invalid%'
     OR resource_path NOT LIKE N'%API_PRODUCT_SRV%'
     OR (
          resource_path LIKE N'%A_Product%'
      AND resource_path NOT LIKE N'%to_Description%'
        )
  );

-- Header fields supported by SilaMaterialErpSync (Material.Code required).
-- Description: map S/4 Product description expand (to_Description/results/0/ProductDescription).
-- IntegrationRecordReader supports numeric array indexes on OData results[]; sync also falls back
-- to ReadSapProductDescription (prefer EN) when the mapped value is empty.
-- Prefer resource expand $expand=to_Description when the connection allows it.
-- Optional plant / SLoc / UOM / valuation targets (map when A_ProductPlant / SLoc / UoM / valuation
-- payloads are available on the configured connection — do not invent field paths):
--   Material.Plant, Material.PlantStatus, Material.PurchasingGroup, Material.MrpType,
--   Material.ProcurementType, Material.ReorderPoint, Material.SafetyStock,
--   Material.StorageLocation, Material.AltUom, Material.AltUomNumerator, Material.AltUomDenominator,
--   Material.ValuationClass, Material.PriceControl, Material.StandardPrice,
--   Material.MovingAveragePrice, Material.PriceUnit, Material.MaterialType
-- Candidate SAP entity sets (activation not assumed): A_ProductPlant, A_ProductStorageLocation,
-- A_ProductUnitsOfMeasure, A_ProductValuation, API_MATERIAL_STOCK_SRV.
;WITH maps AS (
    SELECT * FROM (VALUES
        (N'Product', N'Material.Code', N'UPPER'),
        (N'BaseUnit', N'Material.BaseUom', N'UPPER'),
        (N'ProductGroup', N'Material.MaterialGroup', N'TRIM'),
        (N'to_Description/results/0/ProductDescription', N'Material.Description', N'TRIM'),
        (N'ProductType', N'Material.MaterialType', N'TRIM')
    ) AS v(source_field, target_field, transformation)
)
INSERT INTO buyersystem.api_field_mapping
(
    id, configuration_id, source_field, target_field, transformation, null_policy, default_value,
    is_validated, date_created, date_updated, created_by, updated_by, is_active
)
SELECT
    NEWID(), @Config, m.source_field, m.target_field, m.transformation, N'IGNORE_NULL', NULL,
    1, @Now, @Now, @Actor, @Actor, 1
FROM maps m
WHERE NOT EXISTS (
    SELECT 1 FROM buyersystem.api_field_mapping have
    WHERE have.configuration_id = @Config
      AND have.target_field = m.target_field
      AND have.is_active = 1
);
