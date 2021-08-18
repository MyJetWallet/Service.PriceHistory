using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.DataReader;
using Service.PriceHistory.Jobs;

namespace Service.PriceHistory
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly PriceUpdatingJob _priceUpdatingJob;
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly MyNoSqlTcpClient[] _myNoSqlTcpClientManagers;

        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime, ILogger<ApplicationLifetimeManager> logger, PriceUpdatingJob priceUpdatingJob, MyNoSqlTcpClient[] myNoSqlTcpClientManagers)
            : base(appLifetime)
        {
            _logger = logger;
            _priceUpdatingJob = priceUpdatingJob;
            _myNoSqlTcpClientManagers = myNoSqlTcpClientManagers;
        }

        protected override void OnStarted()
        {
            foreach(var client in _myNoSqlTcpClientManagers)
            {
                client.Start();
            }
            _priceUpdatingJob.Start();
            _logger.LogInformation("OnStarted has been called.");
        }

        protected override void OnStopping()
        {
            _priceUpdatingJob.Dispose();
            foreach(var client in _myNoSqlTcpClientManagers)
            {
                client.Stop();
            }
            _logger.LogInformation("OnStopping has been called.");
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}
