import React, { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  approveRecipe,
  canStartRevision,
  createRecipe,
  deleteRecipeDraft,
  getRecipe,
  rejectRecipe,
  submitRecipe,
  updateRecipe,
  writeFromDetail,
  type SilaRecipeDetail,
} from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';
import ScreenHeader from '../../components/ScreenHeader';
import StatusBadge from '../../components/StatusBadge';
import { Empty, ErrorNotice, Loading } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useMyLocation } from '../../location';
import { useGo } from '../../navigation';
import { errorText, useLoad } from '../../useLoad';
import RecipePicture from './RecipePicture';
import {
  loadIngredientAvailability,
  rememberRecipe,
  scaleIngredients,
  toggleFavoriteRecipe,
  type IngredientAvailability,
} from './recipeStudioHelpers';
import './recipeStudio.css';

type TabId = 'ingredients' | 'prep' | 'stock' | 'scale';

/** Chef-focused recipe detail with scaling, stock check, draft and approval actions. */
const RecipeDetailScreen: React.FC<{ approvalMode?: boolean }> = ({ approvalMode = false }) => {
  const { recipeId = '' } = useParams();
  const go = useGo();
  const { can } = useAccess();
  const { location } = useMyLocation();
  const recipe = useLoad(() => getRecipe(recipeId), recipeId || null);
  const [tab, setTab] = useState<TabId>('ingredients');
  const [servings, setServings] = useState('');
  const [busy, setBusy] = useState(false);
  const [stock, setStock] = useState<{ lines: IngredientAvailability[]; possibleServings: number } | null>(null);
  const [stockError, setStockError] = useState<string | null>(null);
  const [stockLoading, setStockLoading] = useState(false);
  const [comment, setComment] = useState('');
  const [favorited, setFavorited] = useState(false);

  useEffect(() => {
    if (recipeId) rememberRecipe(recipeId);
  }, [recipeId]);

  useEffect(() => {
    if (recipe.data?.servingQty) setServings(String(recipe.data.servingQty));
  }, [recipe.data?.servingQty]);

  const servingNumber = Number(servings) || recipe.data?.servingQty || 1;
  const scaled = useMemo(
    () => (recipe.data ? scaleIngredients(recipe.data, servingNumber) : []),
    [recipe.data, servingNumber],
  );

  const loadStock = async (detail: SilaRecipeDetail) => {
    if (!location?.id) {
      setStockError('Choose a working outlet in Settings to check stock.');
      return;
    }
    setStockLoading(true);
    setStockError(null);
    try {
      const result = await loadIngredientAvailability(detail, location.id, servingNumber);
      setStock(result);
    } catch (caught: unknown) {
      setStockError(errorText(caught, 'Could not check ingredient stock.'));
      setStock(null);
    } finally {
      setStockLoading(false);
    }
  };

  useEffect(() => {
    if (tab === 'stock' && recipe.data) void loadStock(recipe.data);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- reload when tab/servings/location change
  }, [tab, recipe.data?.id, servingNumber, location?.id]);

  const status = (recipe.data?.status || '').toUpperCase();
  const isDraft = status === 'DRAFT' || status === 'REJECTED';
  const isPending = status === 'PENDING_APPROVAL';
  const complex =
    recipe.data?.itemMode === 'BATCH' ||
    (recipe.data?.recipeFormat || '').toUpperCase().includes('BATCH') ||
    (recipe.data?.recipeFormat || '').toUpperCase().includes('SUB');

  const run = async (action: () => Promise<void>, okMessage: string) => {
    setBusy(true);
    try {
      await action();
      window.alert(okMessage);
      recipe.reload();
    } catch (caught: unknown) {
      window.alert(errorText(caught, 'Action failed.'));
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <ScreenHeader
        title={recipe.data?.name ?? 'Recipe'}
        eyebrow={recipe.data?.recipeCode ?? 'Recipe Studio'}
        back
        backTo={approvalMode ? 'recipes/approvals' : 'recipes'}
      />
      <div className="sm-screen">
        {recipe.loading && !recipe.data && <Loading />}
        {recipe.error && <ErrorNotice message={recipe.error} onRetry={recipe.reload} />}
        {recipe.data && (
          <>
            <div className="rs-card" style={{ cursor: 'default' }}>
              <RecipePicture
                recipeId={recipe.data.id}
                hasPicture={recipe.data.hasPicture}
                className="rs-card__img"
                preferDraft={isDraft || isPending}
              />
              <div className="rs-card__body">
                <span className="sm-row">
                  <strong>{recipe.data.name}</strong>
                  <StatusBadge status={recipe.data.readinessStatus || recipe.data.status} />
                </span>
                <span className="sm-meta">
                  {recipe.data.productClass || recipe.data.category || 'Recipe'}
                  {` · approved v${recipe.data.activeVersion || 0}`}
                  {` · portion ${recipe.data.servingQty} ${recipe.data.servingUom}`}
                  {recipe.data.prepTimeMinutes != null ? ` · prep ${recipe.data.prepTimeMinutes}m` : ''}
                </span>
              </div>
            </div>

            <div className="rs-tabs" role="tablist">
              {(
                [
                  ['ingredients', 'Ingredients'],
                  ['prep', 'Prep'],
                  ['scale', 'Scale'],
                  ['stock', 'Stock'],
                ] as const
              ).map(([id, label]) => (
                <button
                  key={id}
                  type="button"
                  className={`rs-tab${tab === id ? ' rs-tab--on' : ''}`}
                  onClick={() => setTab(id)}
                >
                  {label}
                </button>
              ))}
            </div>

            {tab === 'ingredients' && (
              <ul className="sm-list">
                {recipe.data.ingredients.map((ingredient) => (
                  <li key={ingredient.id} className="sm-list-btn">
                    <span className="sm-row">
                      <strong>{ingredient.itemName}</strong>
                      <span className="sm-meta">
                        {ingredient.quantity} {ingredient.uom}
                      </span>
                    </span>
                    <span className="sm-meta">{ingredient.itemCode}</span>
                  </li>
                ))}
                {recipe.data.ingredients.length === 0 && <Empty text="No ingredients" />}
              </ul>
            )}

            {tab === 'prep' && (
              <section className="sm-section">
                <h2>Preparation</h2>
                {recipe.data.preparationSteps ? (
                  <p style={{ whiteSpace: 'pre-wrap' }}>{recipe.data.preparationSteps}</p>
                ) : (
                  <Empty text="No preparation steps" description="Add steps in Cloud Recipe Studio for complex recipes." />
                )}
                {recipe.data.description && (
                  <>
                    <h2>Chef notes</h2>
                    <p style={{ whiteSpace: 'pre-wrap' }}>{recipe.data.description}</p>
                  </>
                )}
                {complex && (
                  <p className="sm-meta">
                    Batch / sub-recipe editing stays in SILA Cloud Recipe Studio.
                  </p>
                )}
              </section>
            )}

            {tab === 'scale' && (
              <section className="sm-section">
                <h2>Portion scaling</h2>
                <p className="sm-meta">Read-only recalculation. Approved recipe data is not changed.</p>
                <div className="rs-scale">
                  <label htmlFor="rs-servings">Servings</label>
                  <input
                    id="rs-servings"
                    className="sm-input"
                    type="number"
                    min={0.1}
                    step="any"
                    value={servings}
                    onChange={(event) => setServings(event.target.value)}
                  />
                  <span className="sm-meta">base {recipe.data.servingQty} {recipe.data.servingUom}</span>
                </div>
                <ul className="sm-list">
                  {scaled.map(({ ingredient, scaledQty }) => (
                    <li key={ingredient.id} className="sm-list-btn">
                      <span className="sm-row">
                        <strong>{ingredient.itemName}</strong>
                        <span className="sm-meta">
                          {scaledQty} {ingredient.uom}
                        </span>
                      </span>
                      <span className="sm-meta">
                        was {ingredient.quantity} {ingredient.uom}
                      </span>
                    </li>
                  ))}
                </ul>
              </section>
            )}

            {tab === 'stock' && (
              <section className="sm-section">
                <h2>Ingredient availability</h2>
                <p className="sm-meta">
                  Outlet: {location?.locationName ?? 'not set'} · estimated servings: {stock?.possibleServings ?? '—'}
                </p>
                {stockLoading && <Loading text="Checking live inventory…" />}
                {stockError && <ErrorNotice message={stockError} onRetry={() => recipe.data && loadStock(recipe.data)} />}
                <ul className="sm-list">
                  {(stock?.lines ?? []).map((line) => (
                    <li key={line.materialId} className="sm-list-btn">
                      <span className="sm-row">
                        <strong>{line.itemName}</strong>
                        <StatusBadge status={line.status} />
                      </span>
                      <span className="sm-meta">
                        need {line.required} · avail {line.available} {line.uom}
                        {line.limiting ? ' · limiting' : ''}
                      </span>
                    </li>
                  ))}
                </ul>
                <button type="button" className="sm-btn" onClick={() => go('inventory/stock')}>
                  Open Live Inventory
                </button>
              </section>
            )}

            <div className="rs-sticky">
              <button
                type="button"
                className="sm-btn"
                onClick={() => setFavorited(toggleFavoriteRecipe(recipe.data!.id).includes(recipe.data!.id))}
              >
                {favorited ? '★' : '☆'}
              </button>
              {can.manageRecipe && (
                <button
                  type="button"
                  className="sm-btn"
                  disabled={busy}
                  onClick={() =>
                    run(async () => {
                      const payload = writeFromDetail(recipe.data!);
                      const id = await createRecipe(payload);
                      go(`recipes/${id}`);
                    }, 'Draft created from this recipe.')
                  }
                >
                  Duplicate draft
                </button>
              )}
              {can.manageRecipe && isDraft && (
                <button
                  type="button"
                  className="sm-btn sm-btn--primary"
                  disabled={busy}
                  onClick={() => run(() => submitRecipe(recipe.data!.id), 'Submitted for approval.')}
                >
                  Submit
                </button>
              )}
              {can.manageRecipe && isDraft && (
                <button
                  type="button"
                  className="sm-btn"
                  disabled={busy}
                  onClick={() =>
                    run(async () => {
                      await deleteRecipeDraft(recipe.data!.id);
                      go('recipes/drafts');
                    }, 'Draft deleted.')
                  }
                >
                  Delete draft
                </button>
              )}
              {can.manageRecipe && canStartRevision(recipe.data.status) && (
                <button type="button" className="sm-btn" onClick={() => go(`recipes/draft/new?from=${recipe.data!.id}`)}>
                  Revise in wizard
                </button>
              )}
            </div>

            {(approvalMode || isPending) && can.approveRecipe && (
              <section className="sm-section">
                <h2>Approval</h2>
                <label className="sm-field">
                  Comment
                  <textarea className="sm-input" rows={3} value={comment} onChange={(event) => setComment(event.target.value)} />
                </label>
                <div className="sm-actions">
                  <button
                    type="button"
                    className="sm-btn sm-btn--primary"
                    disabled={busy}
                    onClick={() =>
                      run(async () => {
                        await approveRecipe(recipe.data!.id, comment);
                        go('recipes/approvals');
                      }, 'Recipe approved.')
                    }
                  >
                    Approve
                  </button>
                  <button
                    type="button"
                    className="sm-btn"
                    disabled={busy || !comment.trim()}
                    onClick={() =>
                      run(async () => {
                        await rejectRecipe(recipe.data!.id, comment);
                        go('recipes/approvals');
                      }, 'Recipe rejected.')
                    }
                  >
                    Reject
                  </button>
                </div>
              </section>
            )}

            {can.manageRecipe && isDraft && !complex && (
              <section className="sm-section">
                <h2>Edit basics</h2>
                <DraftBasicsEditor
                  recipe={recipe.data}
                  busy={busy}
                  onSave={async (next) => {
                    setBusy(true);
                    try {
                      await updateRecipe(recipe.data!.id, next);
                      recipe.reload();
                    } catch (caught: unknown) {
                      window.alert(errorText(caught, 'Could not save draft.'));
                    } finally {
                      setBusy(false);
                    }
                  }}
                />
              </section>
            )}
          </>
        )}
      </div>
    </>
  );
};

const DraftBasicsEditor: React.FC<{
  recipe: SilaRecipeDetail;
  busy: boolean;
  onSave: (write: ReturnType<typeof writeFromDetail>) => Promise<void>;
}> = ({ recipe, busy, onSave }) => {
  const [name, setName] = useState(recipe.name);
  const [steps, setSteps] = useState(recipe.preparationSteps ?? '');
  const [servingQty, setServingQty] = useState(String(recipe.servingQty));

  useEffect(() => {
    setName(recipe.name);
    setSteps(recipe.preparationSteps ?? '');
    setServingQty(String(recipe.servingQty));
  }, [recipe.id, recipe.name, recipe.preparationSteps, recipe.servingQty]);

  return (
    <>
      <label className="sm-field">
        Name
        <input className="sm-input" value={name} onChange={(event) => setName(event.target.value)} />
      </label>
      <label className="sm-field">
        Servings
        <input className="sm-input" type="number" value={servingQty} onChange={(event) => setServingQty(event.target.value)} />
      </label>
      <label className="sm-field">
        Preparation
        <textarea className="sm-input" rows={4} value={steps} onChange={(event) => setSteps(event.target.value)} />
      </label>
      <button
        type="button"
        className="sm-btn sm-btn--primary"
        disabled={busy || !name.trim()}
        onClick={() => {
          const write = writeFromDetail(recipe);
          write.name = name.trim();
          write.preparationSteps = steps.trim() || null;
          write.servingQty = Number(servingQty) || recipe.servingQty;
          void onSave(write);
        }}
      >
        Save draft
      </button>
    </>
  );
};

export default RecipeDetailScreen;
