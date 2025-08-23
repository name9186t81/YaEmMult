using System;
using System.Collections;
using System.Collections.Generic;

using TMPro;

using UnityEngine;

namespace AI
{
	public sealed class Grid : MonoBehaviour
	{
		private enum DebugMode
		{
			Normal,
			Path,
			Points
		}
		[SerializeField] private LayerMask _unWalkable;
		[SerializeField] private Vector2 _worldSize;
		[SerializeField] private float _cellSize = 1f;
		[SerializeField] private int _coverMinSize;
		[SerializeField] private int _maxRoomTransition;
		[SerializeField] private Transform _reachabilityTest;
		[SerializeField] private bool _excludeNotConnectedRooms;
		[SerializeField] private bool _debug;
		[SerializeField] private DebugMode _mode;
		private Transform _cached;

		private Room[] _rooms;
		private float _cellDiameter;
		private Vector2Int _size;
		private Node[,] _grid;



		private void Awake()
		{
			_cached = transform;
			_cellDiameter = _cellSize * 2;
			_size = new Vector2Int(Mathf.RoundToInt(_worldSize.x / _cellDiameter), Mathf.RoundToInt(_worldSize.y / _cellDiameter));

			_grid = new Node[_size.x, _size.y];
			Vector2 startPos = (Vector2)_cached.position - _worldSize / 2;

			for (int x = 0; x < _size.x; x++)
			{
				for (int y = 0; y < _size.y; y++)
				{
					Vector2 pos = startPos + new Vector2(x * _cellDiameter + _cellSize, y * _cellDiameter + _cellSize);
					bool isWalkable = !Physics2D.OverlapCircle(pos, _cellSize, _unWalkable);

					_grid[x, y] = new Node(isWalkable, pos, x, y);
				}
			}

			foreach (Node n in _grid)
			{
				n.SetNeighbours(GetNeighbours(n));
			}

			MarkUnreachable();
			MarkCovers();
			MarkRooms();
		}

