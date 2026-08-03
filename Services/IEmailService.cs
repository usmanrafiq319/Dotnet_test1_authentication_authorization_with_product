namespace Dotnet_test1_authentication_authorization_with_product.Services
{
    public interface IEmailService
    {
    Task SendEmailAsync(string toEmail,string subject,string htmlBody);
    
    }

}
