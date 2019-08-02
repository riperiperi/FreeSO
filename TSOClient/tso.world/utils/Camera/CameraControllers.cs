using FSO.Common.Rendering.Framework.Camera;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Utils.Camera
{
    public class CameraControllers
    {
        //todo: camera combiner
        public Matrix View
        {
            get
            {
                return ActiveCamera.BaseCamera.View;
            }
        }

        public Matrix Projection
        {
            get
            {
                return ActiveCamera.BaseCamera.Projection;
            }
        }

        public CameraController2D Camera2D;
        public CameraController3D Camera3D;
        public CameraControllerFP CameraFirstPerson;

        public List<ICameraController> Cameras;
        public ICameraController ActiveCamera;

        public void SetCameraType(World world, CameraControllerType type)
        {
            ICameraController target;
            switch (type)
            {
                case CameraControllerType._2D:
                    target = Camera2D;
                    break;
                case CameraControllerType._3D:
                    target = Camera3D;
                    break;
                case CameraControllerType.FirstPerson:
                    target = CameraFirstPerson;
                    break;
                default:
                    target = null;
                    break;
            }

            if (target != null)
            {
                target.SetActive(ActiveCamera, world);
                ActiveCamera = target;
            }
        }
    }

    public enum CameraControllerType
    {
        _2D,
        _3D,
        FirstPerson
    }
}
