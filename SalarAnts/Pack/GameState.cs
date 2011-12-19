using System;
using System.Linq;
using System.Collections.Generic;

namespace SalarAnts.Pack
{
    public class GameState : IGameState
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int LoadTime { get; private set; }
        public int TurnTime { get; private set; }

        private DateTime turnStart;

        public int TimeRemaining
        {
            get
            {
#if DEBUG
                return 1000;
#else
				TimeSpan timeSpent = DateTime.Now - turnStart;
				return TurnTime - timeSpent.Milliseconds;
#endif
            }
        }

        public int FogOfView { get { return 8; } }
        public int ViewRadius2 { get; private set; }
        public int AttackRadius2 { get; private set; }
        public int SpawnRadius2 { get; private set; }
        public long PlayerSeed { get; set; }

        public bool[,] VisibleState;
        public List<Location> Unvisiable { get; private set; }
        public HashSet<Location> VisionOffsets;
        public List<Ant> MyAnts { get; private set; }
        public List<AntHill> MyHills { get; private set; }
        public List<Ant> EnemyAnts { get; private set; }
        public HashSet<AntHill> EnemyHills { get; private set; }
        public List<Location> DeadTiles { get; private set; }
        public List<Location> FoodTiles { get; private set; }

        private Tile[,] map;
        public Tile[,] Map
        {
            get { return map; }
        }

        public Tile this[Location location]
        {
            get { return this.Map[location.RowY, location.ColX]; }
        }

        public Tile this[int row, int col]
        {
            get { return this.Map[row, col]; }
        }


