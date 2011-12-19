using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SalarAnts.Defination;
using SalarAnts.Pack;

namespace SalarAnts.Classes
{
	public static class CheckingAround
	{
		/// <summary>
		/// Finding nodes in the range of specified radious!
		/// </summary>
		public static IList<Location> FindTargetsInDirectRadius(
			IGameState state,
			Location locationToCheck,
			int radiousOfCheck,
			Func<Location, bool> isPassable,
			Func<Location, bool> isTarget)
		{
			//var passedNodes = 0;
			var neighborNodes = new Location[4];
			var closedNodes = new List<Location>();
			var openNodes = new Queue<Location>();
			var targets = new List<Location>();

			// initial!
			openNodes.Enqueue(locationToCheck);

			while (openNodes.Count > 0)
			{
				var openNode = openNodes.Dequeue();
				closedNodes.Add(openNode);

				// get neighbors!
				GetNeighborNodesMirrored(state, openNode, locationToCheck, neighborNodes);

				for (int i = 0; i < neighborNodes.Length; i++)
				{
					var neighborN = neighborNodes[i];

					// checking if the location is in the radious!
					var neighborDistance = state.GetDistanceDirect(locationToCheck, neighborN);
					if (neighborDistance > radiousOfCheck)
					{
						closedNodes.Add(neighborN);
						continue;
					}

					//--------------------------------
					// BOTTLE NECK: Uses lots of CPU!
					// is this node already checked?

					if (closedNodes.Exists(x => x.EqualTo(neighborN)))
						continue;

					// use the method!
					if (isPassable.Invoke(neighborN))
					{
						if (isTarget(neighborN))
							targets.Add(neighborN);

						openNodes.Enqueue(neighborN);
						closedNodes.Add(neighborN);
					}
					else
					{
						closedNodes.Add(neighborN);
					}
				}
			}

			// open nodes!
			return targets;
		}
		/// <summary>
		/// Finding nodes in the range of specified radious!
		/// </summary>
		public static IList<Location> FindTargetsInRadius(
			IGameState state,
			Location locationToCheck,
			int radiousOfCheck,
			Func<int, int, bool> isPassable,
			Func<Location, bool> isTarget)
		{
			//var passedNodes = 0;
			var neighborNodes = new Location[4];
			var closedNodes = new List<Location>();
			var openNodes = new Queue<Location>();
			var targets = new List<Location>();

			// initial!
			openNodes.Enqueue(locationToCheck);

			while (openNodes.Count > 0)
			{
				var openNode = openNodes.Dequeue();
				closedNodes.Add(openNode);

				//passedNodes++;

				//// checking if the location is in the radious!
				//var distanceOpenNode = state.GetDistance(locationToCheck, openNode);
				//if (distanceOpenNode > radiousOfCheck)
				//{
				//    continue;
				//}


				// get neighbors!
				GetNeighborNodesMirrored(state, openNode, locationToCheck, neighborNodes);

				for (int i = 0; i < neighborNodes.Length; i++)
				{
					var neighborN = neighborNodes[i];

					// checking if the location is in the radious!
					var neighborDistance = state.GetDistance(locationToCheck, neighborN);
					if (neighborDistance > radiousOfCheck)
					{
						closedNodes.Add(neighborN);
						continue;
					}

					//--------------------------------
					// BOTTLE NECK: Uses lots of CPU!
					// is this node already checked?

					//// SLOWER
					//if (closedNodes.Contains(neighborN))
					//    continue;
					if (closedNodes.Exists(x => x.EqualTo(neighborN)))
						continue;

					// use the method!
					if (isPassable.Invoke(neighborN.RowY, neighborN.ColX))
					{
						if (isTarget(neighborN))
							targets.Add(neighborN);

						openNodes.Enqueue(neighborN);
						closedNodes.Add(neighborN);
					}
					else
					{
						closedNodes.Add(neighborN);
					}
				}
			}

			// open nodes!
			return targets;
		}

