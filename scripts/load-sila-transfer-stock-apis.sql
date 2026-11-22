-- SILA ME Transfer Stock API configs for SILA Test Buyer.
-- Activates FIVE S4 - Transfer Stock (311/312) and FIVE S4 - Plant Transfer Stock (301/302).
-- Copies S4 credentials from the existing Transfer / GRN row. Idempotent.
-- POS sales consumption stays on FIVE_POS_UPDATE — this script does not touch it.

DECLARE @Buyer uniqueidentifier = '15ce77a3-df16-4474-a5da-f874551782d8';
DECLARE @Actor uniqueidentifier = '5c4474fc-5eac-410b-bff4-c04f51195ff6';
DECLARE @TransferId uniqueidentifier = 'd4223a44-b6bf-4bd6-b7cd-520c8f56a6b0';
DECLARE @PlantTransferId uniqueidentifier = '9f9be018-acd5-4045-ae4f-1c10aa8ac991';
DECLARE @Now datetime2 = SYSUTCDATETIME();

DECLARE @S4User nvarchar(250);
DECLARE @S4Password nvarchar(max);
DECLARE @BaseUrl nvarchar(500) = N'https://my419951-api.s4hana.cloud.sap';
DECLARE @Resource nvarchar(500) = N'sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader';
DECLARE @Headers nvarchar(max) = N'{"Accept":"application/json","Content-Type":"application/json","DataServiceVersion":"2.0","MaxDataServiceVersion":"2.0","x-csrf-token":"Fetch"}';

-- Prefer credentials already on Transfer Stock / Plant Transfer / GRN.
SELECT TOP 1 @S4User = username, @S4Password = protected_password
FROM buyersystem.api_integration_configuration
WHERE buyer_id = @Buyer
  AND is_active = 1
  AND name IN (
      N'FIVE S4 - Transfer Stock', N'S4 - Transfer Stock',
      N'FIVE S4 - Plant Transfer Stock', N'S4 - Plant Transfer Stock',
      N'FIVE S4-GRN POSTING', N'S4 - Goods Issue'
  )
  AND username IS NOT NULL
  AND protected_password IS NOT NULL
ORDER BY CASE
    WHEN name IN (N'FIVE S4 - Transfer Stock', N'S4 - Transfer Stock') THEN 0
    WHEN name IN (N'FIVE S4 - Plant Transfer Stock', N'S4 - Plant Transfer Stock') THEN 1
    ELSE 2
END;

IF @S4User IS NULL
BEGIN
    THROW 50001, 'No S4 credentials found to activate FIVE S4 - Transfer Stock.', 1;
END;

-- Rename legacy S4 display names to the Integration Setup names mobile/cloud resolve.
UPDATE buyersystem.api_integration_configuration
SET name = N'FIVE S4 - Transfer Stock',
    process_type = N'POST_TRANSFER_311',
    status = N'ACTIVE',
    is_active = 1,
    date_updated = @Now,
    updated_by = @Actor
WHERE buyer_id = @Buyer
  AND name = N'S4 - Transfer Stock'
  AND NOT EXISTS (
      SELECT 1 FROM buyersystem.api_integration_configuration
      WHERE buyer_id = @Buyer AND name = N'FIVE S4 - Transfer Stock' AND is_active = 1
  );

UPDATE buyersystem.api_integration_configuration
SET name = N'FIVE S4 - Plant Transfer Stock',
    process_type = N'POST_TRANSFER_301',
    status = N'ACTIVE',
    is_active = 1,
    date_updated = @Now,
    updated_by = @Actor
WHERE buyer_id = @Buyer
  AND name = N'S4 - Plant Transfer Stock'
  AND NOT EXISTS (
      SELECT 1 FROM buyersystem.api_integration_configuration
      WHERE buyer_id = @Buyer AND name = N'FIVE S4 - Plant Transfer Stock' AND is_active = 1
  );

-- Ensure same-plant Transfer Stock row exists and is ACTIVE.
IF NOT EXISTS (
    SELECT 1 FROM buyersystem.api_integration_configuration
    WHERE buyer_id = @Buyer AND name = N'FIVE S4 - Transfer Stock' AND is_active = 1
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
        @TransferId, @Buyer, N'ALL', N'FIVE S4 - Transfer Stock', N'POST_TRANSFER_311', N'REST', N'SAP S/4',
        @BaseUrl, @Resource, N'BASIC', @S4User, @S4Password,
        N'POST', N'JSON', NULL, @Headers,
        30, 2, 100, 0, N'ACTIVE',
        @Now, @Now, @Actor, @Actor, 1
    );
END
ELSE
BEGIN
    UPDATE buyersystem.api_integration_configuration
    SET process_type = N'POST_TRANSFER_311',
        status = N'ACTIVE',
        is_active = 1,
        protocol = N'REST',
        system_name = N'SAP S/4',
        base_url = @BaseUrl,
        resource_path = @Resource,
        authentication_type = N'BASIC',
        username = @S4User,
        protected_password = @S4Password,
        http_method = N'POST',
        payload_format = N'JSON',
        headers_json = @Headers,
        entity_code = N'ALL',
        date_updated = @Now,
        updated_by = @Actor
    WHERE buyer_id = @Buyer
      AND name = N'FIVE S4 - Transfer Stock'
      AND is_active = 1;
