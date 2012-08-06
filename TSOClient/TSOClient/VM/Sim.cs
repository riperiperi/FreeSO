/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using SimsLib.ThreeD;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.VM
{
    /// <summary>
    /// Represents a Sim/Character in the game.
    /// </summary>
    public class Sim : SimulationObject
    {
        private int m_CharacterID;
        private string m_Timestamp;
        private string m_Name;
        private string m_Sex;

        private Mesh m_HeadMesh;
        private Texture2D m_HeadTexture;
        public float HeadXPos = 0.0f, HeadYPos = 0.0f;

        /// <summary>
        /// The character's ID, as it exists in the DB.
        /// </summary>
        public int CharacterID
        {
            get { return m_CharacterID; }
            set { m_CharacterID = value; }
        }

        /// <summary>
        /// When was this character last cached by the client?
        /// </summary>
        public string Timestamp
        {
            get { return m_Timestamp; }
            set { m_Timestamp = value; }
        }

        /// <summary>
        /// The character's name, as it exists in the DB.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public string Sex
        {
            get { return m_Sex; }
            set { m_Sex = value; }
        }

        /// <summary>
        /// The headmesh for this Sim.
        /// </summary>
        public Mesh HeadMesh
        {
            get { return m_HeadMesh; }
            set { m_HeadMesh = value; }
        }

        /// <summary>
        /// The headtexture for this Sim.
        /// </summary>
        public Texture2D HeadTexture
        {
            get { return m_HeadTexture; }
            set { m_HeadTexture = value; }
        }

        public Sim(string GUID) : 
            base(GUID)
        {

        }
    }
}
