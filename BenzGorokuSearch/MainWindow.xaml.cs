using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace BenzGorokuSearch
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow:Window
	{
		private bool updatingPartList=false,updatingPeopleList=false;
		private SolidColorBrush grayBrush=new SolidColorBrush(Color.FromRgb(0xCC,0xCC,0xCC));

		public MainWindow()
		{
			InitializeComponent();
			var thisAssembly=Assembly.GetExecutingAssembly();
			var stream=thisAssembly.GetManifestResourceStream("BenzGorokuSearch.benz.ico");
			var decoder=new IconBitmapDecoder(stream,BitmapCreateOptions.DelayCreation,BitmapCacheOption.Default);
			Icon=decoder.Frames[0];
			DataContext=new MainWindowViewModel();
		}
		
		private void ListBox_TargetUpdated(object sender,DataTransferEventArgs e)
		{
			if(e.Property.Name=="ItemsSource"){
				var listBox=sender as ListBox;
				if(listBox.Items.Count>0) listBox.ScrollIntoView(listBox.Items[0]);
			}
		}

		private void Hyperlink_RequestNavigate(object sender,RequestNavigateEventArgs e)
		{
			System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
		}

		private void Hyperlink_MouseEnter(object sender,MouseEventArgs e)
		{
			var link=sender as Hyperlink;
			link.TextDecorations=TextDecorations.Underline;
		}

		private void Hyperlink_MouseLeave(object sender,MouseEventArgs e)
		{
			var link=sender as Hyperlink;
			link.TextDecorations=null;
		}

		private void Window_Closing(object sender,CancelEventArgs e)
		{
			var viewModel=DataContext as MainWindowViewModel;
			viewModel.SaveSettings(RestoreBounds);
		}

		private void PartList_CheckedChanged(object sender,RoutedEventArgs e)
		{
			if(!updatingPartList&&partListView.SelectedItems.Count>1){
				var checkBox=(CheckBox)sender;
				updatingPartList=true;
				foreach(var item in partListView.SelectedItems.Cast<PartNumberViewModel>().Where(p=>p!=checkBox.DataContext).ToArray()){
					item.IsChecked=checkBox.IsChecked.Value;
				}
				updatingPartList=false;
			}
		}

		private void PeopleList_CheckedChanged(object sender,RoutedEventArgs e)
		{
			if(!updatingPeopleList&&peopleListView.SelectedItems.Count>1){
				var checkBox=(CheckBox)sender;
				updatingPeopleList=true;
				foreach(var item in peopleListView.SelectedItems.Cast<PersonViewModel>().Where(p=>p!=checkBox.DataContext).ToArray()){
					item.IsChecked=checkBox.IsChecked.Value;
				}
				updatingPeopleList=false;
			}
		}

		private void Border_MouseEnter(object sender,MouseEventArgs e)
		{
			var border=(Border)sender;
			border.Background=Brushes.White;
		}

		private void Border_MouseLeave(object sender,MouseEventArgs e)
		{
			var border=(Border)sender;
			border.Background=grayBrush;
		}

		private void AddPersonCondition(object sender,RequestNavigateEventArgs e)
		{
			var viewModel=(MainWindowViewModel)DataContext;
			var response=(ResponseViewModel)((Hyperlink)sender).DataContext;
			viewModel.AddPersonConditionAndRefresh(response.Tag);
		}

		private void Window_PreviewKeyDown(object sender,KeyEventArgs e)
		{
			if(e.Key==Key.Enter){
				var viewModel=(MainWindowViewModel)DataContext;
				if(viewModel.SearchCommand.CanExecute(null)) viewModel.SearchCommand.Execute(null);
				e.Handled=true;
			}
		}

		private void RichTextBox_ContextMenuOpening(object sender,ContextMenuEventArgs e)
		{
			var textBox=(RichTextBox)sender;
			var contextMenu=textBox.ContextMenu;
			((MenuItem)contextMenu.Items[0]).IsEnabled=!textBox.Selection.IsEmpty;
			contextMenu.DataContext=textBox;
		}

		private void CopySelectedText(object sender,RoutedEventArgs e)
		{
			var textBox=(RichTextBox)((MenuItem)sender).DataContext;
			textBox.Copy();
		}

		private void CopyAllText(object sender,RoutedEventArgs e)
		{
			var textBox=(RichTextBox)((MenuItem)sender).DataContext;
			Clipboard.Clear();
			Clipboard.SetText(new TextRange(textBox.Document.ContentStart,textBox.Document.ContentEnd).Text);
		}

		private void CopyOriginalUrl(object sender,RoutedEventArgs e)
		{
			var textBox=(RichTextBox)((MenuItem)sender).DataContext;
			var response=(ResponseViewModel)textBox.DataContext;
			Clipboard.Clear();
			Clipboard.SetText(response.ResNumberLink);
		}

		private void CopyIDUrl(object sender,RoutedEventArgs e)
		{
			var textBox=(RichTextBox)((MenuItem)sender).DataContext;
			var response=(ResponseViewModel)textBox.DataContext;
			Clipboard.Clear();
			Clipboard.SetText(response.IDLink);
		}

		private void CopyThreadUrl(object sender,RoutedEventArgs e)
		{
			var textBox=(RichTextBox)((MenuItem)sender).DataContext;
			var response=(ResponseViewModel)textBox.DataContext;
			Clipboard.Clear();
			Clipboard.SetText(response.ThreadLink);
		}
	}
}
