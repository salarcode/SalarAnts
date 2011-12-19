using SalarAnts.Classes;
using SalarAnts.Pack;
using SalarAnts.SalarBot;

namespace SalarAnts
{
	internal class MyBot : Bot
	{
		private SalarBotV2 _salarBot;

		public MyBot()
			: base()
		{
			_salarBot = new SalarBotV2(null, this);
		}

		// DoTurn is run once per turn
		public override void DoTurn(IGameState state)
		{
			_salarBot.State = state;
			_salarBot.EnsureAStar();

			// V1 turn!
			_salarBot.DoTurn();

		}

		private void DefaultTurn(IGameState state)
		{
			// loop through all my ants and try to give them orders
			foreach (Ant ant in state.MyAnts)
			{
				// try all the directions
				foreach (Direction direction in Ants.Aim.Keys)
				{
					// GetDestination will wrap around the map properly
					// and give us a new location
					Location newLoc = state.GetDestination(ant, direction);

					// GetIsPassable returns true if the location is land
					if (state.GetIsPassable(newLoc))
					{
						IssueOrder(ant, direction);
						// stop now, don't give 1 and multiple orders
						break;
					}
				}

				// check if we have time left to calculate more orders
				if (state.TimeRemaining < 10) break;
			}
		}


		//public static void Main (string[] args) {
		//    new Ants().PlayGame(new MyBot());
		//}
	}
}