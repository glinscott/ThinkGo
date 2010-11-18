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
        private bool changing = false;

        public HandicapPage()
        {
            InitializeComponent();

			for (int i = 0; i < 10; i++)
			{
				this.StoneButtons.Items.Add(i);
			}

            this.StoneButtons.SelectedIndex = ThinkGoModel.Instance.Handicap;
            this.StoneButtons.SelectionChanged += new SelectionChangedEventHandler(StoneButtons_SelectionChanged);

            foreach (float komi in new float[] { 0.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8 })
            {
                this.KomiButtons.Items.Add(komi);
            }
            
            this.KomiButtons.SelectedItem = ThinkGoModel.Instance.Komi;
            this.KomiButtons.SelectionChanged += new SelectionChangedEventHandler(KomiButtons_SelectionChanged);
        }

        private void KomiButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.changing)
                return;

            try
            {
                this.changing = true;
                ThinkGoModel.Instance.Komi = (float)this.KomiButtons.SelectedItem;

                ThinkGoModel.Instance.Handicap = 0;
                this.StoneButtons.SelectedIndex = 0;
            }
            finally
            {
                this.changing = false;
            }
        }

        private void StoneButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.changing)
                return;

            try
            {
                this.changing = true;
                ThinkGoModel.Instance.Handicap = (int)this.StoneButtons.SelectedItem;

                ThinkGoModel.Instance.Komi = 0.5f;
                this.KomiButtons.SelectedIndex = 0;
            }
            finally
            {
                this.changing = false;
            }
        }
    }
}
