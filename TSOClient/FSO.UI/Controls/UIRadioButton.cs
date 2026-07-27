using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace FSO.Client.UI.Controls
{
    public class UIRadioButton : UIButton
    {
        private string _RadioGroup;
        public object RadioData { get; set; }

        /// <summary>
        /// Only the selected radio in a group is tab-stoppable.
        /// Arrow keys handle cycling within the group.
        /// </summary>
        public override int TabIndex
        {
            get => (_RadioGroup != null && !Selected) ? -1 : base.TabIndex;
            set => base.TabIndex = value;
        }

        public UIRadioButton() : base(GetTexture(0x0000049C00000001)) {
        }

        public UIRadioButton(Texture2D texture) : base(texture){
        }

        public string RadioGroup
        {
            get { return _RadioGroup; }
            set
            {
                if (_RadioGroup != null && value == null){
                    OnButtonClick -= HandleRadioClick;
                }
                _RadioGroup = value;
                if(_RadioGroup != null){
                    OnButtonClick += HandleRadioClick;
                }
            }
        }

        private void HandleRadioClick(UIElement btn){
            var parent = this.Parent;
            if (parent == null) { return; }

            this.Selected = true;

            var group = GetRadioGroup(this.RadioGroup);
            
            foreach (var child in group){
                if (child != this){
                    child.Selected = false;
                }
            }
        }

        public List<UIRadioButton> GetRadioGroup(string group)
        {
            var result = new List<UIRadioButton>();
            _FindRadioGroup(UIScreen.Current, group, result);
            return result;
        }

        private void _FindRadioGroup(UIContainer container, string group, List<UIRadioButton> targetList)
        {
            foreach(var child in container.GetChildren())
            {
                if(child is UIRadioButton && ((UIRadioButton)child).RadioGroup == group)
                {
                    targetList.Add((UIRadioButton)child);
                }else if(child is UIContainer)
                {
                    _FindRadioGroup((UIContainer)child, group, targetList);
                }
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (IsFocused && Selected && _RadioGroup != null)
            {
                int dir = 0;
                foreach (var key in state.NewKeys)
                {
                    if (key == Keys.Up || key == Keys.Left) dir = -1;
                    else if (key == Keys.Down || key == Keys.Right) dir = 1;
                }
                if (dir != 0)
                {
                    var group = GetRadioGroup(_RadioGroup);
                    int idx = group.IndexOf(this);
                    if (idx != -1)
                    {
                        int next = (idx + dir + group.Count) % group.Count;
                        var target = group[next];
                        target.Selected = true;
                        this.Selected = false;
                        state.InputManager.SetFocus(target);
                    }
                }
            }
        }
    }
}
