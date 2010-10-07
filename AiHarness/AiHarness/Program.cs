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
		static void Main(string[] args)
		{
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

		public void AddProperty(string name)
		{
			this.properties.Add(name, new List<string>());
		}

		public void AddPropertyValue(string name, string value)
		{
			this.properties[name].Add(value);
		}
	}

	public class SgfParser
	{
		private int at = 0;
		private string sgfString;
		private SgfTree root;

		public SgfParser(string sgfString)
		{
			this.sgfString = sgfString;
			this.root = this.ParseTree();
		}

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
					}
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

		private bool Match(char c)
		{
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
