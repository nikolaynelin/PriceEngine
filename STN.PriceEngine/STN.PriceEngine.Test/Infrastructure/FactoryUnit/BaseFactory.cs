using System;
using STN.PriceEngine.Repositories;

namespace STN.PriceEngine.Test.Infrastructure.FactoryUnit
{
    public abstract class BaseFactory<T> : IFactory<T> where T : class
    {
        private readonly IRepository _repository;
        public int Counter { get; private set; }

        protected BaseFactory(IRepository repository)
        {
            _repository = repository;
        }

        public T Create(Action<T> initialization)
        {
            var entity = Build(initialization);
            _repository.Add(entity);
            return entity;
        }

        public T Build(Action<T> initialization)
        {
            Counter++;

            var entity = CreateNew();
            initialization(entity);
            OnInitialized(entity);
            return entity;
        }

        protected abstract T CreateNew();
        protected virtual void OnInitialized(T entity) { }
    }
}
