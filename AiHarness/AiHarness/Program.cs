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
	class Program
	{
/*        private const string fooString = @"(;CA[Windows-1252]SZ[9]AP[MultiGo:4.4.4]MULTIGOGM[1];B[dd];W[ec];B[ed];W[fd];B[fe]
;W[ee];B[ef];W[de];B[df];W[ce];B[gd];W[fc];B[dc];W[ff];B[ge];W[db];B[gc];W[gb];B[eb]
;W[fb];B[hb];W[hc];B[fa];W[ga])";*/

        static void Main(string[] args)
		{
            UctTests.Run();

/*            SgfParser parser = new SgfParser(stayAliveTestString);
            SgfTree tree = parser.Root;

            SgfReplay replay = new SgfReplay(tree);
			while (replay.PlayMove()) ;
			GameSimulator.PrintBoard(replay.Board);

            UctPlayer player = new UctPlayer();
			player.SetBoard(replay.Board);
            player.SetNumSimulations(1000);
            int p = player.GetMove();
            Console.WriteLine(replay.Board.GetPointNotation(p));

			replay.Board.PlaceStone(p);
			GameSimulator.PrintBoard(replay.Board);

            return;*/

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
			this.players[GoBoard.White] = new NoSearchPlayer();
			for (int i = 0; i < 2; i++)
			{
				this.players[i].SetBoard(board);
			}

			int lastMove = GoBoard.MoveResign, lastLastMove = GoBoard.MoveResign;
			while (!(lastMove == GoBoard.MovePass && lastLastMove == GoBoard.MovePass))
			{
				lastLastMove = lastMove;
				lastMove = this.players[board.ToMove].GetMove();

				board.PlaceStone(lastMove);

				GameSimulator.PrintBoard(board);
				Console.ReadKey();
			 }


            GameSimulator.PrintBoard(board);
            float value = board.ScoreSimpleEndPosition(board.Komi);
			//Console.WriteLine(value + " " + ((value > 0) ? "Black wins" : "White wins"));
			return value > 0 ? 1 : (value < 0 ? 0 : 0.5f);
		}

		public static void PrintBoard(GoBoard board)
		{
			for (int y = 0; y < board.Size; y++)
			{
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
