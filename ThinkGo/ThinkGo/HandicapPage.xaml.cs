using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace ThinkGo
{
    public partial class HandicapPage : PhoneApplicationPage
    {
        public HandicapPage()
        {
            InitializeComponent();

			for (int i = 0; i < 10; i++)
			{
				this.StoneButtons.Items.Add(i);
			}
        }
    }
}
