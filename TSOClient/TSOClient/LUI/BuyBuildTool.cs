using System;
using System.Collections.Generic;
using System.Text;

namespace TSOClient.LUI
{
    public abstract class BuyBuildTool
    {
        protected IsometricView myParentView;

        protected bool myCanChangePosition;

        protected bool myCanRotate;

        protected bool myCanZoom;

        public bool CanChangePosition { get { return myCanChangePosition; } }

        public bool CanRotate { get { return myCanRotate; } }

        public bool CanZoom { get { return myCanZoom; } }

        public BuyBuildTool(IsometricView parentView)
        {
            myParentView = parentView;

        }

        public virtual void MouseDown(int x, int y)
        {

        }

        public virtual void MouseUp(int x, int y)
        {

        }

        public virtual void Update()
        {

        }
    }
}
