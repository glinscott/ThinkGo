using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ThinkGo
{
	public partial class BoardHeader : UserControl
	{
		private GoGame game;

		public BoardHeader()
		{
			// Required to initialize variables
			InitializeComponent();

			this.Loaded += new RoutedEventHandler(BoardHeader_Loaded);
		}

		void BoardHeader_Loaded(object sender, RoutedEventArgs e)
		{
			this.game = (GoGame)this.DataContext;
			this.game.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(game_PropertyChanged);

			this.UpdateState();
		}

		void game_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			this.UpdateState();
		}

		private void UpdateState()
		{
			if (this.game.Board.ToMove == GoBoard.White)
			{
				this.WhiteScale.ScaleX = this.WhiteScale.ScaleY = 1;
				this.BlackScale.ScaleX = this.BlackScale.ScaleY = 0.45;
			}
			else
			{
				this.WhiteScale.ScaleX = this.WhiteScale.ScaleY = 0.45;
				this.BlackScale.ScaleX = this.BlackScale.ScaleY = 1;
			}
		}
	}
}