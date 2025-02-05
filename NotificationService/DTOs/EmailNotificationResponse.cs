namespace NotificationService.DTOs
{
    public class EmailNotificationResponse
    {
        //public int Id { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }

        public EmailNotificationResponse(bool success, string message, string status)
        {
            Success = success;
            Message = message;
            Status = status;
        }
    }

}
