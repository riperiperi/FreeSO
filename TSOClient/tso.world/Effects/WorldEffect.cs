using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Effects
{
    public class WorldEffect : Effect
    {
        protected virtual Type TechniqueType
        {
            get { return typeof(LightingEmptyTechniques); }
        }

        protected EffectTechnique[] IndexedTechniques;

        protected WorldEffect(Effect cloneSource) : base(cloneSource)
        {
            PrepareParams();
        }

        public WorldEffect(GraphicsDevice graphicsDevice, byte[] effectCode) : base(graphicsDevice, effectCode)
        {
            PrepareParams();
        }

        public WorldEffect(GraphicsDevice graphicsDevice, byte[] effectCode, int index, int count) : base(graphicsDevice, effectCode, index, count)
        {
            PrepareParams();
        }

        protected virtual void PrepareParams()
        {
            PrepareTechniques();
        }

        public void PrepareTechniques()
        {
            var values = Enum.GetValues(TechniqueType);
            IndexedTechniques = new EffectTechnique[values.Length];
            int i = 0;
            foreach (var value in values)
            {
                IndexedTechniques[i++] = Techniques[Enum.GetName(TechniqueType, value)];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTechnique(int type)
        {
            CurrentTechnique = IndexedTechniques[type];
        }
    }
}
