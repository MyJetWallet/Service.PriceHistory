using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.PriceHistory.Jobs;

namespace Service.PriceHistory
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly PriceUpdatingJob _priceUpdatingJob;
        private readonly ILogger<ApplicationLifetimeManager> _logger;

        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime, ILogger<ApplicationLifetimeManager> logger, PriceUpdatingJob priceUpdatingJob)
            : base(appLifetime)
        {
            _logger = logger;
            _priceUpdatingJob = priceUpdatingJob;
        }

        protected override void OnStarted()
        {
            _priceUpdatingJob.Start();
            _logger.LogInformation("OnStarted has been called.");
        }

        protected override void OnStopping()
        {
            _priceUpdatingJob.Dispose();
            _logger.LogInformation("OnStopping has been called.");
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}
