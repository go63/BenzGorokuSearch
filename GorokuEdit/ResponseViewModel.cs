﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace GorokuEdit
{
	public class ResponseViewModel:ViewModelBase
	{
		private XmlNode originalNode;
		private string tag;

		public string ResNumberString{get;private set;}
		public int PartNumber{get;private set;}
		public int ResNumber{get;private set;}
		public string Name{get;private set;}
		public string ID{get;private set;}
		public DateTime Date{get;private set;}
		public string Text{get;private set;}
		public string Tag
		{
			get
			{
				return tag;
			}
			set
			{
				tag=value;
				NotifyPropertyChanged("Tag");
			}
		}
		public string ThreadLink{get;private set;}
		public string ResNumberLink
		{
			get
			{
				return ThreadLink+'/'+ResNumber.ToString();
			}
		}
		public string IDLink
		{
			get
			{
				return ThreadLink+"/?id="+ID;
			}
		}

		public ResponseViewModel(int partNumber,string threadLink,XmlNode node)
		{
			originalNode=node;
			PartNumber=partNumber;
			ResNumber=int.Parse(originalNode.Attributes[0].Value);
			Name=originalNode.Attributes[1].Value;
			ID=originalNode.Attributes[2].Value;
			Date=DateTime.Parse(originalNode.Attributes[3].Value);
			Tag=originalNode.Attributes[4].Value;
			ThreadLink=threadLink;
			Text=originalNode.InnerText;
			ResNumberString=(PartNumber==-1?"Other":"Part"+partNumber.ToString())+"-"+originalNode.Attributes[0].Value;
		}

		public void WriteToXmlNode()
		{
			originalNode.Attributes[4].Value=Tag;
		}

		public static IEnumerable<ResponseViewModel> ReadFromDatabase(string fileName)
		{
			var database=new XmlDocument();
			database.Load(fileName);
			return ReadFromXmlDocument(database);
		}

		public static IEnumerable<ResponseViewModel> ReadFromXmlDocument(XmlDocument database)
		{
			foreach(var threadNode in database.DocumentElement.ChildNodes.Cast<XmlNode>().ToArray()){
				var partNumber=int.Parse(threadNode.Attributes[0].Value);
				var threadLink=threadNode.Attributes[1].Value;
				foreach(var responseNode in threadNode.ChildNodes.Cast<XmlNode>().ToArray())
					yield return new ResponseViewModel(partNumber,threadLink,responseNode);
			}
		}

		public static IEnumerable<Tuple<int,int[]>> GetThreadIndices(XmlDocument database)
		{
			foreach(var threadNode in database.DocumentElement.ChildNodes.Cast<XmlNode>().ToArray()){
				var partNumber=int.Parse(threadNode.Attributes[0].Value);
				var resNumbers=threadNode.ChildNodes.Cast<XmlNode>().Select(n=>int.Parse(n.Attributes[0].Value)).ToArray();
				yield return Tuple.Create(partNumber,resNumbers);
			}
		}

		public static bool operator<(ResponseViewModel a,ResponseViewModel b)
		{
			if(a.Date<b.Date) return true;
			else if(a.Date>b.Date) return false;
			else if(a.ResNumber<b.ResNumber) return true;
			else if(a.ResNumber>b.ResNumber) return false;
			else if(a.PartNumber<b.PartNumber) return true;
			else if(a.PartNumber>b.PartNumber) return false;
			else return false;
		}

		public static bool operator>(ResponseViewModel a,ResponseViewModel b)
		{
			if(a.Date<b.Date) return false;
			else if(a.Date>b.Date) return true;
			else if(a.ResNumber<b.ResNumber) return false;
			else if(a.ResNumber>b.ResNumber) return true;
			else if(a.PartNumber<b.PartNumber) return false;
			else if(a.PartNumber>b.PartNumber) return true;
			else return false;
		}
	}
}
