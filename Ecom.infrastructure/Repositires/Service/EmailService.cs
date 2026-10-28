using Ecom.Core.Dto;
using Ecom.Core.Services;
using MailKit;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.infrastructure.Repositires.Service
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<EmailService> logger;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task SendEmailAsync(EmailDto emailDTO)
        {
            using var smtp = new MailKit.Net.Smtp.SmtpClient();

            smtp.Timeout = 15000;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                logger.LogInformation("Connecting to Gmail SMTP...");

                await smtp.ConnectAsync(
                    configuration["EmailSetting:Smtp"]!,
                    int.Parse(configuration["EmailSetting:Port"]!),
                    SecureSocketOptions.SslOnConnect);

                logger.LogInformation(
                    "SMTP connected in {Elapsed} ms",
                    stopwatch.ElapsedMilliseconds);

                await smtp.AuthenticateAsync(
                    configuration["EmailSetting:Username"]!,
                    configuration["EmailSetting:Password"]!);

                logger.LogInformation(
                    "SMTP authenticated in {Elapsed} ms",
                    stopwatch.ElapsedMilliseconds);

                var message = new MimeMessage();

                message.From.Add(new MailboxAddress(
                    "My Ecom",
                    configuration["EmailSetting:From"]!));

                message.To.Add(MailboxAddress.Parse(emailDTO.To));
                message.Subject = emailDTO.Subject;
                message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = emailDTO.Content
                };

                await smtp.SendAsync(message);

                logger.LogInformation(
                    "Email accepted by SMTP in {Elapsed} ms",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Email sending failed after {Elapsed} ms",
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
            finally
            {
                if (smtp.IsConnected)
                    await smtp.DisconnectAsync(true);
            }
        }
    }
}
