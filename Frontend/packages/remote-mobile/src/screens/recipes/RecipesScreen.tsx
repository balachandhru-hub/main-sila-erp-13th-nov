import React, { useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { getRecipes, type SilaRecipeListItem } from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';
import ScreenHeader from '../../components/ScreenHeader';
import StatusBadge from '../../components/StatusBadge';
import { Empty, ErrorNotice, Loading } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useGo } from '../../navigation';
import { useLoad } from '../../useLoad';
import RecipePicture from './RecipePicture';
import { PRODUCT_CHIPS, readFavoriteRecipeIds, toggleFavoriteRecipe } from './recipeStudioHelpers';
import './recipeStudio.css';

/** Recipe catalog — Food / Beverage / Minibar / Tobacco with search and status filters. */
const RecipesScreen: React.FC = () => {
  const go = useGo();
  const { can } = useAccess();
  const [params] = useSearchParams();
  const [search, setSearch] = useState(params.get('q') ?? '');
  const [query, setQuery] = useState(params.get('q') ?? '');
  const [productClass, setProductClass] = useState(params.get('class') ?? '');
  const [status, setStatus] = useState('');
  const [favoritesOnly, setFavoritesOnly] = useState(false);
  const [favoriteIds, setFavoriteIds] = useState(() => readFavoriteRecipeIds());
  const [page, setPage] = useState(0);
  const limit = 30;

  const { data, loading, error, reload } = useLoad(
    () =>
      getRecipes({
        search: query || undefined,
        productClass: productClass || undefined,
        status: status || undefined,
        index: page * limit,
        limit,
      }),
    can.manageRecipe || can.approveRecipe ? `catalog-${query}-${productClass}-${status}-${page}` : null,
  );

  const items = useMemo(() => {
    const rows = data?.items ?? [];
    if (!favoritesOnly) return rows;
    return rows.filter((row) => favoriteIds.includes(row.id));
  }, [data?.items, favoritesOnly, favoriteIds]);

  const total = data?.total ?? 0;
  const hasMore = (page + 1) * limit < total;

  return (
    <>
      <ScreenHeader title="Recipe catalog" eyebrow="Recipe Studio" back backTo="recipes" />
      <div className="sm-screen">
        <form
          className="sm-field"
          onSubmit={(event) => {
            event.preventDefault();
            setPage(0);
            setQuery(search.trim());
          }}
        >
          <label htmlFor="sm-recipe-search">Search</label>
          <input
            id="sm-recipe-search"
            className="sm-input"
            value={search}
            placeholder="Name or recipe code"
            onChange={(event) => setSearch(event.target.value)}
          />
        </form>

        <div className="rs-chips" role="tablist" aria-label="Product class">
          {PRODUCT_CHIPS.map((chip) => (
            <button
              key={chip.id || 'all'}
              type="button"
              className={`rs-chip${productClass === chip.id ? ' rs-chip--on' : ''}`}
              onClick={() => {
                setPage(0);
                setProductClass(chip.id);
              }}
            >
              {chip.label}
            </button>
          ))}
        </div>

        <div className="rs-chips" style={{ marginTop: '0.5rem' }}>
          {[
            { id: '', label: 'Any status' },
            { id: 'APPROVED', label: 'Approved' },
            { id: 'DRAFT', label: 'Draft' },
            { id: 'PENDING_APPROVAL', label: 'Pending' },
          ].map((chip) => (
            <button
              key={chip.id || 'any'}
              type="button"
              className={`rs-chip${status === chip.id ? ' rs-chip--on' : ''}`}
              onClick={() => {
                setPage(0);
                setStatus(chip.id);
              }}
            >
              {chip.label}
            </button>
          ))}
          <button
            type="button"
            className={`rs-chip${favoritesOnly ? ' rs-chip--on' : ''}`}
            onClick={() => setFavoritesOnly((value) => !value)}
          >
            Favorites
          </button>
        </div>

        {loading && <Loading text="Loading catalog…" />}
        {error && <ErrorNotice message={error} onRetry={reload} />}
        {!loading && !error && items.length === 0 && (
          <Empty text="No recipes" description="Nothing matches these filters." />
        )}

        <ul className="sm-list">
          {items.map((recipe) => (
            <CatalogCard
              key={recipe.id}
              recipe={recipe}
              favorited={favoriteIds.includes(recipe.id)}
              onOpen={() => go(`recipes/${recipe.id}`)}
              onToggleFavorite={() => setFavoriteIds(toggleFavoriteRecipe(recipe.id))}
            />
          ))}
        </ul>

        {(page > 0 || hasMore) && (
          <div className="sm-actions">
            <button type="button" className="sm-btn" disabled={page === 0} onClick={() => setPage((p) => Math.max(0, p - 1))}>
              Previous
            </button>
            <button type="button" className="sm-btn" disabled={!hasMore} onClick={() => setPage((p) => p + 1)}>
              Next
            </button>
          </div>
        )}
      </div>
    </>
  );
};

const CatalogCard: React.FC<{
  recipe: SilaRecipeListItem;
  favorited: boolean;
  onOpen: () => void;
  onToggleFavorite: () => void;
}> = ({ recipe, favorited, onOpen, onToggleFavorite }) => (
  <li>
    <button type="button" className="rs-card" onClick={onOpen}>
      <RecipePicture recipeId={recipe.id} hasPicture={recipe.hasPicture} className="rs-card__img" />
      <span className="rs-card__body">
        <span className="sm-row">
          <strong>{recipe.name}</strong>
          <StatusBadge status={recipe.readinessStatus || recipe.status} />
        </span>
        <span className="sm-meta">
          {recipe.recipeCode}
          {recipe.productClass ? ` · ${recipe.productClass}` : recipe.category ? ` · ${recipe.category}` : ''}
          {recipe.recipeFormat ? ` · ${recipe.recipeFormat}` : ''}
          {` · v${recipe.activeVersion || recipe.version}`}
        </span>
      </span>
      <span
        role="button"
        tabIndex={0}
        className="sm-btn"
        aria-label={favorited ? 'Remove favorite' : 'Favorite'}
        onClick={(event) => {
          event.stopPropagation();
          onToggleFavorite();
        }}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            event.stopPropagation();
            onToggleFavorite();
          }
        }}
      >
        {favorited ? '★' : '☆'}
      </span>
    </button>
  </li>
);

export default RecipesScreen;
