import React from 'react';
import { deleteRecipeDraft, getRecipes, submitRecipe } from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';
import ScreenHeader from '../../components/ScreenHeader';
import StatusBadge from '../../components/StatusBadge';
import { Empty, ErrorNotice, Loading } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useGo } from '../../navigation';
import { errorText, useLoad } from '../../useLoad';
import RecipePicture from './RecipePicture';
import './recipeStudio.css';

/** Drafts editable by the current user via Cloud Recipe Master. */
const RecipeDraftsScreen: React.FC = () => {
  const go = useGo();
  const { can } = useAccess();
  const { data, loading, error, reload } = useLoad(
    () => getRecipes({ status: 'DRAFT', limit: 50 }),
    can.manageRecipe ? 'recipe-drafts' : null,
  );
  const rejected = useLoad(
    () => getRecipes({ status: 'REJECTED', limit: 20 }),
    can.manageRecipe ? 'recipe-rejected' : null,
  );

  const items = [...(data?.items ?? []), ...(rejected.data?.items ?? [])];

  const act = async (fn: () => Promise<void>) => {
    try {
      await fn();
      reload();
      rejected.reload();
    } catch (caught: unknown) {
      window.alert(errorText(caught, 'Could not update the draft.'));
    }
  };

  return (
    <>
      <ScreenHeader title="My drafts" eyebrow="Recipe Studio" back backTo="recipes" />
      <div className="sm-screen">
        {!can.manageRecipe && <Empty text="Not permitted" description="Draft management needs manage-recipe permission." />}
        {can.manageRecipe && (
          <>
            <button type="button" className="sm-btn sm-btn--primary" onClick={() => go('recipes/draft/new')}>
              New quick draft
            </button>
            {(loading || rejected.loading) && <Loading text="Loading drafts…" />}
            {(error || rejected.error) && <ErrorNotice message={error || rejected.error || ''} onRetry={reload} />}
            {!loading && items.length === 0 && <Empty text="No drafts" description="Create a quick draft to get started." />}
            <ul className="sm-list">
              {items.map((recipe) => (
                <li key={recipe.id}>
                  <div className="rs-card" style={{ cursor: 'default', flexDirection: 'column' }}>
                    <button type="button" className="rs-card" style={{ boxShadow: 'none', padding: 0 }} onClick={() => go(`recipes/${recipe.id}`)}>
                      <RecipePicture recipeId={recipe.id} hasPicture={recipe.hasPicture} className="rs-card__img" preferDraft />
                      <span className="rs-card__body">
                        <span className="sm-row">
                          <strong>{recipe.name}</strong>
                          <StatusBadge status={recipe.status} />
                        </span>
                        <span className="sm-meta">
                          {recipe.recipeCode}
                          {recipe.productClass ? ` · ${recipe.productClass}` : ''}
                          {` · v${recipe.version}`}
                        </span>
                      </span>
                    </button>
                    <div className="sm-actions" style={{ marginTop: '0.5rem' }}>
                      <button type="button" className="sm-btn" onClick={() => go(`recipes/${recipe.id}`)}>
                        Open
                      </button>
                      <button type="button" className="sm-btn" onClick={() => go(`recipes/draft/new?from=${recipe.id}`)}>
                        Edit wizard
                      </button>
                      <button
                        type="button"
                        className="sm-btn sm-btn--primary"
                        onClick={() => void act(() => submitRecipe(recipe.id))}
                      >
                        Submit
                      </button>
                      <button
                        type="button"
                        className="sm-btn"
                        onClick={() => {
                          if (!window.confirm('Delete this draft? Approved history is never removed.')) return;
                          void act(() => deleteRecipeDraft(recipe.id));
                        }}
                      >
                        Delete
                      </button>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          </>
        )}
      </div>
    </>
  );
};

export default RecipeDraftsScreen;
