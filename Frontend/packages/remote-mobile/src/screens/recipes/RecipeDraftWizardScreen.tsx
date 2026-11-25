import React, { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { searchMaterials, type SilaMaterial } from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import {
  createRecipe,
  getRecipe,
  setRecipePicture,
  submitRecipe,
  updateRecipe,
  writeFromDetail,
  type SilaProductClass,
  type SilaRecipeIngredientWrite,
  type SilaRecipeWrite,
} from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';
import ScreenHeader from '../../components/ScreenHeader';
import { Empty, ErrorNotice, Loading } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useGo } from '../../navigation';
import { errorText, useLoad } from '../../useLoad';
import { PRODUCT_CHIPS } from './recipeStudioHelpers';
import './recipeStudio.css';

interface DraftLine {
  key: string;
  materialId: string;
  itemCode: string;
  itemName: string;
  quantity: string;
  uom: string;
}

const emptyWrite = (productClass: SilaProductClass): SilaRecipeWrite => ({
  name: '',
  itemMode: 'RECIPE',
  productClass,
  recipeFormat: 'STANDARD_RECIPE',
  subcategory: '',
  servingQty: 1,
  servingUom: 'EA',
  sellingUom: 'EA',
  preparationSteps: '',
  ingredients: [],
  outletPrices: [],
});

