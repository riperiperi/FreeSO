/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Files.utils;

namespace TSO.Files.formats.iff.chunks
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
            "BodyStringsID",
            "SlotID",
            "BHAV_AllowIntersectionID",
            "UsesInTable",
            "BitField1",
            "BHAV_PrepareFoodID",
            "BHAV_CookFoodID",
            "BHAV_PlaceSurfaceID",
            "BHAV_DisposeID",
            "BHAV_EatID",
            "BHAV_PickupID",
            "BHAV_WashDishID",
            "BHAV_EatSurfaceID",
            "BHAV_SitID",
            "BHAV_StandID",
            "SalePrice",
            "Unused35",
            "Unused36",
            "BrokenBaseGraphicOffset",
            "Unused38",
            "HasCriticalAttributes",
            "BuyModeType",
            "CatalogStringsID",
            "IsGlobalSimObject",
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
            "NumAttributes"
        };

        public ushort StackSize;
        public ushort BaseGraphicID;
        public ushort NumGraphics;
        public ushort TreeTableID;
        public ushort InteractionGroupID;
        public OBJDType ObjectType;
        public ushort MasterID;
        public short SubIndex;
        public ushort AnimationTableID;
        public uint GUID;
        public ushort Disabled;
        public ushort BHAV_Portal;
        public ushort Price;
        public ushort BodyStringID;
        public ushort SlotID;
        public ushort SalePrice;
        public ushort InitialDepreciation;
        public ushort DailyDepreciation;
        public ushort SelfDepreciating;
        public ushort DepreciationLimit;
        public ushort RoomFlags;
        public ushort FunctionFlags;
        public ushort CatalogStringsID;

        public ushort BHAV_MainID;
        public ushort BHAV_GardeningID;
        public ushort BHAV_WashHandsID;
        public ushort BHAV_AllowIntersectionID;
        public ushort UsesFnTable;
        public ushort BitField1;

        public ushort BHAV_PrepareFoodID;
        public ushort BHAV_CookFoodID;
        public ushort BHAV_PlaceSurfaceID;
        public ushort BHAV_DisposeID;
        public ushort BHAV_EatID;
        public ushort BHAV_PickupID;
        public ushort BHAV_WashDishID;
        public ushort BHAV_EatSurfaceID;
        public ushort BHAV_SitID;
        public ushort BHAV_StandID;

        public ushort Global;
        public ushort BHAV_Init;
        public ushort BHAV_Place;
        public ushort BHAV_UserPickup;
        public ushort WallStyle;
        public ushort BHAV_Load;
        public ushort BHAV_UserPlace;
        public ushort ObjectVersion;
        public ushort BHAV_RoomChange;
        public ushort MotiveEffectsID;
        public ushort BHAV_Cleanup;
        public ushort BHAV_LevelInfo;
        public ushort CatalogID;

        public ushort BHAV_ServingSurface;
        public ushort LevelOffset;
        public ushort Shadow;
        public ushort NumAttributes;

        public ushort BHAV_Clean;
        public ushort BHAV_QueueSkipped;
        public ushort FrontDirection;
        public ushort BHAV_WallAdjacencyChanged;
        public ushort MyLeadObject;
        public ushort DynamicSpriteBaseId;
        public ushort NumDynamicSprites;

        public ushort ChairEntryFlags;
        public ushort TileWidth;
        public ushort InhibitSuitCopying;
        public ushort BuildModeType;
        public ushort OriginalGUID1;
        public ushort OriginalGUID2;
        public ushort SuitGUID1;
        public ushort SuitGUID2;
        public ushort BHAV_Pickup;
        public ushort ThumbnailGraphic;
        public ushort ShadowFlags;
        public ushort FootprintMask;
        public ushort BHAV_DynamicMultiTileUpdate;
        public ushort ShadowBrightness;
        public ushort BHAV_Repair;

        public ushort WallStyleSpriteID;
        public ushort RatingHunger;
        public ushort RatingComfort;
        public ushort RatingHygiene;
        public ushort RatingBladder;
        public ushort RatingEnergy;
        public ushort RatingFun;
        public ushort RatingRoom;
        public ushort RatingSkillFlags;

        public ushort[] RawData;


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

        public override void Read(Iff iff, Stream stream)
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
                this.InteractionGroupID = io.ReadUInt16();
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
                this.BHAV_PickupID = io.ReadUInt16();
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
                this.InhibitSuitCopying = io.ReadUInt16();
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
                this.BHAV_Repair = io.ReadUInt16();

                if (numFields > 80)
                {
                    this.WallStyleSpriteID = io.ReadUInt16();
                    this.RatingHunger = io.ReadUInt16();
                    this.RatingComfort = io.ReadUInt16();
                    this.RatingHygiene = io.ReadUInt16();
                    this.RatingBladder = io.ReadUInt16();
                    this.RatingEnergy = io.ReadUInt16();
                    this.RatingFun = io.ReadUInt16();
                    this.RatingRoom = io.ReadUInt16();
                    this.RatingSkillFlags = io.ReadUInt16();
                }

                //if (this.NumAttributes == 0 && ObjectType != OBJDType.Portal)
                //{
                //    System.Diagnostics.Debug.WriteLine(GUID.ToString("x"));
                //}
            }
        }
    }
}
