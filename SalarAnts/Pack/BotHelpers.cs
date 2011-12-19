 
namespace SalarAnts.Pack
{
	/// <summary>
	/// Documents: https://github.com/j-h-a/aichallenge/blob/vis_overlay/VIS_OVERLAY.md
	/// Visualizer supports overlay: https://github.com/j-h-a/aichallenge/tree/vis_overlay/ants
	/// </summary>
	public static class BotHelpers
	{
		public enum Subtile
		{
			TL, TM, TR, ML, MM, MR, BL, BM, BR
		}

		/// <summary>
		/// Imagine each tile (map-square) is divided into nine sub-tiles like a naughts-and-crosses board,
		/// the subtile parameter is a combination of Top-Middle-Bottom and Left-Middle-Right to define
		/// which of the nine sub-tiles you want to draw. It should be one of: TL, TM, TR, ML, MM, MR, BL, BM, BR.
		/// </summary>
		public static void OverlayTileBorder(float x, float y, Subtile subtile)
		{
#if DEBUG
			System.Console.Out.WriteLine("v tileBorder " + y + " " + x + " " + subtile);
#endif
		}

		/// <summary>
		/// tileSubTile fills the specified sub-tile while tileBorder draws a line around the edge of the tile at the specified sub-tile location, or around the whole tile if subtile is MM.
		/// </summary>
		public static void OverlayTileSubTile(float x, float y, Subtile subtile)
		{
#if DEBUG
			System.Console.Out.WriteLine("v tileSubTile " + y + " " + x + " " + subtile);
#endif
		}

		/// <summary>
		/// setLineWidth sets the with for all line-drawing commands. The line width w is in screen-units (not map-units).
		/// </summary>
		/// <param name="w"></param>
		public static void OverlaySetLineWidth(int w)
		{
#if DEBUG
			System.Console.Out.WriteLine("v setLineWidth " + w + "");
#endif
		}
#if DEBUG
		/// <summary>
		/// setLineColour and setFillColour set the colour values for all line-drawing and filled area drawing commands.
		/// Colours are specified as integers for r, g, and b (from 0-255) and the alpha value a is a float (from 0.0-1.0).
		/// </summary>
		public static void OverlaySetLineColor(System.Drawing.Color c)
		{
			OverlaySetLineColor(c.R, c.G, c.B, c.A);
		}
#endif

		/// <summary>
		/// setLineColour and setFillColour set the colour values for all line-drawing and filled area drawing commands.
		/// Colours are specified as integers for r, g, and b (from 0-255) and the alpha value a is a float (from 0.0-1.0).
		/// </summary>
		public static void OverlaySetLineColor(int r, int g, int b, float a)
		{
#if DEBUG
			System.Console.Out.WriteLine("v setLineColor " + r + " " + g + " " + b + " " + a + "");
#endif
		}

#if DEBUG
		/// <summary>
		/// setLineColour and setFillColour set the colour values for all line-drawing and filled area drawing commands.
		/// Colours are specified as integers for r, g, and b (from 0-255) and the alpha value a is a float (from 0.0-1.0).
		/// </summary>
		public static void OverlaySetFillColor(System.Drawing.Color c)
		{
			OverlaySetFillColor(c.R, c.G, c.B, c.A);
		}
#endif
		/// <summary>
		/// setLineColour and setFillColour set the colour values for all line-drawing and filled area drawing commands.
		/// Colours are specified as integers for r, g, and b (from 0-255) and the alpha value a is a float (from 0.0-1.0).
		/// </summary>
		public static void OverlaySetFillColor(int r, int g, int b, float a)
		{
#if DEBUG
			System.Console.Out.WriteLine("v setFillColor " + r + " " + g + " " + b + " " + a + "");
#endif
		}

		public static void OverlayCircle(float x, float y, float radius, bool fill)
		{
#if DEBUG
			System.Console.Out.WriteLine("v circle " + y + " " + x + " " + radius + " " + fill + "");
#endif
		}

		/// <summary>
		/// The i command adds map-information to a specific tile on the current turn.
		/// In the visualizer move your mouse over the tile to see the string you specified.
		/// If you specify this command more than once for the same row and column on any turn,
		/// the additional strings will be appended on a new line.
		/// </summary>
		public static void OverlayText(float x, float y, string text)
		{
#if DEBUG
			System.Console.Out.WriteLine("i " + y + " " + x + " " + text);
#endif
		}

		/// <summary>
		/// The plan-string for the routePlan command is a case-insensitive sequence of direction characters, for example NNEENNWWWWSS.
		/// It draws a line using the current line width and colour from the starting row and column along the planned route.
		/// </summary>
		public static void OverlayRoutePlan(float x, float y, string planString)
		{
#if DEBUG
			System.Console.Out.WriteLine("v routePlan " + y + " " + x + " " + planString);
#endif
		}

		public static void OverlayRect(float x, float y, float w, float h, bool fill)
		{
#if DEBUG
			System.Console.Out.WriteLine("v rect " + y + " " + x + " " + w + " " + h + " " + fill + "");
#endif
		}

		/// <summary>
		/// line and arrow will draw the shortest path between the specified points, taking map-wrapping at the edges into account.
		/// </summary>
		public static void OverlayLine(float x1, float y1, float x2, float y2)
		{
#if DEBUG
			System.Console.Out.WriteLine("v line " + y1 + " " + x1 + " " + y2 + " " + x2 + "");
#endif
		}

		/// <summary>
		/// line and arrow will draw the shortest path between the specified points, taking map-wrapping at the edges into account.
		/// </summary>
		public static void OverlayArrow(float x1, float y1, float x2, float y2)
		{
#if DEBUG
			System.Console.Out.WriteLine("v arrow " + y1 + "," + x1 + "," + y2 + "," + x2 + "");
#endif
		}

		/// <summary>
		/// star draws a star centered at (row, col) with points points. inner_radius and outer_radius control the size of the star and points.
		/// </summary>
		public static void OverlayStar(float x1, float y1, float innerRadius, float outerRadius, float points, bool fill)
		{
#if DEBUG
			System.Console.Out.WriteLine("v star {0} {1} {2} {3} {4} {5}", y1, x1, innerRadius, outerRadius, points, fill);
#endif
		}


	}
}