		private void MarkUnreachable()
		{
			if (_reachabilityTest == null) return;

			var start = NodeFromWorldPoint(_reachabilityTest.position);

			List<Node> nodes = new List<Node>();
			HashSet<Node> close = new HashSet<Node>();
			HashSet<Node> open = new HashSet<Node>();

			nodes.Add(start);
			open.Add(start);
			while (nodes.Count > 0)
			{
				var node = nodes[0];

				nodes.RemoveAt(0);
				open.Remove(node);
				close.Add(node);

				foreach (var n in node.Neighbours)
				{
					if (!n.Walkable || open.Contains(n) || close.Contains(n)) continue;

					nodes.Add(n);
					open.Add(n);
				}
			}

			for (int x = 0; x < _grid.GetLength(0); x++)
			{
				for (int y = 0; y < _grid.GetLength(1); y++)
				{
					if (close.Contains(_grid[x, y])) continue;

					_grid[x, y].Flags |= Node.NodeFlags.UnReachable;
				}
			}
		}
		private void MarkRooms()
		{
			List<Room> rooms = new List<Room>();
			int room = 0;
			for (int x = 1; x < _size.x - 1; x++)
			{
				for (int y = 1; y < _size.y - 1; y++)
				{
					var node = _grid[x, y];
					if (!node.Walkable || node.RoomIndex != -1 || node.IsSegmentBlocker) continue;

					room++;
					Vector2Int min = _size;
					Vector2Int max = Vector2Int.zero;
					List<Node> open = new List<Node>();
					HashSet<int> foundConnections = new HashSet<int>();
					List<Connection> connections = new List<Connection>();
					HashSet<Node> closed = new HashSet<Node>();
					HashSet<Node> openHash = new HashSet<Node>();
					BitArray ownedCells = new BitArray(_size.x * _size.y, false);
					open.Add(node);
					openHash.Add(node);
					Vector2 center = Vector2.zero;
					int size = 0;

					Opened = openHash;
					Closed = closed;
					while (open.Count > 0)
					{
						size++;
						if (size > 30000)
						{
							Debug.LogError("Too many iterations");
							break;
						}

						var localNode = open[0];
						center += localNode.WorldPosition;
						if (localNode.XPos < min.x)
						{
							min = new Vector2Int(localNode.XPos, min.y);
						}
						if (localNode.YPos < min.y)
						{
							min = new Vector2Int(min.x, localNode.YPos);
						}

						if (localNode.XPos > max.x)
						{
							max = new Vector2Int(localNode.XPos, max.y);
						}
						if (localNode.YPos > max.y)
						{
							max = new Vector2Int(max.x, localNode.YPos);
						}

						closed.Add(localNode);
						open.Remove(localNode);
						openHash.Remove(localNode);

						localNode.RoomIndex = room;
						ownedCells[localNode.XPos + localNode.YPos * _size.y] = true;

						int selfX = localNode.XPos;
						int selfY = localNode.YPos;
						int leftX = -1;
						int rightX = -1;
						for (int roomX = 1; roomX < _maxRoomTransition; roomX++)
						{
							if (leftX == -1 && (!IsNodeInRange(selfX - roomX, selfY) || !_grid[selfX - roomX, selfY].Walkable)) leftX = roomX;
							if (rightX == -1 && (!IsNodeInRange(selfX + roomX, selfY) || !_grid[selfX + roomX, selfY].Walkable)) rightX = roomX;

							if (leftX != -1 && rightX != -1)
							{
								int totalLength = Math.Max(leftX, 0) + Math.Max(rightX, 0);
								bool isSegmentBlocker = (IsNodeInRange(selfX, selfY - 1) && _grid[selfX, selfY - 1].IsSegmentBlocker || IsNodeInRange(selfX, selfY + 1) && _grid[selfX, selfY + 1].IsSegmentBlocker);
								if (totalLength > _maxRoomTransition ||
									(max.y - min.y) <= _maxRoomTransition ||
									isSegmentBlocker)
								{
									if (isSegmentBlocker)
									{
										if (IsNodeInRange(selfX, selfY - 1) && _grid[selfX, selfY - 1].IsSegmentBlocker)
										{
											if (!foundConnections.Contains(_grid[selfX, selfY - 1].RoomIndex))
											{
												foundConnections.Add(_grid[selfX, selfY - 1].RoomIndex);
												//connections.Add(new Connection())
											}
										}
										else
										{
											if (!foundConnections.Contains(_grid[selfX, selfY + 1].RoomIndex))
											{
												foundConnections.Add(_grid[selfX, selfY + 1].RoomIndex);
											}
										}
									}
									break;
								}

								for (int j = 0; j < roomX; j++)
								{
									if (IsNodeInRange(selfX - j, selfY))
									{
										_grid[selfX - j, selfY].Flags |= Node.NodeFlags.SegmentBlocker;
										_grid[selfX - j, selfY].RoomIndex = room;
									}
									if (IsNodeInRange(selfX + j, selfY))
									{
										_grid[selfX + j, selfY].Flags |= Node.NodeFlags.SegmentBlocker;
										_grid[selfX + j, selfY].RoomIndex = room;
									}
								}

								break;
							}
						}

						int upY = -1;
						int downY = -1;
						for (int roomY = 1; roomY < _maxRoomTransition; roomY++)
						{
							if (downY == -1 && (!IsNodeInRange(selfX, selfY - roomY) || !_grid[selfX, selfY - roomY].Walkable)) downY = roomY;
							if (upY == -1 && (!IsNodeInRange(selfX, selfY + roomY) || !_grid[selfX, selfY + roomY].Walkable)) upY = roomY;

							if (upY != -1 && downY != -1)
							{
								int totalLength = Math.Max(downY, 0) + Math.Max(upY, 0);
								if (totalLength > _maxRoomTransition ||
									(max.x - min.x) <= _maxRoomTransition ||
									(IsNodeInRange(selfX - 1, selfY) && _grid[selfX - 1, selfY].IsSegmentBlocker || IsNodeInRange(selfX + 1, selfY) && _grid[selfX + 1, selfY].IsSegmentBlocker)) break;

								for (int j = 0; j < roomY; j++)
								{
									if (IsNodeInRange(selfX, selfY - j))
									{
										_grid[selfX, selfY - j].Flags |= Node.NodeFlags.SegmentBlocker;
										_grid[selfX, selfY - j].RoomIndex = room;
									}
									if (IsNodeInRange(selfX, selfY + j))
									{
										_grid[selfX, selfY + j].Flags |= Node.NodeFlags.SegmentBlocker;
										_grid[selfX, selfY + j].RoomIndex = room;
									}
								}
							}
						}

						if (localNode.IsSegmentBlocker)
						{
							continue;
						}

						var neighbours = localNode.Neighbours;
						for (int i = 0; i < neighbours.Length; i++)
						{
							if (neighbours[i].IsSegmentBlocker || !neighbours[i].Walkable || neighbours[i].RoomIndex != -1 || closed.Contains(neighbours[i]) || openHash.Contains(neighbours[i])) continue;

							open.Add(neighbours[i]);
							openHash.Add(neighbours[i]);
						}
					}

					center /= size;
					Room r = new Room(min, max, center, room);
					r.SetBitMask(ownedCells);
					rooms.Add(r);
				}
			}

			foreach(var locRoom in rooms)
			{
				const int MAX_TRIES = 16;
				Node startNode = _grid[locRoom.GCenter.x, locRoom.GCenter.y];

				for (int i = 0; i < MAX_TRIES && startNode == null; i++)
				{
					startNode = _grid[UnityEngine.Random.Range(locRoom.RawMin.x, locRoom.RawMax.x), UnityEngine.Random.Range(locRoom.RawMin.y, locRoom.RawMax.y)];
				}

				if(startNode == null)
				{
					Debug.LogWarning($"Failed to connect room {locRoom.Index} after {MAX_TRIES} tries");
					continue;
				}

				Dictionary<int, List<Vector2>> indexPosition = new Dictionary<int, List<Vector2>>();
				HashSet<int> foundIndexes = new HashSet<int>();
				List<Connection> foundConnections = new List<Connection>();

				List<Node> openList = new List<Node>();
				HashSet<Node> closed = new HashSet<Node>();
				HashSet<Node> opened = new HashSet<Node>();

				opened.Add(startNode);
				openList.Add(startNode);
				int iterations = 0;

				while (openList.Count > 0)
				{
					iterations++;

					if(iterations > 10_000)
					{
						Debug.LogError("Too much iterations");
						break;
					}

					var node = openList[0];
					openList.RemoveAt(0);
					opened.Remove(node);
					closed.Add(node);

					var neighbors = GetNeighbours(node);

					foreach (Node neighbor in neighbors)
					{
						if (neighbor.IsNodeUnValid)
						{
							closed.Add(neighbor);
							continue;
						}

						if (closed.Contains(neighbor)) continue;

						if (neighbor.IsSegmentBlocker)
						{
							var roomCheck = GetNeighbours(neighbor);
							foreach (Node check in roomCheck)
							{
								bool foundCheck = foundIndexes.Contains(check.RoomIndex);
								if (check.IsNodeUnValid || (check.RoomIndex == locRoom.Index || foundCheck))
								{
									if (foundCheck)
									{
										bool isValid = true;
										var list = indexPosition[check.RoomIndex];

										foreach (Vector2 pos in list)
										{
											isValid = (pos - neighbor.WorldPosition).sqrMagnitude > _maxRoomTransition * _maxRoomTransition;

											if (!isValid) break;
										}

										if (!isValid) continue;
									}
								}

								if (!foundCheck)
								{
									foundIndexes.Add(check.RoomIndex);
									indexPosition.Add(check.RoomIndex, new List<Vector2> { neighbor.WorldPosition });
								}
								else
								{
									indexPosition[check.RoomIndex].Add(neighbor.WorldPosition);
								}
								foundConnections.Add(new Connection(
										new Vector2Int(startNode.XPos, startNode.YPos),
										FindCenterSegment(new Vector2Int(neighbor.XPos, neighbor.YPos)),
										locRoom.Index,
										check.RoomIndex,
										Vector2.Distance(startNode.WorldPosition, neighbor.WorldPosition)));
							}
						}
						else
						{
							opened.Add(neighbor);
							openList.Add(neighbor);
						}
					}
				}
				foundConnections.Sort(new ConnectionComparer());
				locRoom.Connections = foundConnections;
			}

			if (_excludeNotConnectedRooms)
			{
				for (int i = 0; i < rooms.Count; i++)
				{
					if (rooms[i].Connections.Count == 0)
					{
						rooms.RemoveAt(i);
						i = Mathf.Max(i - 1, 0);
					}
				}
			}

			_rooms = rooms.ToArray();
		}

