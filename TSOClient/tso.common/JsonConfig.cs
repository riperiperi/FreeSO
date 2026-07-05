using Newtonsoft.Json;

namespace FSO.Common
{
    public class JsonConfig
    {
        public string ActivePath { get; private set; }

        public JsonConfig()
        {
        }

        public virtual void Init()
        {

        }

        public static T Load<T>(string path) where T : JsonConfig, new()
        {
            if (!File.Exists(path))
            {
                var item = new T
                {
                    ActivePath = path
                };
                item.Init();
                item.Save();

                return item;
            }
            else
            {
                var str = File.ReadAllText(path);

                var item = JsonConvert.DeserializeObject<T>(str);
                item.ActivePath = path;

                return item;
            }
        }

        public virtual void Save()
        {
            try
            {
                using (var stream = new StreamWriter(File.Open(ActivePath, FileMode.Create, FileAccess.Write)))
                {
                    stream.Write(JsonConvert.SerializeObject(this));
                }
            }
            catch (Exception) { }
        }
    }
}
