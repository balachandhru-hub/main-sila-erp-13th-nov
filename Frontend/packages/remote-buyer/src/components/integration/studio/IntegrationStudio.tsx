import React, { useCallback, useEffect, useState } from "react";
import { EmptyState, Loader, Modal, toastService } from "@vosox/shared-ui";
import {
  SAP_INVENTORY_CONNECTION,
  deleteIntegration,
  getIntegrationExecutions,
  getIntegrations,
  testIntegration,
  type IntegrationConfiguration,
  type IntegrationExecution,
  type IntegrationSide,
  type SapMovementProfile,
} from "../../../api/integrationsApi";
import "../../silaMe/silaMeTheme.css";
import "../../silaMe/recipes/SilaRecipes.css";
import IntegrationWorkspace from "../IntegrationWorkspace";
import { errorMessage } from "../integrationFormat";
import IntegrationStudioConnections from "./IntegrationStudioConnections";
import IntegrationStudioMatrix from "./IntegrationStudioMatrix";
import IntegrationStudioMonitoring from "./IntegrationStudioMonitoring";
import IntegrationStudioOverview from "./IntegrationStudioOverview";
import IntegrationStudioWizard from "./IntegrationStudioWizard";
import "./IntegrationStudio.css";

type StudioTab =
  | "overview"
  | "connections"
  | "matrix"
  | "wizard"
  | "monitoring"
  | "workflow";

type View =
  | { name: "studio"; tab: StudioTab }
  | { name: "create"; processType: string; suggestedName?: string; suggestedSystem?: string; copyFromId?: string }
  | { name: "open"; configurationId: string; mode: "connection" | "workflow" };

interface IntegrationStudioProps {
  /** "integration" opens connection-focused studio; "workflow" opens mapping/history workflow mode. */
  variant?: "integration" | "workflow";
  side?: IntegrationSide;
  readOnly?: boolean;
  /** Optional host navigation into SILA ME Transaction Tracker. */
  onOpenTracker?: () => void;
}

const TABS: { key: StudioTab; label: string; workflowOnly?: boolean; connectionOnly?: boolean }[] = [
  { key: "overview", label: "Overview" },
  { key: "connections", label: "Connections" },
  { key: "matrix", label: "Movement Matrix" },
  { key: "wizard", label: "Configure Wizard", connectionOnly: true },
  { key: "monitoring", label: "Monitoring" },
  { key: "workflow", label: "All APIs", workflowOnly: true },
];

/**
 * Premium SILA Integration Studio. Reuses the existing integration engine, credential store,
 * IntegrationWorkspace editor, and Transaction Tracker — no parallel SAP posting stack.
 */
