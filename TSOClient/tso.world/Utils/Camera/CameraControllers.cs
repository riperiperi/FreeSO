using FSO.Common;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Utils.Camera
{
    public class CameraControllers : ICamera
    {
        private float TransitionTime = 0.66f;
        public bool DisableTransitions = false;
        public Vector3? ModelTranslation;

        //lerp on transitions:
        // start with active camera. (get matrix)
        // iterate through all transition cameras, lerping 
        public Matrix View
        {
            get
            {
                Matrix result;
                if (TransitionWeights.Count > 0 && !DisableTransitions)
                {
                    //blend together cameras
                    result = GetTransitionMtx(cam => cam.View, DecompLerp); //
                } else
                {
                    result = ActiveCamera.BaseCamera.View;
                }
                if (ModelTranslation == null) return result;
                else return Matrix.CreateTranslation(-ModelTranslation.Value) * result;
            }
        }

        public Matrix Projection
        {
            get
            {
                if (TransitionWeights.Count > 0 && !DisableTransitions)
                {
                    //blend together cameras
                    return GetTransitionMtx(cam => cam.Projection, LerpProj);
                }
                return ActiveCamera.BaseCamera.Projection;
            }
        }

        private Matrix GetTransitionMtx(Func<ICamera, Matrix> matrixProvider, Func<Matrix, Matrix, float, Matrix> blendFunc)
        {
            Matrix baseMtx = matrixProvider(ActiveCamera.BaseCamera);
            foreach (var mtx in TransitionWeights)
            {
                var pct = mtx.Percent;
                if (blendFunc == LerpProj) pct = (float)Math.Pow(pct, mtx.Power);
                if (mtx.IsLinear) baseMtx = Matrix.Lerp(baseMtx, matrixProvider(mtx.Camera), pct);
                else baseMtx = blendFunc(baseMtx, matrixProvider(mtx.Camera), pct);
            }
            return baseMtx;
        }

        private Matrix LerpProj(Matrix from, Matrix to, float i)
        {
            return Matrix.Lerp(from, to, i);
        }

        private Matrix DecompLerp(Matrix from, Matrix to, float i)
        {
            Vector3 scale; Quaternion quat; Vector3 translation;
            from.Decompose(out scale, out quat, out translation);

            Vector3 scale2; Quaternion quat2; Vector3 translation2;
            to.Decompose(out scale2, out quat2, out translation2);

            return Matrix.CreateScale(Vector3.Lerp(scale, scale2, i))
                 * Matrix.CreateFromQuaternion(Quaternion.Slerp(quat, quat2, i))
                 * Matrix.CreateTranslation(Vector3.Lerp(translation, translation2, i));
        }

        public bool Safe2D => TransitionWeights.Count == 0 && ActiveCamera == Camera2D && Camera2D.Camera.RotateOff == 0;

        public Vector3 Position { get => ActiveCamera.BaseCamera.Position; set => ActiveCamera.BaseCamera.Position = value; }
        public Vector3 Target { get => ActiveCamera.BaseCamera.Target; set => ActiveCamera.BaseCamera.Target = value; }
        public Vector3 Up { get => ActiveCamera.BaseCamera.Up; set => ActiveCamera.BaseCamera.Up = value; }
        public Vector3 Translation { get => ActiveCamera.BaseCamera.Translation; set => ActiveCamera.BaseCamera.Translation = value; }
        public Vector2 ProjectionOrigin { get => ActiveCamera.BaseCamera.ProjectionOrigin; set => ActiveCamera.BaseCamera.ProjectionOrigin = value; }
        public float NearPlane { get => ActiveCamera.BaseCamera.NearPlane; set => ActiveCamera.BaseCamera.NearPlane = value; }
        public float FarPlane { get => ActiveCamera.BaseCamera.FarPlane; set => ActiveCamera.BaseCamera.FarPlane = value; }
        public float Zoom { get => ActiveCamera.BaseCamera.Zoom; set => ActiveCamera.BaseCamera.Zoom = value; }
        public float AspectRatioMultiplier { get => ActiveCamera.BaseCamera.AspectRatioMultiplier; set => ActiveCamera.BaseCamera.AspectRatioMultiplier = value; }
        public bool HideUI => ActiveCamera == CameraFirstPerson && CameraFirstPerson.FirstPersonAvatar == null;

        public List<CameraTransition> TransitionWeights = new List<CameraTransition>();

        public CameraController2D Camera2D;
        public CameraController3D Camera3D;
        public CameraControllerFP CameraFirstPerson;

        public List<ICameraController> Cameras;
        private ICameraController _ActiveCamera;
        public ICameraController ActiveCamera => _ActiveCamera ?? Camera2D;
        public CameraControllerType ActiveType;
        private WorldState State;

        private CameraTransition _ExternalTransition;

        public CameraControllers(GraphicsDevice gd, WorldState state)
        {
            State = state;
            Camera2D = new CameraController2D(gd);
            Camera3D = new CameraController3D(gd, state);
            CameraFirstPerson = new CameraControllerFP(gd, state);

            Cameras = new List<ICameraController>() { Camera2D, Camera3D, CameraFirstPerson };
        }

        public void WithTransitionsDisabled(Action action)
        {
            var old = DisableTransitions;
            var oldRot = Camera2D.Camera.RotateOff;
            Camera2D.Camera.RotateOff = 0;
            DisableTransitions = true;
            action();
            DisableTransitions = old;
            Camera2D.Camera.RotateOff = oldRot;
        }

        public bool ExternalTransitionActive()
        {
            if (_ExternalTransition == null) return false;
            else return _ExternalTransition.Percent > 0;
        }

        public CameraTransition GetExternalTransition()
        {
            if (_ExternalTransition == null)
            {
                
                _ExternalTransition = new CameraTransition(new DummyCamera(), 0, 0, 1);
                TransitionWeights.Add(_ExternalTransition);
            }
            return _ExternalTransition;
        }

        public void ClearExternalTransition()
        {
            TransitionWeights.Remove(_ExternalTransition);
            _ExternalTransition = null;
        }

        public void SetCameraType(World world, CameraControllerType type, float transitionTime = -1)
        {
            if (transitionTime < 0) transitionTime = TransitionTime;
            if (_ActiveCamera != null && transitionTime > 0)
            {
                //start transitioning the last camera
                TransitionWeights.Add(new CameraTransition(_ActiveCamera.BaseCamera, 1f, transitionTime, type == CameraControllerType._3D ? (1/50f) : 5f));
            }
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
                TransitionWeights.RemoveAll(x => x.Camera == target.BaseCamera);
                var prev = _ActiveCamera;

                prev = target.BeforeActive(prev, world);
                _ActiveCamera = target;
                target.OnActive(prev, world);
                InvalidateCamera(world.State);
            }
            ActiveType = type;
        }

        public void ForceCamera(WorldState state, CameraControllerType type)
        {
            TransitionWeights.Clear();
            _ExternalTransition = null;
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
                _ActiveCamera = target;
                InvalidateCamera(state);
            }
        }

        public void InvalidateCamera(WorldState state)
        {
            ActiveCamera?.InvalidateCamera(state);
        }

        public void SetDimensions(Vector2 dim)
        {
            foreach (var camera in Cameras)
            {
                camera.SetDimensions(dim);
            }
        }

        public void Update(UpdateState state, World world)
        {
            if (ActiveType == CameraControllerType._2D) ClearExternalTransition();
            for (int i = TransitionWeights.Count - 1; i >= 0; i--)
            {
                var trans = TransitionWeights[i];
                if (trans.Duration > 0)
                {
                    trans.Percent -= (1 / trans.Duration) / FSOEnvironment.RefreshRate;
                }
                if (trans.Percent <= 0f)
                {
                    if (trans == _ExternalTransition) _ExternalTransition = null;
                    TransitionWeights.RemoveAt(i);
                }
            }
            ActiveCamera.Update(state, world);
        }

        public void ProjectionDirty()
        {
        }
    }

    public class CameraTransition
    {
        public ICamera Camera;
        public float Percent;
        public float Duration;
        public float Power = 1f;
        public bool IsLinear;

        public CameraTransition(ICamera camera, float percent, float duration, float power)
        {
            Camera = camera;
            Percent = percent;
            Duration = duration;
            Power = power;
        }
    }

    public enum CameraControllerType
    {
        _2D,
        _3D,
        FirstPerson
    }
}
