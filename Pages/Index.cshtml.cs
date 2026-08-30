using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Timesheet.Models;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Timesheet.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;
        public IndexModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public double TodayHours { get; set; }
        public double WeekHours { get; set; }
        public int PendingApprovals { get; set; }
        public List<TimeViewModel> RecentEntries { get; set; } = new List<TimeViewModel>();
        public int TotalEntries { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalEntries / (double)PageSize);

        public async Task OnGetAsync()
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var userName = User.Identity.Name ?? string.Empty;
            var connStr = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr)) return;

            var today = DateTime.Today;
            var weekStart = today.AddDays(-6); // last 7 days including today

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                // Recent entries (last 7 days) with paging
                var sqlCount = @"
select count(*)
from UserTime ut
join Users u on ut.UserId = u.UserId
where u.UserName = @userName and CONVERT(date, ut.tDate) >= @startDate;";

                using (var cnt = new SqlCommand(sqlCount, conn))
                {
                    cnt.Parameters.AddWithValue("@userName", userName);
                    cnt.Parameters.AddWithValue("@startDate", weekStart.Date);
                    var val = await cnt.ExecuteScalarAsync();
                    TotalEntries = val == null || val is DBNull ? 0 : Convert.ToInt32(val);
                }

                var offset = (Page - 1) * PageSize;
                if (offset < 0) offset = 0;

                var sqlRecent = $@"
select ut.tDate, ut.tHours, ut.Details, p.ProjectName, t.TaskName 
from UserTime ut
join Tasks t on ut.TaskId = t.TaskId
join Projects p on ut.ProjectId = p.ProjectId 
join Users u on ut.UserId = u.UserId
where u.UserName = @userName and CONVERT(date, ut.tDate) >= @startDate
order by ut.tDate desc
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

                using (var cmd = new SqlCommand(sqlRecent, conn))
                {
                    cmd.Parameters.AddWithValue("@userName", userName);
                    cmd.Parameters.AddWithValue("@startDate", weekStart.Date);
                    cmd.Parameters.AddWithValue("@offset", offset);
                    cmd.Parameters.AddWithValue("@pageSize", PageSize);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        RecentEntries.Add(ReadTime(reader));
                    }
                }

                // Today's total hours
                var sqlToday = @"select ISNULL(sum(ut.tHours),0) as Total from UserTime ut join Users u on ut.UserId = u.UserId where u.UserName = @userName and CONVERT(date, ut.tDate) = @date;";
                using (var cmd = new SqlCommand(sqlToday, conn))
                {
                    cmd.Parameters.AddWithValue("@userName", userName);
                    cmd.Parameters.AddWithValue("@date", today.Date);
                    var val = await cmd.ExecuteScalarAsync();
                    TodayHours = val == null || val is DBNull ? 0.0 : Convert.ToDouble(val);
                }

                // Week total hours
                var sqlWeek = @"select ISNULL(sum(ut.tHours),0) as Total from UserTime ut join Users u on ut.UserId = u.UserId where u.UserName = @userName and CONVERT(date, ut.tDate) between @start and @end;";
                using (var cmd = new SqlCommand(sqlWeek, conn))
                {
                    cmd.Parameters.AddWithValue("@userName", userName);
                    cmd.Parameters.AddWithValue("@start", weekStart.Date);
                    cmd.Parameters.AddWithValue("@end", today.Date);
                    var val = await cmd.ExecuteScalarAsync();
                    WeekHours = val == null || val is DBNull ? 0.0 : Convert.ToDouble(val);
                }

                // Pending approvals: try to read a column named IsApproved or Status; fallback to 0
                try
                {
                    var sqlPending = @"select count(*) from UserTime ut join Users u on ut.UserId = u.UserId where u.UserName = @userName and (ISNULL(ut.IsApproved,0) = 0);";
                    using var cmd = new SqlCommand(sqlPending, conn);
                    cmd.Parameters.AddWithValue("@userName", userName);
                    var val = await cmd.ExecuteScalarAsync();
                    PendingApprovals = val == null || val is DBNull ? 0 : Convert.ToInt32(val);
                }
                catch (SqlException)
                {
                    PendingApprovals = 0;
                }
            }
            catch (SqlException)
            {
                // swallow DB errors for now; UI will show defaults
            }
        }

        private static TimeViewModel ReadTime(SqlDataReader reader)
        {
            var t = new TimeViewModel();
            t.Date = reader.IsDBNull(reader.GetOrdinal("tDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("tDate"));
            t.HoursWorked = reader.IsDBNull(reader.GetOrdinal("tHours")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("tHours"));
            t.ProjectName = reader.IsDBNull(reader.GetOrdinal("ProjectName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProjectName"));
            t.TaskName = reader.IsDBNull(reader.GetOrdinal("TaskName")) ? string.Empty : reader.GetString(reader.GetOrdinal("TaskName"));
            t.details = reader.IsDBNull(reader.GetOrdinal("Details")) ? string.Empty : reader.GetString(reader.GetOrdinal("Details"));
            return t;
        }

    }
}
