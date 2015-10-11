using FSO.Client.UI.Panels;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class GizmoController : IDisposable
    {
        private UIGizmo Gizmo;
        private Network.Network Network;
        private IClientDataService DataService;

        public GizmoController(UIGizmo view, Network.Network network, IClientDataService dataService)
        {
            this.Gizmo = view;
            this.Network = network;
            this.DataService = dataService;

            Initialize();
        }

        private void Initialize()
        {
            DataService.Get<Avatar>(Network.MyCharacter).ContinueWith(x =>
            {
                if (!x.IsFaulted){
                    Gizmo.CurrentAvatar.Value = x.Result;
                }
            });
        }

        public void Dispose()
        {
            try {
                Gizmo.CurrentAvatar.Value = null;
            }catch(Exception ex){
            }
        }
    }
}
