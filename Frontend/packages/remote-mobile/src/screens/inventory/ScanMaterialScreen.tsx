import React, { useEffect, useMemo, useRef, useState } from 'react';
import {
  createLiveStockCount,
  formatQty,
  getLiveInventoryDetail,
  getLocations,
  searchLiveInventory,
  type SilaLiveInventoryRow,
} from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import {
  createAdjustment,
  createQuickTransfer,
  getTransfer,
  getTransfers,
  reverseTransfer,
  type SilaTransferDetail,
  type SilaTransferListItem,
} from '../../../../remote-buyer/src/api/silaMe/silaMovementsApi';
import {
  createStockCount,
  identifyStockCountPhoto,
  type SilaStockCountPhotoCandidate,
} from '../../../../remote-buyer/src/api/silaMe/silaStockCountApi';
import BarcodeScanner from '../../components/BarcodeScanner';
import LocationPicker from '../../components/LocationPicker';
import QtyInput, { parseQty } from '../../components/QtyInput';
import ScreenHeader from '../../components/ScreenHeader';
import { ErrorNotice, Loading, Notice, type NoticeMessage } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useMyLocation } from '../../location';
import { useGo } from '../../navigation';
import { errorText, useLoad } from '../../useLoad';
import './liveInventoryMobile.css';

type ScanAction = 'QUICK_ITO' | 'COUNT' | 'DAMAGE' | 'ITO_REVERSAL';

interface ResolvedMaterial {
  materialId: string;
  materialCode: string;
  description: string;
  baseUom: string;
  onHandQty: number;
}

const ACTIONS: Array<{
  id: ScanAction;
  label: string;
  detail: string;
  visible: (can: ReturnType<typeof useAccess>['can']) => boolean;
}> = [
  { id: 'QUICK_ITO', label: 'Quick ITO', detail: 'Move stock to another outlet', visible: (can) => can.manageTransfer },
  { id: 'COUNT', label: 'Count', detail: 'Post physical qty vs book', visible: (can) => can.manageCount || can.initiateCount },
  { id: 'DAMAGE', label: 'Damage', detail: 'Write off damaged stock', visible: (can) => can.postAdjustment },
  { id: 'ITO_REVERSAL', label: 'ITO reversal', detail: 'Reverse a received transfer', visible: (can) => can.manageTransfer },
];

const today = (): string => {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
};

const fromLiveRow = (row: SilaLiveInventoryRow): ResolvedMaterial => ({
  materialId: row.materialId,
  materialCode: row.materialCode,
  description: row.description || row.materialCode,
  baseUom: row.baseUom,
  onHandQty: row.onHandQty,
});

/**
 * Premium Quick Action: scan barcode or photo → material → Quick ITO / Count / Damage / ITO reversal → submit.
 * Source defaults to the signed-in location; destination is chosen for Quick ITO.
 */
