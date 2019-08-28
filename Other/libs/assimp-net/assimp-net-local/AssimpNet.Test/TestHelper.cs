/*
* Copyright (c) 2012-2014 AssimpNet - Nicholas Woodfield
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TK = OpenTK;

namespace Assimp.Test
{
    /// <summary>
    /// Helper for Assimp.NET testing.
    /// </summary>
    public static class TestHelper
    {
        public const float DEFAULT_TOLERANCE = 0.000001f;
        public static float Tolerance = DEFAULT_TOLERANCE;

        private static String m_rootPath = null;

        public static String RootPath
        {
            get
            {
                if(m_rootPath == null)
                {
                    Assembly entryAssembly = Assembly.GetEntryAssembly();
                    String dirPath = String.Empty;

                    if(entryAssembly == null)
                        entryAssembly = Assembly.GetCallingAssembly();

                    if(entryAssembly != null)
                        dirPath = Path.GetDirectoryName(entryAssembly.Location);

                    m_rootPath = dirPath;
                }

                return m_rootPath;
            }
        }

        public static void AssertEquals(float expected, float actual)
        {
            Assert.IsTrue(Math.Abs(expected - actual) <= Tolerance);
        }

        public static void AssertEquals(float expected, float actual, String msg)
        {
            Assert.IsTrue(Math.Abs(expected - actual) <= Tolerance, msg);
        }

        public static void AssertEquals(float x, float y, Vector2D v)
        {
            AssertEquals(x, v.X);
            AssertEquals(y, v.Y);
        }

        public static void AssertEquals(float x, float y, Vector2D v, String msg)
        {
            AssertEquals(x, v.X, msg + String.Format(" => checking X component ({0} == {1}", x, v.X));
            AssertEquals(y, v.Y, msg + String.Format(" => checking Y component ({0} == {1}", y, v.Y));
        }

        public static void AssertEquals(float x, float y, float z, Vector3D v)
        {
            AssertEquals(x, v.X);
            AssertEquals(y, v.Y);
            AssertEquals(z, v.Z);
        }

        public static void AssertEquals(float x, float y, float z, Vector3D v, String msg)
        {
            AssertEquals(x, v.X, msg + String.Format(" => checking X component ({0} == {1}", x, v.X));
            AssertEquals(y, v.Y, msg + String.Format(" => checking Y component ({0} == {1}", y, v.Y));
            AssertEquals(z, v.Z, msg + String.Format(" => checking Z component ({0} == {1}", z, v.Z));
        }

        public static void AssertEquals(float r, float g, float b, float a, Color4D c)
        {
            AssertEquals(r, c.R);
            AssertEquals(g, c.G);
            AssertEquals(b, c.B);
            AssertEquals(a, c.A);
        }

        public static void AssertEquals(float r, float g, float b, float a, Color4D c, String msg)
        {
            AssertEquals(r, c.R, msg + String.Format(" => checking R component ({0} == {1}", r, c.R));
            AssertEquals(g, c.G, msg + String.Format(" => checking G component ({0} == {1}", g, c.G));
            AssertEquals(b, c.B, msg + String.Format(" => checking B component ({0} == {1}", b, c.B));
            AssertEquals(a, c.A, msg + String.Format(" => checking A component ({0} == {1}", a, c.A));
        }

        public static void AssertEquals(float r, float g, float b, Color3D c)
        {
            AssertEquals(r, c.R);
            AssertEquals(g, c.G);
            AssertEquals(b, c.B);
        }

        public static void AssertEquals(float r, float g, float b, Color3D c, String msg)
        {
            AssertEquals(r, c.R, msg + String.Format(" => checking R component ({0} == {1}", r, c.R));
            AssertEquals(g, c.G, msg + String.Format(" => checking G component ({0} == {1}", g, c.G));
            AssertEquals(b, c.B, msg + String.Format(" => checking B component ({0} == {1}", b, c.B));
        }

        public static void AssertEquals(float x, float y, float z, float w, Quaternion q, String msg)
        {
            AssertEquals(x, q.X, msg + String.Format(" => checking X component ({0} == {1}", x, q.X));
            AssertEquals(y, q.Y, msg + String.Format(" => checking Y component ({0} == {1}", y, q.Y));
            AssertEquals(z, q.Z, msg + String.Format(" => checking Z component ({0} == {1}", z, q.Z));
            AssertEquals(w, q.W, msg + String.Format(" => checking W component ({0} == {1}", w, q.W));
        }

        public static void AssertEquals(TK.Matrix4 tkM, Matrix3x3 mat, String msg)
        {
            //Note: OpenTK 4x4 matrix is a row-vector matrix, so compare rows to AssimpNet Matrix3x3 columns
            TK.Vector4 row0 = tkM.Row0;
            TK.Vector4 row1 = tkM.Row1;
            TK.Vector4 row2 = tkM.Row2;

            AssertEquals(row0.X, row0.Y, row0.Z, new Vector3D(mat.A1, mat.B1, mat.C1), msg + " => checking first column vector");
            AssertEquals(row1.X, row1.Y, row1.Z, new Vector3D(mat.A2, mat.B2, mat.C2), msg + " => checking second column vector");
            AssertEquals(row2.X, row2.Y, row2.Z, new Vector3D(mat.A3, mat.B3, mat.C3), msg + " => checking third column vector");
        }

        public static void AssertEquals(TK.Vector4 v1, TK.Vector4 v2, String msg)
        {
            AssertEquals(v1.X, v2.X, msg + String.Format(" => checking X component ({0} == {1}", v1.X, v2.X));
            AssertEquals(v1.Y, v2.Y, msg + String.Format(" => checking Y component ({0} == {1}", v1.Y, v2.Y));
            AssertEquals(v1.Z, v2.Z, msg + String.Format(" => checking Z component ({0} == {1}", v1.Z, v2.Z));
            AssertEquals(v1.W, v2.W, msg + String.Format(" => checking W component ({0} == {1}", v1.W, v2.W));
        }

        public static void AssertEquals(TK.Quaternion q1, Quaternion q2, String msg)
        {
            AssertEquals(q1.X, q2.X, msg + String.Format(" => checking X component ({0} == {1}", q1.X, q2.X));
            AssertEquals(q1.Y, q2.Y, msg + String.Format(" => checking Y component ({0} == {1}", q1.Y, q2.Y));
            AssertEquals(q1.Z, q2.Z, msg + String.Format(" => checking Z component ({0} == {1}", q1.Z, q2.Z));
            AssertEquals(q1.W, q2.W, msg + String.Format(" => checking W component ({0} == {1}", q1.W, q2.W));
        }

        public static void AssertEquals(TK.Matrix4 tkM, Matrix4x4 mat, String msg)
        {
            //Note: OpenTK 4x4 matrix is a row-vector matrix, so compare rows to AssimpNet Matrix4x4 columns
            AssertEquals(tkM.Row0, new TK.Vector4(mat.A1, mat.B1, mat.C1, mat.D1), msg + " => checking first column vector");
            AssertEquals(tkM.Row1, new TK.Vector4(mat.A2, mat.B2, mat.C2, mat.D2), msg + " => checking second column vector");
            AssertEquals(tkM.Row2, new TK.Vector4(mat.A3, mat.B3, mat.C3, mat.D3), msg + " => checking third column vector");
            AssertEquals(tkM.Row3, new TK.Vector4(mat.A4, mat.B4, mat.C4, mat.D4), msg + " => checking third column vector");
        }
    }
}
