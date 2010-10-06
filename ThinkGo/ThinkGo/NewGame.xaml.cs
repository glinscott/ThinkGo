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

namespace ThinkGo
{
	public partial class NewGame : UserControl
	{
		public NewGame()
		{
			// Required to initialize variables
			InitializeComponent();

			this.BoardSizePicker.Items.Add("9x9");
			this.BoardSizePicker.Items.Add("13x13");
			this.BoardSizePicker.Items.Add("19x19");
		}

		private void PlayGame(object sender, System.Windows.RoutedEventArgs e)
		{
			((INavigate)Application.Current.RootVisual).Navigate(new Uri("/GamePage.xaml", UriKind.Relative));
		}

		private void BoardSizeClicked(object sender, System.Windows.RoutedEventArgs e)
		{
		}

		private void BlackNameClicked(object sender, System.Windows.RoutedEventArgs e)
		{
			// TODO: Add event handler implementation here.
		}

		private void WhiteNameClicked(object sender, System.Windows.RoutedEventArgs e)
		{
			// TODO: Add event handler implementation here.
		}
	}
}