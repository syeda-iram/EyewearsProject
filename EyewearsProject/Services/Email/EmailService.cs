using System.Net;
using System.Net.Mail;

namespace EyewearsProject.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            var host = _configuration["EmailSettings:Host"];
            var portValue = _configuration["EmailSettings:Port"];
            var username = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"] ?? "EyeCraft";

            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("EmailSettings:Host is missing.");

            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("EmailSettings:Username is missing.");

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("EmailSettings:Password is missing.");

            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new InvalidOperationException("EmailSettings:FromEmail is missing.");

            if (!int.TryParse(portValue, out var port))
                throw new InvalidOperationException("EmailSettings:Port is invalid.");

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(
                    username,
                    password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            message.To.Add(to);

            await client.SendMailAsync(message);
        }
    }
}