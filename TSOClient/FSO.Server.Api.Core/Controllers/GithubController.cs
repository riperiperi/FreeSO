using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Octokit;

namespace FSO.Server.Api.Core.Controllers
{
    [EnableCors]
    [ApiController]
    public class GithubController : ControllerBase
    {
        readonly GitHubClient client =
            new GitHubClient(new ProductHeaderValue(Api.INSTANCE.Github.AppName), new Uri("https://github.com/"));

        private string StoredToken;
        private static string CSRF;

        // GET: /<controller>/
        [HttpGet]
        [Route("github/")]
        public IActionResult Index()
        {
            if (Api.INSTANCE.Github == null) return NotFound();
            if (Api.INSTANCE.Github.AccessToken != null) return NotFound();

            return Redirect(GetOauthLoginUrl());
        }

        [HttpGet]
        [Route("github/callback")]
        public async Task<IActionResult> Callback(string code, string state)
        {
            if (Api.INSTANCE.Github == null) return NotFound();
            if (Api.INSTANCE.Github.AccessToken != null) return NotFound();

            if (!String.IsNullOrEmpty(code))
            {
                var expectedState = CSRF;
                if (state != expectedState) throw new InvalidOperationException("SECURITY FAIL!");
                //CSRF = null;

                var token = await client.Oauth.CreateAccessToken(
                    new OauthTokenRequest(Api.INSTANCE.Github.ClientID, Api.INSTANCE.Github.ClientSecret, code)
                    {
                        RedirectUri = new Uri("http://localhost:80/github/callback")
                    });
                StoredToken = token.AccessToken;
            }

            return Ok(StoredToken);
        }

        private string GetOauthLoginUrl()
        {
            var rngCsp = new RNGCryptoServiceProvider();
            string csrf = "";
            var random = new byte[24];
            rngCsp.GetBytes(random);
            for (int i=0; i<24; i++)
            {
                csrf += (char)('?' + random[i]/4);
            }
            CSRF = csrf;

            // 1. Redirect users to request GitHub access
            var request = new OauthLoginRequest(Api.INSTANCE.Github.ClientID)
            {
                Scopes = { "admin:org", "repo" },
                State = csrf
            };
            var oauthLoginUrl = client.Oauth.GetGitHubLoginUrl(request);
            return oauthLoginUrl.ToString();
        }
    }
}
