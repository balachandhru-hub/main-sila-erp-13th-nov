using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryKitchen : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        [MaxLength(160)]
        public string? PropertyName { get; set; }

        [Required]
        [MaxLength(60)]
        public string KitchenCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(160)]
        public string KitchenName { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string KitchenType { get; set; } = "MAIN_KITCHEN"; // MAIN_KITCHEN, CENTRAL_KITCHEN, BANQUET_KITCHEN, PASTRY_KITCHEN, BAKERY, COLD_KITCHEN, HOT_KITCHEN, BUTCHERY, BREAKFAST_KITCHEN, SPECIALTY_KITCHEN, CLOUD_KITCHEN, STAFF_KITCHEN, SATELLITE_KITCHEN

        public Guid? AssociatedStoreLocationId { get; set; }

        [MaxLength(160)]
        public string? ResponsibleExecutiveChef { get; set; }

        [MaxLength(160)]
        public string? KitchenSupervisor { get; set; }

        [Precision(18, 2)]
        public decimal DailyPortionCapacity { get; set; } = 1000m;

        public string OutletAssociationsJson { get; set; } = "[]";

        public string SupportedMealPeriodsJson { get; set; } = "[\"BREAKFAST\",\"LUNCH\",\"DINNER\"]";

        public string SupportedCategoriesJson { get; set; } = "[]";

        public string ShiftConfigJson { get; set; } = "[{\"shiftCode\":\"AM\",\"startMinute\":360,\"endMinute\":840},{\"shiftCode\":\"PM\",\"startMinute\":840,\"endMinute\":1320}]";

        public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;

        public DateTime? EffectiveToUtc { get; set; }
    }

    public class SilaCulinaryWorkstation : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid KitchenId { get; set; }

        [Required]
        [MaxLength(60)]
        public string WorkstationCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(160)]
        public string WorkstationName { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string WorkstationType { get; set; } = "COOKING"; // PREPARATION, VEGETABLE_PREP, MEAT_PREP, SEAFOOD_PREP, COOKING, GRILL, FRYING, BAKING, PASTRY, COLD_PREP, PLATING, PACKING, DISPATCH

        public int ConcurrentCapacity { get; set; } = 2;
    }

    public class SilaCulinaryEquipment : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid KitchenId { get; set; }

        public Guid? WorkstationId { get; set; }

        [Required]
        [MaxLength(60)]
        public string EquipmentCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(160)]
        public string EquipmentName { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string EquipmentType { get; set; } = "COMBI_OVEN";

        [Precision(18, 2)]
        public decimal BatchCapacity { get; set; } = 50m;

        [MaxLength(20)]
        public string CapacityUom { get; set; } = "PORTION";

        public int SetupMinutes { get; set; } = 10;

        public int CleaningMinutes { get; set; } = 15;

        public int AvailableFromMinute { get; set; } = 360;

        public int AvailableUntilMinute { get; set; } = 1320;

        public bool UnderMaintenance { get; set; }

        [MaxLength(80)]
        public string? TemperatureCapability { get; set; }

        public bool AllergenSegregated { get; set; }
    }

    public class SilaCulinaryStaffAssignment : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        public Guid UserId { get; set; }

        [Required]
        [MaxLength(160)]
        public string StaffName { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string CulinaryRole { get; set; } = "CHEF_DE_PARTIE";

        public Guid KitchenId { get; set; }

        public Guid? WorkstationId { get; set; }

        [MaxLength(40)]
        public string ShiftCode { get; set; } = "AM";

        [MaxLength(60)]
        public string SkillClassification { get; set; } = "HOT_PRODUCTION";

        [MaxLength(160)]
        public string? SupervisorName { get; set; }

        public string AuthorizedActivitiesJson { get; set; } = "[\"PREP\",\"COOK\",\"QUALITY_CHECK\"]";

        public int AvailableFromMinute { get; set; } = 360;

        public int AvailableUntilMinute { get; set; } = 840;

        public bool IsTemporary { get; set; }

        public bool IsCrossKitchen { get; set; }

        public bool IsAvailable { get; set; } = true;
    }

    public class SilaCulinaryProductionDemand : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        public Guid? KitchenId { get; set; }

        public Guid? OutletId { get; set; }

        public Guid? EventId { get; set; }

        [Required]
        [MaxLength(120)]
        public string SourceDocumentKey { get; set; } = string.Empty; // Prevents duplicate demand conversion

        [Required]
        [MaxLength(40)]
        public string DemandSource { get; set; } = "EVENT_BOOKING"; // PMS_OCCUPANCY, OPERA, POS, MENU_PLAN, BUFFET_PLAN, EVENT_BOOKING, BANQUET_ORDER, OUTLET_REQUEST, CENTRAL_KITCHEN_REQUEST, MANUAL

        [Required]
        [MaxLength(40)]
        public string DemandNature { get; set; } = "CONFIRMED"; // FORECAST | CONFIRMED | MANUAL | REVISED | EMERGENCY

        public Guid? RecipeId { get; set; }

        [Required]
        [MaxLength(180)]
        public string MenuItemName { get; set; } = string.Empty;

        [MaxLength(40)]
        public string MealPeriod { get; set; } = "DINNER";

        public DateTime ProductionDateUtc { get; set; }

        public DateTime RequiredCompletionUtc { get; set; }

        [Precision(18, 2)]
        public decimal ForecastPortions { get; set; }

        [Precision(18, 2)]
        public decimal AdjustedPortions { get; set; }

        public int Priority { get; set; } = 50;

        [MaxLength(40)]
        public string ConversionStatus { get; set; } = "OPEN"; // OPEN | CONVERTED | SUPERSEDED

        public Guid? ProductionPlanId { get; set; }
    }

    public class SilaCulinaryProductionPlan : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [MaxLength(60)]
        public string PlanCode { get; set; } = string.Empty;

        public Guid? PropertyId { get; set; }

        [MaxLength(160)]
        public string? PropertyName { get; set; }

        public DateTime ProductionDateUtc { get; set; }

        [MaxLength(40)]
        public string PlanningPeriod { get; set; } = "DAILY";

        [MaxLength(40)]
        public string MealPeriod { get; set; } = "ALL_DAY";

        public Guid? EventId { get; set; }

        [MaxLength(120)]
        public string? EventReference { get; set; }

        [MaxLength(160)]
        public string ResponsibleChef { get; set; } = "Executive Chef";

        [MaxLength(40)]
        public string SourceDemandType { get; set; } = "MIXED";

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "DRAFT"; // DRAFT, DEMAND_CALCULATED, INGREDIENTS_VALIDATED, SHORTAGE_IDENTIFIED, PENDING_APPROVAL, APPROVED, RELEASED, IN_PROGRESS, PARTIALLY_COMPLETED, PRODUCTION_COMPLETED, CANCELLED

        public int VersionNumber { get; set; } = 1;

        [MaxLength(160)]
        public string? ApprovedByName { get; set; }

        public DateTime? ApprovedAtUtc { get; set; }

        [Precision(18, 2)]
        public decimal TotalEstimatedCost { get; set; }
    }

    public class SilaCulinaryProductionPlanLine : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ProductionPlanId { get; set; }

        public Guid BuyerId { get; set; }

        public Guid? RecipeId { get; set; }

        [MaxLength(60)]
        public string RecipeCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(180)]
        public string MenuItemName { get; set; } = string.Empty;

        public int RecipeVersion { get; set; } = 1;

        [Precision(18, 2)]
        public decimal RequiredPortions { get; set; }

        [Precision(18, 2)]
        public decimal PlannedProductionQty { get; set; }

        [MaxLength(20)]
        public string ProductionUom { get; set; } = "PORTION";

        public Guid? KitchenId { get; set; }

        public Guid? WorkstationId { get; set; }

        public DateTime PlannedStartUtc { get; set; }

        public DateTime RequiredCompletionUtc { get; set; }

        public int Priority { get; set; } = 50;

        [MaxLength(40)]
        public string IngredientReadiness { get; set; } = "DATA_UNAVAILABLE"; // AVAILABLE, PARTIALLY_AVAILABLE, EXPECTED_BEFORE_PRODUCTION, SHORTAGE, QUALITY_BLOCKED, EXPIRED, DATA_UNAVAILABLE

        [MaxLength(40)]
        public string EquipmentReadiness { get; set; } = "READY";

        [MaxLength(40)]
        public string StaffReadiness { get; set; } = "READY";

        [Precision(18, 2)]
        public decimal EstimatedFoodCost { get; set; }

        [MaxLength(40)]
        public string LineStatus { get; set; } = "PLANNED";
    }

    public class SilaCulinaryProductionPlanVersion : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ProductionPlanId { get; set; }

        public Guid BuyerId { get; set; }

        public int VersionNumber { get; set; }

        [MaxLength(300)]
        public string ChangeSummary { get; set; } = string.Empty;

        public string SnapshotJson { get; set; } = "{}";
    }

    public class SilaCulinaryProductionOrder : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [MaxLength(60)]
        public string OrderNumber { get; set; } = string.Empty;

        public Guid? ProductionPlanId { get; set; }

        public Guid? PropertyId { get; set; }

        public Guid? KitchenId { get; set; }

        public Guid? OutletId { get; set; }

        public Guid? EventId { get; set; }

        public Guid? RecipeId { get; set; }

        public int RecipeVersion { get; set; } = 1;

        [Required]
        [MaxLength(180)]
        public string RecipeName { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string OrderType { get; set; } = "INDIVIDUAL"; // INDIVIDUAL, CONSOLIDATED, EVENT, BUFFET, CENTRAL_KITCHEN, REPLENISHMENT, EMERGENCY

        [Precision(18, 2)]
        public decimal PlannedOutputQty { get; set; }

        [Precision(18, 2)]
        public decimal ActualOutputQty { get; set; }

        [MaxLength(20)]
        public string OutputUom { get; set; } = "PORTION";

        public DateTime ScheduledStartUtc { get; set; }

        public DateTime RequiredCompletionUtc { get; set; }

        [MaxLength(160)]
        public string ResponsibleChef { get; set; } = "Chef de Partie";

        public int Priority { get; set; } = 50;

        [Precision(18, 2)]
        public decimal EstimatedProductionCost { get; set; }

        [MaxLength(40)]
        public string ApprovalStatus { get; set; } = "APPROVED";

        [Required]
        [MaxLength(40)]
        public string ExecutionStatus { get; set; } = "CREATED"; // CREATED, PENDING_APPROVAL, RELEASED, INGREDIENTS_RESERVED, PICKING_IN_PROGRESS, INGREDIENTS_READY, PREPARATION_IN_PROGRESS, COOKING_IN_PROGRESS, QUALITY_CHECK, READY_FOR_DISPATCH, PRODUCTION_COMPLETED, ON_HOLD, CANCELLED

        public string ImmutableRecipeSnapshotJson { get; set; } = "{}";
    }

    public class SilaCulinaryProductionOrderIngredient : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ProductionOrderId { get; set; }

        public Guid BuyerId { get; set; }

        public Guid? MaterialId { get; set; }

        [Required]
        [MaxLength(60)]
        public string MaterialCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal GrossRequiredQty { get; set; }

        [Precision(18, 4)]
        public decimal NetUsableQty { get; set; }

        [Precision(18, 4)]
        public decimal YieldAdjustedQty { get; set; }

        [MaxLength(20)]
        public string InventoryUom { get; set; } = "KG";

        [Precision(18, 4)]
        public decimal AvailableEligibleStock { get; set; }

        [Precision(18, 4)]
        public decimal IncomingBeforeProductionQty { get; set; }

        [Precision(18, 4)]
        public decimal ExistingCommitmentsQty { get; set; }

        [Precision(18, 4)]
        public decimal ShortageQty { get; set; }

        [MaxLength(40)]
        public string ReadinessStatus { get; set; } = "AVAILABLE";

        public string TraceabilitySourceJson { get; set; } = "[]";
    }

    public class SilaCulinaryProductionBatch : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid ProductionOrderId { get; set; }

        [Required]
        [MaxLength(60)]
        public string BatchNumber { get; set; } = string.Empty;

        [MaxLength(40)]
        public string BatchType { get; set; } = "EQUIPMENT_BASED"; // FIXED, VARIABLE, EQUIPMENT_BASED, DEMAND_BASED, REPLENISHMENT

        [Precision(18, 2)]
        public decimal PlannedBatchQty { get; set; }

        [Precision(18, 2)]
        public decimal ActualOutputQty { get; set; }

        [MaxLength(20)]
        public string ProductionUom { get; set; } = "PORTION";

        public Guid? KitchenId { get; set; }

        public Guid? WorkstationId { get; set; }

        public Guid? EquipmentId { get; set; }

        public Guid? AssignedStaffUserId { get; set; }

        [MaxLength(160)]
        public string? AssignedChefName { get; set; }

        public DateTime PlannedStartUtc { get; set; }

        public DateTime PlannedFinishUtc { get; set; }

        public DateTime? ActualStartUtc { get; set; }

        public DateTime? ActualFinishUtc { get; set; }

        public int BatchPriority { get; set; } = 50;

        [MaxLength(40)]
        public string QualityStatus { get; set; } = "PENDING"; // PENDING, PASSED, FAILED_HOLD

        [Required]
        [MaxLength(40)]
        public string BatchStatus { get; set; } = "CREATED"; // CREATED, RELEASED, PREPARING, COOKING, PAUSED, QUALITY_CHECK, READY_FOR_DISPATCH, COMPLETED, ON_HOLD, CANCELLED

        public Guid? ParentBatchId { get; set; }

        public DateTime? UseByUtc { get; set; }

        [MaxLength(200)]
        public string? AllergenSummary { get; set; }

        public string QrPayloadJson { get; set; } = "{}";

        public string RecipeSnapshotJson { get; set; } = "{}";
    }

    public class SilaCulinaryProductionTask : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ProductionBatchId { get; set; }

        public Guid BuyerId { get; set; }

        public int SequenceOrder { get; set; } = 1;

        public Guid? DependsOnTaskId { get; set; }

        [Required]
        [MaxLength(60)]
        public string TaskType { get; set; } = "PREPARATION";

        [Required]
        [MaxLength(200)]
        public string Instructions { get; set; } = string.Empty;

        public Guid? WorkstationId { get; set; }

        public Guid? EquipmentId { get; set; }

        public Guid? AssignedStaffUserId { get; set; }

        public int PlannedDurationMinutes { get; set; } = 20;

        public int? ActualDurationMinutes { get; set; }

        public DateTime? ActualStartUtc { get; set; }

        public DateTime? ActualCompletionUtc { get; set; }

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "PENDING"; // PENDING, IN_PROGRESS, PAUSED, COMPLETED, EXCEPTION

        [MaxLength(300)]
        public string? ExceptionReason { get; set; }
    }

    public class SilaCulinaryProductionPickingLine : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BuyerId { get; set; }

        public Guid ProductionOrderId { get; set; }

        public Guid? ProductionBatchId { get; set; }

        public Guid? StockReservationId { get; set; }

        public Guid? MaterialId { get; set; }

        [Required]
        [MaxLength(60)]
        public string MaterialCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal RequiredQty { get; set; }

        [Precision(18, 4)]
        public decimal ReservedQty { get; set; }

        [Precision(18, 4)]
        public decimal PickedQty { get; set; }

        [MaxLength(20)]
        public string InventoryUom { get; set; } = "KG";

        public Guid? StoreLocationId { get; set; }

        [MaxLength(80)]
        public string? InventoryBatchNumber { get; set; }

        public DateTime? ExpiryUtc { get; set; }

        [MaxLength(20)]
        public string PickingStrategy { get; set; } = "FEFO"; // FEFO | FIFO

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "OPEN"; // OPEN, PARTIAL, PICKED, ISSUED, SHORTAGE
    }

    public class SilaCulinaryProductionMaterialIssueRef : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BuyerId { get; set; }

        public Guid ProductionOrderId { get; set; }

        public Guid? ProductionBatchId { get; set; }

        public Guid? GoodsIssueId { get; set; }

        [Required]
        [MaxLength(60)]
        public string IssueNumber { get; set; } = string.Empty;

        [MaxLength(60)]
        public string MaterialCode { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal IssuedQty { get; set; }

        [MaxLength(20)]
        public string IssuedUom { get; set; } = "KG";

        public Guid? FromStoreLocationId { get; set; }

        public Guid? ToKitchenId { get; set; }

        [Required]
        [MaxLength(120)]
        public string IdempotencyKey { get; set; } = string.Empty;

        public bool IsReversed { get; set; }

        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class SilaCulinaryProductionQualityCheckpoint : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BuyerId { get; set; }

        public Guid? PropertyId { get; set; }

        [Required]
        [MaxLength(60)]
        public string CheckpointCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(160)]
        public string CheckpointName { get; set; } = string.Empty;

        [MaxLength(40)]
        public string Stage { get; set; } = "COOKING"; // INGREDIENT, EXPIRY, ALLERGEN, PREP, COOKING, COOLING, HOLDING, PORTION, OUTPUT, DISPATCH

        [MaxLength(80)]
        public string ExpectedCriteria { get; set; } = ">= 75";

        [MaxLength(20)]
        public string MeasurementUom { get; set; } = "C";

        public bool IsMandatory { get; set; } = true;
    }

    public class SilaCulinaryProductionQualityResult : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BuyerId { get; set; }

        public Guid ProductionBatchId { get; set; }

        public Guid? CheckpointId { get; set; }

        [Required]
        [MaxLength(160)]
        public string CheckpointName { get; set; } = string.Empty;

        [MaxLength(80)]
        public string ExpectedValue { get; set; } = string.Empty;

        [MaxLength(80)]
        public string ActualValue { get; set; } = string.Empty;

        [MaxLength(20)]
        public string MeasurementUom { get; set; } = "C";

        public bool Passed { get; set; }

        [MaxLength(160)]
        public string RecordedByName { get; set; } = string.Empty;

        public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(400)]
        public string? Comments { get; set; }

        [MaxLength(400)]
        public string? CorrectiveAction { get; set; }

        public bool SupervisorApproved { get; set; }
    }

    public class SilaCulinaryProductionException : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BuyerId { get; set; }

        [Required]
        [MaxLength(60)]
        public string ExceptionCode { get; set; } = string.Empty;

        public Guid? ProductionOrderId { get; set; }

        public Guid? ProductionBatchId { get; set; }

        [Required]
        [MaxLength(60)]
        public string ExceptionType { get; set; } = "INGREDIENT_UNAVAILABLE";

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = "HIGH"; // CRITICAL, HIGH, MEDIUM, LOW

        [Required]
        [MaxLength(400)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(160)]
        public string ReportedByName { get; set; } = string.Empty;

        [MaxLength(160)]
        public string? AssignedOwner { get; set; }

        [MaxLength(200)]
        public string? RecommendedAction { get; set; }

        [Required]
        [MaxLength(40)]
        public string ResolutionStatus { get; set; } = "OPEN"; // OPEN, IN_PROGRESS, RESOLVED, ESCALATED

        [MaxLength(400)]
        public string? ResolutionNote { get; set; }
    }

    public class SilaCulinaryProductionDispatch : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BuyerId { get; set; }

        [Required]
        [MaxLength(60)]
        public string DispatchNumber { get; set; } = string.Empty;

        public Guid CentralKitchenId { get; set; }

        public Guid ProductionBatchId { get; set; }

        public Guid DestinationOutletId { get; set; }

        [MaxLength(160)]
        public string DestinationOutletName { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal RequestedQty { get; set; }

        [Precision(18, 2)]
        public decimal AllocatedQty { get; set; }

        [Precision(18, 2)]
        public decimal DispatchedQty { get; set; }

        [Precision(18, 2)]
        public decimal ReceivedQty { get; set; }

        public DateTime RequiredDeliveryUtc { get; set; }

        public Guid? InternalTransferOrderId { get; set; }

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "ALLOCATED"; // REQUESTED, ALLOCATED, PARTIALLY_DISPATCHED, DISPATCHED, PARTIALLY_RECEIVED, RECEIVED
    }

    public class SilaCulinaryProductionAudit : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BuyerId { get; set; }

        public Guid ActorUserId { get; set; }

        [MaxLength(160)]
        public string ActorName { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        public string ActionType { get; set; } = string.Empty;

        [MaxLength(80)]
        public string EntityType { get; set; } = string.Empty;

        public Guid? EntityId { get; set; }

        [MaxLength(600)]
        public string Summary { get; set; } = string.Empty;

        public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
