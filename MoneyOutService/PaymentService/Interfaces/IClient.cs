﻿namespace PaymentService.Interfaces
{
    public interface IClient
    {
        Task<T> Get<T>(string url);
        Task<T> Put<T, R>(string url, R query);
        Task<T> Post<T, R>(string url, R query);
    }
}
