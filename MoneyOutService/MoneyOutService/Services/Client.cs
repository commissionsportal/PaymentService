using MoneyOutService.Inerfaces;
using MoneyOutService.Models.Exceptions;

namespace MoneyOutService.Services
{
    public class Client : IClient
    {
        private readonly HttpClient _client;
        private readonly string _apiRootUrl;

        public Client(IConfiguration configuration, HttpClient client)
        {
            _client = client;
            _apiRootUrl = configuration.GetValue<string>("ApiRootUrl");
        }

        private async Task<T> ProcessResult<T>(HttpResponseMessage responseMessage)
        {
            var content = await responseMessage.Content.ReadAsStringAsync();

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedException();
            }

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                throw new UniqueKeyException(content);
            }

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK ||
                responseMessage.StatusCode == System.Net.HttpStatusCode.Created ||
                responseMessage.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
            }

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return default(T);
            }

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new NotFoundException(content);
            }

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                throw new BadRequestException(content);
            }

            throw new System.Exception(content);
        }

        public async Task<T> GetValue<T>(string url)
        {
            var result = await _client.GetAsync(_apiRootUrl + url);
            return await ProcessResult<T>(result);
        }

        public async Task<T> Put<T, R>(string url, R query)
        {
            var result = await _client.PutAsJsonAsync(_apiRootUrl + url, query);
            return await ProcessResult<T>(result);
        }
    }
}
