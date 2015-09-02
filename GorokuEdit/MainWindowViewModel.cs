using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;
using Microsoft.Win32;

namespace GorokuEdit
{
	/*
	 * 変更済みで保存しないまま閉じようとすると警告ダイアログを出す
	 * 最後のレス位置をファイルに書き込む?
	 * 始点と終点を指定してその範囲のレスに同じタグを付ける機能を実装する
	 */
	public class MainWindowViewModel:ViewModelBase
	{
		private ResponseViewModel currentResponse;
		private ResponseViewModel[] responses;
		private int responseLength=0,currentPosition=0;
		private bool isFileOpenning=false,isEdited=false;
		private string currentFileName=null,currentFileFullName;
		private string[] tags,tagPresets;
		private OpenFileDialog openFileDialog=new OpenFileDialog();
		private SaveFileDialog saveFileDialog=new SaveFileDialog();
		private XmlDocument database;
		private Tuple<int,int[]>[] threadIndices;

		public ResponseViewModel CurrentResponse
		{
			get
			{
				return currentResponse;
			}
			private set
			{
				currentResponse=value;
				NotifyPropertyChanged("CurrentResponse");
				NotifyPropertyChanged("CurrentResponseTag");
			}
		}
		public string CurrentResponseTag
		{
			get
			{
				return CurrentResponse==null?null:CurrentResponse.Tag;
			}
			set
			{
				if(CurrentResponse!=null){
					if(CurrentResponse.Tag!=value) IsEdited=true;
					CurrentResponse.Tag=value;
					NotifyPropertyChanged("CurrentResponseTag");
				}
			}
		}
		public int ResponseLength
		{
			get
			{
				return responseLength;
			}
			private set
			{
				responseLength=value;
				NotifyPropertyChanged("ResponseLength");
			}
		}
		public int CurrentPosition
		{
			get
			{
				return currentPosition;
			}
			private set
			{
				currentPosition=value;
				NotifyPropertyChanged("CurrentPosition");
			}
		}
		public string CurrentFileName
		{
			get
			{
				return currentFileName;
			}
			private set
			{
				currentFileName=value;
				NotifyPropertyChanged("CurrentFileName");
			}
		}
		public bool IsEdited
		{
			get
			{
				return isEdited;
			}
			private set
			{
				isEdited=value;
				NotifyPropertyChanged("IsEdited");
			}
		}
		public bool IsFileOpenning
		{
			get
			{
				return isFileOpenning;
			}
			private set
			{
				isFileOpenning=value;
				NotifyPropertyChanged("IsFileOpenning");
			}
		}
		public string[] Tags
		{
			get
			{
				return tags;
			}
			private set
			{
				tags=value;
				NotifyPropertyChanged("Tags");
			}
		}
		public Command OpenCommand{get;private set;}
		public Command SaveCommand{get;private set;}
		public Command SaveAsCommand{get;private set;}
		public Command JumpToCommand{get;private set;}
		public Command PreviousCommand{get;private set;}
		public Command NextCommand{get;private set;}

		public MainWindowViewModel()
		{
			openFileDialog.Filter="XMLファイル (*.xml)|*.xml";
			openFileDialog.FilterIndex=0;
			saveFileDialog.Filter="XMLファイル (*.xml)|*.xml";
			saveFileDialog.FilterIndex=0;
			OpenCommand=new Command(Open);
			SaveCommand=new Command(Save,()=>IsFileOpenning);
			SaveAsCommand=new Command(SaveAs,()=>IsFileOpenning);
			JumpToCommand=new Command(JumpTo,()=>IsFileOpenning);
			PreviousCommand=new Command(()=>Move(true),()=>IsFileOpenning&&CurrentPosition>0);
			NextCommand=new Command(()=>Move(false),()=>IsFileOpenning&&CurrentPosition<responses.Length-1);
			tagPresets=File.Exists("tags.txt")?LoadTagPresets("tags.txt"):new string[]{};
		}

		private void Move(bool isBack)
		{
			CurrentPosition+=isBack?-1:1;
			CurrentResponse=responses[CurrentPosition];
			System.Windows.Input.CommandManager.InvalidateRequerySuggested();
		}

