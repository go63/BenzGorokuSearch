using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Xml;
using BenzGorokuSearch.Properties;

namespace BenzGorokuSearch
{
	class MainWindowViewModel:ViewModelBase
	{
		private ResponseViewModel[] responses,searchResult=null;
		private DateTime publishedDate;
		private bool updating=false,initializing=true,searching=false;
		private bool searchFromSelectedDate=false,searchFromSelectedParts=false,searchFromSelectedPeople=false;
		private string searchWord="",statusText="";
		private DateTime? startDate,endDate;
		private double windowWidth,windowHeight,windowLocationX,windowLocationY;
		private WindowState windowState;
		private PartNumberViewModel[] partsList=null;
		private PersonViewModel[] peopleList=null;
		private SearchQuery query=new SearchQuery();

		public string SearchWord
		{
			get
			{
				return searchWord;
			}
			set
			{
				searchWord=value;
				NotifyPropertyChanged("SearchWord");
			}
		}
		public bool SearchFromSelectedDate
		{
			get
			{
				return searchFromSelectedDate;
			}
			set
			{
				searchFromSelectedDate=value;
				NotifyPropertyChanged("SearchFromSelectedDate");
			}
		}
		public DateTime? StartDate
		{
			get
			{
				return startDate;
			}
			set
			{
				startDate=value;
				NotifyPropertyChanged("StartDate");
			}
		}
		public DateTime? EndDate
		{
			get
			{
				return endDate;
			}
			set
			{
				endDate=value;
				NotifyPropertyChanged("EndDate");
			}
		}
		public bool SearchFromSelectedParts
		{
			get
			{
				return searchFromSelectedParts;
			}
			set
			{
				searchFromSelectedParts=value;
				NotifyPropertyChanged("SearchFromSelectedParts");
			}
		}
		public bool SearchFromSelectedPeople
		{
			get
			{
				return searchFromSelectedPeople;
			}
			set
			{
				searchFromSelectedPeople=value;
				NotifyPropertyChanged("SearchFromSelectedPeople");
			}
		}
		public string StatusText
		{
			get
			{
				return statusText;
			}
			set
			{
				statusText=value;
				NotifyPropertyChanged("StatusText");
			}
		}
		public ResponseViewModel[] SearchResult
		{
			get
			{
				return searchResult;
			}
			set
			{
				searchResult=value;
				NotifyPropertyChanged("SearchResult");
			}
		}
		public double WindowWidth
		{
			get
			{
				return windowWidth;
			}
			set
			{
				if(WindowState!=WindowState.Maximized) windowWidth=value;
				NotifyPropertyChanged("WindowWidth");
			}
		}
		public double WindowHeight
		{
			get
			{
				return windowHeight;
			}
			set
			{
				if(WindowState!=WindowState.Maximized) windowHeight=value;
				NotifyPropertyChanged("WindowHeight");
			}
		}
		public WindowState WindowState
		{
			get
			{
				return windowState;
			}
			set
			{
				windowState=value;
				NotifyPropertyChanged("WindowState");
			}
		}
		public double WindowLocationX
		{
			get
			{
				return windowLocationX;
			}
			set
			{
				if(WindowState!=WindowState.Maximized) windowLocationX=value;
				NotifyPropertyChanged("WindowLocationX");
			}
		}
		public double WindowLocationY
		{
			get
			{
				return windowLocationY;
			}
			set
			{
				if(WindowState!=WindowState.Maximized) windowLocationY=value;
				NotifyPropertyChanged("WindowLocationY");
			}
		}
		public PartNumberViewModel[] PartsList
		{
			get
			{
				return partsList;
			}
			private set
			{
				partsList=value;
				NotifyPropertyChanged("PartsList");
			}
		}
		public PersonViewModel[] PeopleList
		{
			get
			{
				return peopleList;
			}
			private set
			{
				peopleList=value;
				NotifyPropertyChanged("PeopleList");
			}
		}
		public Command SearchCommand{get;private set;}
		public Command SelectAllPartsCommand{get;private set;}
		public Command UnselectAllPartsCommand{get;private set;}
		public Command SelectAllPeopleCommand{get;private set;}
		public Command UnselectAllPeopleCommand{get;private set;}
		public Command CheckUpdateCommand{get;private set;}

