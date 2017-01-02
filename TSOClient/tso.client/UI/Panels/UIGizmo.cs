/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.Utils;
using System.IO;
using FSO.Client.UI.Screens;
using FSO.Common.Rendering.Framework.Model;
using FSO.Client.Network;
using FSO.Common.Utils;
using FSO.Common.DataService.Model;
using FSO.Client.Controllers;
using FSO.Common.DatabaseService.Model;
using FSO.Common;
using FSO.Common.Enum;

namespace FSO.Client.UI.Panels
{
    public class UIGizmoPropertyFilters : UIContainer
    {
        public UIImage Background;

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
        public UIListBoxTextStyle ListBoxColors { get; set; }

        private UIImage Background;
        private UIGizmoTab _Tab = UIGizmoTab.Property;

        private bool PendingSimSearch;
        private bool PendingLotSearch;

        private string SimQuery = "";
        private string LotQuery = "";

        private List<GizmoAvatarSearchResult> SimResults;
        private List<GizmoLotSearchResult> LotResults;

        public UIGizmoSearch(UIScript script, UIGizmo parent)
        {
            Background = script.Create<UIImage>("BackgroundImageSearch");
            this.Add(Background);

            script.LinkMembers(this, true);

            NarrowSearchButton.OnButtonClick += SendSearch;
            WideSearchUpButton.OnButtonClick += SendSearch;

            SearchSlider.AttachButtons(SearchScrollUpButton, SearchScrollDownButton, 1);
            SearchResult.AttachSlider(SearchSlider);
            SearchResult.OnDoubleClick += SearchResult_OnDoubleClick;
            SearchResult.Size = new Microsoft.Xna.Framework.Vector2(188, 108);
            SearchResult.Mask = true;

            ListBoxColors = script.Create<UIListBoxTextStyle>("ListBoxColors", SearchResult.FontStyle);
        }

        private void SearchResult_OnDoubleClick(UIElement button)
        {
            if (SearchResult.SelectedItem == null) { return; }
            var item = SearchResult.SelectedItem.Data as SearchResponseItem;
            if (item == null) { return; }

            switch (_Tab)
            {
                case UIGizmoTab.People:
                    FindController<CoreGameScreenController>().ShowPersonPage(item.EntityId);
                    break;
                case UIGizmoTab.Property:
                    FindController<CoreGameScreenController>().ShowLotPage(item.EntityId);
                    break;
            }
        }

        public UIGizmoTab Tab
        {
            set {
                if(_Tab == UIGizmoTab.People) { 
                    SimQuery = SearchText.CurrentText;
                }else{
                    LotQuery = SearchText.CurrentText;
                }
                _Tab = value;
                UpdateUI();

                if(_Tab == UIGizmoTab.People){
                    SearchText.CurrentText = SimQuery;
                }else{
                    SearchText.CurrentText = LotQuery;
                }
            }
        }

        private void SendSearch(UIElement button)
        {
            var exact = button == NarrowSearchButton;
            var type = _Tab == UIGizmoTab.Property ? SearchType.LOTS : SearchType.SIMS;

            if(type == SearchType.SIMS){
                PendingSimSearch = true;
            }else{
                PendingLotSearch = true;
            }

            UpdateUI();
            ((GizmoSearchController)Controller).Search(SearchText.CurrentText, type, exact);
        }

        private void UpdateUI()
        {
            SearchResult.Items.Clear();

            var rank = 1;

            if (_Tab == UIGizmoTab.People){
                NarrowSearchButton.Disabled = WideSearchUpButton.Disabled = PendingSimSearch;

                if (SimResults != null)
                {
                    SearchResult.Items.AddRange(SimResults.Select(x =>
                    {
                        return new UIListBoxItem(x.Result, new object[] { (rank++).ToString(), x.Result.Name }) {
                            CustomStyle = ListBoxColors,
                            UseDisabledStyleByDefault = new ValuePointer(x, "IsOffline")
                        };
                    }));
                }
            }else{
                NarrowSearchButton.Disabled = WideSearchUpButton.Disabled = PendingLotSearch;

                if(LotResults != null)
                {
                    SearchResult.Items.AddRange(LotResults.Select(x =>
                    {
                        return new UIListBoxItem(x.Result, new object[] { (rank++).ToString(), x.Result.Name })
                        {
                            CustomStyle = ListBoxColors,
                            UseDisabledStyleByDefault = new ValuePointer(x, "IsOffline")
                        };
                    }));
                }
            }

            NoSearchResultsText.Visible = SearchResult.Items.Count == 0;
            SearchResult.Items = SearchResult.Items;
        }

        public void SetResults(List<GizmoAvatarSearchResult> results){
            PendingSimSearch = false;
            SimResults = results;
            UpdateUI();
        }

