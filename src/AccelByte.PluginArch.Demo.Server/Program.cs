// Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System.Net.Http;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Extensions;
using OpenTelemetry.Extensions.Propagators;

using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

using Prometheus;

using AccelByte.PluginArch.Demo.Server.Services;
using AccelByte.PluginArch.Demo.Server.Metric;

namespace AccelByte.PluginArch.Demo.Server
{
    internal class Program
    {
        static int Main(string[] args)
        {
            OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new B3Propagator());
            Metrics.DefaultRegistry.SetStaticLabels(new Dictionary<string, string>()
            {
                { "application", "mm_grpcserver" }
            });

            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddEnvironmentVariables("ABSERVER_");
            builder.WebHost.ConfigureKestrel(opt =>
            {
                opt.AllowAlternateSchemes = true;
            });

            string? appResourceName = Environment.GetEnvironmentVariable("APP_RESOURCE_NAME");
            if (appResourceName == null)
                appResourceName = "MMV2GRPCSERVICE ";

            bool enableAuthorization = builder.Configuration.GetValue<bool>("EnableAuthorization");
            string? strEnableAuth = Environment.GetEnvironmentVariable("PLUGIN_GRPC_SERVER_AUTH_ENABLED");
            if ((strEnableAuth != null) && (strEnableAuth != String.Empty))
                enableAuthorization = (strEnableAuth.Trim().ToLower() == "true");

            bool directLogToLoki = builder.Configuration.GetValue<bool>("DirectLogToLoki");
            if (directLogToLoki)
            {
                string? srLokiUrl = Environment.GetEnvironmentVariable("ASPNETCORE_SERILOG_LOKI");
                if (srLokiUrl == null)
                    srLokiUrl = builder.Configuration.GetValue<string>("LokiUrl");
                if (srLokiUrl != null)
                {
                    builder.Host.UseSerilog((ctx, cfg) =>
                    {
                        cfg.MinimumLevel
                            .Override("Microsoft", LogEventLevel.Information)
                            .Enrich.FromLogContext()
                            .WriteTo.GrafanaLoki(srLokiUrl, new List<LokiLabel>()
                            {
                            new LokiLabel()
                            {
                                Key = "application",
                                Value = appResourceName
                            },
                            new LokiLabel()
                            {
                                Key = "env",
                                Value = ctx.HostingEnvironment.EnvironmentName
                            }
                            })
                            .WriteTo.Console(new RenderedCompactJsonFormatter());
                    });
                }
            }

            builder.Services
                .AddSingleton<IAccelByteServiceProvider, DefaultAccelByteServiceProvider>()
                .AddOpenTelemetry()
                .WithTracing((traceConfig) =>
                {
                    var asVersion = Assembly.GetEntryAssembly()!.GetName().Version;
                    string version = "0.0.0";
                    if (asVersion != null)
                        version = asVersion.ToString();

                    traceConfig
                        .AddSource(appResourceName)
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService(appResourceName, null, version)
                            .AddTelemetrySdk())
                        .AddZipkinExporter()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation();
                });

            // Additional configuration is required to successfully run gRPC on macOS.
            // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
            
            builder.Services.AddGrpcHealthChecks()
                            .AddCheck("Health", () => HealthCheckResult.Healthy());

            builder.Services.AddGrpc((opts) =>
            {
                opts.Interceptors.Add<ExceptionHandlingInterceptor>();
                opts.Interceptors.Add<DebugLoggerServerInterceptor>();
                if (enableAuthorization)
                    opts.Interceptors.Add<AuthorizationInterceptor>();
            });
            builder.Services.AddGrpcReflection();

            var app = builder.Build();
            app.UseGrpcMetrics();
            

            
            app.MapGrpcService<MatchFunctionService>();
            app.MapGrpcReflectionService();

            app.MapGrpcHealthChecksService();
            app.MapMetrics();
            app.Run();
            return 0;
        }
    }
}
