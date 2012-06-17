/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DNA;

namespace MeshReader
{
    public struct Face
    {
        public int X, Y, Z;
    }

    class Program
    {
        private static int m_Version = 0;

        private static int m_BoneCount = 0;
        private static List<string> m_BoneNames = new List<string>();

        private static int m_FaceCount = 0;
        private static Face[] m_Faces;

        private static int m_BndCount = 0;
        private static int[,] m_BoneBindings;

        private static int m_NumTexVerticies = 0;
        private static Single[,] m_TexVerticies;

        private static int m_BlendCount = 0;
        private static int[,] m_BlendData;

        private static int m_VertexCount = 0;
        private static Single[,] m_VertexData;

        static void Main(string[] args)
        {
            BinaryReader Reader = new BinaryReader(
                File.Open("avatardata\\heads\\meshes\\fahc101fa_longwave-head-head.mesh", FileMode.Open));

            m_Version = Endian.SwapInt32(Reader.ReadInt32());
            Console.WriteLine("Version: " + m_Version + "\n");

            m_BoneCount = Endian.SwapInt32(Reader.ReadInt32());
            Console.WriteLine("Number of bones: " + m_BoneCount);

            for (int i = 0; i < m_BoneCount; i++)
            {
                byte StrLen = Reader.ReadByte();
                string BoneName = Encoding.ASCII.GetString(Reader.ReadBytes(StrLen));
                m_BoneNames.Add(BoneName);
                Console.WriteLine(BoneName);
            }

            m_FaceCount = Endian.SwapInt32(Reader.ReadInt32());
            m_Faces = new Face[m_FaceCount];
            Console.WriteLine("Number of faces: " + m_FaceCount);

            for (int i = 0; i < m_FaceCount; i++)
            {
                m_Faces[i].X = Endian.SwapInt32(Reader.ReadInt32());
                m_Faces[i].Y = Endian.SwapInt32(Reader.ReadInt32());
                m_Faces[i].Z = Endian.SwapInt32(Reader.ReadInt32());
            }

            m_BndCount = Endian.SwapInt32(Reader.ReadInt32());
            m_BoneBindings = new int[m_BndCount, 5];
            Console.WriteLine("Number of bonebindings: " + m_BndCount);

            for (int i = 0; i < m_BndCount; i++)
                for (int j = 0; j < 5; j++)
                    m_BoneBindings[i, j] = Endian.SwapInt32(Reader.ReadInt32());

            m_NumTexVerticies = Endian.SwapInt32(Reader.ReadInt32());
            m_TexVerticies = new Single[m_NumTexVerticies, 3];
            Console.WriteLine("Number of texture verticies: " + m_NumTexVerticies);

            switch (m_Version)
            {
                case 0:
                    for (int i = 0; i < m_NumTexVerticies; i++)
                    {
                        //These coordinates aren't reversed, and the Endian class
                        //doesn't support swapping Single values, so do it manually...
                        m_TexVerticies[i, 0] = i;
                        byte[] XOffset = Reader.ReadBytes(4);
                        byte[] YOffset = Reader.ReadBytes(4);

                        Array.Reverse(XOffset);
                        Array.Reverse(YOffset);

                        m_TexVerticies[i, 1] = BitConverter.ToSingle(XOffset, 0);
                        m_TexVerticies[i, 2] = BitConverter.ToSingle(YOffset, 0);
                    }

                    break;
                default:
                    for (int i = 0; i < m_NumTexVerticies; i++)
                    {
                        m_TexVerticies[i, 0] = i;
                        m_TexVerticies[i, 1] = Reader.ReadSingle(); //X offset
                        m_TexVerticies[i, 2] = Reader.ReadSingle(); //Y offset
                    }

                    break;
            }

            m_BlendCount = Endian.SwapInt32(Reader.ReadInt32());
            m_BlendData = new int[m_BlendCount, 2];
            Console.WriteLine("Number of blends: " + m_BlendCount);

            for (int i = 0; i < m_BlendCount; i++)
            {
                m_BlendData[i, 1] = Endian.SwapInt32(Reader.ReadInt32());
                m_BlendData[i, 0] = Endian.SwapInt32(Reader.ReadInt32());
            }

            m_VertexCount = Endian.SwapInt32(Reader.ReadInt32());
            m_VertexData = new Single[m_VertexCount, 7];
            Console.WriteLine("Number of verticies: " + m_VertexCount);

            switch (m_Version)
            {
                case 0:
                    for (int i = 0; i < m_VertexCount; i++)
                    {
                        m_VertexData[i, 0] = i;

                        for (int j = 0; j < 6; j++)
                            m_VertexData[i, j] = Reader.ReadSingle();
                    }

                    break;
                default:
                    for (int i = 0; i < m_VertexCount; i++)
                    {
                        m_VertexData[i, 0] = i;

                        //These coordinates are apparently reversed, but since the file is Big-Endian,
                        //and the default is reading Little-Endian, there should be no need to convert...
                        for (int j = 0; j < 6; j++)
                            m_VertexData[i, j] = Reader.ReadSingle();
                    }

                    break;
            }

            Console.ReadLine();
        }
    }
}
