import React from 'react';
import { getRecipeApprovals } from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';
import ScreenHeader from '../../components/ScreenHeader';
import StatusBadge from '../../components/StatusBadge';
import { Empty, ErrorNotice, Loading } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useGo } from '../../navigation';
import { useLoad } from '../../useLoad';
import RecipePicture from './RecipePicture';
import './recipeStudio.css';

/** Pending recipe approvals for authorized Head Chef / Manager roles. */
const RecipeApprovalsScreen: React.FC = () => {
  const go = useGo();
  const { can } = useAccess();
  const { data, loading, error, reload } = useLoad(
    () => getRecipeApprovals(),
    can.approveRecipe ? 'recipe-approvals' : null,
  );

  return (
    <>
      <ScreenHeader title="Recipe approvals" eyebrow="Recipe Studio" back backTo="recipes" />
      <div className="sm-screen">
        {!can.approveRecipe && (
          <Empty text="Not permitted" description="Recipe approvals need approve-recipe permission." />
        )}
        {can.approveRecipe && (
          <>
            {loading && <Loading text="Loading approvals…" />}
            {error && <ErrorNotice message={error} onRetry={reload} />}
            {!loading && !error && (data ?? []).length === 0 && (
              <Empty text="Inbox clear" description="No recipes are waiting for your approval." />
            )}
            <ul className="sm-list">
              {(data ?? []).map((recipe) => (
                <li key={recipe.id}>
                  <button type="button" className="rs-card" onClick={() => go(`recipes/approvals/${recipe.id}`)}>
                    <RecipePicture recipeId={recipe.id} hasPicture={recipe.hasPicture} className="rs-card__img" preferDraft />
                    <span className="rs-card__body">
                      <span className="sm-row">
                        <strong>{recipe.name}</strong>
                        <StatusBadge status={recipe.status} />
                      </span>
                      <span className="sm-meta">
                        {recipe.recipeCode}
                        {` · v${recipe.version}`}
                        {recipe.submittedOn ? ` · ${recipe.submittedOn}` : ''}
                        {recipe.approverRole ? ` · ${recipe.approverRole}` : ''}
                        {` · ${recipe.ingredientCount} ingredients`}
                      </span>
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </>
        )}
      </div>
    </>
  );
};

export default RecipeApprovalsScreen;
