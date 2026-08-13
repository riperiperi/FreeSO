using System;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace FSO_BrowserClient.Pages
{
    public partial class Index
    {
        [Inject] NavigationManager Navigation { get; set; }

        Microsoft.Xna.Framework.Game _game;

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
                // ?lot=1 — isometric diamond placeholder (default lot view).
                // ?lot=real or ?lot=real=1 — attempt real FSO.LotView (ExternalWorld + terrain).
                var lotParam = QueryValue(uri, "lot");
                var forceLot = lotParam == "1" || IsRealLotParam(lotParam);
                var forceRealLot = IsRealLotParam(lotParam);
                // ?effect=1 — probe stock FreeSO MGFX 11 under sample-content (expected fail; S3).
                // KNIF Content.Load of wwwroot/Content/Effects/colorpoly2D runs unconditionally.
                var probeXnb = QueryValue(uri, "effect") == "1";

                // ?house=<name> — fetch wwwroot/houses/<name>.xml and load its floors/walls
                // into the real lot (implies ?lot=real).
                var houseParam = QueryValue(uri, "house");
                string houseUrl = null;
                if (houseParam != null)
                {
                    houseUrl = new Uri(new Uri(Navigation.BaseUri), $"houses/{houseParam}.xml").AbsoluteUri;
                    forceLot = true;
                    forceRealLot = true;
                }

                // ?furnish=png (default until the FX rebuild lands) — billboard layer.
                // ?furnish=real — real DGRP sprites via RealFurnitureLayer; blocked on
                // the KNIF 2DWorldBatch rebuild (see SESSION-LANES).
                var furnishReal = QueryValue(uri, "furnish") == "real";
                // ?objtech=drawZSprite|drawSimple|… — A/B the object pass technique (debug).
                var objtech = QueryValue(uri, "objtech");
                if (objtech != null && Enum.TryParse<FSO.LotView.Effects.WorldBatchTechniques>(objtech, out var tech))
                    FSO.LotView.WorldEntities.ObjectTechniqueOverride = tech;
                var spriteTest = QueryValue(uri, "spritetest");
                FSO_BrowserClientGame.SpriteTest = spriteTest == "1" || spriteTest == "2";
                FSO_BrowserClientGame.SpriteTestBasic = spriteTest == "2";
                FSO_BrowserClientGame.DepthOutProbe = QueryValue(uri, "depthout") == "1";
                FSO_BrowserClientGame.RedrawProbe = QueryValue(uri, "redraw") == "1";
                FSO_BrowserClientGame.FixedFx = QueryValue(uri, "fx") == "fixed";
                var v2diag = QueryValue(uri, "v2diag");
                RealFurnitureLayer.V2Diag = v2diag == "1";
                RealFurnitureLayer.V2AllMagenta = v2diag == "2";
                // ?zoom=near|medium|far and ?rot=0..3 pin the camera for probes.
                var zoomParam = QueryValue(uri, "zoom");
                var rotParam = QueryValue(uri, "rot");
                var rot = int.TryParse(rotParam, out var r) ? r : -1;

                // ?vm=1 — join the shared lockstep VM through the gateway /sandbox
                // route: content bundle → MEMFS → SERVER Content.Init → local
                // SimAntics VM in lockstep with LotHostLite. ?name= labels the avatar.
                var vmMode = QueryValue(uri, "vm") == "1";
                var vmName = QueryValue(uri, "name");

                _game = new FSO_BrowserClientGame(contentBase, gateway, autoJoin, forceLot, probeXnb, forceRealLot, houseUrl,
                    furnishReal, zoomParam, rot, vmMode, vmName, Navigation.BaseUri);
                _game.Run();
            }

            _game.Tick();
        }

        static bool IsRealLotParam(string lotParam)
        {
            if (string.IsNullOrEmpty(lotParam)) return false;
            // Accept ?lot=real and ?lot=real=1 (value "real=1" after single '=' split).
            return lotParam.Equals("real", StringComparison.OrdinalIgnoreCase)
                || lotParam.StartsWith("real=", StringComparison.OrdinalIgnoreCase);
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
