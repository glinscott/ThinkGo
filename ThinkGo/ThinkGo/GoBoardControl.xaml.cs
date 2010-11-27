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
		private int pieceSize, edgeOffset;
		private GoGame game;
		private bool isUserDropping;
		private Rectangle[] pieces;
        private Rectangle[] estimates;
        private BackgroundWorker worker = new BackgroundWorker();
        private bool isThinking;
        
        private List<TextBlock> moveNumbers = new List<TextBlock>();

        public event EventHandler StartedThinking;
        public event EventHandler DoneThinking;

		public GoBoardControl()
		{
			// Required to initialize variables
			InitializeComponent();
			this.Loaded += new RoutedEventHandler(GoBoardControl_Loaded);
            this.Unloaded += new RoutedEventHandler(GoBoardControl_Unloaded);

            this.worker.DoWork += this.worker_DoWork;
            this.worker.RunWorkerCompleted += this.worker_RunWorkerCompleted;
		}

        public bool IsThinking
        {
            get { return this.isThinking; }
        }

        public void CancelThink()
        {
            if (this.isThinking)
            {
                this.worker.CancelAsync();
                this.isThinking = false;

                if (this.DoneThinking != null)
                {
                    this.DoneThinking(this, EventArgs.Empty);
                }
            }
        }

        public void ShowEstimate(double[] estimate)
        {
            int boardSize = this.game.Board.Size;

            if (this.estimates == null)
            {
                this.estimates = new Rectangle[boardSize * boardSize];
                this.CreateRectangles(this.ScoreEstimateCanvas, this.estimates, 0.5);
            }

            SolidColorBrush white = new SolidColorBrush(Colors.White);
            SolidColorBrush black = new SolidColorBrush(Colors.Black);

            for (int y = 0; y < this.game.Board.Size; y++)
            {
                for (int x = 0; x < this.game.Board.Size; x++)
                {
                    int p = GoBoard.GeneratePoint(x, y);
                    int guiIndex = y * this.game.Board.Size + x;

                    if (estimate[p] < 0.4)
                    {
                        this.estimates[guiIndex].Fill = white;
                    }
                    else if (estimate[p] < 0.6)
                    {
                        this.estimates[guiIndex].Fill = null;
                    }
                    else
                    {
                        this.estimates[guiIndex].Fill = black;
                    }
                }
            }

            this.ScoreEstimateCanvas.Visibility = Visibility.Visible;
        }

        public void HideEstimate()
        {
            this.ScoreEstimateCanvas.Visibility = Visibility.Collapsed;
        }

        void GoBoardControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.game != null)
            {
                this.game.PropertyChanged -= this.OnGamePropertyChanged; 
            }
        }

        private void CreateRectangles(Canvas parent, Rectangle[] pieces, double opacity)
        {
            int boardSize = this.game.Board.Size;
            for (int y = 0; y < boardSize; y++)
            {
                for (int x = 0; x < boardSize; x++)
                {
                    Rectangle rectangle = new Rectangle();
                    rectangle.Fill = null;
                    rectangle.Stroke = null;
                    rectangle.Width = this.pieceSize;
                    rectangle.Height = this.pieceSize;
                    rectangle.Opacity = opacity;
                    Canvas.SetLeft(rectangle, x * this.pieceSize);
                    Canvas.SetTop(rectangle, y * this.pieceSize);
                    parent.Children.Add(rectangle);
                    pieces[y * this.game.Board.Size + x] = rectangle;
                }
            }
        }

		private void GoBoardControl_Loaded(object sender, RoutedEventArgs e)
		{
            this.game = ThinkGoModel.Instance.ActiveGame;
			this.game.PropertyChanged += this.OnGamePropertyChanged;

            int boardSize = this.game.Board.Size;
            
            this.pieceSize = (int)(this.ActualWidth / boardSize);
			this.edgeOffset = (int)(this.pieceSize / 2);

            this.PieceCanvas.Children.Clear();
			this.pieces = new Rectangle[boardSize * boardSize];
            this.CreateRectangles(this.PieceCanvas, this.pieces, 1.0);

            PathGeometry geometry = new PathGeometry();
            GeometryGroup group = new GeometryGroup();
            group.Children.Add(geometry);

            int bottom = this.pieceSize * boardSize - edgeOffset;

			// Draw the inner lines
			for (int i = 1; i < this.game.Board.Size - 1; i++)
			{
				int position = edgeOffset + i * this.pieceSize;
				
				geometry.Figures.Add(this.DrawLine(position, edgeOffset, position, bottom));
				geometry.Figures.Add(this.DrawLine(edgeOffset, position, bottom, position));
			}

            int small = this.game.Board.Size < 13 ? 2 : 3;
            int large = this.game.Board.Size < 13 ? this.game.Board.Size - 3 : this.game.Board.Size - 4;
            int center = this.game.Board.Size / 2;
            
            this.DrawEllipse(group, small, small);
            this.DrawEllipse(group, small, large);
            this.DrawEllipse(group, large, small);
            this.DrawEllipse(group, large, large);
            this.DrawEllipse(group, center, center);

            if (this.game.Board.Size >= 13)
            {
                this.DrawEllipse(group, small, center);
                this.DrawEllipse(group, large, center);
                this.DrawEllipse(group, center, large);
                this.DrawEllipse(group, center, small);
            }

			this.BoardBackground.Data = group;

			// Draw the border
			geometry = new PathGeometry();
			geometry.Figures.Add(this.DrawLine(edgeOffset, edgeOffset, edgeOffset, bottom));
			geometry.Figures.Add(this.DrawLine(bottom, edgeOffset, bottom, bottom));
			geometry.Figures.Add(this.DrawLine(edgeOffset, edgeOffset, bottom, edgeOffset));
			geometry.Figures.Add(this.DrawLine(edgeOffset, bottom, bottom, bottom));

			this.BoardEdge.Data = geometry;

			this.RefreshBoard();

			this.CheckComputerTurn();
		}

        private void DrawEllipse(GeometryGroup group, int x, int y)
        {
            int xPos = this.edgeOffset + x * this.pieceSize;
            int yPos = this.edgeOffset + y * this.pieceSize;

            double radius = 5;
            EllipseGeometry ellipse = new EllipseGeometry();
            ellipse.Center = new Point(this.RoundLine(xPos), this.RoundLine(yPos));
            ellipse.RadiusX = radius;
            ellipse.RadiusY = radius;
            group.Children.Add(ellipse);
        }

		private void OnGamePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (string.Equals(e.PropertyName, "Board"))
			{
				this.RefreshBoard();
			}

			if (string.Equals(e.PropertyName, "Turn"))
			{
                this.RefreshMoveNumbers();
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

            this.RefreshMoveNumbers();
		}

        private PathFigure DrawLine(int x, int y, int x2, int y2)
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
            if (!this.isThinking)
            {
                this.isUserDropping = true;
                this.CaptureMouse();
                this.DrawDropTarget(this.GetDropIndex(e.GetPosition(this)));
            }

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
            bool wasCapture = false;
            foreach (int deletedPiece in this.game.PlayMove(move))
            {
                wasCapture = true;
                this.UpdatePiece(deletedPiece);
            }
            this.UpdatePiece(move);

            Sounds.PlaySound(wasCapture ? Sounds.Capture : Sounds.PlaceStone);

            this.RefreshMoveNumbers();
        }

        private void RefreshMoveNumbers()
        {
            foreach (TextBlock textBlock in this.moveNumbers)
            {
                this.PieceCanvas.Children.Remove(textBlock);
            }

            int numTextBlocks = 0;
            switch (ThinkGoModel.Instance.MoveMarkerOption)
            {
                case MoveMarkerOption.Text1: numTextBlocks = 1; break;
                case MoveMarkerOption.Text2: numTextBlocks = 2; break;
                case MoveMarkerOption.TextAll: numTextBlocks = 1000; break;
            }

            Dictionary<int, bool> hit = new Dictionary<int,bool>(100);
            int end = this.game.Moves.Count - 1;
            for (int i = end; i >= Math.Max(0, end - numTextBlocks + 1); i--)
            {
                if (this.game.Moves[i] < 0)
                    continue;

                byte color = this.game.Board.Board[this.game.Moves[i]];
                if (color == GoBoard.Empty)
                    continue;

                if (hit.ContainsKey(this.game.Moves[i]))
                    continue;
                hit[this.game.Moves[i]] = true;

                TextBlock textBlock = new TextBlock();
                this.moveNumbers.Add(textBlock);
                textBlock.Text = i.ToString();
                textBlock.Foreground = new SolidColorBrush(color == GoBoard.Black ? Colors.White : Colors.Black);
                textBlock.FontSize = 24;

                int x, y;
                GoBoard.GetPointXY(this.game.Moves[i], out x, out y);
                
                Canvas.SetLeft(textBlock, x * this.pieceSize + (this.pieceSize - textBlock.ActualWidth) / 2);
                Canvas.SetTop(textBlock, y * this.pieceSize + (this.pieceSize - textBlock.ActualHeight) / 2);
                this.PieceCanvas.Children.Add(textBlock);
            }
            
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
            if (aiPlayer != null && !this.isThinking)
            {
                this.isThinking = true;
                this.worker.RunWorkerAsync(aiPlayer);

                if (this.StartedThinking != null)
                {
                    this.StartedThinking(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			if (!this.isThinking && this.isUserDropping)
			{
                this.isUserDropping = false;

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
                    {
                        this.isThinking = false;
                        this.PlayMove((int)e.Result);

                        if (this.DoneThinking != null)
                        {
                            this.DoneThinking(this, EventArgs.Empty);
                        }
                    }
                ));
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            GoAIPlayer player = (GoAIPlayer)e.Argument;
            e.Result = player.GetMove();
        }

        private int GetDropIndex(Point point)
		{
			int x = (int)(point.X / this.pieceSize);
			int y = (int)(point.Y / this.pieceSize);

			return (y + 1) * GoBoard.NS + x;
		}

		private void DrawDropTarget(int dropIndex)
		{
            if (!ThinkGoModel.Instance.ShowDropCursor)
                return;

			int x = dropIndex % GoBoard.NS;
			int y = (dropIndex / GoBoard.NS) - 1;

			PathGeometry geometry = new PathGeometry();
            int boardSize = this.game.Board.Size;
			geometry.Figures.Add(this.DrawLine(this.edgeOffset, y * this.pieceSize + this.edgeOffset, boardSize * this.pieceSize - this.edgeOffset, y * this.pieceSize + this.edgeOffset)); 
			geometry.Figures.Add(this.DrawLine(x * this.pieceSize + this.edgeOffset, this.edgeOffset, x * this.pieceSize + this.edgeOffset, boardSize * this.pieceSize - this.edgeOffset));
			this.DropTemp.Data = geometry;
		}

		private double RoundLine(int v)
		{
			return v + 0.5;
		}

		private void UpdatePiece(int index)
		{
			if (index < 0)
			{
				return;
			}

			int x, y;
            GoBoard.GetPointXY(index, out x, out y);
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
}