using System.ComponentModel.DataAnnotations;

namespace MoneyOutService.Models.PaymentureWallet
{
    public class ResponseBase
    {
        public ResponseBase()
        {
            Status = ResponseStatus.Success;
            Message = ResponseStatus.Success.ToString();
            ErrorDescription = string.Empty;
            ErrorTransactionId = string.Empty;
        }

        [Required]
        public ResponseStatus Status { get; set; }

        [Required]
        public string ErrorDescription { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public string ErrorTransactionId { get; set; }
    }

    public enum ResponseStatus
    {
        Success = 0,
        Failed = 1
    }
}