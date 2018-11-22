using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Domain.Realestate
{
    /// <summary>
    /// A simple object format to help design neighbourhoods ingame.
    /// An artist should design the neighbourhoods for the city ingame, then they can be imported on the server
    /// `server import_nhood 1 nhood.json `
    /// This command will insert or replace neighbourhoods for shard_id 1. 
    /// 
    /// The replacement is done based on GUID - if no GUIDs match, the nhood is added.
    /// Similarly... if a GUID exists in the city but not in the import, the nhood will be removed.
    /// All lots will be updated to be within their nearest neighbourhood.
    /// The mayor state for added or removed nhoods will be appropriately updated.
    /// 
    /// Note that moving nhoods during a election cycle is probably a bad idea. The candidates the user can vote for
    /// may suddenly change if that candidate no longer lives in this neighbourhood.
    /// </summary>
    public class CityNeighbourhood
    {
        public string GUID;

        public string Name;
        public string Description = null;
        public Point Location;
        public Color? Color = null;
        public float DistanceMul = 1f; // the lower this number is, the more bias this neighbourhood has over surrounding ones.

        private static Color[] Colors = new Color[] {
            new Color(255, 255, 255),
            new Color(125, 255, 255),
            new Color(255, 125, 255),
            new Color(255, 255, 125),
            new Color(125, 125, 255),
            new Color(255, 125, 125),
            new Color(125, 255, 125),
            new Color(0, 255, 255),
            new Color(255, 255, 0)
        };

        public static void Init(List<CityNeighbourhood> nhoods)
        {
            // set some default colours for neighbourhoods that do not have any assigned.
            // if no GUID is assigned, provide a unique one.

            int id = 0;
            foreach (var nhood in nhoods)
            {
                if (nhood.GUID == null || nhood.GUID == "")
                {
                    var match = true;
                    while (match)
                    {
                        var candidate = Guid.NewGuid().ToString();
                        match = nhoods.Any(x => x.GUID == candidate);
                        if (!match) nhood.GUID = candidate;
                    }
                }

                //set color based on index for now.

                if (nhood.Color == null) nhood.Color = Colors[id % Colors.Length];
                id++;
            }
        }
    }
}
