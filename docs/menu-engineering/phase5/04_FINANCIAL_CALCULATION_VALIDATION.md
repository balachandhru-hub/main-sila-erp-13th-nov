# 04. Financial Calculation Validation Report

**Test Suite:** `SilaMenuEngineeringPhase5CertificationTests.Gate2_GoldenCalculations_ExamplesA_B_C_AndEdgeCases_MatchExactExpectedValues`, `SilaCulinaryFinancialServiceTests`, `SilaMenuEngineeringStudioAcceptanceTests`  
**Result:** PASS (All golden financial & quantity assertions exact to decimal precision).

## 1. Golden Calculation Results (Measured Output)

### Example A — A La Carte / Event Pricing & Margin
- **Inputs:** Net selling price = `AED 150.00`, Food cost per guest = `AED 42.00` (via approved recipe `RCP-GOLD-SALMON`: `0.50 kg * AED 60 + 0.60 kg sub-recipe * AED 20 = AED 42.00`), Expected guests = `500`
- **Measured Output:**
  - Revenue = `AED 75,000.00` (Expected: `75,000`) — **EXACT MATCH**
  - Food Cost = `AED 21,000.00` (Expected: `21,000`) — **EXACT MATCH**
  - Food Cost % = `28.00%` (Expected: `28%`) — **EXACT MATCH**
  - Contribution Margin (Revenue less Food Cost) = `AED 54,000.00` (Expected: `54,000`) — **EXACT MATCH**
  - Non-Net-Profit Disclosure: `CostMarginAnalysisDto.Disclaimer` explicitly states contribution margin is **not Net Profit** and estimated costs are distinguished from actuals.

### Example B — Buffet Portion & Yield Explosion
- **Inputs:** Expected covers = `500`, Participation rate = `60%`, Servings per participant = `1.2`, Planned portion size = `150 g` (`0.150 kg`), Approved yield = `80%` (`10%` trim + `10%` cooking loss)
- **Measured Output:**
  - Expected Portions = `360.00` (Expected: `360`) — **EXACT MATCH**
  - Usable Output = `54.0000 kg` (Expected: `54 kg`) — **EXACT MATCH**
  - Gross Requirement (yield applied exactly once) = `67.5000 kg` (Expected: `67.5 kg`) — **EXACT MATCH**

### Example C — Time-Phased Inventory, Shortage & Pack Rounding
- **Inputs:** Gross requirement = `100.00 kg`, Main Kitchen On-Hand = `45 kg`, Active Reservation = `5 kg`, Expiring Today = `5 kg`, Quarantine Blocked Stock = `10 kg`, On-Time Approved PO = `25 kg`, Late PO (+5d) = `40 kg`, Purchase Pack Size = `12 kg` (`CASE12 -> KG`)
- **Measured Output:**
  - Eligible Available Stock = `35.00 kg` (`45 - 5 reserved - 5 expiring`, excluding `10` blocked) — **EXACT MATCH**
  - Eligible Incoming Supply = `25.00 kg` (`40 kg` late PO excluded) — **EXACT MATCH**
  - Net Shortage = `40.00 kg` (`100 - 35 - 25`) — **EXACT MATCH**
  - Recommended Purchase Quantity = `48.00 kg` (`ceil(40 / 12) * 12`) — **EXACT MATCH**

## 2. Edge Cases Verified
- Zero selling price (`AED 0.00`): `FoodCostPercent = null` (no divide-by-zero exception).
- Unlinked recipe (`LinkedRecipeId = null`): `RecipeLinkStatus = "MENU_ITEM_ONLY"`.
- Circular recipe detection: `SilaCulinaryMrpServiceTests.CircularDependency_BlocksCalculation_WithCyclePath` — **PASS**.
- UOM mass/volume & material-specific conversion: `UomConverterTests` (17/17) — **PASS**.
- Multi-currency FX conversion & missing ingredient cost handling: `SilaCulinaryFinancialServiceTests` — **PASS**.
