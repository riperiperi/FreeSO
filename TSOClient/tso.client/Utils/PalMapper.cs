using FSO.Files;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Client.Utils
{
    public class PalMapper
    {
        private Color Average(List<Color> cols)
        {
            Vector4 sum = new Vector4();
            foreach (var col in cols)
            {
                sum += col.ToVector4();
            }
            sum /= cols.Count;
            return new Color(sum);
        }

        public void DoIt()
        {
            Texture2D from;
            Texture2D to;
            using (var fromS = File.Open(@"C:\Users\Rhys\Desktop\mapfrom.png", FileMode.Open, FileAccess.Read, FileShare.Read)) {
                from = ImageLoader.FromStream(GameFacade.GraphicsDevice, fromS);
            }
            using (var toS = File.Open(@"C:\Users\Rhys\Desktop\mapto2.png", FileMode.Open, FileAccess.Read, FileShare.Read)) {
                to = ImageLoader.FromStream(GameFacade.GraphicsDevice, toS);
            }

            int[] fromDat = new int[from.Width*from.Height];
            Color[] toDat = new Color[to.Width * to.Height];

            from.GetData(fromDat);
            to.GetData(toDat);

            Dictionary<int, List<Color>> colLists = new Dictionary<int, List<Color>>();
            for (int i=0; i<fromDat.Length; i++)
            {
                var pal = fromDat[i];
                List<Color> list;
                if (!colLists.TryGetValue(pal, out list))
                {
                    list = new List<Color>();
                    colLists.Add(pal, list);
                }

                list.Add(toDat[i]);
            }

            var newPal = colLists.Select(x => new KeyValuePair<int, Color>(x.Key, Average(x.Value))).ToDictionary(x => x.Key, x => x.Value);

            for (int i = 0; i < fromDat.Length; i++)
            {
                var pal = fromDat[i];
                var col = newPal[pal];
                toDat[i] = col;
            }

            to.SetData(toDat);

            using (var toS = File.Open(@"C:\Users\Rhys\Desktop\result.png", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                to.SaveAsPng(toS, to.Width, to.Height);
            }
            
        }
    }
}
