-- SILA Material plant / storage-location / ERP stock snapshot / integration exception tables.
-- Source implementation only. DO NOT run against live customer databases without explicit authorization.
-- These tables extend Material Master org assignments; they do NOT replace InventoryBalance / InventoryLedger.
-- SAP calls: 0 from this script.

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = N'buyersystem' AND t.name = N'material_plant_assignment')
BEGIN
    CREATE TABLE buyersystem.material_plant_assignment
    (
        id uniqueidentifier NOT NULL PRIMARY KEY,
        buyer_id uniqueidentifier NOT NULL,
        material_id uniqueidentifier NOT NULL,
        plant_code nvarchar(50) NOT NULL,
        plant_status nvarchar(40) NULL,
        purchasing_group nvarchar(40) NULL,
        mrp_type nvarchar(40) NULL,
        procurement_type nvarchar(40) NULL,
        reorder_point decimal(18,4) NULL,
        safety_stock decimal(18,4) NULL,
        min_lot_size decimal(18,4) NULL,
        max_lot_size decimal(18,4) NULL,
        planned_delivery_days int NULL,
        last_synced_on datetime2 NULL,
        source_system nvarchar(40) NULL,
        date_created datetime2 NOT NULL,
        date_updated datetime2 NOT NULL,
        created_by uniqueidentifier NOT NULL,
        updated_by uniqueidentifier NOT NULL,
        is_active bit NOT NULL
    );
    CREATE UNIQUE INDEX ix_material_plant_assignment_buyer_material_plant
        ON buyersystem.material_plant_assignment (buyer_id, material_id, plant_code);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = N'buyersystem' AND t.name = N'material_storage_location_assignment')
BEGIN
    CREATE TABLE buyersystem.material_storage_location_assignment
    (
        id uniqueidentifier NOT NULL PRIMARY KEY,
        buyer_id uniqueidentifier NOT NULL,
        material_id uniqueidentifier NOT NULL,
        plant_code nvarchar(50) NOT NULL,
        storage_location_code nvarchar(50) NOT NULL,
        inventory_location_id uniqueidentifier NULL,
        assignment_status nvarchar(40) NULL,
        last_synced_on datetime2 NULL,
        source_system nvarchar(40) NULL,
        date_created datetime2 NOT NULL,
        date_updated datetime2 NOT NULL,
        created_by uniqueidentifier NOT NULL,
        updated_by uniqueidentifier NOT NULL,
        is_active bit NOT NULL
    );
    CREATE UNIQUE INDEX ix_material_sloc_assignment_buyer_material_plant_sloc
        ON buyersystem.material_storage_location_assignment (buyer_id, material_id, plant_code, storage_location_code);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = N'buyersystem' AND t.name = N'material_erp_stock_snapshot')
BEGIN
    CREATE TABLE buyersystem.material_erp_stock_snapshot
    (
        id uniqueidentifier NOT NULL PRIMARY KEY,
        buyer_id uniqueidentifier NOT NULL,
        material_id uniqueidentifier NOT NULL,
        plant_code nvarchar(50) NOT NULL,
        storage_location_code nvarchar(50) NOT NULL,
        stock_category nvarchar(40) NOT NULL,
        batch nvarchar(50) NULL,
        special_stock nvarchar(40) NULL,
        quantity decimal(18,4) NOT NULL,
        base_uom nvarchar(20) NOT NULL,
        snapshot_on datetime2 NOT NULL,
        source_system nvarchar(40) NULL,
        inventory_location_id uniqueidentifier NULL,
        date_created datetime2 NOT NULL,
        date_updated datetime2 NOT NULL,
        created_by uniqueidentifier NOT NULL,
        updated_by uniqueidentifier NOT NULL,
        is_active bit NOT NULL
    );
    CREATE INDEX ix_material_erp_stock_snapshot_lookup
        ON buyersystem.material_erp_stock_snapshot (buyer_id, material_id, plant_code, storage_location_code, stock_category, batch);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = N'buyersystem' AND t.name = N'material_integration_exception')
BEGIN
    CREATE TABLE buyersystem.material_integration_exception
    (
        id uniqueidentifier NOT NULL PRIMARY KEY,
        buyer_id uniqueidentifier NOT NULL,
        material_id uniqueidentifier NULL,
        material_code nvarchar(40) NULL,
        exception_type nvarchar(60) NOT NULL,
        plant_code nvarchar(50) NULL,
        storage_location_code nvarchar(50) NULL,
        message nvarchar(1000) NOT NULL,
        resolved bit NOT NULL,
        resolved_on datetime2 NULL,
        date_created datetime2 NOT NULL,
        date_updated datetime2 NOT NULL,
        created_by uniqueidentifier NOT NULL,
        updated_by uniqueidentifier NOT NULL,
        is_active bit NOT NULL
    );
    CREATE INDEX ix_material_integration_exception_buyer_resolved_type
        ON buyersystem.material_integration_exception (buyer_id, resolved, exception_type);
END;
