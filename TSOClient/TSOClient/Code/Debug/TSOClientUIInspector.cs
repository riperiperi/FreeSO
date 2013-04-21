using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TSOClient.Code.UI.Framework;

namespace TSOClient.Code.Debug
{
    public partial class TSOClientUIInspector : Form
    {
        private Dictionary<TreeNode, UIElement> ItemMap;

        public TSOClientUIInspector()
        {
            ItemMap = new Dictionary<TreeNode, UIElement>();
            InitializeComponent();

            propertyBox.Enabled = false;
            RefreshUITree();
        }

        private void refreshBtn_Click(object sender, EventArgs e)
        {
            RefreshUITree();
        }

        private void RefreshUITree()
        {
            ItemMap.Clear();

            var nodes = ExploreUIContainer(GameFacade.Screens.CurrentUIScreen);
            uiTree.Nodes.Clear();

            var rootNode = new TreeNode(GameFacade.Screens.CurrentUIScreen.ToString());
            ItemMap[rootNode] = GameFacade.Screens.CurrentUIScreen;
            rootNode.Nodes.AddRange(nodes.ToArray());

            uiTree.Nodes.Add(rootNode);
        }

        private List<TreeNode> ExploreUIContainer(UIContainer container){
            var result = new List<TreeNode>();

            foreach (var child in container.GetChildren())
            {
                var node = new TreeNode(child.ToString());
                ItemMap.Add(node, child);

                if (child is UIContainer)
                {
                    node.Nodes.AddRange(ExploreUIContainer((UIContainer)child).ToArray());
                }
                result.Add(node);
            }

            return result;
        }

        private void uiTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SetSelected(ItemMap[e.Node]);
        }

        private UIElement Selected;
        private void SetSelected(UIElement element)
        {
            if (element == null)
            {
                propertyBox.Enabled = false;
                return;
            }

            propertyBox.Enabled = true;
            Selected = element;

            valueX.Value = (decimal)element.X;
            valueY.Value = (decimal)element.Y;
            valueScaleX.Value = (decimal)element.ScaleX;
            valueScaleY.Value = (decimal)element.ScaleY;
            valueAlpha.Value = (decimal)element.Opacity;

        }

        private void valueX_ValueChanged(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                Selected.X = (float)valueX.Value;
            }
        }

        private void valueY_ValueChanged(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                Selected.Y = (float)valueY.Value;
            }
        }

        private void valueScaleX_ValueChanged(object sender, EventArgs e)
        {
            if (valueScaleLock.Checked)
            {
                valueScaleY.Value = valueScaleX.Value;
                if (Selected != null)
                {
                    Selected.ScaleX = Selected.ScaleY = (float)valueScaleX.Value;
                }
            }
            else
            {
                if (Selected != null)
                {
                    Selected.ScaleX = (float)valueScaleX.Value;
                }
            }
        }

        private void valueScaleY_ValueChanged(object sender, EventArgs e)
        {
            if (valueScaleLock.Checked)
            {
                valueScaleX.Value = valueScaleY.Value;
                if (Selected != null)
                {
                    Selected.ScaleX = Selected.ScaleY = (float)valueScaleY.Value;
                }
            }
            else
            {
                if (Selected != null)
                {
                    Selected.ScaleY = (float)valueScaleY.Value;
                }
            }
        }

        private void valueAlpha_ValueChanged(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                Selected.Opacity = (float)valueAlpha.Value;
            }
        }

    }
}
