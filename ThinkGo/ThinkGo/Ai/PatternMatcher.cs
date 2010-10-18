namespace ThinkGo.Ai
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System;

    /** Some hard-coded pattern matching routines to match patterns used by MoGo.
        See <a href="http://hal.inria.fr/docs/00/11/72/66/PDF/MoGoReport.pdf">
        Modification of UCT with Patterns in Monte-Carlo Go</a>.

        The move is always in the center of the pattern or at the middle edge
        point (lower line) for edge patterns. The patterns are matched for both
        colors, unless specified otherwise. Notation:
        @verbatim
        O  White            x = Black or Empty
        X = Black           o = White or Empty
        . = Empty           B = Black to Play
        ? = Don't care      W = White to Play
        @endverbatim

        Patterns for Hane. <br>
        True is returned if any pattern is matched.
        @verbatim
        X O X   X O .   X O ?   X O O
        . . .   . . .   X . .   . . .
        ? ? ?   ? . ?   ? . ?   ? . ? B
        @endverbatim

        Patterns for Cut1. <br>
        True is returned if the first pattern is matched, but not the next two.
        @verbatim
        X O ?   X O ?   X O ?
        O . ?   O . O   O . .
        ? ? ?   ? . ?   ? O ?
        @endverbatim

        Pattern for Cut2.
        @verbatim
        ? X ?
        O . O
        x x x
        @endverbatim

        Pattern for Edge. <br>
        True is returned if any pattern is matched.
        @verbatim
        X . ?   ? X ?   ? X O    ? X O    ? X O
        O . ?   o . O   ? . ? B  ? . o W  O . X W
        @endverbatim
    */
    public static class PatternMatcher
    {
        private const int power3_5 = 3 * 3 * 3 * 3 * 3;
        private const int power3_8 = 3 * 3 * 3 * 3 * 3 * 3 * 3 * 3;

        private static bool[][] table = new bool[2][];
        private static bool[][] edgeTable = new bool[2][];

        static PatternMatcher()
        {
            PatternMatcher.table[0] = new bool[power3_8];
            PatternMatcher.table[1] = new bool[power3_8];

            PatternMatcher.edgeTable[0] = new bool[power3_5];
            PatternMatcher.edgeTable[1] = new bool[power3_5];

            InitCenterPatternTable();
            InitEdgePatternTable();
        }

        public static bool MatchAny(GoBoard board, int p)
        {
            if (board.NumNeighbors(p, GoBoard.Black) == 0 &&
                board.NumNeighbors(p, GoBoard.White) == 0)
                return false;

            int x, y;
            GoBoard.GetPointXY(p, out x, out y);
            bool isEdgeX = x == 0 || x == board.Size - 1;
            bool isEdgeY = y == 0 || y == board.Size - 1;

            bool result;
            if (isEdgeX && isEdgeY)
                // Corners
                return false;
            else if (isEdgeX || isEdgeY)
            {
                // Edge
                result = PatternMatcher.edgeTable[board.ToMove][PatternMatcher.CodeOfEdgeNeighbors(board, p)];
            }
            else
            {
                result = PatternMatcher.table[board.ToMove][PatternMatcher.CodeOf8Neighbors(board, p)];
            }
            Debug.Assert(result == PatternMatcher.SlowMatchAny(board, p));
            return result;
        }

        #region Pattern Matching
        private static bool CheckHane1(GoBoard board, int p, byte c, byte opp, int cDir, int otherDir)
        {
            return board.Board[p + cDir] == c &&
                board.Board[p + cDir + otherDir] == opp &&
                board.Board[p + cDir - otherDir] == opp &&
                board.Board[p + otherDir] == GoBoard.Empty &&
                board.Board[p - otherDir] == GoBoard.Empty;
        }

        private static bool CheckCut1(GoBoard board, int p, byte c, int cDir, int otherDir)
        {
            return board.Board[p + otherDir] == c &&
                board.Board[p + cDir + otherDir] == GoBoard.OppColor(c);
        }

        private static bool CheckCut2(GoBoard board, int p, byte c, int cDir, int otherDir)
        {
            Debug.Assert(board.Board[p + cDir] == c);
            byte opp = GoBoard.OppColor(c);
            return board.Board[p - cDir] == c &&
                ((board.Board[p + otherDir] == opp &&
                  board.Board[p - otherDir + cDir] != c &&
                  board.Board[p - otherDir - cDir] != c) ||
                 (board.Board[p - otherDir] == opp &&
                  board.Board[p + otherDir + cDir] != c &&
                  board.Board[p + otherDir - cDir] != c));
        }

        private static bool MatchCut(GoBoard board, int p)
        {
            int numEmpty = board.NumNeighborsEmpty(p);
            // cut1
            byte c1 = board.Board[p + GoBoard.NS];
            if (c1 != GoBoard.Empty &&
                board.NumNeighbors(p, c1) >= 2 &&
                !(board.NumNeighbors(p, c1) == 3 && numEmpty == 1) &&
                (PatternMatcher.CheckCut1(board, p, c1, GoBoard.NS, 1) ||
                 PatternMatcher.CheckCut1(board, p, c1, GoBoard.NS, -1)))
                return true;
            byte c2 = board.Board[p - GoBoard.NS];
            if (c2 != GoBoard.Empty &&
                board.NumNeighbors(p, c2) >= 2 &&
                !(board.NumNeighbors(p, c2) == 3 && numEmpty == 1) &&
                (PatternMatcher.CheckCut1(board, p, c2, -GoBoard.NS, 1) ||
                 PatternMatcher.CheckCut1(board, p, c2, -GoBoard.NS, -1)))
                return true;

            // cut2
            if (c1 != GoBoard.Empty &&
                board.NumNeighbors(p, c1) == 2 &&
                board.NumNeighbors(p, GoBoard.OppColor(c1)) > 0 &&
                board.NumDiagonals(p, c1) <= 3 &&
                PatternMatcher.CheckCut2(board, p, c1, GoBoard.NS, 1))
                return true;
            byte c3 = board.Board[p + 1];
            if (c3 != GoBoard.Empty &&
                board.NumNeighbors(p, c3) == 2 &&
                board.NumNeighbors(p, GoBoard.OppColor(c3)) > 0 &&
                board.NumDiagonals(p, c3) <= 2 &&
                PatternMatcher.CheckCut2(board, p, c3, 1, GoBoard.NS))
                return true;
            
            return false;
        }

        private static bool MatchEdge(GoBoard board, int p, int numBlack, int numWhite)
        {
            int up = PatternMatcher.Up(board, p);
            int side = PatternMatcher.OtherDir(up);
            int numEmpty = board.NumNeighborsEmpty(p);
            byte upColor = board.Board[p + up];

            // edge1
            if (numEmpty > 0 &&
                (numBlack > 0 || numWhite > 0) &&
                upColor == GoBoard.Empty)
            {
                byte c1 = board.Board[p + side];
                if (c1 != GoBoard.Empty && board.Board[p + side + up] == GoBoard.OppColor(c1))
                    return true;
                byte c2 = board.Board[p - side];
                if (c2 != GoBoard.Empty && board.Board[p - side + up] == GoBoard.OppColor(c2))
                    return true;
            }

            // edge2
            if (upColor != GoBoard.Empty &&
                ((upColor == GoBoard.Black && numBlack == 1 && numWhite > 0) ||
                 (upColor == GoBoard.White && numWhite == 1 && numBlack > 0)))
                return true;

            byte toMove = board.ToMove;
            // edge3
            if (upColor == toMove &&
                board.NumDiagonals(p, GoBoard.OppColor(upColor)) > 0)
                return true;

            // edge4
            if (upColor == GoBoard.OppColor(toMove) &&
                board.NumNeighbors(p, upColor) <= 2 &&
                board.NumDiagonals(p, toMove) > 0)
            {
                if (board.Board[p + side + up] == toMove &&
                    board.Board[p + side] != upColor)
                    return true;
                if (board.Board[p - side + up] == toMove &&
                    board.Board[p - side] != upColor)
                    return true;
            }

            // edge5
            if (upColor == GoBoard.OppColor(toMove) &&
                board.NumNeighbors(p, upColor) == 2 &&
                board.NumNeighbors(p, toMove) == 1)
            {
                if (board.Board[p + side + up] == toMove &&
                    board.Board[p + side] == upColor)
                    return true;
                if (board.Board[p - side + up] == toMove &&
                    board.Board[p - side] == upColor)
                    return true;
            }

            return false;
        }

        private static bool MatchHane(GoBoard board, int p, int numBlack, int numWhite)
        {
            int numEmpty = board.NumNeighborsEmpty(p);
            if (numEmpty < 2 || numEmpty > 3)
                return false;
            if ((numBlack < 1 || numBlack > 2) &&
                (numWhite < 1 || numWhite > 2))
                return false;
            if (numEmpty == 2)
            {
                // hane3
                if (numBlack == 1 && numWhite == 1)
                {
                    int dirB = PatternMatcher.FindDir(board, p, GoBoard.Black);
                    int dirW = PatternMatcher.FindDir(board, p, GoBoard.White);
                    if (board.Board[p + dirB + dirW] != GoBoard.Empty)
                        return true;
                }
            }
            else if (numEmpty == 3)
            {
                // hane2 or hane4
                Debug.Assert(numBlack + numWhite == 1);
                byte col = numBlack == 1 ? GoBoard.Black : GoBoard.White;
                byte opp = GoBoard.OppColor(col);
                int dir = PatternMatcher.FindDir(board, p, col);
                int otherDir = PatternMatcher.OtherDir(dir);
                if (board.Board[p + dir + otherDir] == GoBoard.Empty &&
                    board.Board[p + dir - otherDir] == opp)
                    return true; // hane2
                if (board.Board[p + dir - otherDir] == GoBoard.Empty &&
                    board.Board[p + dir + otherDir] == opp)
                    return true; // hane2

                if (board.ToMove == opp)
                {
                    byte c1 = board.Board[p + dir + otherDir];
                    if (c1 != GoBoard.Empty)
                    {
                        byte c2 = board.Board[p + dir - otherDir];
                        if (GoBoard.OppColor(c1) == c2)
                            return true; // hane4
                    }
                }
            }

            // hane1 pattern
            int numBlackDiag = board.NumDiagonals(p, GoBoard.Black);
            if (numBlackDiag >= 2 &&
                numWhite > 0 &&
                (PatternMatcher.CheckHane1(board, p, GoBoard.White, GoBoard.Black, GoBoard.NS, 1) ||
                 PatternMatcher.CheckHane1(board, p, GoBoard.White, GoBoard.Black, -GoBoard.NS, 1) ||
                 PatternMatcher.CheckHane1(board, p, GoBoard.White, GoBoard.Black, 1, GoBoard.NS) ||
                 PatternMatcher.CheckHane1(board, p, GoBoard.White, GoBoard.Black, -1, GoBoard.NS)))
                return true;

            int numWhiteDiag = board.NumDiagonals(p, GoBoard.White);
            if (numWhiteDiag >= 2 &&
                numBlack > 0 &&
                (PatternMatcher.CheckHane1(board, p, GoBoard.Black, GoBoard.White, GoBoard.NS, 1) ||
                 PatternMatcher.CheckHane1(board, p, GoBoard.Black, GoBoard.White, -GoBoard.NS, 1) ||
                 PatternMatcher.CheckHane1(board, p, GoBoard.Black, GoBoard.White, 1, GoBoard.NS) ||
                 PatternMatcher.CheckHane1(board, p, GoBoard.Black, GoBoard.White, -1, GoBoard.NS)))
                return true;

            return false;
        }

        private static int OtherDir(int dir)
        {
            if (dir == GoBoard.NS || dir == -GoBoard.NS)
                return 1;
            Debug.Assert(dir == 1 || dir == -1);
            return GoBoard.NS;
        }

        private static int FindDir(GoBoard board, int p, byte c)
        {
            if (board.Board[p + GoBoard.NS] == c)
                return GoBoard.NS;
            if (board.Board[p - GoBoard.NS] == c)
                return -GoBoard.NS;
            if (board.Board[p + 1] == c)
                return 1;
            Debug.Assert(board.Board[p - 1] == c);
            return -1;
        }
        #endregion

        #region Pattern Initialization
        private static void InitCenterPatternTable()
        {
            GoBoard board = new GoBoard(5);

            int center = GoBoard.GeneratePoint(2, 2);
            for (int i = 0; i < power3_8; i++)
            {
                board.Reset();
                PatternMatcher.SetupCodedPosition(board, i);

                for (byte c = 0; c <= 1; c++)
                {
                    board.ToMove = c;
                    PatternMatcher.table[c][i] = PatternMatcher.SlowMatchAny(board, center);
                }
            }
        }

        private static void InitEdgePatternTable()
        {
            GoBoard board = new GoBoard(5);

            int center = GoBoard.GeneratePoint(0, 2);
            for (int i = 0; i < power3_5; i++)
            {
                board.Reset();
                PatternMatcher.SetupCodedEdgePosition(board, i);

                for (byte c = 0; c <= 1; c++)
                {
                    board.ToMove = c;
                    PatternMatcher.edgeTable[c][i] = PatternMatcher.SlowMatchAny(board, center);
                }
            }
        }

        private static bool SlowMatchAny(GoBoard board, int p)
        {
            Debug.Assert(board.Board[p] == GoBoard.Empty);
            int numBlack = board.NumNeighbors(p, GoBoard.Black);
            int numWhite = board.NumNeighbors(p, GoBoard.White);

            if (numBlack == 0 && numWhite == 0)
                return false;

            int x, y;
            GoBoard.GetPointXY(p, out x, out y);
            bool isEdgeX = x == 0 || x == board.Size - 1;
            bool isEdgeY = y == 0 || y == board.Size - 1;
            if (isEdgeX && isEdgeY)
                // Corners
                return false;
            else if (isEdgeX || isEdgeY)
                // Edge
                return PatternMatcher.MatchEdge(board, p, numBlack, numWhite);
            else
                return PatternMatcher.MatchHane(board, p, numBlack, numWhite) ||
                       PatternMatcher.MatchCut(board, p);
        }

        private static int CodeOf8Neighbors(GoBoard board, int p)
        {
            byte[] colors = board.Board;
            int code =
                ((((((colors[p - GoBoard.NS - 1] * 3 +
                colors[p - GoBoard.NS]) * 3 +
                colors[p - GoBoard.NS + 1]) * 3 +
                colors[p - 1]) * 3 +
                colors[p + 1]) * 3 +
                colors[p + GoBoard.NS - 1]) * 3 +
                colors[p + GoBoard.NS]) * 3 +
                colors[p + GoBoard.NS + 1];
            Debug.Assert(code >= 0 && code < PatternMatcher.power3_8);
            return code;
        }

        private static int CodeOfEdgeNeighbors(GoBoard board, int p)
        {
            byte[] colors = board.Board;
            int up = PatternMatcher.Up(board, p);
            int other = PatternMatcher.OtherDir(up);
            int code = (((colors[p + other] * 3 +
                       colors[p + up + other]) * 3 +
                       colors[p + up]) * 3 +
                       colors[p + up - other]) * 3 +
                       colors[p - other];
            Debug.Assert(code >= 0 && code < PatternMatcher.power3_5);
            return code;
        }

        private static void SetupCodedEdgePosition(GoBoard board, int code)
        {
            int p = GoBoard.GeneratePoint(0, 2);
            // Loop backwards, as decoding gives points in reverse order
            for (int i = 4; i >= 0; i--)
            {
                int np = p + PatternMatcher.EdgeDirection(board, p, i);
                byte c = (byte)(code % 3);
                code /= 3;
                if (c != GoBoard.Empty)
                {
                    board.ToMove = c;
                    List<int> removed = board.PlaceStone(np);
                    Debug.Assert(removed.Count == 0);
                }
            }
        }

        private static int EdgeDirection(GoBoard board, int p, int index)
        {
            int up = PatternMatcher.Up(board, p);
            int other = PatternMatcher.OtherDir(up);
            switch (index)
            {
                case 0: return other;
                case 1: return up + other;
                case 2: return up;
                case 3: return up - other;
                case 4: return -other;
            }
            throw new ArgumentException("index");
        }

        private static void SetupCodedPosition(GoBoard board, int code)
        {
            int p = GoBoard.GeneratePoint(2, 2);
            // Loop backwards, as decoding gives points in reverse order
            for (int i = 7; i >= 0; i--)
            {
                int np = p + GoBoard.DirDelta8[i];
                byte c = (byte)(code % 3);
                code /= 3;
                if (c != GoBoard.Empty)
                {
                    board.ToMove = c;
                    List<int> removed = board.PlaceStone(np);
                    Debug.Assert(removed.Count == 0);
                }
            }
        }

        private static int Up(GoBoard board, int p)
        {
            int x, y;
            GoBoard.GetPointXY(p, out x, out y);
            if (y == 0)
            {
                return GoBoard.NS;
            }
            else if (x == 0)
            {
                return 1;
            }
            else if (x == board.Size - 1)
            {
                return -1;
            }
            else if (y == board.Size - 1)
            {
                return -GoBoard.NS;
            }
            throw new ArgumentException("p");
        }
        #endregion
    }
}
