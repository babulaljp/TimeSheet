using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UEAW.Pages
{
    public class UsersModel : PageModel
    {
        private readonly IConfiguration _config;

        public UsersModel(IConfiguration config)
        {
            _config = config;
        }

        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();

        public async Task OnGetAsync()
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr)) return;

            // Try to load only active users if the column exists; fallback to all users when column not present
            var sqlWithActive = "SELECT UserId, UserName, FirstName, LastName, Email FROM Users WHERE IsActive = 1";
            var sqlAll = "SELECT UserId, UserName, FirstName, LastName, Email FROM Users";

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(sqlWithActive, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    // No active users found or column may not exist; try without WHERE
                    reader.Close();
                    using var cmd2 = new SqlCommand(sqlAll, conn);
                    using var reader2 = await cmd2.ExecuteReaderAsync();
                    while (await reader2.ReadAsync())
                    {
                        Users.Add(ReadUser(reader2));
                    }
                }
                else
                {
                    while (await reader.ReadAsync())
                    {
                        Users.Add(ReadUser(reader));
                    }
                }
            }
            catch (SqlException)
            {
                // Fallback: try without WHERE in case IsActive column doesn't exist
                try
                {
                    using var conn = new SqlConnection(connStr);
                    await conn.OpenAsync();
                    using var cmd = new SqlCommand(sqlAll, conn);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        Users.Add(ReadUser(reader));
                    }
                }
                catch
                {
                    // ignore further errors; Users will be empty
                }
            }
        }

        private static UserViewModel ReadUser(SqlDataReader reader)
        {
            var user = new UserViewModel();
            user.Id = reader.GetFieldValue<int>(reader.GetOrdinal("UserId"));
            user.UserName = reader.IsDBNull(reader.GetOrdinal("UserName")) ? string.Empty : reader.GetString(reader.GetOrdinal("UserName"));
            user.FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? string.Empty : reader.GetString(reader.GetOrdinal("FirstName"));
            user.LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? string.Empty : reader.GetString(reader.GetOrdinal("LastName"));
            user.Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email"));
            return user;
        }

        public class UserViewModel
        {
            public int Id { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }
    }
}
