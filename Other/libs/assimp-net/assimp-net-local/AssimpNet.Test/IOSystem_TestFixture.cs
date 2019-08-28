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
using NUnit.Framework;

namespace Assimp.Test
{
    [TestFixture]
    public class IOSystem_TestFixture
    {
        [Test]
        public void TestMultiSearchDirectoryLoad()
        {
            String fileName = "fenris.lws";
            String[] searchPaths = { Path.Combine(TestHelper.RootPath, "TestFiles\\fenris\\scenes"), Path.Combine(TestHelper.RootPath, "TestFiles\\fenris\\objects") };
            FileIOSystem ioSystem = new FileIOSystem(searchPaths);

            AssimpContext importer = new AssimpContext();
            importer.SetIOSystem(ioSystem);

            //None, using the "target high quality flags caused a crash with this model.
            Scene scene = importer.ImportFile(fileName, PostProcessSteps.None);
            Assert.IsNotNull(scene);
        }

        [Test]
        public void TestMultiSearchDirectoryConvert()
        {
            String fileName = Path.Combine(TestHelper.RootPath, "TestFiles\\fenris\\scenes\\fenris.lws");
            String[] searchPaths = { Path.Combine(TestHelper.RootPath, "TestFiles\\fenris\\objects") };
            FileIOSystem ioSystem = new FileIOSystem(searchPaths);

            AssimpContext importer = new AssimpContext();
            importer.SetIOSystem(ioSystem);

            //Output path has to be specified fully, since we may be creating the file
            String outputPath = Path.Combine(TestHelper.RootPath, "TestFiles\\fenris\\fenris2.obj");
            importer.ConvertFromFileToFile(fileName, PostProcessSteps.None, outputPath, "obj", PostProcessSteps.None);
        }

        [Test]
        public void TestIOSystemError()
        {
            String fileName = "duckduck.dae"; //GOOSE!
            String[] searchPaths = { Path.Combine(TestHelper.RootPath, "TestFiles") };
            FileIOSystem ioSystem = new FileIOSystem(searchPaths);

            AssimpContext importer = new AssimpContext();
            importer.SetIOSystem(ioSystem);
            Assert.Throws<AssimpException>(delegate()
            {
                importer.ImportFile(fileName, PostProcessSteps.None);
            });
        }
    }
}
