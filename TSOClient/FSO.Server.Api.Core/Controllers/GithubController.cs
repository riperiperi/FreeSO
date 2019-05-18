using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octokit;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FSO.Server.Api.Core.Controllers
{
    [ApiController]
    public class GithubController : ControllerBase
    {
        readonly GitHubClient client =
            new GitHubClient(new ProductHeaderValue("FreeSO"), new Uri("https://github.com/"));

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
