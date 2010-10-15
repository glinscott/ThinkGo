namespace ThinkGo.Ai
{
    using System;
    using System.Collections.Generic;
	using System.Diagnostics;

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
            while (numberGames < 1000)
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
            int bestValue = 0;

            current = node.FirstChild;
            while (current != null)
            {
				//Console.WriteLine(this.board.GetPointNotation(current.Move) + " " + current.MoveCount + " " + current.Mean);
                int value = current.MoveCount;
                if (bestChild == null || (value > bestValue && current.HasBeenVisited))
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
            float inverseEval = 1.0f - eval;

            for (int i = 0; i < this.nodes.Count; i++)
            {
                UctNode node = this.nodes[i];
                UctNode parent = i > 0 ? this.nodes[i - 1] : null;
                if (parent != null)
                {
                    parent.PosCount++;
                }
                node.AddGameResult(i % 2 != 0 ? eval : inverseEval);
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
					if (current.MoveCount < expandThreshold)
						break;

					this.perfLegalMoves.Clear();
                    this.GenerateLegalMoves(this.perfLegalMoves);

                    if (this.perfLegalMoves.Count == 0)
                    {
                        isTerminal = true;
                        return true;
                    }

                    this.ScoreMoves(this.perfLegalMoves);
                    this.ExpandNode(current, this.perfLegalMoves);
                    breakAfterSelect = true;
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
                return scoreModification * score * invMaxScore + 0.5f;
            }
            else if (score < -1e-6f)
            {
                return scoreModification * score * invMaxScore + 0.5f;
            }
            // Draw!
            return 0.5f;
        }

        private void ScoreMoves(List<MoveInfo> moves)
        {
			this.policy.Initialize(this.board);
			this.policy.GenerateMove();
			
			PlayoutMoveType moveType = this.policy.MoveType;
			List<int> atariMoves = new List<int>();

			bool isFillBoardRandom = moveType == PlayoutMoveType.Random || moveType == PlayoutMoveType.FillBoard;	
			bool anyHeuristic = this.FindGlobalPatternAndAtariMoves(atariMoves);

			bool isSmallBoard = this.board.Size < 15;
			if (moves.Count > 0 && moves[moves.Count - 1].Point == GoBoard.MovePass)
			{
				// Initialize pass to low value
				MoveInfo move = moves[moves.Count - 1];
				move.Value = 0.1f;
				move.Count = isSmallBoard ? 9 : 18;
				moves[moves.Count - 1] = move;
			}

			// TODO: patterns!

			if (isFillBoardRandom)
			{
				for (int i = 0; i < moves.Count - 1; i++)
				{
					MoveInfo move = moves[i];
					if (this.board.SelfAtari(move.Point, this.board.ToMove))
					{
						move.Value = 0.1f;
						move.Count = isSmallBoard ? 9 : 18;
					}
					else if (atariMoves.Contains(move.Point))
					{
						move.Value = 1.0f;
						move.Count = 3;
					}
					else
					{
						if (!anyHeuristic)
						{
							move.Value = 0.0f;
							move.Count = 0;
						}
						else
						{
							move.Value = 0.5f;
							move.Count = 3;
						}
					}
					moves[i] = move;
				}
			}
			else
			{
				List<int> policyMoves = this.policy.GetEquivalentBestMoves();

				for (int i = 0; i < moves.Count - 1; i++)
				{
					MoveInfo move = moves[i];
					if (policyMoves.Contains(move.Point))
						move.Value = 1.0f;
					if (this.board.SelfAtari(move.Point, this.board.ToMove))
						move.Value = 0.1f;
					else if (atariMoves.Contains(move.Point))
						move.Value = 0.8f;
					else
						move.Value = 0.4f;
					move.Count = isSmallBoard ? 9 : 18;
					moves[i] = move;
				}
			}

			if (this.board.LastMove >= 0)
				this.AddLocalityBonus(moves, isSmallBoard);
        }

		private void AddLocalityBonus(List<MoveInfo> moves, bool isSmallBoard)
		{
			int tx = this.board.LastMove % GoBoard.NS;
			int ty = this.board.LastMove / GoBoard.NS - 1;

			int count = isSmallBoard ? 4 : 5;

			for (int i = 0; i < moves.Count; i++)
			{
				int x, y;
				MoveInfo move = moves[i];
				if (move.Point >= 0)
				{
					GoBoard.GetPointXY(move.Point, out x, out y);
					int dist = Math.Abs(tx - x) + Math.Abs(ty - y); // TODO: use common fate graph distance here
					switch (dist)
					{
						case 1: move.Value += 0.3f; break;
						case 2: move.Value += 0.3f; break;
						case 3: move.Value += 0.2f; break;
						default: move.Value += 0.1f; break;
					}
				}
				else
					move.Value += 0.1f;

				move.Count += count;
				moves[i] = move;
			}
		}

		private bool FindGlobalPatternAndAtariMoves(List<int> atari)
		{
			bool result = false;
			for (int y = 0; y < this.board.Size; y++)
			{
				for (int x = 0; x < this.board.Size; x++)
				{
					int p = GoBoard.GeneratePoint(x, y);
					if (this.board.Board[p] != GoBoard.Empty)
						continue;

					if (UctSearch.SetsAtari(this.board, p))
					{
						atari.Add(p);
						result = true;
					}
				}
			}
			return result;
		}

		private static bool SetsAtari(GoBoard board, int p)
		{
			Debug.Assert(board.Board[p] == GoBoard.Empty);
			byte opp = GoBoard.OppColor(board.ToMove);
			if (board.NumNeighbors(p, opp) == 0)
				return false;
			for (int i = 0; i < 4; i++)
			{
				int dir = GoBoard.DirDelta[i];
				if (board.Board[p + dir] == opp && board.NumLiberties(p + dir) == 2)
					return true;
			}
			return false;
		}

		private void ExpandNode(UctNode node, List<MoveInfo> moves)
        {
            int parentCount = 0;
            UctNode current = null, firstChild = null;
            foreach (MoveInfo moveInfo in moves)
            {
				//Console.WriteLine(moveInfo.Point + " " + moveInfo.Value + " " + moveInfo.Count);
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
                value += weight * child.Mean;
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
}
