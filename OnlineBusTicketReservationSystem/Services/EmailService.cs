using System.Net;
using System.Net.Mail;

namespace OnlineBusTicketReservationSystem.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var host     = _config["EmailSettings:SmtpHost"]!;
            var port     = int.Parse(_config["EmailSettings:SmtpPort"]!);
            var user     = _config["EmailSettings:SmtpUser"]!;
            var pass     = _config["EmailSettings:SmtpPass"]!;
            var fromName = _config["EmailSettings:FromName"] ?? "SRC Travel";

            using var smtp = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl   = true
            };

            var mail = new MailMessage
            {
                From       = new MailAddress(user, fromName),
                Subject    = subject,
                Body       = htmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(toEmail, toName));

            await smtp.SendMailAsync(mail);
        }
    }
}