		public MainWindowViewModel()
		{
			if(!File.Exists("database.xml")){
				if(!System.ComponentModel.DesignerProperties.GetIsInDesignMode(Application.Current.Windows[0])){
					MessageBox.Show("(*｀Д´)ノ！！！\n\ndatabase.xmlがないんだよ\n\n(´；ω；`)\n\nこのアプリと同じフォルダにおいてほしいんだよ","(*｀Д´)ノ！！！",MessageBoxButton.OK,MessageBoxImage.Error);
					Application.Current.Shutdown();
				}
				return;
			}
			StatusText="初期化中...";
			LoadSettings();
			endDate=DateTime.Now.Date;
			SearchCommand=new Command(StartSearch,()=>!initializing&&!searching);
			SelectAllPartsCommand=new Command(()=>SetAllPartsState(true),()=>!initializing);
			UnselectAllPartsCommand=new Command(()=>SetAllPartsState(false),()=>!initializing);
			SelectAllPeopleCommand=new Command(()=>SetAllPeopleState(true),()=>!initializing);
			UnselectAllPeopleCommand=new Command(()=>SetAllPeopleState(false),()=>!initializing);
			CheckUpdateCommand=new Command(StartCheckUpdate,()=>!updating);
			ThreadPool.QueueUserWorkItem(LoadDatabase,"database.xml");
			query.SearchCompleted+=r=>App.Current.Dispatcher.Invoke((Action<ResponseViewModel[]>)EndSearch,new[]{r});
		}

		private void LoadDatabase(object arg)
		{
			var database=new XmlDocument();
			database.Load((string)arg);
			var publishedDate=ResponseViewModel.GetPublishedDate(database);
			var responses=ResponseViewModel.ReadFromXmlDocument(database).ToArray();
			var parts=CreatePartsList(responses);
			var people=CreatePeopleList(responses);
			var action=(Action<ResponseViewModel[],PartNumberViewModel[],PersonViewModel[],DateTime>)EndLoadDatabase;
			Application.Current.Dispatcher.Invoke(action,responses,parts,people,publishedDate);
		}

		private void EndLoadDatabase(ResponseViewModel[] res,PartNumberViewModel[] parts,PersonViewModel[] people,DateTime date)
		{
			responses=res;
			publishedDate=date;
			PartsList=parts;
			PeopleList=people;
			StatusText="";
			initializing=false;
			if(DateTime.Today>Settings.Default.LastUpdateCheckDate) StartCheckUpdate();
			System.Windows.Input.CommandManager.InvalidateRequerySuggested();
		}

		private void StartCheckUpdate()
		{
			updating=true;
			ThreadPool.QueueUserWorkItem(_=>CheckUpdate(UpdateChecker.GetUpdateInfo()));
		}

		private void CheckUpdate(UpdateInfo updateInfo)
		{
			if(updateInfo==null) return;
			else Settings.Default.LastUpdateCheckDate=DateTime.Today;
			var updateDatabase=updateInfo.LatestDatabasePublishedDate>publishedDate;
			var updateApp=UpdateChecker.IsNewerThanCurrentVersion(updateInfo.LatestAppVersion);
			if(updateDatabase&&updateApp){
				var result=MessageBox.Show("新しいデータベースとアプリが利用可能です。\n\nダウンロードしますか?","ベンツ君語録検索",MessageBoxButton.YesNo,MessageBoxImage.Information);
				if(result==MessageBoxResult.Yes){
					Process.Start(updateInfo.LatestDatabaseUrl);
					Process.Start(updateInfo.LatestAppUrl);
					App.Current.Dispatcher.Invoke((Action)App.Current.Shutdown,null);
				}
			}else if(updateDatabase){
				var result=MessageBox.Show("新しいデータベースが利用可能です。\n\nダウンロードしますか?","ベンツ君語録検索",MessageBoxButton.YesNo,MessageBoxImage.Information);
				if(result==MessageBoxResult.Yes){
					Process.Start(updateInfo.LatestDatabaseUrl);
					App.Current.Dispatcher.Invoke((Action)App.Current.Shutdown,null);
				}
			}else if(updateApp){
				var result=MessageBox.Show("新しいアプリが利用可能です。\n\nダウンロードしますか?","ベンツ君語録検索",MessageBoxButton.YesNo,MessageBoxImage.Information);
				if(result==MessageBoxResult.Yes){
					Process.Start(updateInfo.LatestAppUrl);
					App.Current.Dispatcher.Invoke((Action)App.Current.Shutdown,null);
				}
			}
			updating=false;
			App.Current.Dispatcher.Invoke((Action)System.Windows.Input.CommandManager.InvalidateRequerySuggested,null);
		}

