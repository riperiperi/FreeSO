using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TSOClient.Code.UI.Framework;
using TSOClient.ThreeD;
using TSOClient.Code.Utils;
using tso.common.rendering.framework;

namespace TSOClient.Code.Debug
{
    public partial class TSOSceneInspector : Form
    {
        private Dictionary<TreeNode, object> ItemMap;

        public TSOSceneInspector()
        {
            ItemMap = new Dictionary<TreeNode, object>();
            InitializeComponent();
            RefreshUITree();
        }

        private void refreshBtn_Click(object sender, EventArgs e)
        {
            RefreshUITree();
        }

        private void RefreshUITree()
        {
            ItemMap.Clear();

            uiTree.Nodes.Clear();
            foreach (var scene in GameFacade.Scenes.Scenes)
            {
                var node = new TreeNode(scene.ToString());
                var cameraNode = node.Nodes.Add("Camera");
                ItemMap.Add(cameraNode, scene.Camera);

                ItemMap.Add(node, scene);
                node.Nodes.AddRange(ExploreScene(scene).ToArray());
                uiTree.Nodes.Add(node);
            }

            //foreach (var scene in GameFacade.Scenes.ExternalScenes)
            //{
            //    var node = new TreeNode(scene.ToString());

            //    ItemMap.Add(node, scene);
            //    node.Nodes.AddRange(ExploreScene(scene).ToArray());
            //    uiTree.Nodes.Add(node);
            //}

            
            

            //var rootNode = new TreeNode("Scenes");
            //ItemMap[rootNode] = GameFacade.Screens.CurrentUIScreen;
            //rootNode.Nodes.AddRange(nodes.ToArray());

            //uiTree.Nodes.Add(rootNode);
        }

        private List<TreeNode> ExploreScene(_3DScene container)
        {
            var result = new List<TreeNode>();

            foreach (var child in container.GetElements())
            {
                var node = new TreeNode(child.ToString());
                ItemMap.Add(node, child);
                result.Add(node);
            }

            return result;
        }

        private void uiTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (ItemMap.ContainsKey(e.Node))
            {
                var value = ItemMap[e.Node];
                propertyGrid1.SelectedObject = value;
            }
        }

    }
}
