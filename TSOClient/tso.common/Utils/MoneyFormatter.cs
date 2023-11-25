using System.Globalization;

namespace FSO.Common.Utils
{
    public class MoneyFormatter
    {
        public static string Format(uint money)
        {
            var val = money.ToString("N", new CultureInfo("en-US"));
            var io = val.LastIndexOf(".");
            if(io != -1){
                val = val.Substring(0, io);
            }
            return "$" + val;
        }
    }
}
