using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

namespace Mechanics
{
	public static class GlobalProjectilePreProcessor
	{
		private class Comparer : IComparer<ILocalPreProcessor>
		{
			public int Compare(ILocalPreProcessor x, ILocalPreProcessor y)
			{
				return (int)Mathf.Sign(x.Order - y.Order);
			}
		}

		private static IComparer<ILocalPreProcessor> _localComparer;
		private static Dictionary<ProjectilePreHitProcessorType, IProjectilePreHitProcessor> _staticProcessors;

		static GlobalProjectilePreProcessor()
		{
			_localComparer = new Comparer();
			_staticProcessors = new Dictionary<ProjectilePreHitProcessorType, IProjectilePreHitProcessor>();

			var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => (typeof(IProjectilePreHitProcessor)).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
			foreach (var t in types)
			{
				var att = t.GetCustomAttribute<StaticPreProcessorAttribute>();

				if(att == null) continue;

				var instance = Activator.CreateInstance(t);

				if(instance == null)
				{
					Debug.LogWarning($"Static PreProcessor {t.Name} does not have empty constructor and cant be created!");
					continue;
				}

				var processor = (IProjectilePreHitProcessor)instance;

				if(_staticProcessors.TryGetValue(processor.Type, out var val))
				{
					Debug.LogWarning($"Static PreProcessor type conflict {processor.Type} already registered in {processor.GetType().Name} and {val.GetType().Name}");
					return;
				}

				_staticProcessors.Add(processor.Type, processor);
			}
		}

		public static void ProcessProjectile(Projectile projectile, in RaycastHit2D hit)
		{
			if (projectile.Profile == null || projectile.Profile.ActivePreProcessors == null) return;

			for (int i = 0; i < projectile.Profile.ActivePreProcessors.Length; i++)
			{
				if (_staticProcessors.TryGetValue(projectile.Profile.ActivePreProcessors[i], out var val))
				{
					val.Process(projectile, hit);
				}
			}
		}

		public static ILocalPreProcessor[] GetLocalPreHitProcessors(Projectile projectile)
		{
			List<ILocalPreProcessor> processors = new List<ILocalPreProcessor>();

			if (projectile.Profile != null)
			{
				switch (projectile.Profile.Interaction)
				{
					case ProjectileProfile.HitInteraction.Penetrate:
					{
						processors.Add(new PenetratePreProcessor());
						break;
					}
					case ProjectileProfile.HitInteraction.Reflect:
					{
						processors.Add(new ReflectPreProcessor());
						break;
					}
				}
			}

			processors.ForEach(t => t.Init(projectile));
			processors.Sort(_localComparer);
			return processors.ToArray();
		}
	}
}
