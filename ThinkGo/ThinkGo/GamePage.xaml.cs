﻿namespace ThinkGo
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Shell;
    using ThinkGo.Ai;
    using System.Windows.Controls.Primitives;

    public partial class GamePage : PhoneApplicationPage
    {
		private ThinkGoModel model;
		private GoGame activeGame;
		private ApplicationBarIconButton undoButton;
        private UctSearch scoreSearch;
        private bool isComputingScore;

        public GamePage()
        {
			InitializeComponent();

            VisualStateManager.GoToState(this, "ScorePopupClosed", false);

            this.model = ThinkGoModel.Instance;
			this.DataContext = this.model;

            this.model.PropertyChanged += this.OnModelPropertyChanged;

			this.undoButton = (ApplicationBarIconButton)this.ApplicationBar.Buttons[0];

            this.GoBoardControl.StartedThinking += new System.EventHandler(GoBoardControl_StartedThinking);
            this.GoBoardControl.DoneThinking += new System.EventHandler(GoBoardControl_DoneThinking);

            this.Loaded += new RoutedEventHandler(GamePage_Loaded);
		}

        void GamePage_Loaded(object sender, RoutedEventArgs e)
        {
            this.RefreshGame();
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

            if (this.model.ActiveGame != null && this.model.ActiveGame.IsGameOver)
            {
                this.ShowTerritory();
                this.ShowScoring();

                double whiteResult, blackResult;
                double.TryParse(this.scorePopup.WhiteTotal.Text, out whiteResult);
                double.TryParse(this.scorePopup.BlackTotal.Text, out blackResult);

                string winningPlayer = whiteResult > blackResult ? this.activeGame.WhitePlayer.Name : this.activeGame.BlackPlayer.Name;

                this.ScoreEstimateBar.Visibility = Visibility.Visible;
                this.ScoreEstimateText.Text = string.Format("{0} wins by {1}", winningPlayer, Math.Abs(whiteResult - blackResult));
            }
            else
            {
                this.ScoreEstimateBar.Visibility = Visibility.Collapsed;
                this.GoBoardControl.HideEstimate();
            }
        }

		private void OnActiveGamePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
            this.RefreshState();
		}

        private void UndoClicked(object sender, System.EventArgs e)
        {
            if (this.model.ActiveGame == null || !this.model.ActiveGame.CanUndo)
                return;

            this.GoBoardControl.CancelThink();
			this.model.ActiveGame.UndoMove();
            
            Sounds.PlaySound(Sounds.Undo);
        }

        private void SettingsClicked(object sender, System.EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void PassClicked(object sender, System.EventArgs e)
        {
            if (this.GoBoardControl.IsThinking)
                return;

            this.model.ActiveGame.PlayMove(GoBoard.MovePass);
        }

        private void ScoreClicked(object sender, System.EventArgs e)
        {
            if (this.GoBoardControl.IsThinking || this.isComputingScore)
                return;

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (this.isComputingScore)
                    return;

                this.isComputingScore = true;
                double estimate = this.ShowTerritory() * 100;
                GoPlayer player = this.model.ActiveGame.Board.ToMove == GoBoard.Black ? this.model.ActiveGame.BlackPlayer : this.model.ActiveGame.WhitePlayer;
                this.ScoreEstimateText.Text = string.Format("{0} has a {1}% chance of winning", player.Name, Math.Max(0, Math.Min(100, (int)(estimate))));
                this.isComputingScore = false;
            }));
        }

        private void ShowScorePopup(object sender, System.Windows.RoutedEventArgs e)
        {
            this.ShowScoring();
            GoPlayer player = this.model.ActiveGame.Board.ToMove == GoBoard.Black ? this.model.ActiveGame.BlackPlayer : this.model.ActiveGame.WhitePlayer;
        }

        private void ResignClicked(object sender, System.EventArgs e)
        {
            this.ShowTerritory();
            this.ShowScoring();
        }

        private double ShowTerritory()
        {
            this.scoreSearch = new UctSearch(this.model.ActiveGame.Board);
            double estimate = this.scoreSearch.EstimateScore();
            this.GoBoardControl.ShowEstimate(this.scoreSearch.ScoreEstimate);
            this.ScoreEstimateBar.Visibility = Visibility.Visible;
            return estimate;
        }

        private void ShowScoring()
        {
            if (this.scorePopup.Visibility == Visibility.Visible)
            {
                VisualStateManager.GoToState(this, "ScorePopupClosed", true);
            }
            else
            {
                this.scorePopup.Initialize(this.scoreSearch);
                VisualStateManager.GoToState(this, "ScorePopupOpen", true);
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (this.scorePopup.Visibility == Visibility.Visible)
            {
                VisualStateManager.GoToState(this, "ScorePopupClosed", true);
                e.Cancel = true;
            }

            this.GoBoardControl.CancelThink();

            base.OnBackKeyPress(e);
        }

        private void ScorePopupClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
        	VisualStateManager.GoToState(this, "ScorePopupClosed", true);
        }
	}
}
