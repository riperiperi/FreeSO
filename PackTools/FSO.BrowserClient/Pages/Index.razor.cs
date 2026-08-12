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
            // init game
            if (_game == null)
            {
                // Hosted under wwwroot/sample-content/ — same origin as the Blazor app.
                var contentBase = new Uri(new Uri(Navigation.BaseUri), "sample-content/").AbsoluteUri;
                _game = new FSO_BrowserClientGame(contentBase);
                _game.Run();
            }

            // run gameloop
            _game.Tick();
        }
    }
}
