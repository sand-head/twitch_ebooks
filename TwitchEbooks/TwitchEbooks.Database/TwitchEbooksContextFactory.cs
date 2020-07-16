﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace TwitchEbooks.Database
{
    public class TwitchEbooksContextFactory : IDesignTimeDbContextFactory<TwitchEbooksContext>
    {
        public TwitchEbooksContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                .Build();

            var builder = new DbContextOptionsBuilder();
            builder.UseNpgsql(configuration.GetConnectionString("PostgreSQL"));
            return new TwitchEbooksContext(builder.Options);
        }
    }
}
