using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThinkGo;

namespace AiHarness
{
	class Program
	{
		static void Main(string[] args)
		{
			GameSimulator simulator = new GameSimulator();
			for (int i = 0; i < 20; i++)
				simulator.PlayGame();

		}
	}

	class GameSimulator
	{
		private RandomPlayer[] players = new RandomPlayer[2];

		public void PlayGame()
		{
			GoBoard board = new GoBoard(9);
			board.Reset();

			this.players[0] = new RandomPlayer(board);
			this.players[1] = new RandomPlayer(board);

			int lastMove = GoBoard.MoveResign, lastLastMove = GoBoard.MoveResign;
			while (!(lastMove == GoBoard.MovePass && lastLastMove == GoBoard.MovePass))
			{
				lastLastMove = lastMove;
				lastMove = this.players[board.ToMove].GetMove();

				board.PlaceStone(lastMove);
			}
			this.PrintBoard(board);
		}

		private void PrintBoard(GoBoard board)
		{
			for (int y = 0; y < board.Size; y++)
			{
				for (int x = 0; x < board.Size; x++)
				{
					byte stone = board.Board[GoBoard.GenerateMove(x, y)];
					if (stone == GoBoard.White)
					{
						Console.Write('x');
					}
					else if (stone == GoBoard.Black)
					{
						Console.Write('o');
					}
					else
					{
						Console.Write('.');
					}
				}
				Console.WriteLine();
			}
			Console.WriteLine();
		}
	}

	class RandomPlayer
	{
		private GoBoard board;
		private static Random random = new Random();

		public RandomPlayer(GoBoard board)
		{
			this.board = board;
		}

		public int GetMove()
		{
			List<int> moves = this.GenerateMoves();
			if (moves.Count == 1)
			{
				int i = 0;
			}
			return moves[random.Next(moves.Count)];
		}

		private List<int> GenerateMoves()
		{
			List<int> moves = new List<int>();
			for (int y = 0; y  < this.board.Size; y++)
			{
				for (int x = 0; x < this.board.Size; x++)
				{
					int move = GoBoard.GenerateMove(x, y);
					if (this.board.IsLegal(move, this.board.ToMove) &&
						!this.board.IsCompletelySurrounded(move))
					{
						moves.Add(move);
					}
				}
			}

			if (moves.Count == 0)
			{
				moves.Add(GoBoard.MovePass);
			}

			return moves;
		}
	}
}
