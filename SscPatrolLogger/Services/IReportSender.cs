namespace SscPatrolLogger.Services;

public interface IReportSender
{
    Task SendEmailJsAsync(string toEmail, string shift, string rowsHtml);
}
