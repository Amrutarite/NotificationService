using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.DTOs;
using NotificationService.Models;
using NotificationService.Repositories;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace NotificationService.Services
{
    public class EmailSenderService : IEmailSenderService
    {
        private readonly IEmailNotificationRepository _emailNotificationRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSenderService> _logger;
        private readonly SmtpSettings _smtpSettings;

        public EmailSenderService(IEmailNotificationRepository emailNotificationRepository, IConfiguration configuration, ILogger<EmailSenderService> logger)
        {
            _emailNotificationRepository = emailNotificationRepository;
            _configuration = configuration;
            _logger = logger;

            // Get EmailSettings from environment variables or fallback to appsettings.json
            _smtpSettings = new SmtpSettings
            {
                SmtpServer = Environment.GetEnvironmentVariable("EmailSettings__SmtpServer") ?? _configuration["EmailSettings:SmtpServer"],
                Port = int.TryParse(Environment.GetEnvironmentVariable("EmailSettings__Port"), out var port) ? port : int.Parse(_configuration["EmailSettings:Port"] ?? "587"),
                Username = Environment.GetEnvironmentVariable("EmailSettings__Username") ?? _configuration["EmailSettings:Username"],
                Password = Environment.GetEnvironmentVariable("EmailSettings__Password") ?? _configuration["EmailSettings:Password"]
            };

            if (_smtpSettings == null || string.IsNullOrEmpty(_smtpSettings.Username) || string.IsNullOrEmpty(_smtpSettings.Password))
            {
                _logger.LogError("EmailSettings configuration is missing or invalid.");
                throw new Exception("EmailSettings configuration is missing or invalid.");
            }
            else
            {
                _logger.LogInformation("EmailSettings loaded successfully.");
            }
        }

        public async Task<EmailNotificationResponse> ScheduleEmail(EmailNotificationRequest request)
        {
            try
            {
                _logger.LogInformation("Scheduling email...");

                // Fetch sender email from the Username field in EmailSettings
                var senderEmail = _smtpSettings.Username;
                if (string.IsNullOrEmpty(senderEmail))
                {
                    _logger.LogError("Sender email (Username) is not configured in EmailSettings.");
                    throw new Exception("Sender email (Username) is not configured.");
                }

                var emailNotification = new EmailNotification
                {
                    Sender = senderEmail,  // Use Username as the sender email
                    Recipient = request.Recipient,
                    CC = request.CC,
                    Subject = request.Subject,
                    Body = request.Body,
                    Status = "Pending",
                    ScheduledTime = request.ScheduledTime
                };

                var id = await _emailNotificationRepository.SaveEmailNotificationAsync(emailNotification);

                var sendSuccess = false;

                if (emailNotification.ScheduledTime.HasValue)
                {
                    // If ScheduledTime is set, schedule the email using Hangfire or similar mechanism
                    var delay = emailNotification.ScheduledTime.Value - DateTime.UtcNow;

                    if (delay > TimeSpan.Zero)
                    {
                        // Schedule email with Hangfire (for example)
                        BackgroundJob.Schedule(() => SendEmailAsync(emailNotification), delay);
                        _logger.LogInformation($"Email scheduled to be sent at {emailNotification.ScheduledTime}");
                    }
                    else
                    {
                        // If ScheduledTime is in the past, send immediately
                        sendSuccess = await SendEmailAsync(emailNotification);
                    }
                }
                else
                {
                    // If no ScheduledTime, send immediately
                    sendSuccess = await SendEmailAsync(emailNotification);
                }

                emailNotification.Status = sendSuccess ? "Sent" : "Pending";
                emailNotification.SentAt = sendSuccess ? DateTime.Now : null;

                await _emailNotificationRepository.UpdateNotificationStatusAsync(emailNotification.Id, emailNotification.Status, emailNotification.SentAt);

                _logger.LogInformation($"Email scheduled with ID: {id}, Status: {emailNotification.Status}");

                // Exclude the 'Id' field in the response
                return new EmailNotificationResponse(
                    success: sendSuccess,
                    message: sendSuccess ? "Email sent successfully." : "Email sending is pending.",
                    status: emailNotification.Status
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while scheduling the email.");
                throw;
            }
        }

        public async Task<bool> SendEmailAsync(EmailNotification emailNotification)
        {
            try
            {
                _logger.LogInformation("Preparing to send email.");

                var smtpClient = new SmtpClient(_smtpSettings.SmtpServer, _smtpSettings.Port)
                {
                    Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(emailNotification.Sender ?? _smtpSettings.Username),  // Use Username if Sender is null
                    Subject = emailNotification.Subject,
                    Body = emailNotification.Body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(emailNotification.Recipient);
                if (!string.IsNullOrEmpty(emailNotification.CC))
                {
                    mailMessage.CC.Add(emailNotification.CC);
                }

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully.");
                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error: {Message}", smtpEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<string> GetEmailStatus(int id)
        {
            _logger.LogInformation($"Fetching status for email with ID: {id}");
            var email = await _emailNotificationRepository.GetEmailNotificationStatusAsync(id);
            if (email == null)
            {
                _logger.LogWarning($"Email with ID {id} not found.");
                return "Not Found";
            }
            return email.Status;
        }

        public async Task<IEnumerable<EmailNotificationResponse>> GetNotificationsByStatusAsync(string status)
        {
            try
            {
                _logger.LogInformation($"Fetching emails with status: {status}");
                var notifications = await _emailNotificationRepository.GetNotificationsByStatusAsync(status);
                return notifications.Select(n => new EmailNotificationResponse(
                    success: n.Status == "Sent",
                    message: $"Email with ID {n.Id} is {n.Status}.",
                    status: n.Status
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching notifications by status.");
                return Enumerable.Empty<EmailNotificationResponse>();
            }
        }

        public async Task<int> RetryFailedEmailsAsync()
        {
            try
            {
                _logger.LogInformation("Retrying failed emails.");
                var failedEmails = await _emailNotificationRepository.GetNotificationsByStatusAsync("Failed");
                int retriedCount = 0;

                foreach (var email in failedEmails)
                {
                    email.Status = "Pending";
                    await _emailNotificationRepository.UpdateNotificationStatusAsync(email.Id, email.Status, null);
                    retriedCount++;
                }

                _logger.LogInformation($"Retried {retriedCount} failed emails.");
                return retriedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrying failed emails.");
                return 0;
            }
        }
    }
}
