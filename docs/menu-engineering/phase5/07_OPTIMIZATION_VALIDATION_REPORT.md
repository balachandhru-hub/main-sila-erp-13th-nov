# 07. Optimization Validation Report

**Evaluated Engines:** `MenuEngineeringDigitalTwinService.Optimization.cs` + `Backend/sila-culinary-engine` OR-Tools CP-SAT / LP solver  
**Tests:** `Test01_BuffetOptimization_EnforcesHardFoodCostConstraint`, `Test03_InventoryDrivenOptimization_ExcludesReservedAndBlockedStock`, `Test06_PortionOptimization_RespectsApprovedPortionLimits`, `Test07_InsufficientProductionCapacity_ReportsInfeasibilityExplicitly`, `Test13_UnsafeIngredientSubstitution_IsRejected`, `Test14_ConflictingHardConstraints_NeverSilentlyRelaxed`, `Test18_ConcurrentOptimizationJobs_CompleteQuicklyWithinTenantQuota`, `Gate4`.

## 1. Hard Constraint & Safety Validation
- **Food Cost Cap:** Hard constraint (`<= MaxFoodCostPercent`) enforced; conflicting hard constraints never silently relaxed (`Test14` PASS).
- **Production Capacity:** Kitchen capacity ceiling (`OverrideProductionMinutes` / kitchen daily capacity) enforced; insufficient capacity explicitly reports `IsFeasible = false` / infeasibility diagnostics (`Test07` PASS).
- **Food Safety & Allergens:** Unsafe ingredient substitutions violating allergen exclusions are rejected (`Test13` PASS; `Optimization_And_Substitution_EnforceHardAllergenSafety` PASS).
- **Stock Eligibility:** Reserved, blocked/quarantine, and expired stock excluded from inventory-driven menu optimization (`Test03` PASS).
- **Pareto Frontier Multi-Objective Mode:** Generates feasible non-dominated solutions (`Gate4`: `Status = COMPLETED`, `IsFeasible = true`) without claiming global optimality when heuristic/feasible fallback is used.
- **Tenant Concurrency Quota:** Per-tenant `SemaphoreSlim(3, 3)` prevents optimization storms from starving Procurement V1 or DB connections (`Test18` PASS).
