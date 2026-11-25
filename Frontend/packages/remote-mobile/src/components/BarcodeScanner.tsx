import React, { useEffect, useRef, useState } from 'react';

export type ScannedBarcode = {
  rawValue: string;
  format: string;
};

type BarcodeDetectorLike = {
  detect: (source: ImageBitmap | HTMLVideoElement | HTMLCanvasElement) => Promise<Array<{ rawValue: string; format?: string }>>;
};

interface BarcodeScannerProps {
  open: boolean;
  onClose: () => void;
  onDetected: (code: ScannedBarcode) => void;
  title?: string;
}

const FORMATS = [
  'ean_13',
  'ean_8',
  'upc_a',
  'upc_e',
  'code_128',
  'code_39',
  'itf',
  'qr_code',
];

const detectorSupported = (): boolean => typeof window !== 'undefined' && 'BarcodeDetector' in window;

/**
 * Camera barcode scanner — prefers native BarcodeDetector; falls back to capture + detect on still frame,
 * then manual entry. Does not invent materials.
 */
const BarcodeScanner: React.FC<BarcodeScannerProps> = ({ open, onClose, onDetected, title = 'Scan barcode' }) => {
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [manual, setManual] = useState('');
  const [busy, setBusy] = useState(false);
  const [scanning, setScanning] = useState(false);

  useEffect(() => {
    if (!open) return undefined;
    let cancelled = false;
    let timer: number | undefined;

    const stop = () => {
      if (timer) window.clearInterval(timer);
      streamRef.current?.getTracks().forEach((track) => track.stop());
      streamRef.current = null;
      setScanning(false);
    };

    const start = async () => {
      setError(null);
      if (!navigator.mediaDevices?.getUserMedia) {
        setError('Camera is not available in this browser. Type the barcode below.');
        return;
      }
      try {
        const stream = await navigator.mediaDevices.getUserMedia({
          audio: false,
          video: { facingMode: { ideal: 'environment' } },
        });
        if (cancelled) {
          stream.getTracks().forEach((track) => track.stop());
          return;
        }
        streamRef.current = stream;
        if (videoRef.current) {
          videoRef.current.srcObject = stream;
          await videoRef.current.play();
        }
        setScanning(true);

        if (!detectorSupported()) {
          setError('Live barcode decode is not supported here. Capture a still or type the code.');
          return;
        }

        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const Detector = (window as any).BarcodeDetector as new (opts: { formats: string[] }) => BarcodeDetectorLike;
        const detector = new Detector({ formats: FORMATS });
        timer = window.setInterval(async () => {
          const video = videoRef.current;
          if (!video || video.readyState < 2) return;
          try {
            const hits = await detector.detect(video);
            const hit = hits.find((row) => row.rawValue?.trim());
            if (!hit?.rawValue) return;
            stop();
            onDetected({ rawValue: hit.rawValue.trim(), format: hit.format || 'unknown' });
            onClose();
          } catch {
            /* keep scanning */
          }
        }, 450);
      } catch {
        setError('Camera permission denied or unavailable. Type the barcode below.');
      }
    };

    void start();
    return () => {
      cancelled = true;
      stop();
    };
  }, [open, onClose, onDetected]);

  const captureStill = async (file: File | undefined) => {
    if (!file) return;
    setBusy(true);
    setError(null);
    try {
      if (!detectorSupported()) {
        setError('Still-frame decode is not supported. Type the barcode.');
        return;
      }
      const bitmap = await createImageBitmap(file);
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const Detector = (window as any).BarcodeDetector as new (opts: { formats: string[] }) => BarcodeDetectorLike;
      const detector = new Detector({ formats: FORMATS });
      const hits = await detector.detect(bitmap);
      bitmap.close();
      const hit = hits.find((row) => row.rawValue?.trim());
      if (!hit?.rawValue) {
        setError('No barcode detected in the photo. Try again or type the code.');
        return;
      }
      onDetected({ rawValue: hit.rawValue.trim(), format: hit.format || 'unknown' });
      onClose();
    } catch {
      setError('Could not read the barcode from the photo. Type the code.');
    } finally {
      setBusy(false);
    }
  };

  if (!open) return null;

  return (
    <div className="sm-scanner" role="dialog" aria-modal="true" aria-label={title} data-testid="mobile-barcode-scanner">
      <div className="sm-scanner__panel">
        <div className="sm-row">
          <strong>{title}</strong>
          <button type="button" className="sm-btn" onClick={onClose}>
            Close
          </button>
        </div>
        <div className="sm-scanner__frame">
          <video ref={videoRef} className="sm-scanner__video" playsInline muted />
          <div className="sm-scanner__reticle" aria-hidden="true" />
        </div>
        {scanning && <p className="sm-meta">Hold the barcode inside the frame…</p>}
        {error && (
          <p className="sm-notice sm-notice--warning" role="status">
            {error}
          </p>
        )}
        <label className="sm-btn sm-btn--block">
          {busy ? 'Reading photo…' : 'Capture still for decode'}
          <input
            type="file"
            accept="image/*"
            capture="environment"
            hidden
            disabled={busy}
            onChange={(event) => {
              const file = event.target.files?.[0];
              event.target.value = '';
              void captureStill(file);
            }}
          />
        </label>
        <form
          className="sm-field"
          onSubmit={(event) => {
            event.preventDefault();
            const value = manual.trim();
            if (!value) return;
            onDetected({ rawValue: value, format: 'manual' });
            onClose();
          }}
        >
          <label htmlFor="sm-scan-manual">Or type barcode</label>
          <div className="sm-row">
            <input
              id="sm-scan-manual"
              className="sm-input"
              value={manual}
              autoComplete="off"
              onChange={(event) => setManual(event.target.value)}
            />
            <button type="submit" className="sm-btn sm-btn--primary" disabled={!manual.trim()}>
              Use
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default BarcodeScanner;
