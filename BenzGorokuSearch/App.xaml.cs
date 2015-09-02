using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.ComponentModel;
using System.Windows.Media;
using System.Threading;
using System.Reflection;

namespace BenzGorokuSearch
{
	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App:Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
#if !DEBUG
			var splashMessages=new[]
			{
				"(*｀Д´)ノ！！！\n\n初期化中なんだよ",
				"(´；ω；`)\n\nちょっと待つんだよ",
				"<*｀∀´>\nﾁｮﾊﾟｰﾘはそこで待ってろニダ",
				"( ｀ハ´)\nそこで待つﾖﾛｼ",
				"(≧∇≦)ﾐﾃﾃﾈ ﾓｯｺﾘ",
				"<*｀ム´>\n起動中　あとは察してほしい",
				"<σ｀∀´>σ♪♪起動中だぜオイエー",
				"あの　ちょっとまってください",
				"<＊‘∀‘ >⊃─★\nウリが魔法をかけてるから待つニダ",
				"エレガントに待ちましょう マミ様"
			};
			var splashWindow=new Window(){
				Width=400,
				Height=150,
				WindowStartupLocation=WindowStartupLocation.CenterScreen,
				WindowStyle=WindowStyle.None,
				ShowInTaskbar=false,
				Topmost=true,
				AllowsTransparency=true,
				BorderThickness=new Thickness(1),
				BorderBrush=Brushes.Gray
			};
			var stackPanel=new StackPanel();
			splashWindow.Content=stackPanel;
			var executingAssembly=Assembly.GetExecutingAssembly();
			var version=executingAssembly.GetName().Version;
			var title="ベンツ君語録検索 ver"+
				version.Major.ToString()+"."+
				version.Minor.ToString()+
				(version.Build!=0?"."+version.Build.ToString():"");
			var splashMessage=splashMessages[new Random((int)DateTime.Now.Ticks).Next(splashMessages.Length)];
			stackPanel.Children.Add(new Label(){
				HorizontalContentAlignment=HorizontalAlignment.Center,
				Content=title,
				FontSize=25
			});
			stackPanel.Children.Add(new Label(){
				HorizontalContentAlignment=HorizontalAlignment.Center,
				VerticalContentAlignment=VerticalAlignment.Center,
				Content=splashMessage,
				FontSize=20
			});
			splashWindow.Show();
#endif
			var mainWindow=new MainWindow();
#if !DEBUG
			mainWindow.Loaded+=(_,__)=>ThreadPool.QueueUserWorkItem(CloseWindow,splashWindow);
#endif
			mainWindow.Show();
		}

		private void CloseWindow(object window)
		{
			Thread.Sleep(700);
			Application.Current.Dispatcher.Invoke((Action)(()=>(window as Window).Close()),null);
		}
	}

	public class ViewModelBase:INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public void NotifyPropertyChanged(string name)
		{
			if(PropertyChanged!=null) PropertyChanged(this,new PropertyChangedEventArgs(name));
		}
	}
}
