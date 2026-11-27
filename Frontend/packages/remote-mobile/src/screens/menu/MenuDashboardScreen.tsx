import React from 'react';
import { getMenuEngineering } from '../../../../remote-buyer/src/api/silaMe/silaMenuEngineeringApi';
import { getMeMobileSummary } from '../../../../remote-buyer/src/api/silaMe/menuEngineeringStudioApi';
import ScreenHeader from '../../components/ScreenHeader';
import StatusBadge from '../../components/StatusBadge';
import { Empty, ErrorNotice, Loading, OfflineBanner } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useGo } from '../../navigation';
import { useLoad } from '../../useLoad';
import { MENU_VIEWS } from './menuViews';

const today = (): string => new Date().toISOString().slice(0, 10);

const MenuDashboardScreen: React.FC = () => {
  const go = useGo();
  const { can } = useAccess();
  const date = today();
  const { data, loading, error, reload } = useLoad(
    () => getMenuEngineering('dashboard', { dateFrom: date, dateTo: date }),
    can.viewMenuEngineering ? `menu-dash-${date}` : null,
  );
  const mobile = useLoad(
    () => getMeMobileSummary().catch(() => null),
    can.viewMenuEngineering ? `menu-mobile-summary-${date}` : null,
  );
  const dash = data?.dashboard;
  const summary = mobile.data;

  const routeFor = (kind?: string): string => {
    if (kind === 'RECIPE_MISSING' || kind === 'COST_MISSING') return 'menu/items';
    if (kind === 'POS_UNMAPPED') return 'menu/mapping';
    if (kind === 'SHORTAGE' || kind === 'PURCHASE') return 'menu/recommendations';
    if (kind === 'VARIANCE' || kind === 'CONSUMPTION') return 'menu/consumption';
    if (kind === 'PMS_STALE' || kind === 'DEMAND') return 'menu/demand';
    return 'menu';
  };

  return (
    <>
      <ScreenHeader title="Menu Engineering" eyebrow="SILA ME & SILA STORE" intro="Today's published menus, portion instructions, ingredient readiness and operational links." />
      <div className="sm-screen">
        <OfflineBanner />
        {loading && <Loading text="Loading Menu Engineering…" />}
        {error && <ErrorNotice message={error} onRetry={reload} />}
        {!loading && !dash && !error && (
          <Empty text="No Menu Plan configured." description="Create a menu item, then plan today's meal." />
        )}
        {dash && (
          <>
            <div className="sm-grid-2">
              <button type="button" className="sm-stat" onClick={() => go('menu/demand')}>
                <span className="sm-muted">Occupancy</span>
                <strong>{dash.occupancyPercent == null ? '—' : `${dash.occupancyPercent}%`}</strong>
              </button>
              <button type="button" className="sm-stat" onClick={() => go('menu/demand')}>
                <span className="sm-muted">Expected guests</span>
                <strong>{dash.expectedGuests ?? '—'}</strong>
              </button>
              <button type="button" className="sm-stat" onClick={() => go('menu/demand')}>
                <span className="sm-muted">Expected covers</span>
                <strong>{dash.expectedCovers}</strong>
              </button>
              <button type="button" className="sm-stat" onClick={() => go('menu/portions')}>
                <span className="sm-muted">Planned portions</span>
                <strong>{dash.plannedPortions}</strong>
              </button>
              <button type="button" className="sm-stat" onClick={() => go('menu/recommendations')}>
                <span className="sm-muted">Forecast shortage</span>
                <strong>{dash.forecastShortage}</strong>
              </button>
              <button type="button" className="sm-stat" onClick={() => go('menu/consumption')}>
                <span className="sm-muted">Consumption variance</span>
                <strong>{dash.consumptionVariance}</strong>
              </button>
            </div>
            <p className="sm-meta">
              Source {dash.occupancySource || 'MANUAL'} · {dash.occupancyFreshness || 'UNAVAILABLE'}
              {summary ? ` · Pending Approvals: ${summary.pendingApprovals.length} · Ingredient Alerts: ${summary.ingredientReadiness.length}` : ''}
            </p>

            {/* Operational SILA Store handoffs — reuses existing mobile screens without duplicating */}
            <section className="sm-section">
              <h2>Operational Kitchen &amp; Store Actions</h2>
              <div className="sm-grid-2">
                <button type="button" className="sm-btn" onClick={() => go('menu/portions')}>Portion Sampling</button>
                <button type="button" className="sm-btn" onClick={() => go('inventory/goods-issues/new')}>Record Consumption</button>
                <button type="button" className="sm-btn" onClick={() => go('inventory/adjustments/new')}>Record Food Waste</button>
                <button type="button" className="sm-btn" onClick={() => go('inventory/stock')}>Ingredient Stock</button>
                <button type="button" className="sm-btn" onClick={() => go('recipes/catalog')}>Approved Recipes</button>
                <button type="button" className="sm-btn" onClick={() => go('purchasing/voice-po')}>SILA Voice</button>
                {summary && summary.pendingApprovals.length > 0 && (
                  <button type="button" className="sm-btn sm-btn--primary" onClick={() => go('tasks?tab=approvals')}>
                    Pending Approvals ({summary.pendingApprovals.length})
                  </button>
                )}
              </div>
            </section>

            {summary && summary.portionInstructions.length > 0 && (
              <section className="sm-section">
                <h2>Portion Instructions &amp; Readiness</h2>
                <ul className="sm-list">
                  {summary.portionInstructions.slice(0, 6).map((row) => (
                    <li key={row.menuPlanLineId}>
                      <button type="button" className="sm-list-btn" onClick={() => go('menu/portions')}>
                        <span className="sm-row">
                          <strong>{row.menuItemName}</strong>
                          <StatusBadge status={row.varianceStatus} />
                        </span>
                        <span className="sm-meta">
                          {row.expectedPortions} portions · {row.plannedPortion} {row.portionUom} · Usable {row.requiredUsableOutput} {row.portionUom}
                        </span>
                      </button>
                    </li>
                  ))}
                </ul>
              </section>
            )}

            <section className="sm-section">
              <h2>Today's &amp; Published Menus</h2>
              {dash.todayMenus.length === 0 && (summary?.todayMenus.length ?? 0) === 0 && (
                <Empty text="No Menu Plan configured." description="Create a plan for this date." />
              )}
              <ul className="sm-list">
                {(summary?.todayMenus.length ? summary.todayMenus : dash.todayMenus).map((plan) => (
                  <li key={plan.id}>
                    <button type="button" className="sm-list-btn" onClick={() => go('menu/planning')}>
                      <span className="sm-row">
                        <strong>{plan.name}</strong>
                        <StatusBadge status={plan.status} />
                      </span>
                      <span className="sm-meta">
                        {'mealPeriod' in plan ? (plan.mealPeriod || 'Meal') : 'Meal'}
                        {'lines' in plan ? ` · ${plan.lines.length} dishes · ${plan.expectedCovers ?? plan.expectedGuests} covers` : ` · ${plan.lineCount} items`}
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            </section>

            {summary && summary.upcomingMenus.length > 0 && (
              <section className="sm-section">
                <h2>Upcoming Menu Plans</h2>
                <ul className="sm-list">
                  {summary.upcomingMenus.slice(0, 5).map((plan) => (
                    <li key={plan.id}>
                      <button type="button" className="sm-list-btn" onClick={() => go('menu/planning')}>
                        <span className="sm-row">
                          <strong>{plan.name}</strong>
                          <StatusBadge status={plan.status} />
                        </span>
                        <span className="sm-meta">{plan.businessDate.slice(0, 10)} · {plan.mealPeriod || 'Meal'} · {plan.expectedCovers ?? plan.expectedGuests} covers</span>
                      </button>
                    </li>
                  ))}
                </ul>
              </section>
            )}

            <section className="sm-section">
              <h2>Menu mix</h2>
              <div className="sm-grid-2">
                <button type="button" className="sm-stat" onClick={() => go('menu/performance')}><span className="sm-muted">Stars</span><strong>{dash.stars}</strong></button>
                <button type="button" className="sm-stat" onClick={() => go('menu/performance')}><span className="sm-muted">Plowhorses</span><strong>{dash.plowhorses}</strong></button>
                <button type="button" className="sm-stat" onClick={() => go('menu/performance')}><span className="sm-muted">Puzzles</span><strong>{dash.puzzles}</strong></button>
                <button type="button" className="sm-stat" onClick={() => go('menu/performance')}><span className="sm-muted">Dogs</span><strong>{dash.dogs}</strong></button>
              </div>
            </section>

            <section className="sm-section">
              <h2>Attention required</h2>
              {dash.attention.length === 0 && <Empty text="No items require attention." />}
              <ul className="sm-list">
                {dash.attention.map((row, index) => (
                  <li key={`${row.kind}-${index}`}>
                    <button type="button" className="sm-list-btn" onClick={() => go(routeFor(row.kind))}>
                      <span className="sm-row">
                        <strong>{row.title}</strong>
                        <StatusBadge status={row.priority} />
                      </span>
                      <span className="sm-meta">{row.message}</span>
                    </button>
                  </li>
                ))}
              </ul>
            </section>

            <section className="sm-section">
              <h2>AI recommendations</h2>
              {dash.insights.length === 0 && <Empty text="No AI insight available for the selected period." />}
              <ul className="sm-list">
                {dash.insights.slice(0, 6).map((row, index) => (
                  <li key={index}>
                    <button type="button" className="sm-list-btn" onClick={() => go('menu/intelligence')}>
                      <span className="sm-row"><strong>{row.title}</strong><span className="sm-meta">{row.category}</span></span>
                      <span className="sm-meta">{row.reason}</span>
                    </button>
                  </li>
                ))}
              </ul>
            </section>
          </>
        )}

        <section className="sm-section">
          <h2>Menu Engineering</h2>
          <ul className="sm-list">
            {MENU_VIEWS.filter((row) => row.path !== 'menu').map((row) => (
              <li key={row.path}>
                <button type="button" className="sm-list-btn" onClick={() => go(row.path)}>
                  <span className="sm-row"><strong>{row.title}</strong><span aria-hidden="true">›</span></span>
                </button>
              </li>
            ))}
          </ul>
        </section>
      </div>
    </>
  );
};

export default MenuDashboardScreen;
