using System;
using Autofac;

namespace STN.PriceEngine.UtilityServices
{
    public class ContainerConfigurator
    {
        public static IContainer Configure(Action<ContainerBuilder> onBuild)
        {
            var containerBuilder = new ContainerBuilder();
            if (onBuild != null)
            {
                onBuild(containerBuilder);
            }
            IContainer container = containerBuilder.Build(0);
            ServiceLocator.Container = container;
            return container;
        }

        public static void ConfigureIoC()
        {
            ContainerConfigurator.Configure(null);
        }
    }
}
