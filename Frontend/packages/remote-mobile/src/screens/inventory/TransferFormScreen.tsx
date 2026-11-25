import React, { useEffect, useState } from 'react';
import { useLocation, useSearchParams } from 'react-router-dom';
import {
  formatQty,
  getLiveInventoryDetail,
  getLocations,
  searchLiveInventory,
  type SilaLiveInventoryLocation,
} from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import { createQuickTransfer, createTransfer, type SilaTransferWrite } from '../../../../remote-buyer/src/api/silaMe/silaMovementsApi';
import BarcodeScanner from '../../components/BarcodeScanner';
import LocationPicker from '../../components/LocationPicker';
import MaterialPicker, { materialUoms } from '../../components/MaterialPicker';
import QtyInput, { parseQty } from '../../components/QtyInput';
import ScreenHeader from '../../components/ScreenHeader';
import { ErrorNotice, Loading, Notice, type NoticeMessage } from '../../components/StateViews';
import { useMyLocation } from '../../location';
import { useGo } from '../../navigation';
import { errorText, useLoad } from '../../useLoad';
import { isTransferPrefill } from './transferPrefill';

interface FormLine {
  key: string;
  materialId: string;
  label: string;
  uoms: string[];
  uom: string;
  qty: string;
}

/**
 * Quick / standard transfer. Quick mode: scan material → check stock → recommended outlets → select source.
 * Reuses existing transfer APIs only.
 */
