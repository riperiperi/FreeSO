 /*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.Common.Utils
{
    public class IffPrinter
    {
        private HTMLPrinter Printer;

        public IffPrinter(HTMLPrinter printer){
            this.Printer = printer;
        }

        public void PrintAll(IffFile iff){
            this.Print<SPR>(iff);
            this.Print<SPR2>(iff);
            this.Print<OBJD>(iff);
        }

        public void Print<T>(IffFile iff){
            var type = typeof(T);
            var items = iff.List<T>();

            if (items == null) { return; }

            if (type == typeof(SPR2)){
                Printer.H1("SPR2");
                foreach (var item in items){
                    PrintSPR2((SPR2)(object)item);
                }
            }
            else if (type == typeof(OBJD))
            {
                Printer.H1("OBJD");
                foreach (var item in items){
                    PrintOBJD((OBJD)(object)item);
                }
            }else if (type == typeof(SPR))
            {
                Printer.H1("SPR");
                foreach (var item in items)
                {
                    PrintSPR((SPR)(object)item);
                }
            }
        }

        private void PrintSPR(SPR spr)
        {
            Printer.H2("#" + spr.ChunkID + " (" + spr.ChunkLabel + ")");
            var table = Printer.CreateTable(new string[] { "Index", "Pixel" });
            var frameIndex = 0;
            foreach (var frame in spr.Frames)
            {
                table.AddRow(new object[] { frameIndex, frame });
                frameIndex++;
            }
            Printer.Add(table);
        }

        private void PrintOBJD(OBJD item){
            string[] fieldLabels = null;
            switch (item.Version)
            {
                case 142:
                    fieldLabels = OBJD.VERSION_142_Fields;
                    break;
            }

            Printer.H1(item.ChunkID + " (" + item.ChunkLabel + ") GUID = " + item.GUID.ToString("x") + " Version = " + item.Version);
            var table = Printer.CreateTable(new string[] { "Field", "Value" });
            for (var i = 0; i < item.RawData.Length; i++)
            {
                if (fieldLabels != null && i < fieldLabels.Length)
                {
                    table.AddRow(new object[] { i.ToString() +  " (" + fieldLabels[i] + ")", item.RawData[i].ToString() });
                }
                else
                {
                    table.AddRow(new object[] { i.ToString(), item.RawData[i].ToString() });
                }
            }
            Printer.Add(table);
        }

        private void PrintSPR2(SPR2 spr)
        {
            Printer.H2("#" + spr.ChunkID + " (" + spr.ChunkLabel + ")");
            var table = Printer.CreateTable(new string[] { "Index", "Pixel" });
            var frameIndex = 0;
            foreach (var frame in spr.Frames){
                table.AddRow(new object[] { frameIndex, frame });
                frameIndex++;
            }
            Printer.Add(table);
        }


    }
}
*/