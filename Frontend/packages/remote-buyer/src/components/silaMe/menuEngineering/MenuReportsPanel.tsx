import React, { useEffect, useState } from "react";
import { toastService } from "@vosox/shared-ui";
import { exportMeReport, getMeReportCatalog, type MenuEngineeringReportItem } from "../../../api/silaMe/menuEngineeringStudioApi";
import type { MenuFilter } from "../../../api/silaMe/silaMenuEngineeringApi";

interface Props {
  filter: MenuFilter;
  onClose: () => void;
}

const MenuReportsPanel: React.FC<Props> = ({ filter, onClose }) => {
  const [reports, setReports] = useState<MenuEngineeringReportItem[]>([]);
  const [busyKey, setBusyKey] = useState<string>("");

  useEffect(() => {
    void getMeReportCatalog().then(setReports).catch(() => undefined);
  }, []);

  const download = async (reportKey: string, format: string) => {
    setBusyKey(`${reportKey}-${format}`);
    try {
      const exp = await exportMeReport(reportKey, format, filter);
      if (format === "PDF" && exp.textContent) {
        const w = window.open("", "_blank");
        if (w) {
          w.document.write(exp.textContent);
          w.document.close();
          w.print();
          return;
        }
      }
      const blob = new Blob([exp.textContent ?? JSON.stringify(exp.rows, null, 2)], { type: exp.mimeType });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = exp.fileName;
      a.click();
      URL.revokeObjectURL(url);
      toastService.success(`Exported ${exp.fileName}`);
    } catch (e) {
      toastService.error(e instanceof Error ? e.message : "Export failed.");
    } finally {
      setBusyKey("");
    }
  };

  return (
    <div className="sme-panel">
      <div className="sme-panel__header">
        <div>
          <h2 className="sme-panel__title">Menu Engineering Reports &amp; Exports (22 Reports)</h2>
          <div className="sme-muted">Export live tenant &amp; property data in CSV, Excel, JSON, or printable PDF.</div>
        </div>
        <button type="button" className="sila-btn sila-btn--secondary" onClick={onClose}>Close Reports</button>
      </div>
      <table className="sila-table">
        <thead>
          <tr><th>#</th><th>Report</th><th>Category</th><th>Export Formats</th></tr>
        </thead>
        <tbody>
          {reports.map((r, i) => (
            <tr key={r.reportKey}>
              <td>{i + 1}</td>
              <td><strong>{r.title}</strong></td>
              <td><span className="sme-chip">{r.category}</span></td>
              <td style={{ display: "flex", gap: "0.35rem", flexWrap: "wrap" }}>
                {r.supportedFormats.map((fmt) => (
                  <button
                    key={fmt}
                    type="button"
                    className="sila-btn sila-btn--secondary"
                    disabled={busyKey === `${r.reportKey}-${fmt}`}
                    onClick={() => void download(r.reportKey, fmt)}
                  >
                    {fmt}
                  </button>
                ))}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default MenuReportsPanel;
