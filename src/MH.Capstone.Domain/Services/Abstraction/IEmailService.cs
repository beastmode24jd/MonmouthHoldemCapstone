using System.Threading;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IEmailService
    {
        string SenderAddress { get; }

        Task SendAsync(string to, string subject, string htmlBody, string? plainText = null, CancellationToken cancellationToken = default);
    }
}
