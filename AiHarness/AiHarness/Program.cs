using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThinkGo;
using System.Diagnostics;
using ThinkGo.Ai;
using AiHarness.Tests;

namespace AiHarness
{
	public class Region : List<int>
	{
	}

	public class GoSafetySolver
	{
		private GoBoard board;
		private bool[] safePoints;
		private List<List<int>> chains = new List<List<int>>();
		private List<List<Region>> regions = new List<List<Region>>();
		private int[] visited;

		public GoSafetySolver(GoBoard board)
		{
			this.board = board;
			this.safePoints = new bool[GoBoard.MaxPoints];

			this.Solve();
		}

		private void Solve()
		{
			for (int i = 0; i < 2; i++)
			{
				this.regions[i] = new List<Region>();
				this.chains[i] = new List<int>();

				this.visited = new int[GoBoard.MaxPoints];
				for (int j = 0; j < this.visited.Length; j++)
					this.visited[j] = -1;

				for (int y = 0; y < this.board.Size; y++)
				{
					for (int x = 0; x < this.board.Size; x++)
					{
						int p = GoBoard.GeneratePoint(x, y);
						if (this.board.Board[p] == i)
						{
							if (this.board.GetAnchor(p) == p)
							{
								this.chains[i].Add(p);
							}
						}
						else
						{
							Region region = this.BuildRegion(null, (byte)i, x, y, this.regions[i].Count);
							if (region != null)
								this.regions[i].Add(region);
						}
					}
				}

                // TODO: for now, we will use a heuristic here
                // 

//				this.Iterate((byte)i);
			}
		}

/*		private void Iterate(byte c)
		{
			bool changed = true;
			while (changed)
			{
				// Remove chains that don't have at least 2 adjacent regions with all empty points in the region adjacent to the chain
				for (int i = this.chains[c].Count - 1; i >= 0; i--)
				{
					int anchor = this.chains[c][i];
					foreach (int liberty in this.board.GetLiberties(anchor))
					{
						int regionNumber = this.visited[liberty];
						Debug.Assert(regionNumber != -1);
						Region region = this.regions[c][regionNumber];
						
					}
				}
			}
		}*/

		private Region BuildRegion(Region region, byte excludedColor, int x, int y, int regionNumber)
		{
			int p = GoBoard.GeneratePoint(x, y);
			if (this.visited[p] != -1)
				return null;

			this.visited[p] = regionNumber;

			if (this.board.Board[p] == excludedColor)
				return null;

			if (region == null)
				region = new Region();
			region.Add(p);

			if (x - 1 >= 0) this.BuildRegion(region, excludedColor, x - 1, y, regionNumber);
			if (x + 1 < this.board.Size) this.BuildRegion(region, excludedColor, x + 1, y, regionNumber);
			if (y - 1 >= 0) this.BuildRegion(region, excludedColor, x, y - 1, regionNumber);
			if (y + 1 < this.board.Size) this.BuildRegion(region, excludedColor, x, y + 1, regionNumber);

			return region;
		}
	}

	class Program
	{
/*        private const string fooString = @"(;CA[Windows-1252]SZ[9]AP[MultiGo:4.4.4]MULTIGOGM[1];B[dd];W[ec];B[ed];W[fd];B[fe]
;W[ee];B[ef];W[de];B[df];W[ce];B[gd];W[fc];B[dc];W[ff];B[ge];W[db];B[gc];W[gb];B[eb]
;W[fb];B[hb];W[hc];B[fa];W[ga])";*/

        private const string easyTest = @"(;GM[1]FF[4]AP[Drago:4.11]SZ[9]CA[UTF-8]AB[ed][fd][gd][hd][de][ce][ef][ff][gf][hf][if][bf][be]AW[dd][cd][ee][fe][ge][he][ie][df][cf][ae][bd][bg]PL[W])";

        static double ScoreSearch(GoBoard board)
        {
            UctSearch scoreSearch = new UctSearch(board);
            double estimate = scoreSearch.EstimateScore();
            Console.WriteLine("Probability of winning: " + estimate);
            return estimate;
        }

