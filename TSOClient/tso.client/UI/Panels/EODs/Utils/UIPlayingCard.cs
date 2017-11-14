using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;

namespace FSO.Client.UI.Panels.EODs.Utils
{
    public class UIPlayingCard
    {
        /*
         * full card textures have filenames starting with t*** .bmp - e.g. (./uigraphics/eods/casinoshared/cards/tjc.bmp) tback.bmp
         */
        public static UInt64 GetFullCardAssetID(string value, string suit)
        {
            return GetFullCardAssetID(value + "_" + suit);
        }
        public static UInt64 GetFullCardAssetID(string valueAndSuit)
        {
            FullPlayCardAssets cardAsset;
            if (Enum.TryParse<FullPlayCardAssets>(valueAndSuit, true, out cardAsset))
                return (ulong)cardAsset;
            else
                return 0;
        }
        public static UIImage GetFullCardImage(string value, string suit)
        {
            return GetFullCardImage(value + "_" + suit);
        }
        public static UIImage GetFullCardImage(string valueAndSuit)
        {
            UInt64 assetID = GetFullCardAssetID(valueAndSuit);
            if (assetID != 0)
                return new UIImage(UIElement.GetTexture(assetID));
            else
                return null;
        }
        /*
         * partial card textures have filenames starting with b*** .bmp - e.g. (./uigraphics/eods/casinoshared/cards/b6c.bmp) bback.bmp
         */
        public static UInt64 GetPartialCardAssetID(string value, string suit)
        {
            return GetPartialCardAssetID(value + "_" + suit);
        }
        public static UInt64 GetPartialCardAssetID(string valueAndSuit)
        {
            PartialPlayCardAssets cardAsset;
            if (Enum.TryParse<PartialPlayCardAssets>(valueAndSuit, true, out cardAsset))
                return (ulong)cardAsset;
            else
                return 0;
        }
        public static UIImage GetPartialCardImage(string value, string suit)
        {
            return GetPartialCardImage(value + "_" + suit);
        }
        public static UIImage GetPartialCardImage(string valueAndSuit)
        {
            UInt64 assetID = GetPartialCardAssetID(valueAndSuit);
            if (assetID != 0)
                return new UIImage(UIElement.GetTexture(assetID));
            else
                return null;
        }
        /*
         * Card names using the string table, allowing for proper translation to other languages
         */
        public static string GetFullCardName(UInt64 cardAsset)
        {
            if (Enum.IsDefined(typeof(FullPlayCardAssets), cardAsset))
                return GetCardName(Enum.GetName(typeof(FullPlayCardAssets), cardAsset));
            return "";
        }
        public static string GetPartialCardName(UInt64 cardAsset)
        {
            if (Enum.IsDefined(typeof(PartialPlayCardAssets), cardAsset))
                return GetCardName(Enum.GetName(typeof(PartialPlayCardAssets), cardAsset));
            return "";
        }
        public static string GetCardName(string value, string suit)
        {
            return GetCardName(value + "_" + suit);
        }
        public static string GetCardName(string valueUnderscoreSuit)
        {
            string cardName = "";
            if (valueUnderscoreSuit != null)
            {
                var split = valueUnderscoreSuit.Split('_');
                if (split.Length > 1)
                {
                    int valueStringIndex = GetStringIndexOfCardValue(split[0]); // e.g. "Ace" or "Ten" => 1 or 10
                    int suitStringIndex = GetStringIndexOfSuitValue(split[1]); // e.g. "Diamonds" or "Spades" => 18 or 15
                    if (valueStringIndex < 200 && suitStringIndex < 200) // "[value] of [suit]" e.g. "Queen of Spades"
                        cardName = GameFacade.Strings["UIText", "262", "" + valueStringIndex] + GameFacade.Strings["UIText", "262", "14"]
                            + GameFacade.Strings["UIText", "262", "" + suitStringIndex];
                }
            }
            return cardName;
        }
        public static int GetStringIndexOfCardValue(string cardValueString)
        {
            PlayingCardValueStringIndexes value;
            if (Enum.TryParse<PlayingCardValueStringIndexes>(cardValueString, out value))
                return (int)value;
            return 200; // index too high for string table
        }
        public static int GetStringIndexOfSuitValue(string suitValueString)
        {
            PlayingCardSuitStringIndexes suit;
            if (Enum.TryParse<PlayingCardSuitStringIndexes>(suitValueString, out suit))
                return (int)suit;
            return 200; // index too high for string table
        }
    }
    [Flags]
    public enum PlayingCardValueStringIndexes : int
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Joker = 19 // no joker AssetID exists, a custom texture would need to be made for partial and full
    }
    [Flags]
    public enum PlayingCardSuitStringIndexes : int
    {
        Spades = 15,
        Hearts = 16,
        Clubs = 17,
        Diamonds = 18
    }
    [Flags]
    public enum PartialPlayCardAssets : UInt64
    {
        Ace_Clubs = 0x00000C4B00000001,
        Ace_Diamonds = 0x00000C4C00000001,
        Ace_Hearts = 0x00000C4D00000001,
        Ace_Spades = 0x00000C4E00000001,
        Two_Clubs = 0x00000C2B00000001,
        Two_Diamonds = 0x00000C2C00000001,
        Two_Hearts = 0x00000C2D00000001,
        Two_Spades = 0x00000C2E00000001,
        Three_Clubs = 0x00000C2F00000001,
        Three_Diamonds = 0x00000C3000000001,
        Three_Hearts = 0x00000C3100000001,
        Three_Spades = 0x00000C3200000001,
        Four_Clubs = 0x00000C3300000001,
        Four_Diamonds = 0x00000C3400000001,
        Four_Hearts = 0x00000C3500000001,
        Four_Spades = 0x00000C3600000001,
        Five_Clubs = 0x00000C3700000001,
        Five_Diamonds = 0x00000C3800000001,
        Five_Hearts = 0x00000C3900000001,
        Five_Spades = 0x00000C3A00000001,
        Six_Clubs = 0x00000C3B00000001,
        Six_Diamonds = 0x00000C3C00000001,
        Six_Hearts = 0x00000C3D00000001,
        Six_Spades = 0x00000C3E00000001,
        Seven_Clubs = 0x00000C3F00000001,
        Seven_Diamonds = 0x00000C4000000001,
        Seven_Hearts = 0x00000C4100000001,
        Seven_Spades = 0x00000C4200000001,
        Eight_Clubs = 0x00000C4300000001,
        Eight_Diamonds = 0x00000C4400000001,
        Eight_Hearts = 0x00000C4500000001,
        Eight_Spades = 0x00000C4600000001,
        Nine_Clubs = 0x00000C4700000001,
        Nine_Diamonds = 0x00000C4800000001,
        Nine_Hearts = 0x00000C4900000001,
        Nine_Spades = 0x00000C4A00000001,
        Ten_Clubs = 0x00000C2700000001,
        Ten_Diamonds = 0x00000C2800000001,
        Ten_Hearts = 0x00000C2900000001,
        Ten_Spades = 0x00000C2A00000001,
        Jack_Clubs = 0x00000C5000000001,
        Jack_Diamonds = 0x00000C5100000001,
        Jack_Hearts = 0x00000C5200000001,
        Jack_Spades = 0x00000C5300000001,
        Queen_Clubs = 0x00000C5800000001,
        Queen_Diamonds = 0x00000C5900000001,
        Queen_Hearts = 0x00000C5A00000001,
        Queen_Spades = 0x00000C5B00000001,
        King_Clubs = 0x00000C5400000001,
        King_Diamonds = 0x00000C5500000001,
        King_Hearts = 0x00000C5600000001,
        King_Spades = 0x00000C5700000001,
        Back = 0x00000C4F00000001
    }
    [Flags]
    public enum FullPlayCardAssets : UInt64
    {
        Ace_Clubs = 0x00000C8000000001,
        Ace_Diamonds = 0x00000C8100000001,
        Ace_Hearts = 0x00000C8200000001,
        Ace_Spades = 0x00000C8300000001,
        Two_Clubs = 0x00000C6000000001,
        Two_Diamonds = 0x00000C6100000001,
        Two_Hearts = 0x00000C6200000001,
        Two_Spades = 0x00000C6300000001,
        Three_Clubs = 0x00000C6400000001,
        Three_Diamonds = 0x00000C6500000001,
        Three_Hearts = 0x00000C6600000001,
        Three_Spades = 0x00000C6700000001,
        Four_Clubs = 0x00000C6800000001,
        Four_Diamonds = 0x00000C6900000001,
        Four_Hearts = 0x00000C6A00000001,
        Four_Spades = 0x00000C6B00000001,
        Five_Clubs = 0x00000C6C00000001,
        Five_Diamonds = 0x00000C6D00000001,
        Five_Hearts = 0x00000C6E00000001,
        Five_Spades = 0x00000C6F00000001,
        Six_Clubs = 0x00000C7000000001,
        Six_Diamonds = 0x00000C7100000001,
        Six_Hearts = 0x00000C7200000001,
        Six_Spades = 0x00000C7300000001,
        Seven_Clubs = 0x00000C7400000001,
        Seven_Diamonds = 0x00000C7500000001,
        Seven_Hearts = 0x00000C7600000001,
        Seven_Spades = 0x00000C7700000001,
        Eight_Clubs = 0x00000C7800000001,
        Eight_Diamonds = 0x00000C7900000001,
        Eight_Hearts = 0x00000C7A00000001,
        Eight_Spades = 0x00000C7B00000001,
        Nine_Clubs = 0x00000C7C00000001,
        Nine_Diamonds = 0x00000C7D00000001,
        Nine_Hearts = 0x00000C7E00000001,
        Nine_Spades = 0x00000C7F00000001,
        Ten_Clubs = 0x00000C5C00000001,
        Ten_Diamonds = 0x00000C5D00000001,
        Ten_Hearts = 0x00000C5E00000001,
        Ten_Spades = 0x00000C5F00000001,
        Jack_Clubs = 0x00000C8500000001,
        Jack_Diamonds = 0x00000C8600000001,
        Jack_Hearts = 0x00000C8700000001,
        Jack_Spades = 0x00000C8800000001,
        Queen_Clubs = 0x00000C8D00000001,
        Queen_Diamonds = 0x00000C8E00000001,
        Queen_Hearts = 0x00000C8F00000001,
        Queen_Spades = 0x00000C9000000001,
        King_Clubs = 0x00000C8900000001,
        King_Diamonds = 0x00000C8A00000001,
        King_Hearts = 0x00000C8B00000001,
        King_Spades = 0x00000C8C00000001,
        Back = 0x00000C8400000001
    }
}
