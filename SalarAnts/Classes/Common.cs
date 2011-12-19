using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SalarAnts.Pack;

namespace System
{
	public static class Common
	{
		public static IList<Location> CircleMidPoint(int xcenter, int ycenter, int radius)
		{
			List<Location> result = new List<Location>();
			int x = 0;
			int y = radius;
			int p = 1 - radius;

			//plot first set of points/*
			CirclePlotPoints(result, xcenter, ycenter, x, y);
			while (x < y)
			{
				x++;
				if (p < 0)
					p += 2 * x + 1;
				else
				{
					y--;
					p += 2 * (x - y) + 1;
				}
				CirclePlotPoints(result, xcenter, ycenter, x, y);
			}
			return result;
		}

		static void CirclePlotPoints(List<Location> result, int xcenter, int ycenter, int x, int y)
		{
			//result.Add(new Location {ColX =, RowY =});
			int nY, nX;

			nY = ycenter + y; nX = xcenter + x;
			if (!result.Exists(t => t.EqualTo(nY, nX)))
				result.Add(new Location(nY, nX));

			nY = ycenter + y; nX = xcenter - x;
			if (!result.Exists(t => t.EqualTo(nY, nX)))
				result.Add(new Location(nY, nX));

			nY = ycenter - y; nX = xcenter + x;
			if (!result.Exists(t => t.EqualTo(nY, nX)))
				result.Add(new Location(nY, nX));

			nY = ycenter - y; nX = xcenter - x;
			if (!result.Exists(t => t.EqualTo(nY, nX)))
				result.Add(new Location(nY, nX));

			nY = ycenter + x; nX = xcenter + y;
			if (!result.Exists(t => t.EqualTo(nY, nX)))
				result.Add(new Location(nY, nX));

			nY = ycenter + x; nX = xcenter - y;
			if (!result.Exists(t => t.EqualTo(nY, nX)))
				result.Add(new Location(nY, nX));

			nY = ycenter - x; nX = xcenter + y;
			if (!result.Exists(t => t.EqualTo(nY, nX)))
				result.Add(new Location(nY, nX));

			nY = ycenter - x; nX = xcenter - y;
			if (!result.Exists(t => t.EqualTo(nY, nX)))
				result.Add(new Location(nY, nX));

			//Setpixel( xcenter + x , ycenter + y);
			//Setpixel( xcenter - x , ycenter + y);
			//Setpixel( xcenter + x , ycenter – y);
			//Setpixel( xcenter - x , ycenter – y);
			//Setpixel( xcenter + y, ycenter + x);
			//Setpixel( xcenter - y , ycenter + x);
			//Setpixel( xcenter + y , ycenter – x);
			//Setpixel( xcenter – y , ycenter – x);
		}


		public static void GetLocationNeighborsMirrored(IGameState state, Location[] inNeighbors, Location srcLoc)
		{
			inNeighbors[0] = Common.CreateMirrorableLocation(state, srcLoc.RowY, srcLoc.ColX - 1);
			inNeighbors[1] = Common.CreateMirrorableLocation(state, srcLoc.RowY, srcLoc.ColX + 1);
			inNeighbors[2] = Common.CreateMirrorableLocation(state, srcLoc.RowY + 1, srcLoc.ColX);
			inNeighbors[3] = Common.CreateMirrorableLocation(state, srcLoc.RowY - 1, srcLoc.ColX);
		}

		public static Location CreateMirrorableLocation(IGameState state, int rowY, int colX)
		{
			if (colX > state.Width)
			{
				colX = colX - state.Width;
			}
			else if (colX < 0)
			{
				colX = state.Width - Math.Abs(colX);
			}
			if (rowY > state.Height)
			{
				rowY = rowY - state.Height;
			}
			else if (rowY < 0)
			{
				rowY = state.Height - Math.Abs(rowY);
			}
			return new Location(rowY, colX);
		}
	}
}
