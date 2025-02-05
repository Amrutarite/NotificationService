namespace NotificationService.Models
{
    public class SmtpSettings
    {
        public required string? SmtpServer { get; set; }
        public required int Port { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

}
