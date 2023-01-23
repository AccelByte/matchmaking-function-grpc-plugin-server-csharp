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

            //Get Config
            AppSettingConfigRepository appConfig = builder.Configuration.GetSection("AccelByte").Get<AppSettingConfigRepository>();
            bool enableAuthorization = builder.Configuration.GetValue<bool>("EnableAuthorization");
            bool directLogToLoki = builder.Configuration.GetValue<bool>("DirectLogToLoki");

            if (directLogToLoki)
            {
                string? srLokiUrl = Environment.GetEnvironmentVariable("ASPNETCORE_SERILOG_LOKI");
                if (srLokiUrl == null)
                    srLokiUrl = builder.Configuration.GetValue<string>("LokiUrl");

                builder.Host.UseSerilog((ctx, cfg) =>
                {
                    cfg.MinimumLevel
                        .Override("Microsoft", LogEventLevel.Information)
                        .Enrich.FromLogContext()
                        .WriteTo.GrafanaLoki(srLokiUrl,new List<LokiLabel>()
                        {
                            new LokiLabel()
                            {
                                Key = "application",
                                Value = appConfig.ResourceName
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

            builder.Services
                .AddSingleton<IAccelByteServiceProvider, DefaultAccelByteServiceProvider>()
                .AddOpenTelemetryTracing((traceConfig) =>
                {
                    var asVersion = Assembly.GetEntryAssembly()!.GetName().Version;
                    string version = "0.0.0";
                    if (asVersion != null)
                        version = asVersion.ToString();

                    traceConfig
                        .AddSource(appConfig.ResourceName)
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService(appConfig.ResourceName, null, version)
                            .AddTelemetrySdk())
                        .AddZipkinExporter()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation();
                });                

            // Additional configuration is required to successfully run gRPC on macOS.
            // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
            
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
            if (app.Environment.IsDevelopment())
            {
                app.MapGrpcReflectionService();
            }

            app.MapMetrics();
            app.Run();
            return 0;
        }
    }
}