		private PartNumberViewModel[] CreatePartsList(ResponseViewModel[] responses)
		{
			var unsortedPartsList=new List<PartNumberViewModel>();
			foreach(var response in responses)
				if(!unsortedPartsList.Any(p=>p.PartNumber==response.PartNumber)) unsortedPartsList.Add(new PartNumberViewModel(response.PartNumber,false));
			return unsortedPartsList.OrderBy(p=>p.PartNumber).ToArray();
		}

		private PersonViewModel[] CreatePeopleList(ResponseViewModel[] responses)
		{
			var peopleList=new List<PersonViewModel>();
			foreach(var response in responses)
				if(!peopleList.Any(p=>p.Name==response.Tag)) peopleList.Add(new PersonViewModel(response.Tag,false));
			return peopleList.ToArray();
		}

		private void LoadSettings()
		{
			if(!Settings.Default.Upgraded){
				Settings.Default.Upgrade();
				Settings.Default.Upgraded=true;
			}
			WindowLocationX=Settings.Default.WindowLocationX;
			WindowLocationY=Settings.Default.WindowLocationY;
			WindowWidth=Settings.Default.WindowWidth;
			WindowHeight=Settings.Default.WindowHeight;
			WindowState=Settings.Default.IsMaximized?WindowState.Maximized:WindowState.Normal;
		}

		public void SaveSettings(Rect restoreBounds)
		{
			Settings.Default.WindowLocationX=restoreBounds.Left;
			Settings.Default.WindowLocationY=restoreBounds.Top;
			Settings.Default.WindowWidth=WindowWidth;
			Settings.Default.WindowHeight=WindowHeight;
			Settings.Default.IsMaximized=WindowState==WindowState.Maximized;
			Settings.Default.Save();
		}

		private void StartSearch()
		{
			searching=true;
			StatusText="検索中...";
			query.Condition=ExpressionBuilder.Create(SearchWord);
			query.SearchFromSelectedDate=SearchFromSelectedDate;
			if(SearchFromSelectedDate){
				query.StartDate=StartDate;
				query.EndDate=EndDate;
			}
			query.SearchFromSelectedParts=SearchFromSelectedParts;
			if(SearchFromSelectedParts) query.Parts=PartsList.Where(p=>p.IsChecked).Select(p=>p.PartNumber).ToArray();
			query.SearchFromSelectedPeople=SearchFromSelectedPeople;
			if(SearchFromSelectedPeople) query.People=PeopleList.Where(p=>p.IsChecked).Select(p=>p.Name).ToArray();
			query.SearchAsync(responses);
		}

		private void EndSearch(ResponseViewModel[] result)
		{
			SearchResult=result;
			searching=false;
			StatusText="";
			System.Windows.Input.CommandManager.InvalidateRequerySuggested();
		}

		private void SetAllPartsState(bool state)
		{
			foreach(var part in PartsList) part.IsChecked=state;
		}

		private void SetAllPeopleState(bool state)
		{
			foreach(var person in PeopleList) person.IsChecked=state;
		}

		public void AddPersonConditionAndRefresh(string tag)
		{
			SearchFromSelectedPeople=true;
			foreach(var person in PeopleList){
				if(person.Name==tag) person.IsChecked=true;
				else person.IsChecked=false;
			}
			StartSearch();
		}
	}

	public class PartNumberViewModel:ViewModelBase
	{
		private bool isChecked;

		public int PartNumber{get;private set;}
		public string PartNumberString{get;private set;}
		public bool IsChecked
		{
			get
			{
				return isChecked;
			}
			set
			{
				isChecked=value;
				NotifyPropertyChanged("IsChecked");
			}
		}

		public PartNumberViewModel(int partNumber,bool isChecked)
		{
			PartNumber=partNumber;
			IsChecked=isChecked;
			PartNumberString=partNumber==-1?"その他":partNumber.ToString();
		}
	}

	public class PersonViewModel:ViewModelBase
	{
		private bool isChecked;

		public string Name{get;private set;}
		public bool IsChecked
		{
			get
			{
				return isChecked;
			}
			set
			{
				isChecked=value;
				NotifyPropertyChanged("IsChecked");
			}
		}

		public PersonViewModel(string name,bool isChecked)
		{
			Name=name;
			IsChecked=isChecked;
		}
	}
}
