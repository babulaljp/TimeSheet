using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; 
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add cookie authentication
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Login";
    });


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Middleware: ensure dbusername and userid cookies are set for authenticated users
app.Use(async (context, next) =>
{
    try
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            // Only set cookies if not already present
            if (!context.Request.Cookies.ContainsKey("dbusername") || !context.Request.Cookies.ContainsKey("userid"))
            {
                var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrWhiteSpace(connStr))
                {
                    var username = user.Identity.Name;
                    if (!string.IsNullOrEmpty(username))
                    {
                        using var conn = new SqlConnection(connStr);
                        await conn.OpenAsync();

                        using var cmd = new SqlCommand("SELECT UserId, UserName FROM Users WHERE UserName = @u", conn);
                        cmd.Parameters.AddWithValue("@u", username);

                        using var reader = await cmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            var idOrd = reader.GetOrdinal("UserId");
                            var nameOrd = reader.GetOrdinal("UserName");

                            var idVal = reader.IsDBNull(idOrd) ? string.Empty : reader.GetValue(idOrd).ToString();
                            var nameVal = reader.IsDBNull(nameOrd) ? string.Empty : reader.GetString(nameOrd);

                            var cookieOptions = new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = context.Request.IsHttps,
                                SameSite = SameSiteMode.Lax,
                                Expires = DateTimeOffset.UtcNow.AddDays(7)
                            };

                            if (!string.IsNullOrEmpty(nameVal))
                                context.Response.Cookies.Append("dbusername", nameVal, cookieOptions);

                            if (!string.IsNullOrEmpty(idVal))
                            {
                                context.Response.Cookies.Append("userid", idVal, cookieOptions);
                                context.Items["userid"] = idVal;
                            }
                        }
                    }
                }
            }
        }
    }
    catch
    {
        // swallow errors here to avoid breaking requests; consider logging
    }

    await next();
});

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
