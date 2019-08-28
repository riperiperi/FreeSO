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

using NUnit.Framework;
using TK = OpenTK;

namespace Assimp.Test
{
    [TestFixture]
    public class QuaternionTestFixture
    {
        [Test]
        public void TestEquals()
        {
            Quaternion q1 = new Quaternion(.25f, .75f, .5f, 1.0f);
            Quaternion q2 = new Quaternion(.25f, .75f, .5f, 1.0f);
            Quaternion q3 = new Quaternion(.55f, .17f, 1.0f, .15f);

            //Test IEquatable Equals
            Assert.IsTrue(q1.Equals(q2), "Test IEquatable equals");
            Assert.IsFalse(q1.Equals(q3), "Test IEquatable equals");

            //Test object equals override
            Assert.IsTrue(q1.Equals((object) q2), "Tests object equals");
            Assert.IsFalse(q1.Equals((object) q3), "Tests object equals");

            //Test op equals
            Assert.IsTrue(q1 == q2, "Testing OpEquals");
            Assert.IsFalse(q1 == q3, "Testing OpEquals");

            //Test op not equals
            Assert.IsTrue(q1 != q3, "Testing OpNotEquals");
            Assert.IsFalse(q1 != q2, "Testing OpNotEquals");
        }

        [Test]
        public void TestConjugate()
        {
            TK.Quaternion tkQ = TK.Quaternion.FromAxisAngle(TK.Vector3.UnitY, TK.MathHelper.PiOver2);
            Quaternion q = new Quaternion(tkQ.W, tkQ.X, tkQ.Y, tkQ.Z);

            tkQ.Conjugate();
            q.Conjugate();

            TestHelper.AssertEquals(tkQ.X, tkQ.Y, tkQ.Z, tkQ.W, q, "Testing conjugate");
        }

        [Test]
        public void TestGetMatrix()
        {
            TK.Quaternion tkQ = TK.Quaternion.FromAxisAngle(new TK.Vector3(.25f, .5f, 0.0f), TK.MathHelper.PiOver2);
            Quaternion q = new Quaternion(tkQ.W, tkQ.X, tkQ.Y, tkQ.Z);

            TK.Matrix4 tkM = TK.Matrix4.CreateFromAxisAngle(new TK.Vector3(.25f, .5f, 0.0f), TK.MathHelper.PiOver2);
            Matrix4x4 m = q.GetMatrix();

            TestHelper.AssertEquals(tkM, m, "Testing GetMatrix");
        }

        [Test]
        public void TestNormalize()
        {
            TK.Quaternion tkQ = TK.Quaternion.FromAxisAngle(new TK.Vector3(.25f, .5f, 0.0f), TK.MathHelper.PiOver2);
            Quaternion q = new Quaternion(tkQ.W, tkQ.X, tkQ.Y, tkQ.Z);

            tkQ.Normalize();
            q.Normalize();

            TestHelper.AssertEquals(tkQ.X, tkQ.Y, tkQ.Z, tkQ.W, q, "Testing normalize");
        }

        [Test]
        public void TestRotate()
        {
            TK.Vector3 tkV1 = new TK.Vector3(0, 5, 10);
            Vector3D v1 = new Vector3D(0, 5, 10);

            TK.Quaternion tkQ = TK.Quaternion.FromAxisAngle(TK.Vector3.UnitY, TK.MathHelper.PiOver2);
            Quaternion q = new Quaternion(tkQ.W, tkQ.X, tkQ.Y, tkQ.Z);

            TK.Vector3 tkV2 = TK.Vector3.Transform(tkV1, tkQ);
            Vector3D v2 = Quaternion.Rotate(v1, q);

            TestHelper.AssertEquals(tkV2.X, tkV2.Y, tkV2.Z, v2, "Testing rotate");
        }

        [Test]
        public void TestSlerp()
        {
            TK.Quaternion tkQ1 = TK.Quaternion.FromAxisAngle(TK.Vector3.UnitY, TK.MathHelper.PiOver2);
            Quaternion q1 = new Quaternion(tkQ1.W, tkQ1.X, tkQ1.Y, tkQ1.Z);

            TK.Quaternion tkQ2 = TK.Quaternion.FromAxisAngle(TK.Vector3.UnitY, TK.MathHelper.Pi);
            Quaternion q2 = new Quaternion(tkQ2.W, tkQ2.X, tkQ2.Y, tkQ2.Z);

            Quaternion q = Quaternion.Slerp(q1, q2, .5f);

            TK.Quaternion tkQ = TK.Quaternion.Slerp(tkQ1, tkQ2, .5f);

            TestHelper.AssertEquals(tkQ.X, tkQ.Y, tkQ.Z, tkQ.W, q, "Testing slerp");
        }

        [Test]
        public void TestOpMultiply()
        {
            TK.Quaternion tkQ1 = TK.Quaternion.FromAxisAngle(TK.Vector3.UnitY, TK.MathHelper.PiOver2);
            Quaternion q1 = new Quaternion(tkQ1.W, tkQ1.X, tkQ1.Y, tkQ1.Z);

            TK.Quaternion tkQ2 = TK.Quaternion.FromAxisAngle(TK.Vector3.UnitY, TK.MathHelper.Pi);
            Quaternion q2 = new Quaternion(tkQ2.W, tkQ2.X, tkQ2.Y, tkQ2.Z);

            Quaternion q = q1 * q2;

            TK.Quaternion tkQ = tkQ1 * tkQ2;

            TestHelper.AssertEquals(tkQ.X, tkQ.Y, tkQ.Z, tkQ.W, q, "Testing Op multiply");
        }
    }
}
