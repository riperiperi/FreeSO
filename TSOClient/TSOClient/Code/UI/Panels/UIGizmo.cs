/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Controls;
using TSOClient.LUI;
using TSOClient.Code.UI.Framework.Parser;
using TSOClient.Code.Utils;
using TSOClient.VM;
using TSOClient.Code.Data;
using TSOClient.Code.Data.Model;
using SimsLib.ThreeD;

namespace TSOClient.Code.UI.Panels
{
    public class UIGizmoPropertyFilters : UIContainer
    {
        private UIImage Background;

        public UIGizmoPropertyFilters(UIScript script, UIGizmo parent)
        {
            Background = script.Create<UIImage>("BackgroundImageFilters");
            this.Add(Background);

            var filterChildren = parent.GetChildren().Where(x => x.ID != null && x.ID.StartsWith("PropertyFilterButton_")).ToList();
            foreach (var child in filterChildren)
            {
                child.Parent.Remove(child);
                this.Add(child);
            }
        }
    }

    public class UIGizmoSearch : UIContainer
    {
        public UISlider SearchSlider { get; set; }
        public UIButton WideSearchUpButton { get; set; }
        public UIButton NarrowSearchButton { get; set; }
        public UIButton SearchScrollUpButton { get; set; }
        public UIButton SearchScrollDownButton { get; set; }
        public UIListBox SearchResult { get; set; }
        public UITextEdit SearchText { get; set; }
        public UILabel NoSearchResultsText { get; set; }


        private UIImage Background;

        public UIGizmoSearch(UIScript script, UIGizmo parent)
        {
            Background = script.Create<UIImage>("BackgroundImageSearch");
            this.Add(Background);
            

            script.LinkMembers(this, true);
        }
    }

    public class UIGizmoTop100 : UIContainer
    {
        public UISlider Top100Slider { get; set; }
        public UIButton Top100ListScrollUpButton { get; set; }
        public UIButton Top100ListScrollDownButton { get; set; }
        public UIButton Top100SubListScrollUpButton { get; set; }
        public UIButton Top100SubListScrollDownButton { get; set; }
        public UIListBox Top100SubList { get; set; }
        public UIListBox Top100ResultList { get; set; }


        private UIImage Background;

        public UIGizmoTop100(UIScript script, UIGizmo parent)
        {

            Background = script.Create<UIImage>("BackgroundImageTop100Lists");
            this.Add(Background);
            
            script.LinkMembers(this, true);
        }
    }

    public enum UIGizmoTab
    {
        People,
        Property
    }

    public enum UIGizmoView
    {
        Filters,
        Search,
        Top100
    }

    public class UIGizmo : UIContainer
    {
        private UIImage BackgroundImageGizmo;
        private UIImage BackgroundImageGizmoPanel;
        private UIImage BackgroundImagePanel;

        private UIContainer ButtonContainer;

        public UIButton ExpandButton { get; set; }
        public UIButton ContractButton { get; set; }

        public UIButton FiltersButton { get; set; }
        public UIButton SearchButton { get; set; }
        public UIButton Top100ListsButton { get; set; }

        public UIButton PeopleTabButton { get; set; }
        public UIButton HousesTabButton { get; set; }

        public UIGizmoPropertyFilters FiltersProperty;
        public UIGizmoSearch Search;
        public UIGizmoTop100 Top100;

        public UISim SimBox;

