<<<<<<<< HEAD:MoneyOutService/PaymentService/Interfaces/IClient.cs
﻿namespace MoneyOutService.Interfaces
========
﻿namespace PaymentService.Inerfaces
>>>>>>>> main:MoneyOutService/PaymentService/Inerfaces/IClient.cs
{
    public interface IClient
    {
        Task<T> Get<T>(string url);
        Task<T> Put<T, R>(string url, R query);
        Task<T> Post<T, R>(string url, R query);
    }
}