		/// <summary>
		/// TESTED! 
		/// </summary>
		static Location FindTarget(
			IGameState state,
			Location locationToCheck,
			int maxLocationToCheck,
			Func<Location, bool> isPassable,
			Func<Location, bool> isTarget)
		{
			var passedNodes = 0;
			var neighborNodes = new Location[4];
			var closedNodes = new List<Location>();
			var openNodes = new Queue<LinkedItem<Location>>();
			bool targetFound = false;
			var targetFoundWeight = int.MaxValue;
			Location targetFoundLocation = null;

			// initial!
			locationToCheck.ZVal = 0;
			openNodes.Enqueue(new LinkedItem<Location>(locationToCheck, null, null));

			while (openNodes.Count > 0)
			{
				var openNode = openNodes.Dequeue();
				closedNodes.Add(openNode.Value);

				passedNodes++;

				// 4 blocks are passable!
				if (passedNodes / 4 > maxLocationToCheck)
				{
					// finish this!
					break;
				}

				// this means we finished a spin around one time!
				// this ensures the best result! 
				if (targetFound && targetFoundWeight < openNode.Value.ZVal)
				{
					return targetFoundLocation;
				}

				// get neibors!
				GetNeighborNodesMirrored(state, openNode.Value, locationToCheck, neighborNodes);

				foreach (var neighborN in neighborNodes)
				{
					// next node!
					//if (closedNodes.Contains(neighborN))
					if (closedNodes.Exists(x => x.EqualTo(neighborN)))
						continue;

					// use the method!
					if (isPassable.Invoke(neighborN))
					{
						if (openNode.Previous != null)
							neighborN.ZVal = openNode.Value.ZVal + 1;
						else
							neighborN.ZVal = 1;

						// checking if this is target!
						if (isTarget(neighborN))
						{
							//#if TestInput
							//                            SalarAnts.TestInput.SalarAntsVisualRunner.AntMarkLocation(neighborN.ColX, neighborN.RowY, 3);
							//#endif

							if (targetFound)
							{
								if (neighborN.ZVal < targetFoundWeight)
								{
									targetFoundWeight = neighborN.ZVal;
									targetFoundLocation = neighborN;
								}
							}
							else
							{
								targetFound = true;
								targetFoundWeight = neighborN.ZVal;
								targetFoundLocation = neighborN;
							}
						}

						openNodes.Enqueue(new LinkedItem<Location>(neighborN, null, openNode.Value));
					}
					else
					{
						closedNodes.Add(neighborN);
					}
				}
			}

			return targetFoundLocation;
		}

		static IList<Location> FindTargets(
			IGameState state,
			Location locationToCheck,
			int maxLocationToCheck,
			Func<Location, bool> isPassable,
			Func<Location, bool> isTarget)
		{
			var passedNodes = 0;
			var neighborNodes = new Location[4];
			var closedNodes = new List<Location>();
			var openNodes = new Queue<Location>();
			var targets = new List<Location>();

			// initial!
			openNodes.Enqueue(locationToCheck);

			while (openNodes.Count > 0)
			{
				var openNode = openNodes.Dequeue();
				closedNodes.Add(openNode);

				passedNodes++;

				// 4 blocks are passable!
				if (passedNodes / 4 > maxLocationToCheck)
				{
					// finish this!
					break;
				}

				// get neibors!
				GetNeighborNodesMirrored(state, openNode, locationToCheck, neighborNodes);

				foreach (var neighborN in neighborNodes)
				{
					// next node!
					if (closedNodes.Exists(x => x.EqualTo(neighborN)))
						//if (closedNodes.Contains(neighborN))
						continue;

					// use the method!
					if (isPassable.Invoke(neighborN))
					{
						if (isTarget(neighborN))
							targets.Add(neighborN);

						openNodes.Enqueue(neighborN);
					}
					else
					{
						closedNodes.Add(neighborN);
					}
				}
			}

			// open nodes!
			return targets;
		}

