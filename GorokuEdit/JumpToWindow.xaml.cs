using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GorokuEdit
{
	/// <summary>
	/// JumpToWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class JumpToWindow:Window
	{
		private JumpToWindowViewModel viewModel;

		public JumpToWindow(Tuple<int,int[]>[] threads)
		{
			InitializeComponent();
			viewModel=new JumpToWindowViewModel(threads);
			DataContext=viewModel;
		}

		private void Window_PreviewKeyDown(object sender,KeyEventArgs e)
		{
			if(e.Key==Key.Enter){
				DialogResult=true;
				Close();
			}
		}

		public static Tuple<int,int> GetJumpPosition(Tuple<int,int[]>[] threads)
		{
			var window=new JumpToWindow(threads);
			window.Owner=App.Current.MainWindow;
			if(window.ShowDialog().Value) return Tuple.Create(window.viewModel.SelectedThread.Item1,window.viewModel.SelectedResNumber);
			else return null;
		}
	}
}
