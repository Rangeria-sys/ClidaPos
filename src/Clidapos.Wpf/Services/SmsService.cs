using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Clidapos.Wpf.Services
{
    public class SmsSendResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// Sends SMS through whichever gateway URL template is marked Default+Enabled in
    /// SMS Settings. The schema has only one configurable field (APIURL), so the
    /// convention is: the URL contains {phone} and {message} placeholders (already
    /// including your API key, sender ID, etc. as query parameters), and this sends
    /// a GET request to the fully-substituted URL - the common pattern for simple
    /// Kenyan SMS gateway APIs. Example:
    /// https://api.provider.co.ke/send?apikey=XXX&amp;sender=YYY&amp;to={phone}&amp;message={message}
    /// </summary>
    public class SmsService
    {
        private readonly IntegrationSettingsService _settingsService = new();
        private static readonly HttpClient _http = new();

        public async Task<SmsSendResult> SendAsync(string phone, string message)
        {
            var config = await _settingsService.GetActiveSMSConfigAsync();

            if (config == null || string.IsNullOrWhiteSpace(config.APIURL))
                return new SmsSendResult { Success = false, Message = "No active SMS gateway is configured under Master Settings." };

            var url = config.APIURL
                .Replace("{phone}", HttpUtility.UrlEncode(phone.Trim()))
                .Replace("{message}", HttpUtility.UrlEncode(message.Trim()));

            try
            {
                var response = await _http.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode
                    ? new SmsSendResult { Success = true, Message = "SMS sent." }
                    : new SmsSendResult { Success = false, Message = $"Gateway returned an error: {body}" };
            }
            catch (Exception ex)
            {
                return new SmsSendResult { Success = false, Message = ex.Message };
            }
        }
    }
}