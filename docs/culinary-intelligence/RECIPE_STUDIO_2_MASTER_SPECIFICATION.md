# SILA ME — CULINARY INTELLIGENCE SUITE
## RECIPE STUDIO 2.0 — MASTER PRODUCT SPECIFICATION & PHASE 0 BASELINE

> **Repository:** `balachandhru-hub/main-sila-erp-13th-nov`  
> **Document Status:** Authoritative Culinary Intelligence Development Baseline (Preserved in full at Phase 0)  
> **Scope Notice:** This specification defines the complete long-term functional scope across Phases 0 through 5. In Phase 0, only Phase 0 foundation, UI shell, interactive demonstration simulators, AI Engine Setup, provider abstraction, feature flags, permissions, and documentation are implemented. Future capabilities (Phases 1A–5) remain preserved below without omission.

---

## 1. MASTER OBJECTIVE

Extend the existing SILA Recipe Studio into a comprehensive AI-powered Culinary Intelligence Suite for hotels, restaurants, catering companies, banquet operations and hospitality enterprises.

The suite must support the complete lifecycle:

Recipe Generation
→ Ingredient Identification
→ Material Master Matching
→ Material Creation Requests
→ Recipe Costing
→ Recipe Approval
→ Recipe Publishing
→ Menu Planning
→ Event Planning
→ Guest Planning
→ Recipe Explosion
→ Material Requirement Planning
→ Stock Availability
→ Stock Reservation
→ Stock Transfer Recommendations
→ Procurement Recommendations
→ PO Draft Creation
→ Kitchen Production Planning
→ Event Execution
→ Actual Consumption
→ Wastage and Returns
→ Event P&L
→ Analytics
→ Reporting
→ Event Closure
→ Historical Learning.

Build the following integrated capabilities:

1. Recipe Studio Overview.
2. Existing Recipe Master.
3. Existing Legacy Recipe Upload.
4. AI Recipe Planner.
5. Material Intelligence Engine.
6. Material Creation Requests.
7. Menu Studio.
8. AI Event Planner.
9. Event Material Requirement Planning.
10. Event Stock Reservation.
11. Event Procurement Intelligence.
12. Kitchen Production Planner.
13. Food Cost Management.
14. Event Profitability and P&L Studio.
15. What-if Simulation.
16. Culinary Analytics.
17. Report Studio.
18. Event Execution and Reconciliation.
19. Wastage and Sustainability Intelligence.
20. Predictive Culinary Intelligence.
21. SILA Culinary Voice Agent.
22. Culinary AI Engine Setup.
23. Approvals.
24. History and Transaction Tracker.

All these capabilities must be integrated within the existing Recipe Studio.
Do not create a separate standalone application.
This document defines the complete long-term functional scope.
For the current execution, implement only Phase 0 as defined at the end of this specification.
Preserve this entire specification in the repository as the authoritative Culinary Intelligence development baseline.
Do not omit future requirements merely because they are not implemented during Phase 0.

---

# SECTION A — ARCHITECTURE AND REGRESSION PROTECTION

## 2. ABSOLUTE REGRESSION-PROTECTION GUARDRAIL

THIS IS AN EXTENSION OF THE EXISTING SILA APPLICATION.

DO NOT CHANGE ANY UNRELATED:
- Configuration.
- Code.
- Screen.
- GUI.
- Theme.
- Layout.
- Navigation.
- Business logic.
- Workflow.
- API endpoint.
- API configuration.
- API mapping.
- SAP integration.
- Authentication.
- Authorization.
- Tenant architecture.
- Database architecture.
- Existing working functionality.

Do not redesign the entire SILA Cloud application.
Do not redesign the entire SILA Mobile application.
Do not replace the existing Recipe Studio.
Do not create a parallel Recipe Master.
Do not create a parallel Material Master.
Do not create a parallel Inventory Engine.
Do not create a parallel Procurement Engine.
Do not create a parallel Approval Engine.
Do not create a parallel Transaction Tracker.
Do not create a parallel Voice Engine.
Do not modify existing API credentials, endpoints or working mappings.
Do not perform unrelated refactoring.
Do not upgrade unrelated dependencies.
Do not overwrite pre-existing user changes.

Repository: `balachandhru-hub/main-sila-erp-13th-nov` (baseline branch `main`, extended from active Recipe Studio baseline).

Protected application endpoints:
- SILA Cloud: `https://localhost:6001`
- SILA Mobile: `https://localhost:6001/?next=/mobile`
- Buyer API: `http://127.0.0.1:8004`

Preserve:
- Multi-tenant architecture.
- Database-per-tenant architecture.
- Existing control plane.
- Cloud cookie authentication.
- Mobile bearer authentication.
- Existing RBAC.
- Existing property, outlet and store mappings.
- Existing plant and storage location mappings.
- Existing user purchasing profiles.
- Existing Cost Center and G/L Account configuration.
- Existing inventory ledger.
- Existing SAP connectors.
- Existing PO creation API.
- Existing 311/312 stock transfer functionality.
- Existing stock count and adjustment functionality.
- Existing POS consumption.
- Existing Recipe Studio.
- Existing recipe approval and publishing.
- Existing legacy recipe upload.
- Existing image assignment.
- Existing SILA Voice 2.0.
- Existing Transaction Tracker.

