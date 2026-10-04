using System.Net;
using System.Net.Mail;

namespace SpendWise.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendPasswordResetCodeAsync(
            string email,
            string code)
        {
            string smtpHost =
                _configuration["Email:SmtpHost"]
                ?? throw new InvalidOperationException(
                    "Email SMTP host is missing.");

            int smtpPort =
                _configuration.GetValue<int>(
                    "Email:SmtpPort");

            string username =
                _configuration["Email:Username"]
                ?? throw new InvalidOperationException(
                    "Email username is missing.");

            string password =
                _configuration["Email:Password"]
                ?? throw new InvalidOperationException(
                    "Email password is missing.");

            string from =
                _configuration["Email:From"]
                ?? username;

            using var message = new MailMessage();

            message.From = new MailAddress(from);
            message.To.Add(email);

            message.Subject =
                "SpendWise Password Reset Code";

            message.Body =
                $"""
                Hello,

                Your SpendWise password reset code is:

                {code}

                This code expires in 60 seconds.

                If you did not request a password reset,
                please ignore this email.

                SpendWise
                """;

            message.IsBodyHtml = false;

            using var smtp = new SmtpClient(
                smtpHost,
                smtpPort);

            smtp.EnableSsl = true;

            smtp.Credentials =
                new NetworkCredential(
                    username,
                    password);

            await smtp.SendMailAsync(message);
        }
    }
}