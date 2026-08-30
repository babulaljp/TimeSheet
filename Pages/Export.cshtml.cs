using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text;
using Timesheet.Models;

namespace Timesheet.Pages
{
    public class ExportModel : PageModel
    {
        private readonly IConfiguration _config;
        public ExportModel(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return RedirectToPage("/Login");
            }

            var userName = User.Identity.Name ?? string.Empty;
            var connStr = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr)) return BadRequest("No DB");

            var rows = new List<TimeViewModel>();

            var sql = @"
select ut.tDate, ut.tHours, ut.Details, p.ProjectName, t.TaskName 
from UserTime ut
join Tasks t on ut.TaskId = t.TaskId
join Projects p on ut.ProjectId = p.ProjectId 
join Users u on ut.UserId = u.UserId
where u.UserName = @userName
order by ut.tDate asc;";

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@userName", userName);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var t = new TimeViewModel();
                    t.Date = reader.IsDBNull(reader.GetOrdinal("tDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("tDate"));
                    t.HoursWorked = reader.IsDBNull(reader.GetOrdinal("tHours")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("tHours"));
                    t.ProjectName = reader.IsDBNull(reader.GetOrdinal("ProjectName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProjectName"));
                    t.TaskName = reader.IsDBNull(reader.GetOrdinal("TaskName")) ? string.Empty : reader.GetString(reader.GetOrdinal("TaskName"));
                    t.details = reader.IsDBNull(reader.GetOrdinal("Details")) ? string.Empty : reader.GetString(reader.GetOrdinal("Details"));
                    rows.Add(t);
                }
            }
            catch (SqlException)
            {
                return StatusCode(500);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Date,Project,Task,Hours,Notes");
            foreach (var r in rows)
            {
                var date = r.Date == DateTime.MinValue ? string.Empty : r.Date.ToString("yyyy-MM-dd");
                var hours = r.HoursWorked.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                var project = EscapeCsv(r.ProjectName);
                var task = EscapeCsv(r.TaskName);
                var notes = EscapeCsv(r.details);
                sb.AppendLine($"{date},{project},{task},{hours},{notes}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "timesheet.csv");
        }

        private static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            return s;
        }
    }
}
