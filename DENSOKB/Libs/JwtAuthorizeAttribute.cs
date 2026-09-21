using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using System;
using System.Collections.Generic;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace DENSOKB.Libs
{
    public class JwtAuthorizeAttribute : AuthorizationFilterAttribute
    {
        private static readonly string Secret = "your-secret-key";
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            // 1. 检查 Authorization 头
            if (!actionContext.Request.Headers.Contains("Authorization"))
            {
                actionContext.Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
                return;
            }

            var token = actionContext.Request.Headers.Authorization.Parameter;

            try
            {
                // 2. 解码并验证 Token
                IJsonSerializer serializer = new JsonNetSerializer();
                IDateTimeProvider provider = new UtcDateTimeProvider();
                IJwtValidator validator = new JwtValidator(serializer, provider);
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
                IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder,algorithm);

                var payload = decoder.DecodeToObject<Dictionary<string, object>>(token, Secret, verify: true);

                // 3. 检查 Token 是否过期
                var exp = long.Parse(payload["exp"].ToString());
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (now > exp)
                {
                    actionContext.Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
                }
            }
            catch (Exception)
            {
                actionContext.Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }
        }

    }
}