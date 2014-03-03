using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSO.Content.model;
using TSO.Content;
using tso.world.utils;
using TSO.Files.formats.iff.chunks;
using Microsoft.Xna.Framework;

namespace tso.world.components
{
    public class FloorComponent : WorldComponent
    {
        private static Rectangle DESTINATION_NEAR = new Rectangle(4, 316, 128, 64);
        private static Rectangle DESTINATION_MED = new Rectangle(2, 158, 64, 32);
        private static Rectangle DESTINATION_FAR = new Rectangle(1, 79, 32, 16);


        private bool _Dirty = true;
        private bool _DirtyTexture = true;

        private int _Level;
        private ushort _FloorID;
        private Floor _Floor;
        private _2DSprite _Sprite;

        public override float PreferredDrawOrder
        {
            get {
                return 1000.0f;
            }
        }

        public ushort FloorID
        {
            get{
                return _FloorID;
            }
            set {
                _FloorID = value;
                _Floor = null;
                _DirtyTexture = true;
                _Dirty = true;
            }
        }

        public int Level { 
            get { return _Level; } 
            set { 
                _Level = value;
                _Dirty = true; 
            } 
        }

        public override void OnZoomChanged(WorldState world)
        {
            base.OnZoomChanged(world);
            _DirtyTexture = true;
            _Dirty = true;
        }

        public override void OnRotationChanged(WorldState world)
        {
            base.OnRotationChanged(world);
            _DirtyTexture = true;
            _Dirty = true;
        }

        public override void OnScrollChanged(WorldState world)
        {
            base.OnScrollChanged(world);
            _Dirty = true;
        }

        public override void Draw(GraphicsDevice device, WorldState world){
            if(_Dirty){
                if (_Floor == null){
                    _Floor = Content.Get().WorldFloors.Get((ulong)_FloorID);
                    if (_Floor != null)
                    {
                        _Sprite = new _2DSprite
                        {
                            RenderMode = _2DBatchRenderMode.NO_DEPTH
                        };
                    }
                }
                if (_DirtyTexture && _Floor != null){
                    SPR2 sprite = null;
                    switch(world.Zoom){
                        case WorldZoom.Far:
                            sprite = _Floor.Far;
                            _Sprite.DestRect = DESTINATION_FAR;
                            break;
                        case WorldZoom.Medium:
                            sprite = _Floor.Medium;
                            _Sprite.DestRect = DESTINATION_MED;
                            break;
                        case WorldZoom.Near:
                            sprite = _Floor.Near;
                            _Sprite.DestRect = DESTINATION_NEAR;
                            break;
                    }
                    _Sprite.Pixel = world._2D.GetTexture(sprite.Frames[0]);
                    _Sprite.SrcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, _Sprite.Pixel.Width, _Sprite.Pixel.Height);
                }

                _DirtyTexture = false;
                _Dirty = false;
            }

            if (_Sprite != null){
                world._2D.Draw(_Sprite);
            }
        }
    }
}
