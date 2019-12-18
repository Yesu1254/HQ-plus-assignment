using ApiCore.CoreModels;
using ApiCore.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.Commands
{
    public class SendEmailSettings
    {
        public string server { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public int port { get; set; }
        public bool useSsl { get; set; }
        public bool useAuth { get; set; }
        public bool enabled { get; set; }

    }

    public class SendEmail : CommandBase
    {
        public bool Run(RouteRequest request, string host, int port, string fromAddress, string password, string toAddress, string bccAddress, string subject, string htmlBody = "", string body = "", List<Attachment> attachments = null, bool redirectIfLiveOrTest = true)
        {
            try
            {
                if (htmlBody != "" && htmlBody.Contains("{body}"))
                {
                    htmlBody = htmlBody.Replace("{body}", body);
                }
                else
                {
                    htmlBody = body;
                }


                var emailMessage = new MailMessage(fromAddress, toAddress, subject, htmlBody);
                emailMessage.IsBodyHtml = true;

                if (attachments != null && attachments.Count > 0) foreach (var attachment in attachments) emailMessage.Attachments.Add(attachment);
                SmtpClient smtpClient = new SmtpClient();
                smtpClient.Host = "smtp.gmail.com";
                smtpClient.Port = 587;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.EnableSsl = true;
                smtpClient.Credentials = new NetworkCredential(fromAddress.Trim(), password);
                smtpClient.Send(emailMessage);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
