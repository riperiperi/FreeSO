using FSO.Client.Rendering.City.Model;
using FSO.Common.Domain.Realestate;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Neighborhoods;
using Microsoft.Xna.Framework;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server
{
    public class ToolImportNhood : ITool
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDAFactory DAFactory;
        private ImportNhoodOptions Options;

        public ToolImportNhood(ImportNhoodOptions options, IDAFactory factory)
        {
            this.Options = options;
            this.DAFactory = factory;
        }


        public int Run()
        {
            if (Options.JSON == null)
            {
                Console.WriteLine("Please pass: <shard id> <neighbourhood json path>");
                return 1;
            }
            Console.WriteLine("Starting neighborhood import...");

            List<CityNeighbourhood> data = null;
            //first load the JSON
            try
            {
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CityNeighbourhood>>(File.ReadAllText(Options.JSON));
            } catch (FileNotFoundException)
            {
                Console.WriteLine("The JSON file specified could not be found! ");
                return 1;
            }
            catch (Exception)
            {
                Console.WriteLine("An unknown error occurred loading your JSON file. ");
                return 1;
            }

            Console.WriteLine("Found "+data.Count+" neighborhoods.");

            using (var da = (SqlDA)DAFactory.Get())
            {
                var shard = da.Shards.All().FirstOrDefault(x => x.shard_id == Options.ShardId);
                if (shard == null)
                {
                    Console.WriteLine("Could not find the shard with ID "+Options.ShardId+"!");
                    return 1;
                }
                Console.WriteLine("Importing neighborhoods for shard: " + shard.name);

                var existing = da.Neighborhoods.All(Options.ShardId);
                IEnumerable<DbNeighborhood> Deleted = null;
                IEnumerable<CityNeighbourhood> Added = null;
                IEnumerable<CityNeighbourhood> Updated = null;
                Console.WriteLine("Summary:");
                if (existing.Count == 0)
                {
                    Console.WriteLine("There are no existing neighborhoods in this shard. " + data.Count + " will be added.");
                    Added = data;
                }
                else
                {
                    Console.WriteLine(existing.Count + " neighborhoods are already present in this shard.");
                    foreach (var nhood in existing)
                    {
                        var nhood2 = data.FirstOrDefault(x => x.Name == nhood.name);
                        if (nhood2 != null) nhood2.GUID = nhood.guid;
                    }

                    Deleted = existing.Where(x => !data.Any(y => y.GUID == x.guid));
                    if (Deleted.Count() > 0)
                    {
                        Console.WriteLine("WARNING!! " + Deleted.Count() + " neighborhoods will be deleted!");
                        Console.WriteLine("(" + string.Join(", ", Deleted.Select(x => x.name)) + ")");
                    }

                    Added = data.Where(x => !existing.Any(y => y.guid == x.GUID));
                    if (Added.Count() > 0)
                        Console.WriteLine(Added.Count() + " neighborhoods will be added. (" + string.Join(",", Added.Select(x => x.Name)) + ")");

                    Updated = data.Where(x => existing.Any(y => y.guid == x.GUID));
                    if (Updated.Count() > 0)
                        Console.WriteLine(Added.Count() + " neighborhoods will be updated. (" + string.Join(",", Updated.Select(x => x.Name)) + ")");
                }

                Console.WriteLine();
                Console.WriteLine("Confirm (y|n)? Make sure you have backed up your database first, and that the server is NOT running.");

                var input = Console.ReadKey().KeyChar;
                Console.WriteLine();
                if (input == 'y')
                {
                    try
                    {
                        Console.WriteLine("Committing changes...");

                        foreach (var nhood in Added)
                        {
                            var dbn = ImportToReal(nhood);
                            var id = da.Neighborhoods.AddNhood(dbn);
                            Console.WriteLine("Added " + nhood.Name + " with ID " + id + ".");
                        }

                        foreach (var nhood in Updated)
                        {
                            var dbn = ImportToReal(nhood);
                            if (dbn.description == "") dbn.description = null;
                            var count = da.Neighborhoods.UpdateFromJSON(dbn);
                            if (count > 0) Console.WriteLine("Updated " + nhood.Name + ".");
                        }

                        Console.WriteLine("Cleaning up neighborhoods that aren't meant to be here...");
                        var deleted = da.Neighborhoods.DeleteMissing(Options.ShardId, data.Select(x => x.GUID).ToList());
                        if (deleted > 0)
                        {
                            Console.WriteLine("Deleted "+deleted+" neighborhoods! This may adversely affect ongoing mayor cycles.");
                        }

                        //update neighborhoods for each lot now.

                        Console.WriteLine("Updating neighborhoods for existing lots in "+shard.name+"...");

                        var updated = da.Lots.UpdateAllNeighborhoods(Options.ShardId);

                        Console.WriteLine("Updated "+updated+" existing lots. ");

                        Console.WriteLine("Complete.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Something went wrong:");
                        Console.WriteLine(e.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("Aborting.");
                }
            }

            return 0;
        }

        private DbNeighborhood ImportToReal(CityNeighbourhood nhood)
        {
            return new DbNeighborhood()
            {
                name = nhood.Name,
                description = nhood.Description ?? "",
                shard_id = Options.ShardId,
                location = (uint)(MapCoordinates.Pack((ushort)nhood.Location.X, (ushort)nhood.Location.Y)),
                color = (nhood.Color ?? Color.White).PackedValue,
                guid = nhood.GUID
            };
        }
    }
}
