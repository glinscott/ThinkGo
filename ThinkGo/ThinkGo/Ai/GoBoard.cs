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
        public static readonly int[] DirDelta8 = new int[] {  -NS - 1, -NS, -NS + 1, -1, 1, NS - 1, NS, NS + 1 };

		private int[][] numNeighbors;
		private int[] numNeighborsEmpty;
		private Block[] blocks;
		private Block[] blockCache;

		// Perf. variables, so we don't have to keep allocating them
		private PointMarker marker = new PointMarker(), marker2 = new PointMarker();
		private List<Block> adjBlocks = new List<Block>(4);
		private List<int> anchors1 = new List<int>(4), anchors2 = new List<int>(4);

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
		public float Komi = 7.5f;

		public static int GeneratePoint(int x, int y)
		{
			return (y + 1) * NS + x;
		}

		public static void GetPointXY(int p, out int x, out int y)
		{
			y = (p / NS) - 1;
			x = p % NS;
		}

        public string GetPointNotation(int point)
        {
            int x, y;
			GetPointXY(point, out x, out y);

            return "" + (char)(x + 'A') + (this.Size - y).ToString();
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

		public bool SelfAtari(int p, byte c)
		{
			Debug.Assert(this.Board[p] == Empty);

			// Enough empty neighbors, no atari
			if (this.numNeighborsEmpty[p] >= 2)
				return false;

			byte opp = OppColor(c);
			int lib = -1;
			bool hasOwnTarget = false;
			bool hasCapture = false;
			for (int i = 0; i < 4; i++)
			{
				int target = p + DirDelta[i];
				byte color = this.Board[target];
				if (color == Empty)
				{
					if (lib == -1)
						lib = target;
					else if (lib != target)
						return false;
				}
				else if (color == c)
				{
					if (this.blocks[target].Liberties.Count > 2)
						return false;
					else
					{
						// Check block liberties other than p
						LibertyList liberties = this.blocks[target].Liberties;
						for (int j = 0; j < liberties.Count; j++)
						{
							if (liberties[j] != p)
							{
								if (lib == -1)
									lib = liberties[j];
								else if (lib != liberties[j])
									return false;
							}
						}
					}
					hasOwnTarget = true;
				}
				else if (color == opp)
				{
					// Opponent stones, count as liberty if the block is in atari
					if (this.InAtari(target))
					{
						if (lib == -1)
						{
							lib = target;
							hasCapture = true;
						}
						else if (lib != target)
							return false;
					}
				}
			}

			if (lib == -1) // suicide
				return false;
			if (!hasOwnTarget && hasCapture) // ko-type capture, ok
				return false;
			if (hasOwnTarget && hasCapture)
			{
				// Check if we gained some liberties
				List<int> anchors = new List<int>(5);
				List<int> stones = this.blocks[lib].Stones;
				this.NeighborBlocks(p, c, 1, anchors);
				for (int i = 0; i < stones.Count; i++)
				{
					if (stones[i] != lib && this.IsNeighborOfSome(stones[i], anchors, c))
						return false;
				}
			}
			return true;
		}

		public bool IsNeighborOfSome(int p, List<int> anchors, byte c)
		{
			for (int i = 0; i < 4; i++)
			{
				int target = p + DirDelta[i];
				foreach (int anchor in anchors)
				{
					Block b = this.blocks[target];
					if (b != null && anchor == b.Anchor)
						return true;
				}
			}
			return false;
		}

		public bool IsSimpleChain(int block, out int other)
		{
			if (this.NumLiberties(block) < 2)
			{
				other = -1;
				return false;
			}

			Block b = this.blocks[block];
			block = b.Anchor;
			byte color = b.Color;

			int lib1 = b.Liberties[0];
			int lib2 = b.Liberties[1];

			this.anchors1.Clear();
			this.anchors2.Clear();

			this.NeighborBlocks(lib1, color, int.MaxValue, this.anchors1);
			this.NeighborBlocks(lib1, color, int.MaxValue, this.anchors2);

			foreach (int anchor in this.anchors1)
			{
				if (anchor != block && this.anchors2.Contains(anchor))
				{
					other = anchor;
					return true;
				}
			}

			other = -1;
			return false;
		}

		/// <summary>
		/// Check if point is a single point eye with one or two adjacent blocks.
		/// </summary>
		public bool IsSimpleEye(int p, byte c)
		{
			byte opp = OppColor(c);
			if (this.numNeighborsEmpty[p] > 0 || this.numNeighbors[opp][p] > 0)
				return false;

			int anchor1 = -1, anchor2 = -1;
			for (int i = 0; i < 4; i++)
			{
				int target = p + DirDelta[i];
				Block b = this.blocks[target];
				if (b != null && anchor1 != b.Anchor)
				{
					Debug.Assert(this.Board[target] == c);

					if (anchor2 != -1)
						return false;
					anchor2 = anchor1;
					anchor1 = b.Anchor;
				}
			}

			// Surrouned completely by one block
			if (anchor2 == -1)
				return true;

			List<int> foundAnchors = new List<int>(2);
			foreach (int liberty in this.blocks[anchor1].Liberties)
			{
				if (liberty == p)
					continue;

				bool isSecondSharedEye = true;
				foundAnchors.Clear();
				for (int i = 0; i < 4; i++)
				{
					int target = liberty + DirDelta[i];
					if (this.Board[target] == Border)
						continue;

					if (this.Board[target] != c)
					{
						isSecondSharedEye = false;
						break;
					}

					int targetAnchor = this.blocks[target].Anchor;
					if (anchor1 == targetAnchor || anchor2 == targetAnchor)
					{
						isSecondSharedEye = false;
						break;
					}

					if (!foundAnchors.Contains(targetAnchor))
						foundAnchors.Add(targetAnchor);
				}

				if (isSecondSharedEye && foundAnchors.Count == 2)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Check if playing at a liberty gains liberties.  Does not handle capturing moves for efficiency.
		/// This is not required, because capturing moves have a higher priority in the playout.
		/// </summary>
		public bool GainsLiberties(int anchor, int liberty)
		{
			Debug.Assert(this.Board[liberty] == Empty);
			Debug.Assert(this.GetAnchor(anchor) == anchor);
			byte color = this.Board[anchor];
			int needed = -2; // Need 2 liberties (lose 1 by playing on liberty itself)
			for (int i = 0; i < 4; i++)
			{
				int p = liberty + GoBoard.DirDelta[i];
				byte c = this.Board[p];
				if (c == Empty)
				{
					if (!this.IsLibertyOfBlock(p, anchor))
						if (++needed >= 0)
							return true;
				}
				else if (c == color)
				{
					// Merge with block
					int anchor2 = this.GetAnchor(p);
					if (anchor != anchor2)
					{
						foreach (int otherLiberty in this.GetLiberties(p))
						{
							if (!this.IsLibertyOfBlock(otherLiberty, anchor))
								if (++needed >= 0)
									return true;
						}
					}
				}
				// else - capture, not handled
			}
			return false;
		}

		public bool IsLibertyOfBlock(int p, int anchor)
		{
			Debug.Assert(this.Board[p] == Empty);
			Debug.Assert(this.Board[anchor] != Empty);
			Debug.Assert(this.GetAnchor(anchor) == anchor);

			Block b = this.blocks[anchor];
			if (this.numNeighbors[b.Color][p] == 0)
				return false;

			return this.blocks[p - NS] == b ||
				   this.blocks[p + NS] == b ||
				   this.blocks[p - 1] == b ||
				   this.blocks[p + 1] == b;
		}

		public void NeighborBlocks(int p, byte c, int maxLib, List<int> anchors)
		{
			this.marker.Clear();
			this.NeighborBlocksWorker(p, c, maxLib, anchors);
		}

		public void AdjacentBlocks(int p, int maxLib, List<int> anchors)
		{
			Debug.Assert(this.Board[p] != Empty);
			byte opp = OppColor(this.Board[p]);

			this.marker.Clear();

			foreach (int stone in this.blocks[p].Stones)
			{
				this.NeighborBlocksWorker(stone, opp, maxLib, anchors);
			}
		}

		private void NeighborBlocksWorker(int p, byte c, int maxLib, List<int> anchors)
		{
			if (this.numNeighbors[c][p] > 0)
			{
				Block b;
				if ((b = this.blocks[p - NS]) != null &&
					b.Color == c &&
					this.marker.NewMark(b.Anchor) &&
					b.Liberties.Count <= maxLib)
					anchors.Add(b.Anchor);
				if ((b = this.blocks[p + NS]) != null &&
					b.Color == c &&
					this.marker.NewMark(b.Anchor) &&
					b.Liberties.Count <= maxLib)
					anchors.Add(b.Anchor);
				if ((b = this.blocks[p - 1]) != null &&
					b.Color == c &&
					this.marker.NewMark(b.Anchor) &&
					b.Liberties.Count <= maxLib)
					anchors.Add(b.Anchor);
				if ((b = this.blocks[p + 1]) != null &&
					b.Color == c &&
					this.marker.NewMark(b.Anchor) &&
					b.Liberties.Count <= maxLib)
					anchors.Add(b.Anchor);
			}
		}

		/// <summary>
		/// The assumption here is that the game has been played out until the end.  Any groups remaining are alive, or they would
		/// have been taken over.  Any points that are surrounded by one color belong to that color, otherwise they are neutral.
		/// 
		/// Score is returned with black winning as positive numbers.
		/// </summary>
		public float ScoreSimpleEndPosition(float komi, byte[] scoreBoard)
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

                    if (scoreBoard != null)
                        scoreBoard[p] = c;
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
					int i = GoBoard.GeneratePoint(x, y);
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
			this.Komi = board.Komi;

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

        public void PlaceNonPlayedStone(int move, byte color)
        {
            int lastMove = this.LastMove;
            int secondLastMove = this.SecondLastMove;
            byte toMove = this.ToMove;

            this.ToMove = color;
            this.PlaceStone(move);

            // Restore the state to original
            this.ToMove = toMove;
            this.LastMove = lastMove;
            this.SecondLastMove = secondLastMove;
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

        public int NumDiagonals(int p, byte c)
        {
            int result = 0;
            if (this.Board[p - GoBoard.NS - 1] == c) result++;
            if (this.Board[p - GoBoard.NS + 1] == c) result++;
            if (this.Board[p + GoBoard.NS - 1] == c) result++;
            if (this.Board[p + GoBoard.NS + 1] == c) result++;
            return result;
        }

        public int NumNeighborsEmpty(int p)
        {
            return this.numNeighborsEmpty[p];
        }

		public LibertyList GetLiberties(int p)
		{
			return this.blocks[p].Liberties;
		}

		public int NumLiberties(int p)
		{
			return this.blocks[p].Liberties.Count;
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

						if (this.blocks[p].Anchor == p)
						{
							foreach (int liberty in this.blocks[p].Liberties)
							{
								Debug.Assert(this.Board[liberty] == Empty);
							}
							foreach (int stone in this.blocks[p].Stones)
							{
								Debug.Assert(this.Board[stone] == this.blocks[stone].Color);
								for (int i = 0; i < 4; i++)
								{
									if (this.Board[stone + DirDelta[i]] == Empty)
									{
										Debug.Assert(this.blocks[p].Liberties.Contains(stone + DirDelta[i]));
									}
									else if (this.Board[stone + DirDelta[i]] == this.blocks[stone].Color)
									{
										Debug.Assert(this.blocks[p].Stones.Contains(stone + DirDelta[i]));
									}
								}
							}
						}
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

	public class LibertyList : List<int>
	{
		public void Exclude(int p)
		{
			int end = this.Count - 1;
			for (int i = end; i >= 0; i--)
			{
				if (this[i] == p)
				{
					this[i] = this[end];
					this.RemoveAt(end);
					return;
				}
			}
		}
	}
}
