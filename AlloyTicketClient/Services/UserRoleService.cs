using AlloyTicketClient.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AlloyTicketClient.Services
{
    public class UserRoleService
    {
        private readonly string _connectionString;
        private readonly ILogger<UserRoleService> _logger;

        public UserRoleService(IConfiguration configuration, ILogger<UserRoleService> logger)
        {
            // Use the same connection string as the legacy code, fallback to "default" if not found
            _connectionString = configuration.GetConnectionString("default") ?? string.Empty;
            _logger = logger;
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



        public async Task<string> GetUsernameByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return string.Empty;
            email = email.Trim();
            await using var cn = new SqlConnection(_connectionString);
            await cn.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand("spGetUserListByEmail", cn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new SqlParameter("@UserEmail", SqlDbType.NVarChar) { Value = email });
            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var username = reader["UserLogin"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(username))
                    return username;
            }
            return string.Empty;
        }

        public async Task<List<KeyValuePair<string, string>>> GetUserQueuesAsync(string username)
        {
            _logger.LogDebug("Getting user queues for user: {Username}", username);

            username = username?.Trim() ?? string.Empty;
            var queues = new List<KeyValuePair<string, string>>();
            try
            {
                await using var cn = new SqlConnection(_connectionString);
                await cn.OpenAsync().ConfigureAwait(false);
                await using var cmd = new SqlCommand("spGetUserQueueList", cn)
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
                    var appCode = reader["AppCode"]?.ToString()?.Trim() ?? string.Empty;
                    var appQueueName = reader["AppQueueName"]?.ToString()?.Trim() ?? string.Empty;
                    queues.Add(new KeyValuePair<string, string>(appCode, appQueueName));
                }
                _logger.LogDebug("Found {Count} queues for user {Username}", queues.Count, username);
                return queues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user queues for user {Username}", username);
                throw new ApplicationException("Failed to get user queues.", ex);
            }
        }
    }
}
