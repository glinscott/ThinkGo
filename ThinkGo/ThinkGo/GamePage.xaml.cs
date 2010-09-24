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
using System.ComponentModel;

namespace ThinkGo
{
	public class ThinkGoModel
	{
		public ThinkGoModel()
		{
			this.ActiveGame = new GoGame(9, new GoPlayer("Human"), new GoAIPlayer());
		}

		public GoGame ActiveGame { get; private set; }
	}

    public partial class GamePage : PhoneApplicationPage
    {
		private ThinkGoModel model;

        public GamePage()
        {
			InitializeComponent();
		
			//if (DesignerProperties.IsInDesignTool)
			{
				this.model = new ThinkGoModel();
				this.DataContext = this.model;
			}
		}
    }
}
