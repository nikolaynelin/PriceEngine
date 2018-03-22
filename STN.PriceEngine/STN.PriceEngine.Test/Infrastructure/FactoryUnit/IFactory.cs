using System;

namespace STN.PriceEngine.Test.Infrastructure.FactoryUnit
{
    public interface IFactory<out T>
    {
        T Create(Action<T> initialization);
        T Build(Action<T> initialization);
    }
}
