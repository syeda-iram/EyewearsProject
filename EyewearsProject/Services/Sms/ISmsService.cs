namespace EyewearsProject.Services.Sms
{
    public interface ISmsService
    {
        Task SendSmsAsync(
            string phoneNumber,
            string message);
    }
}