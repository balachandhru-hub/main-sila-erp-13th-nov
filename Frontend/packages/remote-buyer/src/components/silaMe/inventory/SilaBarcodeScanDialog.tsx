import React, { useEffect, useRef, useState } from "react";
import { ScanLine, X } from "lucide-react";

interface BarcodeDetectorLike {
  detect: (source: CanvasImageSource) => Promise<{ rawValue: string }[]>;
}

type BarcodeDetectorCtor = new (options?: { formats?: string[] }) => BarcodeDetectorLike;

const detectorCtor = (): BarcodeDetectorCtor | null => {
  const ctor = (window as unknown as { BarcodeDetector?: BarcodeDetectorCtor }).BarcodeDetector;
  return ctor ?? null;
};

interface SilaBarcodeScanDialogProps {
  open: boolean;
  onClose: () => void;
  onCode: (code: string) => void;
}

/** Live camera barcode scan where the browser supports it, with typed-entry and photo fallbacks. */
const SilaBarcodeScanDialog: React.FC<SilaBarcodeScanDialogProps> = ({ open, onClose, onCode }) => {
  const videoRef = useRef<HTMLVideoElement>(null);
  const photoRef = useRef<HTMLInputElement>(null);
  const [manual, setManual] = useState("");
  const [status, setStatus] = useState("");
  const [cameraOn, setCameraOn] = useState(false);

  useEffect(() => {
    if (!open) return;
    setManual("");
    const Ctor = detectorCtor();
    if (!Ctor || !navigator.mediaDevices?.getUserMedia) {
      setStatus("Live scanning is not available in this browser. Type the code or photograph the barcode.");
      return;
    }
    let stream: MediaStream | null = null;
    let stopped = false;
    let timer = 0;
    const detector = new Ctor({ formats: ["ean_13", "ean_8", "upc_a", "upc_e", "code_128", "code_39", "qr_code"] });
    setStatus("Starting camera…");
    navigator.mediaDevices
      .getUserMedia({ video: { facingMode: "environment" } })
      .then((media) => {
        if (stopped) {
          media.getTracks().forEach((track) => track.stop());
          return;
        }
        stream = media;
        const video = videoRef.current;
        if (!video) return;
        video.srcObject = media;
        void video.play();
        setCameraOn(true);
        setStatus("Point the camera at the barcode.");
        const tick = async () => {
          if (stopped) return;
          try {
            const found = await detector.detect(video);
            if (found.length > 0 && found[0].rawValue) {
              stopped = true;
              onCode(found[0].rawValue);
              return;
            }
          } catch {
            // keep scanning
          }
          timer = window.setTimeout(tick, 250);
        };
        void tick();
      })
      .catch(() => setStatus("Camera not available. Type the code or photograph the barcode."));
    return () => {
      stopped = true;
      window.clearTimeout(timer);
      stream?.getTracks().forEach((track) => track.stop());
      setCameraOn(false);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  if (!open) return null;

  const decodePhoto = async (file: File | undefined) => {
    if (!file) return;
    const Ctor = detectorCtor();
    if (!Ctor) {
      setStatus("This browser cannot read barcodes from a photo. Type the code instead.");
      return;
    }
    try {
      const bitmap = await createImageBitmap(file);
      const found = await new Ctor().detect(bitmap);
      if (found.length > 0 && found[0].rawValue) onCode(found[0].rawValue);
      else setStatus("No barcode found in that photo. Try again or type the code.");
    } catch {
      setStatus("Could not read that photo. Type the code instead.");
    }
  };

  return (
    <div className="lic-modal-backdrop" role="presentation" onClick={onClose}>
      <div className="lic-modal lic-modal--scan" role="dialog" aria-modal="true" aria-label="Scan barcode" data-testid="barcode-scan-dialog" onClick={(event) => event.stopPropagation()}>
        <header className="lic-modal__head">
          <h2>Scan barcode</h2>
          <button type="button" className="lic-icon-btn" onClick={onClose} aria-label="Close scan">
            <X size={18} />
          </button>
        </header>
        <div className={`lic-scan__view${cameraOn ? " lic-scan__view--on" : ""}`}>
          <video ref={videoRef} muted playsInline />
          {!cameraOn && <ScanLine size={40} />}
          <span className="lic-scan__laser" aria-hidden />
        </div>
        {status && <p className="lic-scan__status" role="status">{status}</p>}
        <form
          className="lic-scan__manual"
          onSubmit={(event) => {
            event.preventDefault();
            if (manual.trim()) onCode(manual.trim());
          }}
        >
          <input
            type="text"
            inputMode="numeric"
            placeholder="Barcode or material number"
            value={manual}
            onChange={(event) => setManual(event.target.value)}
            aria-label="Barcode or material number"
          />
          <button type="submit" className="lic-btn lic-btn--primary" disabled={!manual.trim()}>Find</button>
        </form>
        <div className="lic-scan__photo">
          <button type="button" className="lic-btn lic-btn--ghost" onClick={() => photoRef.current?.click()}>Photograph a barcode</button>
          <input ref={photoRef} type="file" accept="image/*" capture="environment" hidden onChange={(event) => { void decodePhoto(event.target.files?.[0]); event.target.value = ""; }} />
        </div>
      </div>
    </div>
  );
};

export default SilaBarcodeScanDialog;
