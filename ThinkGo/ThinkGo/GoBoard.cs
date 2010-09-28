namespace ThinkGo
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using System;

	public sealed class GoBoard
	{
		public static Random Random = new Random();

		public const byte White = 0;
		public const byte Black = 1;
		public const byte Empty = 2;
		public const byte Border = 3;

		public static byte OppColor(byte c)
		{
			return (byte)(1 - c);
		}

		public const int MovePass = -1;
		public const int MoveResign = -2;
		public const int MoveNull = -3;

		public const int MaxPoints = 20 * 21;
		public const int NS = 20;

		public static readonly int[] DirDelta = new int[] { NS, -NS, 1, -1 };

		private int[][] numNeighbors;
		private int[] numNeighborsEmpty;
		private Block[] blocks;
		private Block[] blockCache;

		// Perf. variables, so we don't have to keep allocating them
		private PointMarker marker = new PointMarker(), marker2 = new PointMarker();
		private List<Block> adjBlocks = new List<Block>(4);

		public GoBoard(int size)
		{
			this.Size = size;

			this.Board = new byte[MaxPoints];

			this.numNeighbors = new int[2][];
			this.numNeighbors[0] = new int[MaxPoints];
			this.numNeighbors[1] = new int[MaxPoints];

			this.numNeighborsEmpty = new int[MaxPoints];

			this.blocks = new Block[MaxPoints];
			this.blockCache = new Block[MaxPoints];
			for (int i = 0; i < MaxPoints; i++)
			{
				this.blockCache[i] = new Block();
			}
		}

		public byte[] Board;
		public int Size;
		public int KoPoint;
		public int LastMove;
		public int SecondLastMove;
		public int[] Prisoners = new int[2];
		public byte ToMove;
		public List<int> CapturedStones = new List<int>();

		public static int GeneratePoint(int x, int y)
		{
			return (y + 1) * NS + x;
		}

		public bool IsLegal(int move, byte c)
		{
			if (move == MovePass)
				return true;
			if (this.Board[move] != Empty)
				return false;
			if (this.IsSuicide(move, c))
				return false;
			if (move == this.KoPoint && c == this.ToMove)
				return false;
			return true;
		}

		public bool IsSuicide(int p, byte c)
		{
			// If we have some empty squares around us, we can't be a suicide
			if (this.numNeighborsEmpty[p] != 0)
				return false;

			byte opp = OppColor(c);
			for (int i = 0; i < 4; i++)
			{
				byte piece = this.Board[p + DirDelta[i]];
				if (piece == Border) continue;

				// Our block has more than one liberty, then ok (we will be left with at least one liberty)
				if (piece == c && this.blocks[p + DirDelta[i]].Liberties.Count > 1)
					return false;

				// Opponent block with one liberty, then ok (we will kill it)
				if (piece == opp && this.blocks[p + DirDelta[i]].Liberties.Count == 1)
					return false;
			}

			return true;
		}

		/// <summary>
		/// The assumption here is that the game has been played out until the end.  Any groups remaining are alive, or they would
		/// have been taken over.  Any points that are surrounded by one color belong to that color, otherwise they are neutral.
		/// 
		/// Score is returned with black winning as positive numbers.
		/// </summary>
		public float ScoreSimpleEndPosition(float komi)
		{
			float score = -komi;
			for (int y = 0; y < this.Size; y++)
			{
				for (int x = 0; x < this.Size; x++)
				{
					int p = GeneratePoint(x, y);
					byte c = this.Board[p];
					if (c == Empty)
					{
						if (this.numNeighbors[Black][p] > 0 &&
							this.numNeighbors[White][p] == 0)
							c = Black;
						else if (this.numNeighbors[White][p] > 0 &&
								 this.numNeighbors[Black][p] == 0)
							c = White;
					}

					if (c == White)
						--score;
					else if (c == Black)
						++score;
				}
			}
			return score;
		}

		public void Reset()
		{
			for (int i = 0; i < MaxPoints; i++)
			{
				this.Board[i] = Border;
				this.blocks[i] = null;
				this.numNeighbors[0][i] = 0;
				this.numNeighbors[1][i] = 0;
			}

			for (int y = 0; y < this.Size; y++)
			{
				for (int x = 0; x < this.Size; x++)
				{
					int i = (y + 1) * NS + x;
					this.Board[i] = Empty;

					int neighbors = 0;
					if (y != 0) neighbors++;
					if (y != this.Size - 1) neighbors++;
					if (x != 0) neighbors++;
					if (x != this.Size - 1) neighbors++;
					this.numNeighborsEmpty[i] = neighbors;
				}
			}

			this.KoPoint = -1;
			this.LastMove = -2;
			this.SecondLastMove = 2;

			this.Prisoners[0] = this.Prisoners[1] = 0;
			this.ToMove = Black;
		}

		public void Initialize(GoBoard board)
		{
			if (board.Size != this.Size)
				throw new InvalidOperationException();

			this.Prisoners[0] = board.Prisoners[0];
			this.Prisoners[1] = board.Prisoners[1];
			this.KoPoint = board.KoPoint;
			this.LastMove = board.LastMove;
			this.SecondLastMove = board.SecondLastMove;
			this.ToMove = board.ToMove;

			for (int i = 0; i < board.Board.Length; i++)
			{
				this.Board[i] = board.Board[i];
				this.numNeighbors[0][i] = board.numNeighbors[0][i];
				this.numNeighbors[1][i] = board.numNeighbors[1][i];
				this.numNeighborsEmpty[i] = board.numNeighborsEmpty[i];
				if (board.Board[i] == Empty || board.Board[i] == Border)
					this.blocks[i] = null;
				else if (board.blocks[i].Anchor == i)
				{
					byte c = this.Board[i];
					Block b = this.blockCache[i];
					b.Stones.Clear();
					b.Liberties.Clear();

					b.Color = c;
					b.Anchor = i;
					for (int j = 0; j < board.blocks[i].Stones.Count; j++)
					{
						b.Stones.Add(board.blocks[i].Stones[j]);
						this.blocks[board.blocks[i].Stones[j]] = b;
					}
					for (int j = 0; j < board.blocks[i].Liberties.Count; j++)
						b.Liberties.Add(board.blocks[i].Liberties[j]);
				}
			}

			this.DebugVerify();
		}

		public List<int> PlaceStone(int move)
		{
			this.CapturedStones.Clear();
			byte opponent = OppColor(this.ToMove);

			if (move != MovePass)
			{
				this.AddStone(move, this.ToMove);

				if (this.adjBlocks.Count != 0)
					this.adjBlocks.Clear();

				if (this.numNeighbors[this.ToMove][move] > 0 ||
					this.numNeighbors[opponent][move] > 0)
					this.RemoveLibertiesAndKill(move, opponent, this.adjBlocks);

				this.UpdateBlocksAfterAddStone(move, this.ToMove, this.adjBlocks);

				if (this.KoPoint != -1)
				{
					Block b = this.blocks[move];
					if (b.Stones.Count > 1 || b.Liberties.Count > 1)
					{
						this.KoPoint = -1;
					}
				}
				Debug.Assert(this.blocks[move].Liberties.Count != 0, "Suicide not supported");
			}

			this.SecondLastMove = this.LastMove;
			this.LastMove = move;
			this.ToMove = opponent;

			this.DebugVerify();

			return this.CapturedStones;
		}

        public int GetAnchor(int p)
        {
            return this.blocks[p].Anchor;
        }

        public bool InAtari(int p)
        {
            return this.blocks[p].Liberties.Count <= 1;
        }

        public bool OccupiedInAtari(int p)
        {
            Block b = this.blocks[p];
            return b != null && b.Liberties.Count <= 1;
        }

        public int NumNeighbors(int p, byte c)
        {
            return this.numNeighbors[c][p];
        }

        public int TheLiberty(int p)
        {
            return this.blocks[p].Liberties[0];
        }

		/// <summary>
		/// Returns true if point is surrounded by one color and no adjacent block is in atari.
        /// Good criterion for move generation in Monte-Carlo. See:
		/// Remi Coulom: Efficient selectivity and backup operators in
        /// Monte-Carlo tree search, CG2006, Appendix A.1,
        /// http://remi.coulom.free.fr/CG2006/
		/// </summary>
		public bool IsCompletelySurrounded(int p)
		{
			Debug.Assert(this.Board[p] == GoBoard.Empty);
			if (this.numNeighborsEmpty[p] != 0)
				return false;
			if (this.numNeighbors[GoBoard.Black][p] != 0 &&
				this.numNeighbors[GoBoard.White][p] != 0)
				return false;
			Block b;
			if ((b = this.blocks[p - NS]) != null && b.Liberties.Count == 1)
				return false;
			if ((b = this.blocks[p + NS]) != null && b.Liberties.Count == 1)
				return false;
			if ((b = this.blocks[p - 1]) != null && b.Liberties.Count == 1)
				return false;
			if ((b = this.blocks[p + 1]) != null && b.Liberties.Count == 1)
				return false;
			return true;
		}

		private void DebugVerify()
		{
			return;

			for (int y = 0; y < this.Size; y++)
			{
				for (int x = 0; x < this.Size; x++)
				{
					int p = GoBoard.GeneratePoint(x, y);
					Debug.Assert(this.Board[p] != GoBoard.Border);

					if (this.Board[p] != GoBoard.Empty)
					{
						Debug.Assert(this.blocks[p] != null);

						Debug.Assert(this.blocks[p].Stones.Contains(p));
						Debug.Assert(this.blocks[p].Liberties.Count != 0);
					}
					else
					{
						Debug.Assert(this.blocks[p] == null);
					}
				}
			}
		}

		private void UpdateBlocksAfterAddStone(int p, byte c, List<Block> adjBlocks)
		{
			if (adjBlocks.Count == 0)
			{
				this.CreateSingleStoneBlock(p, c);
			}
			else if (adjBlocks.Count == 1)
			{
				this.AddStoneToBlock(p, adjBlocks[0]);
			}
			else
			{
				this.MergeBlocks(p, adjBlocks);
			}
		}

		private void MergeBlocks(int p, List<Block> adjBlocks)
		{
			Debug.Assert(this.Board[p] == adjBlocks[0].Color);
			Debug.Assert(this.numNeighbors[adjBlocks[0].Color][p] > 1);

			Block largestBlock = adjBlocks[0];
			int largestBlockStones = adjBlocks[0].Stones.Count;

			for (int i = 1; i < adjBlocks.Count; i++)
			{
				if (adjBlocks[i].Stones.Count > largestBlockStones)
				{
					largestBlockStones = adjBlocks[i].Stones.Count;
					largestBlock = adjBlocks[i];
				}
			}

			largestBlock.Stones.Add(p);
			
			this.marker.Clear();
			for (int i = 0; i < largestBlock.Liberties.Count; i++)
				this.marker.Include(largestBlock.Liberties[i]);

			for (int i = 0; i < adjBlocks.Count; i++)
			{
				Block b = adjBlocks[i];

				if (b == largestBlock)
					continue;

				for (int j = 0; j < b.Stones.Count; j++)
				{
					largestBlock.Stones.Add(b.Stones[j]);
					this.blocks[b.Stones[j]] = largestBlock;
				}

				for (int j = 0; j < b.Liberties.Count; j++)
				{
					if (this.marker.NewMark(b.Liberties[j]))
					{
						largestBlock.Liberties.Add(b.Liberties[j]);
					}
				}
			}

			this.blocks[p] = largestBlock;
			if (this.Board[p - 1] == Empty && this.marker.NewMark(p - 1))
				largestBlock.Liberties.Add(p - 1);
			if (this.Board[p + 1] == Empty && this.marker.NewMark(p + 1))
				largestBlock.Liberties.Add(p + 1);
			if (this.Board[p - NS] == Empty && this.marker.NewMark(p - NS))
				largestBlock.Liberties.Add(p - NS);
			if (this.Board[p + NS] == Empty && this.marker.NewMark(p + NS))
				largestBlock.Liberties.Add(p + NS);
		}

		private void AddStoneToBlock(int p, Block b)
		{
			Debug.Assert(this.Board[p] == b.Color);

			b.Stones.Add(p);
			if (this.Board[p - 1] == Empty && !this.IsAdjacentTo(p - 1, b))
				b.Liberties.Add(p - 1);
			if (this.Board[p + 1] == Empty && !this.IsAdjacentTo(p + 1, b))
				b.Liberties.Add(p + 1);
			if (this.Board[p - NS] == Empty && !this.IsAdjacentTo(p - NS, b))
				b.Liberties.Add(p - NS);
			if (this.Board[p + NS] == Empty && !this.IsAdjacentTo(p + NS, b))
				b.Liberties.Add(p + NS);
			this.blocks[p] = b;
		}

		private bool IsAdjacentTo(int p, Block b)
		{
			return this.blocks[p - NS] == b ||
				   this.blocks[p + NS] == b ||
				   this.blocks[p - 1] == b ||
				   this.blocks[p + 1] == b;
		}

		private void CreateSingleStoneBlock(int p, byte c)
		{
			Debug.Assert(this.Board[p] == c);
			Debug.Assert(this.numNeighbors[c][p] == 0);

			Block b = this.blockCache[p];
			b.InitSingleStoneBlock(p, c);
			if (this.Board[p - 1] == Empty)
				b.Liberties.Add(p - 1);
			if (this.Board[p + 1] == Empty)
				b.Liberties.Add(p + 1);
			if (this.Board[p - NS] == Empty)
				b.Liberties.Add(p - NS);
			if (this.Board[p + NS] == Empty)
				b.Liberties.Add(p + NS);

			this.blocks[p] = b;
		}

		// Remove liberties from adjacent blocks, and kill opponent blocks w/o liberties
		// Perf. optimization - compute adjacent blocks of own color
		private void RemoveLibertiesAndKill(int p, byte opp, List<Block> ownAdjacentBlocks)
		{
			this.marker.Clear();
			Block b;
			if ((b = this.blocks[p - NS]) != null)
			{
				this.marker.Include(b.Anchor);
				b.Liberties.Exclude(p);
				if (b.Color == opp)
				{
					if (b.Liberties.Count == 0)
					{
						this.KillBlock(b);
					}
				}
				else
				{
					ownAdjacentBlocks.Add(b);
				}
			}

			if ((b = this.blocks[p - 1]) != null && this.marker.NewMark(b.Anchor))
			{
				b.Liberties.Exclude(p);
				if (b.Color == opp)
				{
					if (b.Liberties.Count == 0)
					{
						this.KillBlock(b);
					}
				}
				else
				{
					ownAdjacentBlocks.Add(b);
				}
			}

			if ((b = this.blocks[p + 1]) != null && this.marker.NewMark(b.Anchor))
			{
				b.Liberties.Exclude(p);
				if (b.Color == opp)
				{
					if (b.Liberties.Count == 0)
					{
						this.KillBlock(b);
					}
				}
				else
				{
					ownAdjacentBlocks.Add(b);
				}
			}

			if ((b = this.blocks[p + NS]) != null && this.marker.NewMark(b.Anchor))
			{
				b.Liberties.Exclude(p);
				if (b.Color == opp)
				{
					if (b.Liberties.Count == 0)
					{
						this.KillBlock(b);
					}
				}
				else
				{
					ownAdjacentBlocks.Add(b);
				}
			}
		}

		private void KillBlock(Block b)
		{
			byte c = b.Color;
			byte opp = OppColor(c);

			int[] nn = this.numNeighbors[c];
			for (int i = 0; i < b.Stones.Count; i++)
			{
				int p = b.Stones[i];
				this.AddLibToAdjBlocks(p, opp);

				this.Board[p] = Empty;
				++this.numNeighborsEmpty[p - 1];
				++this.numNeighborsEmpty[p + 1];
				++this.numNeighborsEmpty[p - NS];
				++this.numNeighborsEmpty[p + NS];
				--nn[p - 1];
				--nn[p + 1];
				--nn[p - NS];
				--nn[p + NS];
				this.CapturedStones.Add(p);
				this.blocks[p] = null;
			}
			int numStones = b.Stones.Count;
			this.Prisoners[c] += numStones;
			if (numStones == 1)
			{
				this.KoPoint = b.Anchor;
			}
		}

		private void AddLibToAdjBlocks(int p, byte c)
		{
			if (this.numNeighbors[c][p] == 0)
			{
				return;
			}

			this.marker2.Clear();
			Block b;
			if (this.Board[p - NS] == c)
			{
				b = this.blocks[p - NS];
				if (b != null)
				{
					this.marker2.Include(b.Anchor);
					b.Liberties.Add(p);
				}
			}
			if (this.Board[p - 1] == c)
			{
				b = this.blocks[p - 1];
				if (b != null && this.marker2.NewMark(b.Anchor))
					b.Liberties.Add(p);
			}
			if (this.Board[p + 1] == c)
			{
				b = this.blocks[p + 1];
				if (b != null && this.marker2.NewMark(b.Anchor))
					b.Liberties.Add(p);
			}
			if (this.Board[p + NS] == c)
			{
				b = this.blocks[p + NS];
				if (b != null && !this.marker2.Contains(b.Anchor))
					b.Liberties.Add(p);
			}
		}

		private void AddStone(int p, byte c)
		{
			Debug.Assert(this.Board[p] == Empty);
			Debug.Assert(c == White || c == Black);

			this.Board[p] = c;

			--this.numNeighborsEmpty[p - 1];
			--this.numNeighborsEmpty[p + 1];
			--this.numNeighborsEmpty[p - NS];
			--this.numNeighborsEmpty[p + NS];

			int[] nn = this.numNeighbors[c];
			++nn[p - 1];
			++nn[p + 1];
			++nn[p - NS];
			++nn[p + NS];
		}

		private class Block
		{
			public int Anchor;
			public byte Color;
			public LibertyList Liberties = new LibertyList();
			public List<int> Stones = new List<int>(MaxPoints);

			public void InitSingleStoneBlock(int p, byte c)
			{
				this.Color = c;
				this.Anchor = p;
				
				this.Stones.Clear();
				this.Stones.Add(p);

				this.Liberties.Clear();
			}
		}

		private class LibertyList
		{
			private List<int> points = new List<int>();

			public int Count { get { return this.points.Count; } }
			public int this[int i] { get { return this.points[i]; } }

			public void Add(int p)
			{
				this.points.Add(p);
			}

			public void Clear()
			{
				this.points.Clear();
			}

			public void Exclude(int p)
			{
				int end = this.points.Count - 1;
				for (int i = end; i >= 0; i--)
				{
					if (this.points[i] == p)
					{
						this.points[i] = this.points[end];
						this.points.RemoveAt(end);
						return;
					}
				}
			}
		}

		private class PointMarker
		{
			private int current = 1;
			private int[] marks = new int[MaxPoints];
			
			public void Clear()
			{
				if (++this.current == 0)
				{
					this.marks = new int[MaxPoints];
					this.current = 1;
				}
			}

			public void Include(int p)
			{
				this.marks[p] = this.current;
			}

			public bool NewMark(int p)
			{
				if (this.Contains(p))
				{
					return false;
				}

				this.Include(p);
				return true;
			}

			public bool Contains(int p)
			{
				return this.marks[p] == this.current;
			}
		}
	}
}
