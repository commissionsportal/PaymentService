namespace MoneyOutService.Interfaces
{
    public interface IClient
    {
        Task<T> GetValue<T>(string url);
        Task<T> Put<T, R>(string url, R query);
        Task<T> Post<T, R>(string url, R query);
    }
}