        public GameState(int width, int height,
                         int turntime, int loadtime,
                         int viewradius2, int attackradius2, int spawnradius2)
        {
            Width = width;
            Height = height;

            LoadTime = loadtime;
            TurnTime = turntime;

            ViewRadius2 = viewradius2;
            AttackRadius2 = attackradius2;
            SpawnRadius2 = spawnradius2;

            MyAnts = new List<Ant>();
            MyHills = new List<AntHill>();
            EnemyAnts = new List<Ant>();
            EnemyHills = new HashSet<AntHill>();
            DeadTiles = new List<Location>();
            FoodTiles = new List<Location>();



            map = new Tile[height, width];
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    Map[row, col] = Tile.Land;
                }
            }

            // init
            VisibleState = new bool[height, width];

            // vision!
            VisionOffsets = new HashSet<Location>();
            int mx = (int)Math.Sqrt(ViewRadius2);

            // the offsets
            for (int row = -mx; row <= mx; ++row)
            {
                for (int col = -mx; col <= mx; ++col)
                {
                    int d = row * row + col * col;
                    if (d <= ViewRadius2)
                    {
                        VisionOffsets.Add(new Location(row, col));
                    }
                }
            }

            // possible unvisible places
            Unvisiable = new List<Location>(VisionOffsets.Count);
        }


        public bool IsVisible(Location tile)
        {
            return VisibleState[tile.RowY, tile.ColX];
        }

        /// <summary>
        /// Reset state of unvisible places!
        /// </summary>
        public void ClearVision()
        {
            VisibleState = new bool[Height, Width];
            //for (int row = 0; row < Height; ++row)
            //{
            //    for (int col = 0; col < Width; ++col)
            //    {
            //        Visible[row, col] = false;
            //    }
            //}
        }

        /// <summary>
        /// Calculates visible places
        /// </summary>
        public void SetVision()
        {
            Unvisiable.Clear();
            for (int index = 0; index < MyAnts.Count; index++)
            {
                Ant myAnt = MyAnts[index];
                foreach (Location locOffset in VisionOffsets)
                {
                    int row, col;
                    GetTile(myAnt, locOffset, out row, out col);
                    VisibleState[row, col] = true;
                }
            }

            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    if (VisibleState[row, col] == false)
                        Unvisiable.Add(new Location(row, col));
                }
            }
        }

        void GetTile(Location tile, Location offset, out int rowTile, out int colTile)
        {
            rowTile = (tile.RowY + offset.RowY) % Height;
            if (rowTile < 0)
            {
                rowTile += Height;
            }
            colTile = (tile.ColX + offset.ColX) % Width;
            if (colTile < 0)
            {
                colTile += Width;
            }
        }

        #region State mutators

        public void StartNewTurn()
        {
            // start timer
            turnStart = DateTime.Now;

            // clear ant data
            foreach (Location loc in MyAnts) Map[loc.RowY, loc.ColX] = Tile.Land;
            foreach (Location loc in MyHills) Map[loc.RowY, loc.ColX] = Tile.Land;
            foreach (Location loc in EnemyAnts) Map[loc.RowY, loc.ColX] = Tile.Land;
            //foreach (Location loc in EnemyHills) Map[loc.RowY, loc.ColX] = Tile.Land;
            foreach (Location loc in DeadTiles)
            {
                Map[loc.RowY, loc.ColX] = Tile.Land;
                EnemyHills.RemoveWhere(x => x.EqualTo(loc));
            }

            MyHills.Clear();
            MyAnts.Clear();
            //EnemyHills.Clear();
            EnemyAnts.Clear();
            DeadTiles.Clear();

            // set all known food to unseen
            foreach (Location loc in FoodTiles) Map[loc.RowY, loc.ColX] = Tile.Land;
            FoodTiles.Clear();
        }

        public void RemoveInvalidMyAnt()
        {
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    if (map[row, col] == Tile.AntMine)
                    {
                        if (!MyAnts.Exists(x => x.EqualTo(row, col)))
                        {
                            map[row, col] = Tile.Land;
                        }
                    }

                }
            }

        }

        public void AddAnt(int row, int col, int team)
        {

            Ant ant = new Ant(row, col, team);
            if (team == 0)
            {
                Map[row, col] = Tile.AntMine;
                MyAnts.Add(ant);
            }
            else
            {
                Map[row, col] = Tile.AntEnemy;
                EnemyAnts.Add(ant);
            }
        }

        public void AddFood(int row, int col)
        {
            Map[row, col] = Tile.Food;
            FoodTiles.Add(new Location(row, col));
        }

        public void RemoveFood(int row, int col)
        {
            // an ant could move into a spot where a food just was
            // don't overwrite the space unless it is food
            if (Map[row, col] == Tile.Food)
            {
                Map[row, col] = Tile.Land;
            }
            FoodTiles.Remove(new Location(row, col));
        }

        public void AddWater(int row, int col)
        {
            Map[row, col] = Tile.Water;
        }

        public void DeadAnt(int row, int col)
        {
            // food could spawn on a spot where an ant just died
            // don't overwrite the space unless it is land
            if (Map[row, col] == Tile.Land)
            {
                Map[row, col] = Tile.Dead;
            }

            // but always add to the dead list
            DeadTiles.Add(new Location(row, col));
        }

        public void AntHill(int row, int col, int team)
        {
            AntHill hill = new AntHill(row, col, team);
            if (team == 0)
            {
                if (Map[row, col] == Tile.Land)
                {
                    Map[row, col] = Tile.HillMine;
                }

                MyHills.Add(hill);
            }
            else
            {
                if (Map[row, col] == Tile.Land)
                {
                    Map[row, col] = Tile.HillEnemy;
                }

                // exists?
                if (EnemyHills.FirstOrDefault(x => x.EqualTo(hill)) == null)
                    EnemyHills.Add(hill);
            }
        }

        #endregion

        /// <summary>
        /// Gets whether <paramref name="location"/> is passable or not.
        /// </summary>
        /// <param name="location">The location to check.</param>
        /// <returns><c>true</c> if the location is not water, <c>false</c> otherwise.</returns>
        /// <seealso cref="GetIsUnoccupied"/>
        public bool GetIsPassable(Location location)
        {
            return Map[location.RowY, location.ColX] != Tile.Water;
        }

        /// <summary>
        /// Gets whether <paramref name="location"/> is passable or not.
        /// </summary>
        /// <param name="location">The location to check.</param>
        /// <returns><c>true</c> if the location is not water, <c>false</c> otherwise.</returns>
        /// <seealso cref="GetIsUnoccupied"/>
        public bool GetIsPassable(int y, int x)
        {
            return Map[y, x] != Tile.Water;
        }

        /// <summary>
        /// Gets whether <paramref name="location"/> is occupied or not.
        /// </summary>
        /// <param name="location">The location to check.</param>
        /// <returns><c>true</c> if the location is passable and does not contain an ant, <c>false</c> otherwise.</returns>
        public bool GetIsUnoccupied(Location location)
        {
            var locTile = Map[location.RowY, location.ColX];
            return GetIsPassable(location) && locTile != Tile.AntEnemy && locTile != Tile.AntMine;
        }

        /// <summary>
        /// Gets whether <paramref name="location"/> is occupied or not.
        /// </summary>
        /// <param name="location">The location to check.</param>
        /// <returns><c>true</c> if the location is passable and does not contain an ant, <c>false</c> otherwise.</returns>
        public bool GetIsUnoccupied(int y, int x)
        {
            var locTile = Map[y, x];
            return GetIsPassable(y, x) && locTile != Tile.AntEnemy && locTile != Tile.AntMine;
        }
        /// <summary>
        /// Gets the destination if an ant at <paramref name="location"/> goes in <paramref name="direction"/>, accounting for wrap around.
        /// </summary>
        /// <param name="location">The starting location.</param>
        /// <param name="direction">The direction to move.</param>
        /// <returns>The new location, accounting for wrap around.</returns>
        public Location GetDestination(Location location, Direction direction)
        {
            Location delta = Ants.Aim[direction];

            int row = (location.RowY + delta.RowY) % Height;
            if (row < 0) row += Height; // because the modulo of a negative number is negative

            int col = (location.ColX + delta.ColX) % Width;
            if (col < 0) col += Width;

            return new Location(row, col);
        }

        /// <summary>
        /// Gets the distance between <paramref name="loc1"/> and <paramref name="loc2"/>.
        /// </summary>
        /// <param name="loc1">The first location to measure with.</param>
        /// <param name="loc2">The second location to measure with.</param>
        /// <returns>The distance between <paramref name="loc1"/> and <paramref name="loc2"/></returns>
        public int GetDistance(Location loc1, Location loc2)
        {
            int d_row = Math.Abs(loc1.RowY - loc2.RowY);
            d_row = Math.Min(d_row, Height - d_row);

            int d_col = Math.Abs(loc1.ColX - loc2.ColX);
            d_col = Math.Min(d_col, Width - d_col);

            return d_row + d_col;
        }

        public int GetDistance(int y1, int x1, int y2, int x2)
        {
            int d_row = Math.Abs(y1 - y2);
            d_row = Math.Min(d_row, Height - d_row);

            int d_col = Math.Abs(x1 - x2);
            d_col = Math.Min(d_col, Width - d_col);

            return d_row + d_col;
        }

        /// <summary>
        /// Gets the closest directions to get from <paramref name="loc1"/> to <paramref name="loc2"/>.
        /// </summary>
        /// <param name="loc1">The location to start from.</param>
        /// <param name="loc2">The location to determine directions towards.</param>
        /// <returns>The 1 or 2 closest directions from <paramref name="loc1"/> to <paramref name="loc2"/></returns>
        public ICollection<Direction> GetDirections(Location loc1, Location loc2)
        {
            List<Direction> directions = new List<Direction>();

            if (loc1.RowY < loc2.RowY)
            {
                if (loc2.RowY - loc1.RowY >= Height / 2)
                    directions.Add(Direction.North);
                if (loc2.RowY - loc1.RowY <= Height / 2)
                    directions.Add(Direction.South);
            }
            if (loc2.RowY < loc1.RowY)
            {
                if (loc1.RowY - loc2.RowY >= Height / 2)
                    directions.Add(Direction.South);
                if (loc1.RowY - loc2.RowY <= Height / 2)
                    directions.Add(Direction.North);
            }

            if (loc1.ColX < loc2.ColX)
            {
                if (loc2.ColX - loc1.ColX >= Width / 2)
                    directions.Add(Direction.West);
                if (loc2.ColX - loc1.ColX <= Width / 2)
                    directions.Add(Direction.East);
            }
            if (loc2.ColX < loc1.ColX)
            {
                if (loc1.ColX - loc2.ColX >= Width / 2)
                    directions.Add(Direction.East);
                if (loc1.ColX - loc2.ColX <= Width / 2)
                    directions.Add(Direction.West);
            }

            return directions;
        }

        public bool GetIsVisible(Location loc)
        {
            List<Location> offsets = new List<Location>();
            int squares = (int)Math.Floor(Math.Sqrt(this.ViewRadius2));
            for (int r = -1 * squares; r <= squares; ++r)
            {
                for (int c = -1 * squares; c <= squares; ++c)
                {
                    int square = r * r + c * c;
                    if (square < this.ViewRadius2)
                    {
                        offsets.Add(new Location(r, c));
                    }
                }
            }
            for (int i = 0; i < this.MyAnts.Count; i++)
            {
                var ant = this.MyAnts[i];

                for (int j = 0; j < offsets.Count; j++)
                {
                    var offset = offsets[j];

                    if ((ant.ColX + offset.ColX) == loc.ColX &&
                        (ant.RowY + offset.RowY) == loc.RowY)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public void UpdateMap(int rowY, int colX, Tile newState)
        {
            if (rowY < 0 || colX < 0)
                return;

            if (rowY >= Height || colX >= Width)
                return;

            map[rowY, colX] = newState;
        }
    }
}