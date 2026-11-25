using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpsertSilaUserPoFlexConfig
{
    public class UpsertSilaUserPoFlexConfigCommandHandler : IRequestHandler<UpsertSilaUserPoFlexConfigCommand, SilaUserPoFlexConfigDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpsertSilaUserPoFlexConfigCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaUserPoFlexConfigDto> Handle(UpsertSilaUserPoFlexConfigCommand request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            SilaUserPoFlexConfigWriteDto input = request.Request ?? new SilaUserPoFlexConfigWriteDto();
            Guid targetUserId = request.AdminScope
                ? ResolveTargetUserId(input)
                : request.UserId;
            string configType = SilaUserPoFlexRules.NormalizeType(input.ConfigType);

            if (input.Parameters == null || input.Parameters.Count == 0)
            {
                throw new BadRequestCustomException("Parameters required.", "Add at least one parameter name and value.");
            }

            SilaUserPoFlexConfig row;
            if (request.Id != null)
            {
                await SilaUserPoFlexRules.EnsureOwnedAsync(_repository, _logger, buyer.Id, request.Id.Value);
                row = (await _repository.SilaUserPoFlexConfig
                    .FindFirstByConditionAsync(x => x.Id == request.Id && x.BuyerId == buyer.Id && x.IsActive))!;
                row.ConfigVersion += 1;
            }
            else
            {
                row = new SilaUserPoFlexConfig
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    ConfigVersion = 1
                };
                _repository.SilaUserPoFlexConfig.Create(row);
            }

            row.UserId = targetUserId;
            row.Username = Clean(input.Username, 200);
            row.DisplayName = Clean(input.DisplayName, 200);
            row.ConfigType = configType;
            row.Name = Clean(input.Name, 200) ?? $"{configType} defaults";
            row.IsDefault = input.IsDefault;
            row.Notes = Clean(input.Notes, 1000);
            row.Status = SilaUserPoFlexRules.STATUS_ACTIVE;
            row.IsActive = true;

            // Replace child rows (flex editing).
            List<SilaUserPoFlexParam> existingParams = await _repository.SilaUserPoFlexParam
                .FindByCondition(x => x.ConfigId == row.Id && x.IsActive).ToListAsync(cancellationToken);
            foreach (SilaUserPoFlexParam old in existingParams)
            {
                old.IsActive = false;
                _repository.SilaUserPoFlexParam.Update(old);
            }

            List<SilaUserPoFlexCondition> existingConditions = await _repository.SilaUserPoFlexCondition
                .FindByCondition(x => x.ConfigId == row.Id && x.IsActive).ToListAsync(cancellationToken);
            foreach (SilaUserPoFlexCondition old in existingConditions)
            {
                old.IsActive = false;
                _repository.SilaUserPoFlexCondition.Update(old);
            }

            int order = 0;
            List<SilaUserPoFlexParam> savedParams = new();
            foreach (SilaUserPoFlexParamDto param in input.Parameters)
            {
                string key = (param.ParamKey ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                SilaUserPoFlexParam entity = new()
                {
                    Id = Guid.NewGuid(),
                    ConfigId = row.Id,
                    ParamKey = key.Length > 100 ? key[..100] : key,
                    ParamValue = Clean(param.ParamValue, 500),
                    SortOrder = param.SortOrder > 0 ? param.SortOrder : order++,
                    IsActive = true
                };
                _repository.SilaUserPoFlexParam.Create(entity);
                savedParams.Add(entity);
            }

            order = 0;
            List<SilaUserPoFlexCondition> savedConditions = new();
            foreach (SilaUserPoFlexConditionDto condition in input.Conditions ?? new())
            {
                string whenParam = (condition.WhenParam ?? string.Empty).Trim();
                string thenParam = (condition.ThenParam ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(whenParam) || string.IsNullOrWhiteSpace(thenParam))
                {
                    continue;
                }

                string op = (condition.WhenOperator ?? SilaUserPoFlexRules.OP_EQ).Trim().ToUpperInvariant();
                if (op is not (SilaUserPoFlexRules.OP_EQ or SilaUserPoFlexRules.OP_NE or SilaUserPoFlexRules.OP_CONTAINS))
                {
                    op = SilaUserPoFlexRules.OP_EQ;
                }

                SilaUserPoFlexCondition entity = new()
                {
                    Id = Guid.NewGuid(),
                    ConfigId = row.Id,
                    WhenParam = whenParam.Length > 100 ? whenParam[..100] : whenParam,
                    WhenOperator = op,
                    WhenValue = Clean(condition.WhenValue, 500),
                    ThenParam = thenParam.Length > 100 ? thenParam[..100] : thenParam,
                    ThenValue = Clean(condition.ThenValue, 500),
                    SortOrder = condition.SortOrder > 0 ? condition.SortOrder : order++,
                    IsActive = true
                };
                _repository.SilaUserPoFlexCondition.Create(entity);
                savedConditions.Add(entity);
            }

            if (row.IsDefault)
            {
                await SilaUserPoFlexRules.ClearOtherDefaultsAsync(_repository, buyer.Id, targetUserId, configType, row.Id, cancellationToken);
            }

            if (request.Id != null)
            {
                _repository.SilaUserPoFlexConfig.Update(row);
            }

            InventoryLedger ledger = new(_repository, buyer.Id, request.UserId);
            ledger.AddEvent(
                Common.SILA_REF_USER_PO_PROFILE,
                row.Id,
                request.Id == null ? Common.AUDIT_CREATED : Common.AUDIT_MODIFIED,
                $"{configType}:{row.Name}");
            await _repository.SaveAsync();
            _logger.LogInfo($"PO flex config upserted. Id: {row.Id}, UserId: {row.UserId}, Type: {configType}");
            return SilaUserPoFlexRules.ToDto(row, savedParams, savedConditions);
        }

        private static Guid ResolveTargetUserId(SilaUserPoFlexConfigWriteDto input)
        {
            string? raw = (input.UserId ?? string.Empty).Trim();
            if (Guid.TryParse(raw, out Guid parsed))
            {
                return parsed;
            }

            if (!string.IsNullOrWhiteSpace(raw))
            {
                throw new BadRequestCustomException(
                    "User ID must be a GUID.",
                    $"'{raw}' is not a user GUID. Use “Use signed-in user” or paste the user’s GUID (login names are not accepted here).");
            }

            string? username = (input.Username ?? string.Empty).Trim();
            if (Guid.TryParse(username, out parsed))
            {
                return parsed;
            }

            throw new BadRequestCustomException(
                "User ID is required.",
                "Enter the user GUID for this PO defaults config.");
        }

        private static string? Clean(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            return trimmed.Length <= max ? trimmed : trimmed[..max];
        }
    }
}
