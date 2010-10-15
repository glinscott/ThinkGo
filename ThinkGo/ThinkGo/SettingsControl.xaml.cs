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
	public partial class SettingsControl : UserControl
	{
		public SettingsControl()
		{
			// Required to initialize variables
			InitializeComponent();

            this.NumberMovesPicker.Items.Add("None");
            this.NumberMovesPicker.Items.Add("Last move only");
            this.NumberMovesPicker.Items.Add("Last two moves");
            this.NumberMovesPicker.Items.Add("All moves");

            this.NumberMovesPicker.SelectedIndex = (int)ThinkGoModel.Instance.MoveMarkerOption;

            this.NumberMovesPicker.SelectionChanged += new SelectionChangedEventHandler(NumberMovesPicker_SelectionChanged);
        }

        void NumberMovesPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ThinkGoModel.Instance.MoveMarkerOption = (MoveMarkerOption)this.NumberMovesPicker.SelectedIndex;
        }
	}
}