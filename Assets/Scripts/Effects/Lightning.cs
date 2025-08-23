using System;

using UnityEngine;

namespace Global
{
	[RequireComponent(typeof(LineRenderer))]
	public class Lightning : MonoBehaviour
	{
		[SerializeField] private Transform _endPoint;
		[SerializeField] private int _pointCount;
		[SerializeField] private float _scale;
		[SerializeField] private float _offsetScale;
		[SerializeField] private float _phaseShiftTime;
		[SerializeField] private float _phaseShiftSpeed;
		[SerializeField] private float _phaseOffset;
		[SerializeField] private float _easeStrength;
		private Vector3[] _initialPoints;
		private Vector3[] _lerped;
		private Vector3[] _nextPoints;
		private float _elapsed;
		private float _previousShift;
		private LineRenderer _lineRenderer;

		private void Start()
		{
			_lineRenderer = GetComponent<LineRenderer>();
			_lineRenderer.positionCount = _pointCount;

			_previousShift = UnityEngine.Random.Range(0, _phaseOffset);
			_initialPoints = GeneratePoints();
			_lerped = new Vector3[_pointCount];
			_nextPoints = GeneratePoints();

			_lineRenderer.SetPositions(_initialPoints);
		}

		private void Update()
		{
			_elapsed += Time.deltaTime;

			if(_elapsed > _phaseShiftTime)
			{
				_lineRenderer.SetPositions(_nextPoints);
				_initialPoints = _nextPoints;
				_nextPoints = GeneratePoints();
				_elapsed = 0f;
			}

			float delta = _elapsed / _phaseShiftTime;
			for (int i = 0; i < _pointCount; i++)
			{
				_lerped[i] = Vector3.Lerp(_initialPoints[i], _nextPoints[i], delta);
			}

			_lineRenderer.SetPositions(_lerped);
		}

		private Vector3[] GeneratePoints()
		{
			Vector3[] points = new Vector3[_pointCount];
			Vector2 dir = (_endPoint.position - transform.position).normalized;
			Vector2 perp = new Vector2(-dir.y, dir.x);
			_previousShift += _phaseShiftSpeed;
			float offset = _previousShift;

			for (int i = 0; i < _pointCount; i++)
			{
				float delta = (float)i / (_pointCount - 1);
				float scaledDelta = delta * _scale;
				float noise = (Mathf.Sin(scaledDelta * MathF.E + offset) + Mathf.Sin(scaledDelta * MathF.PI + offset) + Mathf.Cos(2 * scaledDelta + Mathf.PI + offset) + Mathf.Cos(25 * scaledDelta + MathF.E + offset)) / 4;

				float dist = 1 - Mathf.Pow(2 * Mathf.Abs(delta - 0.5f), 1 - _easeStrength);
				points[i] = dir * delta * Vector2.Distance(_endPoint.position, transform.position) + perp * noise * _offsetScale * Mathf.Lerp(1, dist, _easeStrength);
			}

			return points;
		}
	}
}