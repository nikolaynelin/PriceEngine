using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Autofac;
using NUnit.Framework;
using STN.PriceEngine.Context;
using STN.PriceEngine.Repositories;
using STN.PriceEngine.Services;
using STN.PriceEngine.Test.DependencyContainar;
using STN.PriceEngine.Test.Infrastructure.FactoryUnit;
using STN.PriceEngine.UtilityServices;

namespace STN.PriceEngine.Test.Infrastructure
{
    [TestFixture]
    public abstract class DatabaseOrientedTest
    {
        private readonly StnContext _dataContext;
        protected readonly IPriceEngineService PriceEngineService;

        protected DatabaseOrientedTest()
        {
            ContainerConfigurator.Configure(builder =>
            {
                DependencyContainer.Builder(builder);
                builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                    .Where(x => x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFactory<>)))
                    .AsImplementedInterfaces();
            });
            _dataContext = ServiceLocator.GetInstance<StnContext>();
            PriceEngineService = ServiceLocator.GetInstance<IPriceEngineService>();
        }

        #region Protected methods

        /// <summary>
        /// Ins the unit of work.
        /// </summary>
        /// <param name="action">The action.</param>
        protected void InUnitOfWork(Action action)
        {
            using (var uow = ServiceLocator.GetInstance<IRepository>())
            {
                action();
                uow.Commit();
                RefreshDbContext();
            }
        }

        /// <summary>
        /// Deletes the specified item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        protected void Delete<T>(T obj) where T : class
        {
            ServiceLocator.GetInstance<IRepository>().Remove(obj);
        }

        protected void RefreshDbContext()
        {
            var context = ((IObjectContextAdapter)_dataContext).ObjectContext;
            var refreshableObjects = _dataContext.ChangeTracker.Entries().Select(c => c.Entity).ToList();
            context.Refresh(RefreshMode.StoreWins, refreshableObjects);
        }

        protected void Add<T>(T entity) where T : class
        {
            ServiceLocator.GetInstance<IRepository>().Add(entity);
        }

        protected void Add<T>(List<T> entities) where T : class
        {
            var repo = ServiceLocator.GetInstance<IRepository>();

            entities.ForEach(x =>
            {
                repo.Add(x);
            });
        }

        protected T Get<T>(object id) where T : class
        {
            return ServiceLocator.GetInstance<IRepository>().Get<T>(id);
        }

        protected T Get<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return ServiceLocator.GetInstance<IRepository>().Get<T>(predicate).First();
        }

        protected void ClearTable<T>() where T:class
        {
            var repo = ServiceLocator.GetInstance<IRepository>();
            var entities = repo.Get<T>().ToList();
            entities.ForEach(x =>
            {
                repo.Remove(x);
            });
        }
        #endregion
    }
}
