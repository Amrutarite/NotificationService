namespace NotificationService.ErrorHandling
{
    public class ErrorResponse
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string TraceId { get; set; }
    }
}
