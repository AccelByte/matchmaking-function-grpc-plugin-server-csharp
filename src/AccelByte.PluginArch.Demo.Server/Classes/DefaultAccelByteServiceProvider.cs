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

using AccelByte.Sdk.Core;
using AccelByte.Sdk.Feature.AutoTokenRefresh;

using AccelByte.Sdk.Api.Iam.Model;
using AccelByte.Sdk.Core.Client;
using Microsoft.VisualBasic;
using AccelByte.Sdk.Api;
using OpenTelemetry;

namespace AccelByte.PluginArch.Demo.Server
{
    public class DefaultAccelByteServiceProvider : IAccelByteServiceProvider
    {
        private ILogger<DefaultAccelByteServiceProvider> _Logger;

        private Dictionary<string, OauthcommonJWKKey> _Keys;


        private object _RL_Lock = new object();

        private BloomFilter? _RL_TokenCache = null;

        private List<OauthcommonUserRevocationListRecord>? _RL_UserCache = null;


        public AccelByteSDK Sdk { get; }

        public AppSettingConfigRepository Config { get; }

        public static byte[] FromBase64Url(string source)
        {
            string temp = (source.Length % 4 == 0 ? source : source + "====".Substring(source.Length % 4))
                .Replace("_", "/").Replace("-", "+");
            return Convert.FromBase64String(temp);
        }

        protected void FetchJWKS()
        {
            _Logger.LogInformation("Fetching JWKS from AB IAM service.");

            OauthcommonJWKSet? tempResp = Sdk.Iam.OAuth20.GetJWKSV3Op
                .SetPreferredSecurityMethod(Operation.SECURITY_BASIC)
                .Execute();
            if (tempResp == null)
                throw new Exception("Get JWKS response is NULL");

            if (tempResp.Keys == null)
                throw new Exception("JWKS keys is null.");

            _Keys.Clear();
            foreach (var item in tempResp.Keys)
                _Keys.Add(item.Kid!, item);

            _Logger.LogInformation("JWKS fetched.");

            /*string endpoint = $"{Sdk.Configuration.ConfigRepository.BaseUrl}/iam/v3/oauth/jwks";
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, endpoint);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            System.Net.Http.HttpClient client;
            if (Sdk.Configuration.HttpClient is ReliableHttpClient)
                client = ReliableHttpClient.Http!;
            else
                client = DefaultHttpClient.Http;

            HttpResponseMessage resp = client.Send(req);
            if (resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var output = JsonSerializer.Deserialize<OauthcommonJWKSet>(resp.Content.ReadAsStream());
                if (output == null)
                    throw new Exception("Could not deserialize json");

                if (output.Keys == null)
                    throw new Exception("JWKS keys is null.");

                _Keys.Clear();
                foreach (var item in output.Keys)
                    _Keys.Add(item.Kid!, item);

                _Logger.LogInformation("JWKS fetched.");
            }
            else
            {
                StreamReader reader = new StreamReader(resp.Content.ReadAsStream());
                string errMsg = $"JWKS fetch error: {reader.ReadToEnd()}";

                _Logger.LogError(errMsg);
               throw new Exception(errMsg);
            }*/
        }

        protected OauthapiRevocationList? FetchRevocationList()
        {
            try
            {
                var revList = Sdk.Iam.OAuth20.GetRevocationListV3Op
                .SetPreferredSecurityMethod(Operation.SECURITY_BASIC)
                .Execute();
                if (revList == null)
                    throw new Exception("Could not get revocation list.");

                return revList;
            }
            catch (Exception x)
            {
                _Logger.LogError($"Failed to fetch revocation list. {x.Message}");
                return null;
            }            
        }

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
                .Build();

            _Keys = new Dictionary<string, OauthcommonJWKKey>();

            //First time fetch.
            FetchJWKS();
            RefreshRevocationList();
        }

        public JwtSecurityToken ValidateAccessToken(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(accessToken))
                throw new Exception("Invalid access token format.");

            lock (_RL_Lock)
            {
                if (_RL_TokenCache != null)
                {
                    if (_RL_TokenCache.MightContain(accessToken))
                        throw new Exception("Access token is revoked.");
                }
            }

            JwtSecurityToken rawJwt = tokenHandler.ReadJwtToken(accessToken);
            if (!rawJwt.Header.ContainsKey("kid"))
                throw new Exception("missing 'kid' value in jwt header.");
            string keyId = rawJwt.Header["kid"].ToString()!.ToLower();
            if (keyId == String.Empty)
                throw new Exception("empty 'kid' value in jwt header.");

            if (!_Keys.ContainsKey(keyId))
            {
                //Try to refresh the keys first.
                FetchJWKS();

                //Search again
                if (!_Keys.ContainsKey(keyId))
                    throw new Exception("No matching JWK set for this token");
            }

            OauthcommonJWKKey key = _Keys[keyId];

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(
              new RSAParameters()
              {
                  Modulus = (key.N != null ? FromBase64Url(key.N) : new byte[] { }),
                  Exponent = (key.E != null ? FromBase64Url(key.E) : new byte[] { }),
              });

            tokenHandler.ValidateToken(accessToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return rawJwt;
        }

        public void RefreshRevocationList()
        {
            var revList = FetchRevocationList();
            if (revList == null)
                return;

            if (revList.RevokedTokens == null)
                throw new Exception("No revoked token list");

            if (revList.RevokedUsers == null)
                throw new Exception("No revoked user list");

            lock (_RL_Lock)
            {
                _RL_TokenCache = new BloomFilter(revList.RevokedTokens);
                _RL_UserCache = revList.RevokedUsers;
            }
        }
    }
}