ERP schedulers remain PARKED.
NO LIVE SAP POSTINGS DURING DEVELOPMENT OR TESTING.
Use mocked integration responses.
No automatic production material creation, PO release, inventory posting or financial posting.

## 3. MANDATORY EXISTING SYSTEM AUDIT

Before implementing any functionality, inspect the actual repository (documented in `docs/culinary-intelligence/EXISTING_SYSTEM_AUDIT.md`).

Identify:
- Existing Recipe Studio frontend components.
- Existing Recipe Studio backend services.
- Recipe Master schema, Recipe Header, Recipe Ingredients, Recipe Instructions, Recipe Variants, Sub-recipes, Batch recipes, Recipe versioning, Recipe publishing, Legacy recipe upload, Recipe images.
- Material Master, Material aliases, Material category and group, Material UOM conversions.
- Supplier Master, Supplier catalog, Pricing services, Purchase history.
- Live Inventory, Stock reservations (if implemented), Inventory ledger, Batch and expiry management, Stock transfers, Weekly Buckets, Outlet Wishlists, Open purchase orders, GRN, Goods Issue, POS consumption, Wastage posting.
- User purchasing profiles, Existing AI provider configuration, Existing Python services, Existing SILA Voice, Existing approvals, Existing reporting framework, Existing Transaction Tracker, Existing deployment architecture.

Document actual reusable services. Do not invent file paths, API names, database tables or schema fields. Where functionality is missing, identify the dependency and implement it only in its authorized phase.

## 4. COMMON CULINARY INTELLIGENCE ARCHITECTURE

Create a modular architecture with:

### Presentation Layer
- Existing SILA Cloud.
- Existing SILA Mobile.
- Recipe Studio contextual navigation.
- Interactive planning workspaces.
- Dashboards.
- Reports.
- SILA Voice integration.

### AI Agent Layer
- Recipe Generation Agent.
- Material Intelligence Agent.
- Menu Planning Agent.
- Event Planning Agent.
- Procurement Recommendation Agent.
- Production Planning Agent.
- Food Cost Intelligence Agent.
- Profitability Intelligence Agent.
- Predictive Intelligence Agent.

### Deterministic Business Engine Layer
- Recipe Scaling Engine.
- UOM Conversion Engine.
- Yield Calculation Engine.
- Sub-Recipe Explosion Engine.
- Material Requirement Engine.
- Stock Availability Engine.
- Stock Reservation Engine.
- Procurement Optimization Engine.
- Food Cost Engine.
- Event P&L Engine.
- Production Scheduling Engine.
- Consumption Reconciliation Engine.

### Existing Business Service Layer
Reuse existing:
- Recipe Master.
- Material Master.
- Inventory.
- Supplier Master.
- Purchase Orders.
- Goods Receipts.
- Stock Transfers.
- Approvals.
- Transaction Tracker.
- ERP Integration Services.

The LLM must not directly write to the database, execute unrestricted APIs or post to SAP.
AI proposes. Deterministic engines calculate and validate. Authorized existing business services execute.

---

# SECTION B — ADVANCED PYTHON AND AI ENGINE FOUNDATION

## 5. INBUILT SILA CULINARY INTELLIGENCE ENGINE

Build an isolated, modular Python-based culinary intelligence service where compatible with the existing architecture.
Do not replace the existing SILA backend. Evaluate and reuse existing Python infrastructure.
The engine must support local intelligence even when external LLM services are disabled.

Local deterministic capabilities must include:
- Ingredient normalization.
- Material matching.
- Recipe scaling.
- Unit conversion.
- Yield calculation.
- Recipe explosion.
- Material requirement consolidation.
- Stock shortage calculation.
- Food costing.
- Event profitability calculation.
- Constraint optimization.
- Kitchen scheduling.
- Historical statistical analysis.

Local generative recipe creation requires an installed and validated local language model or an approved recipe knowledge base.
Do not pretend deterministic Python libraries alone can generate reliable culinary recipes.

## 6. ADVANCED PYTHON LIBRARIES

Evaluate the following libraries based on actual compatibility, commercial licensing, security, model availability and performance:

- **AI Agent Orchestration:** LangGraph, PydanticAI, Pydantic.
- **Local LLM:** Ollama, llama-cpp-python, existing approved local model infrastructure.
- **External LLM Providers:** google-genai, anthropic.
- **Material Intelligence:** sentence-transformers, RapidFuzz, FAISS, spaCy.
- **Mathematical and Data Engines:** NumPy, SciPy, Polars, Pandas, Python Decimal for financial calculations, Pint for supported unit conversions.
- **Optimization:** Google OR-Tools, OR-Tools CP-SAT, SciPy optimization.
- **Predictive Intelligence:** StatsForecast, LightGBM, XGBoost.
- **Model Runtime:** ONNX Runtime, existing supported CPU/GPU inference infrastructure.
- **API and Background Processing:** FastAPI, asyncio, existing background job framework, Celery only if compatible and genuinely required.
- **Documents and Reporting:** openpyxl, PyMuPDF, Plotly or existing reporting components.

Do not install all libraries blindly. Choose a minimal compatible stack per phase. Use isolated dependency management. Record actual package versions and licenses. Do not change unrelated dependencies.

## 7. AI PROVIDER ROUTER

