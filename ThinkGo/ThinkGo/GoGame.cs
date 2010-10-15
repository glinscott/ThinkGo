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

        public GoGame(int size, GoPlayer whitePlayer, GoPlayer blackPlayer)
        {
            this.Board = new GoBoard(size);
            this.Board.Reset();

            this.WhitePlayer = whitePlayer;
            this.BlackPlayer = blackPlayer;

            this.InitializeComputer(this.WhitePlayer);
            this.InitializeComputer(this.BlackPlayer);
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

        public List<int> PlayMove(int move)
        {
            bool isWhite = this.Board.ToMove == GoBoard.White;

            List<int> deleted = this.Board.PlaceStone(move);
            this.moves.Add(move);

            if (this.Board.LastMove == GoBoard.MovePass &&
                this.Board.SecondLastMove == GoBoard.MovePass)
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
        private Player computer;

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
            return this.computer.GetMove();
        }

        public void SetBoard(GoBoard board)
        {
            this.computer.SetBoard(board);
        }
    }
}
