using AI;

using System.Collections.Generic;
using System.IO;

using UnityEngine;

using YaEm;

namespace Global
{
	public class PathfindingTest : MonoBehaviour
	{
		[SerializeField] private Transform _start;
		[SerializeField] private Transform _startFlank;
		[SerializeField] private Transform _target;
		[SerializeField] private PathFinding _pathFinding;
		[SerializeField] private bool _findingFlank;
		[SerializeField] private float _angleStrength;
		[SerializeField] private float _angleThinness;
		[SerializeField] private float _rad;

		[SerializeField] private LineRenderer _main;
		[SerializeField] private LineRenderer _flank;
		private IReadOnlyList<Vector2> _normalPath;
		private float _elapsed;

		private void Update()
		{
			/*
			if(_elapsed < 1f)
			{
				_elapsed += Time.deltaTime;
				return;
			}
			_elapsed = Time.deltaTime;*/
			if (_findingFlank)
			{
				if(_normalPath == null)
				{
					_normalPath = _pathFinding.FindPath(_start.position, _target.position);
					int ind2 = 0;
					_main.positionCount = _normalPath.Count;
					foreach (var p in _normalPath)
					{
						_main.SetPosition(ind2++, p);
					}
				}
				var res = _pathFinding.TryFindFlank(
					_startFlank.position,
					_target.position, 
					_target.position, 
					(_normalPath[_normalPath.Count - 2] - _normalPath[_normalPath.Count - 1]).AngleFromVector(),
					_angleThinness,
					_angleStrength,
					_rad, 
					out var path);

				_flank.positionCount = path.Count;
				_flank.startColor = _flank.endColor = res ? Color.red : Color.blue;
				int ind = 0;
				foreach(var p in path)
				{
					_flank.SetPosition(ind++, p);
				}
				_normalPath = null;
			}
			else
			{
				_normalPath = _pathFinding.FindPath(_start.position, _target.position);
				int ind2 = 0;
				_main.positionCount = _normalPath.Count;
				foreach (var p in _normalPath)
				{
					_main.SetPosition(ind2++, p);
				}
			}
		}
	}
}