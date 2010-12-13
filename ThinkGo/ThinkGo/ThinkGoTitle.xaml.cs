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
	public partial class ThinkGoTitle : UserControl
	{
		public ThinkGoTitle()
		{
			// Required to initialize variables
			InitializeComponent();

            this.Loaded += new RoutedEventHandler(ThinkGoTitle_Loaded);
		}

        void ThinkGoTitle_Loaded(object sender, RoutedEventArgs e)
        {
            if (ThinkGoModel.Instance.IsTrial)
            {
                this.Title.Text = "ThinkGo (Trial Mode)";
            }
            else
            {
                this.Title.Text = "ThinkGo";
            }
        }
	}
}