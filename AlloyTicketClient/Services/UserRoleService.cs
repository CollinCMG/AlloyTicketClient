using AlloyTicketClient.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace AlloyTicketClient.Services
{
    public class UserRoleService
    {
        private readonly string _connectionString;

        public UserRoleService(IConfiguration configuration)
        {
            // Use the same connection string as the legacy code, fallback to "default" if not found
            _connectionString = configuration.GetConnectionString("default") ?? string.Empty;
        }

        public async Task<List<CMGWebRole>> GetRolesForUserAsync(string username)
        {
            username = username?.Trim() ?? string.Empty;
            var roles = new List<CMGWebRole>();
            await using var cn = new SqlConnection(_connectionString);
            await cn.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand("spGetUserRoleList", cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@UserLogin", SqlDbType.NVarChar) { Value = username });
            cmd.Parameters.Add(new SqlParameter("@AppCode", SqlDbType.NVarChar) { Value = DBNull.Value });
            var paramReturnMessage = new SqlParameter("@ReturnMessage", SqlDbType.NVarChar)
            {
                Size = 80,
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(paramReturnMessage);

            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                roles.Add(new CMGWebRole
                {
                    AppCode = reader["AppCode"]?.ToString()?.Trim(),
                    RoleName = reader["RoleName"]?.ToString()?.Trim()
                });
            }
            return roles;
        }
    }
}
