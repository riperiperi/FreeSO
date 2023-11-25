using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Common.Rendering.Emoji
{
    public class EmojiDictionary
    {
        public Dictionary<string, string> NameToEmojis = new Dictionary<string, string>();
        public Dictionary<string, List<string>> KeywordToCandidates = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> CandidatesToKeywords = new Dictionary<string, List<string>>();

        public EmojiDictionary()
        {
            JObject emojis;
            using (var emojiDict = new StreamReader(
                new FileStream(Path.Combine(FSOEnvironment.ContentDir, "UI/emojis.json"), FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                emojis = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(emojiDict.ReadToEnd());
            }

            foreach (var token in emojis)
            {
                var charValue = token.Value.Value<string>("char");
                var twemojiID = ToCodePoint(charValue);
                NameToEmojis[token.Key] = twemojiID;
                var keys = token.Value.Value<JArray>("keywords");
                foreach (var key in keys)
                    AddKeyword(key.Value<string>(), token.Key);
            }

            using (var emojiDict = new StreamReader(
                new FileStream(Path.Combine(FSOEnvironment.ContentDir, "UI/customemojis.json"), FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                emojis = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(emojiDict.ReadToEnd());
            }

            foreach (var token in emojis)
            {
                NameToEmojis[token.Key] = "!" + token.Value.Value<string>("url");
                var keys = token.Value.Value<JArray>("keywords");
                foreach (var key in keys)
                    AddKeyword(key.Value<string>(), token.Key);
            }
        }

        public void AddKeyword(string keyword, string candidate)
        {
            List<string> cand;
            if (!KeywordToCandidates.TryGetValue(keyword, out cand))
            {
                cand = new List<string>();
                KeywordToCandidates[keyword] = cand;
            }
            cand.Add(candidate);

            if (!CandidatesToKeywords.TryGetValue(candidate, out cand))
            {
                cand = new List<string>();
                CandidatesToKeywords[candidate] = cand;
            }
            cand.Add(keyword);
        }

        public string ToCodePoint(string str)
        {
            var cs = str.ToCharArray();
            var i = 0;
            var c = 0;
            var p = 0;
            var r = new List<string>();
            var zeroWidth = str.Any(x => x == '\x200D');
            while (i < cs.Length)
            {
                c = cs[i++];
                if (c == 0xfe0f && !zeroWidth) continue; //"as image", just ignore this
                if (p > 0)
                {
                    r.Add((0x10000 + ((p - 0xD800) << 10) + (c - 0xDC00)).ToString("x"));
                    p = 0;
                }
                else if (0xD800 <= c && c <= 0xDBFF)
                {
                    p = c;
                }
                else
                {
                    r.Add(c.ToString("x"));
                }
            }
            return string.Join("-", r);
        }
    }
}
