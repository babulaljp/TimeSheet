using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages; 
using Microsoft.Data.SqlClient;
using System.Security.Cryptography; 

namespace UEAW.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _config;

        public LoginModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        public void OnGet()
        {
        }

        public class InputModel
        {
            [Required]
            [Display(Name = "User name")]
            public string UserName { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                return Page();
            }

            var connStr = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr))
            {
                ModelState.AddModelError(string.Empty, "Database connection is not configured.");
                return Page();
            }

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                var sql = "SELECT UserId,UserPassword, UserName FROM dbo.Users WHERE UserName = @UserName";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserName", Input.UserName);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return Page();
                }

                var stored = reader.IsDBNull(reader.GetOrdinal("UserPassword")) ? string.Empty : reader.GetString(reader.GetOrdinal("UserPassword"));
                var dbUserName = reader.IsDBNull(reader.GetOrdinal("UserName")) ? Input.UserName : reader.GetString(reader.GetOrdinal("UserName"));
                 
                if (!VerifyHashedPassword(stored, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return Page();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, dbUserName) 
                };

                var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");

                await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity));

                if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return LocalRedirect(ReturnUrl);
                }

                return RedirectToPage("/Index");
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, "Unable to validate user: " + ex.Message);
                return Page();
            }
        }

        private static bool VerifyHashedPassword(string storedHash, string password)
        {
            if (string.IsNullOrEmpty(storedHash) || password == null) return false;

            // Expected format: iterations.saltBase64.hashBase64
            var parts = storedHash.Split('.');
            if (parts.Length != 3) return false;

            if (!int.TryParse(parts[0], out var iterations)) return false;
            var salt = Convert.FromBase64String(parts[1]);
            var hash = Convert.FromBase64String(parts[2]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(hash.Length);

            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }
    }
}