const ScanMaterialScreen: React.FC = () => {
  const go = useGo();
  const { can } = useAccess();
  const { location: myLocation, locations: myLocations } = useMyLocation();
  const all = useLoad(getLocations, 'scan-all-locations');
  const photoRef = useRef<HTMLInputElement>(null);

  const [scannerOpen, setScannerOpen] = useState(false);
  const [manual, setManual] = useState('');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<NoticeMessage | null>(null);
  const [material, setMaterial] = useState<ResolvedMaterial | null>(null);
  const [candidates, setCandidates] = useState<SilaStockCountPhotoCandidate[]>([]);
  const [photoCountId, setPhotoCountId] = useState<string | null>(null);

  const [action, setAction] = useState<ScanAction | null>(null);
  const [sourceId, setSourceId] = useState(myLocation?.id ?? '');
  const [destinationId, setDestinationId] = useState('');
  const [qty, setQty] = useState('');
  const [reason, setReason] = useState('');
  const [saving, setSaving] = useState(false);

  const [reversals, setReversals] = useState<Array<{ list: SilaTransferListItem; detail: SilaTransferDetail; itemId: string; max: number }>>([]);
  const [reversalId, setReversalId] = useState('');
  const [reversalsLoading, setReversalsLoading] = useState(false);

  useEffect(() => {
    if (myLocation?.id && !sourceId) setSourceId(myLocation.id);
  }, [myLocation?.id, sourceId]);

  const transferable = useMemo(
    () => (all.data ?? []).filter((row) => row.transferEnabled),
    [all.data],
  );
  const destinationOptions = transferable.filter((row) => row.id !== sourceId);
  const sourceOptions = myLocations.length > 0 ? myLocations : transferable;
  const visibleActions = ACTIONS.filter((item) => item.visible(can));

  const resolveCode = async (raw: string) => {
    const code = raw.trim();
    if (!code) return;
    setBusy(true);
    setNotice(null);
    setCandidates([]);
    try {
      const rows = await searchLiveInventory(code, sourceId || myLocation?.id || null, 0, null);
      const row = rows[0];
      if (!row) {
        setMaterial(null);
        setNotice({ tone: 'error', text: 'BARCODE NOT MAPPED. Try another scan or search.' });
        return;
      }
      setMaterial(fromLiveRow(row));
      setAction(null);
      setQty('');
      setReason('');
      setReversalId('');
      setNotice({ tone: 'success', text: `Found ${row.materialCode}. Choose what to do next.` });
    } catch (caught: unknown) {
      setNotice({ tone: 'error', text: errorText(caught, 'Could not resolve the barcode.') });
    } finally {
      setBusy(false);
    }
  };

  const decodeStill = async (file: File): Promise<string | null> => {
    if (typeof window === 'undefined' || !('BarcodeDetector' in window)) return null;
    try {
      const Detector = (window as unknown as { BarcodeDetector: new (opts?: { formats?: string[] }) => { detect: (bmp: ImageBitmap) => Promise<Array<{ rawValue: string }>> } }).BarcodeDetector;
      const detector = new Detector({
        formats: ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'code_39', 'itf', 'qr_code'],
      });
      const bitmap = await createImageBitmap(file);
      const hits = await detector.detect(bitmap);
      bitmap.close();
      return hits[0]?.rawValue?.trim() || null;
    } catch {
      return null;
    }
  };

  const onPhoto = async (file: File | undefined) => {
    if (!file) return;
    setBusy(true);
    setNotice(null);
    setCandidates([]);
    try {
      const decoded = await decodeStill(file);
      if (decoded) {
        await resolveCode(decoded);
        return;
      }

      const locationId = sourceId || myLocation?.id;
      if (!locationId || !(can.manageCount || can.initiateCount)) {
        setNotice({
          tone: 'warning',
          text: 'No barcode on the photo. Type the code, or use AI Identify when you can start a stock count.',
        });
        return;
      }

      let countId = photoCountId;
      if (!countId) {
        countId = await createStockCount({
          locationId,
          countType: 'ADHOC',
          blindCount: false,
          businessDate: today(),
        });
        setPhotoCountId(countId);
      }
      const result = await identifyStockCountPhoto(countId, { photo: file });
      setCandidates(result.candidates);
      setNotice({
        tone: result.candidates.length > 0 ? 'success' : 'warning',
        text: result.message || (result.candidates.length > 0 ? 'Confirm the material.' : 'No match. Scan the barcode instead.'),
      });
    } catch (caught: unknown) {
      setNotice({ tone: 'error', text: errorText(caught, 'Photo identify failed. Scan the barcode instead.') });
    } finally {
      setBusy(false);
    }
  };

  const pickCandidate = async (candidate: SilaStockCountPhotoCandidate) => {
    setBusy(true);
    setNotice(null);
    try {
      const rows = await searchLiveInventory(candidate.materialCode, sourceId || myLocation?.id || null, 0, null);
      const row = rows.find((item) => item.materialId === candidate.materialId) ?? rows[0];
      if (row) {
        setMaterial(fromLiveRow(row));
      } else {
        setMaterial({
          materialId: candidate.materialId,
          materialCode: candidate.materialCode,
          description: candidate.materialName,
          baseUom: candidate.baseUom,
          onHandQty: 0,
        });
      }
      setCandidates([]);
      setAction(null);
      setNotice({ tone: 'success', text: `Selected ${candidate.materialCode}. Choose what to do next.` });
    } catch (caught: unknown) {
      setNotice({ tone: 'error', text: errorText(caught, 'Could not load stock for that material.') });
    } finally {
      setBusy(false);
    }
  };

  useEffect(() => {
    if (action !== 'ITO_REVERSAL' || !material) {
      setReversals([]);
      return;
    }
    let cancelled = false;
    setReversalsLoading(true);
    void (async () => {
      try {
        const lists = await Promise.all([getTransfers('completed', { limit: 40 }), getTransfers('mine', { limit: 40 })]);
        const seen = new Set<string>();
        const unique = lists.flat().filter((row) => {
          if (seen.has(row.id)) return false;
          seen.add(row.id);
          return row.status === 'RECEIVED' || row.status === 'DISCREPANCY';
        });
        const matches: Array<{ list: SilaTransferListItem; detail: SilaTransferDetail; itemId: string; max: number }> = [];
        for (const row of unique.slice(0, 20)) {
          const detail = await getTransfer(row.id);
          const line = detail.items.find((item) => item.materialId === material.materialId);
          if (!line) continue;
          const max = line.remainingReversibleQty ?? Math.max(0, (line.receivedQty || line.dispatchedQty) - (line.reversedQty ?? 0));
          if (max <= 0) continue;
          if (!detail.allowedActions.map((a) => a.toUpperCase()).includes('REVERSE')) continue;
          matches.push({ list: row, detail, itemId: line.id, max });
        }
        if (!cancelled) {
          setReversals(matches);
          setReversalId(matches[0]?.list.id ?? '');
          if (matches[0]) setQty(String(matches[0].max));
        }
      } catch (caught: unknown) {
        if (!cancelled) setNotice({ tone: 'error', text: errorText(caught, 'Could not load reversible ITOs.') });
      } finally {
        if (!cancelled) setReversalsLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [action, material]);

  useEffect(() => {
    if (!material || !sourceId || action === 'ITO_REVERSAL') return;
    let cancelled = false;
    void (async () => {
      try {
        const detail = await getLiveInventoryDetail(material.materialId, { currentLocationId: sourceId });
        const here = detail.locations.find((row) => row.locationId === sourceId);
        if (!cancelled && here) {
          setMaterial((current) => (current ? { ...current, onHandQty: here.onHandQty } : current));
        }
      } catch {
        /* keep scanned on-hand */
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [material?.materialId, sourceId, action]);

  const selectedReversal = reversals.find((row) => row.list.id === reversalId) ?? null;

  const submit = async () => {
    if (!material || !action) return;
    setNotice(null);
    const quantity = parseQty(qty);
    if (action !== 'ITO_REVERSAL' && (quantity == null || quantity <= 0)) {
      setNotice({ tone: 'error', text: 'Enter a quantity above zero.' });
      return;
    }
    if (!sourceId) {
      setNotice({ tone: 'error', text: 'Choose the source location.' });
      return;
    }

    setSaving(true);
    try {
      if (action === 'QUICK_ITO') {
        if (!destinationId || destinationId === sourceId) {
          setNotice({ tone: 'error', text: 'Pick a different destination.' });
          setSaving(false);
          return;
        }
        const id = await createQuickTransfer({
          fromLocationId: sourceId,
          toLocationId: destinationId,
          reason: reason.trim() || null,
          requiredBy: null,
          items: [{ materialId: material.materialId, quantity: quantity!, uom: material.baseUom }],
          alreadyCollected: false,
        });
        go(id ? `inventory/transfers/${id}` : 'inventory/transfers', { replace: true });
        return;
      }

      if (action === 'COUNT') {
        const result = await createLiveStockCount({
          locationId: sourceId,
          materialId: material.materialId,
          physicalQty: quantity!,
          uom: material.baseUom,
          reason: reason.trim() || 'Scan material count',
        });
        setNotice({ tone: 'success', text: result.message || `Posted ${result.adjustmentNumber}.` });
        setSaving(false);
        go(`inventory/adjustments/${result.adjustmentId}`, { replace: true });
        return;
      }

      if (action === 'DAMAGE') {
        if (!reason.trim()) {
          setNotice({ tone: 'error', text: 'Enter the damage reason.' });
          setSaving(false);
          return;
        }
        const id = await createAdjustment({
          locationId: sourceId,
          adjustmentType: 'DAMAGE',
          reason: reason.trim(),
          items: [{ materialId: material.materialId, quantity: quantity!, uom: material.baseUom, unitCost: null }],
        });
        go(id ? `inventory/adjustments/${id}` : 'inventory/adjustments', { replace: true });
        return;
      }

      if (action === 'ITO_REVERSAL') {
        if (!selectedReversal) {
          setNotice({ tone: 'error', text: 'Pick a received ITO to reverse.' });
          setSaving(false);
          return;
        }
        if (!reason.trim()) {
          setNotice({ tone: 'error', text: 'Enter the reversal reason.' });
          setSaving(false);
          return;
        }
        const reverseQty = parseQty(qty);
        if (reverseQty == null || reverseQty <= 0 || reverseQty > selectedReversal.max) {
          setNotice({ tone: 'error', text: `Quantity must be between 0 and ${selectedReversal.max}.` });
          setSaving(false);
          return;
        }
        const result = await reverseTransfer(selectedReversal.list.id, {
          reason: reason.trim(),
          items: [{ itemId: selectedReversal.itemId, quantity: reverseQty }],
        });
        setNotice({ tone: 'success', text: result.message });
        go(`inventory/transfers/${selectedReversal.list.id}`, { replace: true });
        return;
      }
    } catch (caught: unknown) {
      setNotice({ tone: 'error', text: errorText(caught) });
      setSaving(false);
    }
  };

  const resetMaterial = () => {
    setMaterial(null);
    setAction(null);
    setCandidates([]);
    setQty('');
    setReason('');
    setDestinationId('');
    setReversalId('');
    setNotice(null);
  };

  return (
    <>
      <ScreenHeader
        title="Scan material"
        eyebrow="Quick action"
        back
        intro="Scan a barcode or photo, confirm the material, then choose Quick ITO, Count, Damage, or ITO reversal."
      />
      <div className="sm-screen liv-m liv-m--premium liv-m-scan" data-testid="mobile-scan-material">
        <section className="liv-m-scan__capture" aria-label="Capture">
          <div className="liv-m-scan__modes">
            <button type="button" className="liv-m-scan__mode liv-m-scan__mode--primary" disabled={busy} onClick={() => setScannerOpen(true)}>
              <strong>Barcode</strong>
              <span>Live camera scan</span>
            </button>
            <button
              type="button"
              className="liv-m-scan__mode"
              disabled={busy}
              onClick={() => photoRef.current?.click()}
            >
              <strong>Photo</strong>
              <span>Decode or AI identify</span>
            </button>
            <input
              ref={photoRef}
              type="file"
              accept="image/*"
              capture="environment"
              hidden
              onChange={(event) => {
                const file = event.target.files?.[0];
                event.target.value = '';
                void onPhoto(file);
              }}
            />
          </div>
          <div className="liv-m-scan__manual">
            <input
              className="sm-input"
              type="search"
              enterKeyHint="search"
              placeholder="Type barcode or material code"
              value={manual}
              disabled={busy}
              onChange={(event) => setManual(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') void resolveCode(manual);
              }}
            />
            <button type="button" className="sm-btn sm-btn--primary" disabled={busy || !manual.trim()} onClick={() => void resolveCode(manual)}>
              {busy ? '…' : 'Find'}
            </button>
          </div>
        </section>

        <Notice notice={notice} />

        {candidates.length > 0 && (
          <section className="liv-m-scan__candidates" aria-label="Photo matches">
            <h2>Confirm material</h2>
            {candidates.map((candidate) => (
              <button key={candidate.materialId} type="button" className="liv-m-card" onClick={() => void pickCandidate(candidate)}>
                <strong>
                  {candidate.materialName} ({candidate.materialCode})
                </strong>
                <span className="sm-meta">
                  {candidate.baseUom}
                  {candidate.confidence != null ? ` · ${Math.round(candidate.confidence * 100)}%` : ''}
                </span>
              </button>
            ))}
          </section>
        )}

        {material && (
          <>
            <section className="liv-m-scan__material" aria-label="Material">
              <div className="liv-m-scan__hero">
                <p className="liv-m-kicker">Material</p>
                <strong className="liv-m-outlet__name">{material.description}</strong>
                <span className="sm-meta">{material.materialCode}</span>
                <p className="liv-m-card__qty">
                  On hand {formatQty(material.onHandQty)} {material.baseUom}
                </p>
                <button type="button" className="sm-btn sm-btn--secondary sm-btn--small" onClick={resetMaterial}>
                  Scan another
                </button>
              </div>
            </section>

            <section className="liv-m-scan__actions" aria-labelledby="liv-m-scan-actions">
              <h2 id="liv-m-scan-actions">What do you need?</h2>
              <div className="liv-m-quick__grid">
                {visibleActions.map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    className={`liv-m-action${action === item.id ? ' liv-m-action--on' : ''}`}
                    aria-pressed={action === item.id}
                    onClick={() => {
                      setAction(item.id);
                      setNotice(null);
                      if (item.id !== 'ITO_REVERSAL') setQty('');
                    }}
                  >
                    <strong>{item.label}</strong>
                    <span>{item.detail}</span>
                  </button>
                ))}
              </div>
              {visibleActions.length === 0 && <ErrorNotice message="No inventory actions are available for your role." />}
            </section>

            {action && (
              <section className="liv-m-scan__form" aria-label="Submit details">
                <LocationPicker
                  label="Source (default)"
                  locations={sourceOptions}
                  value={sourceId}
                  onChange={setSourceId}
                  placeholder="Choose source"
                />

                {action === 'QUICK_ITO' && (
                  <>
                    {all.loading && <Loading text="Loading destinations…" />}
                    <LocationPicker
                      label="Destination"
                      locations={destinationOptions}
                      value={destinationId}
                      onChange={setDestinationId}
                      placeholder="Choose destination"
                    />
                    <QtyInput label="Quantity" value={qty} onChange={setQty} uom={material.baseUom} uoms={[material.baseUom]} />
                    <div className="sm-field">
                      <label htmlFor="scan-ito-reason">Reason (optional)</label>
                      <input id="scan-ito-reason" className="sm-input" value={reason} onChange={(event) => setReason(event.target.value)} />
                    </div>
                  </>
                )}

                {action === 'COUNT' && (
                  <>
                    <QtyInput label="Physical quantity" value={qty} onChange={setQty} uom={material.baseUom} uoms={[material.baseUom]} />
                    <p className="sm-meta">Book on hand: {formatQty(material.onHandQty)} {material.baseUom}. Variance posts as Z01/Z02.</p>
                    <div className="sm-field">
                      <label htmlFor="scan-count-reason">Note (optional)</label>
                      <input id="scan-count-reason" className="sm-input" value={reason} onChange={(event) => setReason(event.target.value)} />
                    </div>
                  </>
                )}

                {action === 'DAMAGE' && (
                  <>
                    <QtyInput label="Damaged quantity" value={qty} onChange={setQty} uom={material.baseUom} uoms={[material.baseUom]} />
                    <div className="sm-field">
                      <label htmlFor="scan-damage-reason">Reason</label>
                      <input id="scan-damage-reason" className="sm-input" value={reason} maxLength={500} onChange={(event) => setReason(event.target.value)} />
                    </div>
                  </>
                )}

                {action === 'ITO_REVERSAL' && (
                  <>
                    {reversalsLoading && <Loading text="Finding reversible ITOs…" />}
                    {!reversalsLoading && reversals.length === 0 && (
                      <p className="sm-meta">No received ITOs with remaining reversible qty for this material.</p>
                    )}
                    {reversals.length > 0 && (
                      <div className="sm-field">
                        <label htmlFor="scan-ito-pick">ITO</label>
                        <select
                          id="scan-ito-pick"
                          className="sm-select"
                          value={reversalId}
                          onChange={(event) => {
                            const id = event.target.value;
                            setReversalId(id);
                            const match = reversals.find((row) => row.list.id === id);
                            if (match) setQty(String(match.max));
                          }}
                        >
                          {reversals.map((row) => (
                            <option key={row.list.id} value={row.list.id}>
                              {row.list.itoNumber} · max {formatQty(row.max)} · {row.list.fromLocationName} → {row.list.toLocationName}
                            </option>
                          ))}
                        </select>
                      </div>
                    )}
                    <QtyInput
                      label="Reverse quantity"
                      value={qty}
                      onChange={setQty}
                      uom={material.baseUom}
                      uoms={[material.baseUom]}
                      max={selectedReversal?.max}
                    />
                    <div className="sm-field">
                      <label htmlFor="scan-rev-reason">Reversal reason</label>
                      <input id="scan-rev-reason" className="sm-input" value={reason} onChange={(event) => setReason(event.target.value)} />
                    </div>
                  </>
                )}

                <button
                  type="button"
                  className="sm-btn sm-btn--primary sm-btn--block liv-m-scan__submit"
                  disabled={saving || (action === 'ITO_REVERSAL' && !selectedReversal)}
                  onClick={() => void submit()}
                >
                  {saving ? 'Submitting…' : 'Submit'}
                </button>
              </section>
            )}
          </>
        )}
      </div>

      <BarcodeScanner
        open={scannerOpen}
        title="Scan material barcode"
        onClose={() => setScannerOpen(false)}
        onDetected={(scanned) => {
          setScannerOpen(false);
          void resolveCode(scanned.rawValue);
        }}
      />
    </>
  );
};

export default ScanMaterialScreen;
