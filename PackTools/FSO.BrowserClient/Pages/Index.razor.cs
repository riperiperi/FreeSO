using System;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace FSO_BrowserClient.Pages
{
    public partial class Index
    {
        [Inject] NavigationManager Navigation { get; set; }

        Game _game;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                JsRuntime.InvokeAsync<object>("initRenderJS", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public void TickDotNet()
        {
            if (_game == null)
            {
                var contentBase = new Uri(new Uri(Navigation.BaseUri), "sample-content/").AbsoluteUri;
                // Default gateway; override with ?gateway=ws://host:8087
                var gateway = QueryValue(Navigation.ToAbsoluteUri(Navigation.Uri), "gateway")
                    ?? "http://127.0.0.1:8087";

                _game = new FSO_BrowserClientGame(contentBase, gateway);
                _game.Run();
            }

            _game.Tick();
        }

        static string QueryValue(Uri uri, string key)
        {
            var q = uri.Query;
            if (string.IsNullOrEmpty(q) || q.Length < 2) return null;
            foreach (var part in q.TrimStart('?').Split('&'))
            {
                var kv = part.Split(new[] { '=' }, 2);
                if (kv.Length == 2 && string.Equals(Uri.UnescapeDataString(kv[0]), key, StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(kv[1]);
            }
            return null;
        }
    }
}
