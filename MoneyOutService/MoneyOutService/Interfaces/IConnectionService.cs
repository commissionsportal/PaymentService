using System.Data;

namespace MoneyOutService.Interfaces
{
    public interface IConnectionService
    {
        IDbConnection GetConnection();
    }
}
