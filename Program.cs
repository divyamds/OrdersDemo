using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;
using Orders.Api.Services;
using Orders.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.Console());

// Services 
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//  JWT 
var jwt = builder.Configuration.GetSection("Jwt");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"] ?? "CHANGE_ME"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = key
        };
    });

builder.Services.AddAuthorization();

// Rate Limiting 
builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("fixed", options =>
{
    options.PermitLimit = 100;
    options.Window = TimeSpan.FromMinutes(1);
    options.QueueLimit = 0;
}));

builder.Services.AddCors();

// Application services
builder.Services.AddSingleton<InMemoryRepository>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IDiscountClient, DiscountClient>(c =>
{
    c.BaseAddress = new Uri("https://mp785bea31020fd6c237.free.beeceptor.com/");
});

var app = builder.Build();

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["X-XSS-Protection"] = "0";
    await next();
});

//Swagger 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//  Middleware 
app.UseStaticFiles();
app.UseRouting();
app.UseCors(b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();    
app.MapRazorPages();     


app.MapGet("/", () => Results.Redirect("/LoginPage"));


app.Run();

public partial class Program { }
