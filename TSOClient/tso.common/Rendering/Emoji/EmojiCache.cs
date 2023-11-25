using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace FSO.Common.Rendering.Emoji
{
    public class EmojiCache
    {
        public string Source = "https://cdnjs.cloudflare.com/ajax/libs/twemoji/14.0.2/72x72/";
        public int DefaultRes = 24;
        public int Width = 32;
        
        public int NextIndex = 0;
        public List<string> Emojis = new List<string>();
        public Dictionary<string, int> EmojiToIndex = new Dictionary<string, int>();
        public RenderTarget2D EmojiTex;
        public SpriteBatch EmojiBatch;
        public HashSet<int> IncompleteSpaces = new HashSet<int>();
        public HashSet<int> ErrorSpaces = new HashSet<int>();
        private GraphicsDevice GD;
        private bool needClear = true;

        public EmojiCache(GraphicsDevice gd)
        {
            GD = gd;
            EmojiBatch = new SpriteBatch(gd);
            
            EmojiTex = new RenderTarget2D(gd, Width * DefaultRes, Width * DefaultRes, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
        }

        public void ExpandIfNeeded()
        {
            //todo
        }

        public Rectangle GetEmoji(string emojiID) {

            int index;
            if (EmojiToIndex.TryGetValue(emojiID, out index))
            {
                return RectForIndex(index);
            } else
            {
                index = NextIndex++;
                ExpandIfNeeded();
                lock (IncompleteSpaces) IncompleteSpaces.Add(index);
                var client = new WebClient();
                client.DownloadDataCompleted += (object sender, DownloadDataCompletedEventArgs e) =>
                {
                    if (e.Cancelled || e.Error != null || e.Result == null)
                    {
                        lock (ErrorSpaces) ErrorSpaces.Add(index);
                    } else
                    {
                        GameThread.NextUpdate(x =>
                        {
                            try
                            {
                                using (var mem = new MemoryStream(e.Result))
                                {
                                    var tex = Texture2D.FromStream(GD, mem);

                                    var decimated = TextureUtils.Decimate(tex, GD, 72 / DefaultRes, true);
                                    //blit this into our emoji buffer
                                    GD.SetRenderTarget(EmojiTex);
                                    if (needClear)
                                    {
                                        GD.Clear(Color.TransparentBlack);
                                        needClear = false;
                                    }
                                    EmojiBatch.Begin(blendState: BlendState.NonPremultiplied, sortMode: SpriteSortMode.Immediate);
                                    EmojiBatch.Draw(decimated, RectForIndex(index), Color.White);
                                    EmojiBatch.End();
                                    GD.SetRenderTarget(null);
                                }
                            }
                            catch (Exception)
                            {
                                lock (ErrorSpaces) ErrorSpaces.Add(index);
                            }
                        });
                    }
                    lock (IncompleteSpaces) IncompleteSpaces.Remove(index);
                };
                client.DownloadDataAsync(new Uri((emojiID[0] == '!')?(emojiID.Substring(1)):(Source + emojiID + ".png")));
                Emojis.Add(emojiID);
                EmojiToIndex[emojiID] = index;
                return RectForIndex(index);
            }
        }

        private Rectangle RectForIndex(int index)
        {
            return new Rectangle((index % Width) * DefaultRes, (index / Width) * DefaultRes, DefaultRes, DefaultRes);
        }
    }
}
