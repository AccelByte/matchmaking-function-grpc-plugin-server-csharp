using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using OpenTelemetry;

using AccelByte.Sdk.Core;
using AccelByte.Sdk.Feature.AutoTokenRefresh;
using AccelByte.Sdk.Feature.LocalTokenValidation;

using AccelByte.Sdk.Api;
using AccelByte.Sdk.Api.Iam.Model;


namespace AccelByte.PluginArch.Demo.Server
{
    public class DefaultAccelByteServiceProvider : IAccelByteServiceProvider
    {
        private ILogger<DefaultAccelByteServiceProvider> _Logger;

        public AccelByteSDK Sdk { get; }

        public AppSettingConfigRepository Config { get; }

        public DefaultAccelByteServiceProvider(IConfiguration config, ILogger<DefaultAccelByteServiceProvider> logger)
        {
            _Logger = logger;
            Config = config.GetSection("AccelByte").Get<AppSettingConfigRepository>();
            Sdk = AccelByteSDK.Builder
                .SetConfigRepository(Config)
                .UseDefaultCredentialRepository()
                .SetHttpClient(new PluginArchHttpClient())
                .UseDefaultTokenRepository()
                .UseAutoTokenRefresh()
                .UseAutoRefreshForTokenRevocationList()
                .Build();
        }
    }
}