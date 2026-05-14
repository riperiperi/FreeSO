using System;
using System.IO;
using System.Text.Json;
using FSO.SimAntics.Marshals;

namespace FSO.FsovDump;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 1) {
            Console.Error.WriteLine("usage: fsov-dump <path-to.fsov>");
            return 2;
        }
        var bytes = File.ReadAllBytes(args[0]);
        var marshal = new VMMarshal();
        using var ms = new MemoryStream(bytes);
        marshal.Deserialize(new BinaryReader(ms));

        var entities = new System.Collections.Generic.List<object>();
        foreach (var ent in marshal.Entities)
        {
            object record;
            if (ent is VMAvatarMarshal av) {
                record = new {
                    kind="avatar",
                    object_id=(int)av.ObjectID, persist_id=av.PersistID,
                    guid=av.GUID,
                    x=av.Position.TileX, y=av.Position.TileY, level=av.Position.Level
                };
            } else if (ent is VMGameObjectMarshal go) {
                record = new {
                    kind="object",
                    object_id=(int)go.ObjectID, persist_id=go.PersistID,
                    guid=go.GUID,
                    x=go.Position.TileX, y=go.Position.TileY, level=go.Position.Level,
                    dir=(int)go.Direction
                };
            } else continue;
            entities.Add(record);
        }
        Console.WriteLine(JsonSerializer.Serialize(new { count = entities.Count, entities }, new JsonSerializerOptions { WriteIndented = false }));
        return 0;
    }
}
