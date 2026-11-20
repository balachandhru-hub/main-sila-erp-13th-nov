import axiosInstance from "./axiosInstance";
import { readError } from "./readError";

/** Which organization configures an API type. */
export type IntegrationSide = "buyer" | "supplier";

/** "full": pull on demand, with a full sync. "check": read and validate only, nothing is stored. "none": no pull. */
export type IntegrationPullMode = "full" | "check" | "none";

export interface IntegrationProcessType {
  value: string;
  label: string;
  side: IntegrationSide;
  /** The API type has a field mapping. */
  hasMapping: boolean;
  pull: IntegrationPullMode;
  /** Push types: when the application calls the API. */
  calledWhen?: string;
}

/** Every API type. The options and the tabs/actions of an integration are derived from this table. */
export const INTEGRATION_PROCESS_TYPES: IntegrationProcessType[] = [
  { value: "POST_PO", label: "Purchase order (create in ERP)", side: "buyer", hasMapping: false, pull: "none", calledWhen: "Called when a weekly bucket is fully approved, and when a purchase order is created from a contract." },
  { value: "GET_STOCK", label: "Material stock (stock in hand)", side: "buyer", hasMapping: true, pull: "check" },
  { value: "GET_MATERIAL", label: "Material master (read from ERP)", side: "buyer", hasMapping: true, pull: "none", calledWhen: "Called from Material Inventory ERP pull for a company code. Mapping uses Material.* fields. Not pulled by the Integration Hub scheduler." },
  { value: "GET_CONTRACT", label: "Contract (read from ERP)", side: "buyer", hasMapping: false, pull: "none" },
  { value: "POST_CONTRACT", label: "Contract (create in ERP)", side: "buyer", hasMapping: false, pull: "none", calledWhen: "Called when a contract is approved, signed by both parties and created. The id the ERP returns is kept as the contract's ERP contract ID." },
  { value: "POST_SUPPLIER", label: "Supplier onboarding (post to ERP)", side: "buyer", hasMapping: false, pull: "none", calledWhen: "Not called by the application yet." },
  { value: "GET_CATALOG", label: "Product catalog", side: "supplier", hasMapping: true, pull: "full" },
  { value: "GET_CATALOG_STOCK", label: "Product stock", side: "supplier", hasMapping: true, pull: "full" },
  { value: "POST_SALES_ORDER", label: "Sales order (receive in ERP)", side: "supplier", hasMapping: false, pull: "none", calledWhen: "Not called by the application yet." },
  { value: "POST_GOODS_MOVEMENT", label: "Inventory goods movement (post in ERP)", side: "buyer", hasMapping: true, pull: "none", calledWhen: "Called every minute for SILA ME transfers, goods issues, adjustments, stock counts and POS consumption waiting for ERP posting. Mapping is optional: it renames the payload fields." },
  { value: "UPDATE_STOCK", label: "Stock count adjustment (UPDATE_STOCK)", side: "buyer", hasMapping: true, pull: "none", calledWhen: "Called for an approved stock count when this API is active. Name the configuration FIVE_POS_UPDATE to use the tested SAP stock update. Shortage posts Z02 and surplus posts Z01, goods movement code 03, quantity always positive. An empty body sends the SAP material-document shape. A request body template is filled instead when one is saved." },
  { value: "POST_GRN", label: "Goods receipt (post in ERP)", side: "buyer", hasMapping: true, pull: "none", calledWhen: "Called every minute for SILA ME goods receipts waiting for ERP posting. Mapping is optional: it renames the payload fields." },
  { value: "POST_GRN_REVERSAL", label: "Goods receipt reversal (post in ERP)", side: "buyer", hasMapping: true, pull: "none", calledWhen: "Called when Reverse GRN is confirmed. The active configuration is named FIVE S4-GRN REVERSAL. Posts movement type 102 for full or partial quantity. Separate from normal GRN posting." },
  { value: "GET_POS_SALE", label: "POS sales (read from ERP)", side: "buyer", hasMapping: true, pull: "check" },
  { value: "POST_INVOICE", label: "Supplier invoice (post in ERP)", side: "buyer", hasMapping: true, pull: "none", calledWhen: "Called every minute for SILA ME invoices once their goods receipt reached the ERP. Mapping is optional: it renames the payload fields (Invoice.*). Configure one per company code to route SAP or Ariba." },
  { value: "SUPPLIER_INVOICE_ATTACHMENT", label: "API_CV_ATTACHMENT_SRV", side: "buyer", hasMapping: true, pull: "none", calledWhen: "Called after the supplier invoice is posted and the SAP invoice number and fiscal year are stored. The active configuration is named API_CV_ATTACHMENT_SRV, system SAP S/4, operation CreateUrlAsAttachment, business object BUS2081. A failed attachment does not change the invoice posting." },
  { value: "GET_SUPPLIER", label: "Supplier master (read from ERP)", side: "buyer", hasMapping: true, pull: "full" },
  { value: "GET_PO", label: "Purchase orders (read from ERP)", side: "buyer", hasMapping: true, pull: "full" },
  { value: "EXTRACT_INVOICE", label: "Invoice extraction (external OCR)", side: "buyer", hasMapping: true, pull: "none", calledWhen: "Called when an invoice is read and the OCR settings use the external reader. Sends {fileName, contentType, contentBase64}; map the answer to InvoiceExtraction.* fields." },
];

