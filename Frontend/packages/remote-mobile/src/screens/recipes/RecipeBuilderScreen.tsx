import React, { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  createRecipe,
  getRecipe,
  submitRecipe,
  updateRecipe,
  type SilaRecipeDetail,
  type SilaRecipeWrite,
} from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';
import { searchIngredients, type SilaIngredientOption } from '../../../../remote-buyer/src/api/silaMe/silaRecipeToolsApi';
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
import { resolveStudioKind, StudioCover } from '../../../../remote-buyer/src/components/silaMe/recipes/recipeStudioVisuals';
import ScreenHeader from '../../components/ScreenHeader';
import StatusBadge from '../../components/StatusBadge';
import { ErrorNotice, Loading } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useGo } from '../../navigation';
import { errorText } from '../../useLoad';

/** Mobile Smart Recipe Builder — same APIs/costing as Cloud, touch-friendly controls. */
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
  const [scaleTo, setScaleTo] = useState('1');

  useEffect(() => {
    let active = true;
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        if (!recipeId) {
          if (active) {
            setRecipe(null);
            setHeader(headerFrom(null));
            setLines([]);
            setBaseline(snapshotOf([], '1'));
            setScaleTo('1');
          }
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
    if (!pickerOpen) return undefined;
    const term = search.trim();
    if (term.length < 2) {
      setResults([]);
      return undefined;
    }
    let active = true;
    const timer = window.setTimeout(() => {
      searchIngredients({ search: term, limit: 20 })
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
  }, [pickerOpen, search]);

  const costs = useMemo(() => lines.map((line) => lineCost(line, '')), [lines]);
  const totalCost = totalCostAt(lines, '');
  const dirty = !snapshotsEqual(baseline, snapshotOf(lines, header.servingQty));
  const currency = header.currency || recipe?.currency || '';

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
      window.alert('Enter a recipe name on Cloud first for new recipes, or open an existing recipe.');
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
      <div className="sm-screen sm-recipe-builder sm-rstudio">
        {loading && <Loading text="Loading recipe…" />}
        {error && <ErrorNotice message={error} />}
        {!loading && !error && (
          <>
            <div className="sm-row">
              <StatusBadge status={recipe?.status || 'DRAFT'} />
              {dirty && <span className="sm-meta">Unsaved changes</span>}
            </div>
            {recipe && (recipe.status === 'APPROVED' || recipe.status === 'REJECTED') && (
              <p className="sm-meta">Saving creates draft version {(recipe.version ?? 0) + 1}. Active approved version stays until approval.</p>
            )}

            <section className="sm-section">
              <h2>Yield</h2>
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
                <button type="button" className="sm-btn" onClick={() => { setReplaceKey(null); setPickerOpen(true); }}>Add</button>
              </div>
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
              <h2>Cost summary</h2>
              <p className="sm-meta">
                Total {totalCost == null ? 'COST PENDING' : formatCost(totalCost, currency)}
                {' · '}
                /portion {totalCost == null || !(toNumber(header.servingQty) > 0)
                  ? 'COST PENDING'
                  : formatCost(totalCost / toNumber(header.servingQty), currency)}
                {' · '}
                {formatQty(toNumber(header.servingQty))} {header.servingUom}
              </p>
            </section>

            <div className="sm-row sm-recipe-builder__footer">
              <button type="button" className="sm-btn sm-btn--secondary" disabled={undoStack.length === 0 || saving} onClick={undo}>Undo</button>
              <button type="button" className="sm-btn sm-btn--secondary" disabled={!dirty || saving} onClick={() => { setLines(baseline.lines); setHeader((c) => ({ ...c, servingQty: baseline.servingQty })); setUndoStack([]); }}>Cancel changes</button>
              <button type="button" className="sm-btn sm-btn--secondary" disabled={saving} onClick={() => save(false)}>{saving ? 'Saving…' : 'Save draft'}</button>
              <button type="button" className="sm-btn" disabled={saving} onClick={() => save(true)}>Submit</button>
            </div>
          </>
        )}
      </div>

      {pickerOpen && (
        <div className="sm-sheet" role="dialog" aria-label="Material picker">
          <div className="sm-sheet__panel">
            <div className="sm-row">
              <strong>{replaceKey ? 'Replace ingredient' : 'Add ingredient'}</strong>
              <button type="button" className="sm-btn sm-btn--secondary" onClick={() => { setPickerOpen(false); setReplaceKey(null); }}>Close</button>
            </div>
            <label className="sm-field">
              Search Material Master
              <input className="sm-input" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Code or name" />
            </label>
            <ul className="sm-rstudio-shop">
              {results.map((item) => (
                <li key={item.id}>
                  <button type="button" className="sm-rstudio-shop__item" onClick={() => pickMaterial(item)}>
                    <StudioCover compact kind={resolveStudioKind(item.materialGroup, item.category, item.description)} title={item.description} />
                    <span>
                      <strong>{item.description}</strong>
                      <span className="sm-meta">{item.materialCode} · {item.baseUom}</span>
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
