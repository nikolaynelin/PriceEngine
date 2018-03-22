using System;
using STN.PriceEngine.UtilityServices;

namespace STN.PriceEngine.Test.Infrastructure.FactoryUnit
{
    public static class Factory
    {
        public static T Create<T>(Action<T> initialization = null)
        {
            return ServiceLocator.GetInstance<IFactory<T>>().Create(initialization ?? (x => { }));
        }

        public static T Build<T>(Action<T> initialization = null)
        {
            return ServiceLocator.GetInstance<IFactory<T>>().Build(initialization ?? (x => { }));
        }
    }
}
