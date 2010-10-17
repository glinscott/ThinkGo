namespace ThinkGo
{
    using System.ComponentModel;

    public enum MoveMarkerOption
    {
        None,
        Text1,
        Text2,
        TextAll
    }

	public class ThinkGoModel : INotifyPropertyChanged
	{
        private MoveMarkerOption moveMarkerOption = MoveMarkerOption.Text2;
        private bool showDropCursor = true;
        private bool soundEnabled = true;

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
        public MoveMarkerOption MoveMarkerOption
        {
            get { return this.moveMarkerOption; }
            set
            {
                if (this.moveMarkerOption != value)
                {
                    this.moveMarkerOption = value;
                    this.FirePropertyChanged("MoveMarkerOption");
                }
            }
        }

        public bool ShowDropCursor
        {
            get { return this.showDropCursor; }
            set
            {
                if (this.showDropCursor != value)
                {
                    this.showDropCursor = value;
                    this.FirePropertyChanged("ShowDropCursor");
                }
            }
        }

        public bool SoundEnabled
        {
            get { return this.soundEnabled; }
            set
            {
                if (this.soundEnabled != value)
                {
                    this.soundEnabled = value;
                    this.FirePropertyChanged("SoundEnabled");
                }
            }
        }

        public void NewGame(int boardSize, GoPlayer whitePlayer, GoPlayer blackPlayer)
        {
            this.ActiveGame = new GoGame(boardSize, whitePlayer, blackPlayer);
        }

		public event PropertyChangedEventHandler PropertyChanged;

        private void FirePropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
	}
}
