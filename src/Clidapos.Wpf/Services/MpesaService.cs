using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class MpesaResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? MpesaReceiptNumber { get; set; }
        public decimal? AmountPaid { get; set; }
    }

    /// <summary>
    /// Real M-Pesa Daraja API integration: OAuth, STK Push (Lipa Na M-Pesa Online),
    /// and status polling. No public callback endpoint is required - after the push
    /// is sent, InitiateAndAwaitPaymentAsync repeatedly queries Safaricom's Query API
    /// until a definite result comes back or the timeout is reached. This is the
    /// standard approach for a desktop till with no reachable public URL.
    /// </summary>
    public class MpesaService
    {
        private readonly IntegrationSettingsService _settingsService = new();
        private static readonly HttpClient _http = new();

        // Environment field is a combined string, e.g. "Sandbox - Paybill" or "Production - Till Number" -
        // MpesaSetting has no spare column of its own to store the Till/Paybill choice separately.
        private string BaseUrl(string? environment) =>
            (environment ?? "").Trim().StartsWith("Production", StringComparison.OrdinalIgnoreCase)
                ? "https://api.safaricom.co.ke"
                : "https://sandbox.safaricom.co.ke";

        private string TransactionType(string? environment) =>
            (environment ?? "").Contains("Till", StringComparison.OrdinalIgnoreCase)
                ? "CustomerBuyGoodsOnline"
                : "CustomerPayBillOnline";

        private async Task<string> GetAccessTokenAsync(MpesaSetting settings)
        {
            var baseUrl = BaseUrl(settings.Environment);
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/oauth/v1/generate?grant_type=client_credentials");

            var creds = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{settings.ConsumerKey?.Trim()}:{settings.ConsumerSecret?.Trim()}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);

            var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"M-Pesa authorization failed: {body}");

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("M-Pesa did not return an access token.");
        }

        /// <summary>Formats a Kenyan phone number to the 2547XXXXXXXX format Daraja requires.</summary>
        private static string NormalizePhone(string phone)
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("0") && digits.Length == 10) return "254" + digits.Substring(1);
            if (digits.StartsWith("254") && digits.Length == 12) return digits;
            if (digits.StartsWith("7") && digits.Length == 9) return "254" + digits;
            return digits;
        }

        /// <summary>Sends the STK Push (customer sees the M-Pesa PIN prompt on their phone),
        /// then polls the Query API every 3 seconds for up to 2 minutes for a definite result.</summary>
        public async Task<MpesaResult> InitiateAndAwaitPaymentAsync(
            decimal amount, string phoneNumber, string accountReference, string transactionDesc,
            CancellationToken cancellationToken = default)
        {
            var settings = await _settingsService.GetOrCreateMpesaAsync();

            if (string.IsNullOrWhiteSpace(settings.ConsumerKey) || string.IsNullOrWhiteSpace(settings.ConsumerSecret)
                || string.IsNullOrWhiteSpace(settings.Shortcode) || string.IsNullOrWhiteSpace(settings.PassKey))
            {
                return new MpesaResult { Success = false, Message = "M-Pesa API is not configured. Set it up under Master Settings first." };
            }

            var baseUrl = BaseUrl(settings.Environment);
            var normalizedPhone = NormalizePhone(phoneNumber);
            var shortcode = settings.Shortcode.Trim();
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var password = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shortcode}{settings.PassKey.Trim()}{timestamp}"));

            var token = await GetAccessTokenAsync(settings);

            var isTill = TransactionType(settings.Environment) == "CustomerBuyGoodsOnline";

            // For a shared bank Paybill (e.g. KCB's 522533), the Account Number identifies
            // YOUR specific business within it and must be the fixed value from settings -
            // it isn't something that varies per sale. Till/Buy Goods has no real
            // account-number concept, so the caller-supplied reference is used instead.
            var stkAccountReference = isTill
                ? accountReference
                : (string.IsNullOrWhiteSpace(settings.AccountNumber) ? accountReference : settings.AccountNumber.Trim());

            // No public callback endpoint is reachable from a desktop till, and the app
            // relies entirely on polling for the result - Daraja still requires some
            // well-formed URL in the request, so a fixed placeholder is used.
            const string placeholderCallbackUrl = "https://example.com/mpesa/callback";

            var pushPayload = new
            {
                BusinessShortCode = shortcode,
                Password = password,
                Timestamp = timestamp,
                TransactionType = TransactionType(settings.Environment),
                Amount = (int)Math.Round(amount),
                PartyA = normalizedPhone,
                PartyB = shortcode,
                PhoneNumber = normalizedPhone,
                CallBackURL = placeholderCallbackUrl,
                AccountReference = stkAccountReference,
                TransactionDesc = transactionDesc
            };

            var pushRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/mpesa/stkpush/v1/processrequest");
            pushRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            pushRequest.Content = new StringContent(JsonSerializer.Serialize(pushPayload), Encoding.UTF8, "application/json");

            var pushResponse = await _http.SendAsync(pushRequest, cancellationToken);
            var pushBody = await pushResponse.Content.ReadAsStringAsync();

            if (!pushResponse.IsSuccessStatusCode)
                return new MpesaResult { Success = false, Message = $"Could not send the payment prompt: {pushBody}" };

            using var pushDoc = JsonDocument.Parse(pushBody);
            var root = pushDoc.RootElement;

            var responseCode = root.TryGetProperty("ResponseCode", out var rc) ? rc.GetString() : null;
            if (responseCode != "0")
            {
                var desc = root.TryGetProperty("ResponseDescription", out var d) ? d.GetString() : "Request was rejected.";
                return new MpesaResult { Success = false, Message = desc ?? "Request was rejected." };
            }

            var checkoutRequestId = root.GetProperty("CheckoutRequestID").GetString()
                ?? throw new InvalidOperationException("M-Pesa did not return a CheckoutRequestID.");

            // Poll for the result - customer typically responds within 10-30 seconds.
            var maxAttempts = 40; // ~2 minutes at 3-second intervals
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return new MpesaResult { Success = false, Message = "Payment wait cancelled." };

                await Task.Delay(3000, cancellationToken);

                var queryTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var queryPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shortcode}{settings.PassKey.Trim()}{queryTimestamp}"));

                var queryPayload = new
                {
                    BusinessShortCode = shortcode,
                    Password = queryPassword,
                    Timestamp = queryTimestamp,
                    CheckoutRequestID = checkoutRequestId
                };

                var queryRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/mpesa/stkpushquery/v1/query");
                queryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                queryRequest.Content = new StringContent(JsonSerializer.Serialize(queryPayload), Encoding.UTF8, "application/json");

                HttpResponseMessage queryResponse;
                try
                {
                    queryResponse = await _http.SendAsync(queryRequest, cancellationToken);
                }
                catch
                {
                    continue; // transient network hiccup - keep polling
                }

                var queryBody = await queryResponse.Content.ReadAsStringAsync();
                if (!queryResponse.IsSuccessStatusCode) continue; // still pending - Safaricom returns an error until settled

                using var queryDoc = JsonDocument.Parse(queryBody);
                var queryRoot = queryDoc.RootElement;

                if (!queryRoot.TryGetProperty("ResultCode", out var resultCodeEl)) continue;

                var resultCode = resultCodeEl.GetString();
                var resultDesc = queryRoot.TryGetProperty("ResultDesc", out var rd) ? rd.GetString() ?? "" : "";

                if (resultCode == "0")
                {
                    return new MpesaResult
                    {
                        Success = true,
                        Message = "Payment received.",
                        MpesaReceiptNumber = checkoutRequestId,
                        AmountPaid = amount
                    };
                }

                // Any non-zero, non-pending result code means the transaction is finished and failed
                // (customer cancelled, entered wrong PIN, insufficient funds, timed out, etc.)
                if (resultCode != "1032" && !string.IsNullOrEmpty(resultCode))
                {
                    return new MpesaResult { Success = false, Message = string.IsNullOrEmpty(resultDesc) ? "Payment was not completed." : resultDesc };
                }
            }

            return new MpesaResult { Success = false, Message = "Timed out waiting for the customer to complete payment." };
        }
    }
}