# 20. Customer UAT Test Pack

| Scenario | Scope | Automated Coverage | Customer Sign-Off Status |
|---|---|---|---|
| **Scenario A: A La Carte Restaurant** | Menu item -> Recipe -> Price -> Publish -> POS Import -> Matrix -> Consumption -> Optimization | `Gate10`, `Scenario01`–`07` (**PASS**) | Pending Stakeholder Sign-Off |
| **Scenario B: Hotel Breakfast Buffet** | Occupancy -> Covers -> Buffet Menu -> Portions (60% x 1.2) -> Ingredient Req -> Inventory -> Purchase Rec -> Actual Consumption | `Gate2`, `Gate10`, `Scenario08` (**PASS**) | Pending Stakeholder Sign-Off |
| **Scenario C: Banquet Event** | Event 500 guests -> Target 30% FC -> Scenarios -> Revise 650 guests -> Recalculate -> Approve -> Profit/CM | `Gate10`, `Test04` (Phase 4) (**PASS**) | Pending Stakeholder Sign-Off |
| **Scenario D: Ingredient Price Shock** | Salmon +20% -> Reverse Dependency -> Impacted Menus -> Corrective Scenario -> Financial Impact | `Gate4`, `Test02` (Phase 3) (**PASS**) | Pending Stakeholder Sign-Off |
| **Scenario E: Supplier Delivery Delay** | Delay flagged -> Time-phased availability -> Shortage -> Alternative supplier / substitution | `Test12` (Phase 3), `Gate2` (**PASS**) | Pending Stakeholder Sign-Off |
| **Scenario F: Inventory-Driven Menu** | Expiring stock -> Optimization prefers near-expiry inventory -> Food safety preserved -> Reduced buy | `Test05` (Phase 3), `Test04` (Phase 2) (**PASS**) | Pending Stakeholder Sign-Off |
| **Scenario G: Autonomous AI Operations** | Event -> Multi-agent workflow -> Action plan -> Approval -> Execution -> Realized Benefit (`10972.50`) | `Gate10`, `SilaMenuEngineeringPhase4Tests` (**PASS**) | Pending Stakeholder Sign-Off |
| **Scenario H: Procurement V1 Regression** | Catalog -> PO -> SO -> GRN -> GRN Reversal (102) -> Invoice -> Credit Memo with ME flags OFF | `Gate9`, `SilaCreditMemoSapResponseTests` (**PASS**) | Pending Stakeholder Sign-Off |
