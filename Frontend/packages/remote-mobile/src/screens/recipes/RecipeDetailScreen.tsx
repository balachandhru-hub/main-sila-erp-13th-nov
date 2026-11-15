import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import { searchLiveInventory } from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import { getMenuEngineering, saveMenuEngineering } from '../../../../remote-buyer/src/api/silaMe/silaMenuEngineeringApi';
import { getRecipe } from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';
import { formatCost, formatQty } from '../../../../remote-buyer/src/components/silaMe/recipes/recipeFormat';
import { resolveStudioKind, StudioCover } from '../../../../remote-buyer/src/components/silaMe/recipes/recipeStudioVisuals';
import ScreenHeader from '../../components/ScreenHeader';
import StatusBadge from '../../components/StatusBadge';
import { ErrorNotice, Loading } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useMyLocation } from '../../location';
import { useGo } from '../../navigation';
import { errorText, useLoad } from '../../useLoad';

/** Recipe Studio cookbook detail (PWA). */
const RecipeDetailScreen: React.FC = () => {
  const { recipeId = '' } = useParams();
  const go = useGo();
  const { can } = useAccess();
  const { location } = useMyLocation();
  const recipe = useLoad(() => getRecipe(recipeId), recipeId || null);
  const menuItems = useLoad(
    () => getMenuEngineering('items', { search: recipe.data?.name, limit: 30 }),
    can.viewMenuEngineering && recipe.data?.name ? `recipe-menu-${recipe.data.name}` : null,
  );
  const [menuItemId, setMenuItemId] = useState('');
  const [linking, setLinking] = useState(false);
  const linked = (menuItems.data?.items?.items ?? []).find((row) => row.linkedRecipeId === recipeId);
  const stock = useLoad(
    () => (location ? searchLiveInventory('', location.id) : Promise.resolve([])),
    location ? `recipe-stock-${location.id}` : null,
  );
  const onHand = new Map((stock.data ?? []).map((row) => [row.materialId.toLowerCase(), row.onHandQty]));
  const kind = resolveStudioKind(recipe.data?.category, recipe.data?.familyName, recipe.data?.name);

  return (
    <>
      <ScreenHeader title={recipe.data?.name ?? 'Recipe'} eyebrow={recipe.data?.recipeCode ?? 'Recipe Studio'} back backTo="recipes" />
      <div className="sm-screen sm-rstudio">
        {recipe.loading && !recipe.data && <Loading />}
        {recipe.error && <ErrorNotice message={recipe.error} onRetry={recipe.reload} />}
        {recipe.data && (
          <>
            <div className="sm-rstudio-hero">
              <StudioCover kind={kind} title={recipe.data.name} subtitle={recipe.data.category || undefined} />
              <div className="sm-row">
                <span className="sm-meta">
                  {formatQty(recipe.data.servingQty)} {recipe.data.servingUom}
                  {recipe.data.menuPrice != null ? ` · menu ${formatCost(recipe.data.menuPrice, recipe.data.currency)}` : ''}
                  {recipe.data.totalCost != null ? ` · cost ${formatCost(recipe.data.totalCost, recipe.data.currency)}` : ''}
                </span>
                <StatusBadge status={recipe.data.readinessStatus || recipe.data.status} />
              </div>
            </div>
            {can.manageRecipe && (
              <button type="button" className="sm-btn" onClick={() => go(`recipes/${recipeId}/edit`)}>
                Open Recipe Studio editor
              </button>
            )}
            {can.manageMenuEngineering && (
              <section className="sm-section">
                <h2>Link menu item</h2>
                {linked && <p className="sm-meta">Linked to {linked.name} ({linked.menuItemCode})</p>}
                <label className="sm-field">
                  Menu item
                  <select className="sm-input" value={menuItemId} onChange={(event) => setMenuItemId(event.target.value)}>
                    <option value="">Select</option>
                    {(menuItems.data?.items?.items ?? []).map((row) => (
                      <option key={row.id} value={row.id}>{row.name}</option>
                    ))}
                  </select>
                </label>
                <button
                  type="button"
                  className="sm-btn"
                  disabled={!menuItemId || linking}
                  onClick={async () => {
                    setLinking(true);
                    try {
                      await saveMenuEngineering({ action: 'LINK_RECIPE', menuItemId, recipeId });
                      menuItems.reload();
                    } catch (caught: unknown) {
                      window.alert(errorText(caught, 'Could not link the menu item.'));
                    } finally {
                      setLinking(false);
                    }
                  }}
                >
                  Link menu item
                </button>
              </section>
            )}
            <section className="sm-section">
              <h2>Ingredients</h2>
              <ul className="sm-rstudio-ingredients">
                {recipe.data.ingredients.map((ingredient) => {
                  const qty = ingredient.materialId ? onHand.get(ingredient.materialId.toLowerCase()) : undefined;
                  const short = ingredient.materialId != null && (qty ?? 0) <= 0;
                  const cardKind = resolveStudioKind(ingredient.itemName, ingredient.itemCode);
                  return (
                    <li key={ingredient.id} className="sm-rstudio-ingredient">
                      <StudioCover compact kind={cardKind} title={ingredient.itemName} />
                      <span>
                        <span className="sm-row">
                          <strong>{ingredient.itemName}</strong>
                          <span className="sm-meta">{formatQty(ingredient.quantity)} {ingredient.uom}</span>
                        </span>
                        <span className="sm-meta">
                          {ingredient.itemCode}
                          {ingredient.materialId == null
                            ? ''
                            : qty === undefined
                              ? ' · not at this location'
                              : ` · ${qty} on hand${short ? ' · short' : ''}`}
                        </span>
                      </span>
                    </li>
                  );
                })}
              </ul>
            </section>
          </>
        )}
      </div>
    </>
  );
};

export default RecipeDetailScreen;