		/// <summary>
		/// TESTED! 
		/// </summary>
		static IEnumerable<Location> FindPassable(
			IGameState state,
			Location locationToCheck,
			int maxLocationToCheck,
			Func<Location, bool> isPassable)
		{
			var passedNodes = 0;
			var neighborNodes = new Location[4];
			var closedNodes = new List<Location>();
			var openNodes = new Queue<Location>();

			// initial!
			openNodes.Enqueue(locationToCheck);

			while (openNodes.Count > 0)
			{
				var openNode = openNodes.Dequeue();
				closedNodes.Add(openNode);

				passedNodes++;

				// 4 blocks are passable!
				if (passedNodes / 4 > maxLocationToCheck)
				{
					// finish this!
					break;
				}

				// get neibors!
				GetNeighborNodesMirrored(state, openNode, locationToCheck, neighborNodes);

				foreach (var neighborN in neighborNodes)
				{
					// next node!
					if (closedNodes.Exists(x => x.EqualTo(neighborN)))
						continue;

					// use the method!
					if (isPassable.Invoke(neighborN))
					{
						openNodes.Enqueue(neighborN);
					}
					else
					{
						closedNodes.Add(neighborN);
					}
				}
			}

			// open nodes!
			return openNodes;
		}

		/// <summary>
		/// Checks around of a location for free way!
		/// </summary>
		public static IList<Location> CheckAround(IGameState state, Location locationToCheck, int maxLocationToCheck, bool checkMirrorLocations)
		{
			var passedNodes = 0;
			var neighborNodes = new Location[4];
			var closedNodes = new List<Location>();
			var openNodes = new Queue<Location>();

			// initial!
			openNodes.Enqueue(locationToCheck);

			while (openNodes.Count > 0)
			{
				var openNode = openNodes.Dequeue();
				closedNodes.Add(openNode);

				passedNodes++;

				// 4 blocks are passable!
				if (passedNodes / 4 > maxLocationToCheck)
				{
					// finish this!
					break;
				}

				// get neibors!
				if (checkMirrorLocations)
					GetNeighborNodesMirrored(state, openNode, locationToCheck, neighborNodes);
				else
					GetNeighborNodesSimple(openNode, locationToCheck, neighborNodes);

				for (int i = 0; i < neighborNodes.Length; i++)
				{
					var neighborN = neighborNodes[i];

					//Location neighborN = null;
					//if (mirrorNodesAround)
					//    neighborN = MirrorOutsideNodeIfPossible(state, neighborNodeItem);
					//if (neighborN == null)
					//{
					//    neighborN = neighborNodeItem;
					//}

					// next node!
					if (closedNodes.Exists(x => x.EqualTo(neighborN)))
						continue;

					if (state.GetIsPassableSafe(neighborN))
					{
						openNodes.Enqueue(neighborN);

						closedNodes.Add(neighborN);
					}
					else
					{
						closedNodes.Add(neighborN);
					}
				}
			}

			// open nodes!
			return openNodes.ToList();
		}

		private static void GetNeighborNodes(Location locationToCheck, Location[] neighborNodes)
		{
			neighborNodes[0] = (new Location(locationToCheck.RowY + 1, locationToCheck.ColX));
			neighborNodes[1] = (new Location(locationToCheck.RowY - 1, locationToCheck.ColX));
			neighborNodes[2] = (new Location(locationToCheck.RowY, locationToCheck.ColX + 1));
			neighborNodes[3] = (new Location(locationToCheck.RowY, locationToCheck.ColX - 1));
		}

