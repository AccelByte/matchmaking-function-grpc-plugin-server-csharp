using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AccelByte.PluginArch.Demo.Server
{
    public class RevocationListRefresher : BackgroundService
    {
        private readonly ILogger<RevocationListRefresher> _Logger;

        private readonly IAccelByteServiceProvider _ABProvider;

        private readonly TimeSpan _Period;

        public RevocationListRefresher(
            IAccelByteServiceProvider aBProvider,
            ILogger<RevocationListRefresher> logger,
            IConfiguration config)
        {
            _ABProvider = aBProvider;
            _Logger = logger;

            int period = config.GetValue<int>("RevocationListRefreshPeriod");
            _Period = TimeSpan.FromSeconds(period);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            PeriodicTimer timer = new PeriodicTimer(_Period);
            while (
                !stoppingToken.IsCancellationRequested &&
                await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    _ABProvider.RefreshRevocationList();

                    _Logger.LogInformation($"Revocation list refreshed.");
                }
                catch (Exception x)
                {
                    _Logger.LogError($"Failed to refresh revocation list. {x.Message}");
                }
            }
        }


    }
}
