﻿namespace PaymentService.Models
{
    public class HeaderData
    {
        public string User { get; set; }
        public string Token { get; set; }
        public string CallbackToken { get; set; }
        public string CallbackTokenExpiration { get; set; }
    }
}
