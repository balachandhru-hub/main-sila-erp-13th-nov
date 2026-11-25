-- Ensure SILA Test Buyer has a PURCHASE_ORDER Master Approval Flow
-- with sila.test.buyer.admin as the default (order 1) approver.
-- Idempotent.

DECLARE @actor UNIQUEIDENTIFIER = '5c4474fc-5eac-410b-bff4-c04f51195ff6';
DECLARE @buyer UNIQUEIDENTIFIER = '15ce77a3-df16-4474-a5da-f874551782d8';
DECLARE @adminUser UNIQUEIDENTIFIER = 'dca4f6dd-d630-40f3-b349-33c0cf9edb02'; -- sila.test.buyer.admin
DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @flowCode NVARCHAR(40) = 'SILA-PURCHASE-ORDER';
DECLARE @flowType NVARCHAR(40) = 'PURCHASE_ORDER';

-- Resolve admin by login when the known id is missing/renamed.
IF NOT EXISTS (SELECT 1 FROM IdentitySystemDB.identitysystem.[user] WHERE id = @adminUser AND is_active = 1)
BEGIN
    SELECT TOP 1 @adminUser = id
    FROM IdentitySystemDB.identitysystem.[user]
    WHERE user_name = 'sila.test.buyer.admin' AND is_active = 1;
END

IF @adminUser IS NULL
BEGIN
    RAISERROR('sila.test.buyer.admin user not found.', 16, 1);
    RETURN;
END

INSERT INTO MasterDataDb.masterdata.metadata (id, [key], type, description, date_created, date_updated, created_by, updated_by, is_active)
SELECT NEWID(), 'PURCHASE_ORDER', 'APPROVAL_TYPE', 'Purchase order approval', @now, @now, @actor, @actor, 1
WHERE NOT EXISTS (
    SELECT 1 FROM MasterDataDb.masterdata.metadata m
    WHERE m.type = 'APPROVAL_TYPE' AND m.[key] = 'PURCHASE_ORDER' AND m.is_active = 1);

INSERT INTO BuyerSystemDB.buyersystem.master_approval_flow
    (id, approval_code, approval_name, buyer_id, date_created, date_updated, created_by, updated_by, is_active, currency, total_amount, type, scope_kind)
SELECT NEWID(), @flowCode, 'Purchase order approval', @buyer, @now, @now, @actor, @actor, 1, 'AED', 0, @flowType, 'ALL'
WHERE NOT EXISTS (
    SELECT 1 FROM BuyerSystemDB.buyersystem.master_approval_flow have
    WHERE have.buyer_id = @buyer
      AND have.is_active = 1
      AND (have.approval_code = @flowCode OR UPPER(have.type) = @flowType));

-- Prefer the dedicated SILA-PURCHASE-ORDER flow; fall back to any active PURCHASE_ORDER flow.
DECLARE @flowId UNIQUEIDENTIFIER =
(
    SELECT TOP 1 id
    FROM BuyerSystemDB.buyersystem.master_approval_flow
    WHERE buyer_id = @buyer AND is_active = 1
      AND (approval_code = @flowCode OR UPPER(type) = @flowType)
    ORDER BY CASE WHEN approval_code = @flowCode THEN 0 ELSE 1 END, date_created DESC
);

IF @flowId IS NULL
BEGIN
    RAISERROR('PURCHASE_ORDER approval flow could not be created.', 16, 1);
    RETURN;
END

-- Make sure type/name stay correct on the chosen flow.
UPDATE BuyerSystemDB.buyersystem.master_approval_flow
SET type = @flowType,
    approval_name = COALESCE(NULLIF(approval_name, ''), 'Purchase order approval'),
    scope_kind = COALESCE(NULLIF(scope_kind, ''), 'ALL'),
    date_updated = @now,
    updated_by = @actor
WHERE id = @flowId;

-- Bump other approvers so admin can sit at order 1.
UPDATE m
SET m.[order] = m.[order] + 1,
    m.date_updated = @now,
    m.updated_by = @actor
FROM BuyerSystemDB.buyersystem.approval_flow_user_mapping m
WHERE m.approval_flow_id = @flowId
  AND m.is_active = 1
  AND m.user_id <> @adminUser
  AND NOT EXISTS (
      SELECT 1 FROM BuyerSystemDB.buyersystem.approval_flow_user_mapping have
      WHERE have.approval_flow_id = @flowId AND have.user_id = @adminUser AND have.is_active = 1);

INSERT INTO BuyerSystemDB.buyersystem.approval_flow_user_mapping
    (id, approval_flow_id, user_id, [order], date_created, date_updated, created_by, updated_by, is_active)
SELECT NEWID(), @flowId, @adminUser, 1, @now, @now, @actor, @actor, 1
WHERE NOT EXISTS (
    SELECT 1 FROM BuyerSystemDB.buyersystem.approval_flow_user_mapping have
    WHERE have.approval_flow_id = @flowId AND have.user_id = @adminUser AND have.is_active = 1);

UPDATE BuyerSystemDB.buyersystem.approval_flow_user_mapping
SET [order] = 1,
    date_updated = @now,
    updated_by = @actor
WHERE approval_flow_id = @flowId
  AND user_id = @adminUser
  AND is_active = 1
  AND [order] <> 1;

SELECT
    f.id AS flow_id,
    f.approval_code,
    f.approval_name,
    f.type,
    f.scope_kind,
    u.user_name AS approver,
    m.[order] AS approver_order
FROM BuyerSystemDB.buyersystem.master_approval_flow f
JOIN BuyerSystemDB.buyersystem.approval_flow_user_mapping m ON m.approval_flow_id = f.id AND m.is_active = 1
JOIN IdentitySystemDB.identitysystem.[user] u ON u.id = m.user_id
WHERE f.id = @flowId
ORDER BY m.[order];
