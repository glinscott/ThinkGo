using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Phone.Controls
{
    public class MultiLineItem : DependencyObject
    {
        public static readonly DependencyProperty Line1Property = DependencyProperty.Register("Line1", typeof(string), typeof(MultiLineItem), null);
        public string Line1 { get { return (string)GetValue(Line1Property); } set { SetValue(Line1Property, value); } }

        public static readonly DependencyProperty Line2Property = DependencyProperty.Register("Line2", typeof(string), typeof(MultiLineItem), null);
        public string Line2 { get { return (string)GetValue(Line2Property); } set { SetValue(Line2Property, value); } }

        public static readonly DependencyProperty Line3Property = DependencyProperty.Register("Line3", typeof(string), typeof(MultiLineItem), null);
        public string Line3 { get { return (string)GetValue(Line3Property); } set { SetValue(Line3Property, value); } }
    }
}