using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;

namespace FSO.Server.Api.Core.Utils
{
    /// <summary>
    /// Used to mail our users.
    /// Could be more useful in the future when out of Beta.
    /// </summary>
    public class ApiMail
    {
        string subject;
        string template;
        Dictionary<string, string> strings;

        public ApiMail(string template)
        {
            this.template = template;
            this.strings = new Dictionary<string, string>();
        }

        public void AddString(string key, string value)
        {
            strings.Add(key, value);
        }

        private string ComposeBody(Dictionary<string, string> strings)
        {
            string baseTemplate = File.ReadAllText("./MailTemplates/MailBase.html");
            string content = File.ReadAllText("./MailTemplates/" + template + ".html");

            foreach (KeyValuePair<string, string> entry in strings)
            {
                content = content.Replace("%" + entry.Key + "%", entry.Value);
            }

            strings = new Dictionary<string, string>();
            return baseTemplate.Replace("%content%", content);
        }

        public bool Send(string to, string subject)
        {
            Api api = Api.INSTANCE;

            if(api.Config.SmtpEnabled)
            {
                try
                {
                    MailMessage message = new MailMessage();
                    message.From = new MailAddress(api.Config.SmtpUser, "FreeSO Staff");
                    message.To.Add(to);
                    message.Subject = subject;
                    message.IsBodyHtml = true;
                    message.Body = ComposeBody(strings);

                    SmtpClient client = new SmtpClient();
                    client.UseDefaultCredentials = true;

                    client.Host = api.Config.SmtpHost;
                    client.Port = api.Config.SmtpPort;
                    client.EnableSsl = true;
                    client.Credentials = new System.Net.NetworkCredential(api.Config.SmtpUser, api.Config.SmtpPassword); 

                    // Send async
                    client.SendMailAsync(message);

                    return true;
                } catch(Exception e) {
                    return false;
                }  
            }
            return false;
        }
    }
}