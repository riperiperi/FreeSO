using System;
using System.Collections.Generic;
using System.Text;

namespace TSOClient.LUI
{
    class FloorTool : BuyBuildTool
    {
        private int myFloorIndex;
        private int myXInitial = 0, myYInitial = 0;
        private int myXFinal = 0, myYFinal = 0;
        private bool isChangeQueued;
        private bool hasFinalCoords;

        public FloorTool(IsometricView parentView, int floorIdx)
            : base(parentView)
        {
            myCanChangePosition = true;
            myCanRotate = false;
            myCanZoom = false;

            myFloorIndex = floorIdx;
        }

        public override void MouseDown(int x, int y)
        {
            if (!isChangeQueued)
            {
                myXInitial = x;
                myYInitial = y;
                isChangeQueued = true;
            }
            else
            {
                myXFinal = x;
                myYFinal = y;
                hasFinalCoords = true;
            }

            

            base.MouseDown(x, y);
        }

        public override void MouseUp(int x, int y)
        {
            //myXFinal = x;
            //myYFinal = y;
            myCanChangePosition = true;
            myCanRotate = false;
            myCanZoom = false;

            if (hasFinalCoords)
            {
                bool xReversed = false;
                bool yReversed = false;
                if (myXInitial > myXFinal)
                {
                    xReversed = true;
                }
                if (myYInitial > myYFinal)
                {
                    yReversed = true;
                }

                if (xReversed == false && yReversed == false)
                {
                    for (int i = myXInitial; i <= myXFinal; i++)
                    {
                        for (int j = myYInitial; j <= myYFinal; j++)
                        {
                            myParentView.AddFloor((Floor)(ContentManager.Floors[myFloorIndex].AddToWorld(i, j, 0)), i, j);
                        }
                    }
                }
                else if (xReversed == true && yReversed == false)
                {
                    for (int i = myXFinal; i <= myXInitial; i++)
                    {
                        for (int j = myYInitial; j <= myYFinal; j++)
                        {
                            myParentView.AddFloor((Floor)(ContentManager.Floors[myFloorIndex].AddToWorld(i, j, 0)), i, j);
                        }
                    }
                }
                else if (xReversed == false && yReversed == true)
                {
                    for (int i = myXInitial; i <= myXFinal; i++)
                    {
                        for (int j = myYFinal; j <= myYInitial; j++)
                        {
                            myParentView.AddFloor((Floor)(ContentManager.Floors[myFloorIndex].AddToWorld(i, j, 0)), i, j);
                        }
                    }
                }
                else if (xReversed == true && yReversed == true)
                {
                    for (int i = myXFinal; i <= myXInitial; i++)
                    {
                        for (int j = myYFinal; j <= myYInitial; j++)
                        {
                            myParentView.AddFloor((Floor)(ContentManager.Floors[myFloorIndex].AddToWorld(i, j, 0)), i, j);
                        }
                    }
                }
                //isChangeQueued = false;
            }

            isChangeQueued = false;
            hasFinalCoords = false;

            base.MouseUp(x, y);
        }

        public override void Update()
        {
            if (hasFinalCoords)
            {
                bool xReversed = false;
                bool yReversed = false;
                if (myXInitial > myXFinal)
                {
                    xReversed = true;
                }
                if (myYInitial > myYFinal)
                {
                    yReversed = true;
                }

                if (xReversed == false && yReversed == false)
                {
                    for (int i = myXInitial; i <= myXFinal; i++)
                    {
                        for (int j = myYInitial; j <= myYFinal; j++)
                        {
                            myParentView.AddFloorTemporary((Floor)(ContentManager.Floors[myFloorIndex].AddToWorld(i, j, 0)), i, j);
                        }
                    }
                }
                else if (xReversed == true && yReversed == false)
                {
                    for (int i = myXFinal; i <= myXInitial; i++)
                    {
                        for (int j = myYInitial; j <= myYFinal; j++)
                        {
                            myParentView.AddFloorTemporary((Floor)(ContentManager.Floors[myFloorIndex].AddToWorld(i, j, 0)), i, j);
                        }
                    }
                }
                else if (xReversed == false && yReversed == true)
                {
                    for (int i = myXInitial; i <= myXFinal; i++)
                    {
                        for (int j = myYFinal; j <= myYInitial; j++)
                        {
                            myParentView.AddFloorTemporary((Floor)(ContentManager.Floors[myFloorIndex].AddToWorld(i, j, 0)), i, j);
                        }
                    }
                }
                else if (xReversed == true && yReversed == true)
                {
                    for (int i = myXFinal; i <= myXInitial; i++)
                    {
                        for (int j = myYFinal; j <= myYInitial; j++)
                        {
                            myParentView.AddFloorTemporary((Floor)(ContentManager.Floors[myFloorIndex].AddToWorld(i, j, 0)), i, j);
                        }
                    }
                }
                //isChangeQueued = false;
            }

            base.Update();
        }
    }
}
