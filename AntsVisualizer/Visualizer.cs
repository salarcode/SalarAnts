using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace AntsVisualizer
{
	public class Visualizer
	{
		public volatile Bitmap AntMapVisual;
		private volatile Graphics _antMapGraphics;
		private const int LargeFacor = 10;
		private int _initWidth = 2;
		private int _initHeight = 2;
		public Brush MyAntColor = Brushes.Black;
		public Brush EnemyAntColor = Brushes.Red;
		public Brush IssuedOrderMarkColor = Brushes.Cyan;

		public event EventHandler MapUpdated;

		public bool IsInitializing = false;
		private bool _pauseEvent = false;

		private SalarAntsVisual.frmAntVisual _antVisualForm;

		public void StartVisualizerForm()
		{
			Thread appThread = new Thread(() =>
											{
												_antVisualForm = new SalarAntsVisual.frmAntVisual();
												SalarAntsVisual.frmAntVisual.AntRunApplication(_antVisualForm);
											});
			appThread.Start();
		}

		public void RaiseMapUpdated()
		{
			if (!IsInitializing && !_pauseEvent)
			{
				if (MapUpdated != null)
				{
					MapUpdated(this, EventArgs.Empty);
				}
				if (_antVisualForm != null)
				{
					_antVisualForm.RaiseBitmapChanged(AntMapVisual);
				}
			}
		}

		public void InitVisualMap(int width, int height)
		{
			_initWidth = width;
			_initHeight = height;
			if (AntMapVisual != null)
			{
				AntMapVisual.Dispose();
				AntMapVisual = null;
			}
			if (_antMapGraphics != null)
			{
				_antMapGraphics.Dispose();
				_antMapGraphics = null;
			}

			AntMapVisual = new Bitmap(width * LargeFacor, height * LargeFacor);
			_antMapGraphics = Graphics.FromImage(AntMapVisual);
			var f = new Font("Tahoma", 5);
			for (int i = 0; i < width; i++)
			{
				_antMapGraphics.DrawString(i.ToString(), f, Brushes.Green, i * LargeFacor, 1);
			}
			for (int i = 0; i < height; i++)
			{
				_antMapGraphics.DrawString(i.ToString(), f, Brushes.Green, 1, i * LargeFacor);
			}
			RaiseMapUpdated();
		}


		public void MarkIssuedOrder(AntMapLocation location)
		{
			_antMapGraphics.FillPie(IssuedOrderMarkColor,
								   location.ColX * LargeFacor,
								   location.RowY * LargeFacor,
								   LargeFacor,
								   LargeFacor, 0,
								   360);
			RaiseMapUpdated();
		}

		public void MarkIssuedOrder(IEnumerable<AntMapLocation> locations)
		{
			_pauseEvent = true;
			try
			{
				foreach (var location in locations)
				{
					MarkIssuedOrder(location);
				}
			}
			finally
			{
				_pauseEvent = false;
			}
			RaiseMapUpdated();
		}

		public void MarkLocation(int rowY, int colX, Brush color, int val = 2)
		{
			_antMapGraphics.FillPie(color,
									(colX * LargeFacor) + val, (rowY * LargeFacor) + val,
									(LargeFacor / 2), (LargeFacor / 2), 0,
									360);
			RaiseMapUpdated();
		}

		public void MarkLocation(int rowY, int colX, int val)
		{
			Brush enemyAntColor = Brushes.LawnGreen;
			switch (val)
			{
				case 1: enemyAntColor = Brushes.HotPink;
					break;
				case 2: enemyAntColor = Brushes.Yellow;
					break;
				case 3: enemyAntColor = Brushes.Aqua;
					break;
				case 4: enemyAntColor = Brushes.DarkRed;
					break;
				case 5: enemyAntColor = Brushes.Red;
					break;
			}

			_antMapGraphics.FillPie(enemyAntColor,
									(colX * LargeFacor) + val, (rowY * LargeFacor) + val,
									(LargeFacor / 2), (LargeFacor / 2), 0,
									360);
			RaiseMapUpdated();
		}

		public void DrawMapTiles(AntTile[,] tiles)
		{
			_pauseEvent = true;
			try
			{
				for (int row = 0; row < _initHeight; row++)
				{
					for (int col = 0; col < _initWidth; col++)
					{
						DrawTile(tiles[row, col], row, col);
					}
				}
			}
			finally
			{
				_pauseEvent = false;
			}
			RaiseMapUpdated();
		}

		public void DrawMyAnt(int rowY, int colX)
		{
			_antMapGraphics.FillRectangle(MyAntColor, colX * LargeFacor, rowY * LargeFacor, LargeFacor, LargeFacor);
			RaiseMapUpdated();
		}

		public void DrawEnemyAnt(int rowY, int colX)
		{
			_antMapGraphics.FillRectangle(EnemyAntColor, colX * LargeFacor, rowY * LargeFacor, LargeFacor, LargeFacor);
			RaiseMapUpdated();
		}

		public void DrawAnts(IList<AntMapLocation> myAnts, IList<AntMapLocation> enemyAnts)
		{
			_pauseEvent = true;
			try
			{
				foreach (AntMapLocation ant in enemyAnts)
				{
					DrawEnemyAnt(ant.RowY, ant.ColX);
				}
				foreach (AntMapLocation myAnt in myAnts)
				{
					DrawMyAnt(myAnt.RowY, myAnt.ColX);
				}
			}
			finally
			{
				_pauseEvent = false;
			}
			RaiseMapUpdated();

		}

		public void DrawTile(AntTile t, int rowY, int colX)
		{
			//var t = tiles[rowY, colX];
			//Color c = Color.Silver;
			Brush b = Brushes.Silver;
			switch (t)
			{
				case AntTile.AntMine:
					b = MyAntColor;
					break;
				case AntTile.AntEnemy:
					b = EnemyAntColor;
					break;
				case AntTile.Dead:
					b = Brushes.Brown;
					break;
				case AntTile.Land:
					b = Brushes.White;
					break;
				case AntTile.Food:
					b = Brushes.Green;
					break;
				case AntTile.Water:
					b = Brushes.Blue;
					break;
				case AntTile.Unseen:
					b = Brushes.BurlyWood;
					break;
			}

			// draw the tile
			if (t != AntTile.Land)
				_antMapGraphics.FillRectangle(b, colX * LargeFacor, rowY * LargeFacor, LargeFacor, LargeFacor);

			// draw the box
			var p = new Pen(Color.Silver, 1);
			_antMapGraphics.DrawRectangle(p, colX * LargeFacor, rowY * LargeFacor, LargeFacor, LargeFacor);

			RaiseMapUpdated();
		}


	}
}
