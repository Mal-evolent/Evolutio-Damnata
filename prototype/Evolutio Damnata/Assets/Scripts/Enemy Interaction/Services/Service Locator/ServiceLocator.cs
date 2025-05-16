using System;
using System.Collections.Generic;

namespace EnemyInteraction.Services
{
    /// <summary>
    /// Service Locator - Follows the service locator pattern to provide access to services
    /// </summary>
    public class ServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>
        /// Registers a service with the service locator
        /// </summary>
        public void RegisterService<T>(T service) where T : class
        {
            if (service == null) return;
            _services[typeof(T)] = service;
        }

        /// <summary>
        /// Gets a service from the service locator
        /// </summary>
        public T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return service as T;
            }
            return null;
        }

        /// <summary>
        /// Gets all registered services
        /// </summary>
        public IEnumerable<object> GetAllRegisteredServices()
        {
            return _services.Values;
        }
    }
}