/** Short 4-step wizard for simple recipe drafts using Cloud Recipe Master APIs. */
const RecipeDraftWizardScreen: React.FC = () => {
  const go = useGo();
  const { can } = useAccess();
  const [params] = useSearchParams();
  const fromId = params.get('from') ?? '';
  const source = useLoad(() => getRecipe(fromId), fromId || null);

  const [step, setStep] = useState(1);
  const [productClass, setProductClass] = useState<SilaProductClass>('FOOD');
  const [name, setName] = useState('');
  const [subcategory, setSubcategory] = useState('');
  const [servingQty, setServingQty] = useState('1');
  const [servingUom, setServingUom] = useState('EA');
  const [prep, setPrep] = useState('');
  const [picture, setPicture] = useState<File | null>(null);
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [materialQuery, setMaterialQuery] = useState('');
  const [materialHits, setMaterialHits] = useState<SilaMaterial[]>([]);
  const [searching, setSearching] = useState(false);
  const [busy, setBusy] = useState(false);
  const [seeded, setSeeded] = useState(false);

  useEffect(() => {
    if (!source.data || seeded) return;
    const detail = source.data;
    setSeeded(true);
    setName(`${detail.name} (draft)`);
    setProductClass((detail.productClass as SilaProductClass) || 'FOOD');
    setSubcategory(detail.subcategory ?? '');
    setServingQty(String(detail.servingQty || 1));
    setServingUom(detail.servingUom || 'EA');
    setPrep(detail.preparationSteps ?? '');
    setLines(
      detail.ingredients
        .filter((row) => row.materialId)
        .map((row, index) => ({
          key: `src-${index}`,
          materialId: row.materialId!,
          itemCode: row.itemCode,
          itemName: row.itemName,
          quantity: String(row.quantity),
          uom: row.uom || 'EA',
        })),
    );
  }, [source.data, seeded]);

  const classes = useMemo(() => PRODUCT_CHIPS.filter((chip) => chip.id), []);

  const searchMaterialsNow = async () => {
    setSearching(true);
    try {
      setMaterialHits(await searchMaterials(materialQuery));
    } catch (caught: unknown) {
      window.alert(errorText(caught, 'Could not search materials.'));
    } finally {
      setSearching(false);
    }
  };

  const addMaterial = (material: SilaMaterial) => {
    if (lines.some((line) => line.materialId === material.id)) return;
    setLines((current) => [
      ...current,
      {
        key: `${material.id}-${Date.now()}`,
        materialId: material.id,
        itemCode: material.materialCode,
        itemName: material.description,
        quantity: '1',
        uom: (material.baseUom || 'EA').toUpperCase(),
      },
    ]);
  };

  const buildWrite = (): SilaRecipeWrite => {
    const base = fromId && source.data ? writeFromDetail(source.data) : emptyWrite(productClass);
    const ingredients: SilaRecipeIngredientWrite[] = lines.map((line) => ({
      materialId: line.materialId,
      subRecipeId: null,
      quantity: Number(line.quantity) || 0,
      uom: line.uom || null,
    }));
    return {
      ...base,
      name: name.trim(),
      productClass,
      recipeFormat: base.recipeFormat || 'STANDARD_RECIPE',
      itemMode: 'RECIPE',
      subcategory: subcategory.trim() || null,
      servingQty: Number(servingQty) || 1,
      servingUom: servingUom.trim() || 'EA',
      sellingUom: servingUom.trim() || 'EA',
      preparationSteps: prep.trim() || null,
      ingredients,
      outletPrices: base.outletPrices ?? [],
    };
  };

  const validateStep = (): string | null => {
    if (step === 1) {
      if (!name.trim()) return 'Enter a recipe name.';
      if (!(Number(servingQty) > 0)) return 'Portion size must be greater than zero.';
    }
    if (step === 2) {
      if (lines.length === 0) return 'Add at least one ingredient from Material Master.';
      if (lines.some((line) => !(Number(line.quantity) > 0))) return 'Every ingredient needs a quantity.';
    }
    return null;
  };

  const save = async (submit: boolean) => {
    const issue = validateStep();
    if (issue) {
      window.alert(issue);
      return;
    }
    if (!can.manageRecipe) {
      window.alert('You do not have permission to manage recipes.');
      return;
    }
    setBusy(true);
    try {
      const write = buildWrite();
      let id = fromId;
      if (fromId && source.data && (source.data.status || '').toUpperCase() === 'DRAFT') {
        await updateRecipe(fromId, write);
      } else {
        id = await createRecipe(write);
      }
      if (picture) await setRecipePicture(id, picture);
      if (submit) await submitRecipe(id);
      window.alert(submit ? 'Draft submitted for approval.' : 'Draft saved.');
      go(`recipes/${id}`);
    } catch (caught: unknown) {
      window.alert(errorText(caught, 'Could not save the draft.'));
    } finally {
      setBusy(false);
    }
  };

  if (!can.manageRecipe) {
    return (
      <>
        <ScreenHeader title="Quick draft" eyebrow="Recipe Studio" back backTo="recipes" />
        <div className="sm-screen">
          <Empty text="Not permitted" description="Recipe draft creation needs manage-recipe permission." />
        </div>
      </>
    );
  }

  return (
    <>
      <ScreenHeader title="Quick draft" eyebrow={`Step ${step} of 4`} back backTo="recipes" />
      <div className="sm-screen rs-step">
        {fromId && source.loading && <Loading text="Loading source recipe…" />}
        {fromId && source.error && <ErrorNotice message={source.error} onRetry={source.reload} />}

        {step === 1 && (
          <>
            <h2>
              <span className="rs-step-num">1</span>Basics
            </h2>
            <label className="sm-field">
              Recipe name
              <input className="sm-input" value={name} onChange={(event) => setName(event.target.value)} />
            </label>
            <div className="rs-chips">
              {classes.map((chip) => (
                <button
                  key={chip.id}
                  type="button"
                  className={`rs-chip${productClass === chip.id ? ' rs-chip--on' : ''}`}
                  onClick={() => setProductClass(chip.id as SilaProductClass)}
                >
                  {chip.label}
                </button>
              ))}
            </div>
            <label className="sm-field">
              Subcategory
              <input className="sm-input" value={subcategory} onChange={(event) => setSubcategory(event.target.value)} placeholder="e.g. Main Course" />
            </label>
            <label className="sm-field">
              Portion size
              <input className="sm-input" type="number" value={servingQty} onChange={(event) => setServingQty(event.target.value)} />
            </label>
            <label className="sm-field">
              Serving UOM
              <input className="sm-input" value={servingUom} onChange={(event) => setServingUom(event.target.value)} />
            </label>
            <label className="sm-field">
              Recipe image (optional)
              <input className="sm-input" type="file" accept="image/*" onChange={(event) => setPicture(event.target.files?.[0] ?? null)} />
            </label>
          </>
        )}

        {step === 2 && (
          <>
            <h2>
              <span className="rs-step-num">2</span>Ingredients
            </h2>
            <form
              className="sm-field"
              onSubmit={(event) => {
                event.preventDefault();
                void searchMaterialsNow();
              }}
            >
              <label htmlFor="rs-mat-search">Material Master</label>
              <input
                id="rs-mat-search"
                className="sm-input"
                value={materialQuery}
                placeholder="Code or description"
                onChange={(event) => setMaterialQuery(event.target.value)}
              />
            </form>
            {searching && <Loading text="Searching materials…" />}
            <ul className="sm-list">
              {materialHits.slice(0, 8).map((material) => (
                <li key={material.id}>
                  <button type="button" className="sm-list-btn" onClick={() => addMaterial(material)}>
                    <strong>{material.description}</strong>
                    <span className="sm-meta">
                      {material.materialCode} · {material.baseUom}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
            {lines.length === 0 && <Empty text="No ingredients yet" description="Search Material Master and tap to add." />}
            <ul className="sm-list">
              {lines.map((line) => (
                <li key={line.key} className="sm-list-btn">
                  <span className="sm-row">
                    <strong>{line.itemName}</strong>
                    <button type="button" className="sm-btn" onClick={() => setLines((rows) => rows.filter((row) => row.key !== line.key))}>
                      Remove
                    </button>
                  </span>
                  <div className="rs-scale" style={{ marginTop: '0.35rem' }}>
                    <input
                      className="sm-input"
                      type="number"
                      value={line.quantity}
                      onChange={(event) =>
                        setLines((rows) => rows.map((row) => (row.key === line.key ? { ...row, quantity: event.target.value } : row)))
                      }
                    />
                    <input
                      className="sm-input"
                      value={line.uom}
                      onChange={(event) =>
                        setLines((rows) => rows.map((row) => (row.key === line.key ? { ...row, uom: event.target.value } : row)))
                      }
                    />
                  </div>
                  <span className="sm-meta">{line.itemCode}</span>
                </li>
              ))}
            </ul>
          </>
        )}

        {step === 3 && (
          <>
            <h2>
              <span className="rs-step-num">3</span>Instructions
            </h2>
            <label className="sm-field">
              Preparation / cooking notes
              <textarea className="sm-input" rows={8} value={prep} onChange={(event) => setPrep(event.target.value)} />
            </label>
          </>
        )}

        {step === 4 && (
          <>
            <h2>
              <span className="rs-step-num">4</span>Save
            </h2>
            <p>
              <strong>{name || 'Untitled'}</strong>
            </p>
            <p className="sm-meta">
              {productClass}
              {subcategory ? ` · ${subcategory}` : ''}
              {` · ${servingQty} ${servingUom}`}
              {` · ${lines.length} ingredients`}
            </p>
            <p className="sm-meta">Uses Cloud Recipe Master. Advanced batch / sub-recipe work stays in Cloud.</p>
          </>
        )}

        <div className="rs-sticky">
          {step > 1 && (
            <button type="button" className="sm-btn" disabled={busy} onClick={() => setStep((value) => value - 1)}>
              Back
            </button>
          )}
          {step < 4 && (
            <button
              type="button"
              className="sm-btn sm-btn--primary"
              disabled={busy}
              onClick={() => {
                const issue = validateStep();
                if (issue) {
                  window.alert(issue);
                  return;
                }
                setStep((value) => value + 1);
              }}
            >
              Continue
            </button>
          )}
          {step === 4 && (
            <>
              <button type="button" className="sm-btn" disabled={busy} onClick={() => void save(false)}>
                Save draft
              </button>
              <button type="button" className="sm-btn sm-btn--primary" disabled={busy} onClick={() => void save(true)}>
                Submit for approval
              </button>
            </>
          )}
        </div>
      </div>
    </>
  );
};

export default RecipeDraftWizardScreen;
