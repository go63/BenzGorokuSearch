using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;

namespace BenzGorokuSearch
{
	class UpdateInfo
	{
		public DateTime LatestDatabasePublishedDate{get;set;}
		public string LatestDatabaseUrl{get;set;}
		public Version LatestAppVersion{get;set;}
		public string LatestAppUrl{get;set;}

		public UpdateInfo()
		{
		}

		public UpdateInfo(DateTime dbDate,string dbUrl,Version appVersion,string appUrl)
		{
			LatestDatabasePublishedDate=dbDate;
			LatestDatabaseUrl=dbUrl;
			LatestAppVersion=appVersion;
			LatestAppUrl=appUrl;
		}
	}

	//アプリだけダウンロードしてしまった場合に最新版の語録のURLを開くかどうか聞く
	static class UpdateChecker
	{
		//BANされたら順次消して新しいURLを追加する
		public static string[] updateInfoUrls=new[]{
			"http://dl.dropboxusercontent.com/s/8s5z0tdqvpncyje/updates.txt",
			"http://dl.dropboxusercontent.com/s/67tk4szw91h9i4m/updates.txt"
		};

		public static UpdateInfo GetUpdateInfo()
		{
			try{
				var updateInfo=new UpdateInfo();
				var webClient=new WebClient();
				string textData=null;
				foreach(var url in updateInfoUrls){
					try{
						textData=Encoding.UTF8.GetString(webClient.DownloadData(url));
						break;
					}catch(Exception){
						continue;
					}
				}
				if(textData==null) return null;
				var textInfo=textData.Split(new[]{"\r\n"},StringSplitOptions.RemoveEmptyEntries)
					.Select(l=>l.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries));
				foreach(var info in textInfo){
					switch(info[0]){
					case "database":
						updateInfo.LatestDatabasePublishedDate=DateTime.Parse(info[1]);
						updateInfo.LatestDatabaseUrl=info[2];
						break;
					case "app":
						updateInfo.LatestAppVersion=Version.Parse(info[1]);
						updateInfo.LatestAppUrl=info[2];
						break;
					}
				}
				return updateInfo;
			}catch(Exception){
				return null;
			}
		}

		public static bool IsNewerThanCurrentVersion(Version latestVersion)
		{
			var executingAssembly=Assembly.GetExecutingAssembly();
			var version=executingAssembly.GetName().Version;
			return version<latestVersion;
		}
	}
}
