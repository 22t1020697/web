using Microsoft.Data.SqlClient;
using System.Data;

namespace SV22T1020697.Datalayers.SqlServer
{
    public abstract class BaseRepository
    {
        // Trong file BaseRepository.cs
        protected IDbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }
        // Trong file BaseRepository.cs
        protected readonly string _connectionString;

        public BaseRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
    }
}