namespace SalarAnts.Pack
{
	public abstract class Bot
	{
		public abstract void DoTurn(IGameState state);

		public void IssueOrder(Location loc, Direction direction)
		{
			System.Console.Out.WriteLine("o {0} {1} {2}", loc.RowY, loc.ColX, direction.ToChar());
		}
		public static void IssueOrderStatic(Location loc, Direction direction)
		{
			System.Console.Out.WriteLine("o {0} {1} {2}", loc.RowY, loc.ColX, direction.ToChar());
		}
	}
}