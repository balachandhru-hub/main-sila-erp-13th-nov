import React, { useMemo, useRef, useState } from 'react';
import {
  DEFAULT_CULINARY_FEATURE_FLAGS,
  getCulinaryAiStatus,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryAiApi';
import {
  cancelCulinaryGeneration,
  generateCulinaryRecipe,
  getCulinaryPlannerContext,
  getCulinaryRecipeDraft,
  listCulinaryRecipeDrafts,
  prepareCulinaryDishImage,
  refineCulinaryRecipe,
  saveCulinaryRecipeDraft,
  type CulinaryDishImagePrepared,
  type CulinaryRecipeDraftDetail,
  type CulinaryRefineResponse,
  type CulinaryStructuredRecipePayload,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryRecipesApi';
import {
  applyIngredientSubstitution,
  bulkAcceptExactMappings,
  confirmMaterialMapping,
  getMaterialWorkspace,
  matchDraftMaterials,
  prepareMaterialRequestDraft,
  rejectMaterialMapping,
  searchMaterialsForMapping,
  suggestIngredientSubstitutions,
  type SilaCulinaryMaterialMappingLine,
  type SilaCulinaryMaterialSearchItem,
  type SilaCulinaryMaterialWorkspace,
  type SilaCulinarySubstitutionResponse,
} from '../../../../remote-buyer/src/api/silaMe/silaCulinaryMaterialsApi';
import { scaleDraftLocally } from '../../../../remote-buyer/src/components/silaMe/recipes/culinary/culinaryRecipePlannerHelpers';
import {
  calculateRecipeScaling,
  computeMrpRow,
  DEMO_EVENT_MRP_ROWS,
  DEMO_SALMON_RECIPE,
} from '../../../../remote-buyer/src/components/silaMe/recipes/culinary/culinarySimulatorsMath';
import ScreenHeader from '../../components/ScreenHeader';
import { useGo } from '../../navigation';
import { errorText, useLoad } from '../../useLoad';
import './recipeStudio.css';

type MobileTab = 'generate' | 'draft' | 'materials' | 'history' | 'demo';

/**
 * Phase 1A + Phase 1B Mobile Culinary Intelligence — responsive chef AI recipe generation,
 * dish photograph capture/upload, serving scaling, quantity editing, basic refinement,
 * draft saving, draft history, and Material Intelligence mapping (§39, §43).
 * Never fetches or displays administrator AI model settings or credentials.
 */
const CulinarySuitePreviewScreen: React.FC = () => {
  const go = useGo();
  const statusLoad = useLoad(() => getCulinaryAiStatus(), 'mobile-culinary-ai-status');
  const contextLoad = useLoad(() => getCulinaryPlannerContext(), 'mobile-culinary-context');
  const draftsLoad = useLoad(() => listCulinaryRecipeDrafts(), 'mobile-culinary-drafts');

  const [tab, setTab] = useState<MobileTab>('generate');
  const [recipeName, setRecipeName] = useState('');
  const [cuisine, setCuisine] = useState('');
  const [servings, setServings] = useState(10);
  const [servingSize, setServingSize] = useState(180);
  const [dietaryText, setDietaryText] = useState('');
  const [instructionsText, setInstructionsText] = useState('');
  const [dishImage, setDishImage] = useState<CulinaryDishImagePrepared | null>(null);
  const [dishImageName, setDishImageName] = useState<string | null>(null);
  const [generating, setGenerating] = useState(false);
  const [statusMsg, setStatusMsg] = useState<string | null>(null);
  const [errMsg, setErrMsg] = useState<string | null>(null);
  const activeGenRef = useRef<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  const [activeDraft, setActiveDraft] = useState<CulinaryRecipeDraftDetail | null>(null);
  const [payload, setPayload] = useState<CulinaryStructuredRecipePayload | null>(null);
  const [targetServings, setTargetServings] = useState(10);
  const [targetServingSize, setTargetServingSize] = useState(180);
  const [refineText, setRefineText] = useState('');
  const [refining, setRefining] = useState(false);
  const [pendingRefine, setPendingRefine] = useState<CulinaryRefineResponse | null>(null);
  const [saving, setSaving] = useState(false);

  // Phase 1B Mobile Material Intelligence state
  const [matWs, setMatWs] = useState<SilaCulinaryMaterialWorkspace | null>(null);
  const [matLoading, setMatLoading] = useState(false);
  const [matMatching, setMatMatching] = useState(false);
  const [activeMatRef, setActiveMatRef] = useState<string | null>(null);
  const [matSearchQ, setMatSearchQ] = useState('');
  const [matSearchItems, setMatSearchItems] = useState<SilaCulinaryMaterialSearchItem[]>([]);
  const [matSubs, setMatSubs] = useState<SilaCulinarySubstitutionResponse | null>(null);
  const [reqDraftDesc, setReqDraftDesc] = useState('');
  const [reqDraftReason, setReqDraftReason] = useState('');

  // Phase 0 Demo fallback tab state
  const [demoServings, setDemoServings] = useState(10);
  const [guests, setGuests] = useState(500);

  const flags = statusLoad.data?.featureFlags ?? DEFAULT_CULINARY_FEATURE_FLAGS;

  const demoScaling = useMemo(
    () =>
      calculateRecipeScaling({
        baseServings: DEMO_SALMON_RECIPE.baseServings,
        targetServings: demoServings,
        baseServingSizeGrams: DEMO_SALMON_RECIPE.baseServingSizeGrams,
        targetServingSizeGrams: DEMO_SALMON_RECIPE.baseServingSizeGrams,
        prepBufferPercent: 0,
        wasteAllowancePercent: 0,
        ingredients: DEMO_SALMON_RECIPE.ingredients,
      }),
    [demoServings],
  );

  const mrpRows = useMemo(
    () => DEMO_EVENT_MRP_ROWS.map((row) => computeMrpRow(guests, row)),
    [guests],
  );

  const scaledDraft = useMemo(() => {
    if (!payload) return null;
    return scaleDraftLocally({
      recipeName: payload.header.proposed_name,
      baseServings: payload.header.base_servings || 10,
      targetServings,
      baseServingSize: payload.header.serving_size || 180,
      targetServingSize,
      prepBufferPercent: 0,
      useNonlinearRules: true,
      ingredients: payload.ingredients,
    });
  }, [payload, targetServings, targetServingSize]);

  const hydrateDraft = (detail: CulinaryRecipeDraftDetail) => {
    setActiveDraft(detail);
    setPayload(detail.payload);
    setTargetServings(detail.payload.header.base_servings || 10);
    setTargetServingSize(detail.payload.header.serving_size || 180);
    setPendingRefine(null);
    setTab('draft');
  };

  const handlePhotoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setErrMsg(null);
    try {
      const prepared = await prepareCulinaryDishImage(file);
      setDishImage(prepared);
      setDishImageName(file.name);
      setStatusMsg(`Photo ready (${prepared.width}x${prepared.height}). Inferred ingredients will require chef review.`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Unable to prepare dish photo.'));
    } finally {
      e.target.value = '';
    }
  };

  const handleGenerate = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrMsg(null);
    setStatusMsg(null);
    if (!recipeName.trim() && !dishImage) {
      setErrMsg('Enter a recipe name or attach a dish photograph.');
      return;
    }

    const genRef = `MOB-${Date.now()}`;
    activeGenRef.current = genRef;
    const ctrl = new AbortController();
    abortRef.current = ctrl;
    setGenerating(true);

    try {
      const created = await generateCulinaryRecipe(
        {
          generationMode: dishImage ? 'DISH_IMAGE' : instructionsText.trim() ? 'DETAILED_INSTRUCTIONS' : 'DISH_NAME',
          recipeName: recipeName.trim() || 'Dish Inspiration Recipe',
          recipeDescription: instructionsText.trim(),
          cuisine: cuisine.trim(),
          servings: Math.max(1, servings),
          servingSize: Math.max(1, servingSize),
          servingUom: 'G',
          dietaryPreferences: dietaryText
            .split(',')
            .map((s) => s.trim())
            .filter(Boolean),
          locationId: contextLoad.data?.locations?.[0]?.locationId ?? null,
          imageBase64: dishImage?.base64 ?? null,
          imageMimeType: dishImage?.mime_type ?? null,
          generationReference: genRef,
        },
        ctrl.signal,
      );
      hydrateDraft(created);
      draftsLoad.reload();
      setStatusMsg(`Generated via ${created.provider} (${created.model}).`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Recipe generation failed.'));
    } finally {
      setGenerating(false);
      activeGenRef.current = null;
      abortRef.current = null;
    }
  };

  const handleCancelGenerate = async () => {
    abortRef.current?.abort();
    if (activeGenRef.current) {
      await cancelCulinaryGeneration(activeGenRef.current);
    }
    setGenerating(false);
    setStatusMsg('Generation cancelled.');
  };

  const handleRefine = async () => {
    if (!payload || !refineText.trim()) return;
    setErrMsg(null);
    setRefining(true);
    try {
      const res = await refineCulinaryRecipe(
        { instruction: refineText.trim(), currentPayload: payload },
        activeDraft?.id,
      );
      setPendingRefine(res);
      setRefineText('');
    } catch (caught) {
      setErrMsg(errorText(caught, 'Refinement failed.'));
    } finally {
      setRefining(false);
    }
  };

  const handleSaveDraft = async () => {
    if (!activeDraft || !payload) return;
    setErrMsg(null);
    setSaving(true);
    try {
      const saved = await saveCulinaryRecipeDraft(activeDraft.id, {
        expectedRevisionNumber: activeDraft.revisionNumber,
        status: 'CHEF_REVIEW',
        changeType: 'CHEF_EDIT',
        changeSummary: 'Saved from SILA Mobile Culinary AI Planner.',
        payload,
      });
      hydrateDraft(saved);
      draftsLoad.reload();
      setStatusMsg(`Saved ${saved.draftReference} (Rev ${saved.revisionNumber}).`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Failed to save draft.'));
    } finally {
      setSaving(false);
    }
  };

  const handleOpenDraft = async (draftId: string) => {
    setErrMsg(null);
    try {
      const detail = await getCulinaryRecipeDraft(draftId);
      hydrateDraft(detail);
      setMatWs(null);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Unable to open recipe draft.'));
    }
  };

  const loadMaterialWorkspace = async (draftId: string) => {
    setErrMsg(null);
    setMatLoading(true);
    try {
      const ws = await getMaterialWorkspace(draftId);
      setMatWs(ws);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Unable to load material mapping workspace.'));
    } finally {
      setMatLoading(false);
    }
  };

  const handleOpenMaterialsTab = async () => {
    setTab('materials');
    if (activeDraft && (!matWs || matWs.draftId !== activeDraft.id)) {
      await loadMaterialWorkspace(activeDraft.id);
    }
  };

  const handleMobileMatchAll = async () => {
    if (!activeDraft) return;
    setErrMsg(null);
    setMatMatching(true);
    try {
      const ws = await matchDraftMaterials(activeDraft.id, { unresolvedOnly: false });
      setMatWs(ws);
      setStatusMsg(`Matched: ${ws.kpis.exactMatches} exact, ${ws.kpis.suggestedMatches} suggested.`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Material matching failed.'));
    } finally {
      setMatMatching(false);
    }
  };

  const handleMobileBulkAccept = async () => {
    if (!activeDraft) return;
    setErrMsg(null);
    setMatMatching(true);
    try {
      const ws = await bulkAcceptExactMappings(activeDraft.id);
      setMatWs(ws);
      setStatusMsg(`Confirmed ${ws.kpis.confirmedMappings} exact mapping(s).`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Bulk accept failed.'));
    } finally {
      setMatMatching(false);
    }
  };

  const handleMobileLineConfirm = async (
    line: SilaCulinaryMaterialMappingLine,
    materialId: string | null,
    materialCode: string | null,
  ) => {
    if (!activeDraft) return;
    setErrMsg(null);
    try {
      await confirmMaterialMapping(activeDraft.id, line.ingredientTempRef, {
        expectedMappingRevision: line.mappingRevision,
        materialId,
        materialCode,
      });
      await loadMaterialWorkspace(activeDraft.id);
      setActiveMatRef(null);
      setStatusMsg(`Confirmed ${materialCode ?? 'material'} for ${line.originalDescription}.`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Confirm failed.'));
    }
  };

  const handleMobileLineReject = async (line: SilaCulinaryMaterialMappingLine) => {
    if (!activeDraft) return;
    setErrMsg(null);
    try {
      await rejectMaterialMapping(activeDraft.id, line.ingredientTempRef, {
        expectedMappingRevision: line.mappingRevision,
        reason: 'Rejected on mobile review',
      });
      await loadMaterialWorkspace(activeDraft.id);
      setStatusMsg(`Rejected candidate for ${line.originalDescription}.`);
    } catch (caught) {
      setErrMsg(errorText(caught, 'Reject failed.'));
    }
  };

  return (
    <>
      <ScreenHeader title="AI Recipe Planner" eyebrow="Phase 1A Mobile" back backTo="recipes" />
      <div className="sm-screen" data-testid="mobile-culinary-suite-preview">
        {!flags.culinarySuiteEnabled ? (
          <section className="sm-card">
            <p className="sm-meta">
              Culinary Intelligence is currently disabled by your administrator feature flags.
            </p>
          </section>
        ) : (
          <>
            <section className="sm-card">
              <p className="sm-eyebrow">CULINARY INTELLIGENCE · PHASE 1A</p>
              <p className="sm-meta" style={{ marginTop: '0.25rem' }}>
                Engine: <strong>{statusLoad.data?.operatingMode ?? 'LOCAL'}</strong> · Offline AI generation is not claimed unless a verified local backend runtime is connected.
              </p>
            </section>

            <div className="rs-tabs" role="tablist">
              <button type="button" className={`rs-tab ${tab === 'generate' ? 'rs-tab--on' : ''}`} onClick={() => setTab('generate')}>
                Generate
              </button>
              <button type="button" className={`rs-tab ${tab === 'draft' ? 'rs-tab--on' : ''}`} onClick={() => setTab('draft')}>
                Draft{activeDraft ? ` (R${activeDraft.revisionNumber})` : ''}
              </button>
              <button type="button" className={`rs-tab ${tab === 'materials' ? 'rs-tab--on' : ''}`} onClick={handleOpenMaterialsTab}>
                Materials{matWs ? ` (${matWs.kpis.confirmedMappings}/${matWs.kpis.totalIngredients})` : ''}
              </button>
              <button type="button" className={`rs-tab ${tab === 'history' ? 'rs-tab--on' : ''}`} onClick={() => setTab('history')}>
                History ({draftsLoad.data?.length ?? 0})
              </button>
              <button type="button" className={`rs-tab ${tab === 'demo' ? 'rs-tab--on' : ''}`} onClick={() => setTab('demo')}>
                Demo
              </button>
            </div>

            {errMsg && (
              <section className="sm-card" style={{ borderLeft: '4px solid #dc2626' }} role="alert">
                <p style={{ margin: 0, color: '#991b1b', fontSize: '0.85rem' }}>{errMsg}</p>
              </section>
            )}
            {statusMsg && (
              <section className="sm-card" style={{ borderLeft: '4px solid #059669' }} role="status">
                <p style={{ margin: 0, color: '#065f46', fontSize: '0.85rem' }}>{statusMsg}</p>
              </section>
            )}

            {tab === 'generate' && (
              <form className="sm-card" onSubmit={handleGenerate}>
                <div className="sm-field">
                  <label htmlFor="m-p1a-name">Recipe Name</label>
                  <input id="m-p1a-name" placeholder="e.g. Butter Chicken" value={recipeName} onChange={(e) => setRecipeName(e.target.value)} />
                </div>
                <div className="sm-field" style={{ marginTop: '0.5rem' }}>
                  <label htmlFor="m-p1a-cuisine">Cuisine</label>
                  <input id="m-p1a-cuisine" placeholder="e.g. Indian" value={cuisine} onChange={(e) => setCuisine(e.target.value)} />
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem', marginTop: '0.5rem' }}>
                  <div className="sm-field">
                    <label htmlFor="m-p1a-srv">Servings</label>
                    <input id="m-p1a-srv" type="number" min={1} value={servings} onChange={(e) => setServings(Math.max(1, Number(e.target.value) || 1))} />
                  </div>
                  <div className="sm-field">
                    <label htmlFor="m-p1a-sz">Serving Size (g)</label>
                    <input id="m-p1a-sz" type="number" min={10} value={servingSize} onChange={(e) => setServingSize(Math.max(10, Number(e.target.value) || 100))} />
                  </div>
                </div>
                <div className="sm-field" style={{ marginTop: '0.5rem' }}>
                  <label htmlFor="m-p1a-diet">Dietary Preferences</label>
                  <input id="m-p1a-diet" placeholder="e.g. Halal, Gluten-free" value={dietaryText} onChange={(e) => setDietaryText(e.target.value)} />
                </div>
                <div className="sm-field" style={{ marginTop: '0.5rem' }}>
                  <label htmlFor="m-p1a-inst">Chef Instructions (Optional)</label>
                  <textarea id="m-p1a-inst" rows={2} placeholder="Spice level, buffet hold, equipment..." value={instructionsText} onChange={(e) => setInstructionsText(e.target.value)} />
                </div>
                <div className="sm-field" style={{ marginTop: '0.5rem' }}>
                  <label htmlFor="m-p1a-photo">Dish Photograph Inspiration</label>
                  <input id="m-p1a-photo" type="file" accept="image/jpeg,image/png,image/webp" capture="environment" onChange={handlePhotoUpload} />
                  {dishImage && (
                    <p className="sm-meta" style={{ marginTop: '0.25rem' }}>
                      Attached: {dishImageName} ({dishImage.width}x{dishImage.height})
                    </p>
                  )}
                </div>
                <div className="sm-actions" style={{ marginTop: '0.75rem' }}>
                  <button type="submit" className="sm-btn sm-btn--primary" disabled={generating}>
                    {generating ? 'Generating Recipe...' : 'Generate Recipe'}
                  </button>
                  {generating && (
                    <button type="button" className="sm-btn" onClick={handleCancelGenerate}>
                      Cancel
                    </button>
                  )}
                </div>
              </form>
            )}

            {tab === 'draft' && (
              <>
                {!payload ? (
                  <section className="sm-card">
                    <p className="sm-meta">No active recipe draft loaded. Generate a recipe or pick one from History.</p>
                  </section>
                ) : (
                  <>
                    <section className="sm-card">
                      <p className="sm-eyebrow">
                        {activeDraft?.draftReference} · {activeDraft?.provider}/{activeDraft?.model}
                      </p>
                      <h2 style={{ margin: '0.25rem 0' }}>{payload.header.proposed_name}</h2>
                      <p className="sm-meta">{payload.header.description}</p>
                      <p className="sm-meta" style={{ marginTop: '0.35rem' }}>
                        <strong>Allergens (Requires Verification):</strong>{' '}
                        {payload.header.potential_allergens.join(', ') || 'None flagged'}
                      </p>
                    </section>

                    <section className="sm-card">
                      <h3 style={{ margin: '0 0 0.4rem' }}>Portion Scaling ({scaledDraft?.linear_factor ?? 1}x)</h3>
                      <div className="sm-field">
                        <label htmlFor="m-p1a-scale-srv">Servings: <strong>{targetServings}</strong> (base {payload.header.base_servings})</label>
                        <input id="m-p1a-scale-srv" type="range" min={1} max={300} value={targetServings} onChange={(e) => setTargetServings(Number(e.target.value))} />
                      </div>
                      <div className="sm-field" style={{ marginTop: '0.4rem' }}>
                        <label htmlFor="m-p1a-scale-sz">Serving Size ({payload.header.serving_uom})</label>
                        <input id="m-p1a-scale-sz" type="number" min={1} value={targetServingSize} onChange={(e) => setTargetServingSize(Math.max(1, Number(e.target.value) || 1))} />
                      </div>
                    </section>

                    <section className="sm-card">
                      <h3 style={{ margin: '0 0 0.4rem' }}>Ingredients ({payload.ingredients.length})</h3>
                      <ul className="sm-list">
                        {payload.ingredients.map((ing, idx) => {
                          const scaled = scaledDraft?.ingredients.find((s) => s.temp_ref === ing.temp_ref);
                          return (
                            <li key={ing.temp_ref || idx} className="sm-row" style={{ padding: '0.4rem 0', gap: '0.5rem' }}>
                              <div style={{ flex: 1 }}>
                                <strong>{ing.description}</strong>
                                <div className="sm-meta">
                                  Scaled: {scaled?.scaled_raw_qty ?? ing.quantity} {ing.recipe_uom} · {ing.material_mapping_status}
                                </div>
                              </div>
                              <input
                                type="number"
                                step="0.01"
                                aria-label={`Base qty ${ing.description}`}
                                style={{ width: '4.5rem' }}
                                value={ing.quantity}
                                onChange={(e) => {
                                  const qty = Math.max(0.001, Number(e.target.value) || 0.001);
                                  setPayload((prev) => {
                                    if (!prev) return prev;
                                    const next = [...prev.ingredients];
                                    next[idx] = { ...next[idx], quantity: qty, provenance: 'CHEF_EDITED' };
                                    return { ...prev, ingredients: next };
                                  });
                                }}
                              />
                            </li>
                          );
                        })}
                      </ul>
                    </section>

                    <section className="sm-card">
                      <h3 style={{ margin: '0 0 0.4rem' }}>Preparation Steps ({payload.instructions.length})</h3>
                      <div className="rs-step">
                        {payload.instructions.map((s) => (
                          <div key={s.step_number}>
                            <span className="rs-step-num">{s.step_number}</span>
                            <strong>{s.step_title}:</strong> {s.instruction}
                          </div>
                        ))}
                      </div>
                    </section>

                    <section className="sm-card">
                      <h3 style={{ margin: '0 0 0.4rem' }}>AI Conversational Refinement</h3>
                      <div style={{ display: 'flex', gap: '0.4rem' }}>
                        <input
                          placeholder="e.g. Make less spicy or replace chicken with paneer"
                          value={refineText}
                          onChange={(e) => setRefineText(e.target.value)}
                        />
                        <button type="button" className="sm-btn sm-btn--primary" disabled={refining || !refineText.trim()} onClick={handleRefine}>
                          {refining ? '...' : 'Refine'}
                        </button>
                      </div>
                      {pendingRefine && (
                        <div style={{ marginTop: '0.6rem', padding: '0.5rem', background: '#eef2ff', borderRadius: '0.5rem' }}>
                          <p style={{ margin: 0, fontSize: '0.82rem' }}>
                            <strong>Proposed ({pendingRefine.diffSummary.length} changes):</strong> {pendingRefine.summaryMessage}
                          </p>
                          <div className="sm-actions" style={{ marginTop: '0.4rem' }}>
                            <button
                              type="button"
                              className="sm-btn sm-btn--primary"
                              onClick={() => {
                                setPayload(pendingRefine.proposedPayload);
                                setPendingRefine(null);
                              }}
                            >
                              Accept
                            </button>
                            <button type="button" className="sm-btn" onClick={() => setPendingRefine(null)}>
                              Reject
                            </button>
                          </div>
                        </div>
                      )}
                    </section>

                    <div className="rs-sticky">
                      <button type="button" className="sm-btn sm-btn--primary" disabled={saving} onClick={handleSaveDraft}>
                        {saving ? 'Saving...' : 'Save Draft'}
                      </button>
                      <button type="button" className="sm-btn" onClick={handleOpenMaterialsTab}>
                        Map Materials
                      </button>
                      <button type="button" className="sm-btn" onClick={() => go('recipes/catalog')}>
                        Catalog
                      </button>
                    </div>
                  </>
                )}
              </>
            )}

            {tab === 'materials' && (
              <>
                {!activeDraft ? (
                  <section className="sm-card">
                    <p className="sm-meta">Open or generate a recipe draft first to inspect Phase 1B material mappings.</p>
                  </section>
                ) : matLoading ? (
                  <section className="sm-card">
                    <p className="sm-meta">Loading Material Intelligence workspace...</p>
                  </section>
                ) : (
                  <>
                    <section className="sm-card" data-testid="mobile-p1b-materials-header">
                      <p className="sm-eyebrow">PHASE 1B MATERIAL INTELLIGENCE</p>
                      <h2 style={{ margin: '0.25rem 0' }}>{matWs?.recipeName ?? activeDraft.recipeName}</h2>
                      <p className="sm-meta">
                        Status: <strong>{matWs?.readinessStatus ?? 'MATERIAL_MAPPING_PENDING'}</strong> · Confirmed{' '}
                        <strong>{matWs?.kpis.confirmedMappings ?? 0}/{matWs?.kpis.totalIngredients ?? 0}</strong> · Missing{' '}
                        <strong>{matWs?.kpis.missingMaterials ?? 0}</strong>
                      </p>
                      <div className="sm-actions" style={{ marginTop: '0.5rem' }}>
                        <button type="button" className="sm-btn sm-btn--primary" disabled={matMatching} onClick={handleMobileMatchAll}>
                          {matMatching ? 'Matching...' : 'Match All'}
                        </button>
                        <button type="button" className="sm-btn" disabled={matMatching} onClick={handleMobileBulkAccept}>
                          Accept Exact
                        </button>
                        <button type="button" className="sm-btn" onClick={() => loadMaterialWorkspace(activeDraft.id)}>
                          Refresh
                        </button>
                      </div>
                    </section>

                    {(matWs?.lines ?? []).map((line) => {
                      const isExpanded = activeMatRef === line.ingredientTempRef;
                      return (
                        <section key={line.ingredientTempRef} className="sm-card" data-testid={`mobile-p1b-line-${line.ingredientTempRef}`}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '0.5rem' }}>
                            <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                              {line.materialImageUrl ? (
                                <img
                                  src={line.materialImageUrl}
                                  alt={line.materialDescription ?? line.originalDescription}
                                  style={{ width: 34, height: 34, borderRadius: 6, objectFit: 'cover' }}
                                />
                              ) : (
                                <span
                                  style={{
                                    width: 34,
                                    height: 34,
                                    borderRadius: 6,
                                    background: '#f1f5f9',
                                    color: '#475569',
                                    display: 'inline-flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    fontSize: 10,
                                    fontWeight: 700,
                                  }}
                                >
                                  MAT
                                </span>
                              )}
                              <div>
                                <strong>{line.originalDescription}</strong>
                                <div className="sm-meta">
                                  Need: {line.requiredQuantity} {line.recipeUom}
                                  {line.convertedQuantity != null && line.baseUom
                                    ? ` -> ${line.convertedQuantity} ${line.baseUom}`
                                    : line.uomCompatible
                                      ? ''
                                      : ' (UOM Conflict)'}
                                </div>
                              </div>
                            </div>
                            <span className="sm-meta" style={{ fontWeight: 700 }}>{line.mappingStatus}</span>
                          </div>

                          <div className="sm-meta" style={{ marginTop: '0.35rem' }}>
                            Material: <code>{line.materialCode ?? 'UNMATCHED'}</code>{' '}
                            {line.materialDescription ?? ''} · Stock:{' '}
                            <strong>
                              {line.stockStatus === 'UNKNOWN' || line.availableStock == null
                                ? 'Unknown'
                                : `${line.availableStock} ${line.stockUom ?? ''}`}
                            </strong>
                          </div>

                          <div className="sm-actions" style={{ marginTop: '0.45rem' }}>
                            {line.materialId && line.mappingStatus !== 'CONFIRMED' && line.uomCompatible && line.specificationCompatible && (
                              <button
                                type="button"
                                className="sm-btn sm-btn--primary"
                                onClick={() => handleMobileLineConfirm(line, line.materialId ?? null, line.materialCode ?? null)}
                              >
                                Confirm
                              </button>
                            )}
                            <button
                              type="button"
                              className="sm-btn"
                              onClick={() => {
                                if (isExpanded) {
                                  setActiveMatRef(null);
                                } else {
                                  setActiveMatRef(line.ingredientTempRef);
                                  setMatSearchQ(line.originalDescription);
                                  setMatSearchItems([]);
                                  setMatSubs(null);
                                  setReqDraftDesc(line.requestDraft?.proposedDescription ?? line.originalDescription.toUpperCase());
                                  setReqDraftReason(line.requestDraft?.reason ?? line.missingReason ?? '');
                                }
                              }}
                            >
                              {isExpanded ? 'Hide Details' : 'Options / Search'}
                            </button>
                            {line.materialId && (
                              <button type="button" className="sm-btn" onClick={() => handleMobileLineReject(line)}>
                                Reject
                              </button>
                            )}
                          </div>

                          {isExpanded && (
                            <div style={{ marginTop: '0.6rem', paddingTop: '0.6rem', borderTop: '1px solid #e2e8f0' }}>
                              {line.candidates.length > 0 && (
                                <div style={{ marginBottom: '0.5rem' }}>
                                  <strong style={{ fontSize: '0.8rem' }}>Candidates</strong>
                                  {line.candidates.map((c) => (
                                    <div key={c.materialId + c.materialCode} className="sm-row" style={{ padding: '0.3rem 0' }}>
                                      <div>
                                        <code>{c.materialCode}</code> {c.description}
                                        <div className="sm-meta">
                                          {c.matchMethod} · {(c.finalScore * 100).toFixed(0)}% · {c.baseUom}
                                        </div>
                                      </div>
                                      <button
                                        type="button"
                                        className="sm-btn"
                                        disabled={!c.materialActive || !c.plantEligible}
                                        onClick={() => handleMobileLineConfirm(line, c.materialId, c.materialCode)}
                                      >
                                        Select
                                      </button>
                                    </div>
                                  ))}
                                </div>
                              )}

                              <div className="sm-field">
                                <label htmlFor={`m-search-${line.ingredientTempRef}`}>Search Material Master</label>
                                <div style={{ display: 'flex', gap: '0.4rem' }}>
                                  <input
                                    id={`m-search-${line.ingredientTempRef}`}
                                    value={matSearchQ}
                                    onChange={(e) => setMatSearchQ(e.target.value)}
                                    placeholder="Code or description"
                                  />
                                  <button
                                    type="button"
                                    className="sm-btn"
                                    onClick={async () => {
                                      const items = await searchMaterialsForMapping(matSearchQ, matWs?.propertyId ?? null, 10);
                                      setMatSearchItems(items);
                                    }}
                                  >
                                    Find
                                  </button>
                                </div>
                              </div>
                              {matSearchItems.map((it) => (
                                <div key={it.materialId} className="sm-row" style={{ padding: '0.25rem 0' }}>
                                  <span><code>{it.materialCode}</code> {it.description}</span>
                                  <button
                                    type="button"
                                    className="sm-btn"
                                    disabled={!it.plantEligible}
                                    onClick={() => handleMobileLineConfirm(line, it.materialId, it.materialCode)}
                                  >
                                    Assign
                                  </button>
                                </div>
                              ))}

                              <div className="sm-actions" style={{ marginTop: '0.5rem' }}>
                                <button
                                  type="button"
                                  className="sm-btn"
                                  onClick={async () => {
                                    const res = await suggestIngredientSubstitutions(activeDraft.id, line.ingredientTempRef);
                                    setMatSubs(res);
                                  }}
                                >
                                  Review Substitutions
                                </button>
                              </div>
                              {matSubs && matSubs.temp_ref === line.ingredientTempRef && (
                                <div style={{ marginTop: '0.4rem' }}>
                                  {matSubs.suggestions.map((s) => (
                                    <div key={s.proposed_ingredient} className="sm-row" style={{ padding: '0.25rem 0' }}>
                                      <div>
                                        <strong>{s.proposed_ingredient}</strong> ({s.substitution_kind})
                                        <div className="sm-meta">{s.taste_impact}</div>
                                      </div>
                                      <button
                                        type="button"
                                        className="sm-btn"
                                        onClick={async () => {
                                          await applyIngredientSubstitution(activeDraft.id, line.ingredientTempRef, {
                                            expectedMappingRevision: line.mappingRevision,
                                            proposedIngredient: s.proposed_ingredient,
                                            proposedMaterialId: s.proposed_material_id ?? null,
                                            proposedMaterialCode: s.proposed_material_code ?? null,
                                            replacementRatio: s.suggested_replacement_ratio ?? 1,
                                          });
                                          await loadMaterialWorkspace(activeDraft.id);
                                          setStatusMsg(`Applied substitution ${s.proposed_ingredient} to draft.`);
                                        }}
                                      >
                                        Apply
                                      </button>
                                    </div>
                                  ))}
                                </div>
                              )}

                              <div style={{ marginTop: '0.5rem' }}>
                                <strong style={{ fontSize: '0.8rem' }}>Prepare Missing Material Request (Phase 1C)</strong>
                                <div className="sm-field" style={{ marginTop: '0.25rem' }}>
                                  <input
                                    placeholder="Proposed material description"
                                    value={reqDraftDesc}
                                    onChange={(e) => setReqDraftDesc(e.target.value)}
                                  />
                                </div>
                                <div className="sm-field" style={{ marginTop: '0.25rem' }}>
                                  <input
                                    placeholder="Reason / specification"
                                    value={reqDraftReason}
                                    onChange={(e) => setReqDraftReason(e.target.value)}
                                  />
                                </div>
                                <button
                                  type="button"
                                  className="sm-btn"
                                  style={{ marginTop: '0.3rem' }}
                                  disabled={!reqDraftDesc.trim()}
                                  onClick={async () => {
                                    await prepareMaterialRequestDraft(activeDraft.id, line.ingredientTempRef, {
                                      expectedMappingRevision: line.mappingRevision,
                                      requestType: line.recommendedRequestType ?? 'NEW_MATERIAL',
                                      proposedDescription: reqDraftDesc.trim(),
                                      proposedBaseUom: line.recipeUom,
                                      reason: reqDraftReason.trim() || 'Marked missing on mobile',
                                    });
                                    await loadMaterialWorkspace(activeDraft.id);
                                    setStatusMsg(`Prepared Phase 1C request draft for ${line.originalDescription}.`);
                                  }}
                                >
                                  Mark Missing &amp; Save Draft
                                </button>
                              </div>
                            </div>
                          )}
                        </section>
                      );
                    })}
                  </>
                )}
              </>
            )}

            {tab === 'history' && (
              <section className="sm-card">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                  <h2 style={{ margin: 0 }}>Saved AI Drafts</h2>
                  <button type="button" className="sm-btn" onClick={() => draftsLoad.reload()}>Refresh</button>
                </div>
                {(draftsLoad.data ?? []).length === 0 ? (
                  <p className="sm-meta">No AI recipe drafts saved yet.</p>
                ) : (
                  <ul className="sm-list">
                    {(draftsLoad.data ?? []).map((d) => (
                      <li key={d.id} className="sm-row" style={{ padding: '0.5rem 0' }}>
                        <div>
                          <strong>{d.recipeName}</strong>
                          <div className="sm-meta">
                            {d.draftReference} · Rev {d.revisionNumber} · {d.baseServings} srv · {d.status}
                          </div>
                        </div>
                        <button type="button" className="sm-btn" onClick={() => handleOpenDraft(d.id)}>
                          Open
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            )}

            {tab === 'demo' && (
              <>
                <section className="sm-section">
                  <h2>Interactive Portion Scaling (Demo)</h2>
                  <div className="sm-card">
                    <p style={{ fontWeight: 600, margin: 0 }}>{DEMO_SALMON_RECIPE.title}</p>
                    <div className="sm-field" style={{ marginTop: '0.6rem' }}>
                      <label htmlFor="m-cs2-servings">Portions: <strong>{demoServings}</strong></label>
                      <input
                        id="m-cs2-servings"
                        type="range"
                        min={1}
                        max={250}
                        value={demoServings}
                        onChange={(e) => setDemoServings(Number(e.target.value))}
                      />
                    </div>
                    <p className="sm-meta" style={{ marginTop: '0.4rem' }}>
                      Total Demo Cost: <strong>AED {demoScaling.scaledTotalCostAed.toFixed(2)}</strong> (AED {demoScaling.scaledCostPerServingAed.toFixed(2)} / serving)
                    </p>
                    <ul className="sm-list" style={{ marginTop: '0.5rem' }}>
                      {demoScaling.items.slice(0, 4).map((it) => (
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
              </>
            )}

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
