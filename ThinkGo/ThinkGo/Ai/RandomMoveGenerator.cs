namespace ThinkGo.Ai
{
    using System.Collections.Generic;

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
}
