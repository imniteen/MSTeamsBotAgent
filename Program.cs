// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples
{
    using System;
    using System.Collections.Concurrent;
    using Azure.Identity;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureAppConfiguration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using ProactiveBot;
    using ProactiveBot.Models;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            //var version = builder
            //    .Configuration
            //    .GetRequiredSection("Version")?.Value
            //    ?? throw new NullReferenceException("The version value cannot be found.");

            //if (builder.Environment.IsDevelopment())
            //    InitializeDevEnvironment(builder, version);
            //else
            //    InitializeProdEnvironment(builder, version);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
            });

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create a global hashset for our ConversationReferences
            builder.Services.AddSingleton<ConcurrentDictionary<string, ConversationReference>>();

            // Create a global hashset for in-memory data store
            builder.Services.AddSingleton<ConcurrentDictionary<string, InMemoryStore>>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            builder.Services.AddTransient<IBot, ProactiveNotificationBot>();

            var app = builder.Build();

            if (builder.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            app.Run();
        }

        private static void InitializeProdEnvironment(WebApplicationBuilder builder, string version)
        {
            var appConfigurationEndpoint = builder.Configuration.GetRequiredSection
                ("AppConfiguration:Endpoint")?.Value
                ?? throw new Exception();

            var managedIdentityClientId = builder.Configuration.GetRequiredSection
                ("UserAssignedManangedIdentityClientId")?.Value
                ?? throw new Exception();

            ManagedIdentityCredential userAssignedManagedCredentials = new(managedIdentityClientId);
            builder.Configuration.AddAzureAppConfiguration(options =>
            {
                options.Connect(new Uri(appConfigurationEndpoint), userAssignedManagedCredentials)
                    .ConfigureKeyVault(kv => kv.SetCredential(userAssignedManagedCredentials))
                    .Select(KeyFilter.Any, version);
            });
        }

        private static void InitializeDevEnvironment(WebApplicationBuilder builder, string version)
        {
            var appConfigurationConnectionString = builder
                .Configuration
                .GetConnectionString("AppConfig")
                ?? throw new Exception();

            AzureCliCredential azureCliCredential = new();

            builder.Configuration.AddAzureAppConfiguration(options =>
            {
                options.Connect(appConfigurationConnectionString)
                    .ConfigureKeyVault(kv => kv.SetCredential(azureCliCredential))
                    .Select(KeyFilter.Any, version);
            });
        }
    }
}
