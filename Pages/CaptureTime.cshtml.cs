using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations; 
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Timesheet.Pages
{
    public class CaptureTimeModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly ICurrentUser _currentUser;
        public CaptureTimeModel(ICurrentUser currentUser, IConfiguration config) {
            _currentUser = currentUser;
            _config = config;
        }

        [BindProperty]
        public TimeModel CT { get; set; }
        public string userName { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        public class TimeModel
        {
            [Required]
            [Display(Name = "Date")]
            public DateTime Date { get; set; }

            [Required]
            [Display(Name = "Hours Worked")]
            public decimal HoursWorked { get; set; }

            [Required]
            [Display(Name = "Project Name")]
            public int ProjectId { get; set; }

            [Required]
            [Display(Name = "Task Description")]
            public string details { get; set; }

            [Required]
            [Display(Name = "Task Name")]
            public int TaskId { get; set; }

             
        }
         
        public List<SelectListItem> ProjectOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TaskOptions { get; set; } = new List<SelectListItem>();

        public async Task OnGetAsync()
        {
            // ensure CT.Date is initialized from query string SelectedDate (or today)
            if (CT == null)
            {
                CT = new TimeModel { Date = SelectedDate ?? DateTime.Today };
            }

            var connStr = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr)) return;

            if (User.Identity?.IsAuthenticated == true)
            {
                userName = User.Identity.Name;
            }

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                // Load projects
                using (var cmd = new SqlCommand("SELECT ProjectId, ProjectName FROM Projects ORDER BY ProjectName", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var id = reader.GetInt32(reader.GetOrdinal("ProjectId")).ToString();
                        var name = reader.IsDBNull(reader.GetOrdinal("ProjectName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProjectName"));
                        ProjectOptions.Add(new SelectListItem { Value = id, Text = name });
                    }
                }

                // Load tasks
                using (var cmd = new SqlCommand("SELECT TaskId, TaskName FROM Tasks ORDER BY TaskName", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var id = reader.GetInt32(reader.GetOrdinal("TaskId")).ToString();
                        var name = reader.IsDBNull(reader.GetOrdinal("TaskName")) ? string.Empty : reader.GetString(reader.GetOrdinal("TaskName"));
                        TaskOptions.Add(new SelectListItem { Value = id, Text = name });
                    }
                }

            }
            catch (SqlException)
            {
                // Fallback: try without WHERE in case IsActive column doesn't exist

            }
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

            var userId = _currentUser.UserId;

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                var sql = @"
INSERT INTO [dbo].[UserTime] ([TaskId] ,[ProjectId] ,[Details] ,[tHours] ,[tDate] ,[updateAt] ,[UserId])
VALUES (@TaskId, @ProjectId, @Details, @tHours, @tDate, @updateAt, @UserId);";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TaskId", CT.TaskId);
                cmd.Parameters.AddWithValue("@ProjectId", CT.ProjectId);
                cmd.Parameters.AddWithValue("@Details", CT.details);
                cmd.Parameters.AddWithValue("@tHours", CT.HoursWorked);
                cmd.Parameters.AddWithValue("@tDate", CT.Date);
                cmd.Parameters.AddWithValue("@updateAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@UserId", userId); 
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, "Unable to save user: " + ex.Message);
                return Page();
            }
            return RedirectToPage("/ViewTime", new { SelectedDate = CT.Date.ToString("yyyy-MM-dd") });

        }




    }
}
