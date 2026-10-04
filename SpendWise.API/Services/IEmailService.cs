namespace SpendWise.API.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetCodeAsync(
            string email,
            string code);
    }
}