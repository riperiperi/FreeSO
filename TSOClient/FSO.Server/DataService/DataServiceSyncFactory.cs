using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Server.Framework.Aries;
using FSO.Common.DataService;
using FSO.Files.Formats.tsodata;
using System.Reflection;
using FSO.Common.DataService.Framework.Attributes;
using FSO.Server.Protocol.Voltron.Packets;

namespace FSO.Server.DataService
{
    public class DataServiceSyncFactory : IDataServiceSyncFactory
    {
        private IDataService DataService;

        public DataServiceSyncFactory(IDataService ds)
        {
            this.DataService = ds;
        }

        public IDataServiceSync<T> Get<T>(params string[] fields)
        {
            return new DataServiceSync<T>(DataService, fields);
        }
    }

    public class DataServiceSync<T> : IDataServiceSync<T>
    {
        private IDataService DataService;
        private StructField[] Fields;
        private PropertyInfo KeyField;

        public DataServiceSync(IDataService ds, string[] fields)
        {
            this.DataService = ds;
            this.Fields = ds.GetFieldsByName(typeof(T), fields);
            this.KeyField = typeof(T).GetProperties().First(x => x.GetCustomAttribute<Key>() != null);
        }

        public void Sync(IAriesSession target, T item)
        {
            var asObject = (object)item;
            var updates = DataService.SerializeUpdate(Fields, asObject, (uint)KeyField.GetValue(asObject));

            if (updates.Count == 0) { return; }
            var packets = new DataServiceWrapperPDU[updates.Count];

            for(int i=0; i < updates.Count; i++)
            {
                var update = updates[i];
                packets[i] = new DataServiceWrapperPDU() {
                    Body = update,
                    RequestTypeID = 0,
                    SendingAvatarID = 0
                };
            }

            target.Write(packets);
        }
    }
}
