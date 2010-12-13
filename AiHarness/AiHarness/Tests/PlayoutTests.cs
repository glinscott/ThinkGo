using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThinkGo;
using ThinkGo.Ai;

namespace AiHarness.Tests
{
    class UctTests
    {
        public static void Run()
        {
            UctTests test = new UctTests();
            test.SelfAtariCorrectionTests();
            test.IsSimpleChainTests();

            test.SelfAtariTests();
            test.PlayoutTests();
            test.GoodMoveTests();
            test.SimpleUctTests();
            test.RaveRegressionTests();
        }

        private void RaveRegressionTests()
        {
            // RAVE loves J6 here because it's played in a bunch of winning games that start with A4.  A4 gets penalized because if it's not played first, it gets played
            // as atari defense, in a useless cause.  A4 is the only good move though.
            this.VerifyMove("A4", 1000,
                @"(;GM[1]FF[4]AP[Drago:4.11]SZ[9]CA[UTF-8]AB[ed][fd][gd][hd][de][ce][ef][ff][gf][hf][if][bf][be]AW[dd][cd][ee][fe][ge][he][ie][df][cf][ae][bd][bg]PL[W])");
        }

        private GoBoard CreateBoard(string[] board)
        {
            GoBoard result = new GoBoard(board.Length);
            result.Reset();
            for (int y = 0; y < result.Size; y++)
            {
                for (int x = 0; x < result.Size; x++)
                {
                    int p = GoBoard.GeneratePoint(x, result.Size - y - 1);
                    if (board[y][x] == 'X')
                        result.PlaceNonPlayedStone(p, GoBoard.Black);
                    else if (board[y][x] == 'O')
                        result.PlaceNonPlayedStone(p, GoBoard.White);
                }
            }
            return result;
        }

        private void SelfAtariCorrectionTests()
        {
            // 
            // 3 . . . .
            // 2 X X O .
            // 1 . O . .
            //   A B C D
            GoBoard board = new GoBoard(19);
            board.Reset();
            board.PlaceNonPlayedStone(GoBoard.GeneratePoint(0, 1), GoBoard.Black);
            board.PlaceNonPlayedStone(GoBoard.GeneratePoint(1, 1), GoBoard.Black);
            board.PlaceNonPlayedStone(GoBoard.GeneratePoint(1, 0), GoBoard.White);
            board.PlaceNonPlayedStone(GoBoard.GeneratePoint(2, 1), GoBoard.White);
            
            board.ToMove = GoBoard.White;
            Verify(board.DoSelfAtariCorrection(GoBoard.GeneratePoint(0, 0)) == GoBoard.GeneratePoint(2, 0));
            Verify(board.DoSelfAtariCorrection(GoBoard.GeneratePoint(2, 0)) == -1);

            board.ToMove = GoBoard.Black;
            Verify(board.DoSelfAtariCorrection(GoBoard.GeneratePoint(0, 0)) == -1);

            // Capture verification (no self-atari)
            // 3 O O . .
            // 2 X X O .
            // 1 . O . .
            //   A B C D
            board.Reset();
            board.PlaceNonPlayedStone(GoBoard.GeneratePoint(0, 1), GoBoard.Black);
            board.PlaceNonPlayedStone(GoBoard.GeneratePoint(1, 1), GoBoard.Black);
            board.PlaceNonPlayedStone(GoBoard.GeneratePoint(0, 2), GoBoard.White);
            board.PlaceNonPlayedStone(GoBoard.GeneratePoint(1, 0), GoBoard.White);
            board.PlaceNonPlayedStone(GoBoard.GeneratePoint(1, 2), GoBoard.White);
            board.PlaceNonPlayedStone(GoBoard.GeneratePoint(2, 1), GoBoard.White);
            board.ToMove = GoBoard.White;
            Verify(board.DoSelfAtariCorrection(GoBoard.GeneratePoint(0, 0)) == -1);

            // Single stone
            // 3 . .
            // 2 X .
            // 1 . .
            //   A B C D
            board.Reset();
            board.PlaceNonPlayedStone(GoBoard.GeneratePoint(0, 1), GoBoard.Black);
            board.ToMove = GoBoard.White;
            Verify(board.DoSelfAtariCorrection(GoBoard.GeneratePoint(0, 0)) == GoBoard.GeneratePoint(1, 0));
        }

        private void IsSimpleChainTests()
        {
            GoBoard board = this.CreateBoard(
             new string[] {
            "X...XOOX.",
            ".X..X..X.",
            "....XOOX.",
            ".........",
            "..X...X..",
            "O.OX.XO.O",
            "........O",
            "..O...OOO",
            "........."
            });

            int other;
            Verify(board.IsSimpleChain(GoBoard.GeneratePoint(0, 8), out other));
            Verify(other == GoBoard.GeneratePoint(1, 7));
            Verify(board.IsSimpleChain(GoBoard.GeneratePoint(5, 8), out other));
            Verify(other == GoBoard.GeneratePoint(5, 6));
            Verify(board.IsSimpleChain(GoBoard.GeneratePoint(6, 3), out other));
            Verify(other == GoBoard.GeneratePoint(6, 1));
            Verify(!board.IsSimpleChain(GoBoard.GeneratePoint(2, 3), out other));
        }

