using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FSO.Common.Rendering.Emoji
{
    public class EmojiProvider
    {
        public EmojiDictionary Dict;
        public EmojiCache Cache;
        public Random DictRand = new Random();

        public Dictionary<string, string> TranslateShortcuts = new Dictionary<string, string>()
        {
            { "i", "eye" },
            { "you", "point_right" },
            { "your", "point_right" },
            { "you're", "point_right" },
            { "me", "relieved: :point_left" },
            { "i'm", "relieved: :point_left" },
            { "us", "couple" },
            { "we", "couple" },
            { "our", "couple" },
            { "what", "woman_shrugging" },
            { "be", "b" },
            { "to", "two" },
            { "and", "symbols" },
            { "that", "point_up" },
            { "for", "four" },
            { "not", "exclamation" },
            { "this", "point_up" },
            { "but", "exclamation" },
            { "his", "man" },
            { "her", "woman" },
            { "him", "man" },
            { "he", "man" },
            { "she", "woman" },
            { "from", "postbox" },
            { "they", "family_man_woman_girl_boy" },
            { "them", "family_man_woman_girl_boy" },
            { "or", "thinking" },
            { "an", "a" },
            { "will", "thinking" },
            { "my", "relieved: :point_left" },
            { "all", "rainbow_flag" },
            { "would", "tree" },
            { "so", "woman_shrugging" },
            { "out", "outbox_tray" },
            { "if", "thinking" },
            { "about", "arrows_counterclockwise" },
            { "who", "thinking: :family_man_woman_girl_boy" },
            { "get", "gift" },
            { "which", "woman_shrugging" },
            { "go", "door" },
            { "when", "watch" },
            { "make", "toolbox" },
            { "know", "brain" },
            { "take", "takeout_box" },
            { "into", "arrow_heading_down" },
            { "year", "calendar" },
            { "because", "woman_shrugging" },
            { "hmm", "thinking" },
            { "yo", "wave" },
            { "hey", "wave" },
            { "sup", "wave" },
        };

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

        private string StripPunctuation(string word, out string punctuation)
        {
            for (int i=word.Length-1; i>=0; i--)
            {
                if (!char.IsPunctuation(word[i]))
                {
                    punctuation = word.Substring(i + 1);
                    return word.Substring(0, i + 1);
                }
            }
            punctuation = "";
            return word;
        }

        public string SearchForWordHit(string word)
        {
            string direct;
            if (TranslateShortcuts.TryGetValue(word, out direct))
            {
                return ":" + direct + ":";
            }
            
            if (Dict.NameToEmojis.TryGetValue(word, out direct))
            {
                return ":" + word + ":";
            }

            List<string> options;
            if (Dict.KeywordToCandidates.TryGetValue(word, out options))
            {
                return ":" + options[DictRand.Next(options.Count)] + ":";
            }
            return null;
        }

        public string TranslateWordToEmoji(string word)
        {
            string punctuation;
            var lower = StripPunctuation(word.ToLowerInvariant(), out punctuation);
            string result;
            result = SearchForWordHit(lower);
            if (result != null) return result + ' ' + punctuation;
            if (lower.EndsWith("s"))
            {
                result = SearchForWordHit(lower.Substring(0, lower.Length-1));
                if (result != null) return result + ' ' + punctuation;
            }
            if (lower.EndsWith("ing"))
            {
                result = SearchForWordHit(lower.Substring(0, lower.Length - 3));
                if (result != null) return result + ' ' + punctuation;
            }
            return word.Substring(0, lower.Length) + punctuation;
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

        public string EmojiTranslate(string input)
        {
            //search for replacement candidates for each word in input
            var words = input.Split(' ');

            for (int i=0; i<words.Length; i++)
            {
                var word = words[i];
                if (word == "") continue;
                if (word.Length > 2 && word.StartsWith(":") && word.EndsWith(":"))
                {
                    //is this already an emoji? if so, skip it
                    var existing = EmojiFromName(word.Substring(1, word.Length-2));
                    if (existing == null) continue;
                }
                words[i] = TranslateWordToEmoji(word);
            }
            return String.Join(" ", words);
        }

        public string EmojiOnly(string input, int mode)
        {
            if (mode == 2) return EmojiTranslate(input);
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
