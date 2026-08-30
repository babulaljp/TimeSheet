using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using Timesheet.Models;

namespace Timesheet.Pages
{
    public class viewTimeModel : PageModel
    {
        private readonly IConfiguration _config;
        public viewTimeModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty]
        //public TimeModel CT { get; set; }
        public string userName { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime SelectedDate { get; set; } = DateTime.Today;


        public List<TimeViewModel> Times { get; set; } = new List<TimeViewModel>();

        public async Task OnGetAsync()
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr)) return;

            if (User.Identity?.IsAuthenticated == true)
            {
                userName = User.Identity.Name;
            }

            // Try to load only active users if the column exists; fallback to all users when column not present
            var sql = @"
select ut.tDate, ut.tHours, ut.Details, p.ProjectName, t.TaskName 
from UserTime ut
join Tasks t on ut.TaskId = t.TaskId
join Projects p on ut.ProjectId = p.ProjectId 
join Users u on ut.UserId = u.UserId
where u.UserName = @userName and CONVERT(date, ut.tDate) = @date
order by ut.updateAt asc;";


            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@userName", userName ?? string.Empty);
                cmd.Parameters.AddWithValue("@date", SelectedDate.Date);


                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Times.Add(ReadTime(reader));
                }

            }
            catch (SqlException)
            {
                // Fallback: try without WHERE in case IsActive column doesn't exist

            }
        }

        private static TimeViewModel ReadTime(SqlDataReader reader)
        {
            var t = new TimeViewModel();
            t.Date = reader.IsDBNull(reader.GetOrdinal("tDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("tDate"));
            t.HoursWorked = reader.GetDouble(reader.GetOrdinal("tHours"));
            t.ProjectName = reader.IsDBNull(reader.GetOrdinal("ProjectName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProjectName"));
            t.TaskName = reader.IsDBNull(reader.GetOrdinal("TaskName")) ? string.Empty : reader.GetString(reader.GetOrdinal("TaskName")); 
            t.details = reader.IsDBNull(reader.GetOrdinal("Details")) ? string.Empty : reader.GetString(reader.GetOrdinal("Details"));
            return t;
        }

    }
}