        public void SetResults(List<GizmoLotSearchResult> results)
        {
            PendingLotSearch = false;
            LotResults = results;
            UpdateUI();
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

        private UIListBoxTextStyle ListBoxColors;
        private int UpdateCooldown;

        public UIImage Background; //public so we can disable visibility when not selected... workaround to stop background mouse blocking still happening when panel is hidden
        private UISlider HiddenSubSlider;

        private UIGizmoTab _Tab;

        public UIGizmoTop100(UIScript script, UIGizmo parent)
        {

            Background = script.Create<UIImage>("BackgroundImageTop100Lists");
            this.Add(Background);
            
            script.LinkMembers(this, true);

            HiddenSubSlider = new UISlider();
            Top100SubList.Columns[0].TextureBounds = new Microsoft.Xna.Framework.Vector2(17, 17);
            Top100SubList.Columns[0].TextureSelectedFrame = 1;
            Top100SubList.Columns[0].TextureHoverFrame = 2;
            Top100SubList.OnChange += Top100SubList_OnChange;
            Top100SubList.AttachSlider(HiddenSubSlider);
            HiddenSubSlider.AttachButtons(Top100SubListScrollUpButton, Top100SubListScrollDownButton, 1);

            Top100Slider.AttachButtons(Top100ListScrollUpButton, Top100ListScrollDownButton, 1);
            Top100ResultList.AttachSlider(Top100Slider);
            Top100ResultList.Mask = true;
            Top100ResultList.SetSize(150, 138);
            Top100ResultList.OnDoubleClick += Top100ResultList_OnDoubleClick;

            //populateWithXMLHouses();

            Top100ResultList.OnDoubleClick += Top100ItemSelect;
            UpdateCooldown = 100;

            ListBoxColors = script.Create<UIListBoxTextStyle>("ListBoxColors", Top100ResultList.FontStyle);
        }

        public UIGizmoTab Tab
        {
            set
            {
                _Tab = value;
                FindController<GizmoTop100Controller>().SetTab(_Tab);
            }
        }

        private void Top100ResultList_OnDoubleClick(UIElement button)
        {
            if (Top100ResultList.SelectedItem == null) { return; }

            var selected = Top100ResultList.SelectedItem.Data as Top100ListItem;
            if (selected != null)
            {
                if(selected.Top100Entry.TargetId != null)
                {
                    if(selected.Lot != null)
                    {
                        FindController<CoreGameScreenController>().ShowLotPage(selected.Lot.Id);
                    }else if(selected.Avatar != null)
                    {
                        FindController<CoreGameScreenController>().ShowPersonPage(selected.Avatar.Avatar_Id);
                    }
                }
            }
        }

        private void Top100SubList_OnChange(UIElement element)
        {
            if (Top100SubList.SelectedItem == null) { return; }

            var selected = Top100SubList.SelectedItem.Data as Top100CategoryListItem;
            if(selected != null)
            {
                var controller = FindController<GizmoTop100Controller>();
                if (controller != null){
                    controller.SetCategory(selected.Category);
                }
            }
        }

        public void DisplayCategories(List<Top100CategoryListItem> results)
        {
            Top100SubList.Items = results.Select(x =>
            {
                return new UIListBoxItem(x, x.Icon)
                {
                };
            }).ToList();
        }

        public void SelectCategory(Top100Category category)
        {
            var item = Top100SubList.Items.FirstOrDefault(x => ((Top100CategoryListItem)x.Data).Category == category);
            if(Top100SubList.SelectedItem != item){
                Top100SubList.SelectedItem = item;
            }
        }

        public void DisplayResults(List<Top100ListItem> results)
        {
            Top100ResultList.Items = results.Select(x =>
            {
                return new UIListBoxItem(x, x.Top100Entry.Rank.ToString(), new ValuePointer(x, "TargetName")) {
                    Disabled = x.Top100Entry.TargetId == null,
                    UseDisabledStyleByDefault = new ValuePointer(x, "TargetIsOffline"),
                    CustomStyle = ListBoxColors
                };
            }).ToList();
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (UpdateCooldown-- < 0)
            {
                //populateWithXMLHouses();
                UpdateCooldown = 100;
            }
        }

        public void populateWithXMLHouses()
        {
            var xmlHouses = new List<UIXMLLotEntry>();

            string[] paths = Directory.GetFiles(Path.Combine(FSOEnvironment.ContentDir,"Blueprints/"), "*.xml", SearchOption.AllDirectories);
            for (int i = 0; i < paths.Length; i++)
            {
                string entry = paths[i];
                string filename = Path.GetFileName(entry);
                xmlHouses.Add(new UIXMLLotEntry { Filename = filename, Path = entry });
            }

            paths = Directory.GetFiles(Path.Combine(GlobalSettings.Default.StartupPath, @"housedata/"), "*_00.xml", SearchOption.AllDirectories);
            for (int i=0; i<paths.Length; i++)
            {
                string entry = paths[i];
                string filename = Path.GetFileName(entry);
                xmlHouses.Add(new UIXMLLotEntry { Filename = filename, Path = entry });
            }

            Top100ResultList.Items = xmlHouses.Select(x => new UIListBoxItem(x, x.Filename)).ToList();
        }

        private void Top100ItemSelect(UIElement button)
        {
            //((CoreGameScreen)(Parent.Parent)).InitTestLot(((UIXMLLotEntry)Top100ResultList.SelectedItem.Data).Path, true);
        }
    }

