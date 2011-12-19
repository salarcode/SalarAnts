using System;
using SalarAnts.Pack;

namespace SettlersEngine
{
	public class AntAStarPathNode : SettlersEngine.IPathNode<Object>
	{
		public Int32 X;
		public Int32 Y;
		//public Boolean IsWall { get; set; }
		//public Tile type;

		public bool IsWalkable(Object unused)
		{
			//if (unused is Tile)
			//{
			//    var t = (Tile)unused;
			//    return unoccupied(t);
			//}
			//return true;
			return Ants.GameStateInstance.GetIsUnoccupiedSafe(Y, X);
			//return unoccupied(type);
		}

		public Location ToLocation()
		{
			return new Location(Y, X);
		}

		public override string ToString()
		{
			return "rowY: " + X + " ,colX: " + Y;// +", " + type;
		}

		public static AntAStarPathNode[,] GenerateMap(Tile[,] map, int rowY, int colX)
		{
			var result = new AntAStarPathNode[rowY, colX];
			for (int row = 0; row < rowY; row++)
			{
				for (int col = 0; col < colX; col++)
				{
					var t = map[row, col];

					var node = new AntAStarPathNode();
					node.X = col;
					node.Y = row;
					//node.type = t;
					result[row, col] = node;
				}
			}
			return result;
		}

		static bool passable(Tile tile)
		{
			// true if not water
			return tile != Tile.Water;
		}

		static bool unoccupied(Tile tile)
		{
			// true if no ants are at the location
			return passable(tile) && tile != Tile.AntEnemy && tile != Tile.AntMine;
		}

	}

}