/** Push types send data to the external system (EXTRACT_INVOICE sends the file and reads the answer); every other type reads from it. */
export const isPushProcess = (processType: string): boolean =>
  processType.toUpperCase().startsWith("POST_") || processType.toUpperCase() === "EXTRACT_INVOICE" || processType.toUpperCase() === "UPDATE_STOCK" || processType.toUpperCase() === "SUPPLIER_INVOICE_ATTACHMENT";

/** The API types one side can configure. */
export const integrationProcessTypesFor = (side: IntegrationSide): IntegrationProcessType[] =>
  INTEGRATION_PROCESS_TYPES.filter((type) => type.side === side);

/**
 * Named inventory / stock-movement APIs the application already looks up.
 * Integration Hub lists saved rows only; these definitions restore the catalogue of
 * expected names when a tenant has configured GRN 101/102 and nothing else.
 * Mapping notes come from SilaGrnSap, SilaStockCountSap and SilaInventorySap. No SAP URL is stored here.
 */
export type InventoryIntegrationKind = "erp" | "internal" | "unimplemented";

export interface InventoryIntegrationDefinition {
  id: string;
  title: string;
  kind: InventoryIntegrationKind;
  /** Integration Settings name the poster requires, when lookup is by name. */
  configurationName?: string;
  aliases?: string[];
  processType?: string;
  suggestedSystem?: string;
  mapping: string;
  usedFor: string;
}

