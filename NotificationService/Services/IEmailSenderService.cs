using NotificationService.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NotificationService.Services
{
    public interface IEmailSenderService
    {
        Task<EmailNotificationResponse> ScheduleEmail(EmailNotificationRequest request);
        Task<string> GetEmailStatus(int id);
        Task<IEnumerable<EmailNotificationResponse>> GetNotificationsByStatusAsync(string status);
        Task<int> RetryFailedEmailsAsync();
    }
}
