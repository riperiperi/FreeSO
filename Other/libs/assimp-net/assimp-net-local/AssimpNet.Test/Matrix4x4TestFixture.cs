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
using NUnit.Framework;
using TK = OpenTK;

namespace Assimp.Test
{
    [TestFixture]
    public class Matrix4x4TestFixture
    {
        [Test]
        public void TestIndexer()
        {
            float[] values = new float[] { 1.0f, 2.0f, 3.0f, 5.0f, 0.0f, -5.0f, .5f, 100.25f, .3f, .35f, .025f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f };

            Matrix4x4 m = Matrix4x4.Identity;
            for(int i = 0; i < 4; i++)
            {
                for(int j = 0; j < 4; j++)
                {
                    float value = values[(i * 4) + j];
                    //Matrix indices are one-based.
                    m[i + 1, j + 1] = value;
                    TestHelper.AssertEquals(value, m[i + 1, j + 1], String.Format("Testing [{0},{1}] indexer.", i + 1, j + 1));
                }
            }
        }

        [Test]
        public void TestEquals()
        {
            Matrix4x4 m1 = new Matrix4x4(1.0f, 2.0f, 3.0f, 5.0f, 0.0f, -5.0f, .5f, 100.25f, .3f, .35f, .025f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);
            Matrix4x4 m2 = new Matrix4x4(1.0f, 2.0f, 3.0f, 5.0f, 0.0f, -5.0f, .5f, 100.25f, .3f, .35f, .025f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);
            Matrix4x4 m3 = new Matrix4x4(0.0f, 2.0f, 25.0f, 5.0f, 1.0f, 5.0f, 5.5f, 100.25f, 1.25f, 8.5f, 2.25f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);

            //Test IEquatable Equals
            Assert.IsTrue(m1.Equals(m2), "Test IEquatable equals");
            Assert.IsFalse(m1.Equals(m3), "Test IEquatable equals");

            //Test object equals override
            Assert.IsTrue(m1.Equals((object) m2), "Tests object equals");
            Assert.IsFalse(m1.Equals((object) m3), "Tests object equals");

            //Test op equals
            Assert.IsTrue(m1 == m2, "Testing OpEquals");
            Assert.IsFalse(m1 == m3, "Testing OpEquals");

            //Test op not equals
            Assert.IsTrue(m1 != m3, "Testing OpNotEquals");
            Assert.IsFalse(m1 != m2, "Testing OpNotEquals");
        }

        [Test]
        public void TestDecompose()
        {
            Vector3D axis = new Vector3D(.25f, .5f, 0.0f);
            axis.Normalize();

            Quaternion rot = new Quaternion(axis, TK.MathHelper.Pi);
            float x = 50.0f;
            float y = 100.0f;
            float z = -50.0f;

            float scale = 2.0f;

            Matrix4x4 m = Matrix4x4.FromScaling(new Vector3D(scale, scale, scale)) * Matrix4x4.FromAngleAxis(TK.MathHelper.Pi, axis) * Matrix4x4.FromTranslation(new Vector3D(x, y, z));

            Vector3D scaling1;
            Quaternion rotation1;
            Vector3D translation1;
            Assimp.Unmanaged.AssimpLibrary.Instance.DecomposeMatrix(ref m, out scaling1, out rotation1, out translation1);

            Vector3D scaling2;
            Quaternion rotation2;
            Vector3D translation2;
            m.Decompose(out scaling2, out rotation2, out translation2);

            TestHelper.AssertEquals(scaling1.X, scaling1.Y, scaling1.Z, scaling2, "Testing decomposed scaling output");
            TestHelper.AssertEquals(rotation1.X, rotation1.Y, rotation1.Z, rotation1.W, rotation2, "Testing decomposed rotation output");
            TestHelper.AssertEquals(translation1.X, translation1.Y, translation1.Z, translation2, "Testing decomposed translation output");

            m = Matrix4x4.FromAngleAxis(TK.MathHelper.Pi, axis) * Matrix4x4.FromTranslation(new Vector3D(x, y, z));

            m.DecomposeNoScaling(out rotation2, out translation2);

            TestHelper.AssertEquals(rot.X, rot.Y, rot.Z, rot.W, rotation2, "Testing no scaling decomposed rotation output");
            TestHelper.AssertEquals(x, y, z, translation2, "Testing no scaling decomposed translation output");
        }

        [Test]
        public void TestDeterminant()
        {
            float x = TK.MathHelper.Pi;
            float y = TK.MathHelper.PiOver3;

            TK.Matrix4 tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationY(y);
            Matrix4x4 m = Matrix4x4.FromRotationX(x) * Matrix4x4.FromRotationY(y);

            float tkDet = tkM.Determinant;
            float det = m.Determinant();
            TestHelper.AssertEquals(tkDet, det, "Testing determinant");
        }

