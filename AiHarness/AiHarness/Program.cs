using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThinkGo;
using System.Diagnostics;

namespace AiHarness
{
	class Program
	{
		static void Main(string[] args)
		{
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

			this.players[0] = new RandomPlayer(board);
			this.players[1] = new PlayoutPlayer(board);

			int lastMove = GoBoard.MoveResign, lastLastMove = GoBoard.MoveResign;
			while (!(lastMove == GoBoard.MovePass && lastLastMove == GoBoard.MovePass))
			{
				lastLastMove = lastMove;
				lastMove = this.players[board.ToMove].GetMove();

				board.PlaceStone(lastMove);
			}

			float value = board.ScoreSimpleEndPosition(7.5f);
			//Console.WriteLine(value + " " + ((value > 0) ? "Black wins" : "White wins"));
			return value > 0 ? 1 : (value < 0 ? 0 : 0.5f);
//			this.PrintBoard(board);
		}

		public static void PrintBoard(GoBoard board)
		{
			for (int y = 0; y < board.Size; y++)
			{
				for (int x = 0; x < board.Size; x++)
				{
					byte stone = board.Board[GoBoard.GeneratePoint(x, y)];
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

	public abstract class Player
	{
		protected GoBoard board;

		public Player(GoBoard board)
		{
			this.board = board;
		}

		public abstract int GetMove();

		protected List<int> GenerateMoves()
		{
			List<int> moves = new List<int>();
			for (int y = 0; y < this.board.Size; y++)
			{
				for (int x = 0; x < this.board.Size; x++)
				{
					int point = GoBoard.GeneratePoint(x, y);
					if (this.board.Board[point] == GoBoard.Empty)
					{
						moves.Add(point);
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

    public class CaptureGenerator
    {
        private GoBoard board;
        private List<int> candidates = new List<int>();

        public void Initialize(GoBoard board)
        {
            this.board = board;
            this.candidates.Clear();

            for (int y = 0; y < this.board.Size; y++)
            {
                for (int x = 0; x < this.board.Size; x++)
                {
                    int point = GoBoard.GeneratePoint(x, y);
                    if (this.board.Board[point] != GoBoard.Empty &&
                        this.board.GetAnchor(point) == point &&
                        this.board.InAtari(point))
                    {
                        this.candidates.Add(point);
                    }
                }
            }
        }

        public void Generate(List<int> moves)
        {
            Debug.Assert(moves.Count == 0);
            byte opponent = GoBoard.OppColor(this.board.ToMove);
            // Does not check for duplicate generated moves for efficiency
            // reasons.  Usually there are zero or one capture moves.
            for (int i = 0; i < this.candidates.Count; i++)
            {
                int p = this.candidates[i];
                if (!this.board.OccupiedInAtari(p))
                {
                    this.candidates[i] = this.candidates[this.candidates.Count - 1];
                    this.candidates.RemoveAt(this.candidates.Count - 1);
                    i--;
                    continue;
                }
                if (this.board.Board[p] == opponent)
                    moves.Add(this.board.TheLiberty(p));
            }
        }

        public void OnPlay()
        {
            int lastMove = this.board.LastMove;
            if (lastMove < 0)
                return;

            if (this.board.OccupiedInAtari(lastMove))
                this.candidates.Add(this.board.GetAnchor(lastMove));

            // If we don't have any neighbors of our color, then we couldn't have
            // removed any liberties from neighboring opponent stones (as placing
            // our stone would have killed them all, or wouldn't be possible)
            if (this.board.NumNeighbors(lastMove, this.board.ToMove) == 0)
                return;

            if (this.board.OccupiedInAtari(lastMove + GoBoard.NS))
                this.candidates.Add(this.board.GetAnchor(lastMove + GoBoard.NS));
            if (this.board.OccupiedInAtari(lastMove - GoBoard.NS))
                this.candidates.Add(this.board.GetAnchor(lastMove - GoBoard.NS));
            if (this.board.OccupiedInAtari(lastMove + 1))
                this.candidates.Add(this.board.GetAnchor(lastMove + 1));
            if (this.board.OccupiedInAtari(lastMove - 1))
                this.candidates.Add(this.board.GetAnchor(lastMove - 1));
        }
    }

	public class RandomMoveGenerator
	{
		private GoBoard board;
		private List<int> moves = new List<int>();

		public void Initialize(GoBoard board)
		{
			// Initialize and shuffle the list of moves
			this.moves.Clear();
			this.board = board;

			for (int y = 0; y < this.board.Size; y++)
			{
				for (int x = 0; x < this.board.Size; x++)
				{
					int point = GoBoard.GeneratePoint(x, y);
					if (this.board.Board[point] == GoBoard.Empty)
					{
						this.Insert(point);
					}
				}
			}
		}

		public int SelectRandomMove()
		{
			int i = this.moves.Count;
			while (--i >= 0)
			{
				int move = this.moves[i];
				if (this.board.Board[move] != GoBoard.Empty)
				{
					this.moves[i] = this.moves[moves.Count - 1];
					this.moves.RemoveAt(moves.Count - 1);
				}
				else if (PlayoutPolicy.IsMoveGood(this.board, move))
				{
					return move;
				}
			}

			return GoBoard.MovePass;
		}

		public void OnPlay()
		{
			foreach (int captured in this.board.CapturedStones)
			{
				this.Insert(captured);
			}
		}

		/// <summary>
		/// Insert point at a random position in the list.  Re-orders existing list for O(1) performance.
		/// </summary>
		private void Insert(int point)
		{
			if (this.moves.Count == 0)
			{
				this.moves.Add(point);
			}
			else
			{
				int index = GoBoard.Random.Next(this.moves.Count);
				int move = this.moves[index];
				this.moves.Add(move);
				this.moves[index] = point;
			}
		}
	}

	public class PlayoutPolicy
	{
		private RandomMoveGenerator randomGenerator = new RandomMoveGenerator();
        private CaptureGenerator captureGenerator = new CaptureGenerator();
		private GoBoard board;
        private List<int> moves = new List<int>(5);

        // Perf variables
        private List<int> atariDefense = new List<int>(4);

		public static bool IsMoveGood(GoBoard board, int point)
		{
			return board.IsLegal(point, board.ToMove) &&
				   !board.IsCompletelySurrounded(point);

			// TODO: Need to handle not generating mutual atari moves from pure random?
		}

		public void Initialize(GoBoard board)
		{
			this.board = board;

			this.randomGenerator.Initialize(board);
            this.captureGenerator.Initialize(board);
		}

		public int GenerateMove()
		{
            this.moves.Clear();

			int move = GoBoard.MoveNull;

            int lastMove = this.board.LastMove;
            if (move == GoBoard.MoveNull && lastMove >= 0)
            {
                if (this.GenerateAtariCaptureMove())
                {
                    move = this.SelectRandom();
                }
            }

            if (move == GoBoard.MoveNull)
            {
                this.captureGenerator.Generate(this.moves);
                move = this.SelectRandom();
            }

            if (move == GoBoard.MoveNull)
			{
				move = this.randomGenerator.SelectRandomMove();
			}

            if (move == GoBoard.MoveNull)
            {
                move = GoBoard.MovePass;
            }

			return move;
		}

        private bool GenerateAtariCaptureMove()
        {
            Debug.Assert(this.moves.Count == 0);
            if (this.board.InAtari(this.board.LastMove))
            {
                this.moves.Add(this.board.TheLiberty(this.board.LastMove));
                return true;
            }
            return false;
        }

        private bool GenerateAtariDefenseMove()
        {
            Debug.Assert(this.moves.Count == 0);
            byte toMove = this.board.ToMove;
            if (this.board.NumNeighbors(this.board.LastMove, toMove) == 0)
                return false;

            this.atariDefense.Clear();
            for (int i = 0; i < 4; i++)
            {
                int p = this.board.LastMove + GoBoard.DirDelta[i];
                if (this.board.Board[p] != toMove || !this.board.InAtari(p))
                    continue;

                int anchor = this.board.GetAnchor(p);
                if (this.atariDefense.Contains(anchor))
                    continue;
                this.atariDefense.Add(anchor);

                int liberty = this.board.TheLiberty(anchor);
            }
        }

        public void OnPlay()
		{
			this.randomGenerator.OnPlay();
            this.captureGenerator.OnPlay();
		}

        private int SelectRandom()
        {
            if (this.moves.Count == 0)
                return GoBoard.MoveNull;

            int i = GoBoard.Random.Next(this.moves.Count);
            do
            {
                if (PlayoutPolicy.IsMoveGood(this.board, this.moves[i]))
                    return this.moves[i];
                this.moves[i] = this.moves[this.moves.Count - 1];
                this.moves.RemoveAt(this.moves.Count - 1);
                i = i >= this.moves.Count ? 0 : i;
            } while (this.moves.Count > 0);

            return GoBoard.MoveNull;
        }
	}

	class PlayoutPlayer : Player
	{
		private PlayoutPolicy playoutPolicy = new PlayoutPolicy();

		public PlayoutPlayer(GoBoard board) : base(board)
		{
		}

		public override int GetMove()
		{
            List<int> moves = this.GenerateMoves();
			int bestMove = GoBoard.MovePass;
			float bestMoveScore = -100000;
			byte toMove = this.board.ToMove;
			
			GoBoard clone = new GoBoard(this.board.Size);
			PlayoutPolicy policy = new PlayoutPolicy();

			for (int i = 0; i < moves.Count; i++)
            {
				if (PlayoutPolicy.IsMoveGood(this.board, moves[i]))
				{
					clone.Initialize(this.board);
					clone.PlaceStone(moves[i]);

					policy.Initialize(clone);
					float value = Playout(clone, policy);

					// Score is in terms of black
					if (toMove == GoBoard.White)
					{
						value = -value;
					}

					if (value > bestMoveScore)
					{
						bestMoveScore = value;
						bestMove = moves[i];
					}
				}
            }

			return bestMove;
		}

        private float Playout(GoBoard board, PlayoutPolicy policy)
        {
            int moveCount = 0;
			int lastMove = GoBoard.MoveResign, lastLastMove = GoBoard.MoveResign;
			while (!(lastMove == GoBoard.MovePass && lastLastMove == GoBoard.MovePass))
			{
				lastLastMove = lastMove;
				lastMove = policy.GenerateMove();

				board.PlaceStone(lastMove);

				policy.OnPlay();

                moveCount++;

                if (moveCount > 200)
                {
                    Console.WriteLine("overboard");
                    return 0.5f;
                }
			}

			// Score the result
			return board.ScoreSimpleEndPosition(7.5f);
        }
	}

	class RandomPlayer : Player
	{
		public RandomPlayer(GoBoard board) : base(board)
		{
		}

		public override int GetMove()
		{
			List<int> moves = this.GenerateMoves();
			int at = GoBoard.Random.Next(moves.Count);
			int count = 0;
			while (!PlayoutPolicy.IsMoveGood(this.board, moves[at]))
			{
				at = (at + 1) % moves.Count;
				count++;
				if (count == moves.Count)
				{
					return GoBoard.MovePass;
				}
			}
			return moves[at];
		}
	}
}
