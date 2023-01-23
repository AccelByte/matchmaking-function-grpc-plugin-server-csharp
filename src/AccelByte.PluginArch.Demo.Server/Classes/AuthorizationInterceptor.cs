// Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading.Tasks;
using System.Security.Cryptography.Xml;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Grpc.Core;
using Grpc.Core.Interceptors;

using AccelByte.Sdk.Core;
using AccelByte.Sdk.Api;
using AccelByte.Sdk.Feature.LocalTokenValidation;

namespace AccelByte.PluginArch.Demo.Server
{
    public class AuthorizationInterceptor : Interceptor
    {
        private readonly ILogger<AuthorizationInterceptor> _Logger;

        private readonly IAccelByteServiceProvider _ABProvider;

        private readonly string _Namespace;

        private readonly string _ResourceName;

        public AuthorizationInterceptor(ILogger<AuthorizationInterceptor> logger, IAccelByteServiceProvider abSdkProvider)
        {
            _Logger = logger;
            _ABProvider = abSdkProvider;
            _Namespace = abSdkProvider.Config.Namespace;
            _ResourceName = abSdkProvider.Config.ResourceName;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            string qPermission = $"NAMESPACE:{_Namespace}:{_ResourceName}";

            try
            {
                string? authToken = context.RequestHeaders.GetValue("authorization");
                if (authToken == null)
                    throw new RpcException(new Status(StatusCode.Unauthenticated, "No authorization token provided."));

                string[] authParts = authToken.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (authParts.Length != 2)
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid authorization token format"));

                bool b = _ABProvider.Sdk.ValidateToken(authParts[1], qPermission, 2);
                if (!b)
                    throw new Exception("validation failed");

                return await continuation(request, context);
            }
            catch (Exception x)
            {
                _Logger.LogError(x, $"Authorization error: {x.Message}");
                throw new RpcException(new Status(StatusCode.Unauthenticated, x.Message));
            }
        }
    }
}