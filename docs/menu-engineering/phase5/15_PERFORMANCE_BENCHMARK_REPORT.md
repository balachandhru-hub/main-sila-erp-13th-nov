# 15. Performance Benchmark Report

**Test Reference:** `SilaMenuEngineeringPhase5CertificationTests.Gate8_PerformanceAndConcurrency_MeasuresLatenciesUnderParallelLoad`  
**Environment:** In-process EF Core InMemory test host on Cloud VM (Linux 6.12.113+, .NET 8.0.34) using Golden Business Dataset (multi-property, nested sub-recipes, 21-day POS history, inventory batches, reservations, open POs).

## 1. Measured Latencies
| Operation / Composite Workflow | Measured Metric | Value | Notes |
|---|---|---|---|
| Composite (`GetWorkspaceAsync` + `GetPurchasePlanAsync` + `RunSimulationAsync`) — 10 sequential iterations | **Median Latency** | **61.0 ms** | Warm cache / JIT steady-state |
| Composite (`GetWorkspaceAsync` + `GetPurchasePlanAsync` + `RunSimulationAsync`) — 10 sequential iterations | **P95 Latency** | **3,961.0 ms** | First-call cold JIT + Python engine connection timeout fallback (`offline` client) |
| Single Simulation (`RunSimulationAsync`) under 12-way parallel burst | **Batch Wall Time (12 concurrent)** | **483 ms** | 12/12 succeeded (`ParallelSuccess = 12/12`) |
| Python Culinary Engine Suite (`88` endpoint tests) | **Total Suite Time** | **7.25 s** | ~82 ms/test avg |
| Python Voice Engine Suite (`10` intent tests) | **Total Suite Time** | **0.35 s** | ~35 ms/test avg |

## 2. Workload Assumptions & Caveat
- Measured values reflect deterministic VM/InMemory execution without live network latency to external SQL Server under 250-user load or live cloud LLM latency. Production SQL Server load testing against anonymized customer database volume should be performed during Controlled Pilot.