export const INVENTORY_INTEGRATION_DEFINITIONS: InventoryIntegrationDefinition[] = [
  {
    id: "grn-101",
    title: "Goods Receipt (SAP 101)",
    kind: "erp",
    configurationName: "FIVE S4-GRN POSTING",
    aliases: ["FIVE S4 - POST GRN"],
    processType: "POST_GRN",
    suggestedSystem: "SAP S/4",
    mapping: "GoodsMovementCode 01, GoodsMovementType 101. Plant and storage from the GRN location.",
    usedFor: "SILA GRN posting. Looked up by this name only.",
  },
  {
    id: "grn-102",
    title: "GRN Reversal (SAP 102)",
    kind: "erp",
    configurationName: "FIVE S4-GRN REVERSAL",
    processType: "POST_GRN_REVERSAL",
    suggestedSystem: "SAP S/4",
    mapping: "GoodsMovementType 102. Full or partial quantity. Separate from POST_GRN.",
    usedFor: "SILA Reverse GRN. Looked up by this name only.",
  },
  {
    id: "pos-update",
    title: "POS Sales Consumption (Z01)",
    kind: "erp",
    configurationName: "FIVE_POS_UPDATE",
    processType: "UPDATE_STOCK",
    suggestedSystem: "FIVE_S4_INVENTORY_MOVEMENTS",
    mapping: "Profile POS_SALES_CONSUMPTION. GoodsMovementCode 03, GoodsMovementType Z01 via FIVE_POS_UPDATE only.",
    usedFor: "POS recipe consumption posts Z01 through FIVE_POS_UPDATE. Never S4 - Goods Issue.",
  },
  {
    id: "transfer-stock",
    title: "Storage Location Transfer (311)",
    kind: "erp",
    configurationName: "FIVE S4 - Transfer Stock",
    processType: "POST_TRANSFER_311",
    suggestedSystem: "FIVE_S4_INVENTORY_MOVEMENTS",
    mapping: "Profile STORAGE_LOCATION_TRANSFER. GoodsMovementCode 04, GoodsMovementType 311.",
    usedFor: "Standard and quick internal transfers in the same plant.",
  },
  {
    id: "plant-transfer",
    title: "Plant Transfer (301)",
    kind: "erp",
    configurationName: "FIVE S4 - Plant Transfer Stock",
    processType: "POST_TRANSFER_301",
    suggestedSystem: "FIVE_S4_INVENTORY_MOVEMENTS",
    mapping: "Profile PLANT_TRANSFER. GoodsMovementCode 04, GoodsMovementType 301.",
    usedFor: "ITO when source and destination plants differ in the same company code.",
  },
  {
    id: "goods-issue",
    title: "Stock Issue / Shortage (Z02)",
    kind: "erp",
    configurationName: "S4 - Goods Issue",
    processType: "POST_STOCK_ISSUE",
    suggestedSystem: "FIVE_S4_INVENTORY_MOVEMENTS",
    mapping: "Profile STOCK_ISSUE. GoodsMovementCode 03, GoodsMovementType Z02.",
    usedFor: "SILA GOODS_ISSUE_OUT, waste/damage/spoilage, and outbound adjustments. POS sales use FIVE_POS_UPDATE (Z01).",
  },
  {
    id: "positive-adjustment",
    title: "Positive Stock Adjustment (Z01)",
    kind: "erp",
    configurationName: "FIVE S4 - Positive Stock Adjustment",
    processType: "POST_POSITIVE_ADJ",
    suggestedSystem: "FIVE_S4_INVENTORY_MOVEMENTS",
    mapping: "Profile POSITIVE_STOCK_ADJUSTMENT. GoodsMovementType Z01. Confirm GoodsMovementCode against SAP customizing.",
    usedFor: "SILA inbound adjustments. POS return candidate pending SAP verification.",
  },
  {
    id: "pos-reversal",
    title: "POS Return / Reversal (Z01)",
    kind: "erp",
    configurationName: "POS_CONSUMPTION_REVERSAL",
    processType: "POST_POS_REVERSAL",
    suggestedSystem: "FIVE_S4_INVENTORY_MOVEMENTS",
    mapping: "Profile POS_CONSUMPTION_REVERSAL. GoodsMovementCode 03, GoodsMovementType Z01.",
    usedFor: "Reverse POS ingredient consumption with Z01. Z03 remains unconfigured.",
  },
  {
    id: "material-document-read",
    title: "SAP Material Document Read",
    kind: "erp",
    configurationName: "FIVE_S4_MATERIAL_DOCUMENT_READ",
    processType: "GET_MATERIAL_DOCUMENT",
    suggestedSystem: "FIVE_S4_INVENTORY_MOVEMENTS",
    mapping: "Profile MATERIAL_DOCUMENT_READ. GET /A_MaterialDocumentHeader.",
    usedFor: "Read-only reconcile of SAP material documents.",
  },
  {
    id: "material-document-cancel",
    title: "SAP Material Document Cancellation",
    kind: "erp",
    configurationName: "FIVE_S4_MATERIAL_DOCUMENT_CANCEL",
    processType: "POST_MATERIAL_DOCUMENT_CANCEL",
    suggestedSystem: "FIVE_S4_INVENTORY_MOVEMENTS",
    mapping: "Profile MATERIAL_DOCUMENT_CANCEL. POST /Cancel with MaterialDocumentYear + MaterialDocument.",
    usedFor: "Document cancel. GRN 102 reversal stays on FIVE S4-GRN REVERSAL.",
  },
  {
    id: "get-stock",
    title: "Inventory balance / material stock",
    kind: "erp",
    processType: "GET_STOCK",
    mapping: "Stock.MaterialCode, Stock.Quantity; optional Stock.Plant, Stock.StorageLocation, Stock.Uom.",
    usedFor: "Read ERP stock in hand. Check-only; does not write the SILA ledger.",
  },
  {
    id: "get-material",
    title: "Material master",
    kind: "erp",
    processType: "GET_MATERIAL",
    mapping: "Material.Code and optional description, UOM, group, price.",
    usedFor: "Material Inventory ERP pull per company code. Not a scheduled Hub pull.",
  },
  {
    id: "opening-stock",
    title: "Opening stock",
    kind: "internal",
    mapping: "SILA ledger OPENING_STOCK (IN).",
    usedFor: "Waste & Adjustments. No separate SAP Integration record.",
  },
  {
    id: "ito",
    title: "Internal Transfer Order",
    kind: "internal",
    mapping: "SILA TRANSFER_OUT then TRANSFER_IN. ERP uses FIVE S4 - Transfer Stock / Plant Transfer Stock.",
    usedFor: "Create → approve → dispatch → receive. APIs under /sila/transfers.",
  },
  {
    id: "quick-ito",
    title: "Quick Internal Transfer",
    kind: "internal",
    mapping: "Same TRANSFER_OUT / TRANSFER_IN ledger as standard ITO. Mode QUICK.",
    usedFor: "Quick transfer policy. QUICK_TRANSFER_OUT / QUICK_TRANSFER_IN were never separate SAP types.",
  },
  {
    id: "recipe-pos",
    title: "Recipe / POS consumption",
    kind: "internal",
    mapping: "SILA RECIPE_CONSUMPTION / GOODS_ISSUE_OUT. ERP via S4 - Goods Issue (Z02) only.",
    usedFor: "POS batch processing explodes recipes. Does not deduct on import unless the approved POS process posts consumption.",
  },
  {
    id: "stock-count",
    title: "Physical stock count",
    kind: "internal",
    mapping: "SILA STOCK_COUNT_ADJUSTMENT. ERP via FIVE_POS_UPDATE (Z01 surplus, Z02 shortage).",
    usedFor: "Stock count approve posts the ledger, then queues ERP posting.",
  },
  {
    id: "waste",
    title: "Waste / damage / spoilage",
    kind: "internal",
    mapping: "SILA WASTE, DAMAGE, BREAKAGE, SPOILAGE, EXPIRED. ERP Z02 through S4 - Goods Issue.",
    usedFor: "Waste & Adjustments. No direct balance write.",
  },
  {
    id: "live-stock",
    title: "Stock availability",
    kind: "internal",
    mapping: "SILA /sila/inventory/live by location (store/outlet), property via location master.",
    usedFor: "Cloud and PWA live inventory. Company/plant/storage are location fields, not tenant selectors.",
  },
  {
    id: "movement-history",
    title: "Stock movement history",
    kind: "internal",
    mapping: "SILA /sila/inventory/transactions.",
    usedFor: "Inventory ledger. Direction OUT / GOODS_ISSUE_OUT conventions unchanged.",
  },
  {
    id: "reconciliation",
    title: "Inventory reconciliation",
    kind: "internal",
    mapping: "SILA /sila/erp-postings/{id}/reconcile.",
    usedFor: "Manual posting reconcile. Local ERP schedulers stay PARKED. Not a live SAP stock sync.",
  },
];