    public struct UIXMLLotEntry
    {
        public string Filename;
        public string Path;
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
        
        public UIImage PeopleTabBackground { get; set; }
        public UIImage HousesTabBackground { get; set; }

        public UIImage PeopleTab { get; set; }
        public UIImage HousesTab { get; set; }

        public UIButton PeopleTabButton { get; set; }
        public UIButton HousesTabButton { get; set; }


        public UIGizmoPropertyFilters FiltersProperty;
        public UIGizmoSearch Search;
        public UIGizmoTop100 Top100;

        private UIGizmoPIP PIP;

        public Binding<Avatar> CurrentAvatar { get; internal set; }

        public UIGizmo()
        {
            var ui = this.RenderScript("gizmo.uis");

            AddAt(0, (PeopleTab = ui.Create<UIImage>("PeopleTab")));
            AddAt(0, (HousesTab = ui.Create<UIImage>("HousesTab")));
            
            AddAt(0, (PeopleTabBackground = ui.Create<UIImage>("PeopleTabBackground")));
            AddAt(0, (HousesTabBackground = ui.Create<UIImage>("HousesTabBackground")));

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
            Search.BindController<GizmoSearchController>();
            Search.Visible = false;
            this.Add(Search);

            Top100 = new UIGizmoTop100(ui, this);
            Top100.BindController<GizmoTop100Controller>();
            Top100.Visible = false;
            Top100.Background.Visible = false;
            this.Add(Top100);

            ExpandButton.OnButtonClick += new ButtonClickDelegate(ExpandButton_OnButtonClick);
            ContractButton.OnButtonClick += new ButtonClickDelegate(ContractButton_OnButtonClick);

            PeopleTabButton.OnButtonClick += new ButtonClickDelegate(PeopleTabButton_OnButtonClick);
            HousesTabButton.OnButtonClick += new ButtonClickDelegate(HousesTabButton_OnButtonClick);

            FiltersButton.OnButtonClick += new ButtonClickDelegate(FiltersButton_OnButtonClick);
            SearchButton.OnButtonClick += new ButtonClickDelegate(SearchButton_OnButtonClick);
            Top100ListsButton.OnButtonClick += new ButtonClickDelegate(Top100ListsButton_OnButtonClick);

            PIP = ui.Create<UIGizmoPIP>("PipSetup");
            PIP.Initialize();
            Add(PIP);

            CurrentAvatar = new Binding<Avatar>()
                .WithBinding(PIP, "SimBox.Avatar.BodyOutfitId", "Avatar_Appearance.AvatarAppearance_BodyOutfitID")
                .WithBinding(PIP, "SimBox.Avatar.HeadOutfitId", "Avatar_Appearance.AvatarAppearance_HeadOutfitID")
                .WithBinding(PIP, "SimBox.Avatar.Appearance", "Avatar_Appearance.AvatarAppearance_SkinTone", (x) => (Vitaboy.AppearanceType)((byte)x));

            Tab = UIGizmoTab.Property;
            View = UIGizmoView.Filters;
            SetOpen(true);
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
            Tab = UIGizmoTab.Property;
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
        private UIGizmoTab _Tab;
        private UIGizmoTab Tab
        {
            get { return _Tab; }
            set
            {
                _Tab = value;
                Search.Tab = value;
                Top100.Tab = value;
            }
        }

        private void SetOpen(bool open)
        {
            if (m_Open != open)
            {
                if (open) Position = new Microsoft.Xna.Framework.Vector2(Position.X, Position.Y - 6);
                else Position = new Microsoft.Xna.Framework.Vector2(Position.X, Position.Y + 6);
            }
            m_Open = open;
            Redraw();
        }

        private void Redraw()
        {
            var isOpen = m_Open;
            var isClosed = !m_Open;

            if (isOpen)
            {
                PIP.Position = new Microsoft.Xna.Framework.Vector2(6, 30);
            }
            else
            {
                PIP.Position = new Microsoft.Xna.Framework.Vector2(6, 24);
            }

            PeopleTab.Visible = isOpen && Tab == UIGizmoTab.People;
            HousesTab.Visible = isOpen && Tab == UIGizmoTab.Property;
            PeopleTabButton.Selected = Tab == UIGizmoTab.People;
            HousesTabButton.Selected = Tab == UIGizmoTab.Property;

            PeopleTabBackground.Visible = isOpen;
            HousesTabBackground.Visible = isOpen;

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
            Top100.Background.Visible = false;
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
                        Top100.Background.Visible = true;
                        break;
                }
            }
        }
    }
}