        private void SelfAtariTests()
        {
            this.VerifyMove("B9", 1000, @"(;FF[4]CA[UTF-8]AP[GoGui:1.1pre.SVNexported]SZ[9]
KM[6.5]DT[2008-01-02]
AB[ab][bb][cb][db][eb][ea][ae][be][ce][de][ee][ef][ff][gf][hf][if][cg][eh]
AW[da][ca][ad][bd][cd][dd][ed][fd][gc][gb][ga][gd][ge][he][hd][id]PL[B])");
            this.VerifyMove("B9", 1000, @"(;FF[4]CA[UTF-8]AP[GoGui:1.1pre.SVNexported]SZ[9]
KM[6.5]DT[2008-01-02]
AB[ab][bb][cb][db][eb][ea][ae][be][ce][de][ee][ef][ff][gf][hf][if][cg][eh]
AW[da][ca][ad][bd][cd][dd][ed][fd][gc][gb][ga][gd][ge][he][hd][id]PL[W])");
            
            this.VerifyMove("G1", 1000, @"(;FF[4]CA[UTF-8]AP[GoGui:1.2.2]SZ[9]
AB[bi][bh][ch][dh][de][dd][dc][db][da][eh][eg][ef][ea][fh][gh]
AW[ci][di][ei][ee][ed][ec][eb][fi][fg][ff][fe][fd][fc][fb][fa][gg][gb][hh][hg][ii][ih]
PL[W]KM[7.5])");

            this.VerifyMove("G1", 1000, @"(;FF[4]CA[UTF-8]AP[GoGui:1.2.2]SZ[9]
AB[bi][bh][ch][dh][de][dd][dc][db][da][eh][eg][ef][ea][fh][gh]
AW[ci][di][ei][ee][ed][ec][eb][fi][fg][ff][fe][fd][fc][fb][fa][gg][gb][hh][hg][ii][ih]
PL[B]KM[7.5])");
        }

        private void PlayoutTests()
        {
            // Capture heuristic 
            GoBoard board = this.ParseBoard(@"(;FF[4]CA[UTF-8]AP[GoGui:1.1pre1.SVN5252]SZ[9]
KM[7.5]DT[2008-02-11]
AB[ef][fe][gf]AW[ff]PL[W];W[cc]
C[Test that playout policy generates global capture move B F3])");
            PlayoutPolicy policy = new PlayoutPolicy();
            policy.Initialize(board);
            Verify(board.GetPointNotation(policy.GenerateMove()) == "F3");

            // Atari heuristic
            board = this.ParseBoard(@"(;FF[4]CA[UTF-8]AP[GoGui:1.1pre1.SVN5310:5321M]SZ[9]
KM[7.5]DT[2008-03-26]
AB[ai]PL[W];W[ah])");
            policy.Initialize(board);
            Verify(board.GetPointNotation(policy.GenerateMove()) == "B1");

            board = this.ParseBoard(@"(;FF[4]CA[UTF-8]AP[GoGui:1.1pre1.SVN5310:5321M]SZ[9]
KM[7.5]DT[2008-03-26]
AB[ah][bh][cg]AW[ai][bi][ci][ag][ch]PL[B];B[dh]
C[The liberty D1 does not escape the atari.

B3 escapes atari by capturing a block adjacent to the block that was just set atari.]
)");
            policy.Initialize(board);
            Verify(board.GetPointNotation(policy.GenerateMove()) == "B3");

            board = this.ParseBoard(@"(;FF[4]CA[UTF-8]AP[GoGui:1.1pre1.SVN5310:5321M]SZ[9]
KM[7.5]DT[2008-03-26]
AB[ah][bg][di]AW[ai][ag][bi][ci]PL[B];B[ch]
C[Should generate D2 (patterns move).

Atari heuristic should not generate B2, because it does not escape the atari.

Capture heuristic has lower priority than pattern heuristic.]
)");
            policy.Initialize(board);
            Verify(board.GetPointNotation(policy.GenerateMove()) != "B2");
        }

