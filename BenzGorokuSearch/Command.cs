using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace BenzGorokuSearch
{
	class Command:ICommand
	{
		private Action action;
		private Func<bool> canExecute;

		public event EventHandler CanExecuteChanged
		{
			add
			{
				CommandManager.RequerySuggested+=value;
			}
			remove
			{
				CommandManager.RequerySuggested-=value;
			}
		}

		public Command(Action action)
		{
			if(action==null) throw new ArgumentNullException("パラメーターをnullにすることはできません。");
			this.canExecute=()=>true;
			this.action=action;
		}

		public Command(Action action,Func<bool> canExecute)
		{
			if(action==null||canExecute==null)
				throw new ArgumentNullException("パラメーターをnullにすることはできません。");
			this.action=action;
			this.canExecute=canExecute;
		}

		public bool CanExecute(object parameter)
		{
 			return canExecute();
		}

		public void Execute(object parameter)
		{
			action();
		}
	}
}