        static void PlayGameAgainstHuman()
        {
            GoBoard board = new GoBoard(9);
            UctSearch search = new UctSearch(board);
            Console.WriteLine("Type move in format: 'b7', or 'pass'");
            Console.Write("Time per move (seconds): ");
            search.SetMillisecondsToSearch(double.Parse(Console.ReadLine()) * 1000);
            board.Reset();
            GameSimulator.PrintBoard(board);
            for (; ; )
            {
                string move = Console.ReadLine();
                if (string.Equals(move, "pass"))
                    break;
                int y = char.ToLower(move[1]) - 'a';
                int x = move[0] - '1';
                board.PlaceStone(GoBoard.GeneratePoint(x, y));
                GameSimulator.PrintBoard(board);
                search.SearchLoop();
                board.PlaceStone(search.FindBestSequence()[0]);
                GameSimulator.PrintBoard(board);
                Console.WriteLine(search.SimulationsDone);
            }

            GameSimulator.PrintBoard(board);
        }

        static void Main(string[] args)
		{
            //UctTests.Run();

            //PlayGameAgainstHuman();

            SgfParser parser = new SgfParser(easyTest);
            SgfTree tree = parser.Root;

            SgfReplay replay = new SgfReplay(tree);
			while (replay.PlayMove()) ;
			GameSimulator.PrintBoard(replay.Board);

            UctSearch search = new UctSearch(replay.Board);
            search.SetNumSimulations(300);
            search.SearchLoop();
            List<int> moves = search.FindBestSequence();

            foreach (int p in moves)
            {
                Console.WriteLine(replay.Board.GetPointNotation(p));

                replay.Board.PlaceStone(p);
                GameSimulator.PrintBoard(replay.Board);
            }
            //ScoreSearch(replay.Board);
            return;

			GameSimulator simulator = new GameSimulator();
			float value = 0;
			for (int i = 0; i < 200; i++)
			{
				value += simulator.PlayGame();
				float percent = value / (i + 1);
				Console.Write("\r" + string.Format("{0:0.00}% ", (percent * 100))  + value + "/" + (i + 1) + "                            ");
			}
		}
	}

	class GameSimulator
	{
		private Player[] players = new Player[2];

		public float PlayGame()
		{
			GoBoard board = new GoBoard(9);
			board.Reset();

			this.players[GoBoard.Black] = new UctPlayer();
			this.players[GoBoard.White] = new UctPlayer();

			for (int i = 0; i < 2; i++)
			{
				this.players[i].SetBoard(board);
			}
            ((UctPlayer)this.players[GoBoard.White]).SetNumSimulations(30);
			int lastMove = GoBoard.MoveResign, lastLastMove = GoBoard.MoveResign;
			while (!(lastMove == GoBoard.MovePass && lastLastMove == GoBoard.MovePass))
			{
				lastLastMove = lastMove;
				lastMove = this.players[board.ToMove].GetMove();

				board.PlaceStone(lastMove);

				//GameSimulator.PrintBoard(board);
				//Console.ReadKey();
			}


            //GameSimulator.PrintBoard(board);
            float value = board.ScoreSimpleEndPosition(board.Komi, null);
			Console.WriteLine(value + " " + ((value > 0) ? "Black wins" : "White wins"));
			return value > 0 ? 1 : (value < 0 ? 0 : 0.5f);
		}

		public static void PrintBoard(GoBoard board)
		{
            Console.Write(' ');
            for (int top = 0; top < board.Size; top++)
                Console.Write((char)('a' + top));
            Console.WriteLine();
			for (int y = 0; y < board.Size; y++)
			{
                Console.Write(board.Size - y);
				for (int x = 0; x < board.Size; x++)
				{
					int p = GoBoard.GeneratePoint(x, y);
					byte stone = board.Board[p];
					if (stone == GoBoard.White)
					{
						Console.BackgroundColor = ConsoleColor.White;
						if (board.LastMove == p)
							Console.BackgroundColor = ConsoleColor.Yellow;
						Console.Write('X');
					}
					else if (stone == GoBoard.Black)
					{
						Console.BackgroundColor = ConsoleColor.Blue;
						if (board.LastMove == p)
							Console.BackgroundColor = ConsoleColor.Yellow;
						Console.Write('O');
					}
					else
					{
						Console.BackgroundColor = ConsoleColor.Black;
						Console.Write('.');
					}
				}
				Console.BackgroundColor = ConsoleColor.Black;
				Console.WriteLine();
			}
			Console.WriteLine();
		}
	}

}