export const matchInventoryConfigurations = (
  definition: InventoryIntegrationDefinition,
  rows: IntegrationConfiguration[],
): IntegrationConfiguration[] => {
  const names = [definition.configurationName, ...(definition.aliases ?? [])]
    .filter((name): name is string => Boolean(name))
    .map((name) => name.trim().toLowerCase());
  if (names.length > 0) {
    return rows.filter((row) => names.includes(row.name.trim().toLowerCase()));
  }
  if (definition.processType) {
    return rows.filter((row) => row.processType === definition.processType);
  }
  return [];
};

/** Select value that opens the fields for a code that is not in the built-in list. */
export const NEW_API_TYPE = "__new__";

export type ApiTypeDirection = "read" | "send";

const CUSTOM_API_TYPE = /^(GET_|POST_)[A-Z][A-Z0-9_]*$/;

/** True when the code is one of the built-in API types. */
export const isKnownProcessType = (processType: string): boolean =>
  INTEGRATION_PROCESS_TYPES.some((type) => type.value === processType);

/**
 * The code stored for an API type the organization adds. GET_ reads from the external system and POST_ sends to it.
 * A built-in code is returned unchanged.
 */
export const customApiTypeCode = (raw: string, direction: ApiTypeDirection): string => {
  const cleaned = raw.trim().toUpperCase().replace(/[^A-Z0-9_]/g, "_").replace(/_+/g, "_").replace(/^_+|_+$/g, "");
  if (!cleaned) return "";
  if (isKnownProcessType(cleaned)) return cleaned;
  const body = cleaned.replace(/^(GET_|POST_)/, "");
  if (!body) return "";
  return `${direction === "send" ? "POST" : "GET"}_${body}`.slice(0, 30);
};

/** A built-in API type, or a custom GET_/POST_ code of at most 30 characters. */
export const isAcceptableProcessType = (processType: string): boolean =>
  isKnownProcessType(processType) || (processType.length <= 30 && CUSTOM_API_TYPE.test(processType) && !isKnownProcessType(processType));

/** The table row of an API type. A custom code is a connection only: it is not pulled into application data. */
export const integrationProcessOf = (processType: string): IntegrationProcessType =>
  INTEGRATION_PROCESS_TYPES.find((type) => type.value === processType) ?? {
    value: processType,
    label: processType,
    side: "buyer",
    hasMapping: false,
    pull: "none",
    calledWhen: isPushProcess(processType)
      ? "A custom API type is not called for a built-in document. Use Test connection to call it."
      : "A custom API type is read by Test connection. It is not pulled into the application.",
  };

export const INTEGRATION_PROTOCOLS: { value: string; label: string }[] = [
  { value: "REST", label: "REST" },
  { value: "ODATA_V2", label: "OData V2" },
  { value: "ODATA_V4", label: "OData V4" },
  { value: "SOAP", label: "SOAP" },
];

/** Keep an already-stored protocol on the form even if it is a historical alias. */
export const protocolOptionsFor = (current?: string | null): { value: string; label: string }[] => {
  const list = [...INTEGRATION_PROTOCOLS];
  const value = (current || "").trim();
  if (value && !list.some((option) => option.value === value)) {
    list.push({ value, label: value });
  }
  return list;
};

export const INTEGRATION_AUTH_TYPES: { value: string; label: string }[] = [
  { value: "NONE", label: "None" },
  { value: "BASIC", label: "Basic authentication" },
  { value: "API_KEY", label: "API key" },
  { value: "BEARER_TOKEN", label: "Bearer token" },
  { value: "OAUTH2_CLIENT_CREDENTIALS", label: "OAuth2 client credentials" },
  { value: "CUSTOM_TOKEN_ENDPOINT", label: "Custom token endpoint" },
];

export type IntegrationHttpMethod = "GET" | "POST" | "PUT" | "PATCH";

export const INTEGRATION_HTTP_METHODS: IntegrationHttpMethod[] = ["GET", "POST", "PUT", "PATCH"];

export type IntegrationPayloadFormat = "JSON" | "SOAP" | "CXML";

export const INTEGRATION_PAYLOAD_FORMATS: { value: IntegrationPayloadFormat; label: string }[] = [
  { value: "JSON", label: "JSON" },
  { value: "SOAP", label: "SOAP" },
  { value: "CXML", label: "cXML" },
];

