using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using AccelByte.Sdk.Core;
using AccelByte.Sdk.Feature.AutoTokenRefresh;

namespace AccelByte.PluginArch.Demo.Server
{
    public class DefaultAccelByteServiceProvider : IAccelByteServiceProvider
    {
        public AccelByteSDK Sdk { get; }

        public AppSettingConfigRepository Config { get; }

        public DefaultAccelByteServiceProvider(IConfiguration config)
        {
            Config = config.GetSection("AccelByte").Get<AppSettingConfigRepository>();
            Sdk = AccelByteSDK.Builder
                .SetConfigRepository(Config)
                .UseDefaultCredentialRepository()
                .UseDefaultHttpClient()
                .UseDefaultTokenRepository()
                .UseAutoTokenRefresh()
                .Build();
        }
    }
}