		private Vector2Int FindCenterSegment(in Vector2Int pos)
		{
			if (!_grid[pos.x, pos.y].IsSegmentBlocker) return Vector2Int.zero;
				
			Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);
			Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);

			List<Node> opened = new List<Node>();
			HashSet<Node> closed = new HashSet<Node>();

			opened.Add(_grid[pos.x, pos.y]);

			while (opened.Count > 0)
			{
				var node = opened[0];
				max = new Vector2Int(Mathf.Max(max.x, node.XPos), Mathf.Max(max.y, node.YPos));
				min = new Vector2Int(Mathf.Min(min.x, node.XPos), Mathf.Min(min.y, node.YPos));

				opened.RemoveAt(0);
				closed.Add(node);

				var neighbors = GetNeighboursNonDiagonal(node);
				foreach(var neighbor in neighbors)
				{
					if (!neighbor.IsSegmentBlocker || closed.Contains(neighbor)) continue;

					opened.Add(neighbor);
				}
			}

			return (max + min) / 2;
		}

		private void MarkCovers()
		{
			for (int x = 1; x < _size.x - 1; x++)
			{
				for (int y = 1; y < _size.y - 1; y++)
				{
					var node = _grid[x, y];
					if (!node.Walkable) continue;

					if (!_grid[x, y + 1].Walkable || !_grid[x, y - 1].Walkable)
					{
						int locY = y;
						if (!_grid[x, y + 1].Walkable) locY += 1;
						else locY -= 1;

						if (_grid[x - 1, locY].Walkable)
						{
							node.Flags |= Node.NodeFlags.Cover;
							node.Direction = Node.CoverDirection.Left;
							node.NormalDirection = (!_grid[x, y + 1].Walkable) ? Node.CoverDirection.Down : Node.CoverDirection.Up;
						}
						else if (_grid[x + 1, locY].Walkable)
						{
							node.Flags |= Node.NodeFlags.Cover;
							node.Direction = Node.CoverDirection.Right;
							node.NormalDirection = (!_grid[x, y + 1].Walkable) ? Node.CoverDirection.Down : Node.CoverDirection.Up;
						}
					}

					if (!_grid[x + 1, y].Walkable || !_grid[x - 1, y].Walkable)
					{
						int locX = x;
						if (!_grid[x + 1, y].Walkable) locX += 1;
						else locX -= 1;

						if (_grid[locX, y - 1].Walkable)
						{
							node.Flags |= Node.NodeFlags.Cover;
							node.Direction = Node.CoverDirection.Down;
							node.NormalDirection = (!_grid[x + 1, y].Walkable) ? Node.CoverDirection.Left : Node.CoverDirection.Right;
						}
						else if (_grid[locX, y + 1].Walkable)
						{
							node.Flags |= Node.NodeFlags.Cover;
							node.Direction = Node.CoverDirection.Up;
							node.NormalDirection = (!_grid[x + 1, y].Walkable) ? Node.CoverDirection.Left : Node.CoverDirection.Right;
						}
					}

					if (node.IsCover)
					{
						int xDif = node.Direction == Node.CoverDirection.Right ? -1 : node.Direction == Node.CoverDirection.Left ? 1 : 0;
						int yDif = node.Direction == Node.CoverDirection.Up ? -1 : node.Direction == Node.CoverDirection.Down ? 1 : 0;

						int yStart = y;
						int xStart = x;

						int xCheck = node.NormalDirection == Node.CoverDirection.Right ? -1 : node.NormalDirection == Node.CoverDirection.Left ? 1 : 0;
						int yCheck = node.NormalDirection == Node.CoverDirection.Up ? -1 : node.NormalDirection == Node.CoverDirection.Down ? 1 : 0;

						for (int i = 0; i < _coverMinSize; i++)
						{
							int xLoc = xStart + i * xDif;
							int yLoc = yStart + i * yDif;

							if ((!IsNodeInRange(xLoc, yLoc) || !_grid[xLoc, yLoc].Walkable) || (IsNodeInRange(xLoc + xCheck, yLoc + yCheck) && _grid[xLoc + xCheck, yLoc + yCheck].Walkable))
							{
								node.Flags &= ~Node.NodeFlags.Cover;
								node.Direction = Node.CoverDirection.Right;
								break;
							}
						}

						if (node.IsCover)
						{
							for (int i = 1; i < _coverMinSize; i++)
							{
								int xLoc = xStart + i * xDif;
								int yLoc = yStart + i * yDif;

								_grid[xLoc, yLoc].Flags |= Node.NodeFlags.HardCover;
							}
						}
					}
				}
			}
		}

		public Vector2 WorldSize => _worldSize;
		public Node RandomNode => _grid[UnityEngine.Random.Range(0, _grid.GetLength(0)), UnityEngine.Random.Range(0, _grid.GetLength(1))];

		private void OnValidate()
		{
			_cellSize = Mathf.Max(0.01f, _cellSize);
		}

		private Node[] GetNeighbours(Node node)
		{
			List<Node> neighbours = new List<Node>();

			for (int i = -1; i < 2; i++)
			{
				for (int j = -1; j < 2; j++)
				{
					if (i == 0 && j == 0) continue;

					int cx = node.XPos + i;
					int cy = node.YPos + j;

					if (cx >= 0 && cx < _size.x && cy >= 0 && cy < _size.y)
					{
						neighbours.Add(_grid[cx, cy]);
					}
				}
			}

			return neighbours.ToArray();
		}

		private Node[] GetNeighboursNonDiagonal(Node node)
		{
			List<Node> neighbours = new List<Node>();

			for (int i = -1; i < 2; i++)
			{
				for (int j = -1; j < 2; j++)
				{
					if ((i == 0 && j == 0) || (Mathf.Abs(i) == 1 && Mathf.Abs(j) == 1)) continue;

					int cx = node.XPos + i;
					int cy = node.YPos + j;

					if (cx >= 0 && cx < _size.x && cy >= 0 && cy < _size.y)
					{
						neighbours.Add(_grid[cx, cy]);
					}
				}
			}

			return neighbours.ToArray();
		}

		private bool IsNodeInRange(int x, int y)
		{
			return x >= 0 && x < _grid.GetLength(0) &&
				y >= 0 && y < _grid.GetLength(1);
		}

		public void FindRoomPath(Vector2 start, Vector2 end)
		{
			var startNode = NodeFromWorldPoint(start);
			var endNode = NodeFromWorldPoint(end);

			var startRoom = _rooms[startNode.RoomIndex];
			var endRoom = _rooms[endNode.RoomIndex];

			List<Room> rooms = new List<Room>();
			HashSet<Room> open = new HashSet<Room>();
			HashSet<Room> close = new HashSet<Room>();

			rooms.Add(startRoom);
			open.Add(startRoom);

			while (open.Count > 0)
			{
				float best = float.MaxValue;
				Room selected = null;

				for (int i = 0; i < rooms.Count; i++)
				{
					float sDist = (rooms[i].GCenter - startRoom.GCenter).sqrMagnitude;
					float eDist = (rooms[i].GCenter - endRoom.GCenter).sqrMagnitude;

					if(sDist + eDist < best)
					{
						best = sDist;
						selected = rooms[i];
					}
				}

				rooms.Remove(selected);
				open.Remove(selected);
				close.Add(selected);

				for (int i = 0; i < selected.Connections.Count; i++)
				{

				}
			}
		}

		public Node NodeFromWorldPoint(Vector2 worldPosition)
		{
			float percentX = (worldPosition.x - transform.position.x + _worldSize.x / 2) / _worldSize.x;
			float percentY = (worldPosition.y - transform.position.y + _worldSize.y / 2) / _worldSize.y;
			percentX = Mathf.Clamp01(percentX);
			percentY = Mathf.Clamp01(percentY);

			int x = Mathf.RoundToInt((_size.x - 1) * percentX);
			int y = Mathf.RoundToInt((_size.y - 1) * percentY);

			return _grid[x, y];
		}

		public List<Node> Path = new List<Node>();
		public HashSet<Node> Closed = new HashSet<Node>();
		public HashSet<Node> Opened = new HashSet<Node>();
		private void OnDrawGizmos()
		{
			Gizmos.DrawWireCube(transform.position, _worldSize);
			if (!_debug) return;

			if (_grid == null) return;

			foreach (Node n in _grid)
			{
				if (_mode == DebugMode.Path)
				{
					if (Path.Contains(n))
					{
						Gizmos.color = Color.black;
						Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_cellDiameter - .1f));
						continue;
					}
					if (Closed.Contains(n))
					{
						Gizmos.color = Color.gray;
						Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_cellDiameter - .1f));
						continue;
					}
					if (Opened.Contains(n))
					{
						Gizmos.color = Color.blue;
						Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_cellDiameter - .1f));
						continue;
					}
				}

				if (_mode == DebugMode.Points)
				{
					Color defColor = Color.gray;

					if (n.RoomIndex != -1)
					{
						var state = UnityEngine.Random.state;
						UnityEngine.Random.InitState(n.RoomIndex);
						defColor = Color.HSVToRGB(UnityEngine.Random.value, 1, 0.5f);
						UnityEngine.Random.state = state;
					}

					if (!n.Walkable)
					{
						Gizmos.color = Color.red;
						Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_cellDiameter - .1f));
						continue;
					}

					if (n.IsUnreachable)
					{
						Gizmos.color = Color.black;
						Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_cellDiameter - .1f));
						continue;
					}

					if (n.IsCover)
					{
						Gizmos.color = Color.cyan;
						Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_cellDiameter - .1f));

						Gizmos.color = Color.red;
						Gizmos.DrawLine(n.WorldPosition, n.WorldPosition + Node.CoverDirection2Vector(n.Direction));

						Gizmos.color = Color.blue;
						Gizmos.DrawLine(n.WorldPosition, n.WorldPosition + Node.CoverDirection2Vector(n.NormalDirection));
						continue;
					}

					if (n.IsHardCover)
					{
						Gizmos.color = Color.Lerp(Color.cyan, defColor, 0.5f);
						if (n.IsSegmentBlocker)
						{
							Gizmos.color = Color.Lerp(Gizmos.color, Color.yellow, 0.5f);
						}
						Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_cellDiameter - .1f));
						continue;
					}

					if (n.IsSegmentBlocker)
					{
						Gizmos.color = Color.Lerp(Color.yellow, defColor, 0.5f);
						Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_cellDiameter - .1f));
						continue;
					}

					Gizmos.color = defColor;
					Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_cellDiameter - .1f));
					continue;
				}
				Gizmos.color = n.Walkable ? Color.Lerp(Color.green, Color.red, n.WallFactor) : Color.red;
				Gizmos.DrawCube(n.WorldPosition, Vector3.one * (_cellDiameter - .1f));
			}
		}

		public float CellSize => _cellDiameter;
		public int TotalSize => _grid.Length;
	}

	[Serializable]
	public sealed class Room
	{
		[SerializeField] private Vector2Int _rawMin;
		[SerializeField] private Vector2Int _rawMax;
		[SerializeField] private int _index;
		public readonly Vector2Int RawMin;
		public readonly Vector2Int RawMax;
		public readonly Vector2 Center;
		public readonly int Index;
		public List<Connection> Connections = new List<Connection>();

		private BitArray _ownedCells;

		public Room(Vector2Int rawMin, Vector2Int rawMax, Vector2 center, int index)
		{
			RawMin = _rawMin = rawMin;
			RawMax = _rawMax = rawMax;
			Center = center;
			Index = _index = index;
		}

		public void SetBitMask(BitArray array)
		{
			_ownedCells = array;
		}

		public Vector2Int GCenter => (RawMin + RawMax) / 2;
	}

	public sealed class ConnectionComparer : IComparer<Connection>
	{
		public int Compare(Connection x, Connection y)
		{
			return (int)Mathf.Sign(x.RoomInd2 - y.RoomInd2);
		}
	}

	[Serializable]
	public sealed class Connection
	{
		public readonly Vector2Int NodeStart;
		public readonly Vector2Int NodeEnd;
		public readonly int RoomInd1;
		public readonly int RoomInd2;
		public readonly float Distance;

		public Connection(Vector2Int nodeStart, Vector2Int nodeEnd, int roomInd1, int roomInd2, float distance)
		{
			NodeStart = nodeStart;
			NodeEnd = nodeEnd;
			RoomInd1 = roomInd1;
			RoomInd2 = roomInd2;
			Distance = distance;
		}

		public override int GetHashCode()
		{
			return RoomInd1 ^ RoomInd2;
		}
	}

	public sealed class Node : IComparable<Node>
	{
		[Flags]
		public enum NodeFlags
		{
			Cover = 1,
			HardCover = 1 << 1,
			SegmentBlocker = 1 << 2,
			OneWaySegment = 1 << 3,
			UnReachable = 1 << 4
		}

		public enum CoverDirection
		{
			Right,
			Up,
			Left,
			Down
		}

		public readonly bool Walkable;
		public readonly Vector2 WorldPosition;
		public readonly int XPos;
		public readonly int YPos;
		public Node[] Neighbours;
		public Node Previous;
		public NodeFlags Flags;
		public CoverDirection Direction;
		public CoverDirection NormalDirection;

		public float WallFactor;
		public int HCost;
		public int GCost;
		public int ACost;
		public int RoomIndex;
		public int FCost => (HCost + GCost) + (int)((HCost + GCost) * WallFactor);

		public Node(bool walkable, Vector2 worldPos, int x, int y)
		{
			Walkable = walkable;
			WorldPosition = worldPos;
			XPos = x;
			YPos = y;

			Neighbours = null;
			Previous = null;
			WallFactor = HCost = GCost = 0;
			RoomIndex = -1;
		}

		public void SetNeighbours(Node[] neighbours)
		{
			int total = neighbours.Length;
			int unWalkable = 0;
			for (int i = 0; i < total; i++)
			{
				if (neighbours[i].Walkable) continue;

				unWalkable++;
			}

			Neighbours = neighbours;
			WallFactor = (float)unWalkable / total * 0.5f;
		}

		public int CompareTo(Node other)
		{
			int dif = FCost - other.FCost;
			return Math.Sign(dif);
		}

		public static Vector2 CoverDirection2Vector(CoverDirection direction)
		{
			switch (direction)
			{
				case CoverDirection.Left: return Vector2.left;
				case CoverDirection.Right: return Vector2.right;
				case CoverDirection.Up: return Vector2.up;
				case CoverDirection.Down: return Vector2.down;
			}

			throw new Exception();
		}

		public bool IsNodeValid => !IsUnreachable && Walkable;
		public bool IsNodeUnValid => IsUnreachable || !Walkable;
		public bool IsCover => (Flags & NodeFlags.Cover) != 0;
		public bool IsHardCover => (Flags & NodeFlags.HardCover) != 0;
		public bool IsSegmentBlocker => (Flags & NodeFlags.SegmentBlocker) != 0;
		public bool IsUnreachable => (Flags & NodeFlags.UnReachable) != 0;
	}
}