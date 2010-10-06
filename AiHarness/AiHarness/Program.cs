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

			this.players[GoBoard.Black] = new UctPlayer(board);
			this.players[GoBoard.White] = new RandomPlayer(board);

			int lastMove = GoBoard.MoveResign, lastLastMove = GoBoard.MoveResign;
			while (!(lastMove == GoBoard.MovePass && lastLastMove == GoBoard.MovePass))
			{
				lastLastMove = lastMove;
				lastMove = this.players[board.ToMove].GetMove();

				board.PlaceStone(lastMove);

//				GameSimulator.PrintBoard(board);
//				Console.ReadKey();
			}


			float value = board.ScoreSimpleEndPosition(7.5f);
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

    public class UctPlayer : Player
    {
        private UctSearch search;
        
        public UctPlayer(GoBoard board)
            : base(board)
        {
            this.search = new UctSearch(board);
        }

        public override int GetMove()
        {
            this.search.SearchLoop();
            List<int> moves = this.search.FindBestSequence();
            return moves[0];
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
		private List<int> lowLibAnchors = new List<int>(4);

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

			// TODO: generate open board moves

            int lastMove = this.board.LastMove;
            if (move == GoBoard.MoveNull && lastMove >= 0)
            {
                if (this.GenerateAtariCaptureMove())
                {
                    move = this.SelectRandom();
                }
				if (move == GoBoard.MoveNull && this.GenerateAtariDefenseMove())
				{
					move = this.SelectRandom();
				}
				if (move == GoBoard.MoveNull && this.GenerateLowLibMove(lastMove))
				{
					move = this.SelectRandom();
				}

				// TODO: generate pattern moves
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

            Debug.Assert(this.board.IsLegal(move, this.board.ToMove));

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

				// Check if the move on the last liberty would escape the atari
                int liberty = this.board.TheLiberty(anchor);
				if (!this.board.SelfAtari(liberty, toMove))
					this.moves.Add(liberty);

				// Capture adjacent blocks
				List<int> adjacentBlocks = new List<int>(5);
				this.board.AdjacentBlocks(anchor, 1, adjacentBlocks);
				foreach (int opponentBlock in adjacentBlocks)
				{
					int oppLiberty = this.board.TheLiberty(opponentBlock);
					if (oppLiberty != liberty)
						this.moves.Add(oppLiberty);
				}
            }

			return this.moves.Count > 0;
        }

		private bool GenerateLowLibMove(int lastMove)
		{
			Debug.Assert(lastMove >= 0);
			Debug.Assert(this.board.Board[lastMove] != GoBoard.Empty);

			byte toMove = this.board.ToMove;

			// Take liberty of last move
			if (this.board.NumLiberties(lastMove) == 2)
			{
				int anchor = this.board.GetAnchor(lastMove);
				this.PlayGoodLiberties(anchor);
			}

			if (this.board.NumNeighbors(lastMove, toMove) != 0)
			{
				// Play liberties of neighbor blocks
				this.lowLibAnchors.Clear();
				for (int i = 0; i < 4; i++)
				{
					int p = lastMove + GoBoard.DirDelta[i];
					if (this.board.Board[p] == toMove && this.board.NumLiberties(p) == 2)
					{
						int anchor = this.board.GetAnchor(p);
						if (!this.lowLibAnchors.Contains(anchor))
						{
							this.lowLibAnchors.Add(anchor);
							this.PlayGoodLiberties(anchor);
						}
					}
				}
			}

			return this.moves.Count > 0;
		}

		private void PlayGoodLiberties(int anchor)
		{
			int ignored;
			if (!this.board.IsSimpleChain(anchor, out ignored))
			{
				foreach (int liberty in this.board.GetLiberties(anchor))
				{
					if (this.board.GainsLiberties(anchor, liberty) &&
						!this.board.SelfAtari(liberty, this.board.ToMove))
						this.moves.Add(liberty);
				}
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
			{
				return GoBoard.MoveNull;
			}

			if (this.moves.Count == 1)
			{
				int move = this.moves[0];
				if (PlayoutPolicy.IsMoveGood(this.board, move))
					return move;
                this.moves.Clear();
                return GoBoard.MoveNull;
			}

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

	class UctNode
	{
		private MeanTracker mean = new MeanTracker();
		private MeanTracker rave = new MeanTracker();

		public UctNode(MoveInfo moveInfo)
		{
			this.Move = moveInfo.Point;
			this.mean.Add(moveInfo.Value, moveInfo.Count);
			this.rave.Add(moveInfo.RaveValue, moveInfo.RaveCount);
		}

		public int Move { get; private set; }
		public UctNode FirstChild { get; set; }
		public UctNode Next { get; set; }
		public int NumChildren { get; set; }
		public int PosCount { get; set; }

		public bool HasMean { get { return this.mean.IsDefined; } }
        public int MoveCount { get { return (int)(this.mean.Count + 0.5f); } }
        public float Mean { get { return this.mean.Mean; } }

		public bool HasRaveValue { get { return this.rave.IsDefined; } }
		public float RaveCount { get { return this.rave.Count; } }
		public float RaveValue { get { return this.rave.Mean; } }
		
		public bool IsProven
		{
			get { return false; /* TODO! */ }
		}

		public void AddGameResult(float eval)
		{
			this.mean.Add(eval);
		}
	}

	struct MeanTracker
	{
		private float count;
		private float mean;

		public bool IsDefined
		{
			get { return this.count != 0.0f; }
		}

		public float Mean
		{
			get { return this.mean; }
		}

		public float Count
		{
			get { return this.count; }
		}

		public void Add(float value)
		{
			float count = this.count;
			++count;
			this.mean += (value - this.mean) / count;
			this.count = count;
		}

		public void Remove(float value)
		{
			if (this.count > 1)
			{
				float count = this.count;
				--count;
				this.mean += (this.mean - value) / count;
				this.count = count;
			}
			else
			{
				this.count = 0.0f;
				this.mean = 0.0f;
			}
		}

		public void Add(float value, float n)
		{
			float count = this.count;
			count += n;
			this.mean += n * (value - this.mean) / count;
			this.count = count;
		}

		public void Remove(float value, float n)
		{
			if (this.count > n)
			{
				float count = this.count;
				this.count -= n;
				this.mean += n * (this.mean - value) / count;
				this.count = count;
			}
			else
			{
				this.mean = 0.0f;
				this.count = 0.0f;
			}
		}
	}

	public struct MoveInfo
	{
		public int Point;
		public float Value;
		public int Count;
		public float RaveValue;
		public float RaveCount;

		public MoveInfo(int p)
		{
			this.Point = p;
			this.Value = 0.0f;
			this.Count = 0;
			this.RaveValue = this.RaveCount = 0.0f;
		}
	}

	class UctSearch
	{
		private GoBoard board;
        private GoBoard rootBoard;

		private List<UctNode> nodes = new List<UctNode>();
		private List<int> sequence = new List<int>();
		private PlayoutPolicy policy = new PlayoutPolicy();
		private UctNode rootNode = null;

		private float biasTermConstant = 0.7f;
		private float raveWeightInitial = 0.9f;
		private float raveWeightFinal = 20000.0f;
		private float raveWeightParam1, raveWeightParam2;
		private float firstPlayUrgency = 10000.0f;

		// Perf. variables
		private List<MoveInfo> perfLegalMoves = new List<MoveInfo>(100);

		public UctSearch(GoBoard board)
		{
			this.rootBoard = board;
            this.board = new GoBoard(this.rootBoard.Size);

			this.raveWeightParam1 = 1.0f / this.raveWeightInitial;
			this.raveWeightParam2 = 1.0f / this.raveWeightInitial;
		}

		public int MaxGameLength
		{
			get { return 3 * this.board.Size * this.board.Size; }
		}

		public void SearchLoop()
		{
			this.rootNode = new UctNode(new MoveInfo(GoBoard.MoveNull));

			int numberGames = 0;
			while (numberGames < 50)
			{
				this.PlayGame();
				numberGames++;
			}
		}

        public List<int> FindBestSequence()
        {
            List<int> result = new List<int>();
            UctNode current = this.rootNode;
            while (current != null)
            {
                current = this.FindBestChild(current);
                if (current == null)
                    break;
                result.Add(current.Move);
            }
            return result;
        }

        private UctNode FindBestChild(UctNode node)
        {
            UctNode bestChild = null, current = null;
            float bestValue = 0.0f;

            current = node.FirstChild;
            while (current != null)
            {
                float value = node.MoveCount;
                if (bestChild == null || value > bestValue)
                {
                    bestChild = current;
                    bestValue = value;
                }
                current = current.Next;
            }

            return bestChild;
        }

		public void PlayGame()
		{
            this.board.Initialize(this.rootBoard);

            this.nodes.Clear();

			bool isTerminal;
			bool abortInTree = !this.PlayInTree(out isTerminal);

			float eval = 0.0f;

			if (this.nodes.Count != 0 && this.nodes[this.nodes.Count - 1].IsProven)
			{
				// Play some "fake" playouts if node is proven
				// TODO!
			}
			else
			{
				int inTreeMoveCount = this.sequence.Count;

				bool abort = abortInTree;
				if (!abort && !isTerminal)
					abort = !this.PlayoutGame();
				if (abort)
					// Unknown score == 0.5
					eval = 0.5f;
				else
					eval = this.Evaluate();

				int numMoves = this.sequence.Count;
				if ((numMoves & 1) != 0)
					eval = 1 - eval;
			}

            // Take back moves 
            this.sequence.Clear();

			this.UpdateTree(eval);
			// TODO rave!
			// this.UpdateRaveValues();
		}

		private void UpdateTree(float eval)
		{
			// We count all playouts as one result
			float inverseEval = 1.0f - eval;

			for (int i = 0; i < this.nodes.Count; i++)
			{
				UctNode node = this.nodes[i];
				UctNode parent = i > 0 ? this.nodes[i - 1] : null;
				if (parent != null)
				{
					parent.PosCount++;
                }
                node.AddGameResult(i % 2 == 0 ? eval : inverseEval);
            }
		}

		private bool PlayInTree(out bool isTerminal)
		{
			int expandThreshold = 1;

			UctNode current = this.rootNode;
			bool breakAfterSelect = false;

			isTerminal = false;

            this.nodes.Add(this.rootNode);

			while (this.sequence.Count < this.MaxGameLength)
			{
				if (current.IsProven)
					break;

				if (current.NumChildren == 0)
				{
					this.perfLegalMoves.Clear();
					this.GenerateLegalMoves(this.perfLegalMoves);

					if (this.perfLegalMoves.Count == 0)
					{
						isTerminal = true;
						return true;
					}

                    if (current.MoveCount >= expandThreshold)
                    {
                        this.ScoreMoves(this.perfLegalMoves);
                        this.ExpandNode(current, this.perfLegalMoves);
                        breakAfterSelect = true;
                    }
                    else
                        break;
				}

				current = this.SelectChild(current);
				this.nodes.Add(current);
				int move = current.Move;
				
				// TODO: Might need to detect complex-ko here :(
				this.board.PlaceStone(move);
				this.sequence.Add(move);

				if (breakAfterSelect)
					break;
			}

			return true;
		}

		private float Evaluate()
		{
			// Score the result
			float score = this.board.ScoreSimpleEndPosition(7.5f);

			// Score is always in terms of black, but we want to return score relative to player to move.
			if (board.ToMove == GoBoard.White)
				score = -score;

			// Score normalized to -1..1
			float invMaxScore = 1.0f / (board.Size * board.Size + board.Komi);
			float scoreModification = 0.02f;

			if (score > 1e-6f)
			{
				return (1 - scoreModification) + scoreModification * score * invMaxScore - 0.5f;
			}
			else if (score < -1e-6f)
			{
				return scoreModification + scoreModification * score * invMaxScore + 0.5f;
			}
			// Draw!
			return 0.5f;
		}

		private void ScoreMoves(List<MoveInfo> moves)
		{
			// TODO Rave!
			bool isSmallBoard = this.board.Size < 15;
			for (int i = 0; i < moves.Count - 1; i++)
			{
				MoveInfo move = moves[i];
				if (this.board.SelfAtari(moves[i].Point, this.board.ToMove))
				{
					move.Value = 0.1f;
					move.Count = isSmallBoard ? 9 : 18;
				}
				else 
				{
					move.Value = 0.5f;
					move.Count = isSmallBoard ? 9 : 18;
				}
				moves[i] = move;
			}
			if (moves.Count > 0 && moves[moves.Count - 1].Point == GoBoard.MovePass)
			{
				MoveInfo move = moves[moves.Count - 1];
				move.Value = 0.1f;
				move.Count = isSmallBoard ? 9 : 18;
			}
		}

		private void ExpandNode(UctNode node, List<MoveInfo> moves)
		{
			int parentCount = 0;
			UctNode current = null, firstChild = null;
			foreach (MoveInfo moveInfo in moves)
			{
				if (current == null)
				{
					firstChild = current = new UctNode(moveInfo);
				}
				else
				{
					current.Next = new UctNode(moveInfo);
					current = current.Next;
				}
				parentCount += moveInfo.Count;
			}

			node.PosCount = parentCount;
			node.FirstChild = firstChild;
			node.NumChildren = moves.Count;
		}

		private UctNode SelectChild(UctNode node)
		{
			bool useRave = true; // TODO!

			int posCount = node.PosCount;
			if (posCount == 0)
				// If position count is zero, return first child
				return node.FirstChild;

			float logPosCount = (float)Math.Log(posCount);
			UctNode bestNode = null;
			float bestUpperBound = 0;

			UctNode current = node.FirstChild;
			while (current != null)
			{
				float bound = this.GetBound(useRave, logPosCount, current);
				if (bestNode == null || bound > bestUpperBound)
				{
					bestNode = current;
					bestUpperBound = bound;
				}
				current = current.Next;
			}

			return bestNode;
		}

		private float GetBound(bool useRave, float logPosCount, UctNode child)
		{
			float value = this.GetValueEstimate(useRave, child);
			if (this.biasTermConstant == 0.0f)
				return value;
			else
			{
				return value + this.biasTermConstant * (float)Math.Sqrt(logPosCount / (child.MoveCount + 1));
			}
		}

		private float GetValueEstimate(bool useRave, UctNode child)
		{
			float value = 0.0f;
			float weightSum = 0.0f;
			bool hasValue = false;

			if (child.HasMean)
			{
				float weight = child.MoveCount;
				value += weight * 1 - child.Mean;
				weightSum += weight;
				hasValue = true;
			}

			if (useRave && child.HasRaveValue)
			{
				float raveCount = child.RaveCount;
				float weight = raveCount / (this.raveWeightParam1 + this.raveWeightParam2 * raveCount);
				value += weight * child.RaveValue;
				weightSum += weight;
				hasValue = true;
			}

			if (hasValue)
				return value / weightSum;
			else
				return this.firstPlayUrgency;
		}

		public void GenerateLegalMoves(List<MoveInfo> moves)
		{
			if (this.board.LastMove == GoBoard.MovePass &&
				this.board.SecondLastMove == GoBoard.MovePass)
			{
				return;
			}

			for (int y = 0; y < this.board.Size; y++)
			{
				for (int x = 0; x < this.board.Size; x++)
				{
					int p = GoBoard.GeneratePoint(x, y);
					if (this.board.IsLegal(p, this.board.ToMove) &&
						!this.board.IsSimpleEye(p, this.board.ToMove))
						moves.Add(new MoveInfo(p));
				}
			}

			if (moves.Count > 1)
			{
				// Don't randomize the entire move list, too expensive.  Instead make sure the first played move is
				// random.  RAVE will then cause good moves to quickly move to the front.
				int index = GoBoard.Random.Next(moves.Count);
				MoveInfo tmp = moves[0];
				moves[0] = moves[index];
				moves[index] = tmp;
			}

			moves.Add(new MoveInfo(GoBoard.MovePass));
		}

		private bool PlayoutGame()
		{
			this.policy.Initialize(this.board);

			int lastMove = GoBoard.MoveResign, lastLastMove = GoBoard.MoveResign;
			while (!(lastMove == GoBoard.MovePass && lastLastMove == GoBoard.MovePass))
			{
				if (this.sequence.Count > 3 * this.board.Size * this.board.Size)
				{
					// Draw, forced by super-ko
					return false;
				}

				// TODO: mercy rule?
				lastLastMove = lastMove;
				lastMove = this.policy.GenerateMove();

				this.board.PlaceStone(lastMove);
				this.sequence.Add(lastMove);

				this.policy.OnPlay();
			}

			return true;
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

            bool invertScore = board.ToMove == GoBoard.White;

			for (int i = 0; i < moves.Count; i++)
            {
				if (PlayoutPolicy.IsMoveGood(this.board, moves[i]))
				{
					clone.Initialize(this.board);
					clone.PlaceStone(moves[i]);

					policy.Initialize(clone);
					float value = Playout(clone, policy);
                    if (invertScore) value = -value;

					if (value > bestMoveScore)
					{
						bestMoveScore = value;
						bestMove = moves[i];
					}
				}
            }

			return bestMove;
		}

		public static void Replay(List<int> moves, int move)
		{
			GoBoard newBoard = new GoBoard(9);
			newBoard.Reset();
			for (int i = 0; i < moves.Count; i++)
			{
				newBoard.PlaceStone(moves[i]);
			}
			GameSimulator.PrintBoard(newBoard);
			newBoard.PlaceStone(move);
			GameSimulator.PrintBoard(newBoard);
		}

        private static float Playout(GoBoard board, PlayoutPolicy policy)
        {
			List<int> moves = new List<int>();
            int moveCount = 0;
			int lastMove = GoBoard.MoveResign, lastLastMove = GoBoard.MoveResign;
			while (!(lastMove == GoBoard.MovePass && lastLastMove == GoBoard.MovePass))
			{
				// TODO: mercy rule?

				lastLastMove = lastMove;
				lastMove = policy.GenerateMove();

				Debug.Assert(board.IsLegal(lastMove, board.ToMove));

				board.PlaceStone(lastMove);
				moves.Add(lastMove);

				policy.OnPlay();

                moveCount++;

                if (moveCount > 3 * board.Size * board.Size)
                {
					// Draw, forced by super-ko
                    return 0.5f;
                }
			}

			float score = board.ScoreSimpleEndPosition(7.5f);

			return score;
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

    class NoSearchPlayer : Player
    {
        private PlayoutPolicy playoutPolicy = new PlayoutPolicy();

        public NoSearchPlayer(GoBoard board)
            : base(board)
        {
        }

        public override int GetMove()
        {
            PlayoutPolicy policy = new PlayoutPolicy();
            policy.Initialize(this.board);

            return policy.GenerateMove();
        }
    }
}
