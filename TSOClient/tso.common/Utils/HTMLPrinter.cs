//disabled for now so FSO.Files can reference this project.

 /*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Common.Utils
{
    /// <summary>
    /// This tool helps export internal structures such as floor catalogs,
    /// wall catalogs etc to a html file to check that file parsing is working correctly
    /// </summary>
    public class HTMLPrinter : IDisposable
    {
        private List<object> sections = new List<object>();
        private Dictionary<string, string> patterns = new Dictionary<string, string>();
        private string dir;
        private string id;
        private GraphicsDevice Gd;

        public HTMLPrinter(GraphicsDevice gd, string directory, string id)
        {
            this.Gd = gd;
            this.dir = directory;
            this.id = id;

            sections.Add("<link rel=\"stylesheet\" type=\"text/css\" href=\"main.css\"></link>");

            //Add the default patterns
            patterns.Add("h1", "<h1>{0}</h1>");
            patterns.Add("h2", "<h2>{0}</h2>");
            patterns.Add("h3", "<h3>{0}</h3>");
        }

        public void H1(string text){
            Print("h1", text);
        }

        public void H2(string text){
            Print("h2", text);
        }

        public void H3(string text){
            Print("h3", text);
        }

        public Table CreateTable()
        {
            return new Table();
        }

        public void Add(object item){
            this.sections.Add(item);
        }

        public Table CreateTable(params string[] columns)
        {
            var t = new Table();
            foreach (var col in columns)
            {
                t.WithColumn(new TableColumn(col));
            }
            return t;
        }

        public DataTable<T> AddDataTable<T>(IEnumerable<T> values)
        {
            var table = CreateDataTable<T>(values);
            sections.Add(table);
            return table;
        }

        public DataTable<T> CreateDataTable<T>(IEnumerable<T> values)
        {
            return new DataTable<T>(values);
        }

        private string ObjectToString(object obj)
        {
            if (obj != null)
            {
                return obj.ToString();
            }
            return string.Empty;
        }


        public void Print(string pattern, params object[] args)
        {
            var value = string.Format(patterns[pattern], args);
            sections.Add(value);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Directory.CreateDirectory(Path.Combine(dir, id + "_files"));

            var sb = new StringBuilder();

            foreach (var item in sections)
            {
                AppendItem(sb, item);
            }

            File.WriteAllText(Path.Combine(dir, id + ".html"), sb.ToString());
        }

        #endregion


        public void AppendItem(StringBuilder sb, object item)
        {
            if (item is IHTMLAppender)
            {
                ((IHTMLAppender)item).Append(this, sb);
            }else if(item is SPR2Frame){

                try
                {
                    var path = ExportSpriteFrame((SPR2Frame)item);
                    sb.Append("<img src='" + path + "'></img>");
                }
                catch (Exception)
                {
                    sb.Append("Failed to export");
                }
            }
            else if (item is SPRFrame)
            {
                try
                {
                    var path = ExportSpriteFrame((SPRFrame)item);
                    sb.Append("<img src='" + path + "'></img>");
                }
                catch (Exception)
                {
                    sb.Append("Failed to export");
                }
            }
            else if (item != null)
            {
                sb.Append(item.ToString());
            }

            /**else if(item is SPR){

                var t = CreateTable()
                    .WithColumn(new TableColumn("Sprite", 2));

                var sprP = (SPR)item;
                for (var i = 0; i < sprP.FrameCount; i++){
                    try
                    {
                        var frame = sprP.GetFrame(i);
                        t.AddRow((i + 1), frame);
                    }
                    catch (Exception ex)
                    {
                        t.AddRow((i + 1), "Failed to export frame");
                    }
                }

                AppendItem(sb, t);

            }**/

            /**
        }

        private string ExportSpriteFrame(SPRFrame frame)
        {
            var texture = frame.GetTexture(this.Gd);

            var temp = Path.GetTempFileName();
            texture.SaveAsPng(new FileStream(temp, FileMode.OpenOrCreate), texture.Width, texture.Height);

            var hash = FileUtils.ComputeMD5(temp);
            var filename = id + "_files/" + hash + ".png";
            var newDest = Path.Combine(dir, filename);
            if (File.Exists(newDest))
            {
                File.Delete(temp);
            }
            else
            {
                File.Move(temp, newDest);
            }

            return filename;
        }

        private string ExportSpriteFrame(SPR2Frame frame)
        {
            var texture = frame.GetTexture(this.Gd);
            
            var temp = Path.GetTempFileName();
            texture.SaveAsPng(new FileStream(temp, FileMode.OpenOrCreate), texture.Width, texture.Height);

            var hash = FileUtils.ComputeMD5(temp);
            var filename = id + "_files/" + hash + ".png";
            var newDest = Path.Combine(dir, filename);
            if (File.Exists(newDest))
            {
                File.Delete(temp);
            }
            else
            {
                File.Move(temp, newDest);
            }

            return filename;
        }


    }

    public class TableColumn
    {
        public TableColumn(string header)
        {
            this.Header = header;
        }
        public TableColumn(string header, int span)
        {
            this.Header = header;
            this.Span = span;
        }

        public string Header;
        public int Span;
    }

    public class Table : IHTMLAppender
    {
        private List<TableColumn> Columns = new List<TableColumn>();
        private List<object[]> Rows = new List<object[]>();

        public Table WithColumn(TableColumn col)
        {
            Columns.Add(col);
            return this;
        }

        public Table AddRow(params object[] values)
        {
            Rows.Add(values);
            return this;
        }

        #region IHTMLAppender Members

        public void Append(HTMLPrinter printer, StringBuilder sb)
        {
            sb.Append("<table border='1'>");

            if (Columns.Count > 0)
            {
                sb.Append("<tr>");
                foreach (var col in Columns)
                {
                    sb.Append("<th colspan='" + col.Span + "'>" + col.Header + "</th>");
                }
                sb.Append("</tr>");
            }

            foreach (var item in Rows)
            {
                sb.Append("<tr>");
                foreach(var col in item){
                    sb.Append("<td>");
                    printer.AppendItem(sb, col);
                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</table>");
        }

        #endregion
    }


    public class DataTable<T> : IHTMLAppender
    {
        private IEnumerable<T> Items;
        private List<DataTableColumn<T>> Columns;

        public DataTable(IEnumerable<T> items)
        {
            this.Items = items;
            this.Columns = new List<DataTableColumn<T>>();
        }


        public DataTable<T> WithColumn(string label, Func<T, object> value)
        {
            Columns.Add(new DataTableColumn<T> { Heading = label, Value = value });
            return this;
        }

        #region IHTMLAppender Members

        public void Append(HTMLPrinter printer, StringBuilder sb)
        {
            sb.Append("<table border='1'>");
            sb.Append("<tr>");
            foreach (var col in Columns)
            {
                sb.Append("<td>" + col.Heading + "</td>");
            }
            sb.Append("</tr>");

            foreach (var item in Items)
            {
                sb.Append("<tr>");
                foreach (var col in Columns)
                {
                    sb.Append("<th>");
                    printer.AppendItem(sb, col.Value(item));
                    sb.Append("</th>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</table>");
        }

        #endregion
    }

    public interface IHTMLAppender
    {
        void Append(HTMLPrinter printer, StringBuilder sb);
    }

    public class DataTableColumn<T> {
        public string Heading;
        public Func<T, object> Value;
    }
}
*/