from __future__ import annotations

import networkx as nx
from app.schemas.me_twin_opt import (
    HistoricalSimilarityMatch,
    HistoricalSimilarityRequest,
    HistoricalSimilarityResponse,
    TwinDependencyGraphRequest,
    TwinDependencyGraphResponse,
    TwinImpactedNode,
    TwinIntegrityIssue,
)


def analyze_twin_dependency_graph(req: TwinDependencyGraphRequest) -> TwinDependencyGraphResponse:
    g = nx.DiGraph()
    node_map = {n.node_id: n for n in req.nodes}
    issues: list[TwinIntegrityIssue] = []

    for n in req.nodes:
        g.add_node(n.node_id, data=n)
        if not n.is_approved and n.node_type in {"RECIPE", "SUB_RECIPE"}:
            issues.append(
                TwinIntegrityIssue(
                    issue_type="UNAPPROVED_RECIPE",
                    severity="WARNING",
                    node_ids=[n.node_id],
                    detail=f"{n.node_type} '{n.label}' has no approved recipe version.",
                )
            )
        if n.node_type == "INGREDIENT" and (n.unit_cost is None or n.unit_cost < 0):
            issues.append(
                TwinIntegrityIssue(
                    issue_type="MISSING_COST_REFERENCE",
                    severity="ERROR",
                    node_ids=[n.node_id],
                    detail=f"Ingredient '{n.label}' is missing a valid unit cost reference.",
                )
            )

    for e in req.edges:
        if e.source_id not in node_map or e.target_id not in node_map:
            issues.append(
                TwinIntegrityIssue(
                    issue_type="INVALID_REFERENCE",
                    severity="ERROR",
                    node_ids=[e.source_id, e.target_id],
                    detail=f"Edge references missing node ({e.source_id} -> {e.target_id}).",
                )
            )
            continue
        if not e.uom_valid:
            issues.append(
                TwinIntegrityIssue(
                    issue_type="INVALID_UOM_CONVERSION",
                    severity="ERROR",
                    node_ids=[e.source_id, e.target_id],
                    detail=f"Invalid UOM conversion on edge {e.source_id} -> {e.target_id} ({e.uom or 'UNKNOWN'}).",
                )
            )
        g.add_edge(e.source_id, e.target_id, edge=e)

    cycles = list(nx.simple_cycles(g))
    is_acyclic = len(cycles) == 0
    for cyc in cycles:
        issues.append(
            TwinIntegrityIssue(
                issue_type="CIRCULAR_DEPENDENCY",
                severity="ERROR",
                node_ids=cyc,
                detail=f"Circular dependency detected: {' -> '.join(cyc + [cyc[0]])}",
            )
        )

    impacted_nodes: list[TwinImpactedNode] = []
    affected_recipes: list[str] = []
    affected_menu_items: list[str] = []
    affected_menus: list[str] = []
    affected_events: list[str] = []
    total_cost_impact = 0.0
    total_margin_impact = 0.0

    if req.shock_node_id and req.shock_node_id in g and abs(req.price_change_percent) > 1e-9:
        # Compute baseline node costs from explicit unit_cost or bottom-up propagation
        baseline_cost: dict[str, float] = {}
        shocked_cost: dict[str, float] = {}

        # Use DAG condensation if cycles exist so traversal always terminates cleanly
        if is_acyclic:
            topo_order = list(nx.topological_sort(g))
        else:
            topo_order = [n.node_id for n in req.nodes if n.node_id in g]

        for nid in topo_order:
            node = node_map[nid]
            in_edges = list(g.in_edges(nid, data=True))
            if not in_edges:
                base_c = float(node.unit_cost or 0.0)
            else:
                # Aggregate incoming ingredient / sub-recipe costs
                derived = 0.0
                for src, _, attr in in_edges:
                    edge = attr["edge"]
                    yield_factor = max(0.05, float(edge.yield_percent) / 100.0)
                    qty = float(edge.quantity) / yield_factor
                    derived += baseline_cost.get(src, float(node_map[src].unit_cost or 0.0)) * qty
                base_c = float(node.unit_cost) if (node.unit_cost is not None and node.unit_cost > 0 and derived <= 0) else derived
            baseline_cost[nid] = base_c

            if nid == req.shock_node_id:
                shocked_cost[nid] = base_c * (1.0 + req.price_change_percent / 100.0)
            elif not in_edges:
                shocked_cost[nid] = base_c
            else:
                derived_shock = 0.0
                for src, _, attr in in_edges:
                    edge = attr["edge"]
                    yield_factor = max(0.05, float(edge.yield_percent) / 100.0)
                    qty = float(edge.quantity) / yield_factor
                    derived_shock += shocked_cost.get(src, baseline_cost.get(src, 0.0)) * qty
                shocked_cost[nid] = derived_shock if derived_shock > 0 else base_c

        # Reachable descendants from shock_node_id using shortest-path hop distance
        lengths = nx.single_source_shortest_path_length(g, req.shock_node_id)
        for nid, dist in sorted(lengths.items(), key=lambda x: (x[1], x[0])):
            if nid == req.shock_node_id:
                continue
            node = node_map[nid]
            b_cost = baseline_cost.get(nid, 0.0)
            s_cost = shocked_cost.get(nid, b_cost)
            delta = s_cost - b_cost
            if abs(delta) < 1e-9:
                continue
            pct = (delta / b_cost * 100.0) if b_cost > 1e-9 else 0.0
            covers = max(1.0, float(node.planned_covers or 1.0))
            margin_delta = -delta * (covers if node.node_type in {"MENU", "EVENT"} else 1.0)
            impacted_nodes.append(
                TwinImpactedNode(
                    node_id=nid,
                    node_type=node.node_type,
                    label=node.label,
                    hop_distance=dist,
                    baseline_cost=round(b_cost, 4),
                    shocked_cost=round(s_cost, 4),
                    cost_delta=round(delta, 4),
                    cost_delta_percent=round(pct, 2),
                    margin_delta=round(margin_delta, 4),
                )
            )
            if node.node_type in {"RECIPE", "SUB_RECIPE"}:
                affected_recipes.append(nid)
            elif node.node_type == "MENU_ITEM":
                affected_menu_items.append(nid)
                total_cost_impact += delta * covers
                total_margin_impact -= delta * covers
            elif node.node_type == "MENU":
                affected_menus.append(nid)
            elif node.node_type == "EVENT":
                affected_events.append(nid)

        if not affected_menu_items and impacted_nodes:
            # Fallback sum at highest downstream level if menu items aren't directly labeled
            leaf_impacts = [n for n in impacted_nodes if g.out_degree(n.node_id) == 0]
            total_cost_impact = sum(n.cost_delta for n in leaf_impacts)
            total_margin_impact = -total_cost_impact

    return TwinDependencyGraphResponse(
        node_count=g.number_of_nodes(),
        edge_count=g.number_of_edges(),
        is_acyclic=is_acyclic,
        cycles=cycles,
        integrity_issues=issues,
        shock_node_id=req.shock_node_id,
        price_change_percent=req.price_change_percent,
        impacted_nodes=impacted_nodes,
        affected_recipes=affected_recipes,
        affected_menu_items=affected_menu_items,
        affected_menus=affected_menus,
        affected_events=affected_events,
        total_cost_impact=round(total_cost_impact, 2),
        total_margin_impact=round(total_margin_impact, 2),
    )


