using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RabbitMQ.Client;
using System.Text;
using System.Runtime.CompilerServices;
using iCash.Services;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Main")]

internal class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        // Register EncryptionService
        var encryptionSettings = builder.Configuration.GetSection("Encryption");
        builder.Services.AddSingleton(new EncryptionService(
            encryptionSettings["Key"],
            null // IV is no longer needed
        ));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // hello The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}
