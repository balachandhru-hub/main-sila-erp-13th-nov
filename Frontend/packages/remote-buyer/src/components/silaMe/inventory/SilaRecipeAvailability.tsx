import React, { useEffect, useState } from "react";
import { EmptyState, Loader, PageHeader } from "@vosox/shared-ui";
import { formatQty, getLiveInventoryDetail, getMyLocations, type SilaLocation } from "../../../api/silaMe/silaInventoryApi";
import { getRecipe, getRecipes, type SilaRecipeDetail, type SilaRecipeListItem } from "../../../api/silaMe/silaRecipeApi";
import SilaSearchableSelect from "../SilaSearchableSelect";
import "../silaMeTheme.css";
import "./SilaInventory.css";

interface IngredientAvail {
  ingredientCode: string;
  itemCode: string;
  itemName: string;
  neededPerServing: number;
  uom: string;
  available: number;
  possibleServings: number;
  limiting: boolean;
}

/** Theoretical servings from approved recipes and live inventory — no consumption is posted. */
const SilaRecipeAvailability: React.FC = () => {
  const [locations, setLocations] = useState<SilaLocation[]>([]);
  const [locationId, setLocationId] = useState("");
  const [recipes, setRecipes] = useState<SilaRecipeListItem[]>([]);
  const [recipeId, setRecipeId] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [detail, setDetail] = useState<SilaRecipeDetail | null>(null);
  const [lines, setLines] = useState<IngredientAvail[]>([]);
  const [servings, setServings] = useState<number | null>(null);

  useEffect(() => {
    getMyLocations()
      .then((data) => {
        const stocked = data.filter((x) => x.locationType !== "VENUE");
        setLocations(stocked);
        const home = stocked.find((x) => x.assigned) ?? stocked[0];
        if (home) setLocationId(home.id);
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Could not load locations."));
    getRecipes({ status: "APPROVED", active: true, limit: 100 })
      .then((page) => setRecipes(page.items.filter((x) => x.itemMode === "RECIPE" || x.itemMode === "BATCH")))
      .catch(() => setRecipes([]));
  }, []);

  useEffect(() => {
    if (!recipeId || !locationId) {
      setDetail(null);
      setLines([]);
      setServings(null);
      return;
    }
    let active = true;
    setLoading(true);
    setError(null);
    (async () => {
      try {
        const recipe = await getRecipe(recipeId);
        if (!active) return;
        setDetail(recipe);
        const ingredients = (recipe.ingredients ?? []).filter((x) => x.materialId && x.quantity > 0);
        const computed: IngredientAvail[] = [];
        for (const ingredient of ingredients) {
          const stock = await getLiveInventoryDetail(ingredient.materialId!, {
            requiredQty: ingredient.quantity,
            currentLocationId: locationId,
          });
          const available = stock.localAvailable ?? stock.locations.find((x) => x.locationId === locationId)?.availableQty ?? 0;
          const needed = ingredient.quantity;
          const possible = needed > 0 ? Math.floor(available / needed) : 0;
          computed.push({
            ingredientCode: ingredient.ingredientCode,
            itemCode: ingredient.itemCode || stock.materialCode,
            itemName: ingredient.itemName || stock.description,
            neededPerServing: needed,
            uom: ingredient.uom || stock.baseUom,
            available,
            possibleServings: possible,
            limiting: false,
          });
        }
        const maxServings = computed.length === 0 ? 0 : Math.min(...computed.map((x) => x.possibleServings));
        const marked = computed.map((x) => ({ ...x, limiting: x.possibleServings === maxServings && maxServings >= 0 }));
        if (active) {
          setLines(marked);
          setServings(maxServings);
        }
      } catch (err: unknown) {
        if (active) setError(err instanceof Error ? err.message : "Could not calculate availability.");
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => {
      active = false;
    };
  }, [recipeId, locationId]);

  return (
    <div className="sila-me sinv-page">
      <PageHeader
        title="Recipe Availability"
        description="Estimated servings from the approved recipe version and available inventory. Calculation only — no stock is posted."
      />
      <section className="sila-card sinv-filters">
        <SilaSearchableSelect
          label="Location"
          value={locationId}
          clearable={false}
          options={locations.map((location) => ({
            value: location.id,
            label: `${location.locationCode} · ${location.locationName}`,
          }))}
          onChange={setLocationId}
        />
        <SilaSearchableSelect
          label="Approved recipe"
          value={recipeId}
          clearable
          options={recipes.map((recipe) => ({
            value: recipe.id,
            label: `${recipe.recipeCode} · ${recipe.name}`,
          }))}
          onChange={setRecipeId}
        />
      </section>
      {loading ? (
        <Loader size={28} message="Calculating servings…" />
      ) : error ? (
        <EmptyState variant="error" title="Couldn't calculate" description={error} />
      ) : !recipeId ? (
        <EmptyState title="Choose a recipe" description="Select an approved recipe to see ingredient availability." />
      ) : (
        <section className="sila-card">
          <div className="sila-card-header">
            <h2 className="sila-card-title">{detail?.recipeCode} · {detail?.name}</h2>
            <span className="sila-badge sila-badge--success">≈ {servings ?? 0} servings</span>
          </div>
          {lines.length === 0 ? (
            <EmptyState title="No ingredients" description="This recipe has no material ingredients to check." />
          ) : (
            <div className="sila-table-wrap">
              <table className="sila-table">
                <thead>
                  <tr>
                    <th scope="col">Ingredient</th>
                    <th scope="col" className="sinv-num">Per serving</th>
                    <th scope="col" className="sinv-num">Available</th>
                    <th scope="col" className="sinv-num">Possible</th>
                    <th scope="col">Limiting</th>
                  </tr>
                </thead>
                <tbody>
                  {lines.map((line) => (
                    <tr key={line.ingredientCode}>
                      <td>
                        <span className="sila-cell-strong">{line.itemCode}</span>
                        <span className="sinv-sub">{line.itemName}</span>
                      </td>
                      <td className="sinv-num">{formatQty(line.neededPerServing)} {line.uom}</td>
                      <td className="sinv-num">{formatQty(line.available)} {line.uom}</td>
                      <td className="sinv-num">{line.possibleServings}</td>
                      <td>{line.limiting ? <span className="sila-badge sila-badge--warning">Yes</span> : "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
    </div>
  );
};

export default SilaRecipeAvailability;
