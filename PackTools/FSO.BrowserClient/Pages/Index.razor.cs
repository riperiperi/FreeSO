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
                var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
                // Default gateway; override with ?gateway=ws://host:8087
                var gatewayExplicit = QueryValue(uri, "gateway");
                var gateway = gatewayExplicit ?? "http://127.0.0.1:8087";
                // Auto-join when gateway is in the URL (smoke/demo), or ?join=1.
                // ?join=0 disables auto-join (Space still works). Bare / stays texture-only.
                var joinParam = QueryValue(uri, "join");
                var autoJoin = joinParam != "0"
                    && (gatewayExplicit != null || joinParam == "1");
                // ?lot=1 — isometric placeholder without gateway (S5 visual stand-in).
                var forceLot = QueryValue(uri, "lot") == "1";
                // ?effect=1 — probe Content.Load of stock FreeSO colorpoly2D.xnb (expected fail; S3).
                var probeXnb = QueryValue(uri, "effect") == "1";

                _game = new FSO_BrowserClientGame(contentBase, gateway, autoJoin, forceLot, probeXnb);
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
