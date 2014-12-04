using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using ProtocolAbstractionLibraryD;
using SimsLib.FAR3;
using TSO.Files.formats;

namespace PDChat.Sims
{
    public class Sim
    {
        public string Name = "";
        public string Sex = "";
        public string Description = "";
        public string Timestamp = "";
        public int CharacterID = 0;
        public Guid GUID = Guid.NewGuid();
        public CityInfo ResidingCity;

        public AppearanceType Appearance;
        private ulong m_HeadOutfitID = 0;
        private ulong m_BodyOutfitID = 0;

        private Bitmap m_Thumbnail;

        /// <summary>
        /// The ID of the head's outfit. Used by the network protocol.
        /// </summary>
        public ulong HeadOutfitID
        {
            get { return m_HeadOutfitID; }
            set { m_HeadOutfitID = value; }
        }

        /// <summary>
        /// The ID of the body's Outfit. Used by the network protocol.
        /// </summary>
        public ulong BodyOutfitID
        {
            get { return m_BodyOutfitID; }
            set { m_BodyOutfitID = value; }
        }

        /// <summary>
        /// Returns this Sim's thumbnail image.
        /// If the HeadOutfitID isn't set, an empty Bitmap instance
        /// will be returned.
        /// </summary>
        public Bitmap Thumbnail
        {
            get
            {
                if (m_HeadOutfitID != 0)
                {
                    if (m_Thumbnail == null)
                    {
                        m_Thumbnail = GetThumbnail();
                        return m_Thumbnail;
                    }
                    else
                        return m_Thumbnail;
                }
                else
                    return new Bitmap(1, 1);
            }
        }

        /// <summary>
        /// Gets a sim's thumbnail image.
        /// </summary>
        /// <returns></returns>
        private Bitmap GetThumbnail()
        {
            Outfit Oft = new Outfit();
            Appearance Apr = new Appearance();
            Bitmap Thumbnail = new Bitmap(1, 1);

            if (!File.Exists(GlobalSettings.Default.ClientPath + "avatardata\\heads\\outfits\\outfits.dat"))
            {
                Debug.WriteLine("WARNING: Couldn't find: " + GlobalSettings.Default.ClientPath +
                "avatardata\\heads\\outfits\\outfits.dat");

                return Thumbnail;
            }

            FAR3Archive Archive = new FAR3Archive(GlobalSettings.Default.ClientPath + 
                "avatardata\\heads\\outfits\\outfits.dat");
            Oft.Read(new MemoryStream(Archive.GetItemByID(HeadOutfitID)));

            Archive = new FAR3Archive(GlobalSettings.Default.ClientPath +
                "avatardata\\heads\\appearances\\appearances.dat");
            TSO.Common.content.ContentID ApprID;

            switch (Appearance)
            {
                case AppearanceType.Light:
                    ApprID = Oft.GetAppearance(AppearanceType.Light);
                    Apr.Read(new MemoryStream(Archive.GetItemByID(ApprID.Shift())));

                    Archive = new FAR3Archive(GlobalSettings.Default.ClientPath + 
                        "avatardata\\heads\\thumbnails\\thumbnails.dat");
                    Thumbnail = new Bitmap(new MemoryStream(Archive.GetItemByID(Apr.ThumbnailID.Shift())));
                    break;
                case AppearanceType.Medium:
                    ApprID = Oft.GetAppearance(AppearanceType.Medium);
                    Apr.Read(new MemoryStream(Archive.GetItemByID(ApprID.Shift())));

                    Archive = new FAR3Archive(GlobalSettings.Default.ClientPath + 
                        "avatardata\\heads\\thumbnails\\thumbnails.dat");
                    Thumbnail = new Bitmap(new MemoryStream(Archive.GetItemByID(Apr.ThumbnailID.Shift())));
                    break;
                case AppearanceType.Dark:
                    ApprID = Oft.GetAppearance(AppearanceType.Dark);
                    Apr.Read(new MemoryStream(Archive.GetItemByID(ApprID.Shift())));

                    Archive = new FAR3Archive(GlobalSettings.Default.ClientPath + 
                        "avatardata\\heads\\thumbnails\\thumbnails.dat");
                    Thumbnail = new Bitmap(new MemoryStream(Archive.GetItemByID(Apr.ThumbnailID.Shift())));
                    break;
            }

            return Thumbnail;
        }

        public Sim(string GUID)
        {
            this.GUID = new Guid(GUID);
        }
    }
}
