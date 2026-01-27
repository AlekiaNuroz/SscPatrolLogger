namespace SscPatrolLogger.Services;

public interface IReportSender
{
    Task SendEmailJsAsync(string shift, string rowsHtml);
}
