using Actors;

using Core;

using Networking;

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using YaEm;

namespace Global
{
	public sealed class NetController : NetworkMonoBehaviour, IController
	{
		private readonly struct Waypoint
		{
			public readonly Vector2 Position;
			public readonly float Rotation;

			public Waypoint(Vector2 position, float rotation)
			{
				Position = position;
				Rotation = rotation;
			}
		}

		[SerializeField] private float _rotationThreshold = 360 / 255f;
		[SerializeField] private float _positionThreshold = 0.1f;
		[SerializeField] private float _actionsThreshold = 0.1f;

		public IController.Type SelfType => IController.Type.Net;

		public Vector2 WalkDirection { get
			{
				if (_next.HasValue)
				{
					return (_next.Value.Position - _current.Position).normalized;
				}
				else
				{
					return Vector2.zero;
				}
			} }

		public Vector2 LookDirection
		{
			get
			{
				return _lookDirection;
				if (_next.HasValue)
				{
					return MathUtils.Polar2Vector(_next.Value.Rotation * Mathf.Deg2Rad, 1);
				}
				else
				{
					return MathUtils.Polar2Vector(_current.Rotation * Mathf.Deg2Rad, 1);
				}
			}
		}

		private IActor _tracked;
		public event Action<IController.ControllerAction> OnAction;

		private Vector2 _prevPosition;
		private Vector2 _lookDirection;
		private float _prevRotation;
		private float _lerpTime = 1 / 50f;
		private BitArray _activeActions;
		private float[] _timeSinceActions;

		private Queue<Waypoint> _waypoints = new Queue<Waypoint>();
		private Waypoint _current;
		private float _elapsed;
		private Waypoint? _next;

		protected override void Start()
		{
			base.Start();

			_timeSinceActions = new float[Enum.GetValues(typeof(IController.ControllerAction)).Length];
			_activeActions = new BitArray(_timeSinceActions.Length);
			if (!IsOwner)
			{
				if (TryGetComponent<IActor>(out var actor))
				{
					if (!actor.TryChangeController(this))
					{
						Debug.LogError("Cannot change controller");
					}
					else
					{
						_tracked = actor;
						_prevPosition = Position;
						_prevRotation = Rotation;
					}
				}
				else
				{
					Debug.LogError("Cannof find actor");
				}
			}
			else
			{
				if (TryGetComponent<IActor>(out var actor))
				{
					_tracked = actor;
					_tracked.OnAction += ReadTrackedAction;

					_current = new Waypoint(Position, Rotation);
				}
			}
		}

		protected override void Update()
		{
			base.Update();

			if (IsOwner)
			{
				if(!Position.DistanceLess(_prevPosition, _positionThreshold))
				{
					var package = new ActorSyncPackage(Position, Rotation, (uint)ServiceLocator.Get<ListenersCombiner>().Client.Ticks, ID);
					SendPackageToServer(package, ListenerBase.PackageSendOrder.NextTick);
					_prevRotation = Rotation;
					_prevPosition = Position;
				}

				for(int i = 0; i < _timeSinceActions.Length; i++)
				{
					float diff = Time.time - _timeSinceActions[i];
					if(diff > _actionsThreshold)
					{
						bool wasActive = _activeActions[i];
						_activeActions[i] = false;
						if (wasActive)
						{
							SendPackageToServer(new ActorActionPackage((byte)i, 0, ID), ListenerBase.PackageSendOrder.NextTick);
						}
					}
				}
			}
			else
			{
				if (_next.HasValue)
				{
					if(_current.Position.DistanceLess(_next.Value.Position, _positionThreshold))
					{
						_current = _next.Value;
						if(_waypoints.TryDequeue(out var res))
						{
							_next = res;
						}
						else
						{
							_next = null;
						}
					}
				}

				for (int i = 0; i < _timeSinceActions.Length; i++)
				{
					if (_activeActions[i])
					{
						OnAction?.Invoke((IController.ControllerAction)i);
					}
				}
			}
		}

		private void ReadTrackedAction(IController.ControllerAction obj)
		{
			int ind = (int)obj;
			_timeSinceActions[ind] = Time.time;
			bool wasInActive = !_activeActions[ind];
			_activeActions[ind] = true;
			if (wasInActive)
			{
				SendPackageToServer(new ActorActionPackage((byte)obj, 1, ID), ListenerBase.PackageSendOrder.NextTick);
			}
		}

		protected override bool ProcessPackageInternal(IPackage package)
		{
			if (IsOwner) return true;

			if(package.Type == PackageType.ActorSyncFromServer)
			{
				var sync = (ActorSyncFromServerPackage)package;

				transform.position = sync.Position;
				transform.rotation = Quaternion.Euler(0, 0, sync.Rotation);
				_lookDirection = MathUtils.Polar2Vector(sync.Rotation * Mathf.Deg2Rad + 90, 1);
				return true;
				if (!_next.HasValue)
				{
					_next = new Waypoint(sync.Position, sync.Rotation);
				}
				else
				{
					_waypoints.Enqueue(new Waypoint(sync.Position, sync.Rotation));
				}
				return true;
			}
			if(package.Type == PackageType.ActorAction)
			{
				var act = (ActorActionPackage)package;
				_activeActions[act.Action] = act.IsActive == 1;
			}

			return true;
		}
	}
}