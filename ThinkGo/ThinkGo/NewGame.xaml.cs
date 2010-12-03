using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Collections.Generic;
using Phone.Controls;

namespace ThinkGo
{
    public class TimeSetting
    {
        public static List<TimeSetting> TimeSettings = new List<TimeSetting>()
        {
            new TimeSetting(500, ".5 seconds", ".5s"),
            new TimeSetting(1000, "1 second", "1s"),
            new TimeSetting(2500, "2.5 seconds", "2.5s"),
            new TimeSetting(4000, "4 seconds", "4s"),
            new TimeSetting(6000, "6 seconds", "6s"),
            new TimeSetting(10000, "10 seconds", "10s"),
            new TimeSetting(30000, "30 seconds", "30s"),
            new TimeSetting(60000, "1 minute", "1m"),
            new TimeSetting(180000, "3 minutes", "3m"),
        };

        public TimeSetting(int milliseconds, string name, string shortName)
        {
            this.Milliseconds = milliseconds;
            this.Name = name;
            this.ShortName = shortName;
        }

        public int Milliseconds { get; private set; }

        public string Name { get; private set; }
        public string ShortName { get; private set; }
    }

	public partial class NewGame : UserControl
	{
        private PickerBoxDialog aiSettings = new PickerBoxDialog();
        private GoAIPlayer aiSettingPlayer;
        private ListPicker activePicker;
        
		public NewGame()
		{
			// Required to initialize variables
			InitializeComponent();

			this.BoardSizePicker.Items.Add("9x9");
			this.BoardSizePicker.Items.Add("13x13");
			this.BoardSizePicker.Items.Add("19x19");

            GoPlayer human = new GoPlayer("Human");
            GoPlayer ai = new GoAIPlayer();

            this.WhitePlayerPicker.Items.Add(human);
            this.WhitePlayerPicker.Items.Add(ai);

            this.BlackPlayerPicker.Items.Add(human);
            this.BlackPlayerPicker.Items.Add(ai);

            this.BlackPlayerPicker.SelectedIndex = 0;
            this.WhitePlayerPicker.SelectedIndex = 1;

            this.BlackPlayerPicker.SelectionChanged += this.BlackPlayerPicker_SelectionChanged;
            this.WhitePlayerPicker.SelectionChanged += this.WhitePlayerPicker_SelectionChanged;

            this.aiSettings.ItemSource = TimeSetting.TimeSettings;
            this.aiSettings.Style = (Style)this.Resources["Custom"];
            this.aiSettings.AnimationFinished += new EventHandler(aiSettings_Closed);
            this.aiSettings.Title = "Time per move";

            this.Loaded += this.NewGame_Loaded;
        }

        private void BlackPlayerPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.BlackPlayerPicker.SelectedItem is GoAIPlayer)
            {
                this.WhitePlayerPicker.SelectedIndex = 0;
            }
        }

        private void WhitePlayerPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.WhitePlayerPicker.SelectedItem is GoAIPlayer)
            {
                this.BlackPlayerPicker.SelectedIndex = 0;
            }
        }

        void NewGame_Loaded(object sender, RoutedEventArgs e)
        {
            if (ThinkGoModel.Instance.Handicap != 0)
                this.HandicapText.Text = string.Format("{0} stone{1}", ThinkGoModel.Instance.Handicap, ThinkGoModel.Instance.Handicap > 1 ? "s" : string.Empty);
            else
                this.HandicapText.Text = string.Format("{0} komi", ThinkGoModel.Instance.Komi);
        }

        void aiSettings_Closed(object sender, EventArgs e)
        {
            if (this.aiSettingPlayer != null)
            {
                this.aiSettingPlayer.TimeSetting = (TimeSetting)this.aiSettings.SelectedItem;
                this.aiSettingPlayer = null;
            }

            if (this.activePicker != null)
            {
                this.activePicker.UnselectItem();
                this.activePicker = null;
            }
        }

		private void PlayGame(object sender, System.Windows.RoutedEventArgs e)
		{
            int boardSize = 9;
            switch (this.BoardSizePicker.SelectedIndex)
            {
                case 1: boardSize = 13; break;
                case 2: boardSize = 19; break;
            }
            ThinkGoModel.Instance.NewGame(boardSize, (GoPlayer)this.WhitePlayerPicker.SelectedItem, (GoPlayer)this.BlackPlayerPicker.SelectedItem);
			((INavigate)Application.Current.RootVisual).Navigate(new Uri("/GamePage.xaml", UriKind.Relative));
		}

		private void BlackAiPickerClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            this.activePicker = this.BlackPlayerPicker;
            this.HandleClick((GoPlayer)this.BlackPlayerPicker.SelectedItem);
            e.Handled = true;
		}

        private void WhiteAiPickerClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.activePicker = this.WhitePlayerPicker;
            this.HandleClick((GoPlayer)this.WhitePlayerPicker.SelectedItem);
            e.Handled = true;
        }

        private void HandleClick(GoPlayer player)
        {
            GoAIPlayer aiPlayer = player as GoAIPlayer;
            if (aiPlayer == null)
                return;

            this.aiSettingPlayer = aiPlayer;

            this.aiSettings.SelectedIndex = TimeSetting.TimeSettings.IndexOf(aiPlayer.TimeSetting);
            this.aiSettings.Show();
        }

		private void HandicapClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			((INavigate)Application.Current.RootVisual).Navigate(new Uri("/HandicapPage.xaml", UriKind.Relative));
			e.Handled = true;
		}
	}
}