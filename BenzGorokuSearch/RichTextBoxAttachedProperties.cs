using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace BenzGorokuSearch
{
	static class RichTextBoxAttachedProperties
	{
		public static bool GetIsSelected(RichTextBox textBox)
		{
			return !textBox.Selection.IsEmpty;
		}

		public static readonly DependencyProperty IsSelectedProperty=DependencyProperty.RegisterAttached("IsSelected",typeof(bool),typeof(RichTextBoxAttachedProperties),new PropertyMetadata(false));
	}
}
