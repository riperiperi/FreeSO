using System;
using System.Collections.Generic;
using System.Drawing;
using SimplePaletteQuantizer.ColorCaches;
using SimplePaletteQuantizer.ColorCaches.Octree;

namespace SimplePaletteQuantizer.Quantizers.OptimalPalette
{
    public class OptimalPaletteQuantizer : BaseColorCacheQuantizer
    {
        #region | Fields |

        private static readonly List<Color> OptimalPalette;

        #endregion

        #region | Constructors |

        /// <summary>
        /// Initializes the <see cref="OptimalPaletteQuantizer"/> class.
        /// </summary>
        static OptimalPaletteQuantizer()
        {
            OptimalPalette = new List<Color>(256);

            Register(000, 000, 000); Register(000, 000, 102); Register(000, 000, 204); Register(000, 023, 051); 
            Register(000, 023, 153); Register(000, 023, 255); Register(000, 046, 000); Register(000, 046, 102); 
            Register(000, 046, 204); Register(000, 069, 051); Register(000, 069, 153); Register(000, 069, 255);
            Register(000, 092, 000); Register(000, 092, 102); Register(000, 092, 204); Register(000, 115, 051);
            Register(000, 115, 153); Register(000, 115, 255); Register(000, 139, 000); Register(000, 139, 102);
            Register(000, 139, 204); Register(000, 162, 051); Register(000, 162, 153); Register(000, 162, 255);
            Register(000, 185, 000); Register(000, 185, 102); Register(000, 185, 204); Register(000, 208, 051);
            Register(000, 208, 153); Register(000, 208, 255); Register(000, 231, 000); Register(000, 231, 102);
            Register(000, 231, 204); Register(000, 255, 051); Register(000, 255, 153); Register(000, 255, 255);
            Register(042, 000, 051); Register(042, 000, 153); Register(042, 000, 255); Register(042, 023, 000);
            Register(042, 023, 102); Register(042, 023, 204); Register(042, 046, 051); Register(042, 046, 153);
            Register(042, 046, 255); Register(042, 069, 000); Register(042, 069, 102); Register(042, 069, 204);
            Register(042, 092, 051); Register(042, 092, 153); Register(042, 092, 255); Register(042, 115, 000);
            Register(042, 115, 102); Register(042, 115, 204); Register(042, 139, 051); Register(042, 139, 153);
            Register(042, 139, 255); Register(042, 162, 000); Register(042, 162, 102); Register(042, 162, 204);
            Register(042, 185, 051); Register(042, 185, 153); Register(042, 185, 255); Register(042, 208, 000);
            Register(042, 208, 102); Register(042, 208, 204); Register(042, 231, 051); Register(042, 231, 153);
            Register(042, 231, 255); Register(042, 255, 000); Register(042, 255, 102); Register(042, 255, 204);
            Register(085, 000, 000); Register(085, 000, 102); Register(085, 000, 204); Register(085, 023, 051);
            Register(085, 023, 153); Register(085, 023, 255); Register(085, 046, 000); Register(085, 046, 102);
            Register(085, 046, 204); Register(085, 069, 051); Register(085, 069, 153); Register(085, 069, 255);
            Register(085, 092, 000); Register(085, 092, 102); Register(085, 092, 204); Register(085, 115, 051);
            Register(085, 115, 153); Register(085, 115, 255); Register(085, 139, 000); Register(085, 139, 102);
            Register(085, 139, 204); Register(085, 162, 051); Register(085, 162, 153); Register(085, 162, 255);
            Register(085, 185, 000); Register(085, 185, 102); Register(085, 185, 204); Register(085, 208, 051);
            Register(085, 208, 153); Register(085, 208, 255); Register(085, 231, 000); Register(085, 231, 102);
            Register(085, 231, 204); Register(085, 255, 051); Register(085, 255, 153); Register(085, 255, 255);
            Register(127, 000, 051); Register(127, 000, 153); Register(127, 000, 255); Register(127, 023, 000);
            Register(127, 023, 102); Register(127, 023, 204); Register(127, 046, 051); Register(127, 046, 153);
            Register(127, 046, 255); Register(127, 069, 000); Register(127, 069, 102); Register(127, 069, 204);
            Register(127, 092, 051); Register(127, 092, 153); Register(127, 092, 255); Register(127, 115, 000);
            Register(127, 115, 102); Register(127, 115, 204); Register(127, 139, 051); Register(127, 139, 153);
            Register(127, 139, 255); Register(127, 162, 000); Register(127, 162, 102); Register(127, 162, 204);
            Register(127, 185, 051); Register(127, 185, 153); Register(127, 185, 255); Register(127, 208, 000);
            Register(127, 208, 102); Register(127, 208, 204); Register(127, 231, 051); Register(127, 231, 153);
            Register(127, 231, 255); Register(127, 255, 000); Register(127, 255, 102); Register(127, 255, 204);
            Register(170, 000, 000); Register(170, 000, 102); Register(170, 000, 204); Register(170, 023, 051);
            Register(170, 023, 153); Register(170, 023, 255); Register(170, 046, 000); Register(170, 046, 102);
            Register(170, 046, 204); Register(170, 069, 051); Register(170, 069, 153); Register(170, 069, 255);
            Register(170, 092, 000); Register(170, 092, 102); Register(170, 092, 204); Register(170, 115, 051);
            Register(170, 115, 153); Register(170, 115, 255); Register(170, 139, 000); Register(170, 139, 102);
            Register(170, 139, 204); Register(170, 162, 051); Register(170, 162, 153); Register(170, 162, 255);
            Register(170, 185, 000); Register(170, 185, 102); Register(170, 185, 204); Register(170, 208, 051);
            Register(170, 208, 153); Register(170, 208, 255); Register(170, 231, 000); Register(170, 231, 102);
            Register(170, 231, 204); Register(170, 255, 051); Register(170, 255, 153); Register(170, 255, 255);
            Register(212, 000, 051); Register(212, 000, 153); Register(212, 000, 255); Register(212, 023, 000);
            Register(212, 023, 102); Register(212, 023, 204); Register(212, 046, 051); Register(212, 046, 153);
            Register(212, 046, 255); Register(212, 069, 000); Register(212, 069, 102); Register(212, 069, 204);
            Register(212, 092, 051); Register(212, 092, 153); Register(212, 092, 255); Register(212, 115, 000);
            Register(212, 115, 102); Register(212, 115, 204); Register(212, 139, 051); Register(212, 139, 153);
            Register(212, 139, 255); Register(212, 162, 000); Register(212, 162, 102); Register(212, 162, 204);
            Register(212, 185, 051); Register(212, 185, 153); Register(212, 185, 255); Register(212, 208, 000);
            Register(212, 208, 102); Register(212, 208, 204); Register(212, 231, 051); Register(212, 231, 153);
            Register(212, 231, 255); Register(212, 255, 000); Register(212, 255, 102); Register(212, 255, 204);
            Register(255, 000, 000); Register(255, 000, 102); Register(255, 000, 204); Register(255, 023, 051);
            Register(255, 023, 153); Register(255, 023, 255); Register(255, 046, 000); Register(255, 046, 102);
            Register(255, 046, 204); Register(255, 069, 051); Register(255, 069, 153); Register(255, 069, 255);
            Register(255, 092, 000); Register(255, 092, 102); Register(255, 092, 204); Register(255, 115, 051);
            Register(255, 115, 153); Register(255, 115, 255); Register(255, 139, 000); Register(255, 139, 102);
            Register(255, 139, 204); Register(255, 162, 051); Register(255, 162, 153); Register(255, 162, 255);
            Register(255, 185, 000); Register(255, 185, 102); Register(255, 185, 204); Register(255, 208, 051);
            Register(255, 208, 153); Register(255, 208, 255); Register(255, 231, 000); Register(255, 231, 102);
            Register(255, 231, 204); Register(255, 255, 051); Register(255, 255, 153); Register(255, 255, 255);
            Register(204, 204, 204); Register(153, 153, 153); Register(102, 102, 102); Register(051, 051, 051);
        }

        private static void Register(Int32 red, Int32 green, Int32 blue)
        {
            Color color = Color.FromArgb(255, red, green, blue);
            OptimalPalette.Add(color);
        }

        #endregion

        #region << BaseColorCacheQuantizer >>

        /// <summary>
        /// See <see cref="BaseColorCacheQuantizer.OnCreateDefaultCache"/> for more details.
        /// </summary>
        protected override IColorCache OnCreateDefaultCache()
        {
            // use OctreeColorCache best performance/quality
            return new OctreeColorCache();
        }

        /// <summary>
        /// See <see cref="BaseColorCacheQuantizer.OnGetPaletteToCache"/> for more details.
        /// </summary>
        protected override List<Color> OnGetPaletteToCache(Int32 colorCount)
        {
            // otherwise -> job done, luckily we already have one.. yeah, you guessed correctly -> Optimal Palette (TM)
            return OptimalPalette;
        }

        #endregion

        #region << IColorQuantizer >>

        /// <summary>
        /// See <see cref="IColorQuantizer.AllowParallel"/> for more details.
        /// </summary>
        public override Boolean AllowParallel
        {
            get { return true; }
        }

        #endregion
    }
}
