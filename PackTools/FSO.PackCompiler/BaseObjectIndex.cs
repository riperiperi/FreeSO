using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Name search over the base game's object table, so an authoring agent can find a real
    /// GUID to clone appearance from instead of guessing one.
    ///
    /// Why this exists: appearance.clone_from_guid needs a GUID that actually exists, and an
    /// agent has no way to discover one. Every model observed so far either invented a
    /// plausible-looking GUID (which fails the clone) or dropped appearance entirely (which
    /// compiles clean and renders an invisible object). Guessing was the only affordance, so
    /// guessing is what happened.
    ///
    /// Reads packingslips/objecttable.xml directly, the same file AppearanceCloner resolves
    /// GUIDs through, so a hit here is guaranteed to be clonable.
    /// </summary>
    public static class BaseObjectIndex
    {
        public class Entry
        {
            public uint Guid;
            public string Name;      // human-readable, the o= attribute
            public string File;      // .iff basename, the n= attribute
            public int Score;
        }

        // <I g="0xA4B5D104" n="accessoryrack" o="Accessory Rack - Moderate" m="0" .../>
        private static readonly Regex Row = new Regex(
            "<I\\s+g=\"(?<g>0x[0-9A-Fa-f]+)\"\\s+n=\"(?<n>[^\"]*)\"\\s+o=\"(?<o>[^\"]*)\"",
            RegexOptions.Compiled);

        public static string TablePath(string gameDir) =>
            Path.Combine(gameDir, "packingslips", "objecttable.xml");

        public static List<Entry> Load(string gameDir)
        {
            var entries = new List<Entry>();
            foreach (var line in File.ReadLines(TablePath(gameDir)))
            {
                var m = Row.Match(line);
                if (!m.Success) continue;
                entries.Add(new Entry
                {
                    Guid = Convert.ToUInt32(m.Groups["g"].Value, 16),
                    Name = m.Groups["o"].Value,
                    File = m.Groups["n"].Value,
                });
            }
            return entries;
        }

        /// <summary>
        /// Ranked substring search over the display name, then the file name. Ranking is
        /// deliberately crude — the caller only needs a handful of plausible candidates to
        /// choose between, not relevance tuning.
        /// </summary>
        public static List<Entry> Search(IEnumerable<Entry> entries, string query, int limit)
        {
            var terms = query.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.ToLowerInvariant()).ToArray();
            if (terms.Length == 0) return new List<Entry>();

            var hits = new List<Entry>();
            foreach (var e in entries)
            {
                var name = e.Name.ToLowerInvariant();
                var file = e.File.ToLowerInvariant();

                var score = 0;
                var missed = false;
                foreach (var term in terms)
                {
                    if (name == term) score += 100;
                    else if (name.Split(' ', '-').Contains(term)) score += 50;
                    else if (name.Contains(term)) score += 25;
                    else if (file.Contains(term)) score += 10;
                    else { missed = true; break; }
                }
                if (missed) continue;

                // Prefer shorter names: "Chair" should outrank "Chair - Deluxe Recliner"
                // when the query was just "chair".
                score -= name.Length / 8;
                hits.Add(new Entry { Guid = e.Guid, Name = e.Name, File = e.File, Score = score });
            }

            return hits.OrderByDescending(h => h.Score)
                .ThenBy(h => h.Name, StringComparer.Ordinal)
                .Take(limit)
                .ToList();
        }
    }
}
