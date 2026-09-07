using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace ECommerce522.APIV9.Utilities
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var host = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var port = _configuration.GetValue("Email:SmtpPort", 587);
            var username = _configuration["Email:Username"]
                ?? throw new InvalidOperationException("Email:Username is not configured.");
            var password = _configuration["Email:Password"]
                ?? throw new InvalidOperationException("Email:Password is not configured.");
            var from = _configuration["Email:From"] ?? username;

            var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password)
            };

            return client.SendMailAsync(
            new MailMessage(from: from,
                            to: email,
                            subject,
                            htmlMessage
                            )
            {
                IsBodyHtml = true
            });
        }
    }
}
