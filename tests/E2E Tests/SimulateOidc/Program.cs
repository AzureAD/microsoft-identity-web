// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SimulateOidc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseRouting();
            app.MapControllers(); // Fixes - warning ASP0014: Suggest using top level route registrations instead of UseEndpoints (https://aka.ms/aspnet/analyzers)

            app.Run();
        }
    }
}
