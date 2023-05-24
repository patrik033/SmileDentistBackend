using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using Quartz.Impl;
using SmileDentistBackend.Data;
using SmileDentistBackend.Data.Repo;
using SmileDentistBackend.Email;
using SmileDentistBackend.Email.Bookings;
using SmileDentistBackend.Email.Registering;
using SmileDentistBackend.Email.Token;
using SmileDentistBackend.Models;
using SmileDentistBackend.Password;
using SmileDentistBackend.Schedulerer;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//repo
builder.Services.AddScoped<IRepo, Repo>();

//Quartz
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
});

//Quartz Hosted Service
builder.Services.AddQuartzHostedService(quartz => quartz
.WaitForJobsToComplete = true);

//add implementations of interfaces
builder.Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
builder.Services.AddSingleton<DailyJobsToBeScheduled>();
builder.Services.AddSingleton<DailyHourlyJobsToBeScheduled>();
builder.Services.AddHostedService<ScheduledHostedService>();

//mailprovider for bookings
builder.Services.AddSingleton<ISendGridEmailBookings, SendGridEmailBookings>();
//mailprovider for registering users
builder.Services.AddSingleton<ISendGridEmailRegister, SendGridEmailRegister>();
//mailprovider for tokens
builder.Services.AddSingleton<ISendGridEmailTokens, SendGridEmailTokens>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("SendGrid"));

//SqlServer
builder.Services.AddDbContext<QuartzContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

//Identity
builder.Services.AddIdentity<ApplicationUser,IdentityRole>(config =>
{
}).AddEntityFrameworkStores<QuartzContext>();

//configurations for reset password and confirm email
builder.Services.AddTransient<CustomEmailConfirmationTokenProvider<ApplicationUser>>();
builder.Services.AddTransient<PasswordResetTokenProvider<ApplicationUser>>();

//automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

//configure token options
builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    
    options.Tokens.ProviderMap.Add("CustomEmailConfirmation",
        new TokenProviderDescriptor(
            typeof(CustomEmailConfirmationTokenProvider<ApplicationUser>)));

    options.Tokens.EmailConfirmationTokenProvider = "CustomEmailConfirmation";

    options.Tokens.ProviderMap.Add("PasswordReset",
        new TokenProviderDescriptor(
            typeof(PasswordResetTokenProvider<ApplicationUser>)));

    options.Tokens.PasswordResetTokenProvider = "PasswordReset";

    //options.Password.RequireDigit = false;
    //options.Password.RequiredLength = 1;
    //options.Password.RequireLowercase = false;
    //options.Password.RequireUppercase = false;
    //options.Password.RequireNonAlphanumeric = false;
});

var key = builder.Configuration.GetValue<string>("ApiSettings:Secret");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});


builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n " +
        "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
        "Example: \"Bearer 12345aasdäd\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = JwtBearerDefaults.AuthenticationScheme
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
             new OpenApiSecurityScheme
             {
                 Reference = new OpenApiReference
                 {
                     Type = ReferenceType.SecurityScheme,
                     Id = "Bearer"
                 },
                 Scheme = "oauth2",
                 Name = "Bearer",
                 In = ParameterLocation.Header
             },
             new List<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();
