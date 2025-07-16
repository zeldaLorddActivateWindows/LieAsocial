using Microsoft.EntityFrameworkCore;

namespace LieAsocial.Data
{
    public class AppDbContext : DbContext
    {
        protected readonly IConfiguration Configuration;

        public AppDbContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var provider = configuration["DatabaseProvider"] ?? "SqlServer";
            var connectionString = configuration.GetConnectionString($"{provider}Connection");

            switch (provider)
            {
                case "Odbc": options.UseSqlServer(connectionString); break;
                case "LocalDb":
                case "SqlServer":
                default:
                    options.UseSqlServer(connectionString,
                        sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(
                                maxRetryCount: configuration.GetValue<int>("RetryPolicy:MaxRetryCount"),
                                maxRetryDelay: TimeSpan.FromSeconds(configuration.GetValue<int>("RetryPolicy:MaxRetryDelay")),
                                errorNumbersToAdd: null);
                            sqlOptions.CommandTimeout(configuration.GetValue<int>("DatabaseTimeout"));
                        });
                    break;
            }
        }
    }
}
