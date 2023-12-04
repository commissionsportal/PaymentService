using System.Data;

namespace MoneyOutService.Inerfaces
{
    public interface IConnectionService
    {
        IDbConnection GetConnection();
    }
}
