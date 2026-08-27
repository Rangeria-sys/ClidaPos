using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Clidapos.Wpf.Services
{
    public class EmailSendResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// Sends email through whichever server config is marked Default+Active in
    /// Email Settings, using real SMTP (System.Net.Mail - no extra package needed).
    /// </summary>
    public class EmailService
    {
        private readonly IntegrationSettingsService _settingsService = new();

        public async Task<EmailSendResult> SendAsync(string toAddress, string subject, string body)
        {
            var config = await _settingsService.GetActiveEmailConfigAsync();

            if (config == null || string.IsNullOrWhiteSpace(config.ServerName) || config.Port == null)
                return new EmailSendResult { Success = false, Message = "No active Email server is configured under Master Settings." };

            try
            {
                using var client = new SmtpClient(config.ServerName.Trim(), config.Port.Value)
                {
                    EnableSsl = string.Equals(config.TLS_SSL_Required?.Trim(), "Y", StringComparison.OrdinalIgnoreCase),
                    Credentials = new NetworkCredential(config.Username?.Trim(), config.Password?.Trim())
                };

                var fromAddress = string.IsNullOrWhiteSpace(config.SMTPAddress) ? config.Username?.Trim() : config.SMTPAddress.Trim();
                if (string.IsNullOrWhiteSpace(fromAddress))
                    return new EmailSendResult { Success = false, Message = "The email config has no From address configured." };

                using var message = new MailMessage(fromAddress, toAddress.Trim(), subject, body);

                await client.SendMailAsync(message);
                return new EmailSendResult { Success = true, Message = "Email sent." };
            }
            catch (Exception ex)
            {
                return new EmailSendResult { Success = false, Message = ex.Message };
            }
        }
    }
}