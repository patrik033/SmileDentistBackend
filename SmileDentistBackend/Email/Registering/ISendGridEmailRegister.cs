using SendGrid;

namespace SmileDentistBackend.Email.Registering
{
    public interface ISendGridEmailRegister
    {
        Task<Response> SendAsync(string from, string to, string subject, string body,string name);
    }
}
