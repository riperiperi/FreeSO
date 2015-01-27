using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ProtocolAbstractionLibraryD
{
    //Various methods for dealing with protocol specific stuff that don't belong anywhere else.
    public class ProtoHelpers
    {
        /// <summary>
        /// Tries parsing a DateTime string in various formats.
        /// This is an attempt to overcome the fact that the DB stores
        /// DateTime strings in various formats because the client didn't
        /// previously care.
        /// </summary>
        /// <param name="DateTimeStr">A DateTime string.</param>
        /// <returns>A DateTime instance - MAY BE NULL!</returns>
        public static DateTime ParseDateTime(string DateTimeStr)
        {
            DateTime ParsedResult;
            bool IsAmerican = false;

            if (DateTimeStr.Contains(" AM") || DateTimeStr.Contains(" PM"))
                IsAmerican = true;

            if (!IsAmerican)
            {
                if (!DateTime.TryParseExact(DateTimeStr, "yyyy/MM/dd hh:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out ParsedResult))
                {
                    if (!DateTime.TryParseExact(DateTimeStr, "yyyy.MM.dd hh:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out ParsedResult))
                        DateTime.TryParseExact(DateTimeStr, "yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out ParsedResult);
                }
            }
            else
            {
                if (!DateTime.TryParseExact(DateTimeStr, "yyyy/MM/dd hh:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None, out ParsedResult))
                {
                    if (!DateTime.TryParseExact(DateTimeStr, "yyyy.MM.dd hh:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None, out ParsedResult))
                        DateTime.TryParseExact(DateTimeStr, "yyyy-MM-dd hh:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None, out ParsedResult);
                }
            }

            return ParsedResult;
        }
    }
}