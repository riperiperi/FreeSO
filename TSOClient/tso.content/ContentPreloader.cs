using FSO.Client.Utils;
using FSO.Common.Content;
using FSO.Content.Model;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace FSO.Content
{
    public class ContentPreloader
    {
        private List<IContentReference> Pending = new List<IContentReference>();
        private int Total;
        private int Completed;
        private GraphicsDevice Graphics;

        public void Add(IContentReference reference)
        {
            Pending.Add(reference);
        }

        public void Add(IEnumerable<IContentReference> reference)
        {
            Pending.AddRange(reference);
        }

        public void Preload(GraphicsDevice gd)
        {
            Graphics = gd;

            Total = Pending.Count;
            Completed = 0;

            Thread T = new Thread(new ThreadStart(LoadContent));
            //TODO: This should only be set to speed up debug
            //T.Priority = ThreadPriority.AboveNormal;
            T.Start();

            //LoadContent();
        }

        public double Progress
        {
            get
            {
                return ((double)Completed) / ((double)Total);
            }
        }

        public bool IsLoading
        {
            get
            {
                return Completed < Total;
            }
        }

        private void LoadContent()
        {
            Pending.Shuffle();
            
            while (Pending.Count > 0)
            {
                var item = Pending[0];
                Pending.RemoveAt(0);
                var value = item.GetGeneric();

                /*if(value is ITextureRef)
                {
                    ((ITextureRef)value).Get(Graphics);
                }*/
                
                Completed++;
            }
        }
    }
}