Support four operating modes:
1. Local AI only.
2. Gemini.
3. Claude.
4. Hybrid task-based AI selection.

Implement a provider-independent interface. Allow model selection without changing business logic.
Support:
- Local model endpoint.
- Gemini API.
- Anthropic Claude API.
- Existing approved AI provider infrastructure.
- Structured JSON outputs.
- Schema validation.
- Timeout.
- Retry.
- Cancellation.
- Rate limits.
- Provider health.
- Token usage.
- Estimated cost.
- Prompt versioning.
- Response validation.
- Configurable fallback.

Do not automatically send customer data to an external provider unless the tenant has explicitly enabled that provider.
Fallback must respect tenant data privacy and approved provider policies.
If all authorized providers are unavailable, return an actionable error. Do not fabricate AI results.

## 8. CULINARY AI ENGINE SETUP

Extend the existing AI Setup framework. Add a contextual section: `"Culinary Intelligence — AI Engine Setup"`. Do not redesign unrelated Setup screens.

Provide:
- **Operating Mode:** Local AI, Gemini, Claude, Hybrid.
- **Provider Configuration:** Provider enabled/disabled, secure API credential reference, local endpoint, model selection, connection test, model availability test, provider health status, request timeout, maximum output tokens, temperature (where supported), daily usage limit, monthly usage limit, token budget, cost budget, retry policy, fallback policy.
- **Task-Based Engine Selection** (independent engine selection):
  1. Recipe generation.
  2. Menu generation.
  3. Event planning.
  4. Ingredient extraction.
  5. Recipe document interpretation.
  6. Material semantic matching.
  7. Ingredient substitution suggestions.
  8. Food cost recommendations.
  9. Management insights.
  10. Report summaries.
  11. Voice interpretation.
- Material calculations, financial calculations, inventory quantities and actual ERP values must remain deterministic regardless of the selected LLM.
- **Security:** Store credentials securely server-side, never expose full credentials to Mobile or Cloud frontend, mask configured credentials, reuse existing secret storage, restrict configuration to authorized administrators, audit configuration changes.
- **AI Usage Dashboard:** Provider, model, request count, successful requests, failed requests, input tokens, output tokens, estimated AI cost, average response time, fallback usage, local inference status, tenant-wise usage, feature-wise usage. Do not display fabricated live usage metrics.

---

# SECTION C — PREMIUM RECIPE STUDIO GUI

## 9. DESIGN PRINCIPLES

Create a premium, bright, modern hospitality enterprise UI. Reuse the existing SILA Cloud design system and approved components.
Use: clean white or existing light surfaces, premium typography, clear visual hierarchy, indigo and cyan accents compatible with existing SILA styling, recipe photography where authorized, material thumbnails from existing media, interactive KPI cards, expandable panels, responsive data tables, searchable dropdowns, contextual filters, side drawers, step-based planning workflows, comparison views, interactive charts, status indicators, progressive disclosure, loading states, empty states, actionable error states, accessible controls.
Do not use excessive gradients or decorative animation. Do not modify global SILA theme tokens. Apply visual improvements only to new Culinary Intelligence components.

## 10. RECIPE STUDIO NAVIGATION

Preserve all existing Recipe Studio options. Add contextual navigation for:
1. Overview.
2. Recipe Master.
3. Legacy Recipe Upload.
4. AI Recipe Planner.
5. Material Intelligence.
6. Material Creation Requests.
7. Menu Studio.
8. AI Event Planner.
9. Event Material Requirements.
10. Event Procurement.
11. Production Planner.
12. Food Cost Studio.
13. Event P&L.
14. Analytics.
15. Report Studio.
16. Approvals.
17. History and Transaction Tracker.

Provide contextual access to AI Engine Setup using existing navigation conventions. Do not add duplicate global navigation. Unimplemented modules must be clearly marked as planned or disabled. Do not expose nonfunctional buttons as production-ready features.

## 11. CULINARY INTELLIGENCE OVERVIEW DASHBOARD

Design a premium overview with:
- **KPI Cards:** Total recipes, published recipes, draft recipes, pending recipe approvals, AI-generated recipes, pending material mappings, material creation requests, upcoming events, events in progress, events with material shortages, events awaiting procurement, average food cost percentage, average recipe cost, event revenue, event contribution, event contribution margin, wastage percentage.
- **Dashboard Sections:** Upcoming events calendar, recipe performance, event profitability, food cost trend, material shortage alerts, pending approvals, top-performing recipes, high-cost recipes, ingredient price changes, kitchen production readiness, AI activity, recent transactions.
- **Filters:** Property, outlet, kitchen, event type, recipe category, cuisine, date range, event status.
Every production metric must use actual authorized data. Provide a separate clearly labeled demonstration mode for illustrative data. Do not insert demonstration transactions into production tenant databases.

---

# SECTION D — AI RECIPE PLANNER

## 12. RECIPE GENERATION WORKSPACE
Support: Enter a dish name, select an existing recipe, enter free-text culinary instructions, upload a supported recipe document, import an approved recipe reference, upload a dish photograph as inspiration, generate a recipe from available ingredients, generate a recipe based on a target budget. A photograph must not be treated as proof of exact ingredients or quantities.

