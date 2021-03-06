﻿
namespace SettlersEngine
{
	
	public class AntAStarSolver<TPathNode, TUserContext> : SettlersEngine.SpatialAStar<TPathNode, TUserContext> where TPathNode
	: SettlersEngine.IPathNode<TUserContext>
	{
		//protected override Double Heuristic(PathNode inStart, PathNode inEnd)
		//{
		//    return Math.Abs(inStart.X - inEnd.X) + Math.Abs(inStart.Y - inEnd.Y);
		//}

		//protected override Double NeighborDistance(PathNode inStart, PathNode inEnd)
		//{
		//    return Heuristic(inStart, inEnd);
		//}

		public AntAStarSolver(TPathNode[,] inGrid)
			: base(inGrid)
		{
		}
	}


}
