import React, { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  createRecipe,
  getRecipe,
  getRecipes,
  submitRecipe,
  updateRecipe,
  type SilaRecipeDetail,
  type SilaRecipeListItem,
  type SilaRecipeWrite,
} from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';
import {
  getRecipeMasters,
  searchIngredients,
  type SilaIngredientOption,
  type SilaRecipeMaster,
} from '../../../../remote-buyer/src/api/silaMe/silaRecipeToolsApi';
import {
  headerFrom,
  lineCost,
  linesFrom,
  toNumber,
  totalCostAt,
  type HeaderValues,
  type IngredientLine,
} from '../../../../remote-buyer/src/components/silaMe/recipes/recipeEditorModel';
import {
  duplicateLine,
  materialLine,
  pushUndo,
  rememberMaterial,
  replaceMaterialOnLine,
  scaleLines,
  snapshotOf,
  snapshotsEqual,
  type BuilderSnapshot,
} from '../../../../remote-buyer/src/components/silaMe/recipes/recipeBuilderState';
import { formatCost, formatQty, materialUoms } from '../../../../remote-buyer/src/components/silaMe/recipes/recipeFormat';
import {
  loadTargetFoodCostPercent,
  recommendMaterials,
  saveTargetFoodCostPercent,
  suggestClassification,
  suggestRecipeNames,
  suggestSellingPrice,
} from '../../../../remote-buyer/src/components/silaMe/recipes/recipeStudioSuggestions';
import { resolveStudioKind, StudioCover } from '../../../../remote-buyer/src/components/silaMe/recipes/recipeStudioVisuals';
import ScreenHeader from '../../components/ScreenHeader';
import StatusBadge from '../../components/StatusBadge';
import { ErrorNotice, Loading } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useGo } from '../../navigation';
import { errorText } from '../../useLoad';

type CatalogTab = 'recommended' | 'all';

