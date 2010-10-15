namespace ThinkGo
{
    using System.ComponentModel;

    public enum MoveMarkerOption
    {
        None,
        Dot,
        Text1,
        Text2,
        TextAll
    }

	public class ThinkGoModel : INotifyPropertyChanged
	{
        private MoveMarkerOption moveMarkerOption = MoveMarkerOption.Text2;

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
