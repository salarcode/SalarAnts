using System;
using SalarAnts.Defination;
using SalarAnts.Pack;
using SettlersEngine;

namespace SalarAnts.AStarV2
{
	public static class AntAStarMain
	{
		public static SearchPlanDetail GetFindPathPlan(
			IGameState state,
			AntAStarSolver<AntAStarPathNode, Object> aStar,
			Location ant,
			Location destination,
			int threshold)
		{
			//var aStar = new AntAStarSolver<AntAStarPathNode, Object>(antsMap);
			aStar.SearchLimit = threshold;

			// search for result
			var startPoint = new Point(ant.RowY, ant.ColX);
			var endPoint = new Point(destination.RowY, destination.ColX);

			// searching!!!
			var solution = aStar.Search(startPoint, endPoint, null);

			if (solution != null && solution.Count > 0)
			{
				var plan = new SearchPlanDetail();
				plan.Ant = ant;
				plan.Path = solution;
				plan.Goal = destination;
				plan.GoalType = state[destination];
				
				var node = solution.First;
				if (node != null)
				{
					plan.PathCurrent = node;
				}
				else
				{
					plan.PathCurrent = null;
				}

				// weight!
				plan.LeftSteps = solution.Count;

				// is goal reached?
				var last = solution.Last.Value;
				if (last.X == destination.ColX && last.Y == destination.RowY)
				{
					// yeah
				}
				else
				{
					plan.TempDest = new Location(last.Y, last.X);
					plan.TempDestType = state[plan.TempDest];
				}

				// plan details!
				return plan;
			}

			return null;
		}

		//private static SearchPlanDetail GetFindPathPlan(
		//    GameState state,
		//    AntAStarPathNode[,] antsMap,
		//    Location ant,
		//    Location destination,
		//    int threshold)
		//{
		//    var aStar = new AntAStarSolver<AntAStarPathNode, Object>(antsMap);
		//    aStar.SearchLimit = threshold;

		//    // search for result
		//    var startPoint = new Point(ant.RowY, ant.ColX);
		//    var endPoint = new Point(destination.RowY, destination.ColX);

		//    // searching!!!
		//    var solution = aStar.Search(startPoint, endPoint, null);

		//    if (solution != null && solution.Count > 0)
		//    {
		//        var plan = new SearchPlanDetail();
		//        plan.Ant = ant;
		//        plan.Destination = destination;
		//        plan.Path = solution;

		//        var node = solution.First;
		//        if (node != null)
		//        {
		//            plan.PathCurrent = node;
		//        }
		//        else
		//        {
		//            plan.PathCurrent = null;
		//        }

		//        // weight!
		//        plan.RemainingStep = solution.Count;

		//        // plan details!
		//        return plan;
		//    }

		//    return null;
		//}

		//        public static int FindPathCost(
		//            GameState state,
		//            AntAStarPathNode[,] antsMap,
		//            Location startLocation,
		//            Location endLocation,
		//            int threshold,
		//            int c = 1)
		//        {
		//            var aStar = new AntAStarSolver<AntAStarPathNode, Object>(antsMap);
		//            aStar.SearchLimit = threshold;

		//            // search for result
		//            var startPoint = new Point(startLocation.RowY, startLocation.ColX);
		//            var endPoint = new Point(endLocation.RowY, endLocation.ColX);

		//            var solution = aStar.Search(startPoint, endPoint, null);


		//#if TestInput
		//            //if (solution != null && solution.Count > 0)
		//            //    foreach (AntAStarPathNode starPathNode in solution)
		//            //    {
		//            //        SalarAntsVisualRunner.AntMarkLocation(starPathNode.X, starPathNode.Y, c);
		//            //    }
		//#endif

		//            var maxValue = int.MinValue;

		//            if (solution != null && solution.Count > 0)
		//            {
		//                startLocation._closeLocation = null;
		//                startLocation._farLocation = null;

		//                // frist step location
		//                Location loc;
		//                var node = solution.First.Next;
		//                if (node != null)
		//                {
		//                    loc = new Location(node.Value.Y, node.Value.X);
		//                    startLocation._closeLocation = loc;
		//                }

		//                // final step location
		//                node = solution.Last;
		//                if (node != null)
		//                {
		//                    loc = new Location(node.Value.Y, node.Value.X);
		//                    startLocation._farLocation = loc;
		//                }

		//                // the cost of steps
		//                maxValue = solution.Count;

		//            }

		//            if (maxValue == int.MinValue)
		//                return int.MaxValue;
		//            return maxValue;
		//        }
	}
}
