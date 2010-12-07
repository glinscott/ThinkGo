namespace ThinkGo.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
        private Dictionary<string, List<string>> properties = new Dictionary<string, List<string>>();

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

        public string TryGetValue(string name)
        {
            List<string> result;
            if (this.properties.TryGetValue(name, out result))
            {
                if (result.Count > 0)
                    return result[0];
            }
            return null;
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

            int boardSize = 19;
            if (rootNode.TryGetList("SZ").Any())
                boardSize = int.Parse(rootNode.Properties["SZ"][0]);
            this.Board = new GoBoard(boardSize);
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

            this.Board.LastMove = GoBoard.MoveNull;
            this.Board.SecondLastMove = GoBoard.MoveNull;

            string player = rootNode.TryGetValue("PL");
            if (player != null)
            {
                this.Board.ToMove = char.ToUpper(player[0]) == 'W' ? GoBoard.White : GoBoard.Black;
            }
        }

        public bool PlayMove()
        {
            SgfReplayState state = this.stack[this.stack.Count - 1];
            state.ActiveNode++;
            if (state.ActiveNode >= state.Tree.Sequence.Count)
                return false;

            string move = state.Tree.Sequence[state.ActiveNode].TryGetValue(this.Board.ToMove == GoBoard.Black ? "B" : "W");
            if (move == null)
                return false;

            this.Board.PlaceStone(this.GetPoint(move));
            return true;
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
