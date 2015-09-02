using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace GorokuEdit
{
	public class JumpToWindowViewModel:ViewModelBase
	{
		private Tuple<int,int[]>[] threads;
		private Tuple<int,int[]> selectedThread;
		private int selectedResNumber;

		public Tuple<int,int[]>[] Threads
		{
			get
			{
				return threads;
			}
			private set
			{
				threads=value;
				NotifyPropertyChanged("Threads");
			}
		}
		public Tuple<int,int[]> SelectedThread
		{
			get
			{
				return selectedThread;
			}
			set
			{
				selectedThread=value;
				NotifyPropertyChanged("SelectedThread");
				SelectedResNumber=SelectedThread.Item2.First();
			}
		}
		public int SelectedResNumber
		{
			get
			{
				return selectedResNumber;
			}
			set
			{
				selectedResNumber=value;
				NotifyPropertyChanged("SelectedResNumber");
			}
		}

		public Command CloseWindowCommand{get;private set;}

		public JumpToWindowViewModel(Tuple<int,int[]>[] threads)
		{
			CloseWindowCommand=new Command(CloseWindow,()=>SelectedThread!=null);
			this.threads=threads;
			SelectedThread=threads.First();
			SelectedResNumber=SelectedThread.Item2.First();
		}

		public void CloseWindow()
		{
			App.Current.Windows.Cast<Window>().First(w=>w is JumpToWindow).DialogResult=true;
		}
	}
}
