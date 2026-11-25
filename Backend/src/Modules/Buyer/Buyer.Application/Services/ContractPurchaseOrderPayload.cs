using System.Globalization;
using System.Text.Json;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;

namespace Buyer.Application.Services
{
    /// <summary>
    /// The request body of a purchase order created from a contract, in the shape of the SAP S/4 purchase order
    /// API: the header (order type, purchasing organization and group, company code, supplier, date, currency) and
    /// _PurchaseOrderItem with each item's account assignment. A buyer that configured its own body template under
    /// Integrations gets that template, with the tokens filled in ({{supplierId}}, {{currency}} and {{entries}} are other names of {{supplierCode}}, {{documentCurrency}} and {{items}}).
    /// FIVE S4 Create PO (OData V2) uses to_PurchaseOrderItem.results instead.
    /// </summary>
    public static class ContractPurchaseOrderPayload
    {
        public const string DEFAULT_ORDER_TYPE = "NB";
        public const string DEFAULT_ACCOUNT_ASSIGNMENT_CATEGORY = "U";

        /// <summary>The ERP details saved on the order, or an empty set for an order created without any.</summary>
        public static CreateContractPurchaseOrderRequestDto ReadOptions(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new CreateContractPurchaseOrderRequestDto();
            }

            try
            {
                return JsonSerializer.Deserialize<CreateContractPurchaseOrderRequestDto>(json) ?? new CreateContractPurchaseOrderRequestDto();
            }
            catch (JsonException)
            {
                return new CreateContractPurchaseOrderRequestDto();
            }
        }

