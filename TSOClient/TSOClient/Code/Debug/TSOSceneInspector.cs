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

namespace TSOClient.Code.Debug
{
    public partial class TSOSceneInspector : Form
    {
        private Dictionary<TreeNode, object> ItemMap;

        public TSOSceneInspector()
        {
            ItemMap = new Dictionary<TreeNode, object>();
            InitializeComponent();

            tabWorld.Enabled = false;
            tabCamera.Enabled = false;
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

                ItemMap.Add(node, scene);
                node.Nodes.AddRange(ExploreScene(scene).ToArray());
                uiTree.Nodes.Add(node);
            }

            foreach (var scene in GameFacade.Scenes.ExternalScenes)
            {
                var node = new TreeNode(scene.ToString());

                ItemMap.Add(node, scene);
                node.Nodes.AddRange(ExploreScene(scene).ToArray());
                uiTree.Nodes.Add(node);
            }
            

            //var rootNode = new TreeNode("Scenes");
            //ItemMap[rootNode] = GameFacade.Screens.CurrentUIScreen;
            //rootNode.Nodes.AddRange(nodes.ToArray());

            //uiTree.Nodes.Add(rootNode);
        }

        private List<TreeNode> ExploreScene(ThreeDScene container){
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
                if (value is ThreeDScene)
                {
                    SetSelectedScene((ThreeDScene)value);
                }
                else
                {
                    SetSelected((ThreeDElement)ItemMap[e.Node]);
                }
            }
        }


        private ThreeDScene SelectedScene;
        private void SetSelectedScene(ThreeDScene scene)
        {
            if (scene == null)
            {
                tabCamera.Enabled = false;
                return;
            }

            tabCamera.Enabled = true;


            SelectedScene = null;

            cameraX.Value = (decimal)scene.Camera.Position.X;
            cameraY.Value = (decimal)scene.Camera.Position.Y;
            cameraZ.Value = (decimal)scene.Camera.Position.Z;

            cameraTargetX.Value = (decimal)scene.Camera.Target.X;
            cameraTargetY.Value = (decimal)scene.Camera.Target.Y;
            cameraTargetZ.Value = (decimal)scene.Camera.Target.Z;

            SelectedScene = scene;
        }

        private ThreeDElement Selected;
        private void SetSelected(ThreeDElement element)
        {
            if (element == null)
            {
                tabWorld.Enabled = false;
                tabCamera.Enabled = false;
                return;
            }

            SetSelectedScene(element.Scene);

            tabWorld.Enabled = true;
            tabCamera.Enabled = true;

            Selected = null;

            valueX.Value = (decimal)element.Position.X;
            valueY.Value = (decimal)element.Position.Y;
            valueZ.Value = (decimal)element.Position.Z;

            valueScaleX.Value = (decimal)element.Scale.X;
            valueScaleY.Value = (decimal)element.Scale.Y;
            valueScaleZ.Value = (decimal)element.Scale.Z;

            var rotateX = MathUtils.RadianToDegree((double)element.RotationX);
            var rotateY = MathUtils.RadianToDegree((double)element.RotationY);
            var rotateZ = MathUtils.RadianToDegree((double)element.RotationZ);


            valueRotateXBar.Value = (int)rotateX;
            valueRotateX.Value = (decimal)rotateX;

            valueRotateYBar.Value = (int)rotateY;
            valueRotateY.Value = (decimal)rotateY;

            valueRotateZBar.Value = (int)rotateZ;
            valueRotateZ.Value = (decimal)rotateZ;

            Selected = element;

            //valueScaleX.Value = (decimal)element.ScaleX;
            //valueScaleY.Value = (decimal)element.ScaleY;
            //valueAlpha.Value = (decimal)element.Opacity;

        }

        private void valueZ_ValueChanged(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                DoSetPosition();
            }
        }

        private void DoSetPosition()
        {
            Selected.Position = new Microsoft.Xna.Framework.Vector3((float)valueX.Value, (float)valueY.Value, (float)valueZ.Value);
        }



        private void valueScaleX_ValueChanged(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                if (valueScaleLock.Checked)
                {
                    valueScaleY.Value = valueScaleZ.Value = valueScaleX.Value;
                }
                DoSetScale();
            }
        }

        private void valueScaleY_ValueChanged(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                if (valueScaleLock.Checked)
                {
                    valueScaleX.Value = valueScaleZ.Value = valueScaleY.Value;
                }
                DoSetScale();
            }
        }

        private void valueScaleZ_ValueChanged(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                if (valueScaleLock.Checked)
                {
                    valueScaleX.Value = valueScaleY.Value = valueScaleZ.Value;
                }
                DoSetScale();
            }
        }

        private void DoSetScale()
        {
            Selected.Scale = new Microsoft.Xna.Framework.Vector3((float)valueScaleX.Value, (float)valueScaleY.Value, (float)valueScaleZ.Value);
        }


        private void valueRotateXBar_Scroll(object sender, EventArgs e)
        {
            valueRotateX.Value = valueRotateXBar.Value;
            if (Selected != null)
            {
                Selected.RotationX = (float)MathUtils.DegreeToRadian((double)valueRotateX.Value);
            }
        }

        private void valueRotateYBar_Scroll(object sender, EventArgs e)
        {
            valueRotateY.Value = valueRotateYBar.Value;
            if (Selected != null)
            {
                Selected.RotationY = (float)MathUtils.DegreeToRadian((double)valueRotateY.Value);
            }
        }

        private void valueRotateZBar_Scroll(object sender, EventArgs e)
        {
            valueRotateZ.Value = valueRotateZBar.Value;
            if (Selected != null)
            {
                Selected.RotationZ = (float)MathUtils.DegreeToRadian((double)valueRotateZ.Value);
            }
        }

        private void valueRotateX_ValueChanged(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                Selected.RotationX = (float)MathUtils.DegreeToRadian((double)valueRotateX.Value);
            }
        }

        private void valueRotateY_ValueChanged(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                Selected.RotationY = (float)MathUtils.DegreeToRadian((double)valueRotateY.Value);
            }
        }

        private void valueRotateZ_ValueChanged(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                Selected.RotationZ = (float)MathUtils.DegreeToRadian((double)valueRotateZ.Value);
            }
        }

        private void cameraX_ValueChanged(object sender, EventArgs e)
        {
            if (SelectedScene != null)
            {
                SelectedScene.Camera.Position = new Microsoft.Xna.Framework.Vector3(
                    (float)cameraX.Value, (float)cameraY.Value, (float)cameraZ.Value    
                );
            }
        }

        private void cameraTargetX_ValueChanged(object sender, EventArgs e)
        {
            if (SelectedScene != null)
            {
                SelectedScene.Camera.Target = new Microsoft.Xna.Framework.Vector3(
                    (float)cameraTargetX.Value, (float)cameraTargetY.Value, (float)cameraTargetZ.Value
                );
            }
        }


        //private void valueScaleX_ValueChanged(object sender, EventArgs e)
        //{
        //    if (valueScaleLock.Checked)
        //    {
        //        valueScaleY.Value = valueScaleX.Value;
        //        if (Selected != null)
        //        {
        //            Selected.ScaleX = Selected.ScaleY = (float)valueScaleX.Value;
        //        }
        //    }
        //    else
        //    {
        //        if (Selected != null)
        //        {
        //            Selected.ScaleX = (float)valueScaleX.Value;
        //        }
        //    }
        //}

        //private void valueScaleY_ValueChanged(object sender, EventArgs e)
        //{
        //    if (valueScaleLock.Checked)
        //    {
        //        valueScaleX.Value = valueScaleY.Value;
        //        if (Selected != null)
        //        {
        //            Selected.ScaleX = Selected.ScaleY = (float)valueScaleY.Value;
        //        }
        //    }
        //    else
        //    {
        //        if (Selected != null)
        //        {
        //            Selected.ScaleY = (float)valueScaleY.Value;
        //        }
        //    }
        //}

        //private void valueAlpha_ValueChanged(object sender, EventArgs e)
        //{
        //    if (Selected != null)
        //    {
        //        Selected.Opacity = (float)valueAlpha.Value;
        //    }
        //}

    }
}
