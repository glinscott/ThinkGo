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
                int hoursRemaining = Math.Max(0, (int)(ThinkGoModel.Instance.TimeRemaining.TotalHours + 0.5));
                this.Title.Text = string.Format("ThinkGo (Trial - {0} hours remain)", hoursRemaining);
            }
            else
            {
                this.Title.Text = "ThinkGo";
            }
        }
	}
}