		private static void GetNeighborNodesMirrored(IGameState state, Location locationToCheck, Location sourceNode, Location[] neighborNodes)
		{
			if (sourceNode.ColX == locationToCheck.ColX)
			{
				if (locationToCheck.RowY > sourceNode.RowY)
				{
					neighborNodes[0] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY - 1, locationToCheck.ColX));
					neighborNodes[3] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY + 1, locationToCheck.ColX));
				}
				else
				{
					neighborNodes[0] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY + 1, locationToCheck.ColX));
					neighborNodes[3] = (new Location(locationToCheck.RowY - 1, locationToCheck.ColX));
				}
				neighborNodes[1] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY, locationToCheck.ColX + 1));
				neighborNodes[2] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY, locationToCheck.ColX - 1));
			}
			else if (sourceNode.RowY == locationToCheck.RowY)
			{
				if (locationToCheck.ColX > sourceNode.ColX)
				{
					neighborNodes[0] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY, locationToCheck.ColX - 1));
					neighborNodes[3] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY, locationToCheck.ColX + 1));
				}
				else
				{
					neighborNodes[0] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY, locationToCheck.ColX + 1));
					neighborNodes[3] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY, locationToCheck.ColX - 1));
				}
				neighborNodes[1] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY + 1, locationToCheck.ColX));
				neighborNodes[2] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY - 1, locationToCheck.ColX));
			}
			else
			{
				neighborNodes[0] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY, locationToCheck.ColX - 1));
				neighborNodes[1] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY, locationToCheck.ColX + 1));
				neighborNodes[2] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY + 1, locationToCheck.ColX));
				neighborNodes[3] = (Common.CreateMirrorableLocation(state, locationToCheck.RowY - 1, locationToCheck.ColX));
			}
		}
		private static void GetNeighborNodesSimple(Location locationToCheck, Location sourceNode, Location[] neighborNodes)
		{
			if (sourceNode.ColX == locationToCheck.ColX)
			{
				if (locationToCheck.RowY > sourceNode.RowY)
				{
					neighborNodes[0] = (new Location(locationToCheck.RowY - 1, locationToCheck.ColX));
					neighborNodes[3] = (new Location(locationToCheck.RowY + 1, locationToCheck.ColX));
				}
				else
				{
					neighborNodes[0] = (new Location(locationToCheck.RowY + 1, locationToCheck.ColX));
					neighborNodes[3] = (new Location(locationToCheck.RowY - 1, locationToCheck.ColX));
				}
				neighborNodes[1] = (new Location(locationToCheck.RowY, locationToCheck.ColX + 1));
				neighborNodes[2] = (new Location(locationToCheck.RowY, locationToCheck.ColX - 1));
			}
			else if (sourceNode.RowY == locationToCheck.RowY)
			{
				if (locationToCheck.ColX > sourceNode.ColX)
				{
					neighborNodes[0] = (new Location(locationToCheck.RowY, locationToCheck.ColX - 1));
					neighborNodes[3] = (new Location(locationToCheck.RowY, locationToCheck.ColX + 1));
				}
				else
				{
					neighborNodes[0] = (new Location(locationToCheck.RowY, locationToCheck.ColX + 1));
					neighborNodes[3] = (new Location(locationToCheck.RowY, locationToCheck.ColX - 1));
				}
				neighborNodes[1] = (new Location(locationToCheck.RowY + 1, locationToCheck.ColX));
				neighborNodes[2] = (new Location(locationToCheck.RowY - 1, locationToCheck.ColX));
			}
			else
			{
				neighborNodes[0] = (new Location(locationToCheck.RowY, locationToCheck.ColX - 1));
				neighborNodes[1] = (new Location(locationToCheck.RowY, locationToCheck.ColX + 1));
				neighborNodes[2] = (new Location(locationToCheck.RowY + 1, locationToCheck.ColX));
				neighborNodes[3] = (new Location(locationToCheck.RowY - 1, locationToCheck.ColX));
			}
		}


		private static Location MirrorOutsideNodeIfPossible(IGameState state, Location loc)
		{
			Location result = null;
			if (loc.ColX > state.Width)
			{
				result = new Location(loc.RowY, loc.ColX - state.Width);
			}
			else if (loc.ColX < 0)
			{
				result = new Location(loc.RowY, state.Width - Math.Abs(loc.ColX));
			}
			if (loc.RowY > state.Height)
			{
				result = new Location(loc.RowY - state.Height, loc.ColX);
			}
			else if (loc.RowY < 0)
			{
				result = new Location(state.Height - Math.Abs(loc.RowY), loc.ColX);
			}
			return result;
		}
	}
}
