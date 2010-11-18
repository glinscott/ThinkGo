namespace ThinkGo
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using ThinkGo.Ai;
    using System.Windows.Controls;
    using Microsoft.Phone.Controls;
    using System.Windows;

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

        public int WhiteCaptured { get; private set; }
        public int BlackCaptured { get; private set; }

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

            this.WhiteCaptured = this.Board.Prisoners[GoBoard.White];
            this.BlackCaptured = this.Board.Prisoners[GoBoard.Black];

            this.FireChanged(false);

            return deleted;
        }

        public void UndoMove()
        {
            this.Board.Reset();
            this.InitializeHandicap();

            int takeBack = 1;
            if ((this.Board.ToMove == GoBoard.White && this.BlackPlayer.IsComputer) ||
                (this.Board.ToMove == GoBoard.Black && this.WhitePlayer.IsComputer))
            {
                takeBack = 2;
            }
            int end = Math.Max(0, this.moves.Count - takeBack);
            for (int i = 0; i < end; i++)
            {
                this.Board.PlaceStone(moves[i]);
            }

            this.moves.RemoveRange(end, this.moves.Count - end);

            this.FireChanged(true);
        }

        private void FireChanged(bool boardInvalid)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("Turn"));
                this.PropertyChanged(this, new PropertyChangedEventArgs("WhiteCaptured"));
                this.PropertyChanged(this, new PropertyChangedEventArgs("BlackCaptured"));
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
