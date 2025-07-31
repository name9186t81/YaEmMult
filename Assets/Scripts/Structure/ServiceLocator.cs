using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

namespace Core
{
	public static class ServiceLocator
	{
		private static readonly Dictionary<Type, object> _services;

		static ServiceLocator()
		{
			_services = new Dictionary<Type, object>();

			var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IService).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

			foreach (var t in types)
			{
				var att = t.GetCustomAttribute<ServiceAttribute>();
				if(att != null && att.InitFromStart)
				{
					var instance = Activator.CreateInstance(t);
					if(t == null)
					{
						Debug.LogError("Failed to create instance of " + t.Name + " service");
						continue;
					}

					Debug.Log(t.Name);
					if(!Register(t, instance))
					{
						Debug.LogError("Failed to register " + t.Name + " service");
					}
				}
			}
		}

		public static bool Register(Type type, object service)
		{
			if (_services.ContainsKey(type)) return false;

			if (service.GetType() != type) return false;

			_services.Add(type, service);
			return true;
		}

		public static bool Register<T>(T service)
		{
			if (Exists<T>()) return false;

			_services.Add(typeof(T), service);
			return true;
		}

		public static T Get<T>()
		{
			return (T)_services[typeof(T)];
		}

		public static bool TryGet<T>(out T value)
		{
			if(_services.TryGetValue(typeof(T), out var obj))
			{
				value = (T)obj;
				return true;
			}

			value = default;
			return false;
		}

		public static bool Exists<T>()
		{
			return _services.ContainsKey(typeof(T));
		}
	}
}