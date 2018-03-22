using Autofac;
using STN.PriceEngine.Context;
using STN.PriceEngine.Repositories;
using STN.PriceEngine.Services;

namespace STN.PriceEngine.Test.DependencyContainar
{
    public static class DependencyContainer
    {
        public static ContainerBuilder Builder(ContainerBuilder builder)
        {
            builder.RegisterType<StnContext>().As<StnContext>().SingleInstance();
            builder.RegisterType<Repository>().As<IRepository>();
            builder.RegisterType<PriceEngineService>().As<IPriceEngineService>();

            return builder;
        }
    }
}
