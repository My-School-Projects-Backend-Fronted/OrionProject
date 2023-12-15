using ContractManagement.BusinessLogic.Services.Interfaces;
using ContractManagement.DataAccess.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using System.Net;

namespace ContractManagement.BusinessLogic.Services
{
    public  class MailKitService : IMailService
    {
       
        private readonly IConfiguration _configuration;
        public MailKitService( IConfiguration configuration)
        {
            _configuration = configuration;
        }
    
        public async Task SendEmailAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
        {
            var emailSettings = _configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
            var message = new MimeMessage();
            //Ajout de l'expéditeur
            message.From.Add(MailboxAddress.Parse(emailSettings.SenderEmail));
            //Ajout du destinataire
            message.To.Add(MailboxAddress.Parse(recipient));
            // Définition du sujet du message 
            message.Subject = subject;

            //Création du contenu du messsage
            message.Body = new TextPart(TextFormat.Html)
            {
                Text = body
            };
            //Création du client SMTP
            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                //Connection au serveur SMTP avec les paramètres de configuaration
                await client.ConnectAsync(emailSettings.SmtpServer, emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                //Authentification avec les informations d'identification du compte SMTP
                await client.AuthenticateAsync(emailSettings.SenderEmail, emailSettings.Password);
                // Envoie du message par email
                await client.SendAsync(message);
                //Deconnection du client SMTP après l'envoi 
                client.DisconnectAsync(true);
            }
        }

        public async Task SendEmailWithAttachmentAsync(string recipient, string subject, byte[] attachmentData, string attachmentFileName, CancellationToken cancellationToken)
        {
            var emailSettings = _configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
            var message = new MimeMessage();

            message.From.Add(MailboxAddress.Parse(emailSettings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();

            bodyBuilder.Attachments.Add(attachmentFileName, attachmentData, ContentType.Parse("application/pdf"));

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(emailSettings.SmtpServer, emailSettings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
                await client.AuthenticateAsync(emailSettings.SenderEmail, emailSettings.Password, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
            }
        }

    }
}
