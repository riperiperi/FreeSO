using System.Linq;
using System.IO;
using FSO.Common.Content;
using FSO.Files.Utils;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.Vitaboy
{
    /// <summary>
    /// Outfits collect together the light-, medium-, and dark-skinned versions of an 
    /// appearance and associate them collectively with a hand group and a body region (head or body).
    /// </summary>
    public class Outfit 
    {
        public uint LightAppearanceFileID;
        public uint LightAppearanceTypeID;

        public uint MediumAppearanceFileID;
        public uint MediumAppearanceTypeID;

        public uint DarkAppearanceFileID;
        public uint DarkAppearanceTypeID;

        public uint HandGroup;
        public uint Region;

        public string TS1AppearanceID;
        public string TS1TextureID;
        public HandGroup LiteralHandgroup;


        /// <summary>
        /// Gets the ContentID for the appearances referenced by this Outfit.
        /// </summary>
        /// <param name="type">The type of appearance to get.</param>
        /// <returns>A ContentID instance.</returns>
        public ContentID GetAppearance(AppearanceType type)
        {
            if (TS1AppearanceID != null) return new ContentID(TS1AppearanceID);
            switch (type)
            {
                case AppearanceType.Light:
                    return new ContentID(LightAppearanceTypeID, LightAppearanceFileID);
                case AppearanceType.Medium:
                    return new ContentID(MediumAppearanceTypeID, MediumAppearanceFileID);
                case AppearanceType.Dark:
                    return new ContentID(DarkAppearanceTypeID, DarkAppearanceFileID);
            }

            return null;
        }

        /// <summary>
        /// Gets the ContentID for the Handgroup referenced by this Outfit.
        /// </summary>
        /// <returns>A ContentID instance.</returns>
        public ContentID GetHandgroup()
        {
            return new ContentID((uint)18, HandGroup);
        }

        /// <summary>
        /// Reads an Outfit from the supplied Stream.
        /// </summary>
        /// <param name="stream">A Stream instance.</param>
        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream))
            {
                var version = io.ReadUInt32();
                var unknown = io.ReadUInt32();

                LightAppearanceFileID = io.ReadUInt32();
                LightAppearanceTypeID = io.ReadUInt32();

                MediumAppearanceFileID = io.ReadUInt32();
                MediumAppearanceTypeID = io.ReadUInt32();

                DarkAppearanceFileID = io.ReadUInt32();
                DarkAppearanceTypeID = io.ReadUInt32();

                HandGroup = io.ReadUInt32();
                Region = io.ReadUInt32();
            }
        }

        public void ReadHead(string dat)
        {
            TS1AppearanceID = ToApr(dat);
            TS1TextureID = ToTex(dat);
        }

        public void Read(STR bodyStrings)
        {
            var bodies = bodyStrings.GetString(1).Split(';');
            TS1AppearanceID = ToApr(bodies.FirstOrDefault());
            TS1TextureID = ToTex(bodies.FirstOrDefault());

            LiteralHandgroup = new HandGroup()
            {
                TS1HandSet = true,
                LightSkin = new HandSet()
                {
                    LeftHand = new Hand()
                    {
                        Idle = new Gesture() { Name = ToApr(bodyStrings.GetString(17)), TexName = ToTex(bodyStrings.GetString(17)) },
                        Pointing = new Gesture() { Name = ToApr(bodyStrings.GetString(19)), TexName = ToTex(bodyStrings.GetString(19)) },
                        Fist = new Gesture() { Name = ToApr(bodyStrings.GetString(21)), TexName = ToTex(bodyStrings.GetString(21)) }
                    },
                    RightHand = new Hand()
                    {
                        Idle = new Gesture() { Name = ToApr(bodyStrings.GetString(18)), TexName = ToTex(bodyStrings.GetString(18)) },
                        Pointing = new Gesture() { Name = ToApr(bodyStrings.GetString(20)), TexName = ToTex(bodyStrings.GetString(20)) },
                        Fist = new Gesture() { Name = ToApr(bodyStrings.GetString(22)), TexName = ToTex(bodyStrings.GetString(22)) }
                    }
                }
            };
        }

        public void Read(string dat)
        {
            var bodies = dat.Split(';');
            TS1AppearanceID = ToApr(bodies.FirstOrDefault());
            TS1TextureID = ToTex(bodies.FirstOrDefault());

            //right now handgroup comes from the sim. in future might want to search for this.
        }

        private string ToApr(string input)
        {
            return new string(input.TakeWhile(x => x != ',').ToArray()) + ".apr";
        }

        private string ToTex(string input)
        {
            //assume there is one texture bound to the appearance
            var eqIdx = input.IndexOf('=');
            if (eqIdx == -1) return null;
            return input.Substring(eqIdx + 1);
        }
    }
}
