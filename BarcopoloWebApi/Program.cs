using BarcopoloWebApi.Services;
using System;
using System.Text;
using BarcopoloWebApi.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using BarcopoloWebApi.Services.Person;
using BarcopoloWebApi.Services.Auth;
using BarcopoloWebApi.Services.Organization;
using BarcopoloWebApi.Services.SubOrganization;
using BarcopoloWebApi.Services.Token;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using BarcopoloWebApi.Services.Cargo;
using BarcopoloWebApi.Services.CargoType;
using BarcopoloWebApi.Services.Order;
using BarcopoloWebApi.Services.OrderEvent;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Security;
using BarcopoloWebApi.Services.WalletManagement;
using Microsoft.AspNetCore.Identity;


var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("MyDatabase");

var columnOptions = new ColumnOptions();
columnOptions.Store.Remove(StandardColumn.Properties);
columnOptions.Store.Remove(StandardColumn.MessageTemplate);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information() 
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.MSSqlServer(
        connectionString: connectionString,
        sinkOptions: new MSSqlServerSinkOptions { TableName = "Logs", AutoCreateSqlTable = true },
        restrictedToMinimumLevel: LogEventLevel.Information,
        columnOptions: columnOptions)
    .CreateLogger();

// Add services to the container.

builder.Host.UseSerilog();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var security = new OpenApiSecurityScheme
    {
        Name = "Jwt Auth",
        Description = "توکن خود را وارد کنید.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(security.Reference.Id, security);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { security, new List<string>()}
    });
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

builder.Services.AddDbContext<DataBaseContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<IOrganizationCargoTypeService, OrganizationCargoTypeService>();
builder.Services.AddScoped<ISubOrganizationService, SubOrganizationService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICargoService, CargoService>();
builder.Services.AddScoped<IOrderEventService, OrderEventService>();
builder.Services.AddScoped<ICargoTypeService, CargoTypeService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IOrderVehicleAssignmentService, OrderVehicleAssignmentService>();
builder.Services.AddScoped<IOrderWarehouseAssignmentService, OrderWarehouseAssignmentService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IBargirService, BargirService>();
builder.Services.AddScoped<UserTokenRepository, UserTokenRepository>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IWithdrawalRequestService, WithdrawalRequestService>();  
builder.Services.AddScoped<IFrequentAddressService, FrequentAddressService>();
builder.Services.AddScoped<ITokenValidator, TokenValidate>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IPasswordHasher<Person>, PasswordHasher<Person>>();


builder.Services.AddAuthentication(option =>
    {
        option.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
        option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(configureOptions =>
    {
        configureOptions.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidIssuer = builder.Configuration["JwtConfig:issuer"],
            ValidAudience = builder.Configuration["JwtConfig:audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtConfig:key"])),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        configureOptions.SaveToken = true; 
        configureOptions.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = async context =>
            {
                context.NoResult();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new { error = context.Exception.Message });
                await context.Response.WriteAsync(result);
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully.");
                var validator = context.HttpContext.RequestServices.GetRequiredService<ITokenValidator>();
                return validator.ExecuteAsync(context);
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var error = context.HttpContext.Items["AuthError"] as string;
                if (string.IsNullOrEmpty(error))
                {
                    error = "توکن معتبر نیست یا دسترسی وجود ندارد.";
                }

                var result = JsonSerializer.Serialize(new { error });
                await context.Response.WriteAsync(result);
            },
            OnMessageReceived = context =>
            {
                return Task.CompletedTask;
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new { error = "You do not have permission to access this resource." });
                await context.Response.WriteAsync(result);
            },
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("MyDatabase"),
        name: "Database"
    )
    .AddCheck<ComprehensiveHealthCheck>("Comprehensive");


var app = builder.Build();
app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHealthChecks("/health");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