        public UIGizmo()
        {
            var ui = this.RenderScript("gizmo.uis");

            BackgroundImageGizmo = ui.Create<UIImage>("BackgroundImageGizmo");
            this.AddAt(0, BackgroundImageGizmo);

            BackgroundImageGizmoPanel = ui.Create<UIImage>("BackgroundImageGizmoPanel");
            this.AddAt(0, BackgroundImageGizmoPanel);

            BackgroundImagePanel = ui.Create<UIImage>("BackgroundImagePanel");
            this.AddAt(0, BackgroundImagePanel);

            UIUtils.MakeDraggable(BackgroundImageGizmo, this);
            UIUtils.MakeDraggable(BackgroundImageGizmoPanel, this);
            UIUtils.MakeDraggable(BackgroundImagePanel, this);

            ButtonContainer = new UIContainer();
            this.Remove(ExpandButton);
            ButtonContainer.Add(ExpandButton);
            this.Remove(ContractButton);
            ButtonContainer.Add(ContractButton);
            this.Remove(FiltersButton);
            ButtonContainer.Add(FiltersButton);
            this.Remove(SearchButton);
            ButtonContainer.Add(SearchButton);
            this.Remove(Top100ListsButton);
            ButtonContainer.Add(Top100ListsButton);
            this.Add(ButtonContainer);

            FiltersProperty = new UIGizmoPropertyFilters(ui, this);
            FiltersProperty.Visible = false;
            this.Add(FiltersProperty);

            Search = new UIGizmoSearch(ui, this);
            Search.Visible = false;
            this.Add(Search);

            Top100 = new UIGizmoTop100(ui, this);
            Top100.Visible = false;
            this.Add(Top100);

            ExpandButton.OnButtonClick += new ButtonClickDelegate(ExpandButton_OnButtonClick);
            ContractButton.OnButtonClick += new ButtonClickDelegate(ContractButton_OnButtonClick);

            PeopleTabButton.OnButtonClick += new ButtonClickDelegate(PeopleTabButton_OnButtonClick);
            HousesTabButton.OnButtonClick += new ButtonClickDelegate(HousesTabButton_OnButtonClick);

            FiltersButton.OnButtonClick += new ButtonClickDelegate(FiltersButton_OnButtonClick);
            SearchButton.OnButtonClick += new ButtonClickDelegate(SearchButton_OnButtonClick);
            Top100ListsButton.OnButtonClick += new ButtonClickDelegate(Top100ListsButton_OnButtonClick);

            SimBox = new UISim();
            var sim = new Sim(Guid.NewGuid().ToString());
            var maleHeads = new Collection(ContentManager.GetResourceFromLongID((ulong)FileIDs.CollectionsFileIDs.ea_male_heads));
            SimCatalog.LoadSim3D(sim, SimCatalog.GetOutfit(4462471020557), AppearanceType.Light);

            SimBox.Sim = sim;
            SimBox.SimScale = 0.4f;
            SimBox.Position = new Microsoft.Xna.Framework.Vector2(60, 60);

            this.Add(SimBox);

            SetOpen(false);
        }

        void Top100ListsButton_OnButtonClick(UIElement button)
        {
            View = UIGizmoView.Top100;
            SetOpen(true);
        }

        void SearchButton_OnButtonClick(UIElement button)
        {
            View = UIGizmoView.Search;
            SetOpen(true);
        }

        void FiltersButton_OnButtonClick(UIElement button)
        {
            View = UIGizmoView.Filters;
            SetOpen(true);
        }

        void HousesTabButton_OnButtonClick(UIElement button)
        {
            Tab = UIGizmoTab.Property;
            Redraw();
        }

        void PeopleTabButton_OnButtonClick(UIElement button)
        {
            Tab = UIGizmoTab.People;
            Redraw();
        }

        void ContractButton_OnButtonClick(UIElement button)
        {
            SetOpen(false);
        }

        void ExpandButton_OnButtonClick(UIElement button)
        {
            SetOpen(true);
        }

        private bool m_Open = false;
        private UIGizmoView View = UIGizmoView.Filters;
        private UIGizmoTab Tab = UIGizmoTab.Property;

        private void SetOpen(bool open)
        {
            m_Open = open;
            Redraw();
        }

        private void Redraw()
        {
            var isOpen = m_Open;
            var isClosed = !m_Open;

            if (isOpen)
            {
                SimBox.Position = new Microsoft.Xna.Framework.Vector2(60, 66);
            }
            else
            {
                SimBox.Position = new Microsoft.Xna.Framework.Vector2(60, 60);
            }

            PeopleTabButton.Disabled = View == UIGizmoView.Filters;
            FiltersButton.Selected = isOpen && View == UIGizmoView.Filters;
            SearchButton.Selected = isOpen && View == UIGizmoView.Search;
            Top100ListsButton.Selected = isOpen && View == UIGizmoView.Top100;

            ButtonContainer.Y = isOpen ? 6 : 0;

            BackgroundImageGizmo.Visible = isClosed;
            BackgroundImageGizmoPanel.Visible = isOpen;
            BackgroundImagePanel.Visible = isOpen;
            ExpandButton.Visible = isClosed;
            ContractButton.Visible = isOpen;

            FiltersProperty.Visible = false;
            Top100.Visible = false;
            Search.Visible = false;

            PeopleTabButton.Visible = isOpen;
            HousesTabButton.Visible = isOpen;

            if (Tab == UIGizmoTab.People && View == UIGizmoView.Filters)
            {
                View = UIGizmoView.Search;
            }

            if (isOpen)
            {
                switch (View)
                {
                    case UIGizmoView.Filters:
                        FiltersProperty.Visible = true;
                        break;

                    case UIGizmoView.Search:
                        Search.Visible = true;
                        break;

                    case UIGizmoView.Top100:
                        Top100.Visible = true;
                        break;
                }
            }
        }
    }
}
