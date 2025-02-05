using System;
using System.ComponentModel.DataAnnotations;

namespace NotificationService.Models
{
    public class EmailNotification
    {
        public int Id { get; set; }

        [Required] 
        [EmailAddress] 
        public string Sender { get; set; } = string.Empty;

        [Required] 
        [EmailAddress] 
        public string Recipient { get; set; } = string.Empty;

        [EmailAddress] 
        public string CC { get; set; } = string.Empty;

        [Required] 
        public string Subject { get; set; } = string.Empty;

        [Required] 
        public string Body { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";  // Default status

        public DateTime? ScheduledTime { get; set; }

        public DateTime? SentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // Default to current UTC time
    }
}
