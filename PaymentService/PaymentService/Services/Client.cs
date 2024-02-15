using PaymentService.Interfaces;
using PaymentService.Models.Exceptions;
using System.Reflection;

namespace PaymentService.Services
{
    public class Client : IClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public Client(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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
                if (string.IsNullOrWhiteSpace(content)) return default;
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return System.Text.Json.JsonSerializer.Deserialize<T>(content, options);
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

            throw new Exception(content);
        }

        public async Task<T> Get<T>(Dictionary<string, string>? headers, string url)
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(3);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var result = await client.GetAsync(url);

            return await ProcessResult<T>(result);
        }

        public async Task<T> Put<T, R>(Dictionary<string, string>? headers, string url, R query)
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(3);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var result = await client.PutAsJsonAsync(url, query);

            return await ProcessResult<T>(result);
        }

        public async Task<T> PostJson<T, R>(Dictionary<string, string>? headers, string url, R query)
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(3);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var result = await client.PostAsJsonAsync(url, query);

            return await ProcessResult<T>(result);
        }

        public async Task<T> Post<T, R>(Dictionary<string, string>? headers, string url, R query)
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(3);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var content = new FormUrlEncodedContent(query.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(
                    prop => prop.Name,
                    prop => prop.GetValue(query, null)?.ToString() ?? string.Empty
                ));

            var result = await client.PostAsync(url, content);

            return await ProcessResult<T>(result);
        }
    }
}