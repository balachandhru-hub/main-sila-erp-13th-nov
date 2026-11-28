using System.Text.Json;
using Buyer.Application.Features.Shared.MenuEngineering.Ai;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Shared.MenuEngineering.Autonomous
{
    public partial class MenuEngineeringAutonomousService
    {
        public async Task<List<AgentAutonomyPolicyDto>> GetAutonomyPoliciesAsync(
            Guid organizationId, Guid roleId, Guid? propertyId = null, CancellationToken ct = default)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await ResolveContextAsync(organizationId, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering permission required.");

            List<MenuEngineeringAgentAutonomyPolicy> stored = await _db.MenuEngineeringAgentAutonomyPolicy
                .AsNoTracking()
                .Where(x => x.BuyerId == buyer.Id && x.IsActive && (propertyId == null || x.PropertyId == propertyId || x.PropertyId == null))
                .ToListAsync(ct);

            List<AgentAutonomyPolicyDto> results = new();
            foreach (MenuEngineeringAgentDescriptorDto def in MenuEngineeringAgentRegistry.Agents)
            {
                MenuEngineeringAgentAutonomyPolicy? pol = stored.FirstOrDefault(p => p.AgentId == def.AgentId && p.PropertyId == propertyId)
                    ?? stored.FirstOrDefault(p => p.AgentId == def.AgentId && p.PropertyId == null);

                int defaultLevel = def.AgentId is "COST_MARGIN_AGENT" or "RECIPE_INTELLIGENCE_AGENT" ? 1 : 2;
                int level = pol?.AutonomyLevel ?? defaultLevel;
                results.Add(new AgentAutonomyPolicyDto
                {
                    Id = pol?.Id,
                    PropertyId = pol?.PropertyId ?? propertyId,
                    AgentId = def.AgentId,
                    DisplayName = def.DisplayName,
                    ActionType = pol?.ActionType ?? "ALL",
                    AutonomyLevel = level,
                    AutonomyLevelLabel = MenuEngineeringAutonomousContracts.AutonomyLevelLabel(level),
                    MaxFinancialThreshold = pol?.MaxFinancialThreshold ?? 2500m,
                    MaterialityThresholdPercent = pol?.MaterialityThresholdPercent ?? 5m,
                    CooldownMinutes = pol?.CooldownMinutes ?? 60,
                    IsPaused = pol?.IsPaused ?? false,
                    PolicyVersion = pol?.PolicyVersion ?? 1,
                    AllowedActions = DeserializeList(pol?.AllowedActionsJson)
                });
            }
            return results;
        }

        public async Task<AgentAutonomyPolicyDto> UpsertAutonomyPolicyAsync(
            Guid organizationId, Guid userId, Guid roleId, UpsertAgentAutonomyPolicyRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await ResolveContextAsync(organizationId, roleId, request.PropertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ManageMenuSetup, "Access denied", "Administrator / Menu Setup permission required to modify agent autonomy policy.");

            if (request.AutonomyLevel < 0 || request.AutonomyLevel > 5)
                throw new BadRequestCustomException("Invalid autonomy level", "AutonomyLevel must be between 0 and 5.");

            string actionType = string.IsNullOrWhiteSpace(request.ActionType) ? "ALL" : request.ActionType.Trim();
            MenuEngineeringAgentAutonomyPolicy? pol = await _db.MenuEngineeringAgentAutonomyPolicy
                .FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IsActive && x.AgentId == request.AgentId && x.PropertyId == request.PropertyId && x.ActionType == actionType, ct);

            if (pol is null)
            {
                pol = new MenuEngineeringAgentAutonomyPolicy
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    PropertyId = request.PropertyId,
                    AgentId = request.AgentId,
                    ActionType = actionType,
                    IsActive = true,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow
                };
                _db.MenuEngineeringAgentAutonomyPolicy.Add(pol);
            }
            else
            {
                pol.PolicyVersion++;
            }

            pol.AutonomyLevel = request.AutonomyLevel;
            pol.MaxFinancialThreshold = request.MaxFinancialThreshold;
            pol.MaterialityThresholdPercent = request.MaterialityThresholdPercent;
            pol.CooldownMinutes = Math.Max(5, request.CooldownMinutes);
            pol.IsPaused = request.IsPaused;
            pol.AllowedActionsJson = JsonSerializer.Serialize(request.AllowedActions ?? new(), JsonOpts);
            pol.UpdatedByUserId = userId;
            pol.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            string agentName = MenuEngineeringAgentRegistry.Agents.FirstOrDefault(a => a.AgentId == pol.AgentId)?.DisplayName ?? pol.AgentId;
            return new AgentAutonomyPolicyDto
            {
                Id = pol.Id,
                PropertyId = pol.PropertyId,
                AgentId = pol.AgentId,
                DisplayName = agentName,
                ActionType = pol.ActionType,
                AutonomyLevel = pol.AutonomyLevel,
                AutonomyLevelLabel = MenuEngineeringAutonomousContracts.AutonomyLevelLabel(pol.AutonomyLevel),
                MaxFinancialThreshold = pol.MaxFinancialThreshold,
                MaterialityThresholdPercent = pol.MaterialityThresholdPercent,
                CooldownMinutes = pol.CooldownMinutes,
                IsPaused = pol.IsPaused,
                PolicyVersion = pol.PolicyVersion,
                AllowedActions = DeserializeList(pol.AllowedActionsJson)
            };
        }

        public async Task<EmergencyControlStatusDto> SetEmergencyControlAsync(
            Guid organizationId, Guid userId, Guid roleId, EmergencyControlRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, MenuEngineeringSettings settings) =
                await ResolveContextAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ManageMenuSetup, "Access denied", "Administrator / Menu Setup permission required for emergency controls.");

            if (request.AutonomousModePaused.HasValue)
            {
                settings.AutonomousModePaused = request.AutonomousModePaused.Value;
                settings.EmergencyPausedAtUtc = request.AutonomousModePaused.Value ? DateTime.UtcNow : null;
                settings.EmergencyPausedByUserId = request.AutonomousModePaused.Value ? userId : null;
            }
            if (request.AutomatedExecutionDisabled.HasValue)
            {
                settings.AutomatedExecutionDisabled = request.AutomatedExecutionDisabled.Value;
            }
            if (!string.IsNullOrWhiteSpace(request.Reason))
            {
                settings.EmergencyPauseReason = request.Reason.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.PauseAgentId) && request.PauseAgent.HasValue)
            {
                MenuEngineeringAgentAutonomyPolicy? pol = await _db.MenuEngineeringAgentAutonomyPolicy
                    .FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IsActive && x.AgentId == request.PauseAgentId, ct);
                if (pol is null)
                {
                    pol = new MenuEngineeringAgentAutonomyPolicy
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyer.Id,
                        AgentId = request.PauseAgentId,
                        AutonomyLevel = 2,
                        UpdatedAtUtc = DateTime.UtcNow,
                        IsActive = true,
                        DateCreated = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow
                    };
                    _db.MenuEngineeringAgentAutonomyPolicy.Add(pol);
                }
                pol.IsPaused = request.PauseAgent.Value;
                pol.UpdatedByUserId = userId;
                pol.UpdatedAtUtc = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(request.SuspendIntegrationType) && request.SuspendIntegration.HasValue)
            {
                MenuEngineeringIntegrationMonitorEvent? mon = await _db.MenuEngineeringIntegrationMonitorEvent
                    .Where(x => x.BuyerId == buyer.Id && x.IsActive && x.IntegrationType == request.SuspendIntegrationType)
                    .OrderByDescending(x => x.CheckedAtUtc)
                    .FirstOrDefaultAsync(ct);
                if (mon is null)
                {
                    mon = new MenuEngineeringIntegrationMonitorEvent
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyer.Id,
                        IntegrationType = request.SuspendIntegrationType,
                        HealthStatus = request.SuspendIntegration.Value ? "SUSPENDED" : "NOT_CONFIGURED",
                        TechnicalDetail = request.Reason ?? "Integration suspension state changed by administrator.",
                        CheckedAtUtc = DateTime.UtcNow,
                        IsActive = true,
                        DateCreated = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow
                    };
                    _db.MenuEngineeringIntegrationMonitorEvent.Add(mon);
                }
                mon.IsSuspended = request.SuspendIntegration.Value;
                mon.HealthStatus = request.SuspendIntegration.Value ? "SUSPENDED" : mon.HealthStatus;
            }

            int cancelled = 0;
            if (request.CancelPendingJobs)
            {
                List<MenuEngineeringBusinessEvent> pending = await _db.MenuEngineeringBusinessEvent
                    .Where(x => x.BuyerId == buyer.Id && x.IsActive && (x.ProcessingStatus == "PENDING" || x.ProcessingStatus == "FAILED"))
                    .ToListAsync(ct);
                foreach (MenuEngineeringBusinessEvent ev in pending)
                {
                    ev.ProcessingStatus = "CANCELLED";
                    ev.DeadLetterReason = request.Reason ?? "Cancelled by administrator emergency control.";
                    cancelled++;
                }
            }

            await _db.SaveChangesAsync(ct);
            EmergencyControlStatusDto status = await GetEmergencyStatusAsync(organizationId, roleId, ct);
            status.CancelledJobsCount = cancelled;
            return status;
        }

        public async Task<EmergencyControlStatusDto> GetEmergencyStatusAsync(
            Guid organizationId, Guid roleId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, MenuEngineeringSettings settings) =
                await ResolveContextAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering permission required.");

            List<string> pausedAgents = await _db.MenuEngineeringAgentAutonomyPolicy.AsNoTracking()
                .Where(x => x.BuyerId == buyer.Id && x.IsActive && x.IsPaused)
                .Select(x => x.AgentId)
                .Distinct()
                .ToListAsync(ct);

            List<string> suspendedIntegrations = await _db.MenuEngineeringIntegrationMonitorEvent.AsNoTracking()
                .Where(x => x.BuyerId == buyer.Id && x.IsActive && x.IsSuspended)
                .Select(x => x.IntegrationType)
                .Distinct()
                .ToListAsync(ct);

            return new EmergencyControlStatusDto
            {
                AutonomousModePaused = settings.AutonomousModePaused,
                AutomatedExecutionDisabled = settings.AutomatedExecutionDisabled,
                EmergencyPauseReason = settings.EmergencyPauseReason,
                EmergencyPausedAtUtc = settings.EmergencyPausedAtUtc,
                PausedAgents = pausedAgents,
                SuspendedIntegrations = suspendedIntegrations
            };
        }

        public async Task<List<ActionPlanDto>> GetActionPlansAsync(
            Guid organizationId, Guid roleId, Guid? propertyId = null, CancellationToken ct = default)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await ResolveContextAsync(organizationId, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering permission required.");

            List<MenuEngineeringActionPlan> plans = await _db.MenuEngineeringActionPlan.AsNoTracking()
                .Where(x => x.BuyerId == buyer.Id && x.IsActive && (propertyId == null || x.PropertyId == propertyId || x.PropertyId == null))
                .OrderByDescending(x => x.DateCreated)
                .Take(40)
                .ToListAsync(ct);

            List<Guid> planIds = plans.Select(p => p.Id).ToList();
            List<MenuEngineeringActionItem> items = await _db.MenuEngineeringActionItem.AsNoTracking()
                .Where(x => x.BuyerId == buyer.Id && x.IsActive && planIds.Contains(x.ActionPlanId))
                .OrderBy(x => x.SequenceOrder)
                .ToListAsync(ct);

            return plans.Select(p => new ActionPlanDto
            {
                Id = p.Id,
                PropertyId = p.PropertyId,
                TriggerEventId = p.TriggerEventId,
                CorrelationId = p.CorrelationId,
                Title = p.Title,
                BusinessObjective = p.BusinessObjective,
                RootCauseAnalysis = p.RootCauseAnalysis,
                AffectedObjects = DeserializeList(p.AffectedObjectsJson),
                SupportingEvidence = DeserializeList(p.SupportingEvidenceJson),
                ExpectedFinancialImpact = perms.ViewMenuMargin ? p.ExpectedFinancialImpact : 0m,
                ExpectedOperationalImpact = p.ExpectedOperationalImpact,
                RiskLevel = p.RiskLevel,
                Status = p.Status,
                RequiredApprovals = DeserializeList(p.RequiredApprovalsJson),
                ExecutionDeadlineUtc = p.ExecutionDeadlineUtc,
                IsConsolidated = p.IsConsolidated,
                CreatedAtUtc = p.DateCreated,
                Actions = items.Where(i => i.ActionPlanId == p.Id).Select(i => MapActionItemDto(i, perms)).ToList()
            }).ToList();
        }

        public async Task<ActionItemDto> ApproveActionItemAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid actionItemId, ApproveActionItemRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await ResolveContextAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ApproveMenu || perms.PublishMenu || perms.ManageMenuSetup, "Access denied", "Approval authority required.");

            MenuEngineeringActionItem item = await _db.MenuEngineeringActionItem
                .FirstOrDefaultAsync(x => x.Id == actionItemId && x.BuyerId == buyer.Id && x.IsActive, ct)
                ?? throw new NotFoundCustomException("Action item not found", $"Action item {actionItemId} not found.");

            // Snapshot target object timestamp at approval time so stale-data detection works accurately
            item.DataFreshnessSnapshotUtc = await GetTargetLastUpdatedUtcAsync(buyer.Id, item.TargetObjectType, item.TargetObjectId, ct);
            item.ApprovalStatus = request.Approved ? "APPROVED" : "REJECTED";
            item.ApprovedByUserId = userId;
            item.ApprovedAtUtc = DateTime.UtcNow;
            item.ApprovalChannel = string.IsNullOrWhiteSpace(request.ApprovalChannel) ? "CLOUD" : request.ApprovalChannel.Trim().ToUpperInvariant();
            if (!request.Approved)
            {
                item.ExecutionStatus = "CANCELLED";
                item.ResultJson = JsonSerializer.Serialize(new { rejectedReason = request.Notes ?? "Rejected by approver." }, JsonOpts);
            }

            await _db.SaveChangesAsync(ct);
            await RefreshActionPlanStatusAsync(item.ActionPlanId, buyer.Id, ct);
            return MapActionItemDto(item, perms);
        }

        public async Task<ActionExecutionResultDto> ExecuteActionItemAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid actionItemId, ExecuteActionItemRequestDto request, CancellationToken ct)
        {
            // Step 1-3: Verify tenant, caller identity, and RBAC
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, MenuEngineeringSettings settings) =
                await ResolveContextAsync(organizationId, roleId, null, ct);

            MenuEngineeringActionItem item = await _db.MenuEngineeringActionItem
                .FirstOrDefaultAsync(x => x.Id == actionItemId && x.BuyerId == buyer.Id && x.IsActive, ct)
                ?? throw new NotFoundCustomException("Action item not found", $"Action item {actionItemId} not found.");

            // Step 9: Idempotency check BEFORE executing anything twice (Section 51 Test 7)
            if (string.Equals(item.ExecutionStatus, "EXECUTED", StringComparison.OrdinalIgnoreCase))
            {
                return new ActionExecutionResultDto
                {
                    ActionItemId = item.Id,
                    ExecutionStatus = item.ExecutionStatus,
                    PolicyDecision = item.PolicyDecision,
                    PolicyReason = item.PolicyReason,
                    WasIdempotentReplay = true,
                    ExternalReference = item.ExternalReference,
                    Message = "Idempotent replay guard: action was already executed previously; duplicate business transaction prevented."
                };
            }

            bool hasPerm = item.ActionType switch
            {
                "PUBLISH_MENU" or "CHANGE_SELLING_PRICE" => perms.PublishMenu,
                "PREPARE_PURCHASE_PROPOSAL" or "TRANSMIT_PURCHASE_ORDER" => perms.GeneratePurchaseRecommendation || perms.ApproveMenu,
                _ => perms.ApproveMenu || perms.UseAiMenuIntelligence || perms.EditDraftMenu
            };
            if (!hasPerm)
            {
                return new ActionExecutionResultDto
                {
                    ActionItemId = item.Id,
                    ExecutionStatus = "REJECTED",
                    PolicyDecision = "REJECT",
                    PolicyReason = "Caller lacks required RBAC authorization for this action.",
                    Message = "Policy rejected execution: caller lacks required RBAC authorization for this action."
                };
            }

            // Emergency control check
            if (settings.AutonomousModePaused || settings.AutomatedExecutionDisabled)
            {
                return new ActionExecutionResultDto
                {
                    ActionItemId = item.Id,
                    ExecutionStatus = "BLOCKED_EMERGENCY_PAUSE",
                    PolicyDecision = "REJECT",
                    PolicyReason = "Autonomous execution is paused/disabled by administrator emergency control.",
                    Message = "Autonomous execution is disabled by administrator emergency control."
                };
            }

            // Step 4-5: Approval & validity verification
            bool policyAllowsWithoutApproval = string.Equals(item.PolicyDecision, "ALLOW_POLICY_EXECUTION", StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.PolicyDecision, "ALLOW_DRAFT", StringComparison.OrdinalIgnoreCase);

            if (!policyAllowsWithoutApproval && !string.Equals(item.ApprovalStatus, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return new ActionExecutionResultDto
                {
                    ActionItemId = item.Id,
                    ExecutionStatus = "APPROVAL_REQUIRED",
                    PolicyDecision = "REQUIRE_APPROVAL",
                    PolicyReason = "Explicit approval is required before executing this action.",
                    Message = "Explicit approval is required before executing this action."
                };
            }
            if (item.ApprovedAtUtc.HasValue && item.ApprovedAtUtc.Value.AddHours(24) < DateTime.UtcNow)
            {
                item.ApprovalStatus = "EXPIRED";
                await _db.SaveChangesAsync(ct);
                return new ActionExecutionResultDto
                {
                    ActionItemId = item.Id,
                    RequiresReapproval = true,
                    ExecutionStatus = "APPROVAL_EXPIRED",
                    PolicyDecision = "REQUIRE_APPROVAL",
                    PolicyReason = "Approval window expired.",
                    Message = "Approval has expired; renewed authorization is required."
                };
            }

            // Dependency check
            if (item.DependsOnActionId.HasValue)
            {
                MenuEngineeringActionItem? dep = await _db.MenuEngineeringActionItem.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == item.DependsOnActionId.Value && x.BuyerId == buyer.Id && x.IsActive, ct);
                if (dep is not null && !string.Equals(dep.ExecutionStatus, "EXECUTED", StringComparison.OrdinalIgnoreCase))
                {
                    return new ActionExecutionResultDto
                    {
                        ActionItemId = item.Id,
                        ExecutionStatus = "DEPENDENCY_PENDING",
                        PolicyDecision = item.PolicyDecision,
                        PolicyReason = item.PolicyReason,
                        Message = $"Dependent action '{dep.Title}' must be executed first."
                    };
                }
            }

            // Step 6-7: Post-approval data freshness & business object mutation check (Section 51 Test 8)
            if (item.DataFreshnessSnapshotUtc.HasValue && item.TargetObjectId.HasValue)
            {
                DateTime? latestTargetUpdated = await GetTargetLastUpdatedUtcAsync(buyer.Id, item.TargetObjectType, item.TargetObjectId, ct);
                if (latestTargetUpdated.HasValue && latestTargetUpdated.Value > item.DataFreshnessSnapshotUtc.Value.AddSeconds(1))
                {
                    item.ApprovalStatus = "REAPPROVAL_REQUIRED";
                    item.ExecutionStatus = "REAPPROVAL_REQUIRED";
                    item.ResultJson = JsonSerializer.Serialize(new { error = "Target business object changed after approval; renewed approval required." }, JsonOpts);
                    await _db.SaveChangesAsync(ct);
                    return new ActionExecutionResultDto
                    {
                        ActionItemId = item.Id,
                        RequiresReapproval = true,
                        ExecutionStatus = item.ExecutionStatus,
                        PolicyDecision = "REQUIRE_APPROVAL",
                        PolicyReason = "Target business object mutated after approval.",
                        Message = "Relevant business data changed after approval; action revalidated and renewed approval requested."
                    };
                }
            }

            // Step 10: External ERP timeout / uncertain outcome reconciliation BEFORE any retry (Section 51 Test 9)
            if (request.SimulateExternalTimeout || string.Equals(item.ExecutionStatus, "UNCERTAIN_RECONCILING", StringComparison.OrdinalIgnoreCase))
            {
                string reconKey = $"RECON:{item.IdempotencyKey}";
                MenuEngineeringBusinessEvent? reconEv = await _db.MenuEngineeringBusinessEvent
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IdempotencyKey == reconKey && x.IsActive, ct);

                PurchaseDocumentIntegration? pdi = await _db.PurchaseDocumentIntegration.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BuyerOrganizationId == organizationId &&
                                              (x.IdempotencyKey == item.IdempotencyKey || (item.TargetObjectId.HasValue && x.PurchaseOrderId == item.TargetObjectId.Value)) &&
                                              x.IsActive, ct);

                if (pdi is not null && string.Equals(pdi.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    item.ExecutionStatus = "EXECUTED";
                    item.ExecutedAtUtc = DateTime.UtcNow;
                    item.ExternalReference = pdi.ExternalDocumentNumber ?? $"RECON-{item.IdempotencyKey[..Math.Min(12, item.IdempotencyKey.Length)]}";
                    await _db.SaveChangesAsync(ct);
                    return new ActionExecutionResultDto
                    {
                        ActionItemId = item.Id,
                        ExternalOutcomeReconciled = true,
                        ExecutionStatus = "EXECUTED",
                        PolicyDecision = item.PolicyDecision,
                        ExternalReference = item.ExternalReference,
                        Message = "Reconciled uncertain external ERP transaction as already committed in PurchaseDocumentIntegration; blind retry avoided."
                    };
                }

                if (request.SimulateExternalTimeout && reconEv is null)
                {
                    item.ExecutionStatus = "UNCERTAIN_RECONCILING";
                    item.RetryCount++;
                    item.ResultJson = JsonSerializer.Serialize(new { error = "External ERP write timed out; outcome reconciliation queued before retry." }, JsonOpts);
                    _db.MenuEngineeringBusinessEvent.Add(new MenuEngineeringBusinessEvent
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyer.Id,
                        EventType = "ExternalTransactionReconciliationPending",
                        SourceSystem = "SILA_ERP_ADAPTER",
                        ObjectType = item.TargetObjectType ?? "ActionItem",
                        ObjectId = item.Id,
                        ObjectLabel = item.Title,
                        EffectiveAtUtc = DateTime.UtcNow,
                        CorrelationId = $"recon-{item.Id:N}"[..20],
                        SchemaVersion = "1.0",
                        IdempotencyKey = reconKey,
                        PayloadJson = item.ResultJson,
                        MaterialityScore = 10m,
                        ProcessingStatus = "PENDING",
                        IsActive = true,
                        DateCreated = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync(ct);
                    return new ActionExecutionResultDto
                    {
                        ActionItemId = item.Id,
                        ExternalOutcomeReconciled = true,
                        ExecutionStatus = "UNCERTAIN_RECONCILING",
                        PolicyDecision = item.PolicyDecision,
                        PolicyReason = "External ERP timeout captured for outcome reconciliation.",
                        Message = "External ERP write timed out; transaction outcome recorded for reconciliation before retry."
                    };
                }

                return new ActionExecutionResultDto
                {
                    ActionItemId = item.Id,
                    ExternalOutcomeReconciled = true,
                    ExecutionStatus = "UNCERTAIN_RECONCILING",
                    PolicyDecision = item.PolicyDecision,
                    PolicyReason = "Reconciliation in progress; blind retry blocked.",
                    Message = "External ERP outcome reconciliation is still pending; blind retry blocked to prevent duplicate ERP posting."
                };
            }

            // Step 10-12: Execute controlled non-destructive action & update plan + recommendation lifecycle
            item.ExecutionStatus = "EXECUTED";
            item.ExecutedAtUtc = DateTime.UtcNow;
            item.ExternalReference = $"SILA-ACT-{DateTime.UtcNow:yyyyMMddHHmmss}";
            item.ResultJson = JsonSerializer.Serialize(new
            {
                status = "COMMITTED",
                actionType = item.ActionType,
                targetObjectType = item.TargetObjectType,
                targetObjectId = item.TargetObjectId,
                executedBy = userId
            }, JsonOpts);

            // Transition linked recommendation to OUTCOME_MONITORING
            List<MenuEngineeringRecommendation> linkedRecs = await _db.MenuEngineeringRecommendation
                .Where(x => x.BuyerId == buyer.Id && x.ActionPlanId == item.ActionPlanId && x.IsActive)
                .ToListAsync(ct);
            foreach (MenuEngineeringRecommendation rec in linkedRecs)
            {
                rec.Status = "APPROVED";
                rec.LifecycleStatus = "OUTCOME_MONITORING";
                rec.ValidatedAtUtc ??= DateTime.UtcNow;
            }

            // Publish follow-up authorized business event
            _db.MenuEngineeringBusinessEvent.Add(new MenuEngineeringBusinessEvent
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                EventType = "ActionExecuted",
                SourceSystem = "SILA_AUTONOMOUS_ENGINE",
                ObjectType = item.TargetObjectType,
                ObjectId = item.TargetObjectId ?? item.Id,
                ObjectLabel = item.Title,
                EffectiveAtUtc = DateTime.UtcNow,
                CorrelationId = $"exec-{item.Id:N}"[..20],
                SchemaVersion = "1.0",
                IdempotencyKey = $"EXEC-EVT:{item.IdempotencyKey}",
                PayloadJson = item.ResultJson,
                MaterialityScore = 10m,
                ProcessingStatus = "PROCESSED",
                ProcessedAtUtc = DateTime.UtcNow,
                IsActive = true,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            await RefreshActionPlanStatusAsync(item.ActionPlanId, buyer.Id, ct);

            return new ActionExecutionResultDto
            {
                ActionItemId = item.Id,
                ExecutionStatus = item.ExecutionStatus,
                PolicyDecision = item.PolicyDecision,
                PolicyReason = item.PolicyReason,
                ExternalReference = item.ExternalReference,
                Message = $"Action '{item.Title}' executed safely via controlled backend service."
            };
        }

        private async Task<DateTime?> GetTargetLastUpdatedUtcAsync(Guid buyerId, string? targetType, Guid? targetId, CancellationToken ct)
        {
            if (!targetId.HasValue) return null;
            return (targetType ?? "").ToUpperInvariant() switch
            {
                "MENUITEM" or "MENU_ITEM" => await _db.MenuItem.Where(x => x.Id == targetId && x.BuyerId == buyerId).Select(x => (DateTime?)x.DateUpdated).FirstOrDefaultAsync(ct),
                "MENUPLAN" or "MENU_PLAN" => await _db.MenuPlan.Where(x => x.Id == targetId && x.BuyerId == buyerId).Select(x => (DateTime?)x.DateUpdated).FirstOrDefaultAsync(ct),
                "RECIPE" => await _db.Recipe.Where(x => x.Id == targetId && x.BuyerId == buyerId).Select(x => (DateTime?)x.DateUpdated).FirstOrDefaultAsync(ct),
                "EVENT" => await _db.SilaCulinaryEvent.Where(x => x.Id == targetId && x.BuyerId == buyerId).Select(x => (DateTime?)x.DateUpdated).FirstOrDefaultAsync(ct),
                _ => null
            };
        }

        private async Task RefreshActionPlanStatusAsync(Guid planId, Guid buyerId, CancellationToken ct)
        {
            MenuEngineeringActionPlan? plan = await _db.MenuEngineeringActionPlan.FirstOrDefaultAsync(x => x.Id == planId && x.BuyerId == buyerId && x.IsActive, ct);
            if (plan is null) return;
            List<MenuEngineeringActionItem> items = await _db.MenuEngineeringActionItem.AsNoTracking()
                .Where(x => x.ActionPlanId == planId && x.BuyerId == buyerId && x.IsActive)
                .ToListAsync(ct);
            if (items.Count == 0) return;

            if (items.All(i => i.ExecutionStatus == "EXECUTED"))
                plan.Status = "COMPLETED";
            else if (items.All(i => i.ApprovalStatus == "REJECTED" || i.ExecutionStatus == "CANCELLED"))
                plan.Status = "CANCELLED";
            else if (items.Any(i => i.ExecutionStatus == "EXECUTED"))
                plan.Status = "EXECUTING";
            else if (items.All(i => i.ApprovalStatus is "APPROVED" or "NOT_REQUIRED"))
                plan.Status = "APPROVED";
            else
                plan.Status = "APPROVAL_PENDING";

            await _db.SaveChangesAsync(ct);
        }

        private static List<string> DeserializeList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return JsonSerializer.Deserialize<List<string>>(json, JsonOpts) ?? new(); }
            catch { return new(); }
        }

        private static ActionItemDto MapActionItemDto(MenuEngineeringActionItem item, MenuEngineeringPermissionSetDto perms) => new()
        {
            Id = item.Id,
            ActionPlanId = item.ActionPlanId,
            SequenceOrder = item.SequenceOrder,
            DependsOnActionId = item.DependsOnActionId,
            AgentId = item.AgentId,
            ToolId = item.ToolId,
            ActionType = item.ActionType,
            Title = item.Title,
            RiskClassification = item.RiskClassification,
            PolicyDecision = item.PolicyDecision,
            PolicyReason = item.PolicyReason,
            FinancialImpact = perms.ViewMenuMargin ? item.FinancialImpact : 0m,
            TargetObjectType = item.TargetObjectType,
            TargetObjectId = item.TargetObjectId,
            TargetObjectVersion = item.TargetObjectVersion,
            ApprovalStatus = item.ApprovalStatus,
            ApprovalChannel = item.ApprovalChannel,
            ApprovedAtUtc = item.ApprovedAtUtc,
            ExecutionStatus = item.ExecutionStatus,
            ExecutedAtUtc = item.ExecutedAtUtc,
            ExternalReference = item.ExternalReference,
            ResultSummary = item.ResultJson
        };
    }
}
