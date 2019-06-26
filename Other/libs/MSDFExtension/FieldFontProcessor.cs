using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using MSDFData;
using RoyT.TrueType.Helpers;

namespace MSDFExtension
{
    
    [ContentProcessor(DisplayName = "Field Font Processor")]
    public class FieldFontProcessor : ContentProcessor<FontDescription, FieldFont>
    {        
        [DisplayName("msdfgen path")]
        [Description("Path to the msdfgen binary used to generate the multi-spectrum signed distance field")]
        [DefaultValue("msdfgen.exe")]
        public virtual string ExternalPath { get; set; } = "msdfgen.exe";


        [DisplayName("resolution")]
        [Description("Resolution of the the texture to store a single glyph in")]
        [DefaultValue(32)]
        public virtual uint Resolution { get; set; } = 32;

        [DisplayName("range")]
        [Description("Distance field range, in pixels in the output texture")]
        [DefaultValue(4)]
        public virtual uint Range { get; set; } = 4;

        private AtlasBuilder Atlas;

        public override FieldFont Process(FontDescription input, ContentProcessorContext context)
        {
            var msdfgen = Path.Combine(Directory.GetCurrentDirectory(), this.ExternalPath);
            var objPath = Path.Combine(Directory.GetCurrentDirectory(), "obj");

            if (File.Exists(msdfgen))
            {
                var glyphs = new FieldGlyph[input.Characters.Count];

                Atlas = new AtlasBuilder(input.Characters.Count, (int)Resolution);
                // Generate a distance field for each character using msdfgen
                Parallel.For(
                    0,
                    input.Characters.Count,
                    i =>
                    {
                        var c = input.Characters[i];
                        glyphs[i] = CreateFieldGlyphForCharacter(c, input, msdfgen, objPath);                                               
                    });
                
                var kerning = ReadKerningInformation(input.Path, input.Characters);
                return new FieldFont(input.Path, glyphs.Where(x => x != null).ToArray(), kerning, this.Range, Atlas.Finish());
            }

            throw new FileNotFoundException(
                "Could not find msdfgen. Check your content processor parameters",
                msdfgen);
        }

        private FieldGlyph CreateFieldGlyphForCharacter(char c, FontDescription input, string msdfgen, string objPath)
        {            
            var metrics = CreateDistanceFieldForCharacter(input, msdfgen, objPath, c);
            var path = GetOuputPath(objPath, input, c);
            int atlasIndex = 0;
            try
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    atlasIndex = Atlas.AddChar(c, stream);
            } catch (Exception)
            {
                return null;
            }
            File.Delete(path);
            var glyph = new FieldGlyph(c, atlasIndex, metrics);

            return glyph;
        }    

        private Metrics CreateDistanceFieldForCharacter(FontDescription font, string msdfgen, string objPath, char c)
        {
            var outputPath = GetOuputPath(objPath, font, c);
            var res = this.Resolution;
            var startInfo = new ProcessStartInfo(msdfgen)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                Arguments = $"-font \"{font.Path}\" {(int)c} -o \"{outputPath}\" -size {res} {res} -pxrange {this.Range} -autoframe -printmetrics"                
            };
          
            var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Could not start msdfgen.exe");
            }

            var output = process.StandardOutput.ReadToEnd();
            return ParseOutput(output);            
        }

        private static Metrics ParseOutput(string output)
        {
            var advance = 0.0f;
            var scale = 0.0f;
            var translation = Vector2.Zero;

            foreach (var line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                ParseLine(line, "advance = ", FloatHelper.ParseInvariant, ref advance);
                ParseLine(line, "scale = ", FloatHelper.ParseInvariant, ref scale);
                ParseLine(line, "translate = ", ParseVector2, ref translation);
            }

            return new Metrics(advance, scale, translation);
        }

        private static string GetOuputPath(string objPath, FontDescription font, char c)
        {
            var name = Path.GetFileNameWithoutExtension(font.Path);
            return Path.Combine(objPath, $"{name}-{(int)c}.bmp");
        }

        private static Vector2 ParseVector2(string text)
        {
            var args = text.Split(',');
            return new Vector2(FloatHelper.ParseInvariant(args[0]), FloatHelper.ParseInvariant(args[1]));
        }
        
        private static void ParseLine<T>(string line, string match, Func<string ,T> resultParser, ref T result)
        {
            if (line.StartsWith(match, StringComparison.InvariantCultureIgnoreCase))
            {
                var value = line.Substring(match.Length).Trim();
                result = resultParser(value);                
            }            
        }

        private static List<KerningPair> ReadKerningInformation(string path, IReadOnlyList<char> characters)
        {
            var pairs = new List<KerningPair>();

            var font = RoyT.TrueType.TrueTypeFont.FromFile(path);

            foreach (var left in characters)
            {
                foreach (var right in characters)
                {
                    var kerning = KerningHelper.GetHorizontalKerning(left, right, font);                          
                    if (kerning > 0 || kerning < 0)
                    {
                        // Scale the kerning by the same factor MSDFGEN scales it
                        pairs.Add(new KerningPair(left, right, kerning / 64.0f));
                    }
                }
            }

            return pairs;
        }
    }   
}