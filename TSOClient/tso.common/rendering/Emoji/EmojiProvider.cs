using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Rendering.Emoji
{
    public class EmojiProvider
    {
        public EmojiDictionary Dict;
        public EmojiCache Cache;

        public EmojiProvider(GraphicsDevice gd)
        {
            Dict = new EmojiDictionary();
            Cache = new EmojiCache(gd);
        }

        public string EmojiFromName(string name)
        {
            string result;
            if (Dict.NameToEmojis.TryGetValue(name, out result))
                return result;
            return null;
        }

        public Tuple<Texture2D, Rectangle> GetEmoji(string id)
        {
            var rect = Cache.GetEmoji(id);
            return new Tuple<Texture2D, Rectangle>(Cache.EmojiTex, rect);
        }

        public string EmojiToBB(string input)
        {
            //search through the string for emojis to turn to BBcode
            int index = 0;
            int lastColon = -1;
            var result = new StringBuilder();
            while (true)
            {
                var nColon = input.IndexOf(':', index);
                if (nColon == -1) break;
                if (lastColon == -1) result.Append(input.Substring(index, nColon - index)); //add up to the colon
                else
                {
                    //is the string between the two colons an emoji?
                    var emoji = EmojiFromName(input.Substring(lastColon + 1, nColon - (lastColon + 1)));
                    if (emoji == null)
                    {
                        result.Append(":"+input.Substring(index, nColon - index)); //add up to the colon (include the last colon we skipped)
                    } else
                    {
                        result.Append("[emoji=" + emoji + "]     ");
                        lastColon = -1;
                        index = nColon + 1;
                        continue;
                    }
                }
                index = nColon + 1;
                lastColon = nColon;
            }
            result.Append(((lastColon == -1) ? "" : ":") + input.Substring(index));
            return result.ToString();
        }

        public string EmojiOnly(string input)
        {
            //search through the string for emojis to keep
            int index = 0;
            int lastColon = -1;
            var result = new StringBuilder();
            while (true)
            {
                var nColon = input.IndexOf(':', index);
                if (nColon == -1) break;
                else
                {
                    //is the string between the two colons an emoji?
                    var name = input.Substring(lastColon + 1, nColon - (lastColon + 1));
                    var emoji = EmojiFromName(name);
                    if (emoji == null)
                    {
                    }
                    else
                    {
                        result.Append(":" + name + ": ");
                        lastColon = -1;
                        index = nColon + 1;
                        continue;
                    }
                }
                index = nColon + 1;
                lastColon = nColon;
            }
            //result.Append(((lastColon == -1) ? "" : ":"));
            return result.ToString();
        }
    }
}
