using Microsoft.Extensions.DependencyInjection;
using Siel.DependencyInjection;
using Siel.MySql.Store;
using Siel.Store;

namespace Siel.MySql.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static SielBuilder UseMySqlStore(this SielBuilder builder, string connectionString)
        {
            var store = new MySqlStore(connectionString);
            builder.Services.AddSingleton<ITaskStore>(store);
            builder.Services.AddSingleton<IEventStore>(store);
            return builder;
        }
    }
}