using NotificationService.Models;
using NotificationService.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq; // Ensure this is imported for LINQ methods like .Any()
using System.Threading.Tasks;

namespace NotificationService.Repositories
{
    public class EmailNotificationRepository : IEmailNotificationRepository
    {
        private readonly DBExecutor _dbExecutor;
        private readonly ILogger<EmailNotificationRepository> _logger;

        public EmailNotificationRepository(DBExecutor dbExecutor, ILogger<EmailNotificationRepository> logger)
        {
            _dbExecutor = dbExecutor;
            _logger = logger;
        }

        // Save email notification using stored procedure
        public async Task<int> SaveEmailNotificationAsync(EmailNotification emailNotification)
        {
            try
            {
                string storedProcedure = "SaveEmailNotification"; // Stored procedure name
                _logger.LogInformation("Saving email notification to the database using stored procedure.");

                // Map EmailNotification to stored procedure parameters
                var parameters = new
                {
                    Sender = emailNotification.Sender,
                    Recipient = emailNotification.Recipient,
                    CC = emailNotification.CC,
                    Subject = emailNotification.Subject,
                    Body = emailNotification.Body,
                    Status = emailNotification.Status,
                    ScheduledTime = emailNotification.ScheduledTime,
                };

                var result = await _dbExecutor.ExecuteScalarAsync<int>(storedProcedure, parameters);
                _logger.LogInformation($"Email notification saved with ID: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving the email notification.");
                throw; // Re-throw the exception for higher-level handling
            }
        }

        // Get email notification by ID
        public async Task<EmailNotification> GetEmailNotificationStatusAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Fetching email notification with ID: {id}");

                // Call DBExecutor to fetch email by ID
                var email = await _dbExecutor.GetNotificationByIdAsync(id);

                if (email != null)
                {
                    _logger.LogInformation($"Email notification found with ID: {id}, Status: {email.Status}");
                }
                else
                {
                    _logger.LogWarning($"No email notification found with ID: {id}");
                }

                return email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching email notification.");
                throw; // Re-throw for higher-level handling
            }
        }

        // Get all email notifications by status
        public async Task<IEnumerable<EmailNotification>> GetNotificationsByStatusAsync(string status)
        {
            try
            {
                _logger.LogInformation($"Fetching email notifications with status: {status}");

                // Call DBExecutor to fetch emails by status
                var emails = await _dbExecutor.GetNotificationsByStatusAsync(status);

                if (emails == null || !emails.Any()) // Changed to Any()
                {
                    _logger.LogWarning($"No email notifications found with status: {status}");
                }

                return emails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching email notifications by status.");
                throw; // Re-throw for higher-level handling
            }
        }

        // Get email notification by ID (added)
        public async Task<EmailNotification> GetNotificationByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Fetching email notification with ID: {id}");
                var email = await _dbExecutor.GetNotificationByIdAsync(id);

                if (email == null)
                {
                    _logger.LogWarning($"No email notification found with ID: {id}");
                }

                return email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching email notification by ID.");
                throw;
            }
        }

        // Get scheduled emails (with status "Scheduled") (added)
        public async Task<IEnumerable<EmailNotification>> GetScheduledEmailsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all scheduled emails with status 'Pending'.");
                var emails = await _dbExecutor.GetNotificationsByStatusAsync("Pending");

                if (emails == null || !emails.Any()) // Changed to Any()
                {
                    _logger.LogWarning("No scheduled emails found.");
                }

                return emails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching scheduled emails.");
                throw;
            }
        }

        // Update email notification status and SentAt timestamp
        public async Task UpdateNotificationStatusAsync(int id, string status, DateTime? sentAt)
        {
            try
            {
                _logger.LogInformation($"Updating email notification with ID: {id}, Status: {status}, SentAt: {sentAt}");

                string query = "UPDATE EmailNotifications SET Status = @Status, SentAt = @SentAt WHERE Id = @Id";

                await _dbExecutor.ExecuteAsync(query, new
                {
                    Id = id,
                    Status = status,
                    SentAt = sentAt
                });

                _logger.LogInformation($"Email notification with ID: {id} has been updated to Status: {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating email notification with ID: {id}");
                throw;
            }
        }
    }
}
