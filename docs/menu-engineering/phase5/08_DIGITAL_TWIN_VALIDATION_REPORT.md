# 08. Digital Twin Validation Report

**Evaluated Services:** `MenuEngineeringDigitalTwinService.Snapshots.cs`, `Intelligence.cs`, `Optimization.cs`  
**Tests:** `Test02_ReverseDependencyGraph_20PercentPriceIncreaseCascadesAcrossRecipesAndMenus`, `Test08_MonteCarloSimulation_IsSeedReproducible`, `Test09_SensitivityTornado_RanksTopCostAndMarginDrivers`, `Test10_CompareMultipleScenarios_UsesConsistentCalculationBasis`, `Test11_StaleSnapshotDetectionAndRefresh_PreservesPreviousScenarioReproducibility`, `Test12_SupplierDeliveryDelayStressTest_FlagsShortageRiskAndCorrectiveActions`, `Test17_CrossTenantSnapshotAccess_IsDenied`, `Gate4`, `Gate5_Gate6`.

## 1. Twin Coverage & Graph Integrity
- Covers 9 twin domains (Menu, Recipe, Demand, Inventory, Production, Financial, Supplier, Waste, Event) anchored by immutable `MenuEngineeringTwinSnapshot` records (`SourceMaxUpdatedAtUtc`).
- **Reverse Dependency Analysis (`Gate4` & `Test02`):** Shocking `Salmon` (+20%) identified exactly `2` impacted downstream nodes (`Golden Pan-Seared Salmon`, `Grilled Salmon Fillet`) while leaving unrelated `Steamed Saffron Rice` unimpacted.
- **Stale Snapshot Detection (`Test11`):** Updating master recipe/item/inventory timestamps marks older snapshots stale while preserving reproducibility of historical runs.
- **Monte Carlo Seed Reproducibility (`Gate4` & `Test08`):** Running 500 Monte Carlo iterations twice with `Seed = 42` produced identical `ExpectedCost`, `CostP90 = 20587.46`, and `ProbExceedBudget`.
- **Scenario Isolation (`Test18`):** What-If & Twin scenarios never mutate live `MenuPlan`, `Recipe`, or `InventoryBalance` records.
- **BOLA Hardening (Phase ME.5):** `RunMultiObjectiveOptimizationAsync` now explicitly rejects cross-tenant `MenuPlanId` / `BaselineSnapshotId` probes with `NotFoundCustomException`.
