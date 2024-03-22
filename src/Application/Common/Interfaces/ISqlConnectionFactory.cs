using Npgsql;

namespace GoGoClaimApi.Application.Common.Interfaces;
public interface ISqlConnectionFactory
{
    NpgsqlConnection CreateConnection();
}