const IntegrationStudio: React.FC<IntegrationStudioProps> = ({
  variant = "integration",
  side = "buyer",
  readOnly = false,
  onOpenTracker,
}) => {
  const isWorkflow = variant === "workflow";
  const [view, setView] = useState<View>({ name: "studio", tab: isWorkflow ? "workflow" : "overview" });
  const [rows, setRows] = useState<IntegrationConfiguration[]>([]);
  const [executions, setExecutions] = useState<IntegrationExecution[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [pendingDelete, setPendingDelete] = useState<IntegrationConfiguration | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [testingId, setTestingId] = useState<string | null>(null);
  const [wizardProfile, setWizardProfile] = useState<SapMovementProfile | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [nextRows, nextExecutions] = await Promise.all([
        getIntegrations(side),
        getIntegrationExecutions(side).catch(() => [] as IntegrationExecution[]),
      ]);
      setRows(nextRows);
      setExecutions(nextExecutions);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Could not load Integration Studio.");
    } finally {
      setLoading(false);
    }
  }, [side]);

  useEffect(() => {
    if (view.name !== "studio") return undefined;
    let active = true;
    setLoading(true);
    setError(null);
    Promise.all([
      getIntegrations(side),
      getIntegrationExecutions(side).catch(() => [] as IntegrationExecution[]),
    ])
      .then(([nextRows, nextExecutions]) => {
        if (!active) return;
        setRows(nextRows);
        setExecutions(nextExecutions);
      })
      .catch((err: unknown) => {
        if (active) setError(err instanceof Error ? err.message : "Could not load Integration Studio.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => { active = false; };
  }, [view.name, side, variant]);

  useEffect(() => {
    setView({ name: "studio", tab: isWorkflow ? "workflow" : "overview" });
  }, [variant, isWorkflow]);

  const backToStudio = (tab: StudioTab = "overview") => {
    setWizardProfile(null);
    setView({ name: "studio", tab });
  };

  const openConfiguration = (configurationId: string) => {
    setView({ name: "open", configurationId, mode: isWorkflow ? "workflow" : "connection" });
  };

  const startConfigureProfile = (profile: SapMovementProfile) => {
    if (profile.activationPolicy === "blocked_unverified") {
      toastService.error("This profile cannot be activated until SAP verification is complete.");
      return;
    }
    const existing = rows.find((row) => row.name.trim().toLowerCase() === profile.configurationName.trim().toLowerCase());
    if (existing) {
      openConfiguration(existing.id);
      return;
    }
    setWizardProfile(profile);
    setView({ name: "studio", tab: "wizard" });
  };

  const handleTest = async (configurationId: string) => {
    if (readOnly) return;
    setTestingId(configurationId);
    try {
      const result = await testIntegration(side, configurationId);
      if (result.success) toastService.success(result.message || "Connection test succeeded.");
      else toastService.error(result.message || "Connection test failed.");
      await load();
    } catch (err: unknown) {
      toastService.error(errorMessage(err, "The connection test could not be completed."));
    } finally {
      setTestingId(null);
    }
  };

  const handleDelete = async () => {
    if (!pendingDelete) return;
    setDeleting(true);
    try {
      await deleteIntegration(side, pendingDelete.id);
      toastService.success("API deleted.");
      setPendingDelete(null);
      await load();
    } catch (err: unknown) {
      toastService.error(errorMessage(err, "Could not delete the integration."));
    } finally {
      setDeleting(false);
    }
  };

  const handleCopy = (row: IntegrationConfiguration) => {
    setView({
      name: "create",
      processType: row.processType,
      copyFromId: row.id,
      suggestedName: `${row.name} copy`,
      suggestedSystem: row.systemName || SAP_INVENTORY_CONNECTION.connectionName,
    });
  };

  const visibleTabs = TABS.filter((tab) => {
    if (tab.workflowOnly && !isWorkflow) return false;
    if (tab.connectionOnly && isWorkflow) return false;
    return true;
  });

  if (view.name !== "studio") {
    return (
      <IntegrationWorkspace
        startWith={view.name === "create" ? "create" : { configurationId: view.configurationId }}
        initialProcessType={view.name === "create" ? view.processType : undefined}
        initialName={view.name === "create" ? view.suggestedName : undefined}
        initialSystemName={view.name === "create" ? view.suggestedSystem : undefined}
        copyFromId={view.name === "create" ? view.copyFromId : undefined}
        onExit={() => backToStudio(isWorkflow ? "workflow" : "connections")}
        mode={view.name === "open" ? view.mode : isWorkflow ? "workflow" : "connection"}
        side={side}
        readOnly={readOnly}
      />
    );
  }

  const currentTab = view.tab;

  return (
    <div className="sila-me srec-page rstudio-page istudio-page" data-testid="integration-studio">
      <header className="istudio-hero">
        <div>
          <p className="istudio-kicker">SILA Integration Studio</p>
          <h1 className="istudio-hero__title">Integration Studio</h1>
          <p className="istudio-hero__sub">
            Configure SAP inventory movements on the existing GRN integration engine — secure credentials, CSRF,
            Material Document APIs, and Transaction Tracker — without a parallel posting stack.
          </p>
        </div>
        <div className="istudio-hero__actions">
          {!readOnly && !isWorkflow ? (
            <button type="button" className="sila-btn sila-btn--primary" onClick={() => setView({ name: "studio", tab: "wizard" })}>
              Configure wizard
            </button>
          ) : null}
          <button
            type="button"
            className="sila-btn sila-btn--secondary"
            onClick={() => (onOpenTracker ? onOpenTracker() : setView({ name: "studio", tab: "monitoring" }))}
          >
            Transaction Tracker
          </button>
        </div>
      </header>

      <nav className="istudio-tabs" aria-label="Integration Studio tabs">
        {visibleTabs.map((tab) => (
          <button
            key={tab.key}
            type="button"
            className="istudio-tab"
            aria-selected={currentTab === tab.key}
            onClick={() => setView({ name: "studio", tab: tab.key })}
          >
            {tab.label}
          </button>
        ))}
      </nav>

      {loading ? (
        <Loader size={24} message="Loading Integration Studio..." />
      ) : error ? (
        <EmptyState
          variant="error"
          title="Couldn't load Integration Studio"
          description={error}
          action={<button type="button" className="sila-btn sila-btn--secondary" onClick={load}>Try again</button>}
        />
      ) : (
        <>
          {currentTab === "overview" && (
            <IntegrationStudioOverview
              rows={rows}
              executions={executions}
              onOpenConnections={() => setView({ name: "studio", tab: "connections" })}
              onOpenWizard={() => setView({ name: "studio", tab: "wizard" })}
              onOpenMonitoring={() => setView({ name: "studio", tab: "monitoring" })}
            />
          )}

          {currentTab === "connections" && (
            <IntegrationStudioConnections
              rows={isWorkflow ? rows.filter((row) => ["TESTED", "ACTIVE", "INACTIVE"].includes(row.status)) : rows}
              readOnly={readOnly}
              onOpen={openConfiguration}
              onTest={handleTest}
              onCopy={handleCopy}
              onDelete={setPendingDelete}
              onConfigureProfile={startConfigureProfile}
              testingId={testingId}
            />
          )}

          {currentTab === "matrix" && (
            <IntegrationStudioMatrix rows={rows} onOpen={openConfiguration} />
          )}

          {currentTab === "wizard" && !readOnly && (
            <IntegrationStudioWizard
              side={side}
              initialProfile={wizardProfile}
              onCancel={() => backToStudio("connections")}
              onSaved={(configuration) => {
                setWizardProfile(null);
                setView({ name: "open", configurationId: configuration.id, mode: "connection" });
              }}
            />
          )}

          {currentTab === "monitoring" && (
            <IntegrationStudioMonitoring
              rows={rows}
              executions={executions}
              onOpenTracker={() => (onOpenTracker ? onOpenTracker() : toastService.info("Open Setup → SILA ME Transactions for the full tracker."))}
              onOpenConfiguration={openConfiguration}
            />
          )}

          {currentTab === "workflow" && (
            <div className="srec-stack">
              <div className="istudio-banner">
                Workflow &amp; Configuration uses the same saved APIs. Open an API to edit field mapping, request body, activation, and run history.
              </div>
              <IntegrationStudioConnections
                rows={rows.filter((row) => ["TESTED", "ACTIVE", "INACTIVE"].includes(row.status))}
                readOnly={readOnly}
                onOpen={(id) => setView({ name: "open", configurationId: id, mode: "workflow" })}
                onTest={handleTest}
                onCopy={handleCopy}
                onDelete={setPendingDelete}
                testingId={testingId}
              />
              <IntegrationStudioMatrix rows={rows} onOpen={(id) => setView({ name: "open", configurationId: id, mode: "workflow" })} />
            </div>
          )}
        </>
      )}

      <Modal
        isOpen={pendingDelete !== null}
        onClose={() => { if (!deleting) setPendingDelete(null); }}
        variant="danger"
        size="sm"
        headerProps={{ heading: "Delete API" }}
        bodyProps={{
          content: pendingDelete ? `Delete "${pendingDelete.name}"?` : "",
          contentDescription: "The connection, its field mapping and its run history are removed. This cannot be undone. Existing GRN postings already in SAP are not reversed.",
        }}
        footerProps={{
          secondaryButton: { text: "Cancel", onClick: () => setPendingDelete(null), disabled: deleting },
          primaryButton: { text: "Delete", onClick: handleDelete, loading: deleting },
        }}
      />
    </div>
  );
};

export default IntegrationStudio;
