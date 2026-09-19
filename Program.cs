//using AadhaarAndDataExporter.Services;
//using AadhaarExporter.Services;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using System;
//using System.Threading.Tasks;

//namespace AadhaarAndDataExporter;

//class Program
//{
//    static async Task Main(string[] args)
//    {
//        var host = Host.CreateDefaultBuilder(args)
//            .ConfigureServices((context, services) =>
//            {
//                services.Configure<EncryptionKeyConfig>(context.Configuration.GetSection("EncryptionKey"));
//                services.AddSingleton<IAadhaarDecryptor, AadhaarDecryptor>();
//                services.AddTransient<IExportService, DatabaseExportService>();
//            })
//            .Build();

//        var exportService = host.Services.GetRequiredService<IExportService>();

//        bool exit = false;
//        while (!exit)
//        {
//            Console.Clear();
//            Console.WriteLine("=============================================");
//            Console.WriteLine("       DATABASE EXPORT & DECRYPTION TOOL     ");
//            Console.WriteLine("=============================================");
//            Console.WriteLine("1. Export 1.3M Dataset (SourceDb)");
//            Console.WriteLine("2. Export Decrypted Dataset (IdentityDb)");
//            Console.WriteLine("3. Export Missed Table Dataset (SourceDb)");
//            Console.WriteLine("4. Exit");
//            Console.WriteLine("=============================================");
//            Console.Write("Select menu option (1-4): ");

//            var input = Console.ReadLine();
//            Console.WriteLine();

//            try
//            {
//                switch (input)
//                {
//                    case "1":
//                        Console.WriteLine("Running Option 1...");
//                        await exportService.ExportLargeTableAsync("LargeTable_Export.csv");
//                        break;

//                    case "2":
//                        Console.WriteLine("Running Option 2...");
//                        await exportService.ExportDecryptedAadhaarAsync("Decrypted_Export.csv");
//                        break;

//                    case "3":
//                        Console.WriteLine("Running Option 3...");
//                        await exportService.ExportMissedTableAsync("MissedTable_Export.csv");
//                        break;

//                    case "4":
//                        exit = true;
//                        continue;

//                    default:
//                        Console.WriteLine("Invalid selection. Try again.");
//                        break;
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"\n[ERROR] Process failed: {ex.Message}");
//            }

//            if (!exit)
//            {
//                Console.WriteLine("\nPress Enter to return to the main menu...");
//                Console.ReadLine();
//            }
//        }
//    }
//}


//Controller logic

using AadhaarAndDataExporter.Models;
using AadhaarAndDataExporter.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. JWT Authentication Setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// 2. Swagger Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Database Export API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT Bearer token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.Configure<EncryptionKeyConfig>(
    builder.Configuration.GetSection("EncryptionKeyConfig"));
builder.Services.AddScoped<IExportService, DatabaseExportService>();

builder.Services.AddScoped<IAadhaarDecryptor, AadhaarDecryptor>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();