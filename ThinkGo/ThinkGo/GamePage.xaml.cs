namespace ThinkGo
{
	using Microsoft.Phone.Controls;
	using Microsoft.Phone.Shell;
	using System.ComponentModel;

	public class ThinkGoModel : INotifyPropertyChanged
	{
		public ThinkGoModel()
		{
		}

        private static ThinkGoModel instance = null;
        public static ThinkGoModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ThinkGoModel();
                }
                return instance;
            }
        }

		public GoGame ActiveGame { get; private set; }

        public void NewGame(int boardSize, GoPlayer whitePlayer, GoPlayer blackPlayer)
        {
            this.ActiveGame = new GoGame(boardSize, whitePlayer, blackPlayer);
        }

		public event PropertyChangedEventHandler PropertyChanged;
	}

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
		}

		private void OnActiveGamePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			this.RefreshState();
		}

        private void UndoClicked(object sender, System.EventArgs e)
        {
			this.model.ActiveGame.UndoMove();
        }

        private void SettingsClicked(object sender, System.EventArgs e)
        {
        	// TODO - popup settings!
        }

        private void PassClicked(object sender, System.EventArgs e)
        {
			// TODO - dialog for pass, resign or cancel
			this.model.ActiveGame.PlayMove(GoBoard.MovePass);
        }
    }
}
