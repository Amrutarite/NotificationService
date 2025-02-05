using Hangfire;
using Microsoft.Extensions.Logging;
using NotificationService.Models;
using NotificationService.Repositories;
using NotificationService.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationService.BackgroundJobs
{
    public class EmailJob
    {
        private readonly IEmailNotificationRepository _emailNotificationRepository;
        private readonly ILogger<EmailJob> _logger;
        private readonly EmailSenderService _emailSenderService;

        public EmailJob(
            IEmailNotificationRepository emailNotificationRepository,
            ILogger<EmailJob> logger,
            EmailSenderService emailSenderService)
        {
            _emailNotificationRepository = emailNotificationRepository;
            _logger = logger;
            _emailSenderService = emailSenderService;
        }

        // This method processes the scheduled emails. It's ready to be triggered by Hangfire as a recurring job.
        public async Task ProcessScheduledEmailsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting to process scheduled emails...");

                var scheduledEmails = await _emailNotificationRepository.GetNotificationsByStatusAsync("Pending");

                if (!scheduledEmails.Any())
                {
                    _logger.LogInformation("No pending email notifications found.");
                    return;
                }

                foreach (var email in scheduledEmails)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("EmailJob operation was cancelled.");
                        return;
                    }

                    DateTime currentTime = DateTime.Now;
                    if (email.ScheduledTime.HasValue && email.ScheduledTime.Value <= currentTime)
                    {
                        _logger.LogInformation("Processing email: ID = {Id}, Recipient = {Recipient}, ScheduledTime = {ScheduledTime}",
                            email.Id, email.Recipient, email.ScheduledTime);

                        var emailSentSuccessfully = await _emailSenderService.SendEmailAsync(email);

                        if (emailSentSuccessfully)
                        {
                            _logger.LogInformation("Email sent successfully: ID = {Id}", email.Id);
                            email.Status = "Sent";
                            email.SentAt = currentTime;

                            await _emailNotificationRepository.UpdateNotificationStatusAsync(email.Id, email.Status, email.SentAt);
                            _logger.LogInformation("Database updated for email ID = {Id}", email.Id);
                        }
                        else
                        {
                            _logger.LogError("Failed to send email: ID = {Id}", email.Id);
                            email.Status = "Failed";
                            await _emailNotificationRepository.UpdateNotificationStatusAsync(email.Id, email.Status, null);
                            _logger.LogInformation("Updated email status to 'Failed' for ID = {Id}", email.Id);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Email ID = {Id} is not yet scheduled to be sent. ScheduledTime = {ScheduledTime}",
                            email.Id, email.ScheduledTime);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing scheduled emails.");
            }
        }

        // Method for scheduling emails at the exact time (for individual emails)
        public async Task SendEmailAtScheduledTime(int emailId)
        {
            try
            {
                var email = await _emailNotificationRepository.GetNotificationByIdAsync(emailId);

                if (email != null && email.ScheduledTime.HasValue && email.ScheduledTime.Value <= DateTime.Now)
                {
                    _logger.LogInformation("Sending email at scheduled time: ID = {Id}, Recipient = {Recipient}, ScheduledTime = {ScheduledTime}",
                        email.Id, email.Recipient, email.ScheduledTime);

                    var emailSentSuccessfully = await _emailSenderService.SendEmailAsync(email);

                    if (emailSentSuccessfully)
                    {
                        _logger.LogInformation("Email sent successfully: ID = {Id}", email.Id);
                        email.Status = "Sent";
                        email.SentAt = DateTime.Now;

                        await _emailNotificationRepository.UpdateNotificationStatusAsync(email.Id, email.Status, email.SentAt);
                        _logger.LogInformation("Database updated for email ID = {Id}", email.Id);
                    }
                    else
                    {
                        _logger.LogError("Failed to send email: ID = {Id}", email.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("Email ID = {Id} is either not found or not yet scheduled to be sent.", emailId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending scheduled email.");
            }
        }

        // This method is used to get scheduled emails that are yet to be sent
        public async Task<IEnumerable<EmailNotification>> GetScheduledEmailsAsync()
        {
            try
            {
                // Fetch emails that are scheduled (Pending and with a ScheduledTime set in the future)
                var scheduledEmails = await _emailNotificationRepository.GetNotificationsByStatusAsync("Pending");

                // Filter for emails that are scheduled in the future
                var futureEmails = scheduledEmails.Where(e => e.ScheduledTime.HasValue && e.ScheduledTime.Value > DateTime.Now);

                return futureEmails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching scheduled emails.");
                return Enumerable.Empty<EmailNotification>(); // Return an empty list on error
            }
        }
    }
}
