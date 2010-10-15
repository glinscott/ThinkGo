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
        private BackgroundWorker worker = new BackgroundWorker();
        private bool isThinking;
        
        private List<TextBlock> moveNumbers = new List<TextBlock>();

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

            int boardSize = this.game.Board.Size;
            
            this.pieceSize = (int)(this.ActualWidth / boardSize);
			this.edgeOffset = (int)(this.pieceSize / 2);

			this.pieces = new Rectangle[boardSize * boardSize];
			for (int y = 0; y < boardSize; y++)
			{
				for (int x = 0; x < boardSize; x++)
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

            int bottom = this.pieceSize * boardSize - edgeOffset;

			// Draw the inner lines
			for (int i = 1; i < this.game.Board.Size - 1; i++)
			{
				int position = edgeOffset + i * this.pieceSize;
				
				geometry.Figures.Add(this.DrawLine(position, edgeOffset, position, bottom));
				geometry.Figures.Add(this.DrawLine(edgeOffset, position, bottom, position));
			}

			this.BoardBackground.Data = geometry;

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
            foreach (int deletedPiece in this.game.PlayMove(move))
            {
                this.UpdatePiece(deletedPiece);
            }
            this.UpdatePiece(move);

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

            int end = this.game.Moves.Count - 1;
            for (int i = end; i >= Math.Max(0, end - numTextBlocks + 1); i--)
            {
                if (this.game.Moves[i] < 0)
                    continue;

                byte color = this.game.Board.Board[this.game.Moves[i]];
                if (color == GoBoard.Empty)
                    continue;

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
                worker.RunWorkerAsync(aiPlayer);
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			if (!this.isThinking && this.isUserDropping)
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
                    {
                        this.isThinking = false;
                        this.PlayMove((int)e.Result);
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
}