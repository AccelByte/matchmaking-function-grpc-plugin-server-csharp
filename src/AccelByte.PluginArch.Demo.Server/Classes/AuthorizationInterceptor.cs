// Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Grpc.Core;
using Grpc.Core.Interceptors;

using AccelByte.Sdk.Core;
using AccelByte.Sdk.Api;
using System.Security.Cryptography.Xml;

namespace AccelByte.PluginArch.Demo.Server
{
    public class AuthorizationInterceptor : Interceptor
    {
        private readonly ILogger<AuthorizationInterceptor> _Logger;

        private readonly AccelByteSDK _ABSdk;

        private readonly string _Namespace;

        private readonly string _ResourceName;

        public AuthorizationInterceptor(ILogger<AuthorizationInterceptor> logger, IAccelByteServiceProvider abSdkProvider)
        {
            _Logger = logger;
            _ABSdk = abSdkProvider.Sdk;
            _Namespace = abSdkProvider.Config.Namespace;
            _ResourceName = abSdkProvider.Config.ResourceName;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                string? authToken = context.RequestHeaders.GetValue("authorization");
                if (authToken == null)
                    throw new RpcException(new Status(StatusCode.Unauthenticated, "No authorization token provided."));

                string[] authParts = authToken.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (authParts.Length != 2)
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid authorization token format"));

                string accessToken = authParts[1];

                var verifyResponse = _ABSdk.Iam.OAuth20.VerifyTokenV3Op
                    .SetPreferredSecurityMethod(Operation.SECURITY_BASIC)
                    .Execute(authParts[1]);
                if (verifyResponse == null)
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid authorization token value."));

                string qPermission = $"NAMESPACE:{_Namespace}:{_ResourceName}";

                var permissionList = verifyResponse.Permissions;
                if (permissionList == null)
                    throw new RpcException(new Status(StatusCode.PermissionDenied, "Unauthorized call."));

                bool foundMatchingPermission = false;
                foreach (var permission in permissionList)
                {
                    if (permission.Resource == qPermission)
                    {
                        foundMatchingPermission = true;
                        break;
                    }   
                }

                if (!foundMatchingPermission)
                    throw new RpcException(new Status(StatusCode.PermissionDenied, "Unauthorized call."));

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