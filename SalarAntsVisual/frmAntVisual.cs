using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SalarAntsVisual
{
	//public enum VisualAntTile { AntMine, AntEnemy, Dead, Land, Food, Water, Unseen, Hill }
	//public struct VisualAntMapLocation
	//{
	//    public int ColX;
	//    public int RowY;
	//    public VisualAntMapLocation(int colX, int rowY)
	//    {
	//        ColX = colX;
	//        RowY = rowY;
	//    }
	//}

	public partial class frmAntVisual : Form
	{
		public frmAntVisual()
		{
			InitializeComponent();
			BitmapChanged += frmAntVisual_BitmapChanged;
		}

		public static void AntRunApplication(frmAntVisual frm)
		{
			Program.RunTheForm(frm);
		}

		public delegate void BitmapChangedHandler(Bitmap newBmp);

		public event BitmapChangedHandler BitmapChanged;

		public void RaiseBitmapChanged(Bitmap newBmp)
		{
			if (BitmapChanged != null)
				BitmapChanged(newBmp);
		}

		void frmAntVisual_BitmapChanged(Bitmap newBmp)
		{
			if (this.InvokeRequired)
			{
				BeginInvoke(new BitmapChangedHandler(ChangeMap), new object[] {newBmp});
			}
			else
			{
				ChangeMap(newBmp);
			}
		}

		void ChangeMap(Bitmap bmp)
		{
			picMap.Image = bmp;
		}


 		private void btnExit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		#region OLD
		//public static volatile Bitmap AntMapVisual;
		//private static volatile Graphics antMapGraphics;

		//private const int largeFacor = 10;
		//private static int initWidth = 2;
		//private static int initHeight = 2;

		//public static void AntInitVisual(int width, int height)
		//{
		//    initWidth = width;
		//    initHeight = height;
		//    AntMapVisual = new Bitmap(width * largeFacor, height * largeFacor);
		//    antMapGraphics = Graphics.FromImage(AntMapVisual);
		//    var f = new Font("Tahoma", 5);
		//    for (int i = 0; i < width; i++)
		//    {
		//        antMapGraphics.DrawString((i).ToString(), f, Brushes.Green, i * largeFacor, 1);
		//    }
		//    for (int i = 0; i < height; i++)
		//    {
		//        antMapGraphics.DrawString((i).ToString(), f, Brushes.Green, 1, i * largeFacor);
		//    }
		//}

		//public static void AntSetAnts(List<VisualAntMapLocation> myAnts, List<VisualAntMapLocation> enemyAnts)
		//{
		//    Brush myAntColor = Brushes.Black;
		//    Brush enemyAntColor = Brushes.Red;

		//    foreach (VisualAntMapLocation ant in enemyAnts)
		//    {
		//        antMapGraphics.FillRectangle(enemyAntColor, ant.ColX * largeFacor, ant.RowY * largeFacor, largeFacor, largeFacor);
		//        //AntMapVisual.SetPixel(ant.ColX * largeFacor, ant.RowY * largeFacor, enemyAntColor);
		//    }
		//    foreach (VisualAntMapLocation myAnt in myAnts)
		//    {
		//        antMapGraphics.FillRectangle(myAntColor, myAnt.ColX * largeFacor, myAnt.RowY * largeFacor, largeFacor, largeFacor);
		//        //AntMapVisual.SetPixel(myAnt.ColX * largeFacor, myAnt.RowY * largeFacor, myAntColor);
		//    }
		//}

		//public static void AntIssueOrderToLoc(VisualAntMapLocation visualAnt, VisualAntMapLocation location)
		//{
		//    Brush enemyAntColor = Brushes.Cyan;

		//    antMapGraphics.FillPie(enemyAntColor, location.ColX * largeFacor, location.RowY * largeFacor, largeFacor, largeFacor, 0,
		//                           360);

		//}

		//public static void AntMarkLocation(VisualAntMapLocation loc, int val)
		//{
		//    Brush enemyAntColor = Brushes.LawnGreen;
		//    switch (val)
		//    {
		//        case 1: enemyAntColor = Brushes.HotPink;
		//            break;
		//        case 2: enemyAntColor = Brushes.Yellow;
		//            break;
		//        case 3: enemyAntColor = Brushes.Aqua;
		//            break;
		//        case 4: enemyAntColor = Brushes.DarkRed;
		//            break;
		//        case 5: enemyAntColor = Brushes.Red;
		//            break;
		//    }

		//    antMapGraphics.FillPie(enemyAntColor, (loc.ColX * largeFacor) + val, (loc.RowY * largeFacor) + val, (largeFacor / 2), (largeFacor / 2), 0,
		//                           360);
		//}


		//public static void AntInitMap(VisualAntTile[,] tiles)
		//{
		//    for (int row = 0; row < initHeight; row++)
		//    {
		//        for (int col = 0; col < initWidth; col++)
		//        {
		//            DrawTile(tiles[row, col], row, col);
		//            //AntMapVisual.SetPixel(col * largeFacor, row * largeFacor, c);
		//        }
		//    }
		//}

		//public static void DrawTile(VisualAntTile t, int row, int col)
		//{
		//    //var t = tiles[row, col];
		//    Color c = Color.Silver;
		//    Brush b = Brushes.Silver;
		//    switch (t)
		//    {
		//        case VisualAntTile.AntMine:
		//            c = Color.Black;
		//            b = Brushes.Black;
		//            break;
		//        case VisualAntTile.AntEnemy:
		//            c = Color.Indigo;
		//            b = Brushes.Indigo;
		//            break;
		//        case VisualAntTile.Dead:
		//            c = Color.Brown;
		//            b = Brushes.Brown;
		//            break;
		//        case VisualAntTile.Land:
		//            c = Color.White;
		//            b = Brushes.White;
		//            break;
		//        case VisualAntTile.Food:
		//            c = Color.Green;
		//            b = Brushes.Green;
		//            break;
		//        case VisualAntTile.Water:
		//            c = Color.Blue;
		//            b = Brushes.Blue;
		//            break;
		//        case VisualAntTile.Unseen:
		//            c = Color.BurlyWood;
		//            b = Brushes.BurlyWood;
		//            break;
		//    }

		//    Pen p = new Pen(Color.Silver, 1);

		//    if (t != VisualAntTile.Land)
		//        antMapGraphics.FillRectangle(b, col * largeFacor, row * largeFacor, largeFacor, largeFacor);
		//    antMapGraphics.DrawRectangle(p, col * largeFacor, row * largeFacor, largeFacor, largeFacor);
		//}


		#endregion
	}
}
