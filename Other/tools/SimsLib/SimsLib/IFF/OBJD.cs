/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using LogThis;

namespace SimsLib.IFF
{
    [Serializable()]
    public enum ObjectType
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
    /// The OBJD (OBJect Definition) is the main chunk for an object, and is the first chunk loaded by the VM.
    /// </summary>
    [Serializable()]
    public class OBJD : ISerializable
    {
        private int m_ID;

        private int m_Version;
        private ushort m_InitStackSize;
        //If non-zero, the base graphic is the resource ID of the first (or only) DGRP resource associated with this object.
        //If zero, it is not known how the image is provided for the object. 
        private ushort m_BaseGraphicID;
        //The number of graphics is the number of DGRP resources that are used to display various states of the object.
        private ushort m_NumGraphics;
        private ushort m_MainFuncID;
        private ushort m_GardeningFuncID;
        //The tree table ID is the resource ID of a TTAB (i.e., the set of menu actions) 
        //that is used to interact with the object. If zero, there is no interaction with the object.
        private ushort m_TreeTableID;
        private ushort m_InteractionGroup;
        private ObjectType m_ObjectType;
        
        //The master ID is used to identify multi-tile objects. If the master ID is zero, the OBJD is a 
        //single-tile object and the sub-index is ignored. If the master ID is non-zero, all OBJDs in the 
        //IFF file with the same master ID belong to a multi-tile object.
        private ushort m_MasterID;
        public bool IsMaster = false;
        public bool IsMultiTile = false;
        private ushort m_SubIndex;
        private ushort m_WashHandsID;
        private ushort m_AnimTableID;

        private uint m_GUID;

        private ushort m_Disabled;
        private ushort m_Portal;
        private ushort m_Price;
        private ushort m_BodyStringsID;
        private ushort m_SLOTID;
        private ushort m_AllowsIntersectionID;
        private ushort m_PrepareFoodID;
        private ushort m_CookFoodID;
        private ushort m_PlaceOnSurfaceID;
        private ushort m_DisposeID;
        private ushort m_EatFoodID;
        private ushort m_PickupFromSlotID;
        private ushort m_WashDishID;
        private ushort m_EatingSurfaceID;
        private ushort m_SitID;
        private ushort m_StandID;
        private ushort m_SalePrice;
        private ushort m_InitialDepreciation;
        private ushort m_DailyDepreciation;
        private ushort m_SelfDepreciating;
        private ushort m_DepreciationLimit;

        /// <summary>
        /// The chunk ID of this OBJD chunk.
        /// </summary>
        public int ChunkID
        {
            get { return m_ID; }
        }

        /// <summary>
        /// The initial size to set the stack for this object's code to.
        /// </summary>
        public ushort InitialStackSize
        {
            get { return m_InitStackSize; }
        }

        /// <summary>
        /// If non-zero, the base graphic is the resource ID of the first (or only) DGRP resource
        /// associated with this object. If zero, it is not known how the image is provided for the object. 
        /// </summary>
        public ushort BaseGraphicsID
        {
            get { return m_BaseGraphicID; }
        }

        /// <summary>
        /// The number of graphics is the number of DGRP resources that are used to display various states of the object.
        /// </summary>
        public ushort NumGraphics
        {
            get { return m_NumGraphics; }
        }

        /// <summary>
        /// The ID of the BHAV chunk that contains the code for the object's main function.
        /// This should be ignored if an OBJf with the same ID as this OBJD is present.
        /// </summary>
        public ushort MainFuncID
        {
            get { return m_MainFuncID; }
        }

        /// <summary>
        /// The ID of the BHAV chunk that contains the code for the object's gardening function.
        /// This should be ignored if an OBJf with the same ID as this OBJD is present.
        /// </summary>
        public ushort GardeningFuncID
        {
            get { return m_GardeningFuncID; }
        }

        /// <summary>
        /// The tree table ID is the resource ID of a TTAB (i.e., the set of menu actions) 
        /// that is used to interact with the object.  If zero, there is no interaction with the object.
        /// </summary>
        public ushort TreeTableID
        {
            get { return m_TreeTableID; }
        }

        /// <summary>
        /// The master ID for this OBJD. If this is 0,
        /// the object is a single-tile object.
        /// </summary>
        public ushort MasterID
        {
            get { return m_MasterID; }
        }

        /// <summary>
        /// The sub index of this OBJD. If this is -1,
        /// the object is a master for a multi-tile object.
        /// In a multi-tile slave OBJD, the sub-index gives the (x,y) offset for the tile.  
        /// The upper byte is the zero-relative y offset, while the lower byte is the 
        /// zero-relative x offset.  
        /// </summary>
        public ushort SubIndex
        {
            get { return m_SubIndex; }
        }

        /// <summary>
        /// A unique 32-bit GUID for this object.
        /// </summary>
        public uint GUID
        {
            get { return m_GUID; }
        }