/** Suggestions for the external system name; any other name can be typed. */
export const INTEGRATION_SYSTEMS: string[] = ["SAP S/4", "Ariba", "FIVE_S4_INVENTORY_MOVEMENTS"];

/** Shared SAP Material Document service defaults for inventory movement profiles. */
export const SAP_INVENTORY_CONNECTION = {
  connectionName: "FIVE_S4_INVENTORY_MOVEMENTS",
  systemType: "SAP S/4HANA Cloud",
  baseUrl: "https://my419951-api.s4hana.cloud.sap",
  serviceRoot: "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV",
  protocol: "ODATA_V2",
  contentType: "application/json",
  accept: "application/json",
} as const;

export type SapMovementDirection = "OUT" | "IN" | "TRANSFER" | "READ" | "CANCEL";
export type SapProfileActivationPolicy = "active_allowed" | "validation_required" | "blocked_unverified" | "unconfigured";

/**
 * SILA Integration Studio SAP posting profiles.
 * Names match the inventory poster lookups (SilaInventorySap / SilaStockCountSap / SilaGrnSap).
 * Profile 4 (POS return) uses Z01 positive adjustment.
 * Z03 is intentionally absent until its SAP purpose is confirmed.
 */
export interface SapMovementProfile {
  id: string;
  profileCode: string;
  title: string;
  configurationName: string;
  aliases?: string[];
  processType: string;
  method: "GET" | "POST";
  resourcePath: string;
  goodsMovementCode?: string;
  goodsMovementType?: string;
  direction: SapMovementDirection;
  purpose: string;
  activationPolicy: SapProfileActivationPolicy;
  statusNote: string;
}

export const SAP_MOVEMENT_PROFILES: SapMovementProfile[] = [
  {
    id: "pos-sales-consumption",
    profileCode: "POS_SALES_CONSUMPTION",
    title: "POS Sales Consumption",
    configurationName: "FIVE_POS_UPDATE",
    processType: "UPDATE_STOCK",
    method: "POST",
    resourcePath: "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader",
    goodsMovementCode: "03",
    goodsMovementType: "Z01",
    direction: "OUT",
    purpose: "POS ingredient consumption after recipe explosion",
    activationPolicy: "active_allowed",
    statusNote: "Uses only FIVE_POS_UPDATE. Never S4 - Goods Issue. GoodsMovementType Z01 / code 03.",
  },
  {
    id: "stock-issue",
    profileCode: "STOCK_ISSUE",
    title: "Stock Issue / Shortage",
    configurationName: "S4 - Goods Issue",
    processType: "POST_STOCK_ISSUE",
    method: "POST",
    resourcePath: "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader",
    goodsMovementCode: "03",
    goodsMovementType: "Z02",
    direction: "OUT",
    purpose: "Inventory shortage / direct consumption / waste (non-POS)",
    activationPolicy: "active_allowed",
    statusNote: "Canonical name S4 - Goods Issue (legacy FIVE S4 - Goods Issue still resolves). POS sales use FIVE_POS_UPDATE (Z01).",
  },
  {
    id: "positive-stock-adjustment",
    profileCode: "POSITIVE_STOCK_ADJUSTMENT",
    title: "Positive Stock Adjustment",
    configurationName: "FIVE S4 - Positive Stock Adjustment",
    processType: "POST_POSITIVE_ADJ",
    method: "POST",
    resourcePath: "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader",
    goodsMovementCode: "03",
    goodsMovementType: "Z01",
    direction: "IN",
    purpose: "Positive inventory adjustment",
    activationPolicy: "validation_required",
    statusNote: "GoodsMovementCode 03 matches the current poster; confirm against SAP customizing before production activation.",
  },
  {
    id: "pos-consumption-reversal",
    profileCode: "POS_CONSUMPTION_REVERSAL",
    title: "POS Return / Reversal",
    configurationName: "POS_CONSUMPTION_REVERSAL",
    processType: "POST_POS_REVERSAL",
    method: "POST",
    resourcePath: "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader",
    goodsMovementCode: "03",
    goodsMovementType: "Z01",
    direction: "IN",
    purpose: "Reverse POS ingredient consumption with Z01 positive adjustment",
    activationPolicy: "active_allowed",
    statusNote: "Uses GoodsMovementType Z01 (same family as positive stock adjustment). Cancel is not used for POS reversal.",
  },
  {
    id: "storage-location-transfer",
    profileCode: "STORAGE_LOCATION_TRANSFER",
    title: "Storage Location Transfer",
    configurationName: "FIVE S4 - Transfer Stock",
    processType: "POST_TRANSFER_311",
    method: "POST",
    resourcePath: "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader",
    goodsMovementCode: "04",
    goodsMovementType: "311",
    direction: "TRANSFER",
    purpose: "Same-plant storage location transfer",
    activationPolicy: "active_allowed",
    statusNote: "Looked up by name in SilaInventorySap.SamePlantTransfer.",
  },
  {
    id: "plant-transfer",
    profileCode: "PLANT_TRANSFER",
    title: "Plant Transfer",
    configurationName: "FIVE S4 - Plant Transfer Stock",
    processType: "POST_TRANSFER_301",
    method: "POST",
    resourcePath: "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader",
    goodsMovementCode: "04",
    goodsMovementType: "301",
    direction: "TRANSFER",
    purpose: "Plant-to-plant stock transfer in the same company",
    activationPolicy: "active_allowed",
    statusNote: "Looked up by name in SilaInventorySap.PlantTransfer.",
  },
  {
    id: "material-document-read",
    profileCode: "MATERIAL_DOCUMENT_READ",
    title: "SAP Material Document Read",
    configurationName: "FIVE_S4_MATERIAL_DOCUMENT_READ",
    processType: "GET_MATERIAL_DOCUMENT",
    method: "GET",
    resourcePath: "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader",
    direction: "READ",
    purpose: "Read / reconcile SAP material documents",
    activationPolicy: "validation_required",
    statusNote: "Safe read-only profile. Custom GET_MATERIAL_DOCUMENT process type.",
  },
  {
    id: "material-document-cancel",
    profileCode: "MATERIAL_DOCUMENT_CANCEL",
    title: "SAP Material Document Cancellation",
    configurationName: "FIVE_S4_MATERIAL_DOCUMENT_CANCEL",
    processType: "POST_MATERIAL_DOCUMENT_CANCEL",
    method: "POST",
    resourcePath: "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/Cancel",
    direction: "CANCEL",
    purpose: "Cancel an original SAP material document (MaterialDocumentYear + MaterialDocument)",
    activationPolicy: "validation_required",
    statusNote: "GRN reversal continues to use movement 102 via FIVE S4-GRN REVERSAL, not Cancel. CancelItem is not wired as a separate SILA poster.",
  },
];

