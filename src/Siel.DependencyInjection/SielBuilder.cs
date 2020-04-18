using Microsoft.Extensions.DependencyInjection;

namespace Siel.DependencyInjection
{
    public class SielBuilder
    {
        public IServiceCollection Services { get; private set; }

        public SielBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}