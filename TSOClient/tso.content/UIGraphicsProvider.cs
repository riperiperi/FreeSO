using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using FSO.Content.Framework;
using FSO.Content.Codecs;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;
using FSO.Content.Model;
using System.IO;
using FSO.Files;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to UI texture (*.bmp) data in FAR3 archives.
    /// </summary>
    public class UIGraphicsProvider : FAR3Provider<ITextureRef>
    {
        public static uint[] MASK_COLORS = new uint[]{
            new Color(0xFF, 0x00, 0xFF, 0xFF).PackedValue,
            new Color(0xFE, 0x02, 0xFE, 0xFF).PackedValue,
            new Color(0xFF, 0x01, 0xFF, 0xFF).PackedValue
        };

        private Dictionary<ulong, string> Files = new Dictionary<ulong, string>();
        private Dictionary<ulong, ITextureRef> FilesCache = new Dictionary<ulong, ITextureRef>();

        //For some reason, the rack eod has a graphic id that we don't, but the file does exist under another iD.
        //Can't see any problem with file parser so putting in a mapping for now
        private Dictionary<ulong, ulong> Pointers = new Dictionary<ulong, ulong>();

        public UIGraphicsProvider(Content contentManager)
            : base(contentManager, new TextureCodec(MASK_COLORS), new Regex("uigraphics/.*\\.dat"))
        {
            Files[0x00000Cb800000002] = "uigraphics/friendshipweb/friendshipwebalpha.tga";
            Files[0x00000Cbfb00000001] = "uigraphics/hints/hint_mechanicskill.bmp";

            Files[0x1AF0856DDBAC] = "uigraphics/chat/balloonpointersadbottom.bmp";
            Files[0x1B00856DDBAC] = "uigraphics/chat/balloonpointersadside.bmp";
            Files[0x1B10856DDBAC] = "uigraphics/chat/balloontilessad.bmp";

            Files[0x1972454856DDBAC] = "uigraphics/friendshipweb/f_web_inbtn.bmp";
            Files[0x3D3AEF0856DDBAC] = "uigraphics/friendshipweb/f_web_outbtn.bmp";
            //./uigraphics/eods/costumetrunk/eod_costumetrunkbodySkinBtn.bmp
            Pointers[0x0000028800000001] = 0x0000094600000001;

            
        }

        public static string ReplacementImportDir = "D:/Stuff/waifu/UIScaled/";

        public void ExportAll(GraphicsDevice gd)
        {
            var replacementExportDir = "D:/Stuff/waifu/UI/";
            Directory.CreateDirectory(replacementExportDir);

            foreach (var item in List())
            {
                var texr = item.Get();
                var img = texr.GetImage();
                using (var stream = File.Open(replacementExportDir + ((Far3ProviderEntry<ITextureRef>)item).ID.ToString("x16") + ".png", FileMode.Create))
                    ImageLoaderHelpers.SavePNGFunc(img.Data, img.Width, img.Height, stream);
            }
        }

        protected override ITextureRef ResolveById(ulong id)
        {
            if (Pointers.ContainsKey(id)){
                id = Pointers[id];
            }
            if (Files.ContainsKey(id))
            {
                //Non far3 file
                if (FilesCache.ContainsKey(id)) { return FilesCache[id]; }
                var path = this.ContentManager.GetPath(Files[id]);
                using (var stream = File.OpenRead(path)) {
                    FilesCache.Add(id, Codec.Decode(stream));
                    return FilesCache[id];
                }
            }
            var result = base.ResolveById(id);
            /*
            if (result.ReplacePath == null)
            {
                if (File.Exists(ReplacementImportDir + id.ToString("x16") + "_[NS-L3][x2.000000].png"))
                {
                    result.ReplacePath = ReplacementImportDir + id.ToString("x16") + "_[NS-L3][x2.000000].png";
                }
            }
            */
            return result;
        }
    }
}