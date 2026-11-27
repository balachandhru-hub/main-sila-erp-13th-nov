# 06. Forecasting Model Evaluation Report

**Evaluated Services:** `MenuEngineeringAiOrchestratorService.GetBacktestedDemandForecastAsync`, `Backend/sila-culinary-engine` forecasting endpoints  
**Tests:** `Gate4_ForecastOptimizationDigitalTwinMonteCarlo_ValidateCorrectnessAndIsolation`, `Test04_PredictDemandUsingValidatedHistoricalSales_WithBacktestMetrics`, `Test04_HierarchicalDemandForecast_LeakageFreeWithBacktestMetrics`, `Test05_ColdStartPrediction_DisclosesUncertaintyAndComparables`, `Test12_HistoricalDataInsufficient_DisclosesLimitationWithoutFabricatedPrediction`.

## 1. Backtesting & Baseline Governance
- Chronological train/test split enforced (no future data leakage into training windows).
- Rolling-origin evaluation compares candidate models against **7-Day Seasonal Naive (`SEASONAL_NAIVE_7D`)**, Moving Average, and Exponential Smoothing baselines using **MAE**, **WAPE**, **Forecast Bias**, and **Prediction Interval Coverage**.
- Baseline Selection Rule: Complex models are NOT selected unless they outperform the validated baseline on out-of-sample rolling error. On the 21-day golden hotel dataset, `SEASONAL_NAIVE_7D` was selected cleanly (`Selected = true`).

## 2. Cold-Start, Insufficient Data & LLM Independence
- When historical POS observations are insufficient, the forecaster explicitly flags `ColdStart = true`, discloses data limitations and comparable proxies, and avoids fabricated precision.
- Forecasting operates **100% independently of external LLM availability**.

## 3. Evidence Summary
- `Gate4`: Selected Model = `SEASONAL_NAIVE_7D`, 7-day horizon generated with interval bounds — **PASS**.
- `Test04` (ME.2 & ME.3), `Test05` (ME.3), `Test12` (ME.4): **PASS**.
- Python `sila-culinary-engine` forecasting & demand test suite: **PASS** (included in 88/88 pytest pass).