        public static string Build(
            PurchaseOrder order,
            IReadOnlyList<PurchaseOrderItem> items,
            CreateContractPurchaseOrderRequestDto options,
            string? template,
            string? contractNumber,
            bool odataV2 = false)
        {
            string orderType = Clear(options.PurchaseOrderType) ?? DEFAULT_ORDER_TYPE;
            // Not set (null): the default U, as the orders of a contract have always had. Set to an empty text: no account
            // assignment category is sent, which is how a normal stock item is ordered.
            string? category = options.AccountAssignmentCategory == null ? DEFAULT_ACCOUNT_ASSIGNMENT_CATEGORY : Clear(options.AccountAssignmentCategory);
            string date = (options.PurchaseOrderDate ?? order.OrderDate).ToString("yyyy-MM-dd'T'00:00:00", CultureInfo.InvariantCulture);
            bool useODataV2 = odataV2
                || (!string.IsNullOrWhiteSpace(template)
                    && template.Contains("to_PurchaseOrderItem", StringComparison.OrdinalIgnoreCase));

            List<Dictionary<string, object>> orderItems = new List<Dictionary<string, object>>();
            for (int index = 0; index < items.Count; index++)
            {
                orderItems.Add(Item(items[index], (index + 1) * 10, category, options, order.Currency, useODataV2));
            }

            if (!string.IsNullOrWhiteSpace(template) && template.Contains("{{", StringComparison.Ordinal))
            {
                return template
                    .Replace("{{purchaseOrderNumber}}", order.PoNumber)
                    .Replace("{{purchaseOrderId}}", order.Id.ToString())
                    .Replace("{{contractId}}", order.ContractId?.ToString() ?? string.Empty)
                    .Replace("{{contractNumber}}", contractNumber ?? string.Empty)
                    .Replace("{{purchaseOrderType}}", orderType)
                    .Replace("{{purchasingOrganization}}", options.PurchasingOrganization ?? string.Empty)
                    .Replace("{{purchasingGroup}}", options.PurchasingGroup ?? string.Empty)
                    .Replace("{{companyCode}}", options.CompanyCode ?? string.Empty)
                    .Replace("{{supplierCode}}", options.SupplierCode ?? string.Empty)
                    .Replace("{{supplierId}}", options.SupplierCode ?? string.Empty)
                    .Replace("{{purchaseOrderDate}}", date)
                    .Replace("{{documentCurrency}}", order.Currency ?? string.Empty)
                    .Replace("{{currency}}", order.Currency ?? string.Empty)
                    .Replace("{{items}}", JsonSerializer.Serialize(orderItems))
                    .Replace("{{entries}}", JsonSerializer.Serialize(orderItems));
            }

            // FIVE S4 Create PO (OData V2 API_PURCHASEORDER_PROCESS_SRV) expects to_PurchaseOrderItem.results.
            if (useODataV2)
            {
                return JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["PurchaseOrderType"] = orderType,
                    ["PurchasingOrganization"] = options.PurchasingOrganization ?? string.Empty,
                    ["PurchasingGroup"] = options.PurchasingGroup ?? string.Empty,
                    ["CompanyCode"] = options.CompanyCode ?? string.Empty,
                    ["Supplier"] = options.SupplierCode ?? string.Empty,
                    ["PurchaseOrderDate"] = date,
                    ["DocumentCurrency"] = order.Currency ?? string.Empty,
                    ["to_PurchaseOrderItem"] = new Dictionary<string, object>
                    {
                        ["results"] = orderItems
                    }
                });
            }

            return JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["PurchaseOrderType"] = orderType,
                ["PurchasingOrganization"] = options.PurchasingOrganization ?? string.Empty,
                ["PurchasingGroup"] = options.PurchasingGroup ?? string.Empty,
                ["CompanyCode"] = options.CompanyCode ?? string.Empty,
                ["Supplier"] = options.SupplierCode ?? string.Empty,
                ["PurchaseOrderDate"] = date,
                ["DocumentCurrency"] = order.Currency ?? string.Empty,
                ["_PurchaseOrderItem"] = orderItems
            });
        }

        private static Dictionary<string, object> Item(
            PurchaseOrderItem item,
            int number,
            string? category,
            CreateContractPurchaseOrderRequestDto options,
            string? orderCurrency,
            bool odataV2)
        {
            Dictionary<string, object> result = new Dictionary<string, object>
            {
                ["PurchaseOrderItem"] = number.ToString(CultureInfo.InvariantCulture),
                ["PurchaseOrderItemText"] = item.ProductName
            };
            Add(result, "AccountAssignmentCategory", category);
            // Text PO: omit Material so S/4 uses item text + material group.
            Add(result, "Material", item.MaterialCode);
            // OData V2 (FIVE CREATE PO) wants quantity/price as decimal strings ("10.000"); OData V4 wants JSON numbers.
            result["OrderQuantity"] = odataV2 ? DecString(item.Quantity, 3) : Number(item.Quantity);
            string? unit = Clear(item.UnitOfMeasure);
            Add(result, "PurchaseOrderQuantityUnit", unit);
            result["NetPriceAmount"] = odataV2 ? DecString(item.UnitPrice ?? 0, 2) : Number(item.UnitPrice ?? 0);

            // S/4 wants the currency of the item next to its price (NetPriceAmount): "Together with property 'NetPriceAmount' also
            // property 'DocumentCurrency' needs to be provided". The currency of the line, otherwise that of the order.
            Add(result, "DocumentCurrency", Clear(item.Currency) ?? Clear(orderCurrency));

            // The price per quantity (NetPriceQuantity) comes with the unit it is priced in (OrderPriceUnit): "Together with property
            // 'NetPriceQuantity' also property 'OrderPriceUnit' needs to be provided". The price is per one unit of the order unit.
            if (unit != null)
            {
                result["NetPriceQuantity"] = odataV2 ? "1" : 1;
                result["OrderPriceUnit"] = unit;
            }

            Add(result, "Plant", options.Plant);
            Add(result, "StorageLocation", Clear(item.StorageLocation) ?? options.StorageLocation);
            Add(result, "MaterialGroup", item.MaterialGroup);

            string? costCenter = Clear(item.CostCenter);
            string? glAccount = Clear(options.GlAccount);
            if (costCenter != null || glAccount != null)
            {
                Dictionary<string, object> assignment = new Dictionary<string, object>
                {
                    ["Quantity"] = odataV2 ? DecString(item.Quantity, 3) : Number(item.Quantity)
                };
                Add(assignment, "CostCenter", costCenter);
                Add(assignment, "GLAccount", glAccount);
                // Do not send PurchaseOrderQuantityUnit on account assignment — S/4 treats it as read-only there.

                if (odataV2)
                {
                    // API_PURCHASEORDER_PROCESS_SRV A_PurchaseOrderItem → to_AccountAssignment (A_PurOrdAccountAssignment).
                    // to_PurchaseOrderItemAccount is not a valid deep-insert nav on this service.
                    result["to_AccountAssignment"] = new Dictionary<string, object>
                    {
                        ["results"] = new[] { assignment }
                    };
                }
                else
                {
                    result["_PurOrdAccountAssignment"] = new[] { assignment };
                }
            }

            return result;
        }

        // A decimal as a JSON number without the zeros the database adds: 5.0000 is sent as 5, 70.4600 as 70.46.
        private static decimal Number(decimal value)
        {
            return value / 1.0000000000000000000000000000m;
        }

        // OData V2 Edm.Decimal is commonly posted as a quoted decimal string.
        private static string DecString(decimal value, int decimals)
        {
            string format = decimals <= 0 ? "0" : "0." + new string('0', decimals);
            return Number(value).ToString(format, CultureInfo.InvariantCulture);
        }

        // Empty values are left out: the ERP treats an empty string as a value, not as missing.
        private static void Add(Dictionary<string, object> target, string name, string? value)
        {
            string? text = Clear(value);
            if (text != null)
            {
                target[name] = text;
            }
        }

        private static string? Clear(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
