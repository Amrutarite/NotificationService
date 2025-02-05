namespace NotificationService.DTOs
{
    public class EmailNotificationRequest
    {
        public string Recipient { get; set; }
        public string CC { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }

        public DateTime? ScheduledTime { get; set; }

        // Default constructor for initialization (for model binding in API requests)
        public EmailNotificationRequest() { }

        // Constructor with parameters (if you want to initialize it directly with values)
        public EmailNotificationRequest(string recipient, string cc, string subject, string body, DateTime? scheduledTime = null)
        {
            Recipient = recipient;
            CC = cc;
            Subject = subject;
            Body = body;
            ScheduledTime = scheduledTime; // Set scheduledTime if provided, otherwise it will be null
        }
    }
}


