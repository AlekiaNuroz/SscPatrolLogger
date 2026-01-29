using System.Text;
using System.Text.Json;

namespace SscPatrolLogger.Services;

public sealed class EmailJsReportSender(HttpClient http) : IReportSender
{
    private readonly HttpClient _http = http;

    public async Task SendEmailJsAsync(string toEmail, string shift, string rowsHtml)
    {
        const string serviceId = "service_785wfif";
        const string templateId = "template_70klndz";
        const string publicKey = "PXlRgcuYyDEu_pLHZ";

        var payload = new
        {
            service_id = serviceId,
            template_id = templateId,
            user_id = publicKey,
            template_params = new
            {
                to_email = toEmail,
                shift,
                rows = rowsHtml
            }
        };

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.emailjs.com/api/v1.0/email/send")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("origin", "http://localhost");

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"EmailJS error {response.StatusCode}: {body}");
        }
    }
}