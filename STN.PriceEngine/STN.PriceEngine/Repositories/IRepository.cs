using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace STN.PriceEngine.Repositories
{
    public interface IRepository:IDisposable
    {
        T Get<T>(object entityId) where T : class;

        IQueryable<T> Get<T>(Expression<Func<T, bool>> predicate = null) where T : class;

        IQueryable<T> Get<T>(Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>> entityInclude) where T : class;

        void Update<T>(T model) where T : class;

        void Remove<T>(T entity) where T : class;

        void Add<T>(T entity) where T : class;

        int ExecuteSqlCommand(string sql, params object[] paramsObjects);

        IList<T> SqlQuery<T>(string sql);

        IList<T> SqlQuery<T>(string sql, params object[] paramsObjects);

        void Commit();
    }
}