## 13. RECIPE GENERATION INPUTS
Provide: Recipe name, cuisine, recipe category, recipe type, number of servings, serving size, serving UOM, preparation style, cooking method, dietary preference, allergens to avoid, target food cost, target selling price, maximum ingredient budget, available-stock-only option, preferred ingredients, excluded ingredients, property, kitchen, AI engine selection (if authorized), additional chef instructions. Use current user context for property and kitchen.

## 14. AI GENERATED RECIPE OUTPUT
Generate structured output containing:
- **Recipe Header:** Proposed recipe name, description, cuisine, category, recipe type, serving count, portion size, preparation time, cooking time, total estimated time, cooking method, batch yield, storage instructions, shelf-life suggestion requiring verification, dietary tags, potential allergen indicators.
- **Ingredients:** Ingredient description, ingredient category, quantity, recipe UOM, raw quantity, edible quantity, yield percentage, preparation loss, cooking loss, optional ingredient indicator, substitution options, preparation notes, material matching status, actual material reference after validation, estimated ingredient cost, cost source.
- **Instructions:** Preparation steps, cooking steps, sequence, batch preparation instructions, equipment suggestions, plating instructions, quality checkpoints, food safety review requirements.
- **Sub-Recipes:** Sauces, marinades, dressings, dough, stock, gravy, syrups, prepared bases, beverage mixtures. Reuse existing recipe formats and identifiers.

## 15. INTERACTIVE RECIPE SCALING
Provide: Number of servings input, serving size input, batch size input, quantity adjustment, yield adjustment, preparation buffer adjustment, waste allowance adjustment. Recalculate ingredient requirements and costing interactively using validated material-specific UOM conversions. Support chef-approved nonlinear scaling rules for spices, seasoning, leavening agents, cooking liquids, batch-size constraints, and equipment capacity. Show original and recalculated values without overwriting approved recipes.

## 16. RECIPE PLANNER ILLUSTRATIVE EXAMPLE
Demonstration recipe: `"Grilled Salmon with Lemon Butter Sauce"`, Base servings: 10.
Illustrative ingredients & unit costs:
- Salmon fillet: 1.8 kg @ AED 85/kg
- Unsalted butter: 0.25 kg @ AED 32/kg
- Lemon: 0.4 kg @ AED 8/kg
- Garlic: 0.06 kg @ AED 15/kg
- Asparagus: 1.0 kg @ AED 28/kg
- Olive oil: 0.15 L @ AED 35/L
- Sea salt: 0.025 kg @ AED 5/kg
- Fresh dill: 0.05 kg @ AED 45/kg
Display: ingredient table, material matching status, recipe quantity, unit cost, extended cost, total recipe cost, cost per serving, serving adjustment, missing material indicator, material creation request action. Clearly marked as demonstration data.

## 17. RECIPE EDITOR
Allow chefs to add/remove ingredients, modify quantities, change UOM, modify serving size, adjust preparation instructions, add sub-recipes, change ingredient alternatives, review material mappings, recalculate costs, compare AI suggestions, regenerate selected sections, save draft, submit for approval. Preserve manual chef edits and provenance.

## 18. RECIPE APPROVAL AND PUBLISHING
Workflow: AI Generated Draft → Chef Review → Material Mapping → Missing Material Resolution → Cost Validation → Recipe Approval → Recipe ID Creation or Existing Version Update → Image Assignment → Publishing. Reuse existing Recipe Master services, IDs, versioning, and approval history.

---

# SECTION E — MATERIAL INTELLIGENCE

## 19. MATERIAL MATCHING ENGINE
13-step matching sequence: (1) Exact material code, (2) Exact normalized description, (3) Approved material aliases, (4) Brand and product attributes, (5) Material category, (6) Material group, (7) Ingredient form, (8) Pack size, (9) UOM compatibility, (10) Fuzzy matching, (11) Semantic similarity, (12) Candidate ranking, (13) Chef confirmation where required. Tenant-isolated search only.

## 20. MATERIAL MATCHING ACCURACY
Distinguish: fresh vs frozen, raw vs cooked, whole vs sliced, salted vs unsalted, fresh cream vs cooking cream, meat cuts, fish species, food grades, brand variants, bottle sizes, pack sizes, base UOM, purchasing UOM, allergen characteristics. Never fabricate material codes or auto-select ambiguous matches.

## 21. MATERIAL MATCHING WORKSPACE
Display ingredient cards/table with AI ingredient description, suggested material, actual code, image, description, category, base/purchasing UOM, stock, latest approved cost, matching status, evidence, candidate alternatives. Statuses: Exact match, Suggested match, Multiple candidates, Unmatched, Material creation requested, Pending approval, Material created, Mapping completed.

## 22. MATERIAL CREATION REQUEST
Auto-prepare draft with proposed description, long description, category, group, base/purchasing UOM, pack size, brand, specification, storage requirements, proposed supplier, estimated price with source, reason, recipe/event reference, requested by, property, plant, supporting documents, allergen info, approval status. Workflow: Draft → Duplicate Check → Chef Submission → Procurement Review → Master Data Review → Approval → Material Creation → Material Synchronization → Recipe Mapping → Closure.

## 23. INGREDIENT SUBSTITUTION ENGINE
Suggest alternatives based on culinary suitability, current stock, cost, supplier availability, prep requirements, yield, dietary restrictions, verified allergens. Require chef approval.

---

# SECTION F — MENU STUDIO