export const matchSapMovementProfile = (
  profile: SapMovementProfile,
  rows: IntegrationConfiguration[],
): IntegrationConfiguration | undefined => {
  const names = [profile.configurationName, ...(profile.aliases ?? [])]
    .map((name) => name.trim().toLowerCase());
  return rows.find((row) => names.includes(row.name.trim().toLowerCase()));
};

/** Header name used for an API key when none is entered. */
export const INTEGRATION_DEFAULT_API_KEY_HEADER = "X-API-KEY";

/** Tokens the POST_PO request body template can contain. */
export const INTEGRATION_PO_BODY_TOKENS: string[] = [
  "{{weeklyBucketId}}", "{{bucketCode}}", "{{companyCode}}", "{{plant}}", "{{supplierId}}", "{{supplierName}}",
  "{{buyerDocumentNumber}}", "{{shipTo}}", "{{orderDate}}", "{{currency}}", "{{deliveryInstruction}}", "{{entries}}",
  "{{purchaseOrderId}}", "{{contractId}}", "{{contractNumber}}",
  "{{purchaseOrderNumber}}", "{{purchaseOrderType}}", "{{purchaseOrderDate}}", "{{purchasingOrganization}}",
  "{{purchasingGroup}}", "{{supplierCode}}", "{{documentCurrency}}", "{{items}}",
];

/** Tokens the POST_CONTRACT request body template can contain. */
export const INTEGRATION_CONTRACT_BODY_TOKENS: string[] = [
  "{{contractId}}", "{{contractNumber}}", "{{contractName}}", "{{rfqNumber}}", "{{supplierId}}", "{{buyerOrganizationId}}",
  "{{startDate}}", "{{endDate}}", "{{amount}}", "{{currency}}",
];

/** Value Fetch marks a CSRF handshake header (SAP: x-csrf-token=Fetch); same rule as the server. */
export const isCsrfSwitchHeader = (value: string): boolean => value.trim().toLowerCase() === "fetch";

/** Extra request headers cannot carry credentials. CSRF Fetch is allowed (matches the server). */
export const isReservedIntegrationHeader = (name: string, value = ""): boolean => {
  if (isCsrfSwitchHeader(value)) return /authorization/i.test(name);
  return /authorization|secret|token|key/i.test(name);
};

export const INTEGRATION_NULL_POLICIES: { value: string; label: string }[] = [
  { value: "IGNORE_NULL", label: "Ignore empty values" },
  { value: "WRITE_NULL", label: "Write empty values" },
  { value: "DEFAULT_VALUE", label: "Use a default value" },
];

