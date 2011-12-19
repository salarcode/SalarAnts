using System;
using System.Collections.Generic;

namespace SalarAnts.Pack
{
	public class LocationEqualityComparer : IEqualityComparer<Location>
	{

		public bool Equals(Location x, Location y)
		{
			return x.EqualTo(y);
		}

		public int GetHashCode(Location obj)
		{
			return obj.GetHashCode();
		}
	}

	public class Location : IEquatable<Location>
	{
		//private int _rowY;

		/// <summary>
		/// Gets the row of this location.
		/// </summary>
		public int RowY;

		//private int _colX;

		/// <summary>
		/// Gets the column of this location.
		/// </summary>
		public int ColX;

		/// <summary>
		/// Weight!
		/// </summary>
		public int ZVal;

		public Location(int rowY, int colx)
		{
			this.RowY = rowY;
			this.ColX = colx;
		}


		/// <summary>
		/// changes location of this!
		/// </summary>
		public void MoveToDirection(Direction direction)
		{
			var newLoc = Ants.Aim[direction];
			this.ColX += newLoc.ColX;
			this.RowY += newLoc.RowY;
		}

		public void Relocate(int rowY, int colx)
		{
			RowY = rowY;
			ColX = colx;
		}

		/// <summary>
		/// changes location of this!
		/// </summary>
		public Location CloneToDirection(Direction direction)
		{
			var newLoc = Ants.Aim[direction];
			return new Location(RowY + newLoc.RowY, ColX + newLoc.ColX);
		}

		public override string ToString()
		{
			return string.Format("ColX={0}, RowY={1}, Hash={2}", ColX, RowY, this.HashCode());
		}
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != typeof(Location))
				return false;

			return Equals((Location)obj);
		}

		public bool EqualTo(Location other)
		{
			return other.RowY == this.RowY && other.ColX == this.ColX;
		}

		public bool EqualTo(int rowY, int colX)
		{
			return rowY == this.RowY && colX == this. ColX;
		}

		public bool Equals(Location other)
		{
			//if (ReferenceEquals(null, other))
			//    return false;
			//if (ReferenceEquals(this, other))
			//    return true;

			return other.RowY == this.RowY && other.ColX == this.ColX;
		}

		public override int GetHashCode()
		{
			return HashCode();
		}

		/// <summary>
		/// Preventing from using Object.GetHashCode() method!!
		/// </summary>
		public int HashCode()
		{
			unchecked
			{
				return (this.RowY * 397) ^ this.ColX;
			}
		}
	}

	public class TeamLocation : Location, IEquatable<TeamLocation>
	{
		/// <summary>
		/// Gets the team of this ant.
		/// </summary>
		public int Team { get; private set; }

		public TeamLocation(int row, int col, int team)
			: base(row, col)
		{
			this.Team = team;
		}

		public bool Equals(TeamLocation other)
		{
			return base.Equals(other) && other.Team == Team;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = this.ColX;
				result = (result * 397) ^ this.RowY;
				result = (result * 397) ^ this.Team;
				return result;
			}
		}
	}

	public class Ant : TeamLocation, IEquatable<Ant>
	{
		public Ant(int row, int col, int team)
			: base(row, col, team)
		{
		}

		public bool Equals(Ant other)
		{
			return base.Equals(other);
		}
	}

	public class AntHill : TeamLocation, IEquatable<AntHill>
	{
		public AntHill(int row, int col, int team)
			: base(row, col, team)
		{
		}

		public bool Equals(AntHill other)
		{
			return base.Equals(other);
		}
	}
}