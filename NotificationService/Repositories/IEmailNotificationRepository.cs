using NotificationService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NotificationService.Repositories
{
    public interface IEmailNotificationRepository
    {
        Task<int> SaveEmailNotificationAsync(EmailNotification emailNotification);
        Task<EmailNotification> GetEmailNotificationStatusAsync(int id);
        Task<IEnumerable<EmailNotification>> GetNotificationsByStatusAsync(string status);

        // Add these two methods
        Task<EmailNotification> GetNotificationByIdAsync(int id);
        Task<IEnumerable<EmailNotification>> GetScheduledEmailsAsync();

        Task UpdateNotificationStatusAsync(int id, string status, DateTime? sentAt);
    }
}
