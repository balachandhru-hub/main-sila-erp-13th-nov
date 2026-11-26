import React, { useMemo, useState } from 'react';
import {
  getRecipeApprovals,
  getRecipes,
  type SilaRecipeListItem,
} from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';
import ScreenHeader from '../../components/ScreenHeader';
import StatusBadge from '../../components/StatusBadge';
import { Empty, ErrorNotice, Loading } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useMyLocation } from '../../location';
import { useGo } from '../../navigation';
import { useLoad } from '../../useLoad';
import RecipePicture from './RecipePicture';
import { PRODUCT_CHIPS, readFavoriteRecipeIds, readRecentRecipeIds } from './recipeStudioHelpers';
import './recipeStudio.css';

const RecipeStudioHomeScreen: React.FC = () => {
  const go = useGo();
  const { can } = useAccess();
  const { location } = useMyLocation();
  const [search, setSearch] = useState('');
  const [productClass, setProductClass] = useState('');
  const recentIds = useMemo(() => readRecentRecipeIds(), []);
  const favoriteIds = useMemo(() => readFavoriteRecipeIds(), []);

  const catalog = useLoad(
    () =>
      getRecipes({
        search: search.trim() || undefined,
        productClass: productClass || undefined,
        limit: 24,
      }),
    can.manageRecipe || can.approveRecipe ? `rs-home-${search}-${productClass}` : null,
  );
  const drafts = useLoad(
    () => getRecipes({ status: 'DRAFT', limit: 8 }),
    can.manageRecipe ? 'rs-home-drafts' : null,
  );
  const approvals = useLoad(
    () => getRecipeApprovals(),
    can.approveRecipe ? 'rs-home-approvals' : null,
  );

  const items = catalog.data?.items ?? [];
  const recent = recentIds
    .map((id) => items.find((row) => row.id === id))
    .filter((row): row is SilaRecipeListItem => Boolean(row));
  const favorites = favoriteIds
    .map((id) => items.find((row) => row.id === id))
    .filter((row): row is SilaRecipeListItem => Boolean(row));

  const openSearch = (event: React.FormEvent) => {
    event.preventDefault();
    const q = search.trim();
    go(q ? `recipes/catalog?q=${encodeURIComponent(q)}` : 'recipes/catalog');
  };

  return (
    <>
      <ScreenHeader title="Recipe Studio" eyebrow="SILA Mobile" back backTo="home" />
      <div className="sm-screen">
        <div className="rs-home-hero">
          <p className="sm-meta">
            {location ? `${location.locationName}${location.locationCode ? ` · ${location.locationCode}` : ''}` : 'No outlet assigned'}
          </p>
        </div>

        <form className="sm-field" onSubmit={openSearch}>
          <label htmlFor="rs-home-search">Search recipes</label>
          <input
            id="rs-home-search"
            className="sm-input"
            value={search}
            placeholder="Name or code"
            onChange={(event) => setSearch(event.target.value)}
          />
        </form>

        <div className="rs-chips" role="tablist" aria-label="Category">
          {PRODUCT_CHIPS.map((chip) => (
            <button
              key={chip.id || 'all'}
              type="button"
              className={`rs-chip${productClass === chip.id ? ' rs-chip--on' : ''}`}
              onClick={() => setProductClass(chip.id)}
            >
              {chip.label}
            </button>
          ))}
        </div>

        <div className="sm-actions" style={{ marginTop: '0.75rem' }}>
          <button type="button" className="sm-btn" onClick={() => go(productClass ? `recipes/catalog?class=${productClass}` : 'recipes/catalog')}>
            Catalog
          </button>
          {can.manageRecipe && (
            <button type="button" className="sm-btn sm-btn--primary" onClick={() => go('recipes/draft/new')}>
              Quick draft
            </button>
          )}
          {can.manageRecipe && (
            <button type="button" className="sm-btn" onClick={() => go('recipes/drafts')}>
              My drafts
            </button>
          )}
          {can.approveRecipe && (
            <button type="button" className="sm-btn" onClick={() => go('recipes/approvals')}>
              Approvals{(approvals.data?.length ?? 0) > 0 ? ` (${approvals.data!.length})` : ''}
            </button>
          )}
          <button type="button" className="sm-btn" onClick={() => go('recipes/culinary-suite')}>
            Culinary 2.0 (Demo)
          </button>
        </div>

        {catalog.loading && <Loading text="Loading recipes…" />}
        {catalog.error && <ErrorNotice message={catalog.error} onRetry={catalog.reload} />}

        {favorites.length > 0 && (
          <section className="sm-section">
            <h2>Favorites</h2>
            <RecipeCardList items={favorites} onOpen={(id) => go(`recipes/${id}`)} />
          </section>
        )}

        {recent.length > 0 && (
          <section className="sm-section">
            <h2>Recent</h2>
            <RecipeCardList items={recent} onOpen={(id) => go(`recipes/${id}`)} />
          </section>
        )}

        <section className="sm-section">
          <h2>{productClass ? `${productClass} recipes` : 'Recipes'}</h2>
          {!catalog.loading && !catalog.error && items.length === 0 && (
            <Empty text="No recipes" description="Try another category or search in the catalog." />
          )}
          <RecipeCardList items={items.slice(0, 8)} onOpen={(id) => go(`recipes/${id}`)} />
          {items.length > 8 && (
            <button type="button" className="sm-btn" onClick={() => go(productClass ? `recipes/catalog?class=${productClass}` : 'recipes/catalog')}>
              View full catalog
            </button>
          )}
        </section>

        {can.manageRecipe && (
          <section className="sm-section">
            <h2>My drafts</h2>
            {drafts.loading && <Loading text="Loading drafts…" />}
            {!drafts.loading && (drafts.data?.items ?? []).length === 0 && (
              <Empty text="No drafts" description="Create a quick draft when you need a new recipe." />
            )}
            <RecipeCardList items={drafts.data?.items ?? []} onOpen={(id) => go(`recipes/${id}`)} />
          </section>
        )}

        {can.approveRecipe && (
          <section className="sm-section">
            <h2>Pending approvals</h2>
            {approvals.loading && <Loading text="Loading approvals…" />}
            {!approvals.loading && (approvals.data ?? []).length === 0 && (
              <Empty text="Nothing waiting" description="You have no recipe approvals right now." />
            )}
            <RecipeCardList items={approvals.data ?? []} onOpen={(id) => go(`recipes/approvals/${id}`)} />
          </section>
        )}
      </div>
    </>
  );
};

const RecipeCardList: React.FC<{ items: SilaRecipeListItem[]; onOpen: (id: string) => void }> = ({ items, onOpen }) => (
  <ul className="sm-list">
    {items.map((recipe) => (
      <li key={recipe.id}>
        <button type="button" className="rs-card" onClick={() => onOpen(recipe.id)}>
          <RecipePicture recipeId={recipe.id} hasPicture={recipe.hasPicture} className="rs-card__img" />
          <span className="rs-card__body">
            <span className="sm-row">
              <strong>{recipe.name}</strong>
              <StatusBadge status={recipe.readinessStatus || recipe.status} />
            </span>
            <span className="sm-meta">
              {recipe.recipeCode}
              {recipe.productClass ? ` · ${recipe.productClass}` : ''}
              {recipe.recipeFormat ? ` · ${recipe.recipeFormat}` : ''}
              {` · v${recipe.version}`}
            </span>
          </span>
        </button>
      </li>
    ))}
  </ul>
);

export default RecipeStudioHomeScreen;
