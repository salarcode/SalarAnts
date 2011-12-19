using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SalarAnts.Defination;
using SalarAnts.Pack;

namespace SalarAnts.SalarBot
{
	public class PlanManagerV1
	{
		public enum PlanType
		{
			None,
			Attack,
			Food,
			Hill,
			NewBorn,
			Move,
			Explore
		}

		public IGameState GameState { get; set; }

		public List<AttackPlanDetail> AttackPlans { get; private set; }
		public List<SearchPlanDetail> FoodPlans { get; private set; }
		public List<MovePlanDetail> MovePlans { get; private set; }
		public List<SearchPlanDetail> NewBornPlans { get; private set; }
		public List<Location> MyFreeAnts { get; set; }
		public List<SearchPlanDetail> ExplorePlans { get; private set; }

		public PlanManagerV1()
		{
			//GameState = gameState;
			FoodPlans = new List<SearchPlanDetail>();
			MovePlans = new List<MovePlanDetail>();
			AttackPlans = new List<AttackPlanDetail>();
			NewBornPlans = new List<SearchPlanDetail>();
			MyFreeAnts = new List<Location>();
			ExplorePlans = new List<SearchPlanDetail>();
		}


		public void RemoveInvalidPlans()
		{
			MovePlans.Clear();
			MyFreeAnts.Clear();
			//NewBornPlans.Clear();

			for (int i = ExplorePlans.Count - 1; i >= 0; i--)
			{
				var explorer = ExplorePlans[i];

				if (explorer.PathCurrent != null)
					if (explorer.PathCurrent.Value.X != explorer.Ant.ColX || explorer.PathCurrent.Value.Y != explorer.Ant.RowY)
					{
						ExplorePlans.RemoveAt(i);
						continue;
					}

				// not available deleted
				if (!GameState.MyAnts.Exists(x => x.EqualTo(explorer.Ant)))
				{
					ExplorePlans.RemoveAt(i);
					continue;
				}

			}

			for (int i = NewBornPlans.Count - 1; i >= 0; i--)
			{
				var newBorn = NewBornPlans[i];

				if (newBorn.PathCurrent != null)
					if (newBorn.PathCurrent.Value.X != newBorn.Ant.ColX || newBorn.PathCurrent.Value.Y != newBorn.Ant.RowY)
					{
						NewBornPlans.RemoveAt(i);
						continue;
					}
			}

			for (int i = FoodPlans.Count - 1; i >= 0; i--)
			{
				var food = FoodPlans[i];

				if (food.PathCurrent != null)
					if (food.PathCurrent.Value.X != food.Ant.ColX || food.PathCurrent.Value.Y != food.Ant.RowY)
					{
						FoodPlans.RemoveAt(i);
						continue;
					}

				// not available deleted
				if (!GameState.MyAnts.Exists(x => x.EqualTo(food.Ant)))
				{
					FoodPlans.RemoveAt(i);
					continue;
				}

				try
				{
					if (GameState[food.Ant] != Tile.AntMine)
					{
						FoodPlans.RemoveAt(i);
						continue;
					}
				}
				catch (Exception)
				{
					FoodPlans.RemoveAt(i);
					continue;
				}

				try
				{
					var dest = GameState[food.GetFinalMoveLoc()];
					if (dest != Tile.Food && dest != Tile.HillEnemy)
					{
						FoodPlans.RemoveAt(i);
						continue;
					}
				}
				catch (Exception)
				{
					FoodPlans.RemoveAt(i);
					continue;
				}
			}

			foreach (var deadTile in GameState.DeadTiles)
			{
				var antIndex = FoodPlans.FirstIndexOf(x => x.Ant.EqualTo(deadTile));
				if (antIndex >= 0)
					FoodPlans.RemoveAt(antIndex);
				AttackPlans.RemoveFirst(x => x.Ant.EqualTo(deadTile));
				AttackPlans.RemoveFirst(x => x.Enemy.EqualTo(deadTile));
			}
		}


		/// <summary>
		/// Index of ant in food plan
		/// </summary>
		public int GetFoodPlanIndexAnt(Location ant)
		{
			return AttackPlans.FirstIndexOf(x => x.Ant.EqualTo(ant));
		}

		public void AddFreeAntChecked(Location ant)
		{
			// check if it has already a plan
			if (!HasPlanAnt(ant,
						 PlanType.Move,
						 PlanType.Attack,
						 PlanType.Food,
						 PlanType.Hill,
						 PlanType.Explore))
			{
				MyFreeAnts.Add(ant);
			}
		}

