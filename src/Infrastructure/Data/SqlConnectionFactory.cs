using GoGoClaimApi.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace GoGoClaimApi.Infrastructure.Data;
internal sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(
            _configuration.GetConnectionString("DefaultConnection"));
    }
}
