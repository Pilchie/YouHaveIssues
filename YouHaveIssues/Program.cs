using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

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

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}
