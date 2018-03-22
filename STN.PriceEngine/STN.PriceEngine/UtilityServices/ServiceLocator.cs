using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;

namespace STN.PriceEngine.UtilityServices
{
    public class ServiceLocator
    {
        private const string CONTAINER_NOT_READY = "Container not ready";

        private static IContainer _container;

        private static ILifetimeScope _lifetime;

        public static IContainer Container
        {
            get
            {
                return ServiceLocator._container;
            }
            set
            {
                ServiceLocator._container = value;
            }
        }

        private static void CheckReady()
        {
            if (ServiceLocator._container == null)
            {
                throw new InvalidOperationException(ServiceLocator.CONTAINER_NOT_READY);
            }
        }

        public static ILifetimeScope GetLifetime()
        {
            if (ServiceLocator._lifetime == null)
            {
                ServiceLocator._lifetime = ServiceLocator._container.BeginLifetimeScope();
            }
            return ServiceLocator._lifetime;
        }

        public static void DisposeLifetime()
        {
            if (ServiceLocator._lifetime != null)
            {
                ServiceLocator._lifetime.Dispose();
            }
            ServiceLocator._lifetime = null;
        }

        public static T GetInstance<T>()
        {
            ServiceLocator.CheckReady();
            return ResolutionExtensions.Resolve<T>(ServiceLocator._container);
        }

        public static T GetInstance<T>(params object[] prms)
        {
            ServiceLocator.CheckReady();
            return ResolutionExtensions.Resolve<T>(ServiceLocator._container, from p in prms
                                                                              select new TypedParameter(p.GetType(), p));
        }

        private static T ResolveAllWithParameters<T>(IEnumerable<Parameter> parameters)
        {
            return ResolutionExtensions.Resolve<T>(ServiceLocator._container, parameters);
        }

        public static T ResolveAllWithParameters<T>(IDictionary<string, object> parameters)
        {
            var list = new List<Parameter>();
            foreach (KeyValuePair<string, object> current in parameters)
            {
                list.Add(new NamedParameter(current.Key, current.Value));
            }
            return ServiceLocator.ResolveAllWithParameters<T>(list);
        }

        public static T GetInstance<T>(string key)
        {
            ServiceLocator.CheckReady();
            return ResolutionExtensions.ResolveNamed<T>(ServiceLocator._container, key);
        }

        public static T GetInstance<T>(string key, params object[] prms)
        {
            ServiceLocator.CheckReady();
            return ResolutionExtensions.ResolveNamed<T>(ServiceLocator._container, key, from p in prms
                                                                                        select new TypedParameter(p.GetType(), p));
        }

        public static object GetInstance(Type serviceType)
        {
            ServiceLocator.CheckReady();
            return ResolutionExtensions.Resolve(ServiceLocator._container, serviceType);
        }

        public static object GetInstance(Type serviceType, string key)
        {
            ServiceLocator.CheckReady();
            return ResolutionExtensions.ResolveNamed(ServiceLocator._container, key, serviceType);
        }
    }
}
