using Autofac;
using STN.PriceEngine.Context;
using STN.PriceEngine.Services;

namespace STN.PriceEngine.DependencyContainar
{
    public static class DependencyContainer
    {
        public static ContainerBuilder Builder(ContainerBuilder builder)
        {
            builder.RegisterType<StnContext>().As<StnContext>();
            //builder.RegisterType<Repository>().As<IRepository>().InstancePerLifetimeScope();
            builder.RegisterType<PriceEngineService>().As<IPriceEngineService>();
            builder.RegisterType<DataService>().As<IDataService>();

            return builder;
        }
    }
}