## 24. MENU MANAGEMENT
Breakfast, Lunch, Dinner, Buffet, À la carte, Banquet, Corporate events, Weddings, Conferences, Seasonal, Special promotions, Beverage, Dietary menus. Create, select published recipes, generate AI menu, group by course, reorder, compare, cost, selling price, food cost, dietary coverage, allergen matrix, submit for approval, templates, duplicate.

## 25. AI MENU OPTIMIZATION
Support prompts like `"Create an international buffet for 500 guests with vegetarian, seafood, chicken, desserts and beverages, keeping food cost below 28%."` Combine LLM proposals with deterministic costing and OR-Tools constraint evaluation. Explain conflicting constraints rather than claiming infeasible menus meet budget.

## 26. MENU ENGINEERING
Popularity, selling price, ingredient cost, contribution, sales volume, profitability, prep complexity, wastage, availability. Stars / Plowhorses / Puzzles / Dogs only where sufficient actual sales data exists.

## 27. CUSTOMER-FACING MENU PRESENTATION
Premium preview, approved recipe photographs, course layout, event/property branding, dietary/verified allergen indicators, package pricing, PDF export, print layout.

---

# SECTION G — AI EVENT PLANNER

## 28. EVENT MASTER
Event ID, name, customer, customer reference, property, venue, type, date, start/end times, prep start/deadline, expected/guaranteed/additional/actual guests, adult/child categories, service type, cuisine, menu, responsible chef/F&B manager, kitchen, storage location, revenue model, price per guest, fixed revenue, target food cost, target contribution margin, budget, dietary requirements, notes, status.

## 29. EVENT LIFECYCLE
Draft → Menu Planning → Costing → Material Planning → Pending Approval → Approved → Stock Reserved → Procurement In Progress → Production Planned → In Preparation → Ready for Service → Event In Progress → Event Completed → Reconciliation Pending → Financial Review → Closed / Cancelled. Valid state transitions only.

## 30. EVENT PLANNING STUDIO UI
15 contextual tabs: Event Overview, Guest Planning, Menu, Recipes, Material Requirements, Stock Availability, Procurement, Production Schedule, Event P&L, What-if Analysis, Execution, Wastage and Returns, Reports, Approvals, History. Defined readiness indicators + blockers.

## 31. EVENT PLANNER ILLUSTRATIVE EXAMPLE
Demonstration event: `Corporate Gala Dinner` (`EVT-2027-0001`), Date: `15 January 2027`, Expected guests: `500` (`480` guaranteed + `20` additional), Service type: `International buffet`, Selling price: `AED 350` per guest, Target food cost: `28%`, Kitchen: `Banquet Kitchen`.
Suggested menu:
- Starters: Caesar Salad, Arabic Mezze, Mushroom Soup
- Main Courses: Grilled Salmon, Roast Chicken, Vegetable Biryani, Pasta
- Desserts: Chocolate Mousse, Cheesecake, Fresh Fruit
- Beverages: Fresh Juice, Mocktails, Soft Drinks, Water

## 32. GUEST PLANNING AND MENU QUANTITIES
Expected dish uptake %, guaranteed vs optional portions, adult/child portions, portion size, serving frequency, buffet replenishment allowance, prep/service buffer, raw-to-edible/cooking yield, existing prepared stock, historical consumption patterns, chef overrides with reasons.

---

# SECTION H — INTERACTIVE EVENT MATERIAL REQUIREMENT PLANNING

## 33. RECIPE EXPLOSION ENGINE
Event → Menu → Menu Items → Recipe Versions → Sub-Recipes → Ingredients → Material Master → Base UOM → Purchasing UOM. Nested sub-recipes, circular dependency detection, UOM consolidation, prepared stock deduction, no double-counting of yield/wastage.

## 34. INTERACTIVE MATERIAL REQUIREMENT SIMULATOR
Editable inputs (guest count, guaranteed guests, dish uptake, portion size, serving size, prep buffer, usable yield, cooking yield, existing prepared qty, available stock, incoming qty, existing reservations, safety stock, purchasing pack size) recalculating expected portions, edible qty, raw requirement, adjusted requirement, net available stock, projected stock, shortage, recommended transfer, recommended purchase, pack rounding, excess after rounding, estimated procurement cost. Sliders + numeric inputs + scenario save/compare/reset.

## 35. MATERIAL REQUIREMENT CALCULATION EXAMPLE
Guests: 500, Salmon uptake: 60%, Edible portion: 180 g, Prep buffer: 10%, Usable yield: 90%.
- Expected portions: `500 × 60% = 300 portions`
- Edible requirement: `300 × 180 g = 54 kg`
- Buffered edible requirement: `54 × 1.10 = 59.4 kg`
- Raw requirement: `59.4 ÷ 0.90 = 66 kg`
- Available stock: `25 kg`, Eligible incoming PO: `10 kg`
- Remaining shortage: `66 - 25 - 10 = 31 kg`

## 36. CONSOLIDATED MATERIAL REQUIREMENT TABLE
Columns: Material code, description, image, group, recipe/menu item references, required qty, base UOM, available stock, existing reservations, usable stock, incoming PO qty, incoming transfer qty, projected availability, shortage, recommended transfer, recommended purchase, purchasing UOM, pack size, supplier, required delivery date, estimated cost, material status, procurement status. Search, filter, sort, group by supplier/kitchen/group, expand recipe contribution, export, print, planning snapshots.

