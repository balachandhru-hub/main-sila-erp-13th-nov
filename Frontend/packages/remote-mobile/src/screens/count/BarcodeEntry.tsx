import React, { useState } from 'react';
import { identifyBarcode, type SilaStockCountBarcode } from '../../../../remote-buyer/src/api/silaMe/silaStockCountApi';
import BarcodeScanner from '../../components/BarcodeScanner';
import { errorText } from '../../useLoad';

interface BarcodeEntryProps {
  stockCountId: string;
  onFound: (found: SilaStockCountBarcode) => void;
  /** When true, open the camera scanner immediately (e.g. from AI fallback). */
  forceOpenScanner?: boolean;
  onScannerClosed?: () => void;
}

/** Camera barcode scan + keyboard-wedge / typed entry. Resolves via Material Master only. */
const BarcodeEntry: React.FC<BarcodeEntryProps> = ({ stockCountId, onFound, forceOpenScanner = false, onScannerClosed }) => {
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [scannerOpen, setScannerOpen] = useState(forceOpenScanner);

  const resolve = async (raw: string) => {
    const value = raw.trim();
    if (!value) return;
    setBusy(true);
    setError(null);
    try {
      const found = await identifyBarcode(stockCountId, value);
      setCode('');
      onFound(found);
    } catch (caught: unknown) {
      setError(errorText(caught, 'BARCODE NOT MAPPED. Search the material.'));
    } finally {
      setBusy(false);
    }
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    await resolve(code);
  };

  return (
    <section className="sm-section" aria-label="Scan barcode" data-testid="count-barcode-entry">
      <h2 className="sm-section__title">Scan Barcode</h2>
      <button
        type="button"
        className="sm-btn sm-btn--primary sm-btn--block"
        onClick={() => setScannerOpen(true)}
        disabled={busy}
      >
        Open camera scanner
      </button>
      <form onSubmit={submit}>
        <div className="sm-field">
          <label htmlFor="sm-barcode">Or type / wedge barcode</label>
          <div className="sm-row">
            <input
              id="sm-barcode"
              className="sm-input"
              inputMode="text"
              autoComplete="off"
              value={code}
              onChange={(event) => setCode(event.target.value)}
            />
            <button type="submit" className="sm-btn sm-btn--primary" disabled={busy || !code.trim()}>
              {busy ? '…' : 'Find'}
            </button>
          </div>
        </div>
      </form>
      {error && (
        <p className="sm-notice sm-notice--error" role="alert">
          {error}
        </p>
      )}
      <BarcodeScanner
        open={scannerOpen}
        title="Scan material barcode"
        onClose={() => {
          setScannerOpen(false);
          onScannerClosed?.();
        }}
        onDetected={(scanned) => {
          void resolve(scanned.rawValue);
        }}
      />
    </section>
  );
};

export default BarcodeEntry;
