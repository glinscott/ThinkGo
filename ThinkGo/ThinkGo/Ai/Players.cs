namespace ThinkGo.Ai
{
    using System.Collections.Generic;
    using System.Diagnostics;
	using System;

    public abstract class Player
    {
        protected GoBoard board;

        public Player()
        {
        }

        public virtual void SetBoard(GoBoard board)
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

        public override void SetBoard(GoBoard board)
        {
            base.SetBoard(board);
            this.search = new UctSearch(board);
        }

        public override int GetMove()
        {
            this.search.SearchLoop();
            List<int> moves = this.search.FindBestSequence();
            return moves[0];
        }
    }

    class PlayoutPlayer : Player
    {
        private PlayoutPolicy playoutPolicy = new PlayoutPolicy();

        public override int GetMove()
        {
			List<KeyValuePair<float, int>> scores = new List<KeyValuePair<float, int>>();
            List<int> moves = this.GenerateMoves();
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

					scores.Add(new KeyValuePair<float, int>(value, moves[i]));
                }
            }

			if (scores.Count > 0)
			{
				scores.Sort(new Comparison<KeyValuePair<float, int>>((a, b) => (int)((b.Key - a.Key) * 10)));
				
				// Take the top 10
				int bestWins = -1, bestMove = 0;
				for (int i = 0; i < Math.Min(scores.Count, 10); i++)
				{
					clone.Initialize(this.board);
					clone.PlaceStone(scores[i].Value);
					policy.Initialize(clone);
					int wins = 0;
					for (int j = 0; j < 1000; j++) if (Playout(clone, policy) > 0) wins++;
					if (wins > bestWins)
					{
						bestWins = wins;
						bestMove = scores[i].Value;
					}
				}
				return bestMove;
			}

            return GoBoard.MovePass;
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

        public override int GetMove()
        {
            PlayoutPolicy policy = new PlayoutPolicy();
            policy.Initialize(this.board);

            return policy.GenerateMove();
        }
    }
}
