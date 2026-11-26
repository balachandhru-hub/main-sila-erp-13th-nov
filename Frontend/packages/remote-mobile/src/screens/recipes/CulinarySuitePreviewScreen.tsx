import React, { useMemo, useState } from 'react';
import {
  DEFAULT_CULINARY_FEATURE_FLAGS,
  getCulinaryAiStatus,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryAiApi';
import {
  calculateRecipeScaling,
  computeMrpRow,
  DEMO_EVENT_MRP_ROWS,
  DEMO_SALMON_RECIPE,
} from '../../../../remote-buyer/src/components/silaMe/recipes/culinary/culinarySimulatorsMath';
import ScreenHeader from '../../components/ScreenHeader';
import { useGo } from '../../navigation';
import { useLoad } from '../../useLoad';
import './recipeStudio.css';

/**
 * Bounded Phase 0 Mobile preview for Recipe Studio 2.0 (§77 & Step 14).
 * Never fetches or displays administrator AI credentials or unrestricted financial settings.
 */
const CulinarySuitePreviewScreen: React.FC = () => {
  const go = useGo();
  const statusLoad = useLoad(() => getCulinaryAiStatus(), 'mobile-culinary-ai-status');
  const [servings, setServings] = useState(10);
  const [guests, setGuests] = useState(500);

  const flags = statusLoad.data?.featureFlags ?? DEFAULT_CULINARY_FEATURE_FLAGS;

  const scaling = useMemo(
    () =>
      calculateRecipeScaling({
        baseServings: DEMO_SALMON_RECIPE.baseServings,
        targetServings: servings,
        baseServingSizeGrams: DEMO_SALMON_RECIPE.baseServingSizeGrams,
        targetServingSizeGrams: DEMO_SALMON_RECIPE.baseServingSizeGrams,
        prepBufferPercent: 0,
        wasteAllowancePercent: 0,
        ingredients: DEMO_SALMON_RECIPE.ingredients,
      }),
    [servings],
  );

  const mrpRows = useMemo(
    () => DEMO_EVENT_MRP_ROWS.map((row) => computeMrpRow(guests, row)),
    [guests],
  );

  return (
    <>
      <ScreenHeader title="Culinary Suite 2.0" eyebrow="Phase 0 Mobile Preview" back backTo="recipes" />
      <div className="sm-screen" data-testid="mobile-culinary-suite-preview">
        {!flags.culinarySuiteEnabled ? (
          <section className="sm-card">
            <p className="sm-meta">
              Recipe Studio 2.0 preview is currently disabled by your administrator feature flags.
            </p>
          </section>
        ) : (
          <>
            <section className="sm-card">
              <p className="sm-eyebrow">ISOLATED PHASE 0 DEMONSTRATION</p>
              <p className="sm-meta" style={{ marginTop: '0.25rem' }}>
                AI Mode: <strong>{statusLoad.data?.operatingMode ?? 'LOCAL'}</strong> · Offline AI generation is not claimed until a verified local runtime is enabled.
              </p>
            </section>

            <section className="sm-section">
              <h2>Interactive Portion Scaling (Demo)</h2>
              <div className="sm-card">
                <p style={{ fontWeight: 600, margin: 0 }}>{DEMO_SALMON_RECIPE.title}</p>
                <div className="sm-field" style={{ marginTop: '0.6rem' }}>
                  <label htmlFor="m-cs2-servings">Portions: <strong>{servings}</strong></label>
                  <input
                    id="m-cs2-servings"
                    type="range"
                    min={1}
                    max={250}
                    value={servings}
                    onChange={(e) => setServings(Number(e.target.value))}
                  />
                </div>
                <p className="sm-meta" style={{ marginTop: '0.4rem' }}>
                  Total Demo Cost: <strong>AED {scaling.scaledTotalCostAed.toFixed(2)}</strong> (AED {scaling.scaledCostPerServingAed.toFixed(2)} / serving)
                </p>
                <ul className="sm-list" style={{ marginTop: '0.5rem' }}>
                  {scaling.items.slice(0, 4).map((it) => (
                    <li key={it.id} className="sm-row" style={{ padding: '0.35rem 0' }}>
                      <span>{it.name}</span>
                      <span><strong>{it.grossQuantityWithBuffers.toFixed(2)} {it.uom}</strong></span>
                    </li>
                  ))}
                </ul>
              </div>
            </section>

            <section className="sm-section">
              <h2>Event Shortage Preview (EVT-2027-0001 Demo)</h2>
              <div className="sm-card">
                <div className="sm-field">
                  <label htmlFor="m-cs2-guests">Guests: <strong>{guests}</strong></label>
                  <input
                    id="m-cs2-guests"
                    type="range"
                    min={50}
                    max={1000}
                    step={10}
                    value={guests}
                    onChange={(e) => setGuests(Number(e.target.value))}
                  />
                </div>
                <ul className="sm-list" style={{ marginTop: '0.5rem' }}>
                  {mrpRows.map((r) => (
                    <li key={r.id} className="sm-row" style={{ padding: '0.35rem 0' }}>
                      <span>{r.materialName}</span>
                      <span>
                        {r.materialExists
                          ? r.shortageQty > 0
                            ? `Shortage ${r.shortageQty.toFixed(1)} ${r.baseUom}`
                            : 'Covered'
                          : 'Material Req.'}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            </section>

            <div className="sm-actions">
              <button type="button" className="sm-btn" onClick={() => go('recipes/catalog')}>
                Existing Recipe Catalog
              </button>
              <button type="button" className="sm-btn" onClick={() => go('recipes/approvals')}>
                Existing Approvals
              </button>
            </div>
          </>
        )}
      </div>
    </>
  );
};

export default CulinarySuitePreviewScreen;