const TransferFormScreen: React.FC = () => {
  const go = useGo();
  const [params] = useSearchParams();
  const quick = params.get('mode') === 'quick';
  const [alreadyCollected, setAlreadyCollected] = useState<boolean>(() => quick && params.get('collected') === '1');
  const routerState: unknown = useLocation().state;
  const prefill = isTransferPrefill(routerState) ? routerState : null;
  const { locations: myLocations, location: myLocation } = useMyLocation();
  const all = useLoad(getLocations, 'all-locations');
  const [chosenFromId, setFromId] = useState<string>(prefill?.fromLocationId ?? '');
  const [chosenToId, setToId] = useState<string>(prefill?.toLocationId ?? '');
  const fromId = chosenFromId;
  const toId = chosenToId || myLocation?.id || '';
  const [requiredBy, setRequiredBy] = useState<string>('');
  const [reason, setReason] = useState<string>(prefill?.reason ?? '');
  const [lines, setLines] = useState<FormLine[]>(() =>
    prefill?.material
      ? [
          {
            key: prefill.material.id,
            materialId: prefill.material.id,
            label: `${prefill.material.description} (${prefill.material.materialCode})`,
            uoms: [prefill.material.baseUom],
            uom: prefill.material.baseUom,
            qty: prefill.quantity ? String(prefill.quantity) : '',
          },
        ]
      : [],
  );
  const [saving, setSaving] = useState(false);
  const [notice, setNotice] = useState<NoticeMessage | null>(null);
  const [scannerOpen, setScannerOpen] = useState(false);
  const [scanBusy, setScanBusy] = useState(false);
  const [nearby, setNearby] = useState<SilaLiveInventoryLocation[]>([]);
  const [localOnHand, setLocalOnHand] = useState<string | null>(null);

  const transferable = (all.data ?? []).filter((location) => location.transferEnabled);
  const myIds = myLocations.map((location) => location.id);
  const toOptions = quick ? transferable : transferable.filter((location) => myIds.includes(location.id));
  const fromOptions = transferable.filter((location) => location.id !== toId);

  const primaryMaterialId = lines[0]?.materialId ?? '';

  useEffect(() => {
    if (!quick || !primaryMaterialId || !toId) {
      setNearby([]);
      setLocalOnHand(null);
      return;
    }
    let cancelled = false;
    void (async () => {
      try {
        const detail = await getLiveInventoryDetail(primaryMaterialId, {
          currentLocationId: toId,
          requiredQty: parseQty(lines[0]?.qty) || undefined,
        });
        if (cancelled) return;
        const here = detail.locations.find((row) => row.locationId === toId);
        setLocalOnHand(here ? `${formatQty(here.onHandQty)} ${here.baseUom}` : null);
        setNearby(
          detail.locations
            .filter((row) => row.locationId !== toId && row.onHandQty > 0 && row.transferEnabled !== false)
            .sort((a, b) => b.onHandQty - a.onHandQty)
            .slice(0, 6),
        );
      } catch {
        if (!cancelled) {
          setNearby([]);
          setLocalOnHand(null);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [quick, primaryMaterialId, toId, lines]);

  const updateLine = (key: string, change: Partial<FormLine>) =>
    setLines((current) => current.map((line) => (line.key === key ? { ...line, ...change } : line)));

  const addMaterial = (materialId: string, label: string, uoms: string[], uom: string, qty = '') => {
    if (lines.some((line) => line.materialId === materialId)) {
      setNotice({ tone: 'warning', text: `${label} is already in the list.` });
      return;
    }
    setNotice(null);
    setLines((current) => [...current, { key: materialId, materialId, label, uoms, uom, qty }]);
  };

  const resolveBarcode = async (raw: string) => {
    const code = raw.trim();
    if (!code) return;
    setScanBusy(true);
    setNotice(null);
    try {
      const rows = await searchLiveInventory(code, toId || myLocation?.id || '', 0, null);
      const row = rows[0];
      if (!row) {
        setNotice({ tone: 'error', text: 'BARCODE NOT MAPPED. Search the material.' });
        return;
      }
      addMaterial(
        row.materialId,
        `${row.description || row.materialCode} (${row.materialCode})`,
        [row.baseUom],
        row.baseUom,
      );
      setNotice({ tone: 'success', text: `Found ${row.materialCode}. Check stock and pick a source.` });
    } catch (caught: unknown) {
      setNotice({ tone: 'error', text: errorText(caught, 'Could not resolve the barcode.') });
    } finally {
      setScanBusy(false);
    }
  };

  const submit = async () => {
    setNotice(null);
    if (!fromId || !toId || fromId === toId) {
      setNotice({ tone: 'error', text: 'Choose two different locations.' });
      return;
    }
    if (lines.length === 0) {
      setNotice({ tone: 'error', text: 'Add at least one material.' });
      return;
    }
    const bad = lines.find((line) => !parseQty(line.qty));
    if (bad) {
      setNotice({ tone: 'error', text: `Enter a quantity above zero for ${bad.label}.` });
      return;
    }
    const payload: SilaTransferWrite = {
      fromLocationId: fromId,
      toLocationId: toId,
      reason: reason.trim() || null,
      requiredBy: quick ? null : requiredBy || null,
      items: lines.map((line) => ({ materialId: line.materialId, quantity: parseQty(line.qty) ?? 0, uom: line.uom || null })),
      ...(quick ? { alreadyCollected } : {}),
    };
    setSaving(true);
    try {
      const id = quick ? await createQuickTransfer(payload) : await createTransfer(payload);
      go(id ? `inventory/transfers/${id}` : 'inventory/transfers', { replace: true });
    } catch (caught: unknown) {
      setNotice({ tone: 'error', text: errorText(caught) });
      setSaving(false);
    }
  };

  return (
    <>
      <ScreenHeader
        title={quick ? (alreadyCollected ? 'Record Quick Transfer' : 'Quick Transfer') : 'Request transfer'}
        eyebrow={quick ? 'Inventory' : 'ITO'}
        back
        intro={
          quick
            ? 'Scan material, check stock, pick a recommended source, then transfer.'
            : 'The source location approves and dispatches; you receive it.'
        }
      />
      <div className="sm-screen" data-testid="mobile-transfer-form">
        {quick && (
          <label className="sm-check">
            <input type="checkbox" checked={alreadyCollected} onChange={(event) => setAlreadyCollected(event.target.checked)} />
            Already collected (I took the stock from the source)
          </label>
        )}
        {all.loading && <Loading text="Loading locations…" />}
        {all.error && <ErrorNotice message={all.error} onRetry={all.reload} />}
        {all.data && (
          <>
            <LocationPicker label="From" locations={fromOptions} value={fromId} onChange={setFromId} placeholder="Choose source" />
            <LocationPicker label={quick ? 'To (defaults to mine)' : 'To'} locations={toOptions} value={toId} onChange={setToId} placeholder="Choose destination" />
          </>
        )}
        {!quick && (
          <div className="sm-field">
            <label htmlFor="sm-required-by">Required by (optional)</label>
            <input id="sm-required-by" className="sm-input" type="date" value={requiredBy} onChange={(event) => setRequiredBy(event.target.value)} />
          </div>
        )}
        <div className="sm-field">
          <label htmlFor="sm-reason">Reason (optional)</label>
          <input id="sm-reason" className="sm-input" value={reason} onChange={(event) => setReason(event.target.value)} />
        </div>

        <section className="sm-section" aria-labelledby="sm-lines">
          <div className="sm-row">
            <h2 id="sm-lines">Materials ({lines.length})</h2>
            {quick && (
              <button type="button" className="sm-btn sm-btn--secondary" disabled={scanBusy} onClick={() => setScannerOpen(true)} data-testid="transfer-scan-barcode">
                {scanBusy ? '…' : 'Scan material'}
              </button>
            )}
          </div>
          {localOnHand && (
            <p className="sm-meta">On hand at destination: {localOnHand}</p>
          )}
          {lines.map((line) => (
            <div key={line.key} className="sm-card">
              <div className="sm-row">
                <strong>{line.label}</strong>
                <button
                  type="button"
                  className="sm-btn sm-btn--small sm-btn--danger"
                  aria-label={`Remove ${line.label}`}
                  onClick={() => setLines((current) => current.filter((item) => item.key !== line.key))}
                >
                  Remove
                </button>
              </div>
              <QtyInput
                label="Quantity"
                value={line.qty}
                onChange={(qty) => updateLine(line.key, { qty })}
                uoms={line.uoms}
                uom={line.uom}
                onUomChange={(uom) => updateLine(line.key, { uom })}
              />
            </div>
          ))}
          <MaterialPicker
            label="Add material"
            onPick={(material) => {
              addMaterial(
                material.id,
                `${material.description} (${material.materialCode})`,
                materialUoms(material),
                material.baseUom,
              );
            }}
          />
        </section>

        {quick && nearby.length > 0 && (
          <section className="sm-section" aria-label="Recommended sources" data-testid="transfer-recommended-sources">
            <h2>Recommended sources</h2>
            <p className="sm-meta">Stores / outlets with available stock for the scanned material.</p>
            <ul className="sm-list">
              {nearby.map((row) => (
                <li key={row.locationId}>
                  <button
                    type="button"
                    className="sm-list-btn"
                    onClick={() => {
                      setFromId(row.locationId);
                      setNotice({ tone: 'success', text: `Source set to ${row.locationName}.` });
                    }}
                  >
                    <span className="sm-row">
                      <span>
                        <strong>{row.locationName}</strong>
                        <span className="sm-meta">
                          {' '}
                          {formatQty(row.onHandQty)} {row.baseUom}
                          {row.propertyName ? ` · ${row.propertyName}` : ''}
                        </span>
                      </span>
                      <span className="sm-meta">Use</span>
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </section>
        )}

        <Notice notice={notice} />
        <button type="button" className="sm-btn sm-btn--primary sm-btn--block" disabled={saving} onClick={submit}>
          {saving ? 'Sending…' : quick ? (alreadyCollected ? 'Submit already collected' : 'Request quick transfer') : 'Submit'}
        </button>
      </div>

      <BarcodeScanner
        open={scannerOpen}
        title="Scan material for transfer"
        onClose={() => setScannerOpen(false)}
        onDetected={(scanned) => {
          void resolveBarcode(scanned.rawValue);
        }}
      />
    </>
  );
};

export default TransferFormScreen;