/** Mobile/tablet ingredient-first Recipe Studio — same APIs/costing as Cloud. */
const RecipeBuilderScreen: React.FC = () => {
  const { recipeId = '' } = useParams();
  const go = useGo();
  const { can } = useAccess();
  const [recipe, setRecipe] = useState<SilaRecipeDetail | null>(null);
  const [header, setHeader] = useState<HeaderValues>(() => headerFrom(null));
  const [lines, setLines] = useState<IngredientLine[]>([]);
  const [baseline, setBaseline] = useState<BuilderSnapshot>(() => snapshotOf([], '1'));
  const [undoStack, setUndoStack] = useState<BuilderSnapshot[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [replaceKey, setReplaceKey] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [results, setResults] = useState<SilaIngredientOption[]>([]);
  const [catalogPool, setCatalogPool] = useState<SilaIngredientOption[]>([]);
  const [approvedRecipes, setApprovedRecipes] = useState<SilaRecipeListItem[]>([]);
  const [families, setFamilies] = useState<SilaRecipeMaster[]>([]);
  const [categories, setCategories] = useState<SilaRecipeMaster[]>([]);
  const [catalogTab, setCatalogTab] = useState<CatalogTab>('recommended');
  const [scaleTo, setScaleTo] = useState('1');
  const [nameConfirmed, setNameConfirmed] = useState(false);
  const [classConfirmed, setClassConfirmed] = useState(false);
  const [targetFoodCostPercent, setTargetFoodCostPercent] = useState(() => loadTargetFoodCostPercent());

  useEffect(() => {
    let active = true;
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const [browse, approved, loadedFamilies, loadedCategories] = await Promise.all([
          searchIngredients({ search: '', limit: 40 }),
          getRecipes({ active: true, limit: 100 }).catch(() => ({ items: [] as SilaRecipeListItem[] })),
          getRecipeMasters('families').catch(() => [] as SilaRecipeMaster[]),
          getRecipeMasters('categories').catch(() => [] as SilaRecipeMaster[]),
        ]);
        if (!active) return;
        setCatalogPool(browse.items);
        setApprovedRecipes(approved.items);
        setFamilies(loadedFamilies);
        setCategories(loadedCategories);

        if (!recipeId) {
          setRecipe(null);
          setHeader(headerFrom(null));
          setLines([]);
          setBaseline(snapshotOf([], '1'));
          setScaleTo('1');
          setNameConfirmed(false);
          setClassConfirmed(false);
          return;
        }
        const detail = await getRecipe(recipeId);
        if (!active) return;
        const loaded = linesFrom(detail, []);
        setRecipe(detail);
        setHeader(headerFrom(detail));
        setLines(loaded);
        const snap = snapshotOf(loaded, String(detail.servingQty));
        setBaseline(snap);
        setScaleTo(String(detail.servingQty));
        setUndoStack([]);
        setNameConfirmed(Boolean(detail.name?.trim()));
        setClassConfirmed(Boolean(detail.familyId || detail.categoryId));
      } catch (caught: unknown) {
        if (active) setError(errorText(caught, 'Could not load the recipe.'));
      } finally {
        if (active) setLoading(false);
      }
    };
    load();
    return () => {
      active = false;
    };
  }, [recipeId]);

  useEffect(() => {
    if (!pickerOpen || catalogTab !== 'all') return undefined;
    const term = search.trim();
    let active = true;
    const timer = window.setTimeout(() => {
      searchIngredients({ search: term, limit: 24 })
        .then((page) => {
          if (active) setResults(page.items);
        })
        .catch(() => {
          if (active) setResults([]);
        });
    }, 250);
    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [pickerOpen, search, catalogTab]);

  const costs = useMemo(() => lines.map((line) => lineCost(line, '')), [lines]);
  const totalCost = totalCostAt(lines, '');
  const dirty = !snapshotsEqual(baseline, snapshotOf(lines, header.servingQty));
  const currency = header.currency || recipe?.currency || '';
  const pricing = useMemo(
    () => suggestSellingPrice(totalCost, toNumber(header.servingQty), targetFoodCostPercent, costs),
    [totalCost, header.servingQty, targetFoodCostPercent, costs],
  );
  const nameSuggestions = useMemo(() => (nameConfirmed ? [] : suggestRecipeNames(lines)), [lines, nameConfirmed]);
  const classification = useMemo(() => suggestClassification(lines, families, categories), [lines, families, categories]);
  const materialRecs = useMemo(
    () => recommendMaterials(lines, approvedRecipes, catalogPool, 20),
    [lines, approvedRecipes, catalogPool],
  );
  const sheetItems = catalogTab === 'recommended' ? materialRecs.items : results;

  if (!can.manageRecipe) {
    return (
      <>
        <ScreenHeader title="Recipe builder" eyebrow="Recipes" back backTo="recipes" />
        <div className="sm-screen"><ErrorNotice message="You do not have permission to edit recipes." /></div>
      </>
    );
  }

  const commit = (next: IngredientLine[]) => {
    setUndoStack((stack) => pushUndo(stack, snapshotOf(lines, header.servingQty)));
    setLines(next);
  };

  const undo = () => {
    setUndoStack((stack) => {
      if (stack.length === 0) return stack;
      const previous = stack[stack.length - 1];
      setLines(previous.lines);
      setHeader((current) => ({ ...current, servingQty: previous.servingQty }));
      setScaleTo(previous.servingQty);
      return stack.slice(0, -1);
    });
  };

  const pickMaterial = (option: SilaIngredientOption) => {
    rememberMaterial(option.id);
    if (replaceKey) {
      commit(lines.map((line) => (line.key === replaceKey ? replaceMaterialOnLine(line, option, line.quantity, option.baseUom) : line)));
      setReplaceKey(null);
    } else {
      commit([...lines, materialLine(option)]);
    }
    setPickerOpen(false);
    setSearch('');
  };

  const buildRequest = (): SilaRecipeWrite => ({
    name: header.name.trim() || recipe?.name || 'Recipe',
    description: header.description.trim() || null,
    familyId: header.familyId || recipe?.familyId || null,
    categoryId: header.categoryId || recipe?.categoryId || null,
    itemMode: header.itemMode,
    servingQty: toNumber(header.servingQty),
    servingUom: header.servingUom.trim() || 'EA',
    sellingUom: header.sellingUom.trim() || 'EA',
    posCode: header.posCode.trim() || null,
    posItem: header.posItem.trim() || null,
    currency: header.currency.trim() || null,
    ingredients: lines.map((line) => ({
      materialId: line.material?.id ?? null,
      subRecipeId: line.subRecipe?.id ?? null,
      quantity: toNumber(line.quantity),
      uom: line.uom || null,
    })),
    outletPrices: (recipe?.outletPrices ?? []).map((price) => ({
      outletLocationId: price.outletLocationId,
      menuPrice: price.menuPrice,
    })),
  });

  const save = async (submit: boolean) => {
    if (!header.name.trim() && !recipe?.name) {
      window.alert('Enter a recipe name before saving.');
      return;
    }
    if (lines.length === 0) {
      window.alert('Add at least one ingredient.');
      return;
    }
    if (lines.some((line) => !(toNumber(line.quantity) > 0))) {
      window.alert('Every ingredient needs a quantity greater than zero.');
      return;
    }
    setSaving(true);
    try {
      let id = recipe?.id ?? null;
      if (recipe) await updateRecipe(recipe.id, buildRequest());
      else id = await createRecipe(buildRequest());
      if (submit && id) await submitRecipe(id);
      if (id) go(`recipes/${id}`);
    } catch (caught: unknown) {
      window.alert(errorText(caught, 'Could not save the recipe.'));
    } finally {
      setSaving(false);
    }
  };

  const move = (index: number, delta: number) => {
    const nextIndex = index + delta;
    if (nextIndex < 0 || nextIndex >= lines.length) return;
    const next = [...lines];
    const [row] = next.splice(index, 1);
    next.splice(nextIndex, 0, row);
    commit(next);
  };

  return (
    <>
      <ScreenHeader
        title={recipe ? `Edit ${recipe.recipeCode}` : 'New recipe'}
        eyebrow="Recipe Studio"
        back
        backTo={recipeId ? `recipes/${recipeId}` : 'recipes'}
      />
      <div className="sm-screen sm-recipe-builder sm-rstudio sm-rstudio-builder">
        {loading && <Loading text="Loading recipe…" />}
        {error && <ErrorNotice message={error} />}
        {!loading && !error && (
          <div className="sm-rstudio-workspace">
            <div className="sm-rstudio-main">
              <div className="sm-row">
                <StatusBadge status={recipe?.status || 'DRAFT'} />
                {!recipe && <span className="sm-meta">Local draft</span>}
                {dirty && <span className="sm-meta">Unsaved changes</span>}
              </div>
              {recipe && (recipe.status === 'APPROVED' || recipe.status === 'REJECTED') && (
                <p className="sm-meta">Saving creates draft version {(recipe.version ?? 0) + 1}. Active approved version stays until approval.</p>
              )}

              <section className="sm-section">
                <h2>My Recipe</h2>
                <label className="sm-field">
                  Recipe name
                  <input
                    className="sm-input"
                    value={header.name}
                    placeholder="Optional while building — required on save"
                    onChange={(event) => {
                      setNameConfirmed(true);
                      setHeader((current) => ({ ...current, name: event.target.value }));
                    }}
                  />
                </label>
                {nameSuggestions.length > 0 && (
                  <div className="sm-chip-row">
                    {nameSuggestions.map((item) => (
                      <button
                        key={item.name}
                        type="button"
                        className="sm-chip"
                        onClick={() => {
                          setNameConfirmed(true);
                          setHeader((current) => ({ ...current, name: item.name }));
                        }}
                      >
                        {item.name}
                      </button>
                    ))}
                  </div>
                )}
                <label className="sm-field">
                  Portions
                  <input
                    className="sm-input"
                    type="number"
                    min="0"
                    step="any"
                    value={header.servingQty}
                    onChange={(event) => {
                      setUndoStack((stack) => pushUndo(stack, snapshotOf(lines, header.servingQty)));
                      setHeader((current) => ({ ...current, servingQty: event.target.value }));
                      setScaleTo(event.target.value);
                    }}
                  />
                </label>
                <label className="sm-field">
                  Scale ingredients to
                  <div className="sm-row">
                    <input className="sm-input" type="number" min="0" step="any" value={scaleTo} onChange={(event) => setScaleTo(event.target.value)} />
                    <button
                      type="button"
                      className="sm-btn sm-btn--secondary"
                      onClick={() => {
                        const from = toNumber(header.servingQty);
                        const to = toNumber(scaleTo);
                        if (!(from > 0) || !(to > 0)) return;
                        setUndoStack((stack) => pushUndo(stack, snapshotOf(lines, header.servingQty)));
                        setLines(scaleLines(lines, from, to));
                        setHeader((current) => ({ ...current, servingQty: String(to) }));
                      }}
                    >
                      Apply
                    </button>
                  </div>
                </label>
              </section>

              <section className="sm-section">
                <div className="sm-row">
                  <h2>Ingredients ({lines.length})</h2>
                  <button
                    type="button"
                    className="sm-btn"
                    onClick={() => {
                      setReplaceKey(null);
                      setCatalogTab('recommended');
                      setPickerOpen(true);
                    }}
                  >
                    Add materials
                  </button>
                </div>
                {lines.length === 0 && (
                  <p className="sm-meta">Start with ingredients — drag is desktop-only; on mobile use Add materials.</p>
                )}
                <ul className="sm-rstudio-ingredients">
                  {lines.map((line, index) => {
                    const cost = costs[index];
                    const name = line.material?.description ?? line.subRecipe?.name ?? '';
                    const code = line.material?.materialCode ?? line.subRecipe?.recipeCode ?? '';
                    return (
                      <li key={line.key} className="sm-rstudio-ingredient sm-card">
                        <StudioCover compact kind={resolveStudioKind(name, code)} title={name || code} />
                        <div>
                          <div className="sm-row">
                            <strong>{name || code}</strong>
                            <span className="sm-meta">{cost.cost == null ? 'COST PENDING' : formatCost(cost.cost, currency)}</span>
                          </div>
                          <p className="sm-meta">{code}</p>
                          <div className="sm-row">
                            <input
                              className="sm-input"
                              type="number"
                              min="0"
                              step="any"
                              value={line.quantity}
                              aria-label="Quantity"
                              onChange={(event) => commit(lines.map((row) => (row.key === line.key ? { ...row, quantity: event.target.value } : row)))}
                            />
                            {line.material ? (
                              <select
                                className="sm-input"
                                value={line.uom}
                                aria-label="UOM"
                                onChange={(event) => commit(lines.map((row) => (row.key === line.key ? { ...row, uom: event.target.value } : row)))}
                              >
                                {Array.from(new Set([line.uom.toUpperCase(), ...materialUoms(line.material)])).map((unit) => (
                                  <option key={unit} value={unit}>{unit}</option>
                                ))}
                              </select>
                            ) : (
                              <span className="sm-meta">{line.uom}</span>
                            )}
                          </div>
                          <div className="sm-row sm-recipe-builder__actions">
                            <button type="button" className="sm-btn sm-btn--secondary" onClick={() => move(index, -1)} disabled={index === 0}>Up</button>
                            <button type="button" className="sm-btn sm-btn--secondary" onClick={() => move(index, 1)} disabled={index === lines.length - 1}>Down</button>
                            {line.material && (
                              <button type="button" className="sm-btn sm-btn--secondary" onClick={() => { setReplaceKey(line.key); setPickerOpen(true); }}>Replace</button>
                            )}
                            <button
                              type="button"
                              className="sm-btn sm-btn--secondary"
                              onClick={() => {
                                const copy = duplicateLine(line);
                                const next = [...lines];
                                next.splice(index + 1, 0, copy);
                                commit(next);
                              }}
                            >
                              Dup
                            </button>
                            <button type="button" className="sm-btn sm-btn--secondary" onClick={() => commit(lines.filter((row) => row.key !== line.key))}>Remove</button>
                          </div>
                        </div>
                      </li>
                    );
                  })}
                </ul>
              </section>

              <section className="sm-section">
                <h2>Live costing</h2>
                <p className="sm-meta">
                  Total {pricing.incomplete || totalCost == null ? 'COST PENDING' : formatCost(totalCost, currency)}
                  {' · '}
                  /portion {pricing.incomplete || pricing.costPerPortion == null ? 'COST PENDING' : formatCost(pricing.costPerPortion, currency)}
                  {' · '}
                  Suggest sell {pricing.incomplete || pricing.suggestedSellingPrice == null ? 'COST PENDING' : formatCost(pricing.suggestedSellingPrice, currency)}
                  {' · '}
                  {formatQty(toNumber(header.servingQty))} {header.servingUom}
                </p>
                <label className="sm-field">
                  Target food cost %
                  <input
                    className="sm-input"
                    type="number"
                    min={1}
                    max={99}
                    value={targetFoodCostPercent}
                    onChange={(event) => {
                      const next = Number(event.target.value);
                      const safe = Number.isFinite(next) && next > 0 && next < 100 ? next : 30;
                      setTargetFoodCostPercent(safe);
                      saveTargetFoodCostPercent(safe);
                    }}
                  />
                </label>
              </section>

              {classification && !classConfirmed && (
                <section className="sm-section">
                  <h2>Suggested classification</h2>
                  <p className="sm-meta">{classification.reason}</p>
                  <button
                    type="button"
                    className="sm-btn sm-btn--secondary"
                    onClick={() => {
                      setClassConfirmed(true);
                      setHeader((current) => ({
                        ...current,
                        familyId: classification.familyId || current.familyId,
                        categoryId: classification.categoryId || current.categoryId,
                      }));
                    }}
                  >
                    Apply {classification.familyName} / {classification.categoryName}
                  </button>
                </section>
              )}

              <div className="sm-row sm-recipe-builder__footer">
                <button type="button" className="sm-btn sm-btn--secondary" disabled={undoStack.length === 0 || saving} onClick={undo}>Undo</button>
                <button type="button" className="sm-btn sm-btn--secondary" disabled={!dirty || saving} onClick={() => { setLines(baseline.lines); setHeader((c) => ({ ...c, servingQty: baseline.servingQty })); setUndoStack([]); }}>Cancel changes</button>
                <button type="button" className="sm-btn sm-btn--secondary" disabled={saving} onClick={() => save(false)}>{saving ? 'Saving…' : 'Save draft'}</button>
                <button type="button" className="sm-btn" disabled={saving} onClick={() => save(true)}>Submit</button>
              </div>
            </div>

            <aside className="sm-rstudio-aside" aria-label="Recommended materials">
              <h2>Recommended</h2>
              <p className="sm-meta">{materialRecs.reason}</p>
              <ul className="sm-rstudio-shop">
                {materialRecs.items.slice(0, 8).map((item) => (
                  <li key={item.id}>
                    <button type="button" className="sm-rstudio-shop__item" onClick={() => pickMaterial(item)}>
                      <StudioCover compact kind={resolveStudioKind(item.materialGroup, item.category, item.description)} title={item.description} />
                      <span>
                        <strong>{item.description}</strong>
                        <span className="sm-meta">{item.materialCode} · {item.baseUom} · {item.materialGroup || 'Not Classified'}</span>
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
              <button type="button" className="sm-btn" onClick={() => { setCatalogTab('all'); setPickerOpen(true); }}>Browse all materials</button>
            </aside>
          </div>
        )}
      </div>

      {pickerOpen && (
        <div className="sm-sheet" role="dialog" aria-label="Material catalog">
          <div className="sm-sheet__panel">
            <div className="sm-row">
              <strong>{replaceKey ? 'Replace ingredient' : 'Material Catalog'}</strong>
              <button type="button" className="sm-btn sm-btn--secondary" onClick={() => { setPickerOpen(false); setReplaceKey(null); }}>Close</button>
            </div>
            <div className="sm-chip-row">
              <button type="button" className={`sm-chip${catalogTab === 'recommended' ? ' is-active' : ''}`} onClick={() => setCatalogTab('recommended')}>Recommended</button>
              <button type="button" className={`sm-chip${catalogTab === 'all' ? ' is-active' : ''}`} onClick={() => setCatalogTab('all')}>All Materials</button>
            </div>
            {catalogTab === 'recommended' && <p className="sm-meta">{materialRecs.reason}</p>}
            {catalogTab === 'all' && (
              <label className="sm-field">
                Search Material Master
                <input className="sm-input" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Code or name" />
              </label>
            )}
            <ul className="sm-rstudio-shop">
              {sheetItems.map((item) => (
                <li key={item.id}>
                  <button type="button" className="sm-rstudio-shop__item" onClick={() => pickMaterial(item)}>
                    <StudioCover compact kind={resolveStudioKind(item.materialGroup, item.category, item.description)} title={item.description} />
                    <span>
                      <strong>{item.description}</strong>
                      <span className="sm-meta">
                        {item.materialCode} · {item.baseUom} · {item.materialGroup || 'Not Classified'} / {item.category || 'Not Classified'}
                      </span>
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        </div>
      )}
    </>
  );
};

export default RecipeBuilderScreen;