		private void Open()
		{
			if(IsEdited){
				var result=ShowNotSavedMessage();
				if(result==MessageBoxResult.Yes) Save();
				else if(result==MessageBoxResult.Cancel) return;
			}
			if(openFileDialog.ShowDialog().Value){
				try{
					database=new XmlDocument();
					database.Load(openFileDialog.FileName);
					responses=ResponseViewModel.ReadFromXmlDocument(database).ToArray();
					threadIndices=ResponseViewModel.GetThreadIndices(database).ToArray();
					database.PreserveWhitespace=true;
				}catch(Exception){
					MessageBox.Show("(´；ω；`)\n\nファイルをちゃんと読み込めなかったんだよ","ベンツ君語録タグ編集",MessageBoxButton.OK,MessageBoxImage.Error);
					return;
				}
				IsEdited=false;
				IsFileOpenning=true;
				saveFileDialog.InitialDirectory=Path.GetDirectoryName(openFileDialog.FileName);
				CurrentFileName=Path.GetFileName(openFileDialog.FileName);
				currentFileFullName=openFileDialog.FileName;
				ResponseLength=responses.Length;
				Tags=GetTags(responses);
				if(ResponseLength==0) CurrentPosition=-1;
				else{
					CurrentPosition=0;
					CurrentResponse=responses[CurrentPosition];
				}
			}
			System.Windows.Input.CommandManager.InvalidateRequerySuggested();
		}

		private string[] GetTags(ResponseViewModel[] responses)
		{
			var fileTags=new List<string>();
			foreach(var response in responses) if(!fileTags.Contains(response.Tag)) fileTags.Add(response.Tag);
			return fileTags.Concat(tagPresets).ToArray();
		}

		private string NormalizeTag(string tag)
		{
			var normalizeTable=new Dictionary<char,char>(){
				{'０','0'},{'１','1'},{'２','2'},{'３','3'},{'４','4'},
				{'５','5'},{'６','6'},{'７','7'},{'８','8'},{'９','9'},
				{'（','('},{'）',')'},
			};
			var buffer=new StringBuilder(tag.Length);
			var isPrevCharQuestion=false;
			for(int i=0;i<tag.Length;++i){
				if(!isPrevCharQuestion&&(tag[i]=='?'||tag[i]=='？')){
					isPrevCharQuestion=true;
					buffer.Append('?');
				}else{
					isPrevCharQuestion=false;
					if(normalizeTable.Any(p=>p.Key==tag[i])) buffer.Append(normalizeTable[tag[i]]);
					else buffer.Append(tag[i]);
				}
			}
			return buffer.ToString();
		}

		private string[] LoadTagPresets(string tagPresetFileName)
		{
			return File.ReadAllLines(tagPresetFileName,Encoding.UTF8)
				.Where(t=>t!=""&&!string.IsNullOrWhiteSpace(t))
				.Select(NormalizeTag)
				.ToArray();
		}

		private void Save()
		{
			foreach(var response in responses) response.WriteToXmlNode();
			database.Save(currentFileFullName);
			IsEdited=false;
		}

		private void SaveAs()
		{
			if(saveFileDialog.ShowDialog().Value){
				foreach(var response in responses) response.WriteToXmlNode();
				database.Save(saveFileDialog.FileName);
				CurrentFileName=Path.GetFileName(saveFileDialog.FileName);
				currentFileFullName=saveFileDialog.FileName;
				IsEdited=false;
			}
		}

		public MessageBoxResult ShowNotSavedMessage()
		{
			return MessageBox.Show("ファイルの変更内容が保存されていません。\n保存しますか?","ベンツ君語録タグ編集",MessageBoxButton.YesNoCancel,MessageBoxImage.Warning);
		}

		private void JumpTo()
		{
			var jumpPosition=JumpToWindow.GetJumpPosition(threadIndices);
			if(jumpPosition==null) return;
			for(int i=0;i<responses.Length;++i){
				if(responses[i].PartNumber==jumpPosition.Item1&&responses[i].ResNumber==jumpPosition.Item2){
					CurrentPosition=i;
					CurrentResponse=responses[CurrentPosition];
					System.Windows.Input.CommandManager.InvalidateRequerySuggested();
				}
			}
		}
	}
}
