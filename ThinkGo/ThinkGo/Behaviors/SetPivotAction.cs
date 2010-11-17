namespace ThinkGo.Behaviors
{
    using System.Windows;
    using System.Windows.Interactivity;
    using Microsoft.Phone.Controls;

    public class SetPivotIndexAction : TargetedTriggerAction<DependencyObject>
    {
        public SetPivotIndexAction()
        {
            // Insert code required on object creation below this point.
        }

        protected override void Invoke(object o)
        {
            // Insert code that defines what the Action will do when triggered/invoked.

            PhoneApplicationPage page = null;
            FrameworkElement p = this.Target as FrameworkElement;
            while (p != null)
            {
                if (typeof(PhoneApplicationPage).IsAssignableFrom(p.GetType()))
                {
                    page = (PhoneApplicationPage)p;
                    break;
                }
                p = p.Parent as FrameworkElement;
            }

            if (page != null)
            {
                Pivot pivot = this.Target as Pivot;
                if (pivot != null)
                {
                    string pivotIndex = "";
                    if (page.NavigationContext.QueryString.TryGetValue("PivotIndex", out pivotIndex))
                    {
                        pivot.SelectedIndex = int.Parse(pivotIndex);
                    }
                }
            }
        }
    }
}