        /// <summary>
        /// Presumably, the disabled value is set to one to prevent the object from showing up in the game. 
        /// There are some shutters that have this set; no other object does. 
        /// </summary>
        public ushort Disabled
        {
            get { return m_Disabled; }
        }

        /// <summary>
        /// Is this object a portal (staircase)?
        /// </summary>
        public ushort Portal
        {
            get { return m_Portal; }
        }

        public ushort Price
        {
            get { return m_Price; }
        }

        /// <summary>
        /// The body strings ID is the ID of a STR# resource that contains various information about the character, 
        /// including things like the gender, sex, color, body type, and how to dress them for various circumstances. 
        /// Only characters and NPCs have this set.
        /// </summary>
        public ushort BodystringsID
        {
            get { return m_BodyStringsID; }
        }

        /// <summary>
        /// The slot ID is the ID of the SLOT resource associated with this object. 
        /// The SLOT resource provides routing infomation that allows a character to approach the object. 
        /// </summary>
        public ushort SLOTID
        {
            get { return m_SLOTID; }
        }

        /// <summary>
        /// The ID of a BHAV that contains a function (tree) used as a last resort for intersection calculation 
        /// (to determine if two objects are allowed to intersect).
        /// </summary>
        public ushort AllowsIntersectionID
        {
            get { return m_AllowsIntersectionID; }
        }

        public OBJD(byte[] ChunkData, int ID)
        {
            m_ID = ID;

            MemoryStream MemStream = new MemoryStream(ChunkData);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Version = Reader.ReadInt32();

            if (m_Version != 138)
            {
                //Assume log statements will be stored in the client's log...
                Log.LogThis("Tried loading OBJD chunk version: " + m_Version + " (SimsLib.dll)", eloglevel.error);
                return;
            }

            m_InitStackSize = Reader.ReadUInt16();
            m_BaseGraphicID = Reader.ReadUInt16();
            m_NumGraphics = Reader.ReadUInt16();

            m_MainFuncID = Reader.ReadUInt16();
            m_GardeningFuncID = Reader.ReadUInt16();

            m_TreeTableID = Reader.ReadUInt16();
            m_InteractionGroup = Reader.ReadUInt16();
            m_ObjectType = (ObjectType)Reader.ReadUInt16();

            m_MasterID = Reader.ReadUInt16();

            if (m_MasterID != 0)
                IsMultiTile = true;

            if (IsMultiTile)
            {
                m_SubIndex = Reader.ReadUInt16();

                if ((short)m_SubIndex == -1)
                    IsMaster = true;
            }

            m_WashHandsID = Reader.ReadUInt16();
            m_AnimTableID = Reader.ReadUInt16();

            m_GUID = Reader.ReadUInt32();

            m_Disabled = Reader.ReadUInt16();
            m_Portal = Reader.ReadUInt16();
            m_Price = Reader.ReadUInt16();

            if(m_ObjectType == ObjectType.Person)
                m_BodyStringsID = Reader.ReadUInt16();

            m_SLOTID = Reader.ReadUInt16();
            m_AllowsIntersectionID = Reader.ReadUInt16();

            Reader.ReadBytes(4); //Unknown.

            m_PrepareFoodID = Reader.ReadUInt16();
            m_CookFoodID = Reader.ReadUInt16();
            m_PlaceOnSurfaceID = Reader.ReadUInt16();
            m_DisposeID = Reader.ReadUInt16();
            m_EatFoodID = Reader.ReadUInt16();
            m_PickupFromSlotID = Reader.ReadUInt16();
            m_WashDishID = Reader.ReadUInt16();
            m_EatingSurfaceID = Reader.ReadUInt16();
            m_SitID = Reader.ReadUInt16();
            m_StandID = Reader.ReadUInt16();
            m_SalePrice = Reader.ReadUInt16();
            m_InitialDepreciation = Reader.ReadUInt16();
            m_DailyDepreciation = Reader.ReadUInt16();
            m_SelfDepreciating = Reader.ReadUInt16();
            m_DepreciationLimit = Reader.ReadUInt16();
        }

