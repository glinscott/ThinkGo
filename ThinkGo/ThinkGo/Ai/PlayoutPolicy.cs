﻿namespace ThinkGo.Ai
{
    using System.Collections.Generic;
    using System.Diagnostics;

	public enum PlayoutMoveType
	{
		Random,
		FillBoard,
		AtariCapture,
		AtariDefense,
		LowLib,
		Pattern,
		Capture,
		Pass
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
					this.MoveType = PlayoutMoveType.AtariCapture;
                    move = this.SelectRandom();
                }
                if (move == GoBoard.MoveNull && this.GenerateAtariDefenseMove())
                {
					this.MoveType = PlayoutMoveType.AtariDefense;
					move = this.SelectRandom();
                }
                if (move == GoBoard.MoveNull && this.GenerateLowLibMove(lastMove))
                {
					this.MoveType = PlayoutMoveType.LowLib;
                    move = this.SelectRandom();
                }
                if (move == GoBoard.MoveNull && this.GeneratePatternMove(lastMove))
                {
                    this.MoveType = PlayoutMoveType.Pattern;
                    move = this.SelectRandom();
                }
            }

            if (move == GoBoard.MoveNull)
            {
				this.MoveType = PlayoutMoveType.Capture;
                this.captureGenerator.Generate(this.moves);
                move = this.SelectRandom();
            }

            if (move == GoBoard.MoveNull)
            {
				this.MoveType = PlayoutMoveType.Random;
                move = this.randomGenerator.SelectRandomMove();
            }

            if (move == GoBoard.MoveNull)
            {
                this.MoveType = PlayoutMoveType.Pass;
                move = GoBoard.MovePass;
            }
            else if (move != GoBoard.MovePass)
            {
                int newMove = this.board.DoSelfAtariCorrection(move);
                if (newMove != -1)
                {
                    move = newMove;
                }
            }

            Debug.Assert(this.board.IsLegal(move, this.board.ToMove));

            return move;
        }

		public List<int> GetEquivalentBestMoves()
		{
			List<int> result = new List<int>();
			foreach (int move in this.moves)
			{
				if (PlayoutPolicy.IsMoveGood(this.board, move))
				{
					result.Add(move);
				}
			}
			return result;
		}

		public PlayoutMoveType MoveType
		{
			get;
			private set;
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

        private bool GeneratePatternMove(int lastMove)
        {
            Debug.Assert(this.moves.Count == 0);
            Debug.Assert(lastMove >= 0);

            this.CheckPatternMove(lastMove - GoBoard.NS - 1);
            this.CheckPatternMove(lastMove - GoBoard.NS);
            this.CheckPatternMove(lastMove - GoBoard.NS + 1);
            this.CheckPatternMove(lastMove - 1);
            this.CheckPatternMove(lastMove + 1);
            this.CheckPatternMove(lastMove + GoBoard.NS - 1);
            this.CheckPatternMove(lastMove + GoBoard.NS);
            this.CheckPatternMove(lastMove + GoBoard.NS + 1);

            // Second last move
            int original = lastMove;
            lastMove = this.board.SecondLastMove;
            if (lastMove >= 0)
            {
                this.CheckPatternMove2(lastMove - GoBoard.NS - 1, original);
                this.CheckPatternMove2(lastMove - GoBoard.NS, original);
                this.CheckPatternMove2(lastMove - GoBoard.NS + 1, original);
                this.CheckPatternMove2(lastMove - 1, original);
                this.CheckPatternMove2(lastMove + 1, original);
                this.CheckPatternMove2(lastMove + GoBoard.NS - 1, original);
                this.CheckPatternMove2(lastMove + GoBoard.NS, original);
                this.CheckPatternMove2(lastMove + GoBoard.NS + 1, original);
            }

            return this.moves.Count > 0;
        }

        private void CheckPatternMove(int p)
        {
            if (p >= 0 && p < this.board.Board.Length &&
                this.board.Board[p] == GoBoard.Empty &&
                PatternMatcher.MatchAny(this.board, p) &&
                !this.board.SelfAtari(p, this.board.ToMove))
                this.moves.Add(p);
        }

        private void CheckPatternMove2(int p, int lastMove)
        {
            if (p >= 0 && p < this.board.Board.Length && 
                this.board.Board[p] == GoBoard.Empty &&
                !PlayoutPolicy.IsIn8(p, lastMove) &&
                PatternMatcher.MatchAny(this.board, p) &&
                !this.board.SelfAtari(p, this.board.ToMove))
                this.moves.Add(p);
        }

        private static bool IsIn8(int p1, int p2)
        {
            int d = p2 - p1;
            foreach (int delta in GoBoard.DirDelta8)
                if (delta == d)
                    return true;
            return false;
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
}
