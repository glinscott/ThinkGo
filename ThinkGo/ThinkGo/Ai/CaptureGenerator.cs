namespace ThinkGo.Ai
{
    using System.Collections.Generic;
    using System.Diagnostics;

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
}
