using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;
using YaEm;

namespace AI
{
	[RequireComponent(typeof(Grid))]
	public class PathFinding : MonoBehaviour
	{
		public enum EuristicType
		{
			Euclidian,
			EuclidianSquare,
			Room
		}
		[SerializeField] private Grid _grid;
		[SerializeField] private EuristicType _type;
		[SerializeField] private int _maxNodesPerCall;
		[SerializeField] private int _maxFlankNodes;

		private void Awake()
		{
			_grid = GetComponent<Grid>();
		}

		public IReadOnlyList<Vector2> GetRandomPath(Vector2 start, out Vector2 end)
		{
			const int MAXTRIES = 16;
			for (int i = 0; i < MAXTRIES; i++)
			{
				Node rn = _grid.RandomNode;
				if (!rn.Walkable || rn.IsUnreachable) continue;

				end = rn.WorldPosition;
				return FindPath(start, rn.WorldPosition);
			}

			end = Vector2.zero;
			return null;
		}

		public bool TryFindFlank(Vector2 start, Vector2 end, Vector2 avoidPoint, float angle, float angleThinness, float angleStrength, float avoidRadius, out IReadOnlyList<Vector2> path)
		{
			Node st = _grid.NodeFromWorldPoint(start);
			Node en = _grid.NodeFromWorldPoint(end);

			BinaryHeap<Node> openHeap = new BinaryHeap<Node>(BinaryHeap<Node>.Mode.Lowest);
			HashSet<Node> closed = new HashSet<Node>();
			HashSet<Node> openHash = new HashSet<Node>();
			openHeap.Insert(st);
			openHash.Add(st);

			float invSqRadius = 1 / (avoidRadius * avoidRadius);
			float normalAngle = Mathf.InverseLerp(0, Mathf.PI * 2, MathUtils.NormalizeAngle(angle));
			int c = 0;

			while (openHash.Count > 0)
			{
				c++;
			
				if(c > _maxFlankNodes)
				{
					path = null;
					return false;
				}

				Node cur = openHeap.ExtractMinimal();

				openHash.Remove(cur);
				closed.Add(cur);

				if (cur == en)
				{
					_grid.Opened = openHash;
					_grid.Closed = closed;
					path = RetracePath(st, en);

					Vector2 average = Vector2.zero;
					for(int i = 0; i < 5 && i < path.Count - 1; i++)
					{
						average += path[path.Count - i - 2] - path[path.Count - i - 1];
					}
					return Vector2.Dot(average.normalized, MathUtils.Polar2Vector(angle, 1)) < 0.25f;
				}

				foreach (Node neighbour in cur.Neighbours)
				{
					if (!neighbour.Walkable || closed.Contains(neighbour)) continue;

					Vector2 dir = avoidPoint - neighbour.WorldPosition;
					float neighborAngle = Mathf.InverseLerp(0, Mathf.PI * 2, MathUtils.NormalizeAngle(Mathf.Atan2(dir.y, dir.x)));
					float abs = Mathf.Abs(neighborAngle - normalAngle);
					float angleFactor = abs > angleThinness ? (Mathf.Clamp01(abs) * angleStrength) : 0;
					float baseFactor = dir.sqrMagnitude * invSqRadius;
					float radFactor = Mathf.Max(0, 1 - (baseFactor < 0.2f ? 1 : baseFactor));

					int aCost = (int)(radFactor * radFactor * angleFactor * 10);
					int newCost = cur.GCost + GetDistance(cur, neighbour) + aCost;

					if ((newCost) < (neighbour.GCost) || !openHash.Contains(neighbour))
					{
						neighbour.GCost = newCost;
						//neighbour.ACost = aCost;
						neighbour.HCost = GetDistance(neighbour, en);
						neighbour.Previous = cur;

						if (!openHash.Contains(neighbour))
						{
							openHeap.Insert(neighbour);
							openHash.Add(neighbour);
						}
					}
				}
			}

			path = null;
			return false;
		}

		public IReadOnlyList<Vector2> FindPath(Vector2 start, Vector2 end, bool fullPath = false)
		{
			Node st = _grid.NodeFromWorldPoint(start);
			Node en = _grid.NodeFromWorldPoint(end);

			int cells = 0;

			BinaryHeap<Node> openHeap = new BinaryHeap<Node>(BinaryHeap<Node>.Mode.Lowest);
			HashSet<Node> closed = new HashSet<Node>();	
			HashSet<Node> openHash = new HashSet<Node>();	
			openHeap.Insert(st);
			openHash.Add(st);

			while (openHeap.Size > 0)
			{
				Node cur = openHeap.ExtractMinimal();

				openHash.Remove(cur);
				closed.Add(cur);
				cells++;

				if(cells == _maxNodesPerCall && !fullPath)
				{
					_grid.Opened = openHash;
					_grid.Closed = closed;
					return RetracePath(st, cur);
				}

				if (cur == en)
				{
					_grid.Opened = openHash;
					_grid.Closed = closed;
					return RetracePath(st, en);
				}

				foreach (Node neighbour in cur.Neighbours)
				{
					if (!neighbour.Walkable || closed.Contains(neighbour)) continue;

					int newCost = cur.GCost + GetDistance(cur, neighbour);
					if (newCost < neighbour.GCost || !openHash.Contains(neighbour))
					{
						neighbour.GCost = newCost;
						neighbour.HCost = GetDistance(neighbour, en);
						neighbour.Previous = cur;

						if (!openHash.Contains(neighbour))
						{
							openHeap.Insert(neighbour);
							openHash.Add(neighbour);
						}
					}
				}
			}
			return null;
		}

		private IReadOnlyList<Vector2> RetracePath(Node start, Node end)
		{
			List<Node> path = new List<Node>();
			List<Vector2> pathV = new List<Vector2>();
			Node cur = end;

			while (cur != start)
			{
				path.Add(cur);
				pathV.Add(cur.WorldPosition);
				cur = cur.Previous;
			}

			_grid.Path = path;
			pathV.Reverse();
			return pathV;
		}

		private int GetDistance(Node a, Node b)
		{
			int rx = Mathf.Abs(a.XPos - b.XPos);
			int ry = Mathf.Abs(a.YPos - b.YPos);

			if (_type == EuristicType.EuclidianSquare)
			{
				rx *= rx;
				ry *= ry;
			}

			if (rx > ry)
			{
				return 14 * ry + 10 * (rx - ry);
			}
			return 14 * rx + 10 * (ry - rx);
		}
	}
}