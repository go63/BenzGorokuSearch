using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace GorokuEdit
{
	public class CurrentFileNameConverter:IMultiValueConverter
	{
		public object Convert(object[] values,Type targetType,object parameter,System.Globalization.CultureInfo culture)
		{
			return "ベンツ君語録タグ編集"+(string.IsNullOrWhiteSpace((string)values[0])?"":" - "+(string)values[0])+((bool)values[1]?"*":"");
		}

		public object[] ConvertBack(object value,Type[] targetTypes,object parameter,System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class IndexConverter:IMultiValueConverter
	{
		public object Convert(object[] values,Type targetType,object parameter,System.Globalization.CultureInfo culture)
		{
			return ((int)values[0]+1).ToString()+" / "+((int)values[1]).ToString();
		}

		public object[] ConvertBack(object value,Type[] targetTypes,object parameter,System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}
