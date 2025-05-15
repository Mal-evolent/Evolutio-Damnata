using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnemyInteraction.Managers
{
    public interface IDependencyProvider
    {
        T GetService<T>() where T : class;
        void RegisterService<T>(T service) where T : class;
        bool HasService<T>() where T : class;
    }

    public class DependencyProvider : IDependencyProvider
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly MonoBehaviour _context;
        
        public DependencyProvider(MonoBehaviour context)
        {
            _context = context;
        }
        
        public T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            
            return null;
        }
        
        public void RegisterService<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }
        
        public bool HasService<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
    }
}