        [Test]
        public void TestFromAngleAxis()
        {
            TK.Matrix4 tkM = TK.Matrix4.CreateFromAxisAngle(TK.Vector3.UnitY, TK.MathHelper.Pi);
            Matrix4x4 m = Matrix4x4.FromAngleAxis(TK.MathHelper.Pi, new Vector3D(0, 1, 0));

            TestHelper.AssertEquals(tkM, m, "Testing from angle axis");
        }

        [Test]
        public void TestFromEulerAnglesXYZ()
        {
            float x = TK.MathHelper.Pi;
            float y = 0.0f;
            float z = TK.MathHelper.PiOver4;

            TK.Matrix4 tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationZ(z);
            Matrix4x4 m = Matrix4x4.FromEulerAnglesXYZ(x, y, z);
            Matrix4x4 m2 = Matrix4x4.FromEulerAnglesXYZ(new Vector3D(x, y, z));

            TestHelper.AssertEquals(tkM, m, "Testing create from euler angles");
            Assert.IsTrue(m == m2, "Testing if create from euler angle as a vector is the same as floats.");
        }

        [Test]
        public void TestFromRotationX()
        {
            float x = TK.MathHelper.Pi;

            TK.Matrix4 tkM = TK.Matrix4.CreateRotationX(x);
            Matrix4x4 m = Matrix4x4.FromRotationX(x);

            TestHelper.AssertEquals(tkM, m, "Testing from rotation x");
        }

        [Test]
        public void TestFromRotationY()
        {
            float y = TK.MathHelper.Pi;

            TK.Matrix4 tkM = TK.Matrix4.CreateRotationY(y);
            Matrix4x4 m = Matrix4x4.FromRotationY(y);

            TestHelper.AssertEquals(tkM, m, "Testing from rotation y");
        }

        [Test]
        public void TestFromRotationZ()
        {
            float z = TK.MathHelper.Pi;

            TK.Matrix4 tkM = TK.Matrix4.CreateRotationZ(z);
            Matrix4x4 m = Matrix4x4.FromRotationZ(z);

            TestHelper.AssertEquals(tkM, m, "Testing from rotation z");
        }

        [Test]
        public void TestFromScaling()
        {
            float x = 1.0f;
            float y = 2.0f;
            float z = 3.0f;

            TK.Matrix4 tkM = TK.Matrix4.Scale(x, y, z);
            Matrix4x4 m = Matrix4x4.FromScaling(new Vector3D(x, y, z));

            TestHelper.AssertEquals(tkM, m, "Testing from scaling");
        }

        [Test]
        public void TestFromToMatrix()
        {
            Vector3D from = new Vector3D(1, 0, 0);
            Vector3D to = new Vector3D(0, 1, 0);

            TK.Matrix4 tkM = TK.Matrix4.CreateRotationZ(TK.MathHelper.PiOver2);
            Matrix4x4 m = Matrix4x4.FromToMatrix(from, to);

            TestHelper.AssertEquals(tkM, m, "Testing From-To rotation matrix");
        }

        [Test]
        public void TestFromTranslation()
        {
            float x = 52.0f;
            float y = -100.0f;
            float z = 5.0f;

            TK.Matrix4 tkM = TK.Matrix4.CreateTranslation(x, y, z);
            Matrix4x4 m = Matrix4x4.FromTranslation(new Vector3D(x, y, z));

            TestHelper.AssertEquals(tkM, m, "Testing from translation");
        }

        [Test]
        public void TestInverse()
        {
            float x = TK.MathHelper.PiOver6;
            float y = TK.MathHelper.Pi;

            TK.Matrix4 tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationY(y);
            Matrix4x4 m = Matrix4x4.FromRotationX(x) * Matrix4x4.FromRotationY(y);

            tkM.Invert();
            m.Inverse();

            TestHelper.AssertEquals(tkM, m, "Testing inverse");
        }

        [Test]
        public void TestIdentity()
        {
            TK.Matrix4 tkM = TK.Matrix4.Identity;
            Matrix4x4 m = Matrix4x4.Identity;

            Assert.IsTrue(m.IsIdentity, "Testing IsIdentity");
            TestHelper.AssertEquals(tkM, m, "Testing is identity to baseline");
        }

        [Test]
        public void TestTranspose()
        {
            float x = TK.MathHelper.Pi;
            float y = TK.MathHelper.PiOver4;

            TK.Matrix4 tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationY(y);
            Matrix4x4 m = Matrix4x4.FromRotationX(x) * Matrix4x4.FromRotationY(y);

            tkM.Transpose();
            m.Transpose();
            TestHelper.AssertEquals(tkM, m, "Testing transpose");
        }

        [Test]
        public void TestOpMultiply()
        {
            float x = TK.MathHelper.Pi;
            float y = TK.MathHelper.PiOver3;

            TK.Matrix4 tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationY(y);
            Matrix4x4 m = Matrix4x4.FromRotationX(x) * Matrix4x4.FromRotationY(y);

            TestHelper.AssertEquals(tkM, m, "Testing Op multiply");
        }
    }
}