## 37. ILLUSTRATIVE MATERIAL REQUIREMENT DASHBOARD
Demo rows:
- Salmon: Required 66 kg, Available 25 kg, Incoming 10 kg, Shortage 31 kg
- Chicken: Required 100 kg, Available 60 kg, Incoming 40 kg, Shortage 0
- Basmati Rice: Required 75 kg, Available 100 kg, Incoming 0, Shortage 0
- Butter: Required 20 kg, Available 12 kg, Incoming 0, Shortage 8 kg
- Olive Oil: Required 15 L, Available 10 L, Incoming 5 L, Shortage 0
- Asparagus: Required 45 kg, Available 15 kg, Incoming 0, Shortage 30 kg
- Fresh Dill: Required 3 kg, Material not found, Material creation request required

## 38. EVENT STOCK AVAILABILITY
Integrate with Live Inventory for preparation date: on-hand, blocked, QI, expired/near-expiry, existing reservations, other approved events, outlets, Weekly Buckets, pending transfers, confirmed incoming POs, expected GRNs, safety stock, batch eligibility, prep deadline.

## 39. EVENT STOCK RESERVATION
Tenant-isolated, property/location-specific, event/material-referenced, UOM-validated, atomic concurrency controls, partial fulfillment, release/cancel, audit history; never alters physical inventory ledger directly.

---

# SECTION I — EVENT PROCUREMENT INTELLIGENCE

## 40. REPLENISHMENT RECOMMENDATION ENGINE
Priority order: (1) Event kitchen eligible stock, (2) Authorized alternative storage locations, (3) Eligible incoming POs, (4) Incoming stock transfers, (5) Chef-approved ingredient alternatives, (6) Remaining shortage, (7) Procurement recommendation.

## 41. STOCK TRANSFER RECOMMENDATIONS
Reuse existing ITO and 311/312 stock transfer configuration/APIs. No cross-plant 311 assumption. No automatic posting.

## 42. PROCUREMENT RECOMMENDATIONS
Net shortage, approved suppliers, catalog/contract pricing, pack size, MOQ, lead time, required delivery date, delivery schedule, open POs, purchasing org, user purchasing profile, account assignment, budget; OR-Tools optimization where appropriate.

## 43. EXISTING PO CREATION API
Reuse existing SILA PO creation API, User PO Defaults & Account Assignment (Company Code, Plant, Storage Location, Purchasing Org/Group, Cost Center, G/L Account, Account Assignment Category), idempotency, event-to-PO traceability, and approval workflow.

## 44. MULTI-EVENT PROCUREMENT
Consolidated demand across approved events while preserving individual event traceability and preventing double-counting.

---

# SECTION J — FOOD COST AND EVENT PROFITABILITY

## 45. RECIPE FOOD COST ENGINE
Ingredient cost, sub-recipe cost, batch cost, yield-adjusted cost, cost per portion/serving, wastage cost, selling price, contribution, food cost %, target vs actual, price variance. Configured cost source priority; missing cost marked missing (not zero); Decimal financial precision.

## 46. EVENT P&L VERSIONS
Three independent versions: Estimated P&L, Committed P&L, Actual P&L, with version snapshots and comparison.

## 47. EVENT REVENUE
Food, beverage, per-guest package, fixed event revenue, venue rental, service charges retained, additional services, sponsorship, other income, discounts, credit adjustments, refunds; gross vs net (excluding VAT collected for tax authority).

## 48. EVENT COST ELEMENTS
Food consumption, beverage consumption, kitchen/service manpower, temporary staffing, equipment rental, decoration, entertainment, transportation, utilities allocation, external catering, venue allocation, wastage, spoilage, direct expenses, approved overhead. Actual material cost derives from actual consumption and valuation (not raw PO value).

## 49. EVENT P&L DASHBOARD
Estimated/committed/actual revenue, food/beverage/manpower/other costs, total cost, gross contribution, contribution margin, food/beverage cost %, cost/revenue/contribution per guest, budget variance, break-even guests, planned vs actual guests, wastage cost; interactive charts.

## 50. INTERACTIVE EVENT P&L SIMULATOR
Editable inputs (guest count, guaranteed guests, selling price/guest, fixed revenue, food/beverage cost/guest, manpower, fixed/variable expenses, discount, target margin) recalculating revenue, total cost, contribution, contribution margin, food cost %, cost/guest, break-even guests, required selling price for target margin, max allowable food cost; best/base/worst scenario comparison.

## 51. ILLUSTRATIVE P&L EXAMPLE
Guests: `500`, Selling price: `AED 350/guest`, Food cost: `AED 90/guest`, Beverage cost: `AED 25/guest`, Other fixed expenses: `AED 35,000`.
Expected results:
- Revenue: `AED 175,000`
- Food cost: `AED 45,000`
- Beverage cost: `AED 12,500`
- Other expenses: `AED 35,000`
- Total event cost: `AED 92,500`
- Event contribution: `AED 82,500`
- Contribution margin: `~47.14%` (`82,500 / 175,000 = 47.1429%`)
- Food cost % of package revenue: `~25.71%` (`45,000 / 175,000 = 25.7143%`)
- Break-even guests: `149` (`ceil(35,000 / (350 - 90 - 25)) = ceil(35,000 / 235) = 149`)