def compute_historical_similarity(req: HistoricalSimilarityRequest) -> HistoricalSimilarityResponse:
    target = req.target
    t_dishes = {d.strip().lower() for d in target.dishes if d.strip()}
    matches: list[HistoricalSimilarityMatch] = []

    for c in req.candidates:
        if c.record_id == target.record_id:
            continue
        score = 0.0
        matching: list[str] = []
        diffs: list[str] = []

        if c.event_type.lower() == target.event_type.lower():
            score += 0.18
            matching.append(f"Event Type ({c.event_type})")
        else:
            diffs.append(f"Event Type: {c.event_type} vs {target.event_type}")

        if c.service_format.lower() == target.service_format.lower():
            score += 0.15
            matching.append(f"Service Format ({c.service_format})")
        else:
            diffs.append(f"Service Format: {c.service_format} vs {target.service_format}")

        if c.cuisine.lower() == target.cuisine.lower():
            score += 0.14
            matching.append(f"Cuisine ({c.cuisine})")
        else:
            diffs.append(f"Cuisine: {c.cuisine} vs {target.cuisine}")

        if c.outlet.lower() == target.outlet.lower():
            score += 0.08
            matching.append(f"Outlet ({c.outlet})")

        if c.season.lower() == target.season.lower():
            score += 0.07
            matching.append(f"Season ({c.season})")

        # Numeric proximity: guest count, selling price, food cost target
        g_gap = abs(c.guest_count - target.guest_count) / max(1.0, target.guest_count)
        g_sim = max(0.0, 1.0 - min(1.0, g_gap))
        score += 0.13 * g_sim
        if g_sim >= 0.75:
            matching.append(f"Guest Count (~{int(c.guest_count)})")
        else:
            diffs.append(f"Guest Count: {int(c.guest_count)} vs {int(target.guest_count)}")

        p_gap = abs(c.selling_price - target.selling_price) / max(1.0, target.selling_price)
        p_sim = max(0.0, 1.0 - min(1.0, p_gap))
        score += 0.10 * p_sim
        if p_sim >= 0.8:
            matching.append(f"Selling Price (~{c.selling_price:.0f})")

        c_dishes = {d.strip().lower() for d in c.dishes if d.strip()}
        if t_dishes and c_dishes:
            jaccard = len(t_dishes & c_dishes) / max(1, len(t_dishes | c_dishes))
            score += 0.15 * jaccard
            if jaccard >= 0.3:
                matching.append(f"Menu Overlap ({round(jaccard * 100)}%)")
        else:
            # Neutral partial credit when dish list omitted
            score += 0.05

        matches.append(
            HistoricalSimilarityMatch(
                record_id=c.record_id,
                title=c.title,
                similarity_score=round(min(1.0, score), 4),
                matching_factors=matching,
                important_differences=diffs,
                actual_food_cost_percent=round(c.actual_food_cost_percent, 2),
                actual_waste_percent=round(c.actual_waste_percent, 2),
                actual_margin=round(c.actual_margin, 2),
            )
        )

    matches.sort(key=lambda m: m.similarity_score, reverse=True)
    return HistoricalSimilarityResponse(matches=matches[: req.top_k])
