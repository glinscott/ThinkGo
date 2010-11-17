using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ThinkGo.Ai;

namespace ThinkGo
{
	public partial class ScorePopup : UserControl
	{
		public ScorePopup()
		{
			// Required to initialize variables
			InitializeComponent();
		}

        public void Initialize(UctSearch endgameSearch)
        {
            int black = 0, white = 0;
            GoBoard board = endgameSearch.Board;
            for (int y = 0; y < board.Size; y++)
            {
                for (int x = 0; x < board.Size; x++)
                {
                    int p = GoBoard.GeneratePoint(x, y);

                    if (endgameSearch.ScoreEstimate[p] < 0.4)
                    {
                        white++;
                    }
                    else if (endgameSearch.ScoreEstimate[p] >= 0.6)
                    {
                        black++;
                    }
                }
            }

            this.WhiteName.Text = ThinkGoModel.Instance.ActiveGame.WhitePlayer.Name;
            this.BlackName.Text = ThinkGoModel.Instance.ActiveGame.BlackPlayer.Name;

            this.WhiteTerritory.Text = white.ToString();
            this.BlackTerritory.Text = black.ToString();
            this.WhiteKomi.Text = board.Komi > 0 ? board.Komi.ToString() : string.Empty;
            this.BlackKomi.Text = board.Komi < 0 ? board.Komi.ToString() : string.Empty;
            this.WhiteHandicap.Text = string.Empty; // TODO
            this.BlackHandicap.Text = string.Empty;
            this.WhiteTotal.Text = (white + (board.Komi > 0 ? board.Komi : 0)).ToString();
            this.BlackTotal.Text = (black - (board.Komi < 0 ? board.Komi : 0)).ToString();
        }
	}
}