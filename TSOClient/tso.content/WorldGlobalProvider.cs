/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Content.Framework;
using FSO.Common.Content;
using System.Xml;
using FSO.Content.Codecs;
using System.Text.RegularExpressions;
using System.IO;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Formats.OTF;
using FSO.Files.FAR1;
using FSO.Common.TS1;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to global (*.otf, *.iff) data in FAR3 archives.
    /// </summary>
    public class WorldGlobalProvider
    {
        private Dictionary<string, GameGlobal> Cache; //indexed by lowercase filename, minus directory and extension.
        private Content ContentManager;
        private FAR1Archive TS1Provider;

        public TS1Curve[] HappyWeight;
        public TS1Curve[] HappyWeightChild;
        public TS1Curve[] InteractionScore;
        public TS1Curve[] InteractionScoreChild;
        public TS1Curve[] HouseScore;

        public WorldGlobalProvider(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        /// <summary>
        /// Creates a new cache for loading of globals.
        /// </summary>
        public void Init()
        {
            Cache = new Dictionary<string, GameGlobal>();
            if (ContentManager.TS1)
            {
                TS1Provider = new FAR1Archive(Path.Combine(ContentManager.TS1BasePath, "GameData/Global/Global.far"), false);
            }
        }

        public void InitCurves()
        {
            var global = Get("global");
            InteractionScore = CurveFromSTR(global.Resource.Get<STR>(501), true);
            HappyWeight = CurveFromSTR(global.Resource.Get<STR>(502), true);
            InteractionScoreChild = CurveFromSTR(global.Resource.Get<STR>(503), true);
            HappyWeightChild = CurveFromSTR(global.Resource.Get<STR>(504), true);
            HouseScore = CurveFromSTR(global.Resource.Get<STR>(505), false);

            /*
            PermutationTest(HappyWeight, new float[] { 98.5f, 97.92f, 98.325f, 99.32f, 98.674f, 80.268f, 99.479f, 99f }, 30f);
            PermutationTest(HappyWeight, new float[] { -50.375f, -45.52f, -40.126f, -30.17f, -20.3f, -100f, -10.13f, -0.25f }, 9.42593f);
            PermutationTest(HappyWeight, new float[] { 46.5f, 72.081f, 48.027f, 53.71f, 49.2f, -100f, 95.442f, 50.75f }, 37.593f);
            */
        }

        private void PermutationTest(TS1Curve[] weights, float[] values, float expected)
        {
            //first transform the motives because i'm done with my life
            var original = new float[values.Length];
            Array.Copy(values, original, values.Length);
            for (int v=0; v<values.Length; v++)
            {
                values[v] = InteractionScore[(v > 4) ? v + 1 : v].GetPoint(values[v]);
            }

            int[] bestPermutation = null;
            float bestDistance = float.PositiveInfinity;

            //left side: values
            //right side: weights
            //permute all combinations of value -> weight assignments, and find the closest to the target value

            var n = values.Length;
            var wList = weights.ToList();
            while (wList.Count < n)
            {
                wList.Add(new TS1Curve("(-100;1) (100;1)"));
            }
            weights = wList.ToArray();
            var assignments = Enumerable.Range(0, n).ToArray();
            var evals = 0;

            Action processAssignment = () =>
            {
                evals++;
                float sumMotive = 0;
                float sumWeight = 0;
                for (int j=0; j<n; j++)
                {
                    var motive = values[j];
                    var oMotive = original[j];
                    var myWCurve = weights[assignments[j]];

                    var weight = myWCurve.GetPoint((float)Math.Round(oMotive));
                    sumMotive += motive * weight;
                    sumWeight += weight;
                }
                var finalScore = sumMotive / sumWeight;
                var dist = Math.Abs(finalScore - expected);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestPermutation = assignments.ToArray();
                }
            };

            Action<int, int> swap = (int ind1, int ind2) =>
            {
                var temp = assignments[ind1];
                assignments[ind1] = assignments[ind2];
                assignments[ind2] = temp;
            };

            var c = new int[values.Length]; //algorithm depth state

            processAssignment();

            int i = 0;
            while (i < n)
            {
                if (c[i] < i)
                {
                    if (i % 2 == 0) swap(0, 1);
                    else swap(c[i], i);
                    processAssignment();
                    c[i]++;
                    i = 0;
                }
                else
                {
                    c[i] = 0;
                    i++;
                }
            }
        }

        private TS1Curve[] CurveFromSTR(STR str, bool cache)
        {
            var length = str.Length;
            var result = new TS1Curve[length];
            for (int i=0; i<length; i++)
            {
                result[i] = new TS1Curve(str.GetString(i));
                if (cache)
                {
                    result[i].BuildCache(-100, 100);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets a resource.
        /// </summary>
        /// <param name="filename">The filename of the resource to get.</param>
        /// <returns>A GameGlobal instance containing the resource.</returns>
        public GameGlobal Get(string filename)
        {
            filename = filename.ToLowerInvariant();
            lock (Cache)
            {
                if (Cache.ContainsKey(filename))
                {
                    return Cache[filename];
                }

                //if we can't load this let it throw an exception...
                //probably sanity check this when we add user objects.

                GameGlobalResource resource = null;

                if (TS1Provider != null)
                {
                    var data = TS1Provider.GetEntry(
                        TS1Provider.GetAllEntries().FirstOrDefault(x => x.Key.ToLowerInvariant() == (filename + ".iff").ToLowerInvariant()));

                    if (data != null)
                    {
                        using (var stream = new MemoryStream(data))
                        {
                            var iff = new IffFile();
                            iff.Read(stream);
                            iff.InitHash();
                            iff.SetFilename(filename + ".iff");
                            resource = new GameGlobalResource(iff, null);
                        }
                    }
                    
                }
                else
                { 
          var iff = new IffFile(Path.Combine(ContentManager.BasePath, "objectdata/globals/" + filename + ".iff"));
                    iff.InitHash();
                    OTFFile otf = null;
                    try
                    {
                        var rewrite = PIFFRegistry.GetOTFRewrite(filename + ".otf");
                        otf = new OTFFile(rewrite ?? Path.Combine(ContentManager.BasePath, ("objectdata/globals/" + filename + ".otf")));
                    }
                    catch (IOException)
                    {
                        //if we can't load an otf, it probably doesn't exist.
                    }
                    resource = new GameGlobalResource(iff, otf);
                }

                var item = new GameGlobal
                {
                    Resource = resource
                };

                Cache.Add(filename, item);

                return item;
            }
        }
    }

    public class GameGlobal
    {
        public GameGlobalResource Resource;
    }

    /// <summary>
    /// A global can be an OTF (Object Tuning File) or an IFF.
    /// </summary>
    public class GameGlobalResource : GameIffResource
    {
        public IffFile Iff;
        public OTFFile Tuning;

        public override IffFile MainIff
        {
            get { return Iff; }
        }

        public GameGlobalResource(IffFile iff, OTFFile tuning)
        {
            this.Iff = iff;
            this.Tuning = tuning;
            Recache();
        }

        public override void Recache()
        {
            base.Recache();
            if (Tuning != null)
            {
                foreach (var table in Tuning.Tables)
                {
                    if (table == null) continue;
                    foreach (var item in table.Keys)
                    {
                        if (item == null) continue;
                        TuningCache[((uint)table.ID << 16) | (uint)(item.ID)] = (short)item.Value;
                    }
                }
            }
        }

        public override T Get<T>(ushort id)
        {
            var type = typeof(T);

            T item1 = this.Iff.Get<T>(id);
            if (item1 != null)
            {
                return item1;
            }

            if (type == typeof(OTFTable))
            {
                if (Tuning != null)
                {
                    return (T)(object)Tuning.GetTable(id);
                }
            }

            return default(T);
        }

        public override List<T> List<T>()
        {
            return this.Iff.List<T>();
        }
    }
}
