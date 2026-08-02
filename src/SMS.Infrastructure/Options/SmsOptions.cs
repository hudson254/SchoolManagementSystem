namespace SMS.Infrastructure.Options
{
    public class SmsOptions
    {
        public string AccountSid { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }
}