		public void AddFreeAntChecked(Location ant, params PlanType[] plans)
		{
			// check if it has already a plan
			if (!HasPlanAnt(ant, plans))
			{
				MyFreeAnts.Add(ant);
			}
		}

		public bool HasPlanDest(Location dest, params PlanType[] plans)
		{
			foreach (var plan in plans)
			{
				switch (plan)
				{
					case PlanType.Move:
						if (MovePlans.Exists(x => x.Dest.EqualTo(dest)))
							return true;
						break;

					case PlanType.Attack:
						if (AttackPlans.Exists(x => x.NextStep.EqualTo(dest)))
							return true;
						break;

					case PlanType.Food:
						if (FoodPlans.Exists(x => x.GetFinalMoveLoc().EqualTo(dest)))
							return true;
						break;

					case PlanType.Hill:
						if (FoodPlans.Exists(x => x.GetFinalMoveLoc().EqualTo(dest)))
							return true;
						break;

					case PlanType.NewBorn:
						if (NewBornPlans.Exists(x => x.GetFinalMoveLoc().EqualTo(dest)))
							return true;
						break;
					case PlanType.Explore:
						if (ExplorePlans.Exists(x => x.GetFinalMoveLoc().EqualTo(dest)))
							return true;
						break;
				}
			}
			return false;
		}

		public bool HasPlanAnt(Location ant, params PlanType[] plans)
		{
			foreach (var plan in plans)
			{
				switch (plan)
				{
					case PlanType.Move:
						if (MovePlans.Exists(x => x.Ant.EqualTo(ant)))
							return true;
						break;

					case PlanType.Attack:
						if (AttackPlans.Exists(x => x.Ant.EqualTo(ant)))
							return true;
						break;

					case PlanType.Food:
						if (FoodPlans.Exists(x => x.Ant.EqualTo(ant)))
							return true;
						break;

					case PlanType.Hill:
						if (FoodPlans.Exists(x => x.Ant.EqualTo(ant)))
							return true;
						break;

					case PlanType.NewBorn:
						if (NewBornPlans.Exists(x => x.Ant.EqualTo(ant)))
							return true;
						break;

					case PlanType.Explore:
						if (ExplorePlans.Exists(x => x.Ant.EqualTo(ant)))
							return true;
						break;
				}
			}
			return false;
		}

		public bool HasPlanAnt(Location ant)
		{
			bool food = FoodPlans.Exists(x => x.Ant.EqualTo(ant));
			if (food) return true;

			bool move = MovePlans.Exists(x => x.Ant.EqualTo(ant));
			if (move) return true;

			bool attack = AttackPlans.Exists(x => x.Ant.EqualTo(ant));
			if (attack) return true;

			var born = NewBornPlans.Exists(x => x.Ant.EqualTo(ant));
			if (born) return true;

			var exp = ExplorePlans.Exists(x => x.Ant.EqualTo(ant));
			if (exp) return true;

			// has plan
			return false;
		}

		//public void ClearPlans()
		//{
		//    AttackPlans.Clear();
		//    FoodPlans.Clear();
		//    MovePlans.Clear();
		//    //NewBornPlans.Clear();
		//    MyFreeAnts.Clear();
		//}

		public void CancelOtherPlansForAnt(Location ant)
		{
			FoodPlans.RemoveAll(x => x.Ant.EqualTo(ant));
			AttackPlans.RemoveAll(x => x.Ant.EqualTo(ant));
			MovePlans.RemoveAll(x => x.Ant.EqualTo(ant));
			NewBornPlans.RemoveAll(x => x.Ant.EqualTo(ant));
			ExplorePlans.RemoveAll(x => x.Ant.EqualTo(ant));
		}

		public void CancelPlans(Location ant, params PlanType[] plans)
		{
			foreach (var plan in plans)
			{
				switch (plan)
				{
					case PlanType.Move:
						MovePlans.RemoveAll(x => x.Ant.EqualTo(ant));
						break;

					case PlanType.Attack:
						AttackPlans.RemoveAll(x => x.Ant.EqualTo(ant));
						break;

					case PlanType.Food:
						FoodPlans.RemoveAll(x => x.Ant.EqualTo(ant));
						break;

					case PlanType.Hill:
						FoodPlans.RemoveAll(x => x.Ant.EqualTo(ant));
						break;

					case PlanType.NewBorn:
						NewBornPlans.RemoveAll(x => x.Ant.EqualTo(ant));
						break;
					case PlanType.Explore:
						ExplorePlans.RemoveAll(x => x.Ant.EqualTo(ant));
						break;
				}
			}
		}
	}
}
