namespace ThinkGo
{
    using System.ComponentModel;
    using System;
    using System.IO.IsolatedStorage;
    using System.Diagnostics;
    using Microsoft.Phone.Marketplace;

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

        private float komi = 6.5f;
        private int handicap = 0;

        private bool isTrial;

        private IsolatedStorageSettings isolatedStore;

		public ThinkGoModel()
		{
            try
            {
                // Get the settings for this application.
                this.isolatedStore = IsolatedStorageSettings.ApplicationSettings;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception while using IsolatedStorageSettings: " + e.ToString());
            }

            this.RefreshFromIsolatedStore();
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
        public void Deserialize(SimplePropertyReader reader)
        {
            this.ActiveGame = GoGame.DeSerialize(reader);
        }

        public bool IsTrial
        {
            get { return this.isTrial; }
        }

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

        public float Komi
        {
            get { return this.komi; }
            set
            {
                if (Math.Abs(this.komi - value) > 1e-6)
                {
                    this.komi = value;
                    this.FirePropertyChanged("Komi");
                }
            }
        }

        public int Handicap
        {
            get { return this.handicap; }
            set
            {
                if (this.handicap != value)
                {
                    this.handicap = value;
                    this.FirePropertyChanged("Handicap");
                }
            }
        }

        public void NewGame(int boardSize, GoPlayer whitePlayer, GoPlayer blackPlayer)
        {
            this.ActiveGame = new GoGame(boardSize, whitePlayer, blackPlayer, this.handicap, this.komi);
        }

        private void UpdateIsolatedStore()
        {
            if (this.isolatedStore != null)
            {
                this.isolatedStore["MoveMarkerOption"] = this.MoveMarkerOption;
                this.isolatedStore["ShowDropCursor"] = this.ShowDropCursor;
                this.isolatedStore["SoundEnabled"] = this.SoundEnabled;
                this.isolatedStore["Komi"] = this.Komi;
                this.isolatedStore["Handicap"] = this.Handicap;
                this.isolatedStore.Save();
            }
        }

        private void RefreshFromIsolatedStore()
        {
            LicenseInformation licenseInformation = new LicenseInformation();
            this.isTrial = licenseInformation.IsTrial();

            if (this.isolatedStore != null)
            {
                if (!this.isolatedStore.TryGetValue<MoveMarkerOption>("MoveMarkerOption", out this.moveMarkerOption))
                    this.moveMarkerOption = MoveMarkerOption.Text2;

                if (!this.isolatedStore.TryGetValue<bool>("ShowDropCursor", out this.showDropCursor))
                    this.showDropCursor = true;

                if (!this.isolatedStore.TryGetValue<bool>("SoundEnabled", out this.soundEnabled))
                    this.soundEnabled = true;

                if (!this.isolatedStore.TryGetValue<float>("Komi", out this.komi))
                    this.komi = 6.5f;

                if (!this.isolatedStore.TryGetValue<int>("Handicap", out this.handicap))
                    this.handicap = 0;
            }
        }

		public event PropertyChangedEventHandler PropertyChanged;

        private void FirePropertyChanged(string name)
        {
            this.UpdateIsolatedStore();

            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
	}
}
