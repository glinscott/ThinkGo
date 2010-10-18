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
        private byte ourColor;
        private int numSimulations = 1000;

        private float biasTermConstant = 0.7f;
        private float raveWeightInitial = 1.0f;
        private float raveWeightFinal = 5000.0f;
        private float raveWeightParam1, raveWeightParam2;
        private float firstPlayUrgency = 10000.0f;

        // Perf. variables
        private List<MoveInfo> perfLegalMoves = new List<MoveInfo>(100);
        private int[] perfFirstPlay = new int[512], perfFirstPlayOpp = new int[512];

        public UctSearch(GoBoard board)
        {
            this.rootBoard = board;
            this.board = new GoBoard(this.rootBoard.Size);

            this.raveWeightParam1 = 1.0f / this.raveWeightInitial;
            this.raveWeightParam2 = 1.0f / this.raveWeightFinal;
        }

        public int MaxGameLength
        {
            get { return 3 * this.board.Size * this.board.Size; }
        }

        public void SetNumSimulations(int numSimulations)
        {
            this.numSimulations = numSimulations;
        }

        public void SearchLoop()
        {
            this.ourColor = this.rootBoard.ToMove;
            this.rootNode = new UctNode(new MoveInfo(GoBoard.MoveNull));

            int numberGames = 0;
            while (numberGames < this.numSimulations)
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
#if NO
                if (this.hackIsroot)
                {
                    Console.WriteLine(this.board.GetPointNotation(current.Move) + " " + current.MoveCount + " " + this.GetValueEstimate(true, current));
                }
#endif
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
                bool abort = abortInTree;
                if (!abort && !isTerminal)
                    abort = !this.PlayoutGame();
                if (abort)
                    // Unknown score == 0.5
                    eval = 0.5f;
                else
                    eval = this.Evaluate();

                if (this.board.ToMove != this.ourColor)
                    eval = 1 - eval;
            }

            this.UpdateTree(eval);
            this.UpdateRaveValues(eval);

#if NO
            if (this.sequence[0] == 26)
            {
                if (eval > 0.5)
                {
                    Console.WriteLine(eval);
                }
            }
                // move = 26
                UctNode child = this.rootNode.FirstChild;
                UctNode badMove = null, goodMove = null;
                while (child != null)
                {
                    if (child.Move == 26)
                        badMove = child;
                    else if (child.Move == 24)
                        goodMove = child;
                    child = child.Next;
                }
                if (goodMove != null)
                {
                    Console.WriteLine(goodMove.RaveValue + " " + goodMove.RaveCount);
                }
#endif

            // Take back moves 
            this.sequence.Clear();
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
                // Counter-intuitive, but node[0] == root node w/ no move, not our first move
                node.AddGameResult(i % 2 != 0 ? eval : inverseEval);
            }
        }

        private void UpdateRaveValues(float eval)
        {
            if (this.sequence.Count == 0)
                return;

            int numNodes = this.nodes.Count;
            bool opp = this.board.ToMove == this.ourColor;

            for (int i = 0 ; i < this.board.Board.Length; i++)
            {
                this.perfFirstPlay[i] = int.MaxValue;
                this.perfFirstPlayOpp[i] = int.MaxValue;
            }

            for (int i = this.sequence.Count - 1; i >= 0; i--)
            {
                int move = this.sequence[i];
                if (move >= 0)
                {
                    if (opp)
                    {
                        this.perfFirstPlayOpp[move] = Math.Min(this.perfFirstPlayOpp[move], i);
                        if (i < numNodes)
                        {
                            this.UpdateRaveValues(1.0f - eval, i, this.perfFirstPlayOpp);
                        }
                    }
                    else
                    {
                        this.perfFirstPlay[move] = Math.Min(this.perfFirstPlay[move], i);
                        if (i < numNodes)
                        {
                            this.UpdateRaveValues(eval, i, this.perfFirstPlay);
                        }
                    }
                }
                opp = !opp;
            }
        }

        private void UpdateRaveValues(float eval, int i, int[] firstPlay)
        {
            UctNode node = this.nodes[i];
            if (node.NumChildren == 0)
                return;

            int length = this.sequence.Count;
            UctNode child = node.FirstChild;
            while (child != null)
            {
                int move = child.Move;
                if (move < 0)
                {
                    child = child.Next;
                    continue;
                }
                int first = firstPlay[move];
                if (first == int.MaxValue)
                {
                    child = child.Next;
                    continue;
                }

                float weight = 2.0f - (float)(first - i) / (length - i);
                child.AddRaveValue(eval, weight);
                child = child.Next;
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
            float score = this.board.ScoreSimpleEndPosition(board.Komi);

            // Score is always in terms of black, but we want to return score relative to player to move.
            if (board.ToMove == GoBoard.White)
                score = -score;

            // Score normalized to -1..1
            float invMaxScore = 1.0f / (board.Size * board.Size + board.Komi);
            float scoreModification = 0.02f;

            if (score > 1e-6f)
            {
                return 
                    1 - scoreModification
                    + scoreModification * score * invMaxScore;
            }
            else if (score < -1e-6f)
            {
                return
                    scoreModification
                    + scoreModification * score * invMaxScore;
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
            List<int> patternMoves = new List<int>();

			bool isFillBoardRandom = moveType == PlayoutMoveType.Random || moveType == PlayoutMoveType.FillBoard;	
			bool anyHeuristic = this.FindGlobalPatternAndAtariMoves(patternMoves, atariMoves);

			bool isSmallBoard = this.board.Size < 15;
			if (moves.Count > 0 && moves[moves.Count - 1].Point == GoBoard.MovePass)
			{
				// Initialize pass to low value
				MoveInfo move = moves[moves.Count - 1];
				move.Value = 0.001f;
				move.Count = isSmallBoard ? 9 : 18;
				moves[moves.Count - 1] = move;
			}

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
                    else if (patternMoves.Contains(move.Point))
                    {
                        move.Value = 0.9f;
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
                    if (this.board.SelfAtari(move.Point, this.board.ToMove))
                        move.Value = 0.1f;
                    else if (atariMoves.Contains(move.Point))
                        move.Value = 0.8f;
                    else if (patternMoves.Contains(move.Point))
                        move.Value = 0.6f;
                    else
                        move.Value = 0.4f;
                    
                    if (policyMoves.Contains(move.Point))
                        move.Value = 1.0f;

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
						case 1: move.Value += 0.6f; break;
						case 2: move.Value += 0.3f; break;
						case 3: move.Value += 0.2f; break;
						default: move.Value += 0.1f; break;
					}
                    move.Count += count;
                    moves[i] = move;
                }
			}
		}

		private bool FindGlobalPatternAndAtariMoves(List<int> pattern, List<int> atari)
		{
			bool result = false;
			for (int y = 0; y < this.board.Size; y++)
			{
				for (int x = 0; x < this.board.Size; x++)
				{
					int p = GoBoard.GeneratePoint(x, y);
					if (this.board.Board[p] != GoBoard.Empty)
						continue;

                    if (PatternMatcher.MatchAny(this.board, p))
                    {
                        pattern.Add(p);
                        result = true;
                    }

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
            bool useRave = true;

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
                Debug.Assert(child.MoveCount <= Math.Pow(Math.E, logPosCount) + 0.5);
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
