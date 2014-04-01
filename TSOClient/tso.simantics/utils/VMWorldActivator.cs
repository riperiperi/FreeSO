using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world;
using tso.world.model;
using tso.world.components;
using TSO.Content;
using Microsoft.Xna.Framework;
using TSO.Files.formats.iff.chunks;

namespace TSO.Simantics.utils
{
    /// <summary>
    /// Handles object creation and destruction
    /// </summary>
    public class VMWorldActivator
    {
        private VM VM;
        private World World;
        private Blueprint Blueprint;

        public VMWorldActivator(VM vm, World world){
            this.VM = vm;
            this.World = world;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public Blueprint LoadFromXML(XmlHouseData model){
            this.Blueprint = new Blueprint(model.Size, model.Size);
            VM.Context.Blueprint = Blueprint;

            foreach (var obj in model.Objects.Where(x => x.Level == 1)){
                CreateObject(obj);
            }
            foreach (var floor in model.World.Floors.Where(x => x.Level == 0)){
                Blueprint.SetFloor(floor.X, floor.Y, new FloorComponent() { FloorID = (ushort)floor.Value });
            }

            var testAquarium = new XmlHouseDataObject(); //used to create an aquarium to test with on the lot. remove this before final! (cant be giving out free aquariums!!)
            testAquarium.GUID = "0x98E0F8BD";
            testAquarium.X = 33;
            testAquarium.Y = 57;
            testAquarium.Level = 1;
            testAquarium.Dir = 4;
            CreateObject(testAquarium);


            var testCounter = new XmlHouseDataObject(); //test fridge
            testCounter.GUID = "0x675C18AF";
            testCounter.X = 34;
            testCounter.Y = 53;
            testCounter.Level = 1;
            testCounter.Dir = 4;
            CreateObject(testCounter);

            testCounter = new XmlHouseDataObject(); //test hat rack
            testCounter.GUID = "0x01DACE5C";
            testCounter.X = 36;
            testCounter.Y = 53;
            testCounter.Level = 1;
            testCounter.Dir = 4;
            CreateObject(testCounter);

            testCounter = new XmlHouseDataObject(); //test tp1
            testCounter.GUID = "0x96a776ce";
            testCounter.X = 40;
            testCounter.Y = 53;
            testCounter.Level = 1;
            testCounter.Dir = 0;
            CreateObject(testCounter);

            testCounter = new XmlHouseDataObject(); //test tp2
            testCounter.GUID = "0x96a776ce";
            testCounter.X = 20;
            testCounter.Y = 53;
            testCounter.Level = 1;
            testCounter.Dir = 0;
            CreateObject(testCounter);

            testCounter = new XmlHouseDataObject(); //test piano
            testCounter.GUID = "0x379EE047";
            testCounter.X = 20;
            testCounter.Y = 20;
            testCounter.Level = 1;
            testCounter.Dir = 2;
            CreateObject(testCounter);

            
            testCounter = new XmlHouseDataObject(); //test limo
            testCounter.GUID = "0x9750EA9D";
            testCounter.X = 30;
            testCounter.Y = 30;
            testCounter.Level = 1;
            testCounter.Dir = 4;
            CreateObject(testCounter);

            testCounter = new XmlHouseDataObject(); //test Fountain
            testCounter.GUID = "0x3565E02A";
            testCounter.X = 40;
            testCounter.Y = 30;
            testCounter.Level = 1;
            testCounter.Dir = 4;
            CreateObject(testCounter);

            testCounter = new XmlHouseDataObject(); //test Aqu2
            testCounter.GUID = "0x2FC9B87D";
            testCounter.X = 35;
            testCounter.Y = 36;
            testCounter.Level = 1;
            testCounter.Dir = 4;
            CreateObject(testCounter);

            testCounter = new XmlHouseDataObject(); //test Hot tub
            testCounter.GUID = "0x8FED54C2";
            testCounter.X = 35;
            testCounter.Y = 39;
            testCounter.Level = 1;
            testCounter.Dir = 4;
            CreateObject(testCounter);


            testCounter = new XmlHouseDataObject(); //test Hot tub
            testCounter.GUID = "0x5E8B157A";
            testCounter.X = 25;
            testCounter.Y = 40;
            testCounter.Level = 1;
            testCounter.Dir = 4;
            CreateObject(testCounter);

            

            Blueprint.Terrain = CreateTerrain(model);
            World.State.WorldSize = model.Size;
            return this.Blueprint;
        }

        private TerrainComponent CreateTerrain(XmlHouseData model)
        {
            var terrain = new TerrainComponent(new Rectangle(1, 1, model.Size - 2, model.Size - 2));
            this.InitWorldComponent(terrain);
            return terrain;
        }

        public VMAvatar CreateAvatar()
        {
            var avatar = new VMAvatar();
            this.InitWorldComponent(avatar.WorldUI);
            Blueprint.AddAvatar((AvatarComponent)avatar.WorldUI);
            VM.AddEntity(avatar);
            return avatar;
        }

        public VMGameObject CreateObject(XmlHouseDataObject obj){
            return VM.Context.CreateObjectInstance(obj.GUIDInt, (short)obj.X, (short)obj.Y, (sbyte)obj.Level, obj.Direction);
        }


        private void InitWorldComponent(WorldComponent component)
        {
            component.Initialize(this.World.State.Device, this.World.State);
        }

    }
}
