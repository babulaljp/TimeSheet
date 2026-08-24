using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Security.Cryptography;

namespace UEAW.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(IConfiguration config, ILogger<RegisterModel> logger)
        {
            _config = config;
            _logger = logger;
        }

        [BindProperty]
        public UserModel UM { get; set; }
        public void OnGet()
        {
        }

        public class UserModel
        {
            [Required]
            [Display(Name = "User name")]
            public string UserName { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required] 
            public string FirstName { get; set; }

            [Required] 
            public string LastName { get; set; }

            [Required]
            public string Email { get; set; }

            public string ErrorMessage { get; set; }

        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var connStr = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr))
            {
                ModelState.AddModelError(string.Empty, "Database connection string is not configured.");
                return Page();
            }
            var hashed = HashPassword(UM.Password); // format: iterations.saltBase64.hashBase64

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                var sql = @"
INSERT INTO dbo.Users (UserName, UserPassword, FirstName, LastName, Email)
VALUES (@UserName, @PasswordHash, @FirstName, @LastName, @Email);";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserName", UM.UserName);
                cmd.Parameters.AddWithValue("@PasswordHash", hashed);
                cmd.Parameters.AddWithValue("@FirstName", UM.FirstName);
                cmd.Parameters.AddWithValue("@LastName", UM.LastName);
                cmd.Parameters.AddWithValue("@Email", UM.Email);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Unable to save user to database for user {UserName}", UM?.UserName);
                ModelState.AddModelError(string.Empty, "Unable to save user: " + ex.Message);
                //UM.ErrorMessage = ex.Message;
                return Page();
            }

            return RedirectToPage("/Index");
        }

        private static string HashPassword(string password, int iterations = 100_000, int saltSize = 16, int hashSize = 32)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[saltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(hashSize);

            return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }
    }
}
