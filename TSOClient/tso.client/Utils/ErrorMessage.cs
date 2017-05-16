using FSO.Client.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Utils
{
    public class ErrorMessage
    {
        public string Message = "Unknown Error";
        public string Title = "Error";
        public UIAlertButton[] Buttons = UIAlertButton.Ok();

        public static ErrorMessage FromLiteral(string message)
        {
            return new ErrorMessage { Message = message };
        }

        public static ErrorMessage FromLiteral(string title, string message)
        {
            return new ErrorMessage { Title = title, Message = message };
        }

        public static ErrorMessage FromUIText(string table, string msgKey)
        {
            return new ErrorMessage { Message = GameFacade.Strings.GetString(table, msgKey) };
        }

        public static ErrorMessage FromUIText(string table, string titleKey, string msgKey)
        {
            return new ErrorMessage
            {
                Title = GameFacade.Strings.GetString(table, titleKey),
                Message = GameFacade.Strings.GetString(table, msgKey)
            };
        }
    }
}
