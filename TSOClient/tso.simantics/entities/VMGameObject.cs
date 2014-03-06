using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content;
using tso.world.components;
using tso.world.model;
using Microsoft.Xna.Framework;
using TSO.Files.formats.iff.chunks;
using TSO.Simantics.model;

namespace TSO.Simantics
{
    public class VMGameObject : VMEntity
    {
        /** Definition **/

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
                ((ObjectComponent)this.WorldUI).DynamicSpriteFlags = this.DynamicSpriteFlags;
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
                ((ObjectComponent)WorldUI).DGRP = dgrp;
                return true;
            }
            return false;
        }


        public override void Init(TSO.Simantics.VMContext context){
            base.Init(context);
            //context.World.AddComponent(this.WorldUI);

            /** Aquarium, we will allow the load and main functions to run for this object **/
  
        }

        public override Direction Direction { 
            get { return ((ObjectComponent)WorldUI).Direction; }
            set { ((ObjectComponent)WorldUI).Direction = value; }
        }
        public override Vector3 Position { 
            get { return new Vector3(WorldUI.TileX, WorldUI.TileY, 0.0f); }
            set { /*yeah should probably implement this*/ }
        }

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
