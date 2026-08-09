using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Buy Mode catalog entries for downloaded objects. The engine only shows a
    /// Content/Objects/*.iff object in Buy Mode when {ContentDir}/Objects/catalog_downloads.xml
    /// has a matching &lt;P g="GUID" s="category" p="price" t="tags" n="name" /&gt; entry
    /// (tso.content/WorldObjectCatalog.cs Init()).
    /// </summary>
    public class CatalogEntry
    {
        public uint Guid;
        public sbyte Category;
        public uint Price;
        public string Name;
        public string Tags; // comma separated, null = no t attribute

        public static CatalogEntry For(PackObject obj)
        {
            return new CatalogEntry
            {
                Guid = obj.Guid,
                // category is optional in the schema; uncategorised objects land in misc
                Category = (obj.Category != null) ? Names.Categories[obj.Category] : Names.Categories["misc"],
                Price = (uint)obj.Price,
                Name = obj.Name ?? "",
                Tags = (obj.Tags.Count > 0) ? string.Join(", ", obj.Tags) : null,
            };
        }
    }

    public static class CatalogXml
    {
        /// <summary>Writes a standalone fragment file containing one P element per object.</summary>
        public static void WriteFragment(string path, IEnumerable<CatalogEntry> entries)
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement("Catalog");
            doc.AppendChild(root);
            foreach (var entry in entries) root.AppendChild(CreateP(doc, entry));
            Save(doc, path);
        }

        /// <summary>
        /// Idempotent upsert into a catalog_downloads.xml: entries are matched by their g
        /// attribute (GUID); existing entries are updated in place, others left untouched.
        /// Creates the file with a Catalog root when missing.
        /// </summary>
        public static void Upsert(string path, IEnumerable<CatalogEntry> entries)
        {
            var doc = new XmlDocument();
            if (File.Exists(path)) doc.Load(path);
            else doc.AppendChild(doc.CreateElement("Catalog"));
            var root = doc.DocumentElement;

            var existing = doc.GetElementsByTagName("P").Cast<XmlElement>().ToList();
            foreach (var entry in entries)
            {
                var match = existing.FirstOrDefault(p => MatchesGuid(p, entry.Guid));
                if (match != null) SetAttributes(match, entry);
                else root.AppendChild(CreateP(doc, entry));
            }
            Save(doc, path);
        }

        private static bool MatchesGuid(XmlElement p, uint guid)
        {
            var g = p.GetAttribute("g");
            if (g == "") return false;
            try
            {
                return Convert.ToUInt32(g, 16) == guid;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static XmlElement CreateP(XmlDocument doc, CatalogEntry entry)
        {
            var p = doc.CreateElement("P");
            SetAttributes(p, entry);
            return p;
        }

        private static void SetAttributes(XmlElement p, CatalogEntry entry)
        {
            p.SetAttribute("g", entry.Guid.ToString("X8"));
            p.SetAttribute("s", entry.Category.ToString());
            p.SetAttribute("p", entry.Price.ToString());
            if (entry.Tags != null) p.SetAttribute("t", entry.Tags);
            else p.RemoveAttribute("t");
            p.SetAttribute("n", entry.Name);
        }

        private static void Save(XmlDocument doc, string path)
        {
            using (var writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true }))
                doc.Save(writer);
        }
    }
}
