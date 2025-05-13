using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WEB_BMS.Controllers
{
    public static class PaypalConfiguration
    {
        public static APIContext GetAPIContext()
        {
            var config = new Dictionary<string, string>
            {
                { "mode", "sandbox" }, // sandbox hoặc live
                { "clientId", "ASGiOnm68yx36vu6w0TnTGZKCAMflwIT56pMQAHfZdB9e8K94eqe9TGhrLUcnwrA_2bBEmkUzHzCYCWV" },
                { "clientSecret", "EFyfLTYr34J8j3GQXKL1xXhr-HZ7Wi4RcS6aPeaQU2t-T4fNIQndHNg7QUP_DHff2Qb9c8GDmzHtuCQ_" }
            };

            var accessToken = new OAuthTokenCredential(
                config["clientId"],
                config["clientSecret"],
                config).GetAccessToken();

            var apiContext = new APIContext(accessToken)
            {
                Config = config
            };

            return apiContext;
        }
    }

}