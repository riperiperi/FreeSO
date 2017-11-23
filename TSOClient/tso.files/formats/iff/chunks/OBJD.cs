﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Files.Utils;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// Type of OBJD.
    /// </summary>
    [Serializable()]
    public enum OBJDType
    {
        Unknown = 0,
        //Character or NPC
        Person = 2,
        //Buyable objects
        Normal = 4,
        //Roaches, Stoves2, TrClownGen, AnimTester, HelpSystem, JobFinder, NPCController, Stoves, 
        //Tutorial, VisitGenerator, phonecall, unsnacker, CCPhonePlugin, EStove
        SimType = 7,
        //Stairs, doors, pool diving board & ladder, windows(?)
        Portal = 8,
        Cursor = 9,
        PrizeToken = 10,
        //Temporary location for drop or shoo
        Internal = 11,
        Food = 34
    }

    /// <summary>
    /// This is an object definition, the main chunk for an object and the first loaded by the VM. 
    /// There can be multiple master OBJDs in an IFF, meaning that one IFF file can define multiple objects. 
    /// </summary>
    public class OBJD : IffChunk
    {
        public uint Version;

        public static string[] VERSION_142_Fields = new string[]
        {
            "StackSize",
            "BaseGraphicID",
            "NumGraphics",
            "BHAV_MainID",
            "BHAV_GardeningID",
            "TreeTableID",
            "InteractionGroupID",
            "ObjectType",
            "MasterID",
            "SubIndex",
            "BHAV_WashHandsID",
            "AnimationTableID",
            "GUID1",
            "GUID2",
            "Disabled",
            "BHAV_Portal",
            "Price",
            "BodyStringID",
            "SlotID",
            "BHAV_AllowIntersectionID",
            "UsesFnTable",
            "BitField1",
            "BHAV_PrepareFoodID",
            "BHAV_CookFoodID",
            "BHAV_PlaceSurfaceID",
            "BHAV_DisposeID",
            "BHAV_EatID",
            "BHAV_PickupFromSlotID",
            "BHAV_WashDishID",
            "BHAV_EatSurfaceID",
            "BHAV_SitID",
            "BHAV_StandID",

            "SalePrice",
            "InitialDepreciation",
            "DailyDepreciation",
            "SelfDepreciating",
            "DepreciationLimit",
            "RoomFlags",
            "FunctionFlags",
            "CatalogStringsID",
                
            "Global",
            "BHAV_Init",
            "BHAV_Place",
            "BHAV_UserPickup",
            "WallStyle",
            "BHAV_Load",
            "BHAV_UserPlace",
            "ObjectVersion",
            "BHAV_RoomChange",
            "MotiveEffectsID",
            "BHAV_Cleanup",
            "BHAV_LevelInfo",
            "CatalogID",
            "BHAV_ServingSurface",
            "LevelOffset",
            "Shadow",
            "NumAttributes",

            "BHAV_Clean",
            "BHAV_QueueSkipped",
            "FrontDirection",
            "BHAV_WallAdjacencyChanged",
            "MyLeadObject",
            "DynamicSpriteBaseId",
            "NumDynamicSprites",

            "ChairEntryFlags",
            "TileWidth",
            "LotCategories",
            "BuildModeType",
            "OriginalGUID1",
            "OriginalGUID2",
            "SuitGUID1",
            "SuitGUID2",
            "BHAV_Pickup",
            "ThumbnailGraphic",
            "ShadowFlags",
            "FootprintMask",
            "BHAV_DynamicMultiTileUpdate",
            "ShadowBrightness",
            "BHAV_Repair",

            "WallStyleSpriteID",
            "RatingHunger",
            "RatingComfort",
            "RatingHygiene",
            "RatingBladder",
            "RatingEnergy",
            "RatingFun",
            "RatingRoom",
            "RatingSkillFlags",

            "NumTypeAttributes",
            "MiscFlags",
            "TypeAttrGUID1",
            "TypeAttrGUID2"
        };

        public ushort GUID1
        {
            get { return (ushort)(GUID); }
            set { GUID = (GUID & 0xFFFF0000) | value; }
        }
        public ushort GUID2
        {
            get { return (ushort)(GUID>>16); }
            set { GUID = (GUID & 0x0000FFFF) | ((uint)value<<16); }
        }

        public ushort StackSize { get; set; }
        public ushort BaseGraphicID { get; set; }
        public ushort NumGraphics { get; set; }
        public ushort TreeTableID { get; set; }
        public short InteractionGroupID { get; set; }
        public OBJDType ObjectType { get; set; }
        public ushort MasterID { get; set; }
        public short SubIndex { get; set; }
        public ushort AnimationTableID { get; set; }
        public uint GUID { get; set; }
        public ushort Disabled { get; set; }
        public ushort BHAV_Portal { get; set; }
        public ushort Price { get; set; }
        public ushort BodyStringID { get; set; }
        public ushort SlotID { get; set; }
        public ushort SalePrice { get; set; }
        public ushort InitialDepreciation { get; set; }
        public ushort DailyDepreciation { get; set; }
        public ushort SelfDepreciating { get; set; }
        public ushort DepreciationLimit { get; set; }
        public ushort RoomFlags { get; set; }
        public ushort FunctionFlags { get; set; }
        public ushort CatalogStringsID { get; set; }

        public ushort BHAV_MainID { get; set; }
        public ushort BHAV_GardeningID { get; set; }
        public ushort BHAV_WashHandsID { get; set; }
        public ushort BHAV_AllowIntersectionID { get; set; }
        public ushort UsesFnTable { get; set; }
        public ushort BitField1 { get; set; }

        public ushort BHAV_PrepareFoodID { get; set; }
        public ushort BHAV_CookFoodID { get; set; }
        public ushort BHAV_PlaceSurfaceID { get; set; }
        public ushort BHAV_DisposeID { get; set; }
        public ushort BHAV_EatID { get; set; }
        public ushort BHAV_PickupFromSlotID { get; set; }
        public ushort BHAV_WashDishID { get; set; }
        public ushort BHAV_EatSurfaceID { get; set; }
        public ushort BHAV_SitID { get; set; }
        public ushort BHAV_StandID { get; set; }

        public ushort Global { get; set; }
        public ushort BHAV_Init { get; set; }
        public ushort BHAV_Place { get; set; }
        public ushort BHAV_UserPickup { get; set; }
        public ushort WallStyle { get; set; }
        public ushort BHAV_Load { get; set; }
        public ushort BHAV_UserPlace { get; set; }
        public ushort ObjectVersion { get; set; }
        public ushort BHAV_RoomChange { get; set; }
        public ushort MotiveEffectsID { get; set; }
        public ushort BHAV_Cleanup { get; set; }
        public ushort BHAV_LevelInfo { get; set; }
        public ushort CatalogID { get; set; }

        public ushort BHAV_ServingSurface { get; set; }
        public ushort LevelOffset { get; set; }
        public ushort Shadow { get; set; }
        public ushort NumAttributes { get; set; }
   
        public ushort BHAV_Clean { get; set; }
        public ushort BHAV_QueueSkipped { get; set; }
        public ushort FrontDirection { get; set; }
        public ushort BHAV_WallAdjacencyChanged { get; set; }
        public ushort MyLeadObject { get; set; }
        public ushort DynamicSpriteBaseId { get; set; }
        public ushort NumDynamicSprites { get; set; }

        public ushort ChairEntryFlags { get; set; }
        public ushort TileWidth { get; set; }
        public ushort LotCategories { get; set; }
        public ushort BuildModeType { get; set; }
        public ushort OriginalGUID1 { get; set; }
        public ushort OriginalGUID2 { get; set; }
        public ushort SuitGUID1 { get; set; }
        public ushort SuitGUID2 { get; set; }
        public ushort BHAV_Pickup { get; set; }
        public ushort ThumbnailGraphic { get; set; }
        public ushort ShadowFlags { get; set; }
        public ushort FootprintMask { get; set; }
        public ushort BHAV_DynamicMultiTileUpdate { get; set; }
        public ushort ShadowBrightness { get; set; }
        public ushort BHAV_Repair { get; set; }

        public ushort WallStyleSpriteID { get; set; }
        public ushort RatingHunger { get; set; }
        public ushort RatingComfort { get; set; }
        public ushort RatingHygiene { get; set; }
        public ushort RatingBladder { get; set; }
        public ushort RatingEnergy { get; set; }
        public ushort RatingFun { get; set; }
        public ushort RatingRoom { get; set; }
        public ushort RatingSkillFlags { get; set; }

        public ushort[] RawData;
        public ushort NumTypeAttributes { get; set; }
        public ushort MiscFlags { get; set; }
        public uint TypeAttrGUID;

        public ushort FunctionSubsort { get; set; }
        public ushort DTSubsort { get; set; }
        public ushort KeepBuying { get; set; }
        public ushort VacationSubsort { get; set; }
        public ushort ResetLotAction { get; set; }
        public ushort CommunitySubsort { get; set; }
        public ushort DreamFlags { get; set; }
        public ushort RenderFlags { get; set; }
        public ushort VitaboyFlags { get; set; }
        public ushort STSubsort { get; set; }
        public ushort MTSubsort { get; set; }

        public ushort TypeAttrGUID1
        {
            get { return (ushort)(TypeAttrGUID); }
            set { TypeAttrGUID = (TypeAttrGUID & 0xFFFF0000) | value; }
        }
        public ushort TypeAttrGUID2
        {
            get { return (ushort)(TypeAttrGUID >> 16); }
            set { TypeAttrGUID = (TypeAttrGUID & 0x0000FFFF) | ((uint)value << 16); }
        }

        public bool IsMaster
        {
            get
            {
                return !IsMultiTile;
            }
        }

        public bool IsMultiTile
        {
            get {
                return MasterID != 0;
            }
        }

        public T GetPropertyByName<T>(string name)
        {
            Type me = typeof(OBJD);
            var prop = me.GetProperty(name);
            return (T)Convert.ChangeType(prop.GetValue(this, null), typeof(T));
        }

        public void SetPropertyByName(string name, object value)
        {
            Type me = typeof(OBJD);
            var prop = me.GetProperty(name);
            try
            {
                value = Convert.ChangeType(value, prop.PropertyType);
            } catch {
                value = Enum.Parse(prop.PropertyType, value.ToString());
            }
            prop.SetValue(this, value, null);
        }

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                this.Version = io.ReadUInt32();

                /**136 (80 fields)
                    138a (95 fields) - Used for The Sims 1 base game objects?
                    138b (108 fields) - Used for The Sims 1 expansion objects?
                    139 (96 fields)
                    140 (97 fields)
                    141 (97 fields)
                    142 (105 fields)**/
                var numFields = 80;
                if (Version == 138)
                {
                    numFields = 95;
                }
                else if (Version == 139)
                {
                    numFields = 96;
                }
                else if (Version == 140)
                {
                    numFields = 97;
                }
                else if (Version == 141)
                {
                    numFields = 97;
                }
                else if (Version == 142)
                {
                    numFields = 105;
                }

                numFields -= 2;
                RawData = new ushort[numFields];
                io.Mark();

                for (var i = 0; i < numFields; i++)
                {
                    RawData[i] = io.ReadUInt16();
                }

                io.SeekFromMark(0);

                this.StackSize = io.ReadUInt16();
                this.BaseGraphicID = io.ReadUInt16();
                this.NumGraphics = io.ReadUInt16();
                this.BHAV_MainID = io.ReadUInt16();
                this.BHAV_GardeningID = io.ReadUInt16();
                this.TreeTableID = io.ReadUInt16();
                this.InteractionGroupID = io.ReadInt16();
                this.ObjectType = (OBJDType)io.ReadUInt16();
                this.MasterID = io.ReadUInt16();
                this.SubIndex = io.ReadInt16();
                this.BHAV_WashHandsID = io.ReadUInt16();
                this.AnimationTableID = io.ReadUInt16();
                this.GUID = io.ReadUInt32();
                this.Disabled = io.ReadUInt16();
                this.BHAV_Portal = io.ReadUInt16();
                this.Price = io.ReadUInt16();
                this.BodyStringID = io.ReadUInt16();
                this.SlotID = io.ReadUInt16();
                this.BHAV_AllowIntersectionID = io.ReadUInt16();
                this.UsesFnTable = io.ReadUInt16();
                this.BitField1 = io.ReadUInt16();
                this.BHAV_PrepareFoodID = io.ReadUInt16();
                this.BHAV_CookFoodID = io.ReadUInt16();
                this.BHAV_PlaceSurfaceID = io.ReadUInt16();
                this.BHAV_DisposeID = io.ReadUInt16();
                this.BHAV_EatID = io.ReadUInt16();
                this.BHAV_PickupFromSlotID = io.ReadUInt16();
                this.BHAV_WashDishID = io.ReadUInt16();
                this.BHAV_EatSurfaceID = io.ReadUInt16();
                this.BHAV_SitID = io.ReadUInt16();
                this.BHAV_StandID = io.ReadUInt16();

                this.SalePrice = io.ReadUInt16();
                this.InitialDepreciation = io.ReadUInt16();
                this.DailyDepreciation = io.ReadUInt16();
                this.SelfDepreciating = io.ReadUInt16();
                this.DepreciationLimit = io.ReadUInt16();
                this.RoomFlags = io.ReadUInt16();
                this.FunctionFlags = io.ReadUInt16();
                this.CatalogStringsID = io.ReadUInt16();
                
                this.Global = io.ReadUInt16();
                this.BHAV_Init = io.ReadUInt16();
                this.BHAV_Place = io.ReadUInt16();
                this.BHAV_UserPickup = io.ReadUInt16();
                this.WallStyle = io.ReadUInt16();
                this.BHAV_Load = io.ReadUInt16();
                this.BHAV_UserPlace = io.ReadUInt16();
                this.ObjectVersion = io.ReadUInt16();
                this.BHAV_RoomChange = io.ReadUInt16();
                this.MotiveEffectsID = io.ReadUInt16();
                this.BHAV_Cleanup = io.ReadUInt16();
                this.BHAV_LevelInfo = io.ReadUInt16();
                this.CatalogID = io.ReadUInt16();
                this.BHAV_ServingSurface = io.ReadUInt16();
                this.LevelOffset = io.ReadUInt16();
                this.Shadow = io.ReadUInt16();
                this.NumAttributes = io.ReadUInt16();

                this.BHAV_Clean = io.ReadUInt16();
                this.BHAV_QueueSkipped = io.ReadUInt16();
                this.FrontDirection = io.ReadUInt16();
                this.BHAV_WallAdjacencyChanged = io.ReadUInt16();
                this.MyLeadObject = io.ReadUInt16();
                this.DynamicSpriteBaseId = io.ReadUInt16();
                this.NumDynamicSprites = io.ReadUInt16();

                this.ChairEntryFlags = io.ReadUInt16();
                this.TileWidth = io.ReadUInt16();
                this.LotCategories = io.ReadUInt16();
                this.BuildModeType = io.ReadUInt16();
                this.OriginalGUID1 = io.ReadUInt16();
                this.OriginalGUID2 = io.ReadUInt16();
                this.SuitGUID1 = io.ReadUInt16();
                this.SuitGUID2 = io.ReadUInt16();
                this.BHAV_Pickup = io.ReadUInt16();
                this.ThumbnailGraphic = io.ReadUInt16();
                this.ShadowFlags = io.ReadUInt16();
                this.FootprintMask = io.ReadUInt16();
                this.BHAV_DynamicMultiTileUpdate = io.ReadUInt16();
                this.ShadowBrightness = io.ReadUInt16();

                if (numFields > 78)
                {
                    this.BHAV_Repair = io.ReadUInt16();
                    this.WallStyleSpriteID = io.ReadUInt16();
                    this.RatingHunger = io.ReadUInt16();
                    this.RatingComfort = io.ReadUInt16();
                    this.RatingHygiene = io.ReadUInt16();
                    this.RatingBladder = io.ReadUInt16();
                    this.RatingEnergy = io.ReadUInt16();
                    this.RatingFun = io.ReadUInt16();
                    this.RatingRoom = io.ReadUInt16();
                    this.RatingSkillFlags = io.ReadUInt16();
                    if (numFields > 90)
                    {
                        this.NumTypeAttributes = io.ReadUInt16();
                        this.MiscFlags = io.ReadUInt16();
                        this.TypeAttrGUID = io.ReadUInt32();
                        try
                        {
                            this.FunctionSubsort = io.ReadUInt16();
                            this.DTSubsort = io.ReadUInt16();
                            this.KeepBuying = io.ReadUInt16();
                            this.VacationSubsort = io.ReadUInt16();
                            this.ResetLotAction = io.ReadUInt16();
                            this.CommunitySubsort = io.ReadUInt16();
                            this.DreamFlags = io.ReadUInt16();
                            this.RenderFlags = io.ReadUInt16();
                            this.VitaboyFlags = io.ReadUInt16();
                            this.STSubsort = io.ReadUInt16();
                            this.MTSubsort = io.ReadUInt16();
                        } catch (Exception)
                        {
                            //past this point if these fields are here is really a mystery
                        }
                    }
                    if (this.TypeAttrGUID == 0) this.TypeAttrGUID = GUID;
                }
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteUInt32(142);
                foreach (var prop in VERSION_142_Fields)
                {
                    io.WriteUInt16((ushort)GetPropertyByName<int>(prop));
                }
                for (int i = VERSION_142_Fields.Length; i < 105; i++)
                {
                    io.WriteUInt16(0);
                }
            }
            return true;
        }
    }
}
