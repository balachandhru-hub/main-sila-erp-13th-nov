-- SILA ME Positive Stock Adjustment API for SILA Test Buyer.
-- Activates S4 - Positive Stock Adjustment (POST_POSITIVE_ADJ) with Z02 sample body.
-- Reversals post Z02 on this API; consumptions stay Z01 on Goods Issue / waste / POS.
-- Idempotent. Does not touch other Integration Setup rows.

DECLARE @Buyer uniqueidentifier = '15ce77a3-df16-4474-a5da-f874551782d8';
DECLARE @Actor uniqueidentifier = '5c4474fc-5eac-410b-bff4-c04f51195ff6';
DECLARE @PositiveId uniqueidentifier = 'c6aff6ce-2f57-4915-b047-a3a394526464';
DECLARE @Now datetime2 = SYSUTCDATETIME();

DECLARE @S4User nvarchar(250);
DECLARE @S4Password nvarchar(max);
DECLARE @BaseUrl nvarchar(500) = N'https://my419951-api.s4hana.cloud.sap';
DECLARE @Resource nvarchar(500) = N'sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader';
DECLARE @Headers nvarchar(max) = N'{"Accept":"application/json","Content-Type":"application/json","DataServiceVersion":"2.0","MaxDataServiceVersion":"2.0","x-csrf-token":"Fetch"}';
DECLARE @Body nvarchar(max) = N'{
  "DocumentDate": "2026-11-19T00:00:00",
  "PostingDate": "2026-11-19T00:00:00",
  "GoodsMovementCode": "03",
  "MaterialDocumentHeaderText": "SILA Positive Stock Adjustment",
  "ReferenceDocument": "SILA-ADJ-10001",
  "to_MaterialDocumentItem": {
    "results": [
      {
        "Material": "000000000000100001",
        "Plant": "1060",
        "StorageLocation": "0001",
        "GoodsMovementType": "Z02",
        "QuantityInEntryUnit": "5.000",
        "EntryUnit": "KG"
      }
    ]
  }
}';

-- Prefer credentials already on Positive / Goods Issue / Transfer / GRN.
SELECT TOP 1 @S4User = username, @S4Password = protected_password
FROM buyersystem.api_integration_configuration
WHERE buyer_id = @Buyer
  AND is_active = 1
  AND name IN (
      N'S4 - Positive Stock Adjustment', N'FIVE S4 - Positive Stock Adjustment',
      N'S4 - Goods Issue', N'FIVE S4 - Goods Issue',
      N'FIVE S4 - Transfer Stock', N'S4 - Transfer Stock',
      N'FIVE S4-GRN POSTING'
  )
  AND username IS NOT NULL
  AND protected_password IS NOT NULL
ORDER BY CASE
    WHEN name IN (N'S4 - Positive Stock Adjustment', N'FIVE S4 - Positive Stock Adjustment') THEN 0
    WHEN name IN (N'S4 - Goods Issue', N'FIVE S4 - Goods Issue') THEN 1
    ELSE 2
END;

IF @S4User IS NULL
BEGIN
    THROW 50001, 'No S4 credentials found to activate S4 - Positive Stock Adjustment.', 1;
END;

-- Rename FIVE display name to the Integration Setup name reverse/posting resolve.
UPDATE buyersystem.api_integration_configuration
SET name = N'S4 - Positive Stock Adjustment',
    process_type = N'POST_POSITIVE_ADJ',
    status = N'ACTIVE',
    is_active = 1,
    date_updated = @Now,
    updated_by = @Actor
WHERE buyer_id = @Buyer
  AND name = N'FIVE S4 - Positive Stock Adjustment'
  AND NOT EXISTS (
      SELECT 1 FROM buyersystem.api_integration_configuration
      WHERE buyer_id = @Buyer AND name = N'S4 - Positive Stock Adjustment' AND is_active = 1
  );

-- Ensure S4 - Positive Stock Adjustment row exists and is ACTIVE with Z02 sample body.
IF NOT EXISTS (
    SELECT 1 FROM buyersystem.api_integration_configuration
    WHERE buyer_id = @Buyer AND name = N'S4 - Positive Stock Adjustment' AND is_active = 1
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
        @PositiveId, @Buyer, N'ALL', N'S4 - Positive Stock Adjustment', N'POST_POSITIVE_ADJ', N'REST', N'SAP S/4',
        @BaseUrl, @Resource, N'BASIC', @S4User, @S4Password,
        N'POST', N'JSON', @Body, @Headers,
        30, 2, 100, 0, N'ACTIVE',
        @Now, @Now, @Actor, @Actor, 1
    );
END
ELSE
BEGIN
    UPDATE buyersystem.api_integration_configuration
    SET process_type = N'POST_POSITIVE_ADJ',
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
        request_body = @Body,
        headers_json = @Headers,
        entity_code = N'ALL',
        date_updated = @Now,
        updated_by = @Actor
    WHERE buyer_id = @Buyer
      AND name = N'S4 - Positive Stock Adjustment'
      AND is_active = 1;
END;

SELECT id, name, process_type, entity_code, status, is_active,
       CASE WHEN CAST(request_body AS nvarchar(max)) LIKE N'%"GoodsMovementType": "Z02"%' THEN 1 ELSE 0 END AS body_has_z02
FROM buyersystem.api_integration_configuration
WHERE buyer_id = @Buyer
  AND name IN (N'S4 - Positive Stock Adjustment', N'FIVE S4 - Positive Stock Adjustment')
ORDER BY name;
