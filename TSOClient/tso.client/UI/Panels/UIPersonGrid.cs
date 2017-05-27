using FSO.Client.UI.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.SimAntics;
using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels
{
    public class UIPersonGrid : UIContainer
    {
        private VM vm;
        private HashSet<VMAvatar> Display;
        private int Page;
        private List<UIVMPersonButton> CurrentIcons;

        public int Columns = 9;
        public int Rows = 2;

        public UIPersonGrid(VM vm)
        {
            this.vm = vm;
            Page = 0;
            Display = new HashSet<VMAvatar>();
            CurrentIcons = new List<UIVMPersonButton>();
            UpdatePeople();
        }

        public void UpdatePeople()
        {
            bool change = false;

            var lastDisp = new List<VMAvatar>(Display);
            foreach (var sim in lastDisp)
            {
                if (sim.Dead || sim.PersistID == vm.MyUID)
                {
                    Display.Remove(sim);
                    change = true;
                }  
            }

            foreach (var sim in vm.Context.ObjectQueries.Avatars)
            {
                if (!Display.Contains(sim) && sim.PersistID != vm.MyUID)
                {
                    Display.Add((VMAvatar)sim);
                    change = true;
                }
            }

            if (change)
            {
                Page = Math.Min(Page, (Display.Count / (Columns * Rows)));
                DrawPage();
                Invalidate();
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            UpdatePeople();
        }

        public void DrawPage()
        {
            Page = Math.Min(Page, (Display.Count / (Columns * Rows)));
            if (Page < 0) Page = 0;
            foreach (var icon in CurrentIcons) Remove(icon);
            CurrentIcons.Clear();
            var startInd = Page * (Columns * Rows);

            for (int x=0; x<Columns; x++)
            {
                for (int y=0; y<Rows; y++)
                {
                    var ind = startInd + x + Columns * y;
                    if (ind < Display.Count) {
                        var icon = new UIVMPersonButton(Display.ElementAt(ind), vm, false);
                        CurrentIcons.Add(icon);
                        Add(icon);
                        icon.Position = new Vector2(x*(34+12), y*(34+11));
                    }
                }
            }
        }

        public void NextPage()
        {
            Page++;
            DrawPage();
        }

        public void PreviousPage()
        {
            Page--;
            DrawPage();
        }
    }
}
