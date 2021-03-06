﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Siel.Scheduler;
using Siel.Store;

namespace Siel.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static SielBuilder AddSiel(this IServiceCollection serviceCollection,
            Action<SielBuilder> configure = null)
        {
            serviceCollection.AddSingleton<IScheduler, DefaultScheduler>();
            serviceCollection.AddSingleton<IHostedService>(provider =>
            {
                var scheduler = provider.GetRequiredService<IScheduler>();
                return scheduler;
            });
            var builder = new SielBuilder(serviceCollection);
            configure?.Invoke(builder);
            return builder;
        }

        public static SielBuilder UseDependencyInjectionTaskFactory(this SielBuilder builder)
        {
            builder.Services.AddSingleton<ITaskFactory, DependencyInjectionTaskFactory>();
            return builder;
        }

        public static SielBuilder UseInMemoryStore(this SielBuilder builder)
        {
            var store = new InMemoryStore();
            builder.Services.AddSingleton<ITaskStore>(store);
            builder.Services.AddSingleton<IEventStore>(store);
            return builder;
        }
    }
}