import React, { useEffect, useMemo, useRef, useState } from "react";
import { Camera, Loader2, ScanEye, Upload, X } from "lucide-react";
import {
  analyzeBottleLevel,
  type SilaBottleVision,
  type SilaLiveInventoryRow,
} from "../../../api/silaMe/silaInventoryApi";
import {
  applyVisionReading,
  mlFromFill,
  parsePackSizeMl,
  splitSealedFraction,
  type VisionApplication,
} from "./liveInventoryModel";

interface SilaVisionAiModalProps {
  open: boolean;
  /** Bottle rows (beverage / minibar, counted in bottles) the reading can be applied to. */
  bottleRows: SilaLiveInventoryRow[];
  initialMaterialId?: string | null;
  initialFile?: File | null;
  canApply: boolean;
  onClose: () => void;
  onApply: (row: SilaLiveInventoryRow, reading: VisionApplication) => void;
}

const METHOD_TEXT: Record<string, string> = {
  CV_HEURISTIC: "Vision engine (liquid line)",
  LLM_VISION: "Vision model",
  "LLM_VISION+CV": "Vision model + liquid line",
  NONE: "No reading",
};

const SilaVisionAiModal: React.FC<SilaVisionAiModalProps> = ({
  open,
  bottleRows,
  initialMaterialId,
  initialFile,
  canApply,
  onClose,
  onApply,
}) => {
  const uploadRef = useRef<HTMLInputElement>(null);
  const cameraRef = useRef<HTMLInputElement>(null);
  const [materialId, setMaterialId] = useState("");
  const [packMl, setPackMl] = useState("750");
  const [file, setFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<SilaBottleVision | null>(null);
  const [manualPct, setManualPct] = useState<number | null>(null);

  const selected = useMemo(() => bottleRows.find((row) => row.materialId === materialId) ?? null, [bottleRows, materialId]);

  const runAnalysis = async (photo: File, nominalMl: number | null, description: string | null) => {
    setBusy(true);
    setError(null);
    setResult(null);
    setManualPct(null);
    try {
      const vision = await analyzeBottleLevel(photo, { nominalMl, description });
      setResult(vision);
      if (vision.fillRatio != null) setManualPct(Math.round(vision.fillRatio * 100));
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Could not read the bottle photo.");
    } finally {
      setBusy(false);
    }
  };

  useEffect(() => {
    if (!open) return;
    const preset = bottleRows.find((row) => row.materialId === initialMaterialId) ?? null;
    setMaterialId(preset?.materialId ?? "");
    setPackMl(String(parsePackSizeMl(preset?.description) ?? 750));
    setResult(null);
    setError(null);
    setManualPct(null);
    setFile(initialFile ?? null);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, initialMaterialId, initialFile]);

  useEffect(() => {
    if (!file) {
      setPreviewUrl(null);
      return;
    }
    const url = URL.createObjectURL(file);
    setPreviewUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [file]);

  useEffect(() => {
    if (!open || !initialFile) return;
    const preset = bottleRows.find((row) => row.materialId === initialMaterialId) ?? null;
    const nominal = parsePackSizeMl(preset?.description) ?? 750;
    void runAnalysis(initialFile, nominal, preset?.description ?? null);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, initialFile]);

  if (!open) return null;

  const nominalMl = Number(packMl) > 0 ? Number(packMl) : 750;

  const pickFile = (picked: File | undefined) => {
    if (!picked) return;
    setFile(picked);
    void runAnalysis(picked, nominalMl, selected?.description ?? null);
  };

  const onPickMaterial = (id: string) => {
    setMaterialId(id);
    const row = bottleRows.find((entry) => entry.materialId === id);
    const size = parsePackSizeMl(row?.description);
    if (size) setPackMl(String(size));
  };

  const fill = manualPct != null ? manualPct / 100 : result?.fillRatio ?? null;
  const ml = fill != null ? mlFromFill(fill, result?.nominalMl ?? nominalMl) : null;
  const sealedCount = selected ? splitSealedFraction(selected.availableQty ?? selected.onHandQty).sealed : 0;
  const reading = fill != null ? applyVisionReading({ fillRatio: fill, nominalMl: result?.nominalMl ?? nominalMl }, sealedCount) : null;
  const edited = result?.fillRatio != null && manualPct != null && Math.round(result.fillRatio * 100) !== manualPct;
  const lineTop = result?.levelLineRatio != null ? `${(result.levelLineRatio * 100).toFixed(1)}%` : null;
  const box = result?.bottleBox;

  return (
    <div className="lic-modal-backdrop" role="presentation" onClick={onClose}>
      <div
        className="lic-modal lic-modal--vision"
        role="dialog"
        aria-modal="true"
        aria-label="Vision AI bottle level"
        data-testid="vision-ai-modal"
        onClick={(event) => event.stopPropagation()}
      >
        <header className="lic-vision__head">
          <div>
            <p className="lic-eyebrow lic-eyebrow--light">Vision AI</p>
            <h2>Bottle level reader</h2>
          </div>
          <button type="button" className="lic-icon-btn lic-icon-btn--light" onClick={onClose} aria-label="Close Vision AI">
            <X size={18} />
          </button>
        </header>

        <div className="lic-vision__body">
          <div className="lic-vision__stage">
            {previewUrl ? (
              <div className="lic-vision__frame">
                <img src={previewUrl} alt="Bottle photo" />
                {box && box.length === 4 && (
                  <span
                    className="lic-vision__box"
                    style={{ left: `${box[0] * 100}%`, top: `${box[1] * 100}%`, width: `${(box[2] - box[0]) * 100}%`, height: `${(box[3] - box[1]) * 100}%` }}
                  />
                )}
                {lineTop && (
                  <span className="lic-vision__line" style={{ top: lineTop }}>
                    <em>Liquid level</em>
                  </span>
                )}
                {busy && (
                  <div className="lic-vision__busy">
                    <Loader2 className="lic-spin" size={26} />
                    <span>Reading the liquid level…</span>
                  </div>
                )}
              </div>
            ) : (
              <div className="lic-vision__empty">
                <ScanEye size={34} />
                <p>Photograph the bottle upright, label facing the camera, with the whole bottle in frame.</p>
              </div>
            )}
            <div className="lic-vision__pick">
              <button type="button" className="lic-btn lic-btn--dark" onClick={() => cameraRef.current?.click()}>
                <Camera size={16} /> Take photo
              </button>
              <button type="button" className="lic-btn lic-btn--dark" onClick={() => uploadRef.current?.click()}>
                <Upload size={16} /> Upload
              </button>
              <input ref={cameraRef} type="file" accept="image/*" capture="environment" hidden onChange={(event) => { pickFile(event.target.files?.[0]); event.target.value = ""; }} />
              <input ref={uploadRef} type="file" accept="image/*" hidden onChange={(event) => { pickFile(event.target.files?.[0]); event.target.value = ""; }} />
            </div>
          </div>

          <div className="lic-vision__side">
            <label className="lic-field lic-field--dark">
              <span>Bottle</span>
              <select value={materialId} onChange={(event) => onPickMaterial(event.target.value)}>
                <option value="">Not linked to an item</option>
                {bottleRows.map((row) => (
                  <option key={row.materialId} value={row.materialId}>
                    {row.materialCode} · {row.description}
                  </option>
                ))}
              </select>
            </label>
            <label className="lic-field lic-field--dark">
              <span>Bottle size (ml)</span>
              <input
                type="number"
                min={10}
                max={5000}
                value={packMl}
                onChange={(event) => setPackMl(event.target.value)}
                onBlur={() => {
                  if (file && !busy) void runAnalysis(file, nominalMl, selected?.description ?? null);
                }}
              />
            </label>

            {error && <p className="lic-vision__error" role="alert">{error}</p>}

            {result && !error && (
              <div className="lic-vision__result" data-testid="vision-result">
                {!result.bottleDetected ? (
                  <p className="lic-vision__warn">No bottle was found in this photo. Retake it with the whole bottle in frame.</p>
                ) : (
                  <>
                    <div className="lic-vision__status">
                      <span className={`lic-pill ${result.isOpened ? "lic-pill--amber" : "lic-pill--green"}`}>
                        {result.sealStatus === "SEALED" ? "Sealed" : result.sealStatus === "OPENED" ? "Opened" : "Seal not determined"}
                      </span>
                      <span className="lic-vision__conf">
                        Confidence <b>{Math.round(result.confidence * 100)}%</b>
                      </span>
                    </div>
                    {fill != null && ml && (
                      <>
                        <div className="lic-vision__ml">
                          <div>
                            <small>Remaining</small>
                            <strong>{ml.remainingMl} ml</strong>
                          </div>
                          <div>
                            <small>Consumed</small>
                            <strong className="lic-vision__consumed">{ml.consumedMl} ml</strong>
                          </div>
                          <div>
                            <small>Level</small>
                            <strong>{Math.round(fill * 100)}%</strong>
                          </div>
                        </div>
                        <div className="lic-vision__bar" aria-hidden>
                          <span style={{ width: `${Math.round(fill * 100)}%` }} />
                        </div>
                        <label className="lic-field lic-field--dark">
                          <span>Adjust level{edited ? " (manual)" : ""}</span>
                          <input
                            type="range"
                            min={0}
                            max={100}
                            step={1}
                            value={manualPct ?? 0}
                            onChange={(event) => setManualPct(Number(event.target.value))}
                          />
                        </label>
                      </>
                    )}
                    {selected && reading && (
                      <p className="lic-vision__total">
                        {sealedCount} sealed + {reading.fraction.toFixed(2)} open = <b>{reading.total.toFixed(2)} {selected.baseUom}</b>
                      </p>
                    )}
                  </>
                )}
                <p className="lic-vision__method">
                  {METHOD_TEXT[result.method] ?? result.method}
                  {result.model ? ` · ${result.model}` : ""}
                </p>
                {result.needsManualReview && (
                  <p className="lic-vision__warn" role="status">Low confidence. Check the level against the bottle before applying.</p>
                )}
                {[...(result.warnings ?? [])].map((text) => (
                  <p key={text} className="lic-vision__warn">{text}</p>
                ))}
                {(result.notices ?? []).map((text) => (
                  <p key={text} className="lic-vision__note">{text}</p>
                ))}
              </div>
            )}

            <div className="lic-vision__actions">
              <button type="button" className="lic-btn lic-btn--ghost-dark" onClick={onClose}>Close</button>
              <button
                type="button"
                className="lic-btn lic-btn--primary"
                disabled={!canApply || !selected || !reading || busy || !result?.bottleDetected}
                title={!selected ? "Link the photo to an item to apply it" : !canApply ? "You cannot post stock counts" : undefined}
                onClick={() => {
                  if (selected && reading) onApply(selected, reading);
                }}
              >
                Apply to shelf count
              </button>
            </div>
            <p className="lic-vision__foot">Nothing is posted yet. Applying fills the shelf count, which you post after reviewing.</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default SilaVisionAiModal;
