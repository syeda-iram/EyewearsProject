namespace EyewearsProject.Services.Sms
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;

        public SmsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendSmsAsync(
            string phoneNumber,
            string message)
        {
            // SMS provider integration will be added here.
            // Currently no SMS provider is configured.

            await Task.CompletedTask;
        }
    }
}