        private void GoodMoveTests()
        {
            // G7 creates an eye, otherwise stones are dead
            this.VerifyMove("G7", 1000,
@"(;CA[utf-8]FF[4]RU[Chinese]AP[MultiGo:4.4.4]SZ[9]GM[1]GN[189582]DT[2007-11-11]
PC[(CGOS) 9x9 Computer Go Server]PB[StoneGrid_1107_25k]BR[2109]PW[Uct-20071109]
WR[2082]KM[7.5]TM[300]RE[B+Resign]MULTIGOGM[1];B[ee]BL[300];W[dg]WL[300];B[cf]BL[289]
;W[ff]WL[300];B[gd]BL[278];W[dc]WL[300];B[ef]BL[268];W[eg]WL[291];B[ec]BL[257];W[eb]
WL[279];B[fe]BL[247];W[bc]WL[267];B[gg]BL[237];W[bg]WL[255];B[cg]BL[227];W[ch]WL[255]
;B[bh]BL[217];W[bi]WL[255];B[dh]BL[207];W[ah]WL[255];B[eh]BL[197];W[gb]WL[255];B[bf]
BL[187];W[fc]WL[244];B[hc]BL[177];W[fh]WL[235];B[fg]BL[168];W[ei]WL[235];B[df]BL[158]
;W[di]WL[235];B[cc]BL[149];W[cd]WL[235];B[cb]BL[140];W[bd]WL[235];B[ed]BL[131];W[db]
WL[235];B[hb]BL[123];W[af]WL[235];B[ae]BL[115];W[ag]WL[235];B[dd]BL[107];W[be]WL[230]
;B[ga]BL[100];W[fa]WL[230];B[bb]BL[92];W[ha]WL[224];B[ab]BL[85])");

            // J4 is the only move to save from nakade
            this.VerifyMove("J4", 1000, @"(;CA[Windows-1252]AB[ac][bc][cc][cd][gh][gd][hh][hi][dh][eh][ff][ge][hd][de][ee]
[df][cf][cg][bg]AW[bb][cb][db][dc][dd][be][bf][ce][dg][fh][hg][ih][eg][gf][he][fg]
[bd][gg][fi]AP[MultiGo:4.4.4]SZ[9]AB[ie]MULTIGOGM[1]PL[W])");
        }

        private void SimpleUctTests()
        {
            this.VerifyMove("B1", 1000,
            @"(;FF[4]CA[UTF8]AP[GoGui:0.9.x]SZ[9]KM[6.5]
AB[ah][af][ad][ab][bh][bf][be][bd][bc][bb][ba][ch][cf][cd][cb][di][dh][df][de][dd][dc][db][da][ef][ee][ed][ec][eb][ea][fc][fb][fa][gc][gb][hb][ha][ib]
AW[ag][bg][cg][dg][ei][eh][eg][fi][fh][fg][ff][fe][fd][gh][gf][gd][hi][hh][hg][hf][he][hd][hc][ih][if][id][ic]
C[Whoever plays B1, wins the game. A resonable UCT player should only generate the moves A1, B1, C1 and detect that B1 is the only win even with a very low number of simulations (<500 ?)]
PL[B])");

            // Only move to keep group alive
            this.VerifyMove("D4", 1000,
@"(;CA[utf-8]GM[1]FF[4]AP[MultiGo:4.4.4]SZ[9]RU[Chinese]GN[200058]DT[2007-11-28]
PC[(CGOS) 9x9 Computer Go Server]PB[ControlBoy]BR[1525]PW[Uct-20071123]WR[2128]
KM[7.5]TM[300]RE[B+Resign]MULTIGOGM[1];B[fd]BL[298];W[ee]WL[300];B[ed]BL[298];W[de]
WL[300];B[dd]BL[296];W[dh]WL[289];B[ce]BL[296];W[cf]WL[289];B[fe]BL[296];W[cd]WL[255]
;B[be]BL[296];W[bf]WL[255];B[df]BL[296];W[ef]WL[249];B[ff]BL[295];W[dg]WL[248];B[eg]
BL[295])");

/*            this.VerifyMove("E9", 10000,
@"(;CA[Windows-1252]SZ[9]AP[MultiGo:4.4.4]MULTIGOGM[1];B[dd];W[ec];B[ed];W[fd];B[fe]
;W[ee];B[ef];W[de];B[df];W[ce];B[gd];W[fc];B[dc];W[ff];B[ge];W[db];B[gc];W[gb];B[eb]
;W[fb];B[hb];W[hc];B[fa])");*/
        }

        private void VerifyMove(string move, int numSims, string boardString)
        {
            GoBoard board = this.ParseBoard(boardString);
            UctSearch search = new UctSearch(board);
            search.SetNumSimulations(numSims);
            search.SearchLoop();
            Verify(board.GetPointNotation(search.FindBestSequence()[0]) == move);
        }

        private GoBoard ParseBoard(string sgf)
        {
            SgfParser parser = new SgfParser(sgf);
            SgfTree tree = parser.Root;

            SgfReplay replay = new SgfReplay(tree);
            while (replay.PlayMove()) ;

            return replay.Board;
        }

        private void Verify(bool value)
        {
            if (!value) throw new ArgumentException("Failed basic test");
        }
    }
}
