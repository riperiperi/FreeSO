/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.Client.UI;
using System;

namespace FSO.Client.Utils
{
    public class UIUtils
    {
        public static UIDragHandler MakeDraggable(UIElement mouseTarget, UIElement dragControl)
        {
            var handler = new UIDragHandler(mouseTarget, dragControl);
            return handler;
        }

        public static UIDragHandler MakeDraggable(UIElement mouseTarget, UIElement dragControl, bool bringToFront)
        {
            var handler = new UIDragHandler(mouseTarget, dragControl);
            handler.BringToFront = bringToFront;
            return handler;
        }

        public static UITooltipHandler GiveTooltip(UIElement target)
        {
            var handler = new UITooltipHandler(target);
            return handler;
        }

        public static UIWordWrapOutput WordWrap(string text, int width, TextStyle style)
        {
            return WordWrap(text, width, style, int.MaxValue);
        }

        public static UIWordWrapOutput WordWrap(string text, int width, TextStyle style, int maxLines)
        {
            var result = new UIWordWrapOutput();
            result.Lines = new List<string>();
            var textLines = text.Split('\n');// new string[] {text}; //only support single line for now, since we're only using this utility function for captions
		    int maxWidth = 0;
		    int curpos = 0;
		    var positions = new List<int>();
		    for (var l=0; l<textLines.Length; l++) {
			    List<string> words = textLines[l].Split(' ').ToList();

			    while (words.Count > 0) {
                    var atMax = maxLines == result.Lines.Count+1;
				    var lineBuffer = new List<string>();
                    int i = 0;
				    for (i=0; i<words.Count; i++) {
                        lineBuffer.Add(words[i]);
					    var str = JoinWordList(lineBuffer);      //(lineBuffer.concat([words[i]])).join(" ");
                        int w = (int)(style.MeasureString(str).X);
					    if (w > width) {
                            if (atMax)
                            {
                                lineBuffer.Clear();
                                lineBuffer.Add(style.TruncateToWidth(str, width));
                                break;
                            }

                            lineBuffer.RemoveAt(lineBuffer.Count-1);
						    if (lineBuffer.Count == 0) {
							    for (var j=words[i].Length-1; j>0; j--) {
								    var str2 = words[i].Substring(0, j);
                                    var w2 = (int)(style.MeasureString(str2).X);
								    if (w2 <= width) {
									    curpos += j;
									    lineBuffer.Add(words[i].Substring(0, j));
									    words[i] = words[i].Substring(j);
									    if (w > maxWidth) maxWidth = w;
									    break;
								    }
							    }
						    }
						    break;
					    } else {
						    if (w > maxWidth) maxWidth = w;
                            curpos += words[i].Length + 1;
					    }
				    }
                    result.Lines.Add(JoinWordList(lineBuffer));
                    positions.Add(curpos);
                    if (atMax)
                    {
                        words.Clear();
                        l = textLines.Length; //exit early
                    }
                    else
                        words.RemoveRange(0, i);
			    }
			    //curpos++;
		    }
            result.Positions = positions;
            result.MaxWidth = maxWidth;
            result.Height = result.Lines.Count * style.LineHeight;
		    return result;
        }

        private static string JoinWordList(List<string> input) {
            var result = new StringBuilder();
            for (int i = 0; i < input.Count; i++)
            {
                result.Append(input.ElementAt(i));
                if (i != input.Count-1) result.Append(" ");
            }
            return result.ToString();
        }
    }

    public class UIDragHandler
    {
        public UIElement MouseTarget;
        public UIElement DragControl;
        public UIMouseEventRef MouseEvent;
        public bool BringToFront = false;

        private UpdateHookDelegate UpdateHook;

        public UIDragHandler(UIElement mouseTarget, UIElement dragControl)
        {
            UpdateHook = new UpdateHookDelegate(Update);

            MouseTarget = mouseTarget;
            DragControl = dragControl;
            MouseEvent = mouseTarget.ListenForMouse(mouseTarget.GetBounds(), new UIMouseEvent(DragMouseEvents));
        }

        private bool m_doDrag = false;
        private float m_dragOffsetX;
        private float m_dragOffsetY;

        /// <summary>
        /// Handle mouse events for dragging
        /// </summary>
        /// <param name="evt"></param>
        private void DragMouseEvents(UIMouseEventType evt, UpdateState state)
        {
            switch (evt)
            {
                case UIMouseEventType.MouseDown:
                    if (BringToFront)
                    {
                        DragControl.Parent.Add(DragControl);
                    }
                    /** Start drag **/
                    m_doDrag = true;
                    DragControl.AddUpdateHook(UpdateHook);

                    var position = DragControl.GetMousePosition(state.MouseState);
                    m_dragOffsetX = position.X;
                    m_dragOffsetY = position.Y;
                    break;

                case UIMouseEventType.MouseUp:
                    /** Stop drag **/
                    m_doDrag = false;
                    DragControl.RemoveUpdateHook(UpdateHook);
                    break;
            }
        }

        private void Update(UpdateState state)
        {
            if (m_doDrag)
            {
                /** Drag the dialog box **/
                var position = DragControl.Parent.GetMousePosition(state.MouseState);
                DragControl.X = position.X - m_dragOffsetX;
                DragControl.Y = position.Y - m_dragOffsetY;
            }
        }
    }

    public class UITooltipHandler
    {
        public UIElement Target;
        private UpdateHookDelegate UpdateHook;

        public UITooltipHandler(UIElement target)
        {
            UpdateHook = new UpdateHookDelegate(Update);

            Target = target;
            Target.AddUpdateHook(UpdateHook);
        }

        private bool m_active = false;
        private float m_fade;
        private Vector2 m_position;

        private void Update(UpdateState state)
        {
            Vector2 pt = Target.GetMousePosition(state.MouseState);
            var pt2 = new Microsoft.Xna.Framework.Point((int)pt.X, (int)pt.Y);
            if (m_active)
            {
                if (m_fade < 1) m_fade += 0.1f;
                if (m_fade > 1) m_fade = 1;

                state.UIState.TooltipProperties.Show = true;
                state.UIState.TooltipProperties.Color = Color.Black;
                state.UIState.TooltipProperties.UpdateDead = false;
                state.UIState.TooltipProperties.Position = m_position;
                state.UIState.TooltipProperties.Opacity = m_fade;
                state.UIState.Tooltip = Target.Tooltip;
                /** fade in **/
                if (!Target.GetBounds().Contains(pt2) || !GameFacade.Focus || !Target.WillDraw())
                {
                    m_active = false;
                    state.UIState.TooltipProperties.Show = false;
                    state.UIState.TooltipProperties.Opacity = 0;
                    m_fade = 0;
                }
            }
            else
            {
                if (Target.GetBounds().Contains(pt2) && Target.Tooltip != null && Target.WillDraw() && GameFacade.Focus)
                {
                    m_active = true;
                    state.UIState.TooltipProperties.Show = true;
                    state.UIState.TooltipProperties.Color = Color.Black;
                    state.UIState.TooltipProperties.Opacity = 0;
                    state.UIState.TooltipProperties.UpdateDead = false;
                    state.UIState.Tooltip = Target.Tooltip;
                    m_fade = 0;

                    m_position = new Vector2(state.MouseState.X, Target.LocalPoint(new Vector2(0, 0)).Y); //at top of element
                }
            }
        }
    }

    public class UIWordWrapOutput {
        public int MaxWidth;
        public List<string> Lines;
        public List<int> Positions;
        public int Height;
    }
}
