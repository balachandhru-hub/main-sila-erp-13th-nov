using Buyer.Application.Features.Shared;
using Buyer.Application.Features.Shared.MenuEngineering.Autonomous;
using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Services.Jobs
{
    /// <summary>
    /// Periodic autonomous culinary intelligence runner. Processes pending business events,
    /// monitors integration & data readiness, and generates role-specific daily briefings
    /// independently per tenant without blocking core ERP or procurement V1 workflows.
    /// </summary>
    [DisallowConcurrentExecution]
    public class MenuEngineeringAutonomousJob : IJob
    {
        private const string JOB_NAME = nameof(MenuEngineeringAutonomousJob);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILoggerManager _logger;

        public MenuEngineeringAutonomousJob(IServiceScopeFactory scopeFactory, ILoggerManager logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInfo($"[{JOB_NAME}] start");
            try
            {
                List<Guid> buyerIds;
                using (IServiceScope scope = _scopeFactory.CreateScope())
                {
                    RepositoryContext db = scope.ServiceProvider.GetRequiredService<RepositoryContext>();
                    buyerIds = await db.BuyerBusinessProfile.AsNoTracking()
                        .Where(x => x.IsActive)
                        .Select(x => x.Id)
                        .Take(100)
                        .ToListAsync(context.CancellationToken);
                }

                int succeeded = 0;
                int failed = 0;

                foreach (Guid buyerId in buyerIds)
                {
                    if (context.CancellationToken.IsCancellationRequested) break;
                    try
                    {
                        using IServiceScope tenantScope = _scopeFactory.CreateScope();
                        IMenuEngineeringAutonomousService svc = tenantScope.ServiceProvider.GetRequiredService<IMenuEngineeringAutonomousService>();
                        await svc.RunScheduledAutonomousCycleForBuyerAsync(buyerId, context.CancellationToken);
                        succeeded++;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        failed++;
                        _logger.LogError($"[{JOB_NAME}] Buyer {buyerId} error: {SilaLogText.Short(ex.Message)}");
                    }
                }

                _logger.LogInfo($"[{JOB_NAME}] end. Buyers: {buyerIds.Count}, Succeeded: {succeeded}, Failed: {failed}");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError($"[{JOB_NAME}] end with error. Error: {SilaLogText.Short(exception.Message)}");
            }
        }
    }
}
