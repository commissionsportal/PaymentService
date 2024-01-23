using Microsoft.Extensions.Options;
using PaymentService.Options;
using System.Net.Http.Headers;

namespace PaymentService
{
    public class ClientMessageHandler : DelegatingHandler
    {
        private readonly string _clientToken;

        public ClientMessageHandler(IOptions<PaymentureMoneyOutServiceOptions> options)
        {
            _clientToken = options.Value.ClientToken;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _clientToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}