END;

-- Ensure plant-to-plant Transfer Stock row exists and is ACTIVE.
IF NOT EXISTS (
    SELECT 1 FROM buyersystem.api_integration_configuration
    WHERE buyer_id = @Buyer AND name = N'FIVE S4 - Plant Transfer Stock' AND is_active = 1
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
        @PlantTransferId, @Buyer, N'ALL', N'FIVE S4 - Plant Transfer Stock', N'POST_TRANSFER_301', N'REST', N'SAP S/4',
        @BaseUrl, @Resource, N'BASIC', @S4User, @S4Password,
        N'POST', N'JSON', NULL, @Headers,
        30, 2, 100, 0, N'ACTIVE',
        @Now, @Now, @Actor, @Actor, 1
    );
END
ELSE
BEGIN
    UPDATE buyersystem.api_integration_configuration
    SET process_type = N'POST_TRANSFER_301',
        status = N'ACTIVE',
        is_active = 1,
        protocol = N'REST',
        system_name = N'SAP S/4',
        base_url = @BaseUrl,
        resource_path = @Resource,
        authentication_type = N'BASIC',
        username = @S4User,
        protected_password = @S4Password,
        http_method = N'POST',
        payload_format = N'JSON',
        headers_json = @Headers,
        entity_code = N'ALL',
        date_updated = @Now,
        updated_by = @Actor
    WHERE buyer_id = @Buyer
      AND name = N'FIVE S4 - Plant Transfer Stock'
      AND is_active = 1;
END;

-- Durable Material Document sample bodies (runtime still builds the live ITO payload).
DECLARE @TransferBody nvarchar(max) = N'{
  "DocumentDate": "{{documentDate}}",
  "PostingDate": "{{postingDate}}",
  "GoodsMovementCode": "04",
  "MaterialDocumentHeaderText": "{{headerText}}",
  "ReferenceDocument": "{{referenceDocument}}",
  "to_MaterialDocumentItem": [
    {
      "Material": "{{material}}",
      "Plant": "{{plant}}",
      "StorageLocation": "{{storageLocation}}",
      "GoodsMovementType": "311",
      "IssuingOrReceivingStorageLoc": "{{receivingStorageLocation}}",
      "QuantityInEntryUnit": "{{quantity}}",
      "EntryUnit": "{{entryUnit}}"
    }
  ]
}';

DECLARE @PlantBody nvarchar(max) = N'{
  "DocumentDate": "{{documentDate}}",
  "PostingDate": "{{postingDate}}",
  "GoodsMovementCode": "04",
  "MaterialDocumentHeaderText": "{{headerText}}",
  "ReferenceDocument": "{{referenceDocument}}",
  "to_MaterialDocumentItem": [
    {
      "Material": "{{material}}",
      "Plant": "{{plant}}",
      "StorageLocation": "{{storageLocation}}",
      "GoodsMovementType": "301",
      "IssuingOrReceivingPlant": "{{receivingPlant}}",
      "IssuingOrReceivingStorageLoc": "{{receivingStorageLocation}}",
      "QuantityInEntryUnit": "{{quantity}}",
      "EntryUnit": "{{entryUnit}}"
    }
  ]
}';

UPDATE buyersystem.api_integration_configuration
SET request_body = @TransferBody,
    date_updated = @Now,
    updated_by = @Actor
WHERE buyer_id = @Buyer
  AND name = N'FIVE S4 - Transfer Stock'
  AND is_active = 1;

UPDATE buyersystem.api_integration_configuration
SET request_body = @PlantBody,
    date_updated = @Now,
    updated_by = @Actor
WHERE buyer_id = @Buyer
  AND name = N'FIVE S4 - Plant Transfer Stock'
  AND is_active = 1;

-- Deactivate any leftover S4-named duplicates so Integration Setup shows one active FIVE row.
UPDATE legacy
SET status = N'INACTIVE',
    is_active = 0,
    date_updated = @Now,
    updated_by = @Actor
FROM buyersystem.api_integration_configuration AS legacy
WHERE legacy.buyer_id = @Buyer
  AND legacy.name IN (N'S4 - Transfer Stock', N'S4 - Plant Transfer Stock')
  AND EXISTS (
      SELECT 1 FROM buyersystem.api_integration_configuration AS active_five
      WHERE active_five.buyer_id = @Buyer
        AND active_five.is_active = 1
        AND active_five.status = N'ACTIVE'
        AND (
              (legacy.name = N'S4 - Transfer Stock' AND active_five.name = N'FIVE S4 - Transfer Stock')
           OR (legacy.name = N'S4 - Plant Transfer Stock' AND active_five.name = N'FIVE S4 - Plant Transfer Stock')
        )
  );
