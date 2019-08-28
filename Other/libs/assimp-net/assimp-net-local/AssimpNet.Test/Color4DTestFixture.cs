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

namespace Assimp.Test
{
    [TestFixture]
    public class Color4DTestFixture
    {
        [Test]
        public void TestIndexer()
        {
            float r = .25f, g = .5f, b = .05f, a = 1.0f;
            Color4D c = new Color4D();
            c[0] = r;
            c[1] = g;
            c[2] = b;
            c[3] = a;
            TestHelper.AssertEquals(r, c[0], "Test Indexer, R");
            TestHelper.AssertEquals(g, c[1], "Test Indexer, G");
            TestHelper.AssertEquals(b, c[2], "Test Indexer, B");
            TestHelper.AssertEquals(a, c[3], "Test Indexer, A");
        }

        [Test]
        public void TestEquals()
        {
            float r1 = .25f, g1 = .1f, b1 = .75f, a1 = 1.0f;
            float r2 = .75f, g2 = 1.0f, b2 = 1.0f, a2 = .5f;

            Color4D c1 = new Color4D(r1, g1, b1, a1);
            Color4D c2 = new Color4D(r1, g1, b1, a1);
            Color4D c3 = new Color4D(r2, g2, b2, a2);

            //Test IEquatable Equals
            Assert.IsTrue(c1.Equals(c2), "Test IEquatable equals");
            Assert.IsFalse(c1.Equals(c3), "Test IEquatable equals");

            //Test object equals override
            Assert.IsTrue(c1.Equals((object) c2), "Tests object equals");
            Assert.IsFalse(c1.Equals((object) c3), "Tests object equals");

            //Test op equals
            Assert.IsTrue(c1 == c2, "Testing OpEquals");
            Assert.IsFalse(c1 == c3, "Testing OpEquals");

            //Test op not equals
            Assert.IsTrue(c1 != c3, "Testing OpNotEquals");
            Assert.IsFalse(c1 != c2, "Testing OpNotEquals");
        }

        [Test]
        public void TestIsBlack()
        {
            Color4D c1 = new Color4D(0, 0, 0, 1.0f);
            Color4D c2 = new Color4D(.25f, 1.0f, .5f, 1.0f) * .002f;
            Color4D c3 = new Color4D(.25f, .65f, 1.0f);

            Assert.IsTrue(c1.IsBlack(), "Testing isBlack");
            Assert.IsTrue(c2.IsBlack(), "Testing isBlack");
            Assert.IsFalse(c3.IsBlack(), "Testing !isBlack");
        }

        [Test]
        public void TestOpAdd()
        {
            float r1 = .5f, g1 = .25f, b1 = .7f, a1 = 1.0f;
            float r2 = .2f, g2 = .1f, b2 = .05f, a2 = .25f;
            float r = r1 + r2;
            float g = g1 + g2;
            float b = b1 + b2;
            float a = a1 + a2;

            Color4D c1 = new Color4D(r1, g1, b1, a1);
            Color4D c2 = new Color4D(r2, g2, b2, a2);
            Color4D c = c1 + c2;

            TestHelper.AssertEquals(r, g, b, a, c, "Testing OpAdd");
        }

        [Test]
        public void TestOpAddValue()
        {
            float r1 = .5f, g1 = .25f, b1 = .7f, a1 = 1.0f;
            float value = .2f;
            float r = r1 + value;
            float g = g1 + value;
            float b = b1 + value;
            float a = a1 + value;

            Color4D c1 = new Color4D(r1, g1, b1, a1);

            //Test left to right
            Color4D c = c1 + value;
            TestHelper.AssertEquals(r, g, b, a, c, "Testing OpAddValue");

            //Test right to left
            c = value + c1;
            TestHelper.AssertEquals(r, g, b, a, c, "Testing OpAddValue");
        }

        [Test]
        public void TestOpSubtract()
        {
            float r1 = .5f, g1 = .25f, b1 = .7f, a1 = 1.0f;
            float r2 = .2f, g2 = .1f, b2 = .05f, a2 = .25f;
            float r = r1 - r2;
            float g = g1 - g2;
            float b = b1 - b2;
            float a = a1 - a2;

            Color4D c1 = new Color4D(r1, g1, b1, a1);
            Color4D c2 = new Color4D(r2, g2, b2, a2);
            Color4D c = c1 - c2;

            TestHelper.AssertEquals(r, g, b, a, c, "Testing OpSubtract");
        }

        [Test]
        public void TestOpSubtractByValue()
        {
            float r1 = .5f, g1 = .25f, b1 = .7f, a1 = 1.0f;
            float value = .2f;
            float r = r1 - value;
            float g = g1 - value;
            float b = b1 - value;
            float a = a1 - value;

            Color4D c1 = new Color4D(r1, g1, b1, a1);

            //Test left to right
            Color4D c = c1 - value;
            TestHelper.AssertEquals(r, g, b, a, c, "Testing OpSubtractValue");

            r = value - r1;
            g = value - g1;
            b = value - b1;
            a = value - a1;

            //Test right to left
            c = value - c1;
            TestHelper.AssertEquals(r, g, b, a, c, "Testing OpSubtractValue");
        }

        [Test]
        public void TestOpMultiply()
        {
            float r1 = .5f, g1 = .25f, b1 = .7f, a1 = 1.0f;
            float r2 = .2f, g2 = .1f, b2 = .05f, a2 = .25f;
            float r = r1 * r2;
            float g = g1 * g2;
            float b = b1 * b2;
            float a = a1 * a2;

            Color4D c1 = new Color4D(r1, g1, b1, a1);
            Color4D c2 = new Color4D(r2, g2, b2, a2);
            Color4D c = c1 * c2;

            TestHelper.AssertEquals(r, g, b, a, c, "Testing OpMultiply");
        }

        [Test]
        public void TestOpMultiplyByScalar()
        {
            float r1 = .5f, g1 = .25f, b1 = .7f, a1 = 1.0f;
            float value = .2f;
            float r = r1 * value;
            float g = g1 * value;
            float b = b1 * value;
            float a = a1 * value;

            Color4D c1 = new Color4D(r1, g1, b1, a1);

            //Test left to right
            Color4D c = c1 * value;
            TestHelper.AssertEquals(r, g, b, a, c, "Testing OpMultiplyByValue");

            //Test right to left
            c = value * c1;
            TestHelper.AssertEquals(r, g, b, a, c, "Testing OpMultiplyByValue");
        }

        [Test]
        public void TestDivide()
        {
            float r1 = .5f, g1 = .25f, b1 = .7f, a1 = 1.0f;
            float r2 = .2f, g2 = .1f, b2 = .05f, a2 = .25f;
            float r = r1 / r2;
            float g = g1 / g2;
            float b = b1 / b2;
            float a = a1 / a2;

            Color4D c1 = new Color4D(r1, g1, b1, a1);
            Color4D c2 = new Color4D(r2, g2, b2, a2);
            Color4D c = c1 / c2;

            TestHelper.AssertEquals(r, g, b, a, c, "Testing OpDivide");
        }

        [Test]
        public void TestDivideByFactor()
        {
            float r1 = .5f, g1 = .25f, b1 = .7f, a1 = 1.0f;
            float value = .2f;
            float r = r1 / value;
            float g = g1 / value;
            float b = b1 / value;
            float a = a1 / value;

            Color4D c1 = new Color4D(r1, g1, b1, a1);

            Color4D c = c1 / value;
            TestHelper.AssertEquals(r, g, b, a, c, "Testing OpDivideByFactor");
        }
    }
}
