namespace ThinkGo
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using ThinkGo.Ai;

    public class GoGame : INotifyPropertyChanged
    {
        private List<int> moves = new List<int>();
        private int handicap;

        public GoGame(int size, GoPlayer whitePlayer, GoPlayer blackPlayer, int handicap, float komi)
        {
            this.Board = new GoBoard(size);
            this.Board.Reset();
            this.Board.Komi = komi;

            this.handicap = handicap;
            this.InitializeHandicap();

            this.WhitePlayer = whitePlayer;
            this.BlackPlayer = blackPlayer;

            this.InitializeComputer(this.WhitePlayer);
            this.InitializeComputer(this.BlackPlayer);
        }

        private void InitializeHandicap()
        {
            int small = this.Board.Size < 13 ? 2 : 3;
            int large = this.Board.Size < 13 ? this.Board.Size - 3 : this.Board.Size - 4;
            int center = this.Board.Size / 2;

            if (this.handicap >= 1)
            {
                this.Board.ToMove = GoBoard.White;
                this.Board.PlaceNonPlayedStone(GoBoard.GeneratePoint(small, small), GoBoard.Black);
            }
            if (this.handicap >= 2)
                this.Board.PlaceNonPlayedStone(GoBoard.GeneratePoint(large, large), GoBoard.Black);
            if (this.handicap >= 3)
                this.Board.PlaceNonPlayedStone(GoBoard.GeneratePoint(small, large), GoBoard.Black);
            if (this.handicap >= 4)
                this.Board.PlaceNonPlayedStone(GoBoard.GeneratePoint(large, small), GoBoard.Black);
            if (this.handicap == 5)
                this.Board.PlaceNonPlayedStone(GoBoard.GeneratePoint(center, center), GoBoard.Black);
            if (this.handicap >= 6)
            {
                this.Board.PlaceNonPlayedStone(GoBoard.GeneratePoint(center, large), GoBoard.Black);
                this.Board.PlaceNonPlayedStone(GoBoard.GeneratePoint(center, small), GoBoard.Black);
            }
            if (this.handicap >= 7)
                this.Board.PlaceNonPlayedStone(GoBoard.GeneratePoint(center, center), GoBoard.Black);
            if (this.handicap >= 8)
                this.Board.PlaceNonPlayedStone(GoBoard.GeneratePoint(large, center), GoBoard.Black);
            if (this.handicap >= 9)
                this.Board.PlaceNonPlayedStone(GoBoard.GeneratePoint(small, center), GoBoard.Black);
        }

        private void InitializeComputer(GoPlayer player)
        {
            GoAIPlayer aiPlayer = player as GoAIPlayer;
            if (aiPlayer != null)
            {
                aiPlayer.SetBoard(this.Board);
            }
        }

        public IList<int> Moves
        {
            get { return this.moves; }
        }

        public GoPlayer WhitePlayer { get; private set; }
        public GoPlayer BlackPlayer { get; private set; }
        public GoBoard Board { get; private set; }

        public bool CanUndo
        {
            get { return this.moves.Count > 0; }
        }

        public bool IsGameOver
        {
            get
            {
                return
                    this.Board.LastMove == GoBoard.MovePass &&
                    this.Board.SecondLastMove == GoBoard.MovePass;
            }
        }

        public void Serialize(SimplePropertyWriter writer)
        {
            writer.Write("Handicap", this.handicap.ToString());
            writer.Write("Komi", this.Board.Komi.ToString());
            writer.Write("Size", this.Board.Size.ToString());
            writer.Write("MoveCount", this.moves.Count.ToString());
            writer.Write("ToMove", this.Board.ToMove.ToString());
            this.WhitePlayer.Serialize(writer, "White");
            this.BlackPlayer.Serialize(writer, "Black");
            for (int i = 0; i < this.moves.Count; i++)
            {
                writer.Write("Move" + i, this.moves[i].ToString());
            }
        }

        public static GoGame DeSerialize(SimplePropertyReader reader)
        {
            int handicap = int.Parse(reader.GetValue("Handicap"));
            float komi = float.Parse(reader.GetValue("Komi"));
            int size = int.Parse(reader.GetValue("Size"));
            GoPlayer whitePlayer = GoPlayer.DeSerialize(reader, "White");
            GoPlayer blackPlayer = GoPlayer.DeSerialize(reader, "Black");

            GoGame game = new GoGame(size, whitePlayer, blackPlayer, handicap, komi);

            byte toMove = game.Board.ToMove;
            game.Board.ToMove = byte.Parse(reader.GetValue("ToMove"));

            int moveCount = int.Parse(reader.GetValue("MoveCount"));
            game.moves = new List<int>(moveCount);
            for (int i = 0; i < moveCount; i++)
            {
                game.moves.Add(int.Parse(reader.GetValue("Move" + i)));
                game.Board.PlaceNonPlayedStone(game.moves[game.moves.Count - 1], toMove);
                toMove = toMove == GoBoard.White ? GoBoard.Black : GoBoard.White;
            }

            return game;
        }

        public List<int> PlayMove(int move)
        {
            bool isWhite = this.Board.ToMove == GoBoard.White;

            List<int> deleted = this.Board.PlaceStone(move);
            this.moves.Add(move);

            if (this.IsGameOver)
            {
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("GameOver"));
                }
                return deleted;
            }

            this.FireChanged(false);

            return deleted;
        }

        public void UndoMove()
        {
            int takeBack = 1;
            if ((this.Board.ToMove == GoBoard.White && this.BlackPlayer.IsComputer) ||
                (this.Board.ToMove == GoBoard.Black && this.WhitePlayer.IsComputer))
            {
                takeBack = 2;
            }
            int end = Math.Max(0, this.moves.Count - takeBack);

            // Restore to original state
            this.Board.Reset();
            this.InitializeHandicap();

            for (int i = 0; i < end; i++)
            {
                this.Board.PlaceStone(this.moves[i]);
            }

            this.moves.RemoveRange(end, this.moves.Count - end);

            this.FireChanged(true);
        }

        private void FireChanged(bool boardInvalid)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("Turn"));
                if (boardInvalid)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Board"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class GoPlayer
    {
        public GoPlayer(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }
        public virtual bool IsComputer { get { return false; } }

        public virtual void Serialize(SimplePropertyWriter writer, string prefix)
        {
            writer.Write(prefix + "Name", this.Name);
            writer.Write(prefix + "Type", this.IsComputer ? "1" : "0");
        }

        public static GoPlayer DeSerialize(SimplePropertyReader reader, string prefix)
        {
            int type = int.Parse(reader.GetValue(prefix + "Type"));
            if (type == 0)
            {
                return new GoPlayer(reader.GetValue(prefix + "Name"));
            }

            GoAIPlayer player = new GoAIPlayer();
            player.TimeSetting = TimeSetting.TimeSettings[int.Parse(reader.GetValue(prefix + "Time"))];
            return player;
        }
    }

    public class GoAIPlayer : GoPlayer
    {
        private UctPlayer computer;

        public GoAIPlayer()
            : base("ThinkGo")
        {
            this.computer = new UctPlayer();
            this.TimeSetting = TimeSetting.TimeSettings[3];
        }

        public override bool IsComputer { get { return true; } }
        public TimeSetting TimeSetting { get; set; }

        public override void Serialize(SimplePropertyWriter writer, string prefix)
        {
            base.Serialize(writer, prefix);
            writer.Write(prefix + "Time", TimeSetting.TimeSettings.IndexOf(this.TimeSetting).ToString());
        }

        public void CancelSearch()
        {
            this.computer.CancelSearch();
        }

        public int GetMove()
        {
            this.computer.SetMillisecondsToSearch(this.TimeSetting.Milliseconds);
            return this.computer.GetMove();
        }

        public void SetBoard(GoBoard board)
        {
            this.computer.SetBoard(board);
        }
    }
}