---

# SECTION K — KITCHEN PRODUCTION PLANNER

## 52. PRODUCTION PLANNING
Kitchen-wise allocation, recipe-wise production, batch size/count, prep start/deadline, cooking duration, equipment, responsible chef/team, material issue, status, quality checkpoints, service readiness, chef validation of food safety/holding constraints.

## 53. KITCHEN SCHEDULING ENGINE
Evaluate OR-Tools CP-SAT for kitchen/equipment/staff capacity, prep duration, batch/recipe/sub-recipe dependencies, service time, holding limits, food safety constraints, conflict detection.

## 54. PRODUCTION PLANNER UI
Daily production board, kitchen-wise schedule, Gantt-style timeline, batch cards, assigned chef, dependencies, material readiness, progress, delayed tasks, deadlines, authorized rescheduling.

## 55. ILLUSTRATIVE PRODUCTION SCHEDULE
Service starts at `19:00`:
- `08:00–11:00`: Marinades and sauces — Main Kitchen
- `09:00–14:00`: Dessert preparation — Pastry Kitchen
- `10:00–13:00`: Vegetable preparation — Cold Kitchen
- `11:00–15:00`: Meat preparation — Butchery
- `14:00–18:00`: Main course cooking — Hot Kitchen
- `17:00–19:00`: Buffet setup — Banquet Team
- `19:00–22:00`: Service and replenishment — Banquet Kitchen

---

# SECTION L — EVENT EXECUTION, CONSUMPTION AND WASTAGE

## 56. EVENT MATERIAL LIFECYCLE
Planned, approved, reserved, ordered, received, transferred, issued, prepared, served, consumed, returned usable, wasted, spoiled, remaining, reconciled quantities using the existing immutable inventory ledger.

## 57. EVENT CONSUMPTION
Reuse existing Goods Issue, recipe consumption, POS consumption, stock transfer, inventory ledger; prevent duplicate consumption.

## 58. WASTAGE AND DAMAGE
Preparation/cooking wastage, spoilage, expired materials, breakage, service wastage, buffet leftovers, overproduction; reuse existing configured movement types/reasons (evaluate Z01/Z02 configuration carefully without inventing SAP movement types).

## 59. EVENT RETURNS
Validate identity, quantity, batch, expiry, condition, food safety eligibility, source event, destination location; never return unsafe/spoiled food to available stock.

## 60. EVENT RECONCILIATION AND CLOSURE
Review actual guests/revenue, reservations, open POs, GRNs, transfers, consumption, returns, wastage, unresolved material requests, pending expenses, variances, pending approvals; release unused reservations; preserve final snapshot.

---

# SECTION M — CULINARY ANALYTICS

## 61. RECIPE INTELLIGENCE DASHBOARD
Recipe counts, published, AI-generated, approval cycle time, avg cost/serving, high-cost recipes, price impact, profitability, matching rate, missing materials, substitution savings, usage frequency, menu item performance.

## 62. EVENT INTELLIGENCE DASHBOARD
Total/upcoming/completed events, readiness, revenue, cost, contribution, margin, avg cost/revenue per guest, avg food cost, procurement commitments, shortages, on-time readiness, planned vs actual consumption, wastage, budget variance.

## 63. INTERACTIVE DASHBOARDS
Date range, property, kitchen, recipe category, event type, supplier filters; drill-down, cross-filtering, tooltips, tables, export, saved filters, data freshness; KPI cards, bar/line/donut/waterfall/heatmap/calendar/Gantt/variance/Pareto charts.

## 64. ILLUSTRATIVE ANALYTICS DATA
Clearly labeled demo dashboard:
- Published recipes: `1,248`
- Upcoming events: `24`
- Average food cost: `26.8%`
- Event contribution margin: `42.5%`
- Target food cost: `28%`
- Monthly revenue & cost: Jan `450k / 285k`, Feb `580k / 350k`, Mar `720k / 430k`, Apr `650k / 380k`, May `850k / 480k`, Jun `920k / 520k` (AED)
- Monthly food cost trend: Jan `32%`, Feb `30.5%`, Mar `29%`, Apr `27.5%`, May `27%`, Jun `26.8%`

---

# SECTION N — REPORT STUDIO

## 65. RECIPE REPORTS (17 Reports)
1. Recipe Master Report
2. Recipe Cost Sheet
3. Ingredient Usage Report
4. Material Matching Report
5. Missing Material Report
6. Material Creation Request Report
7. Recipe Version History
8. Recipe Approval Report
9. Recipe Publishing Report
10. Allergen Matrix
11. Dietary Matrix
12. Nutrition Estimate Report
13. Recipe Profitability Report
14. Menu Engineering Report
15. Ingredient Price Impact Report
16. Ingredient Substitution Report
17. AI Recipe Generation History

## 66. EVENT REPORTS (23 Reports)
1. Event Summary Report
2. Event Menu Report
3. Event Guest Planning Report
4. Event P&L Report
5. Estimated versus Committed versus Actual P&L
6. Event Material Requirement Report
7. Stock Availability Report
8. Material Shortage Report
9. Procurement Recommendation Report
10. Event Purchase Order Report
11. Event GRN Report
12. Event Stock Transfer Report
13. Event Stock Reservation Report
14. Kitchen Production Report
15. Planned versus Actual Consumption
16. Event Wastage Report
17. Event Returns Report
18. Guest-wise Cost Report
19. Event Profitability Variance Report
20. Multi-Event Consolidated Report
21. Event Closure and Reconciliation Report
22. Event Approval Report
23. Event Transaction History

