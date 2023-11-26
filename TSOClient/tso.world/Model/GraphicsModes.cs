using System;

namespace FSO.LotView.Model
{
    [Flags]
    public enum ComponentRenderMode
    {
        _2D = 1,
        _3D = 2,
        Both = 3
    }

    public enum CameraRenderMode
    {
        _2D = 1,
        _2DRotate = 2, //2d camera, but must render with 3d instead
        _3D = 3
    }

    public enum GlobalGraphicsMode
    {
        Full2D, // only use 3d objects when 
        Hybrid2D, // load 2d and 3d objects, use 3d arch
        Full3D // do not load 2d dgrp
    }

    public static class ComponentRenderModeExtensions
    {
        public static bool IsSet(this ComponentRenderMode mode, ComponentRenderMode flag)
        {
            return (mode & flag) == flag;
        }
    }
}
