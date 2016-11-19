using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Controls
{
    public class UIRadioButton : UIButton
    {
        private string _RadioGroup;
        public object RadioData { get; set; }

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
    }
}