## 67. REPORT FUNCTIONALITY
Authorized filters, report preview, Excel export, PDF export, print, approval report attachment, event/recipe reference, generation timestamp, generated by, currency, data source, planning snapshot reference, actual vs estimated indicators.

---

# SECTION O — FOOD SAFETY, ALLERGENS AND NUTRITION

## 68. ALLERGEN INTELLIGENCE
Ingredient/recipe/menu allergen matrices, event dietary requirements, cross-contact warnings, substitution allergen impact, verified supplier specs, chef review, approval history; unknown allergen remains unknown.

## 69. NUTRITION INTELLIGENCE
Estimated calories, protein, carbs, fat, fiber, sugar, sodium, per-serving nutrition; distinguish estimated vs verified; never fabricate.

## 70. DIETARY MANAGEMENT
Vegetarian, Vegan, Gluten-free, Dairy-free, Nut-free, Halal, customer-configured classifications; require verified ingredient/prep data.

---

# SECTION P — PREDICTIVE INTELLIGENCE AND SUSTAINABILITY

## 71. HISTORICAL EVENT LEARNING
Analyze expected vs actual guests, dish uptake, consumption/guest, buffet replenishment, ingredient usage, wastage, profitability, supplier delivery, prep duration; require user acceptance.

## 72. DEMAND FORECASTING
Evaluate StatsForecast, LightGBM, XGBoost where sufficient historical data exists; uncertainty and data sufficiency indicators; time-based validation.

## 73. EVENT SUSTAINABILITY
Total wastage, wastage/guest, wastage %, wastage cost, overproduction indicators, avoidable vs unavoidable, ingredient utilization, reusable returns, waste reduction trend.

---

# SECTION Q — SILA VOICE INTEGRATION

## 74. SILA CULINARY VOICE AGENT
Extend existing SILA Voice 2.0 agent registry (`Backend/sila-voice-engine`) in relevant phase for culinary commands without bypassing approvals.

## 75. SILA VOICE 2.0 VISUAL IDENTITY FREEZE
Preserve exact orb and color palette (`#E0F2FE`, `#38BDF8`, `#6366F1`, `#312E81`, `#818CF8`, `#FFFFFF`, `#DC2626`), concentric rings, glow, white waveform, continuous conversation, and existing controls.

---

# SECTION R — CLOUD AND MOBILE

## 76. CLOUD FUNCTIONALITY
Full culinary planning and administration workspace across all authorized capabilities.

## 77. MOBILE FUNCTIONALITY
Chef-friendly responsive experience reusing existing Mobile architecture without exposing admin credentials or unrestricted financial settings.

---

# SECTION S — SECURITY, AUDIT AND APPROVAL

## 78. ROLE-BASED ACCESS CONTROL
Reuse existing RBAC across Executive Chef, Chef, Sous Chef, F&B Manager, Banquet Manager, Storekeeper, Procurement, Master Data Administrator, Finance Controller, Property Administrator, Approver, Auditor.

## 79. APPROVAL WORKFLOWS
Reuse existing approval framework for recipe, material request, menu, event, budget, MRP, procurement, cost override, substitution, event closure.

## 80. TRANSACTION TRACKER
Reuse existing Transaction Tracker (`InventoryWorkflowEvent` / `SilaConsolidatedTracker`) with correlation ID, tenant, user, property, references, status, error, retry eligibility; never log credentials.

## 81. AI GOVERNANCE
Track provider, model, prompt version, timestamps, input/output versions, user edits, validation/approval results, tokens, estimated cost; prompt injection protection; tool allowlists; structured outputs.

---

# SECTION T — DEVELOPMENT ROADMAP

- **82. Phase 0 — Foundation, UI and AI Setup** (Active Execution Scope)
- **83. Phase 1A — AI Recipe Generation**
- **84. Phase 1B — Material Intelligence**
- **85. Phase 1C — Material Requests and Recipe Lifecycle**
- **86. Phase 2A — Menu and Event Planner**
- **87. Phase 2B — Event MRP**
- **88. Phase 2C — Stock and Procurement**
- **89. Phase 3A — Food Cost and Event P&L**
- **90. Phase 3B — Analytics and Optimization**
- **91. Phase 4A — Production Planner**
- **92. Phase 4B — Execution and Reconciliation**
- **93. Phase 5 — Advanced Culinary Intelligence**

---

# SECTION U — TESTING AND ACCEPTANCE

- **94. Master Test Coverage:** Recipe, Material, Event, Procurement, Financial, Production, AI, and Regression test suites.
- **95. Master Acceptance Criteria:** Full Requirement Traceability Matrix in `docs/culinary-intelligence/REQUIREMENT_TRACEABILITY_MATRIX.md`.

---

# SECTION V — CURRENT EXECUTION INSTRUCTIONS (PHASE 0)

- **96. Execute Phase 0 Only** (Steps 1–15)
- **97. Phase 0 Acceptance Criteria**
- **98. Final Implementation Report**
