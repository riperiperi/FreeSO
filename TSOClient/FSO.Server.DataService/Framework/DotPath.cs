using System.Linq;
using System.Text;

namespace FSO.Common.DataService.Framework
{
    public class DotPath
    {
    }

    public class DotPathResultComponent
    {
        public object Value;
        public uint Id;
        public uint TypeId;
        public DotPathResultComponentType Type;
        public string Name;
        public bool Persist;
    }

    public enum DotPathResultComponentType
    {
        PROVIDER,
        FIELD,
        ARRAY_ITEM
    }

    public class DotPathResult
    {
        public DotPathResultComponent[] Path;

        public DotPathResultComponent GetValue()
        {
            return Path.Last();
        }

        public DotPathResultComponent GetParent()
        {
            return Path[Path.Length - 2];
        }

        public uint GetProvider()
        {
            return Path.First(x => x.Type == DotPathResultComponentType.PROVIDER).Id;
        }

        public DotPathResultComponent GetEntity()
        {
            return Path[1];
        }

        public string GetKeyPath(string finalMember)
        {
            var result = GetKeyPath(0);
            if(result != "")
            {
                return result + "." + finalMember;
            }
            else
            {
                return finalMember;
            }
        }

        public string GetKeyPath()
        {
            return GetKeyPath(0);
        }

        public string GetKeyPath(int offset)
        {
            var result = new StringBuilder();
            for(int i=2; i < Path.Length - offset; i++)
            {
                if(i > 2){
                    result.Append(".");
                }
                result.Append(Path[i].Name);
            }
            return result.ToString();
        }
    }
}
