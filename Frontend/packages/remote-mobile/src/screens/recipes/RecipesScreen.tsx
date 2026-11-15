import React, { useState } from 'react';
import { getRecipes, type SilaRecipeListItem } from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';
import { costPerServing, formatCost, formatQty, recipeLabel } from '../../../../remote-buyer/src/components/silaMe/recipes/recipeFormat';
import { resolveStudioKind, StudioCover } from '../../../../remote-buyer/src/components/silaMe/recipes/recipeStudioVisuals';
import ScreenHeader from '../../components/ScreenHeader';
import StatusBadge from '../../components/StatusBadge';
import { Empty, ErrorNotice, Loading } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useGo } from '../../navigation';
import { useLoad } from '../../useLoad';

/** Recipe Studio catalog (PWA) — image-led cards over the same recipes API. */
const RecipesScreen: React.FC = () => {
  const go = useGo();
  const { can } = useAccess();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const { data, loading, error, reload } = useLoad(() => getRecipes({ search: query, limit: 30 }), `recipes-${query}`);
  const items: SilaRecipeListItem[] = data?.items ?? [];

  return (
    <>
      <ScreenHeader title="Recipe Studio" eyebrow="Culinary catalog" back backTo="home" />
      <div className="sm-screen sm-rstudio">
        {can.manageRecipe && (
          <button type="button" className="sm-btn" onClick={() => go('recipes/new')}>New recipe</button>
        )}
        <form
          className="sm-field"
          onSubmit={(event) => {
            event.preventDefault();
            setQuery(search.trim());
          }}
        >
          <label htmlFor="sm-recipe-search">Search</label>
          <input
            id="sm-recipe-search"
            className="sm-input"
            value={search}
            placeholder="Code or name"
            onChange={(event) => setSearch(event.target.value)}
          />
        </form>
        {loading && <Loading text="Loading Recipe Studio…" />}
        {error && <ErrorNotice message={error} onRetry={reload} />}
        {!loading && !error && items.length === 0 && <Empty text="No recipes" description="Nothing matches this search." />}
        <div className="sm-rstudio-grid">
          {items.map((recipe) => {
            const kind = resolveStudioKind(recipe.category, recipe.familyName, recipe.name);
            const portion = recipe.costComplete || recipe.activeVersion > 0
              ? (recipe.costPerServing ?? costPerServing(recipe.totalCost, recipe.servingQty))
              : null;
            return (
              <button key={recipe.id} type="button" className="sm-rstudio-card" onClick={() => go(`recipes/${recipe.id}`)}>
                <StudioCover kind={kind} title={recipe.name} subtitle={recipe.category || undefined} />
                <span className="sm-rstudio-card__body">
                  <span className="sm-row">
                    <strong>{recipe.name}</strong>
                    <StatusBadge status={recipe.readinessStatus || recipe.status} />
                  </span>
                  <span className="sm-meta">
                    {recipe.recipeCode} · {recipeLabel(recipe.itemMode)} · {formatQty(recipe.servingQty)} {recipe.servingUom}
                  </span>
                  <span className="sm-meta">
                    {portion == null ? 'COST PENDING' : formatCost(portion, recipe.currency)} / portion
                    {` · ${recipe.ingredientCount} ingredients`}
                  </span>
                </span>
              </button>
            );
          })}
        </div>
      </div>
    </>
  );
};

export default RecipesScreen;
