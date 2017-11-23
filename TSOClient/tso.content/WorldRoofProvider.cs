﻿using FSO.Content.Codecs;
using FSO.Content.Framework;
using FSO.Content.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSO.Content
{
    public class WorldRoofProvider : FileProvider<ITextureRef>
    {
        public WorldRoofProvider(Content contentManager) : base(contentManager, new TextureCodec(), 
            new Regex(contentManager.TS1? "GameData/Roofs/.*\\.bmp" : "housedata/roofs/.*\\.jpg"))
        {
            UseTS1 = contentManager.TS1;
        }

        public int Count
        {
            get
            {
                return Items.Count;
            }
        }

        public string IDToName(int id)
        {
            return Items[id].Name;
        }
    }
}
