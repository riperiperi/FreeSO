using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Common.Rendering.Framework.Model;
using FSO.Client.UI.Controls;
using FSO.Common.Utils;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering.Framework.IO;

namespace FSO.Client.UI.Panels
{
    public class UIEmojiSuggestions : UIContainer
    {
        public UITextEdit Watching;
        public string LastSearch;

        public UIEmojiSuggestions(UITextEdit parent)
        {
            Watching = parent;
            Watching.OnEnterPress += Watching_OnEnterPress;
        }

        private void Watching_OnEnterPress(UIElement element)
        {
            if (SelectBestTerm()) Watching.EventSuppressed = true;
        }

        public override void Update(UpdateState state)
        {
            Position = Watching.Position + new Vector2(0, Watching.Size.Y);
            base.Update(state);
            
            //
            var showSuggestions = (state.InputManager.GetFocus() == Watching);
            if (showSuggestions)
            {
                showSuggestions = false;
                var lastWord = Watching.CurrentText.Substring(0, Watching.GetSelectedInd()).Split(null).LastOrDefault();
                if (lastWord != null && lastWord.Count (x => x == ':') % 2 == 1)
                {
                    //we're eligible for suggestions if there are an odd number of colons
                    var colonInd = lastWord.LastIndexOf(':');
                    var search = lastWord.Substring(colonInd + 1);
                    showSuggestions = true;
                    if (search != LastSearch)
                    {
                        PopulateSearch(search);
                    }
                }
            }
            if (showSuggestions)
            {
                if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Right))
                {
                    if (SelectBestTerm()) showSuggestions = false;
                }
                if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Down))
                    MoveSelection(1);
                if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Up))
                    MoveSelection(-1);
            }
            if (!showSuggestions)
            {
                //remove children if no suggestions
                var childCopy = new List<UIElement>(Children);
                foreach (var child in childCopy) Remove(child);
            }
        }

        public void MoveSelection(int off)
        {
            var i = Children.FindIndex(x => ((UIEmojiSuggestion)x).Selected);
            var ni = i + off;
            if (ni >= Children.Count || ni < 0) return;
            ((UIEmojiSuggestion)Children[i]).Selected = false;
            ((UIEmojiSuggestion)Children[ni]).Selected = true;
        }

        private int Sqr(int i)
        {
            return i * i;
        }

        public void PopulateSearch(string term)
        {
            var dict = GameFacade.Emojis.Dict;
            var childCopy = new List<UIElement>(Children);
            foreach (var child in childCopy) Remove(child);
            var termChars = term.ToCharArray();

            var scored = dict.CandidatesToKeywords.Select(x =>new Tuple<int, string>(
                (int)x.Value.Max(y => Sqr(LCS(termChars, y.ToCharArray())))
                + Sqr(LCS(termChars, x.Key.ToCharArray()))
                + (x.Key.StartsWith(term) ? (Sqr(termChars.Length + ((x.Key == term) ? 1 : 0)) * 2) : 0),
                x.Key
                )).OrderByDescending(x => x.Item1);

            var best4 = scored.Take(4).ToArray();
            var bestScore = best4.First().Item1;
            UIEmojiSuggestion last = null;
            for (int i=0; i<4; i++)
            {
                var ranked = best4[i];
                if (bestScore - ranked.Item1 > bestScore / 2) break;
                var suggestion = new UIEmojiSuggestion(ranked.Item2, this);
                suggestion.Position = new Vector2(0, i * 22);
                suggestion.Selected = (i == 0);
                Add(suggestion);
                last = suggestion;
            }
            last.Last = true;

            LastSearch = term;
        }

        public bool SelectBestTerm()
        {
            var bestOption = (UIEmojiSuggestion)Children.FirstOrDefault(x => ((UIEmojiSuggestion)x).Selected);
            if (bestOption != null)
            {
                SelectTerm(bestOption.EmojiName);
                return true;
            }
            return false;
        }

        public void SelectTerm(string term)
        {
            var sel = Watching.GetSelectedInd();
            var replInd = Watching.CurrentText.Substring(0, sel).LastIndexOf(':');
            if (replInd != -1)
            {
                Watching.CurrentText = Watching.CurrentText.Substring(0, replInd) + ':' + term + ": " + Watching.CurrentText.Substring(sel);
                Watching.SelectionToEnd();
            }
        }

        public void ClearSelection()
        {
            foreach (UIEmojiSuggestion child in Children)
                child.Selected = false;
        }

        //quick lcs from stack overflow
        //i'm mega lazy, but at least i provide credit!!
        //https://stackoverflow.com/questions/21797599/how-can-i-find-lcs-length-between-two-large-strings
        public static int LCS(char[] str1, char[] str2)
        {
            int[,] l = new int[str1.Length, str2.Length];
            int lcs = -1;
            string substr = string.Empty;
            int end = -1;

            for (int i = 0; i < str1.Length; i++)
            {
                for (int j = 0; j < str2.Length; j++)
                {
                    if (str1[i] == str2[j])
                    {
                        if (i == 0 || j == 0)
                        {
                            l[i, j] = 1;
                        }
                        else
                            l[i, j] = l[i - 1, j - 1] + 1;
                        if (l[i, j] > lcs)
                        {
                            lcs = l[i, j];
                            end = i;
                        }

                    }
                    else
                        l[i, j] = 0;
                }
            }

            for (int i = end - lcs + 1; i <= end; i++)
            {
                substr += str1[i];
            }

            return lcs;
        }
    }

    public class UIEmojiSuggestion : UIElement
    {
        public Texture2D PxWhite;
        public Rectangle Emoji;
        public bool Last;
        public bool Selected;
        public string EmojiName;
        public TextStyle Style;
        public UIEmojiSuggestions Owner;
        public UIMouseEventRef ClickHandler;

        public UIEmojiSuggestion(string emojiName, UIEmojiSuggestions parent)
        {
            PxWhite = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);
            Style = TextStyle.DefaultLabel.Clone();
            Style.Size = 8;
            EmojiName = emojiName;
            Owner = parent;

            Emoji = GameFacade.Emojis.GetEmoji(GameFacade.Emojis.EmojiFromName(emojiName)).Item2;
            ClickHandler =
                ListenForMouse(new Rectangle(0, 0, 200, 22), new UIMouseEvent(MouseEvent));
        }

        public void MouseEvent(UIMouseEventType type, UpdateState state)
        {
            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    Owner.ClearSelection();
                    Selected = true;
                    break;
                case UIMouseEventType.MouseDown:
                    Owner.SelectBestTerm();
                    break;
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            var emojis = GameFacade.Emojis.Cache.EmojiTex;
            DrawLocalTexture(batch, PxWhite, null, Vector2.Zero, new Vector2(200, 22), new Color(57, 85, 117));
            if (Selected) DrawLocalTexture(batch, PxWhite, null, Vector2.Zero, new Vector2(200, 22), Color.White*0.25f);
            DrawLocalTexture(batch, emojis, Emoji, new Vector2(3, 3), new Vector2(16/24f));
            DrawLocalString(batch, ":" + EmojiName + ":", new Vector2(24, 3), Style);
            if (!Last) DrawLocalTexture(batch, PxWhite, null, new Vector2(0, 21), new Vector2(200, 1), Color.White * 0.5f);
        }
    }
}
