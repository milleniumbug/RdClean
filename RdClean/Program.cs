using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RdClean.Data;
using RdClean.Services;
using Sail.ComfyUi;

namespace RdClean;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<ApplicationDbContext>();
        
        builder.Services.AddAuthentication()
            .AddDiscord(options =>
            {
                options.ClientId = builder.Configuration["Discord:ClientId"]!;
                options.ClientSecret = builder.Configuration["Discord:ClientSecret"]!;
            });

        
        builder.Services.AddRazorPages();
        builder.Services.AddMvc();
        builder.Services.AddSingleton<RedrawTaskService>();
        builder.Services.AddScoped<RedrawService>();
        builder.Services.AddScoped<DeleteService>();
        string comfyUiUrl = builder.Configuration["ComfyUiUrl"]!;
        builder.Services.AddScoped<ComfyUiClient>(provider => new ComfyUiClient(new HttpClient()
        {
            BaseAddress = new Uri(comfyUiUrl),
        }));
        string fileStoragePath = builder.Configuration["FileStoragePath"]!;
        builder.Services.AddScoped<IFileProvider>(provider =>
            new FileProvider(new DirectoryInfo(fileStoragePath)));
        
        builder.Services.AddHostedService(provider => provider.GetRequiredService<RedrawTaskService>());

        var app = builder.Build();
        
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
        
        app.Use((context, next) =>
        {
            context.Request.Scheme = "https";
            return next(context);
        });
        
        // Required to serve files with no extension in the .well-known folder
        var options = new StaticFileOptions()
        {
            ServeUnknownFileTypes = true,
        };

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllers();
        app.MapRazorPages();

        app.Run();
    }
}