        public OBJD(SerializationInfo Info, StreamingContext Context)
        {
            m_Version = (int)Info.GetValue("Version", typeof(int));
            m_InitStackSize = (ushort)Info.GetValue("InitStackSize", typeof(ushort));
            m_BaseGraphicID = (ushort)Info.GetValue("BaseGraphicID", typeof(ushort));
            m_NumGraphics = (ushort)Info.GetValue("NumGraphics", typeof(ushort));
            m_MainFuncID = (ushort)Info.GetValue("MainFuncID", typeof(ushort));
            m_GardeningFuncID = (ushort)Info.GetValue("GardeningFuncID", typeof(ushort));
            m_TreeTableID = (ushort)Info.GetValue("TreeTableID", typeof(ushort));
            m_InteractionGroup = (ushort)Info.GetValue("InteractionGroup", typeof(ushort));
            m_ObjectType = (ObjectType)Info.GetValue("ObjectType", typeof(ObjectType));
            m_MasterID = (ushort)Info.GetValue("MasterID", typeof(ushort));
            IsMaster = (bool)Info.GetValue("IsMaster", typeof(bool));
            IsMultiTile = (bool)Info.GetValue("IsMultiTile", typeof(bool));
            m_WashHandsID = (ushort)Info.GetValue("WashHandsID", typeof(ushort));
            m_AnimTableID = (ushort)Info.GetValue("AnimTableID", typeof(ushort));
            m_GUID = (uint)Info.GetValue("GUID", typeof(uint));
            m_Disabled = (ushort)Info.GetValue("Disabled", typeof(ushort));
            m_Portal = (ushort)Info.GetValue("Portal", typeof(ushort));
            m_Price = (ushort)Info.GetValue("Price", typeof(ushort));
            m_BodyStringsID = (ushort)Info.GetValue("BodyStringsID", typeof(ushort));
            m_SLOTID = (ushort)Info.GetValue("SLOTID", typeof(ushort));
            m_AllowsIntersectionID = (ushort)Info.GetValue("AllowsIntersection", typeof(ushort));
            m_PrepareFoodID = (ushort)Info.GetValue("PrepareFoodID", typeof(ushort));
            m_CookFoodID = (ushort)Info.GetValue("CookFoodID", typeof(ushort));
            m_PlaceOnSurfaceID = (ushort)Info.GetValue("PlaceOnSurfaceID", typeof(ushort));
            m_DisposeID = (ushort)Info.GetValue("DisposeID", typeof(ushort));
            m_EatFoodID = (ushort)Info.GetValue("EatFoodID", typeof(ushort));
            m_PickupFromSlotID = (ushort)Info.GetValue("PickupFromSlotID", typeof(ushort));
            m_WashDishID = (ushort)Info.GetValue("WashDishID", typeof(ushort));
            m_SitID = (ushort)Info.GetValue("SitID", typeof(ushort));
            m_StandID = (ushort)Info.GetValue("StandID", typeof(ushort));
            m_SalePrice = (ushort)Info.GetValue("SalePrice", typeof(ushort));
            m_InitialDepreciation = (ushort)Info.GetValue("InitialDepreciation", typeof(ushort));
            m_DailyDepreciation = (ushort)Info.GetValue("DailyDepreciation", typeof(ushort));
            m_SelfDepreciating = (ushort)Info.GetValue("SelfDepreciating", typeof(ushort));
            m_DepreciationLimit = (ushort)Info.GetValue("DepreciationLimit", typeof(ushort));
        }

        public void GetObjectData(SerializationInfo Info, StreamingContext Context)
        {
            Info.AddValue("Version", m_Version);
            Info.AddValue("InitStackSize", m_InitStackSize);
            Info.AddValue("BaseGraphicID", m_BaseGraphicID);
            Info.AddValue("NumGraphics", m_NumGraphics);
            Info.AddValue("MainFuncID", m_MainFuncID);
            Info.AddValue("GardeningFuncID", m_GardeningFuncID);
            Info.AddValue("TreeTableID", m_TreeTableID);
            Info.AddValue("InteractionGroup", m_InteractionGroup);
            Info.AddValue("ObjectType", m_ObjectType);
            Info.AddValue("MasterID", m_MasterID);
            Info.AddValue("IsMaster", IsMaster);
            Info.AddValue("IsMultiTile", IsMultiTile);
            Info.AddValue("WashHandsID", m_WashHandsID);
            Info.AddValue("AnimTableID", m_AnimTableID);
            Info.AddValue("GUID", m_GUID);
            Info.AddValue("Disabled", m_Disabled);
            Info.AddValue("Portal", m_Portal);
            Info.AddValue("Price", m_Price);
            Info.AddValue("BodyStringsID", m_BodyStringsID);
            Info.AddValue("SLOTID", m_SLOTID);
            Info.AddValue("AllowsIntersection", m_AllowsIntersectionID);
            Info.AddValue("PrepareFoodID", m_PrepareFoodID);
            Info.AddValue("CookFoodID", m_CookFoodID);
            Info.AddValue("PlaceOnSurfaceID", m_PlaceOnSurfaceID);
            Info.AddValue("DisposeID", m_DisposeID);
            Info.AddValue("EatFoodID", m_EatFoodID);
            Info.AddValue("PickupFromSlotID", m_PickupFromSlotID);
            Info.AddValue("WashDishID", m_WashDishID);
            Info.AddValue("SitID", m_SitID);
            Info.AddValue("StandID", m_StandID);
            Info.AddValue("SalePrice", m_SalePrice);
            Info.AddValue("InitialDepreciation", m_InitialDepreciation);
            Info.AddValue("DailyDepreciation", m_DailyDepreciation);
            Info.AddValue("SelfDepreciating", m_SelfDepreciating);
            Info.AddValue("DepreciationLimit", m_DepreciationLimit);
        }
    }
}
