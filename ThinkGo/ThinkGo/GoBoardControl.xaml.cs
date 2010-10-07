namespace ThinkGo
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Shapes;
	using System.ComponentModel;
using ThinkGo.Ai;
    using System.Windows.Threading;

	public partial class GoBoardControl : UserControl
	{
		private double pieceSize, edgeOffset;
		private GoGame game;
		private bool isUserDropping;
		private Rectangle[] pieces;
        private BackgroundWorker worker = new BackgroundWorker();

		public GoBoardControl()
		{
			// Required to initialize variables
			InitializeComponent();
			this.Loaded += new RoutedEventHandler(GoBoardControl_Loaded);

            this.worker.DoWork += this.worker_DoWork;
            this.worker.RunWorkerCompleted += this.worker_RunWorkerCompleted;
		}

		private void GoBoardControl_Loaded(object sender, RoutedEventArgs e)
		{
			this.game = (GoGame)this.DataContext;
			this.game.PropertyChanged += this.OnGamePropertyChanged;

			this.pieceSize = (int)(this.ActualWidth / this.game.Board.Size);
			this.edgeOffset = (int)(this.pieceSize / 2);

			this.pieces = new Rectangle[this.game.Board.Size * this.game.Board.Size];
			for (int y = 0; y < this.game.Board.Size; y++)
			{
				for (int x = 0; x < this.game.Board.Size; x++)
				{
					Rectangle rectangle = new Rectangle();
					rectangle.Fill = null;
					rectangle.Stroke = null;
					rectangle.Width = this.pieceSize;
					rectangle.Height = this.pieceSize;
					Canvas.SetLeft(rectangle, x * this.pieceSize);
					Canvas.SetTop(rectangle, y * this.pieceSize);
					this.PieceCanvas.Children.Add(rectangle);
					this.pieces[y * this.game.Board.Size + x] = rectangle;
				}
			}

			PathGeometry geometry = new PathGeometry();

			// Draw the inner lines
			for (int i = 1; i < this.game.Board.Size - 1; i++)
			{
				double position = edgeOffset + i * this.pieceSize;
				
				geometry.Figures.Add(this.DrawLine(position, edgeOffset, position, this.ActualHeight - edgeOffset));
				geometry.Figures.Add(this.DrawLine(edgeOffset, position, this.ActualWidth - edgeOffset, position));
			}

			this.BoardBackground.Data = geometry;

			// Draw the border
			geometry = new PathGeometry();
			geometry.Figures.Add(this.DrawLine(edgeOffset, edgeOffset, edgeOffset, this.ActualHeight - edgeOffset));
			geometry.Figures.Add(this.DrawLine(this.ActualWidth - edgeOffset, edgeOffset, this.ActualWidth - edgeOffset, this.ActualHeight - edgeOffset));
			geometry.Figures.Add(this.DrawLine(edgeOffset, edgeOffset, this.ActualWidth - edgeOffset, edgeOffset));
			geometry.Figures.Add(this.DrawLine(edgeOffset, this.ActualHeight - edgeOffset, this.ActualWidth - edgeOffset, this.ActualHeight - edgeOffset));

			this.BoardEdge.Data = geometry;

			this.RefreshBoard();

			this.CheckComputerTurn();
		}

		private void OnGamePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (string.Equals(e.PropertyName, "Board"))
			{
				this.RefreshBoard();
			}

			if (string.Equals(e.PropertyName, "Turn"))
			{
				this.CheckComputerTurn();
			}
		}

		private void RefreshBoard()
		{
			for (int y = 0; y < this.game.Board.Size; y++)
			{
				for (int x = 0; x < this.game.Board.Size; x++)
				{
					this.UpdatePiece(GoBoard.GeneratePoint(x, y));
				}
			}
		}

		private PathFigure DrawLine(double x, double y, double x2, double y2)
		{
			PathFigure figure = new PathFigure();
			figure.StartPoint = new Point(this.RoundLine(x), this.RoundLine(y));
			LineSegment lineSegment = new LineSegment();
			lineSegment.Point = new Point(this.RoundLine(x2), this.RoundLine(y2));
			figure.Segments.Add(lineSegment);
			return figure;
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			this.isUserDropping = true;
			this.CaptureMouse();
			this.DrawDropTarget(this.GetDropIndex(e.GetPosition(this)));

			base.OnMouseLeftButtonDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (this.isUserDropping)
			{
				this.DrawDropTarget(this.GetDropIndex(e.GetPosition(this)));
			}

			base.OnMouseMove(e);
		}

        private void PlayMove(int move)
        {
            foreach (int deletedPiece in this.game.PlayMove(move))
            {
                this.UpdatePiece(deletedPiece);
            }
            this.UpdatePiece(move);
        }

        private void CheckComputerTurn()
        {
            if (this.game.Board.ToMove == GoBoard.Black)
                this.CheckComputerTurn(this.game.BlackPlayer);
            else
                this.CheckComputerTurn(this.game.WhitePlayer);
        }

        private void CheckComputerTurn(GoPlayer player)
        {
            GoAIPlayer aiPlayer = player as GoAIPlayer;
            if (aiPlayer != null)
            {
                worker.RunWorkerAsync(aiPlayer);
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			if (this.isUserDropping)
			{
				int dropIndex = this.GetDropIndex(e.GetPosition(this));
				this.DropTemp.Data = null;

				if (this.game.Board.IsLegal(dropIndex, this.game.Board.ToMove))
				{
                    this.PlayMove(dropIndex);
				}

				this.ReleaseMouseCapture();
			}

			base.OnMouseLeftButtonUp(e);
		}

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Result is int)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                    this.PlayMove((int)e.Result)));
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            GoAIPlayer player = (GoAIPlayer)e.Argument;
            e.Result = player.Computer.GetMove();
        }

        private int GetDropIndex(Point point)
		{
			int x = (int)(point.X / this.pieceSize);
			int y = (int)(point.Y / this.pieceSize);

			return (y + 1) * GoBoard.NS + x;
		}

		private void DrawDropTarget(int dropIndex)
		{
			int x = dropIndex % GoBoard.NS;
			int y = (dropIndex / GoBoard.NS) - 1;

			PathGeometry geometry = new PathGeometry();
			geometry.Figures.Add(this.DrawLine(this.edgeOffset, y * this.pieceSize + this.edgeOffset, this.ActualWidth - this.edgeOffset, y * this.pieceSize + this.edgeOffset)); 
			geometry.Figures.Add(this.DrawLine(x * this.pieceSize + this.edgeOffset, this.edgeOffset, x * this.pieceSize + this.edgeOffset, this.ActualHeight - this.edgeOffset));
			this.DropTemp.Data = geometry;
		}

		private double RoundLine(double v)
		{
			return Math.Floor(v) + 0.5;
		}

		private void UpdatePiece(int index)
		{
			if (index < 0)
			{
				return;
			}

			int x = index % GoBoard.NS;
			int y = (index / GoBoard.NS) - 1;
			int guiIndex = y * this.game.Board.Size + x;

			if (this.game.Board.Board[index] == GoBoard.Empty)
			{
				this.pieces[guiIndex].Fill = null;
			}
			else
			{
				string resourceName = this.game.Board.Board[index] == GoBoard.Black ? "BlackPiece" : "WhitePiece";
				this.pieces[guiIndex].Fill = (ImageBrush)Application.Current.Resources[resourceName];
			}
		}
	}

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
                aiPlayer.Computer.SetBoard(this.Board);
            }
        }

		public IEnumerable<int> Moves
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

			this.WhiteCaptured = this.Board.Prisoners[GoBoard.White];
			this.BlackCaptured = this.Board.Prisoners[GoBoard.Black];

			this.FireChanged(false);

			return deleted;
		}

		public void UndoMove()
		{
			this.Board.Reset();

			int takeBack = 1;
			if ((this.Board.ToMove == GoBoard.Black && this.BlackPlayer.IsComputer) ||
				(this.Board.ToMove == GoBoard.White && this.WhitePlayer.IsComputer))
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

		public GoAIPlayer() : base("Computer")
		{
            this.computer = new PlayoutPlayer();
		}

		public override bool IsComputer { get { return true; } }
        public Player Computer { get { return this.computer; } }
	}
}