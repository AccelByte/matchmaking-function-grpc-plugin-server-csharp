// Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

using AccelByte.PluginArch.Demo.Server.Services;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Extensions;
using OpenTelemetry.Extensions.Propagators;
using System.Diagnostics;

namespace AccelByte.PluginArch.Demo.Server
{
    internal class Program
    {
        static int Main(string[] args)
        {
            OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new B3Propagator());

            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddEnvironmentVariables("ABSERVER_");

            builder.Services.AddSingleton<IAccelByteServiceProvider, DefaultAccelByteServiceProvider>();
            //builder.Services.AddSingleton<RevocationListRefresher>();
            builder.Services.AddHostedService<RevocationListRefresher>();

            //Get Config
            AppSettingConfigRepository appConfig = builder.Configuration.GetSection("AccelByte").Get<AppSettingConfigRepository>();
            bool enableAuthorization = builder.Configuration.GetValue<bool>("EnableAuthorization");

            builder.Services.AddOpenTelemetryTracing((traceConfig) =>
            {
                var asVersion = Assembly.GetEntryAssembly()!.GetName().Version;
                string version = "0.0.0";
                if (asVersion != null)
                    version = asVersion.ToString();

                traceConfig
                    .AddSource(appConfig.ResourceName)
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(appConfig.ResourceName, null, version))
                    //.AddConsoleExporter()
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
            
            app.MapGrpcService<MatchFunctionService>();

            if (app.Environment.IsDevelopment())
            {
                app.MapGrpcReflectionService();
            }

            // Warmup required provider.
            app.Services.GetService<IAccelByteServiceProvider>();

            app.Run();

            return 0;
        }
    }
}