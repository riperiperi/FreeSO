/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): Nicholas Roth.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimsLib
{
    /// <summary>
    /// Class for code that can be used in several places.
    /// </summary>
    class Misc
    {
        /// <summary>
        /// Wraps a string to a specific linewidth. If the string is too long,
        /// a linebreak is added. Courtesy of Ryth.
        /// </summary>
        /// <param name="spriteFont">The font used for displaying the string.</param>
        /// <param name="text">The string itself.</param>
        /// <param name="maxLineWidth">The maximum width of the text.</param>
        /// <returns>The original string with possible added linebreaks.</returns>
        public static string WrapText(SpriteFont spriteFont, string text, float maxLineWidth)
        {
            string[] words = text.Split(' ');

            StringBuilder sb = new StringBuilder();

            float lineWidth = 0f;

            float spaceWidth = spriteFont.MeasureString(" ").X;

            foreach (string word in words)
            {
                Vector2 size = spriteFont.MeasureString(word);

                if (lineWidth + size.X < maxLineWidth)
                {
                    sb.Append(word + " ");
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    sb.Append("\n" + word + " ");
                    lineWidth = size.X + spaceWidth;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Calculates the center of a given width, if a string of text needs to be centered. Courtesy of Ryth.
        /// </summary>
        /// <param name="Font">The font used for displaying the string.</param>
        /// <param name="Text">The string itself.</param>
        /// <param name="Width">The given width within which the text needs to be centered.</param>
        /// <param name="Y">The Y-coordinate of where to display the text.</param>
        /// <returns>A Vector2 instance that can be passed to SpriteBatch.DrawString()</returns>
        public static Vector2 CenterText(SpriteFont Font, string Text, int Width, float Y)
        {
            Vector2 TextSize = Font.MeasureString(Text);
            Vector2 TextCenter = new Vector2(Width / 2, Y);

            return TextCenter - (TextSize / 2);
        }
    }
}