export interface IntegrationConfiguration {
  id: string;
  entityCode: string;
  name: string;
  processType: string;
  systemName?: string | null;
  protocol: string;
  baseUrl: string;
  resourcePath?: string | null;
  httpMethod?: IntegrationHttpMethod | null;
  payloadFormat?: IntegrationPayloadFormat | null;
  requestBody?: string | null;
  headers?: Record<string, string> | null;
  authenticationType: string;
  username?: string | null;
  apiKeyHeader?: string | null;
  credentialStatus: string;
  timeoutSeconds: number;
  retryCount: number;
  pageSize?: number | null;
  watermarkField?: string | null;
  lastWatermark?: string | null;
  lastAttemptAt?: string | null;
  lastSuccessfulRunAt?: string | null;
  nextRunAt?: string | null;
  isRunning: boolean;
  lastErrorSafe?: string | null;
  scheduleCron?: string | null;
  status: string;
  testedAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

/** Secrets are write-only: leaving one null keeps the value already stored. */
export interface IntegrationConfigurationWrite {
  name: string;
  entityCode: string;
  processType: string;
  systemName?: string | null;
  protocol: string;
  baseUrl: string;
  resourcePath?: string | null;
  httpMethod: IntegrationHttpMethod;
  payloadFormat: IntegrationPayloadFormat;
  requestBody?: string | null;
  headers?: Record<string, string> | null;
  authenticationType: string;
  username?: string | null;
  apiKeyHeader?: string | null;
  apiKey?: string | null;
  password?: string | null;
  clientId?: string | null;
  clientSecret?: string | null;
  bearerToken?: string | null;
  tokenEndpoint?: string | null;
  tokenScope?: string | null;
  tokenHeaders?: Record<string, string> | null;
  tokenBody?: Record<string, string> | null;
  timeoutSeconds: number;
  retryCount: number;
  pageSize?: number | null;
  watermarkField?: string | null;
  scheduleCron?: string | null;
}

export interface IntegrationTestResult {
  success: boolean;
  message: string;
  httpStatus?: number | null;
  testedAt: string;
}

export interface IntegrationSchemaProperty {
  name: string;
  type: string;
  nullable: boolean;
}

export interface IntegrationSchemaEntity {
  name: string;
  entitySet?: string | null;
  properties: IntegrationSchemaProperty[];
  keys: string[];
}

export interface IntegrationSchema {
  configurationId: string;
  metadataUrl: string;
  discoveredAt: string;
  entities: IntegrationSchemaEntity[];
}

export interface IntegrationTargetField {
  targetField: string;
  area: string;
  dataType: string;
  required: boolean;
  allowedTransformations: string[];
}

export interface IntegrationMappingWrite {
  sourceField: string;
  targetField: string;
  transformation?: string | null;
  nullPolicy: string;
  defaultValue?: string | null;
}

export interface IntegrationMapping extends IntegrationMappingWrite {
  id: string;
  configurationId: string;
  isValidated: boolean;
  updatedAt: string;
}

export interface IntegrationExecution {
  id: string;
  configurationId: string;
  trigger: string;
  status: string;
  startedAt: string;
  completedAt?: string | null;
  recordsRead: number;
  recordsCreated: number;
  recordsUpdated: number;
  recordsFailed: number;
  watermarkBefore?: string | null;
  watermarkAfter?: string | null;
  errorCode?: string | null;
  errorMessageSafe?: string | null;
}

/** Buyer APIs are stored by the Buyer service, supplier APIs by the Supplier service. */
const BASE: Record<IntegrationSide, string> = {
  buyer: "/api/v1/buyer/integrations",
  supplier: "/api/v1/supplier/integrations",
};

const asArray = <T,>(value: unknown): T[] => (Array.isArray(value) ? (value as T[]) : []);

export const getIntegrations = async (side: IntegrationSide): Promise<IntegrationConfiguration[]> => {
  try {
    const response = await axiosInstance.get<IntegrationConfiguration[]>(BASE[side]);
    return asArray<IntegrationConfiguration>(response.data);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not load the integrations."));
  }
};

export const getIntegration = async (side: IntegrationSide, configurationId: string): Promise<IntegrationConfiguration> => {
  try {
    const response = await axiosInstance.get<IntegrationConfiguration>(`${BASE[side]}/${configurationId}`);
    return response.data;
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not load the integration."));
  }
};

export const createIntegration = async (side: IntegrationSide, payload: IntegrationConfigurationWrite): Promise<IntegrationConfiguration> => {
  try {
    const response = await axiosInstance.post<IntegrationConfiguration>(BASE[side], payload);
    return response.data;
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not save the integration."));
  }
};

export const deleteIntegration = async (side: IntegrationSide, configurationId: string): Promise<void> => {
  try {
    await axiosInstance.delete(`${BASE[side]}/${configurationId}`);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not delete the integration."));
  }
};

export const updateIntegration = async (
  side: IntegrationSide,
  configurationId: string,
  payload: IntegrationConfigurationWrite,
): Promise<IntegrationConfiguration> => {
  try {
    const response = await axiosInstance.put<IntegrationConfiguration>(`${BASE[side]}/${configurationId}`, payload);
    return response.data;
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not update the integration."));
  }
};

/** Saves only the request body template and its format. The API keeps its status, so an active API stays active. Buyer APIs only. */
export const updateIntegrationRequestBody = async (
  configurationId: string,
  payloadFormat: IntegrationPayloadFormat,
  requestBody: string | null,
): Promise<IntegrationConfiguration> => {
  try {
    const response = await axiosInstance.put<IntegrationConfiguration>(`${BASE.buyer}/${configurationId}/request-body`, { payloadFormat, requestBody });
    return response.data;
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not save the request body."));
  }
};

export const testIntegration = async (side: IntegrationSide, configurationId: string): Promise<IntegrationTestResult> => {
  try {
    const response = await axiosInstance.post<IntegrationTestResult>(`${BASE[side]}/${configurationId}/test`);
    return response.data;
  } catch (error: unknown) {
    throw new Error(readError(error, "The connection test could not be completed."));
  }
};

/**
 * Copies base URL, auth, protected credentials and OData headers from a working connection onto
 * another API of the same organization. Keeps the target process type, name and request body.
 * Buyer APIs only.
 */
export const alignIntegrationConnection = async (
  configurationId: string,
  sourceConfigurationId: string,
): Promise<IntegrationConfiguration> => {
  try {
    const response = await axiosInstance.post<IntegrationConfiguration>(
      `${BASE.buyer}/${configurationId}/align-connection`,
      { sourceConfigurationId },
    );
    return response.data;
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not copy the connection settings."));
  }
};

export const setIntegrationActive = async (side: IntegrationSide, configurationId: string, active: boolean): Promise<void> => {
  try {
    await axiosInstance.post(`${BASE[side]}/${configurationId}/${active ? "activate" : "deactivate"}`);
  } catch (error: unknown) {
    throw new Error(readError(error, active ? "Could not activate the integration." : "Could not deactivate the integration."));
  }
};

export const pullIntegration = async (side: IntegrationSide, configurationId: string, fullSync: boolean): Promise<IntegrationExecution> => {
  try {
    const response = await axiosInstance.post<IntegrationExecution>(`${BASE[side]}/${configurationId}/pull`, { fullSync });
    return response.data;
  } catch (error: unknown) {
    throw new Error(readError(error, "The pull could not be started."));
  }
};

export const getIntegrationExecutions = async (side: IntegrationSide, configurationId?: string): Promise<IntegrationExecution[]> => {
  try {
    const response = await axiosInstance.get<IntegrationExecution[]>(`${BASE[side]}/executions`, {
      params: configurationId ? { configurationId } : {},
    });
    return asArray<IntegrationExecution>(response.data);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not load the run history."));
  }
};

/** The mapping targets of one API type; empty for a type without field mapping. */
export const getIntegrationTargetFields = async (side: IntegrationSide, processType: string): Promise<IntegrationTargetField[]> => {
  try {
    const response = await axiosInstance.get<IntegrationTargetField[]>(`${BASE[side]}/target-fields`, {
      params: { processType },
    });
    return asArray<IntegrationTargetField>(response.data);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not load the target fields."));
  }
};

const normalizeSchema = (schema: IntegrationSchema): IntegrationSchema => ({
  ...schema,
  entities: asArray<IntegrationSchemaEntity>(schema?.entities).map((entity) => ({
    ...entity,
    properties: asArray<IntegrationSchemaProperty>(entity.properties),
    keys: asArray<string>(entity.keys),
  })),
});

/** The last discovered schema, or null when none has been discovered yet. */
export const getIntegrationSchema = async (side: IntegrationSide, configurationId: string): Promise<IntegrationSchema | null> => {
  try {
    const response = await axiosInstance.get<IntegrationSchema | null>(`${BASE[side]}/${configurationId}/schema`);
    return response.data ? normalizeSchema(response.data) : null;
  } catch (error: unknown) {
    if (axiosStatus(error) === 404) return null;
    throw new Error(readError(error, "Could not load the schema."));
  }
};

export const discoverIntegrationSchema = async (side: IntegrationSide, configurationId: string): Promise<IntegrationSchema> => {
  try {
    const response = await axiosInstance.post<IntegrationSchema>(`${BASE[side]}/${configurationId}/schema`);
    return normalizeSchema(response.data);
  } catch (error: unknown) {
    throw new Error(readError(error, "Schema discovery failed. Test the connection and check the base URL."));
  }
};

export const getIntegrationMappings = async (side: IntegrationSide, configurationId: string): Promise<IntegrationMapping[]> => {
  try {
    const response = await axiosInstance.get<IntegrationMapping[]>(`${BASE[side]}/${configurationId}/mappings`);
    return asArray<IntegrationMapping>(response.data);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not load the field mappings."));
  }
};

/** Replaces every mapping of the configuration; the backend validates them against the target registry. */
export const saveIntegrationMappings = async (
  side: IntegrationSide,
  configurationId: string,
  mappings: IntegrationMappingWrite[],
): Promise<IntegrationMapping[]> => {
  try {
    const response = await axiosInstance.put<IntegrationMapping[]>(`${BASE[side]}/${configurationId}/mappings`, mappings);
    return asArray<IntegrationMapping>(response.data);
  } catch (error: unknown) {
    throw new Error(readError(error, "The field mappings could not be saved."));
  }
};

function axiosStatus(error: unknown): number | undefined {
  if (typeof error !== "object" || error === null) return undefined;
  const response = (error as { response?: { status?: number } }).response;
  return response?.status;
}
