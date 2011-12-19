using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SalarAnts;
using SalarAnts.Pack;

#if TestInput
using SalarAnts.SalarBot;
#endif

namespace SalarAnts
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			new Ants().PlayGame(new MyBot());
			//Test();

		}

#if TestInput


		//static void Test()
		//{
		//    SalarAntsVisualRunner.VisualStart();
		//    frmAntVisual.AntInitVisual(35, 35);

		//    var tiles = new AntTile[35, 35];
		//    for (int row = 0; row < 35; row++)
		//    {
		//        for (int col = 0; col < 35; col++)
		//        {
		//            tiles[row, col] = AntTile.Land;
		//        }
		//    }
		//    frmAntVisual.AntInitMap(tiles);

		//    int attackRaduis = 5;


		//    var thisAnt = new Location(26, 27);
		//    var myAnts = new List<Location>()
		//                    {
		//                        //new Location(25,27),
								
		//                        new Location(25,30),
		//                        new Location(23,26),
		//                        new Location(25,24),
		//                        new Location(30,25),
		//                        new Location(30,29),
								
		//                        //new Location(30,30),
		//                        //new Location(28,27),
		//                        //new Location(29,27),
		//                    };

		//    var enemyAnts = new List<Location>()
		//                    {
		//                        //new Location(25,30),
		//                        //new Location(23,26),
		//                        new Location(25,24),
		//                        new Location(30,25),
		//                        //new Location(30,29),

		//                        //new Location(26,27),
		//                        //new Location(26,27),
		//                        //new Location(27,31),
		//                    };

		//    foreach (Location myAnt in myAnts)
		//    {
		//        frmAntVisual.DrawTile(AntTile.AntMine, myAnt.RowY, myAnt.ColX);
		//    }
		//    foreach (Location a in enemyAnts)
		//    {
		//        frmAntVisual.DrawTile(AntTile.AntEnemy, a.RowY, a.ColX);
		//    }
		//    frmAntVisual.DrawTile(AntTile.Water, thisAnt.RowY, thisAnt.ColX);

		//    var found = SalarBotV1.AttackOrDefence(thisAnt, attackRaduis, myAnts, enemyAnts);
		//    frmAntVisual.AntMarkLocation(new AntMapLocation(found.ColX, found.RowY), 1);

		//}
#endif

	}
}