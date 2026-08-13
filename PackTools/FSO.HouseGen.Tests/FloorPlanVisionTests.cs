using System;
using Xunit;

namespace FSO.HouseGen.Tests
{
    public class FloorPlanVisionTests
    {
        [Fact]
        public void ParseAndValidate_AcceptsKatFlatJson()
        {
            var json = System.IO.File.ReadAllText(Example("layouts/kat-flat.json"));
            var layout = FloorPlanVision.ParseAndValidate(json);
            Assert.Equal(3, layout.Rooms.Count);
            Assert.Equal(3, layout.Doors.Count);
            Assert.Equal(2, layout.Windows.Count);
            Assert.Equal(77, layout.Size);
        }

        [Fact]
        public void ParseAndValidate_StripsMarkdownFence()
        {
            var inner = System.IO.File.ReadAllText(Example("layouts/one-room.json"));
            var fenced = "Here you go:\n```json\n" + inner + "\n```\n";
            var layout = FloorPlanVision.ParseAndValidate(fenced);
            Assert.Single(layout.Rooms);
            Assert.Equal("the A1 room", layout.Rooms[0].Name);
        }

        [Fact]
        public void ParseAndValidate_RejectsOverlappingRooms()
        {
            var bad =
                @"{ ""Size"": 77, ""Rooms"": [
                    { ""Name"": ""a"", ""X"": 30, ""Y"": 30, ""Width"": 4, ""Height"": 4, ""Floor"": 3 },
                    { ""Name"": ""b"", ""X"": 32, ""Y"": 32, ""Width"": 4, ""Height"": 4, ""Floor"": 3 }
                ], ""Doors"": [], ""Windows"": [] }";
            var ex = Assert.Throws<InvalidOperationException>(() => FloorPlanVision.ParseAndValidate(bad));
            Assert.Contains("overlap", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseAndValidate_RejectsDoorWithNoWall()
        {
            var bad =
                @"{ ""Size"": 77, ""Rooms"": [
                    { ""Name"": ""a"", ""X"": 30, ""Y"": 30, ""Width"": 4, ""Height"": 4, ""Floor"": 3 }
                ], ""Doors"": [ { ""X"": 10, ""Y"": 10, ""Edge"": ""west"" } ], ""Windows"": [] }";
            var ex = Assert.Throws<InvalidOperationException>(() => FloorPlanVision.ParseAndValidate(bad));
            Assert.Contains("no wall", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExtractJsonObject_FindsEmbeddedObject()
        {
            var raw = FloorPlanVision.ExtractJsonObject("prefix {\"Size\":77,\"Rooms\":[]} suffix");
            Assert.StartsWith("{", raw);
            Assert.EndsWith("}", raw);
        }

        private static string Example(string relative)
        {
            // Tests run from bin/Debug/net9.0 — walk up to PackTools/examples.
            var d = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                var path = System.IO.Path.Combine(d.FullName, "examples", relative);
                if (System.IO.File.Exists(path)) return path;
                path = System.IO.Path.Combine(d.FullName, "PackTools", "examples", relative);
                if (System.IO.File.Exists(path)) return path;
                d = d.Parent;
            }
            throw new System.IO.FileNotFoundException("example not found: " + relative);
        }
    }
}
