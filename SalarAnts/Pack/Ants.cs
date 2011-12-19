using System;
using System.IO;
using System.Collections.Generic;

namespace SalarAnts.Pack
{
	public class Ants
	{
		public static readonly Location North = new Location(-1, 0);
		public static readonly Location South = new Location(1, 0);
		public static readonly Location West = new Location(0, -1);
		public static readonly Location East = new Location(0, 1);

		public static IDictionary<Direction, Location> Aim = new Dictionary<Direction, Location>
		                                                     	{
		                                                     		{Direction.North, North},
		                                                     		{Direction.East, East},
		                                                     		{Direction.South, South},
		                                                     		{Direction.West, West}
		                                                     	};

		private const string READY = "ready";
		private const string GO = "go";
		private const string END = "end";

		private static GameState _state;
		public static IGameState GameStateInstance { get { return _state; } }



#if DEBUG
		private static string LogFile()
		{
#if TestInput
			return @"C:\slr\Projects\SalarAnts\Tools\SalarAnts\antinputlog.log";
#else
            return Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "antinputlog.log");
#endif
		}
#endif

		public void PlayGame(Bot bot)
		{
			var input = new List<string>();

#if TestInput || Benchmark

#if TestInput_NOT
			SalarAntsVisualRunner.VisualStart();
#endif
			using (var logFile = File.OpenText(LogFile()))
			{
				//foreach (var line in testData)
				while (!logFile.EndOfStream)
				{
					string line = logFile.ReadLine().Trim().ToLower();

					if (line.Equals(READY))
					{
						ParseSetup(input);
						FinishTurn();
						input.Clear();
					}
					else if (line.Equals(GO))
					{
						_state.StartNewTurn();
						ParseUpdate(input);

                        // BUGFIX:
                        _state.RemoveInvalidMyAnt();
						try
						{
							bot.DoTurn(_state);
						}
						finally
						{
							FinishTurn();
						}
						input.Clear();
					}
					else if (line.Equals(END))
					{
						break;
					}
					else
					{
						input.Add(line);
					}
				}
			}

#else
			try
			{
#if DebugSaveInput

				using (var inputLogFile = File.Create(LogFile()))
				{
					var inputLog = new StreamWriter(inputLogFile);
					inputLog.AutoFlush = true;
#endif
				while (true)
				{

					string line = System.Console.In.ReadLine();
					if (line == null)
						return;

					line = line.Trim().ToLower();
#if DebugSaveInput
						inputLog.WriteLine(line);
#endif

					if (line.Equals(READY))
					{
						ParseSetup(input);
						FinishTurn();
						input.Clear();
					}
					else if (line.Equals(GO))
					{
						_state.StartNewTurn();
						ParseUpdate(input);
                        
                        // BUGFIX:
                        _state.RemoveInvalidMyAnt();
                        
                        bot.DoTurn(_state);
						FinishTurn();
						input.Clear();
					}
					else if (line.Equals(END))
					{
						break;
					}
					else
					{
						input.Add(line);
					}
				}
#if DebugSaveInput
					inputLog.Close();
					inputLogFile.Close();
				}
#endif

			}
			catch (Exception e)
			{
#if DEBUG
				FileStream fs = new FileStream("debug.log", System.IO.FileMode.Create, System.IO.FileAccess.Write);
				StreamWriter sw = new StreamWriter(fs);
				sw.WriteLine(e);
				sw.Close();
				fs.Close();
#endif
			}
#endif

		}


		// parse initial input and setup starting game state
		private void ParseSetup(List<string> input)
		{
			int width = 0, height = 0;
			int turntime = 0, loadtime = 0;
			int viewradius2 = 0, attackradius2 = 0, spawnradius2 = 0;
			long playerSeed = DateTime.Now.Ticks;

			foreach (string line in input)
			{
				if (line.Length <= 0) continue;

				string[] tokens = line.Split();
				string key = tokens[0];

				if (key.Equals(@"cols"))
				{
					width = int.Parse(tokens[1]);
				}
				else if (key.Equals(@"rows"))
				{
					height = int.Parse(tokens[1]);
				}
				else if (key.Equals(@"player_seed"))
				{
					playerSeed = Convert.ToInt64(tokens[1]);
				}
				else if (key.Equals(@"turntime"))
				{
					turntime = int.Parse(tokens[1]);
				}
				else if (key.Equals(@"loadtime"))
				{
					loadtime = int.Parse(tokens[1]);
				}
				else if (key.Equals(@"viewradius2"))
				{
					viewradius2 = int.Parse(tokens[1]);
				}
				else if (key.Equals(@"attackradius2"))
				{
					attackradius2 = int.Parse(tokens[1]);
				}
				else if (key.Equals(@"spawnradius2"))
				{
					spawnradius2 = int.Parse(tokens[1]);
				}
			}

			_state = new GameState(width, height,
									   turntime, loadtime,
									   viewradius2, attackradius2, spawnradius2);
			_state.PlayerSeed = playerSeed;
		}

		// parse engine input and update the game state
		private void ParseUpdate(List<string> input)
		{
			// do some stuff first

			foreach (string line in input)
			{
				if (line.Length <= 0) continue;

				string[] tokens = line.Split();

				if (tokens.Length >= 3)
				{
					int row = int.Parse(tokens[1]);
					int col = int.Parse(tokens[2]);

					if (tokens[0].Equals("a"))
					{
						_state.AddAnt(row, col, int.Parse(tokens[3]));
					}
					else if (tokens[0].Equals("f"))
					{
						_state.AddFood(row, col);
					}
					else if (tokens[0].Equals("r"))
					{
						_state.RemoveFood(row, col);
					}
					else if (tokens[0].Equals("w"))
					{
						_state.AddWater(row, col);
					}
					else if (tokens[0].Equals("d"))
					{
						_state.DeadAnt(row, col);
					}
					else if (tokens[0].Equals("h"))
					{
						_state.AntHill(row, col, int.Parse(tokens[3]));
					}
				}
			}
		}

		private void FinishTurn()
		{
			System.Console.Out.WriteLine(GO);
		}
	}
}