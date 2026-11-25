import React, { useState } from 'react';
import {
  identifyStockCountPhoto,
  type SilaStockCountPhotoCandidate,
} from '../../../../remote-buyer/src/api/silaMe/silaStockCountApi';
import { Notice, type NoticeMessage } from '../../components/StateViews';
import { errorText } from '../../useLoad';

interface CountPhotoIdentifyProps {
  stockCountId: string;
  onConfirm: (candidate: SilaStockCountPhotoCandidate) => void;
  onScanBarcodeInstead?: () => void;
}

const pct = (confidence: number | undefined): string => {
  if (confidence == null || Number.isNaN(confidence)) return '';
  return ` · ${Math.round(Math.max(0, Math.min(1, confidence)) * 100)}%`;
};

/**
 * Take Photo — AI Identify via SILA Material Recognition Engine.
 * User must confirm a Material Master candidate before counting. Inventory is not changed.
 */
const CountPhotoIdentify: React.FC<CountPhotoIdentifyProps> = ({ stockCountId, onConfirm, onScanBarcodeInstead }) => {
  const [busy, setBusy] = useState(false);
  const [hint, setHint] = useState('');
  const [notice, setNotice] = useState<NoticeMessage | null>(null);
  const [candidates, setCandidates] = useState<SilaStockCountPhotoCandidate[]>([]);
  const [preferBarcode, setPreferBarcode] = useState(false);

  const run = async (file: File | undefined) => {
    if (!file && !hint.trim()) {
      setNotice({ tone: 'warning', text: 'Take a photo or enter a product hint first.' });
      return;
    }
    setBusy(true);
    setNotice(null);
    setCandidates([]);
    setPreferBarcode(false);
    try {
      const result = await identifyStockCountPhoto(stockCountId, { photo: file ?? null, hintText: hint });
      setCandidates(result.candidates);
      setPreferBarcode(Boolean(result.preferBarcodeFallback));
      setNotice({
        tone: result.configured && result.candidates.length > 0 ? 'success' : 'warning',
        text: result.message || 'Search the material. Inventory was not changed.',
      });
    } catch (caught: unknown) {
      setNotice({ tone: 'error', text: errorText(caught, 'Photo identification is not available. Search the material.') });
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="sm-section" aria-label="Take photo — AI identify" data-testid="count-photo-identify">
      <h2 className="sm-section__title">Take Photo — AI Identify</h2>
      <div className="sm-field">
        <label htmlFor="sm-ai-hint">Product hint (optional)</label>
        <input
          id="sm-ai-hint"
          className="sm-input"
          value={hint}
          placeholder="Brand, name, or size"
          onChange={(event) => setHint(event.target.value)}
        />
      </div>
      <label className="sm-btn sm-btn--primary sm-btn--block">
        {busy ? 'Recognising…' : 'Take Photo — AI Identify'}
        <input
          type="file"
          accept="image/*"
          capture="environment"
          hidden
          disabled={busy}
          onChange={(event) => {
            const file = event.target.files?.[0];
            event.target.value = '';
            void run(file);
          }}
        />
      </label>
      {!busy && hint.trim() && (
        <button type="button" className="sm-btn sm-btn--block" onClick={() => void run(undefined)}>
          Match hint without photo
        </button>
      )}
      <Notice notice={notice} />
      {preferBarcode && onScanBarcodeInstead && (
        <button type="button" className="sm-btn sm-btn--secondary sm-btn--block" onClick={onScanBarcodeInstead}>
          Scan Barcode Instead
        </button>
      )}
      {candidates.map((candidate) => (
        <button
          key={candidate.materialId}
          type="button"
          className="sm-btn sm-btn--primary sm-btn--block"
          onClick={() => onConfirm(candidate)}
        >
          Confirm {candidate.materialName}
          <span className="sm-meta">
            {' '}
            Material {candidate.materialCode}
            {pct(candidate.confidence)}
            {candidate.category ? ` · ${candidate.category}` : ''}
          </span>
        </button>
      ))}
    </section>
  );
};

export default CountPhotoIdentify;
