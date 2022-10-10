using System;
using Microsoft.Extensions.Configuration;
using AccelByte.Sdk.Core.Logging;
using AccelByte.Sdk.Core.Repository;

namespace AccelByte.PluginArch.Demo.Server
{
    public class AppSettingConfigRepository : IConfigRepository
    {
        public string BaseUrl { get; set; } = String.Empty;

        public string ClientId { get; set; } = String.Empty;

        public string ClientSecret { get; set; } = String.Empty;

        public string AppName { get; set; } = String.Empty;

        public string TraceIdVersion { get; set; } = String.Empty;

        public string Namespace { get; set; } = String.Empty;

        public bool EnableTraceId { get; set; } = false;

        public bool EnableUserAgentInfo { get; set; } = false;

        public string ResourceName { get; set; } = String.Empty;

        public IHttpLogger? Logger { get; set; } = null;
    }
}
