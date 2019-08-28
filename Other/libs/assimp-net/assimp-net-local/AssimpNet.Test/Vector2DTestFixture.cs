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

namespace Assimp.Test
{
    [TestFixture]
    public class Vector2DTestFixture
    {
        [Test]
        public void TestIndexer()
        {
            float x = 1, y = 2;
            Vector2D v = new Vector2D();
            v[0] = x;
            v[1] = y;
            TestHelper.AssertEquals(x, v[0], "Test Indexer, X");
            TestHelper.AssertEquals(y, v[1], "Test Indexer, Y");
        }

        [Test]
        public void TestSet()
        {
            float x = 10.5f, y = 109.21f;
            Vector2D v = new Vector2D();
            v.Set(x, y);

            TestHelper.AssertEquals(x, y, v, "Test v.Set()");
        }

        [Test]
        public void TestEquals()
        {
            float x = 1, y = 2;
            float x2 = 3, y2 = 4;

            Vector2D v1 = new Vector2D(x, y);
            Vector2D v2 = new Vector2D(x, y);
            Vector2D v3 = new Vector2D(x2, y2);

            //Test IEquatable Equals
            Assert.IsTrue(v1.Equals(v2), "Test IEquatable equals");
            Assert.IsFalse(v1.Equals(v3), "Test IEquatable equals");

            //Test object equals override
            Assert.IsTrue(v1.Equals((object) v2), "Tests object equals");
            Assert.IsFalse(v1.Equals((object) v3), "Tests object equals");

            //Test op equals
            Assert.IsTrue(v1 == v2, "Testing OpEquals");
            Assert.IsFalse(v1 == v3, "Testing OpEquals");

            //Test op not equals
            Assert.IsTrue(v1 != v3, "Testing OpNotEquals");
            Assert.IsFalse(v1 != v2, "Testing OpNotEquals");
        }

        [Test]
        public void TestLength()
        {
            float x = -62, y = 5;

            Vector2D v = new Vector2D(x, y);
            Assert.AreEqual((float) Math.Sqrt(x * x + y * y), v.Length(), "Testing v.Length()");
        }

        [Test]
        public void TestLengthSquared()
        {
            float x = -5, y = 25f;

            Vector2D v = new Vector2D(x, y);
            Assert.AreEqual((float) (x * x + y * y), v.LengthSquared(), "Testing v.LengthSquared()");
        }

        [Test]
        public void TestNegate()
        {
            float x = 2, y = 5;

            Vector2D v = new Vector2D(x, y);
            v.Negate();
            TestHelper.AssertEquals(-x, -y, v, "Testing v.Negate()");
        }

        [Test]
        public void TestNormalize()
        {
            float x = 5, y = 12;
            Vector2D v = new Vector2D(x, y);
            v.Normalize();
            float invLength = 1.0f / (float) System.Math.Sqrt((x * x) + (y * y));
            x *= invLength;
            y *= invLength;

            TestHelper.AssertEquals(x, y, v, "Testing v.Normalize()");
        }

        [Test]
        public void TestOpAdd()
        {
            float x1 = 2, y1 = 5;
            float x2 = 10, y2 = 15;
            float x = x1 + x2;
            float y = y1 + y2;

            Vector2D v1 = new Vector2D(x1, y1);
            Vector2D v2 = new Vector2D(x2, y2);

            Vector2D v = v1 + v2;

            TestHelper.AssertEquals(x, y, v, "Testing v1 + v2");
        }

        [Test]
        public void TestOpSubtract()
        {
            float x1 = 2, y1 = 5;
            float x2 = 10, y2 = 15;
            float x = x1 - x2;
            float y = y1 - y2;

            Vector2D v1 = new Vector2D(x1, y1);
            Vector2D v2 = new Vector2D(x2, y2);

            Vector2D v = v1 - v2;

            TestHelper.AssertEquals(x, y, v, "Testing v1 - v2");
        }

        [Test]
        public void TestOpNegate()
        {
            float x = 22, y = 75;

            Vector2D v = -(new Vector2D(x, y));

            TestHelper.AssertEquals(-x, -y, v, "Testting -v)");
        }

        [Test]
        public void TestOpMultiply()
        {
            float x1 = 2, y1 = 5;
            float x2 = 10, y2 = 15;
            float x = x1 * x2;
            float y = y1 * y2;

            Vector2D v1 = new Vector2D(x1, y1);
            Vector2D v2 = new Vector2D(x2, y2);

            Vector2D v = v1 * v2;

            TestHelper.AssertEquals(x, y, v, "Testing v1 * v2");
        }

        [Test]
        public void TestOpMultiplyByScalar()
        {
            float x1 = 2, y1 = 5;
            float scalar = 25;

            float x = x1 * scalar;
            float y = y1 * scalar;

            Vector2D v1 = new Vector2D(x1, y1);

            //Left to right
            Vector2D v = v1 * scalar;
            TestHelper.AssertEquals(x, y, v, "Testing v * scale");

            //Right to left
            v = scalar * v1;
            TestHelper.AssertEquals(x, y, v, "Testing scale * v");
        }

        [Test]
        public void TestOpDivide()
        {
            float x1 = 105f, y1 = 4.5f;
            float x2 = 22f, y2 = 25.2f;

            float x = x1 / x2;
            float y = y1 / y2;

            Vector2D v1 = new Vector2D(x1, y1);
            Vector2D v2 = new Vector2D(x2, y2);

            Vector2D v = v1 / v2;

            TestHelper.AssertEquals(x, y, v, "Testing v1 / v2");
        }

        [Test]
        public void TestOpDivideByFactor()
        {
            float x1 = 55f, y1 = 2f;
            float divisor = 5f;

            float x = x1 / divisor;
            float y = y1 / divisor;

            Vector2D v = new Vector2D(x1, y1) / divisor;

            TestHelper.AssertEquals(x, y, v, "Testing v / divisor");
        }
    }
}
