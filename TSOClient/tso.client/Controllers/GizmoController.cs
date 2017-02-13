using FSO.Client.UI.Panels;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Enum;
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

        public void RequestFilter(LotCategory cat)
        {
            if (Gizmo.CurrentAvatar != null && Gizmo.CurrentAvatar.Value != null)
            {
                Gizmo.CurrentAvatar.Value.Avatar_Top100ListFilter.Top100ListFilter_Top100ListID = (uint)cat;
                DataService.Sync(Gizmo.CurrentAvatar.Value, new string[] { "Avatar_Top100ListFilter.Top100ListFilter_Top100ListID" });
            }
        }

        public void ClearFilter()
        {
            Gizmo.FilterList = System.Collections.Immutable.ImmutableList<uint>.Empty;
        }
    }
}
