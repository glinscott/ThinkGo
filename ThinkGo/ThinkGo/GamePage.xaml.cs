﻿namespace ThinkGo
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Shell;
    using ThinkGo.Ai;

    public partial class GamePage : PhoneApplicationPage
    {
		private ThinkGoModel model;
		private GoGame activeGame;
		private ApplicationBarIconButton undoButton;

        public GamePage()
        {
			InitializeComponent();
		
			this.model = ThinkGoModel.Instance;
			this.DataContext = this.model;

            this.model.PropertyChanged += this.OnModelPropertyChanged;

			this.undoButton = (ApplicationBarIconButton)this.ApplicationBar.Buttons[0];

			this.RefreshGame();

            this.GoBoardControl.StartedThinking += new System.EventHandler(GoBoardControl_StartedThinking);
            this.GoBoardControl.DoneThinking += new System.EventHandler(GoBoardControl_DoneThinking);
		}

        void GoBoardControl_DoneThinking(object sender, System.EventArgs e)
        {
            this.ThinkingBar.Visibility = Visibility.Collapsed;
        }

        void GoBoardControl_StartedThinking(object sender, System.EventArgs e)
        {
            this.ThinkingBar.IsIndeterminate = false;
            this.ThinkingBar.IsIndeterminate = true;
            this.ThinkingBar.Visibility = Visibility.Visible;
        }

		private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			this.RefreshGame();
		}

		private void RefreshGame()
		{
			if (this.activeGame != null)
			{
				this.activeGame.PropertyChanged -= this.OnActiveGamePropertyChanged;
			}

			this.activeGame = this.model.ActiveGame;

			this.RefreshState();

			if (this.activeGame != null)
			{
				this.activeGame.PropertyChanged += this.OnActiveGamePropertyChanged;
			}
		}

		private void RefreshState()
		{
			this.undoButton.IsEnabled = this.model.ActiveGame != null && this.model.ActiveGame.CanUndo;
            this.ScoreEstimateBar.Visibility = Visibility.Collapsed;
            this.GoBoardControl.HideEstimate();
        }

		private void OnActiveGamePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
            if (e.PropertyName == "GameOver")
            {
                this.NavigationService.GoBack();
            }

			this.RefreshState();
		}

        private void UndoClicked(object sender, System.EventArgs e)
        {
			this.model.ActiveGame.UndoMove();
        }

        private void SettingsClicked(object sender, System.EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void PassClicked(object sender, System.EventArgs e)
        {
			// TODO - dialog for pass, resign or cancel
			this.model.ActiveGame.PlayMove(GoBoard.MovePass);
        }

        private void ScoreClicked(object sender, System.EventArgs e)
        {
            UctSearch search = new UctSearch(this.model.ActiveGame.Board);
            double estimate = search.EstimateScore();
            this.GoBoardControl.ShowEstimate(search.ScoreEstimate);
            this.ScoreEstimateBar.Visibility = Visibility.Visible;
            GoPlayer player = this.model.ActiveGame.Board.ToMove == GoBoard.Black ? this.model.ActiveGame.BlackPlayer : this.model.ActiveGame.WhitePlayer;
            this.ScoreEstimateText.Text = string.Format("{0} has a {1}.{2}% chance of winning", player.Name, (int)(estimate * 100), (int)(estimate * 1000) % 10);
        }

        private void ShowScorePopup(object sender, System.Windows.RoutedEventArgs e)
        {
        	// TODO!
        }
	}
}
