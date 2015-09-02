using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GorokuEdit
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow:Window
	{
		private MainWindowViewModel viewModel;

		public MainWindow()
		{
			InitializeComponent();
			var thisAssembly=Assembly.GetExecutingAssembly();
			var stream=thisAssembly.GetManifestResourceStream("GorokuEdit.benz.ico");
			var image=new IconBitmapDecoder(stream,BitmapCreateOptions.DelayCreation,BitmapCacheOption.Default);
			Icon=image.Frames[0];
			viewModel=new MainWindowViewModel();
			DataContext=viewModel;
		}

		private void Window_PreviewKeyDown(object sender,KeyEventArgs e)
		{
			var viewModel=(MainWindowViewModel)DataContext;
			if(Keyboard.IsKeyDown(Key.LeftCtrl)||Keyboard.IsKeyDown(Key.RightCtrl)){
				if(e.Key==Key.Up&&comboBox.SelectedIndex>0) comboBox.SelectedIndex--;
				else if(e.Key==Key.Down&&comboBox.SelectedIndex<comboBox.Items.Count-1) comboBox.SelectedIndex++;
			}else{
				if(e.Key==Key.Up&&viewModel.PreviousCommand.CanExecute(null)) viewModel.PreviousCommand.Execute(null);
				else if(e.Key==Key.Down&&viewModel.NextCommand.CanExecute(null)) viewModel.NextCommand.Execute(null);
			}
			if(e.Key==Key.Up||e.Key==Key.Down) e.Handled=true;
		}

		private void Window_Closing(object sender,System.ComponentModel.CancelEventArgs e)
		{
			if(viewModel.IsEdited){
				var result=viewModel.ShowNotSavedMessage();
				if(result==MessageBoxResult.Cancel) e.Cancel=true;
				else if(result==MessageBoxResult.Yes) viewModel.SaveCommand.Execute(null);
			}
		}
	}
}
