using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content;
using tso.world.components;
using tso.world.model;
using Microsoft.Xna.Framework;
using tso.files.formats.iff.chunks;
using tso.simantics.model;

namespace tso.simantics
{
    public class VMGameObject : VMEntity
    {
        /** Definition **/
        private ObjectComponent WorldUI;

        public VMGameObject(GameObject def, ObjectComponent worldUI) : base(def)
        {
            this.WorldUI = worldUI;

            /*var mainFunction = def.Master.MainFuncID;
            if (mainFunction > 0)
            {
                var bhav = def.Iff.BHAVs.First(x => x.ID == mainFunction);
                int y = 22;
            }*/
        }

        public override void SetDynamicSpriteFlag(ushort index, bool set)
        {
            base.SetDynamicSpriteFlag(index, set);
            if (this.WorldUI != null){
                this.WorldUI.DynamicSpriteFlags = this.DynamicSpriteFlags;
            }
        }

        public override bool SetValue(VMStackObjectVariable var, short value)
        {
            return base.SetValue(var, value);
        }

        public override short GetValue(VMStackObjectVariable var)
        {
            return base.GetValue(var);
        }

        public bool RefreshGraphic()
        {
            var newGraphic = Object.OBJ.BaseGraphicID + ObjectData[(int)VMStackObjectVariable.Graphic];
            var dgrp = Object.Resource.Get<DGRP>((ushort)newGraphic);
            if (dgrp != null)
            {
                WorldUI.DGRP = dgrp;
                return true;
            }
            return false;
        }


        public override void Init(tso.simantics.VMContext context){
            base.Init(context);
            //context.World.AddComponent(this.WorldUI);

            /** Aquarium, we will allow the load and main functions to run for this object **/
  
        }

        public Direction Direction { get { return WorldUI.Direction; } }
        public Vector3 Position { get { return new Vector3(WorldUI.TileX, WorldUI.TileY, 0.0f); } }

        public override string ToString()
        {
            var strings = Object.Resource.Get<CTSS>(Object.OBJ.CatalogStringsID);
            if (strings != null){
                return strings.GetString(0);
            }
            var label = Object.OBJ.ChunkLabel;
            if (label != null && label.Length > 0){
                return label;
            }
            return Object.OBJ.GUID.ToString("X");
        }
        
    }
}
