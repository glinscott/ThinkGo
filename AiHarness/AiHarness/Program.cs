using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThinkGo;
using System.Diagnostics;
using ThinkGo.Ai;

namespace AiHarness
{
	class Program
	{
        private const string testString = @"(;FF[4]CA[UTF8]AP[GoGui:0.9.x]SZ[9]KM[6.5]
AB[ah][af][ad][ab][bh][bf][be][bd][bc][bb][ba][ch][cf][cd][cb][di][dh][df][de][dd][dc][db][da][ef][ee][ed][ec][eb][ea][fc][fb][fa][gc][gb][hb][ha][ib]
AW[ag][bg][cg][dg][ei][eh][eg][fi][fh][fg][ff][fe][fd][gh][gf][gd][hi][hh][hg][hf][he][hd][hc][ih][if][id][ic]
C[Whoever plays B1, wins the game. A resonable UCT player should only generate the moves A1, B1, C1 and detect that B1 is the only win even with a very low number of simulations (<500 ?)]
PL[B])";

        static void Main(string[] args)
		{
            SgfParser parser = new SgfParser(testString);
            SgfTree tree = parser.Root;

            SgfReplay replay = new SgfReplay(tree);
            GameSimulator.PrintBoard(replay.Board);

            UctPlayer player = new UctPlayer();
            player.SetBoard(replay.Board);
            int p = player.GetMove();
            Console.WriteLine(replay.Board.GetPointNotation(p));
            
            return;

			GameSimulator simulator = new GameSimulator();
			float value = 0;
			for (int i = 0; i < 200; i++)
			{
				value += simulator.PlayGame();
				float percent = value / (i + 1);
				Console.Write("\r" + string.Format("{0:0.00}% ", (percent * 100))  + value + "/" + (i + 1) + "                            ");
			}
		}
	}

	class GameSimulator
	{
		private Player[] players = new Player[2];

		public float PlayGame()
		{
			GoBoard board = new GoBoard(9);
			board.Reset();

			this.players[GoBoard.Black] = new UctPlayer();
			this.players[GoBoard.White] = new PlayoutPlayer();
			for (int i = 0; i < 2; i++)
			{
				this.players[i].SetBoard(board);
			}

			int lastMove = GoBoard.MoveResign, lastLastMove = GoBoard.MoveResign;
			while (!(lastMove == GoBoard.MovePass && lastLastMove == GoBoard.MovePass))
			{
				lastLastMove = lastMove;
				lastMove = this.players[board.ToMove].GetMove();

				board.PlaceStone(lastMove);

//				GameSimulator.PrintBoard(board);
//				Console.ReadKey();
			 }


			float value = board.ScoreSimpleEndPosition(7.5f);
			//Console.WriteLine(value + " " + ((value > 0) ? "Black wins" : "White wins"));
			return value > 0 ? 1 : (value < 0 ? 0 : 0.5f);
		}

		public static void PrintBoard(GoBoard board)
		{
			for (int y = 0; y < board.Size; y++)
			{
				for (int x = 0; x < board.Size; x++)
				{
					int p = GoBoard.GeneratePoint(x, y);
					byte stone = board.Board[p];
					if (stone == GoBoard.White)
					{
						Console.BackgroundColor = ConsoleColor.White;
						if (board.LastMove == p)
							Console.BackgroundColor = ConsoleColor.Yellow;
						Console.Write('X');
					}
					else if (stone == GoBoard.Black)
					{
						Console.BackgroundColor = ConsoleColor.Blue;
						if (board.LastMove == p)
							Console.BackgroundColor = ConsoleColor.Yellow;
						Console.Write('O');
					}
					else
					{
						Console.BackgroundColor = ConsoleColor.Black;
						Console.Write('.');
					}
				}
				Console.BackgroundColor = ConsoleColor.Black;
				Console.WriteLine();
			}
			Console.WriteLine();
		}
	}

	public class SgfTree
	{
		private List<SgfNode> sequence = new List<SgfNode>();
		private List<SgfTree> children = new List<SgfTree>();

        public IList<SgfNode> Sequence { get { return this.sequence; } }
        public IList<SgfTree> Children { get { return this.children; } }

        public void AddNode(SgfNode node)
		{
			this.sequence.Add(node);
		}

		public void AddChild(SgfTree child)
		{
			this.children.Add(child);
		}
	}

	public class SgfNode
	{
		private Dictionary<string, List<string>> properties = new Dictionary<string,List<string>>();

        public IDictionary<string, List<string>> Properties { get { return this.properties; } }

		public void AddProperty(string name)
		{
			this.properties.Add(name, new List<string>());
		}

		public void AddPropertyValue(string name, string value)
		{
			this.properties[name].Add(value);
		}

        public IEnumerable<string> TryGetList(string name)
        {
            List<string> result;
            if (this.properties.TryGetValue(name, out result))
            {
                foreach (string s in result)
                    yield return s;
            }
            yield break;
        }
	}

    public class SgfReplayState
    {
        public SgfTree Tree { get; set; }
        public int ActiveNode { get; set; }
    }

    public class SgfReplay
    {
        private SgfTree root;
        private List<SgfReplayState> stack = new List<SgfReplayState>();

        public SgfReplay(SgfTree root)
        {
            this.root = root;
            this.Initialize(root);
            this.stack.Add(new SgfReplayState());
            
            this.stack[0].ActiveNode = 0;
            this.stack[0].Tree = root;
        }

        private void Initialize(SgfTree root)
        {
            SgfNode rootNode = root.Sequence[0];

            this.Board = new GoBoard(int.Parse(rootNode.Properties["SZ"][0]));
            this.Board.Reset();

            foreach (string position in rootNode.TryGetList("AB"))
            {
                this.Board.ToMove = GoBoard.Black;
                this.Board.PlaceStone(this.GetPoint(position));
            }
            foreach (string position in rootNode.TryGetList("AW"))
            {
                this.Board.ToMove = GoBoard.White;
                this.Board.PlaceStone(this.GetPoint(position));
            }

            this.Board.ToMove = char.ToUpper(rootNode.Properties["PL"][0][0]) == 'W' ? GoBoard.White : GoBoard.Black;
        }

        private int GetPoint(string coord)
        {
            int x = coord[0] - 'a';
            int y = coord[1] - 'a';
            return GoBoard.GeneratePoint(x, y);
        }

        public GoBoard Board { get; private set; }
    }

	public class SgfParser
	{
		private int at = 0;
		private string sgfString;

		public SgfParser(string sgfString)
		{
			this.sgfString = sgfString;
			this.Root = this.ParseTree();
		}

        public SgfTree Root { get; private set; }
        
        private SgfTree ParseTree()
		{
			if (this.Match('('))
			{
				SgfTree tree = new SgfTree();
				SgfNode child;
				while ((child = this.ParseNode()) != null)
				{
					tree.AddNode(child);
				}
				SgfTree treeChild;
				while ((treeChild = this.ParseTree()) != null)
				{
					tree.AddChild(treeChild);
				}
				this.Match(')');
				return tree;
			}
			return null;
		}

		private SgfNode ParseNode()
		{
			if (this.Match(';'))
			{
				SgfNode node = new SgfNode();
				while (this.ParseProperty(node)) ;
				return node;
			}
			return null;
		}

		private bool ParseProperty(SgfNode parent)
		{
            this.SkipWhitespace();
			if (this.IsAlpha())
			{
				string propertyName = "" + this.Char();
				this.at++;
				while (this.IsAlpha())
				{
					propertyName += this.Char();
					this.at++;
				}
				parent.AddProperty(propertyName);
				while (this.Match('['))
				{
					string propertyValue = string.Empty;
					while (!this.MatchNoMove(']'))
					{
						propertyValue += this.Char();
                        this.at++;
					}
                    this.Match(']');
					parent.AddPropertyValue(propertyName, propertyValue);
				}
				return true;
			}
			return false;
		}

		private bool IsAlpha()
		{
			return this.Char() >= 'A' && this.Char() <= 'Z';
		}

        private void SkipWhitespace()
        {
            while (this.Char() != 0 && char.IsWhiteSpace(this.Char()))
                this.at++;
        }

		private bool Match(char c)
		{
            this.SkipWhitespace();
			if (c == this.Char())
			{
				this.at++;
				return true;
			}
			return false;
		}

		private bool MatchNoMove(char c)
		{
			if (c == this.Char())
			{
				return true;
			}
			return false;
		}

		private char Char()
		{
			if (this.at >= this.sgfString.Length)
				return (char)0;
			return this.sgfString[this.at];
		}
	}
}
