namespace PaymentService.Interfaces
{
    public interface IClient
    {
        Task<T> Get<T>(Dictionary<string, string>? headers, string url);
        Task<T> Put<T, R>(Dictionary<string, string>? headers, string url, R query);
        Task<T> PostJson<T, R>(Dictionary<string, string>? headers, string url, R query);
        Task<T> Post<T, R>(Dictionary<string, string>? headers, string url, R query);
    }
}
