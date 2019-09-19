using System;
using System.Collections.Generic;
using System.Linq;
using IniParser;
using Microsoft.Xna.Framework.Content.Pipeline;
using MSDFData;

namespace MSDFExtension
{   
    [ContentImporter(".ini", DisplayName = "Field Font Importer", DefaultProcessor = "FieldFontProcessor")]
    public class FieldFontImporter : ContentImporter<FontDescription>
    {     
        public override FontDescription Import(string filename, ContentImporterContext context)
        {
            return Parse(filename);            
        }

        private static FontDescription Parse(string filename)
        {            
            var parser = new FileIniDataParser();
            var data = parser.ReadFile(filename, System.Text.Encoding.UTF8);

            var fontSection = data.Sections["font"];
            var path = fontSection["path"];

            var characterSection = data.Sections["characters"] ?? fontSection;
            var characters = ParseRanges(characterSection["ranges"]);

            return new FontDescription(path, characters);
        }


        private static char[] ParseRanges(string ranges)
        {
            var tuples = ParseTuples(ranges);

            var characters = new HashSet<char>();
            foreach (var tuple in tuples)
            {
                // Every tuple should consist of two characters seperated by a comma
                var parts = tuple.Split(',');
                if (parts.Length != 2)
                {
                    throw new Exception($"Unexpected number of tuple elements in tuple: {tuple}");
                }            

                if (parts[0].Length != 1 || parts[1].Length != 1)
                {
                    throw new Exception($"A tuple can only contain two characters seperated by a comma: {tuple}");
                }

                // Compute the entire character range from the two extremes (inclusive)
                var start = parts[0][0];
                var end = parts[1][0];
                
                for (int i = start; i <= end; i++)
                {
                    characters.Add((char) i);
                }

            }

            return characters.ToArray();
        }

        /// <summary>
        /// Parses tuples, and returns an enumerable with the contents of each tuple (so without the braces)
        /// </summary>        
        private static IEnumerable<string> ParseTuples(string ranges)
        {
            var tuples = new List<string>();

            // -1 signals the we have not seen the opening brace of the tuple yet
            var start = -1;
            for (var i = 0; i < ranges.Length; i++)
            {
                var c = ranges[i];
                if (start > -1)
                {                    
                    if (c == ')')
                    {
                        var length = i - start - 1;
                        if (length < 1)
                        {
                            throw new Exception($"Empty tuple at position {start}");
                        }
                        tuples.Add(ranges.Substring(start + 1, length));
                        start = -1;
                    }                    
                    else  if (c == '(')
                    {
                        throw new Exception(
                            $"Unexpected character '(', tuple was already openened at position {start}");
                    }                          
                }
                else if (c == '(')
                {
                    start = i;
                }
            }

            return tuples;
        }
    }

}
