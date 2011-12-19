using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SalarAnts.Pack
{
	public static class GameStateHelpers
	{
		public static Location GetDirectionLocationCloser(this IGameState state, Location loc1, Location loc2, Location closer)
		{
			//return loc2;
			var directions = state.GetDirections(loc1, loc2);
			if (directions.Count > 1)
			{
				var dir = directions.MinValue(
					x =>
					{
						var minLoc = state.GetDestination(loc1, x);
						if (state.GetIsPassableSafe(minLoc))
							return state.GetDistanceDirect(minLoc, closer);
						return 1000000;
					});

				return state.GetDestination(loc1, dir);
			}
			return loc2;
		}

		public static Location GetDirectionLocationFar(this IGameState state, Location loc1, Location loc2, Location far)
		{
			//return loc2;
			var directions = state.GetDirections(loc1, loc2);
			if (directions.Count > 1)
			{
				var dir = directions.MaxValue(
					x =>
					{
						var minLoc = state.GetDestination(loc1, x);
						if (state.GetIsPassableSafe(minLoc))
							return state.GetDistanceDirect(minLoc, far);
						return -1;
					});

				return state.GetDestination(loc1, dir);
			}
			return loc2;
		}

		public static Location GetDirectionLocationPassable(this IGameState state, Location loc1, Location loc2)
		{
			//return loc2;
			var directions = state.GetDirections(loc1, loc2);
			if (directions.Count > 1)
			{
				foreach (var direction in directions)
				{
					var loc = state.GetDestination(loc1, direction);
					if (state.GetIsPassableSafe(loc))
					{
						return loc;
					}
				}
			}
			return loc2;
		}

		public static int GetDistanceDirect(this IGameState state, Location p1, Location p2)
		{
			return (int)Math.Sqrt(Math.Pow((p1.ColX - p2.ColX), 2) + Math.Pow((p1.RowY - p2.RowY), 2));
		}

        public static int GetDistanceDirect(this IGameState state, int y1, int x1, int y2, int x2)
		{
			return (int)Math.Sqrt(Math.Pow((x1 - x2), 2) + Math.Pow((y1 - y2), 2));
		}

		public static bool LocationIsInMap(this IGameState state, Location location)
		{
			if (location.RowY < 0 || location.ColX < 0)
				return false;
			if (location.RowY > state.Height)
				return false;
			if (location.ColX > state.Width)
				return false;
			return true;
		}
		public static bool GetIsPassableSafe(this IGameState state, Location location)
		{
			if (location.RowY < 0 || location.ColX < 0)
				return false;
			if (location.RowY > state.Height)
				return false;
			if (location.ColX > state.Width)
				return false;
			try
			{
				return state.GetIsPassable(location);
			}
			catch (Exception)
			{
				return false;
			}
		}
		public static bool GetIsPassableSafe(this IGameState state, int y, int x)
		{
			if (y < 0 || x < 0)
				return false;
			if (y > state.Height)
				return false;
			if (x > state.Width)
				return false;
			try
			{
				return state.GetIsPassable(y, x);
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static bool GetIsUnoccupiedMirrored(this IGameState state, Location location)
		{
			int x = location.ColX;
			int y = location.RowY;

			if (y > state.Height)
				y = y - state.Height;
			if (x > state.Width)
				x = x - state.Width;
			if (x < 0)
				x = state.Width - Math.Abs(x);
			if (y < 0)
				y = state.Height - Math.Abs(y);

			try
			{
				return state.GetIsUnoccupied(y, x);
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static bool GetIsUnoccupiedSafe(this IGameState state, Location location)
		{
			if (location.RowY < 0 || location.ColX < 0)
				return false;
			if (location.RowY > state.Height)
				return false;
			if (location.ColX > state.Width)
				return false;
			try
			{
				return state.GetIsUnoccupied(location);
			}
			catch (Exception)
			{
				return false;
			}
		}
		public static bool GetIsUnoccupiedSafe(this IGameState state, int y, int x)
		{
			if (y < 0 || x < 0)
				return false;
			if (y > state.Height)
				return false;
			if (x > state.Width)
				return false;
			try
			{
				return state.GetIsUnoccupied(y, x);
			}
			catch (Exception)
			{
				return false;
			}
		}

	}
}
