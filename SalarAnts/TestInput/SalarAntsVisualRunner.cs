using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if TestInput_NOT
using System.Threading;
using System.Drawing;
using AntsVisualizer;
using SalarAnts.Pack;
using SalarAnts.SalarBot;
using SettlersEngine;

#endif

namespace SalarAnts.TestInput
{
#if TestInput_NOT
	public class SalarAntsVisualRunner
	{
		private static Visualizer _visualizer = new Visualizer();
		private static bool _visualStarted = false;

		public static Visualizer Visualizer
		{
			get { return _visualizer; }
		}

		public static void VisualStart()
		{
			if (_visualStarted == false)
			{
				_visualStarted = true;
				_visualizer.StartVisualizerForm();
			}
		}

		public static void UpdateVisualInit(IGameState state)
		{
			VisualStart();
			_visualizer.InitVisualMap(state.Width, state.Height);
			DrawMapTiles(state);
		}

		public static void DrawPlans(PlanManagerV1 planMan )
		{
			foreach (var attackPlan in planMan.AttackPlans)
			{
				MarkLocation(attackPlan.NextStep, Brushes.Red, 2);
			}
			//foreach (var p in planMan.ExplorePlans)
			//{
			//    MarkLocation(p.GetFinalMoveLoc(), Brushes.Blue, 2);
			//}
			//foreach (var p in planMan.FoodPlans)
			//{
			//    MarkLocation(p.GetFinalMoveLoc(), Brushes.Green, 2);
			//}
			foreach (var p in planMan.ExplorePlans)
			{
				MarkLocation(p.PathCurrent.Value.X, p.PathCurrent.Value.Y, Brushes.Blue, 5);
			}
			foreach (var p in planMan.FoodPlans)
			{
				MarkLocation(p.PathCurrent.Value.X,p.PathCurrent.Value.Y, Brushes.Green, 2);
			}
		}


		public static void DrawAnts(IList<Location> myAntsList, IList<Location> enemyAntsList)
		{
			foreach (Location ant in myAntsList)
			{
				_visualizer.DrawMyAnt(ant.RowY, ant.ColX);
			}
			foreach (Location ant in enemyAntsList)
			{
				_visualizer.DrawEnemyAnt(ant.RowY, ant.ColX);
			}
		}

		public static void MarkLocation(int colX, int rowY, Brush color, int val)
		{
			_visualizer.MarkLocation(rowY, colX, color, val);
		}

		public static void MarkLocation(int colX, int rowY, int val)
		{
			_visualizer.MarkLocation(rowY, colX, val);
		}

		public static void MarkLocation(Location loc, int val)
		{
			_visualizer.MarkLocation(loc.RowY, loc.ColX, val);
		}

		public static void MarkLocation(Location loc, Brush color, int val = 2)
		{
			_visualizer.MarkLocation(loc.RowY, loc.ColX, color, val);
		}

		public static void DrawMyAnt(int rowY, int colX)
		{
			_visualizer.DrawMyAnt(rowY, colX);
		}

		public static void DrawEnemyAnt(int rowY, int colX)
		{
			_visualizer.DrawEnemyAnt(rowY, colX);
		}

		static void DrawMapTiles(IGameState state)
		{
			for (int row = 0; row < state.Height; row++)
			{
				for (int col = 0; col < state.Width; col++)
				{
					var tile = (AntsVisualizer.AntTile)((int)state[row, col]);
					_visualizer.DrawTile(tile, row, col);
				}
			}
		}
	}

	//public class SalarAntsVisualRunner_OLD
	//{
	//    public static void VisualStart()
	//    {
	//        Thread appThread = new Thread(() =>
	//                                        {
	//                                            frmAntVisual.AntRunApplication();
	//                                        });
	//        appThread.Start();
	//    }

	//    public static void UpdateVisualInit(IGameState state)
	//    {
	//        frmAntVisual.AntInitVisual(state.Width, state.Height);
	//        AntTile[,] tiles = GetVisualMap(state);
	//        frmAntVisual.AntInitMap(tiles);
	//    }

	//    public static AntTile[,] GetVisualMap(IGameState state)
	//    {
	//        var result = new AntTile[state.Height, state.Width];
	//        for (int row = 0; row < state.Height; row++)
	//        {
	//            for (int col = 0; col < state.Width; col++)
	//            {
	//                result[row, col] = (AntTile)((int)state[row, col]);
	//            }
	//        }
	//        return result;
	//    }

	//    public static void AntSetAnts(List<Location> myAntsList, List<Location> enemyAntsList)
	//    {
	//        var myAnts = new List<AntMapLocation>(myAntsList.Count);
	//        var enemyAnts = new List<AntMapLocation>(enemyAntsList.Count);

	//        foreach (Location enemy in enemyAntsList)
	//        {
	//            enemyAnts.Add(new AntMapLocation(enemy.ColX, enemy.RowY));
	//        }

	//        foreach (Location ant in myAntsList)
	//        {
	//            myAnts.Add(new AntMapLocation(ant.ColX, ant.RowY));
	//        }

	//        frmAntVisual.AntSetAnts(myAnts, enemyAnts);
	//    }

	//    public static void AntMarkLocation(IEnumerable<AntAStarPathNode> locList, int val)
	//    {
	//        foreach (var n in locList)
	//        {
	//            AntMarkLocation(n.X, n.Y, val);
	//        }
	//    }

	//    public static void AntMarkLocation(int colX, int rowY, int val)
	//    {
	//        frmAntVisual.AntMarkLocation(new AntMapLocation(colX, rowY), val);
	//    }

	//    //public static void AntIssueOrderToLoc(GameState state, Location ant, char dir)
	//    //{
	//    //    var location = state.GetDestination(ant, dir);
	//    //    frmAntVisual.AntIssueOrderToLoc(new AntMapLocation(ant.colX, ant.rowY), new AntMapLocation(location.colX, location.rowY));
	//    //}
	//}
#endif
}
