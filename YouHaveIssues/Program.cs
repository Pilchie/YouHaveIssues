using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace YouHaveIssues
{
    public class Program
    {
        public static readonly string? SourceVersion;
        public static readonly string? SourceBranch;

        static Program()
        {
            var metadata = typeof(Program).Assembly
                .GetCustomAttributes()
                .OfType<AssemblyMetadataAttribute>()
                .ToDictionary(m => m.Key, m => m.Value);
            SourceVersion = metadata.TryGetValue("Build.SourceVersion", out var sourceVer) ? sourceVer : null;
            SourceBranch = metadata.TryGetValue("Build.SourceBranch", out var sourceBranch) ? sourceBranch : null;
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
