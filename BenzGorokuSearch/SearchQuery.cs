using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BenzGorokuSearch
{
	enum ExpressionType
	{
		Literal,And,Or,Not,Dummy
	}

	interface ConditionExpression
	{
		ExpressionType Type{get;}
		bool IsMatch(string text);
	}

	class LiteralExpression:ConditionExpression
	{

		public ExpressionType Type
		{
			get
			{
				return ExpressionType.Literal;
			}
		}

		public string Keyword{get;set;}

		public int IndexOf(string text,string searchText)
		{
			int findIndex=-1,searchTextIndex=0;
			for(int i=0;i<text.Length&&searchTextIndex<searchText.Length;++i){
				char currentChar=text[i],searchChar=searchText[searchTextIndex];
				if(((searchChar=='\u25CB'||searchChar=='\u25EF'||searchChar=='*')&&
					!char.IsWhiteSpace(currentChar)&&
					!char.IsControl(currentChar)&&
					!char.IsSeparator(currentChar))||
					char.ToUpper(searchChar)==char.ToUpper(currentChar)){
					if(findIndex==-1) findIndex=i;
					++searchTextIndex;
				}else{
					findIndex=-1;
					searchTextIndex=0;
				}
			}
			if(searchTextIndex<searchText.Length) findIndex=-1;
			return findIndex;
		}

		public bool IsMatch(string text)
		{
			return IndexOf(text,Keyword)!=-1;
		}

		public LiteralExpression(string keyword)
		{
			Keyword=keyword;
		}
	}

	class AndOperatorExpression:ConditionExpression
	{
		public ExpressionType Type
		{
			get
			{
				return ExpressionType.And;
			}
		}

		public ConditionExpression Operand1{get;set;}
		public ConditionExpression Operand2{get;set;}

		public bool IsMatch(string text)
		{
			return Operand1.IsMatch(text)&&Operand2.IsMatch(text);
		}

		public AndOperatorExpression(ConditionExpression op1,ConditionExpression op2)
		{
			Operand1=op1;
			Operand2=op2;
		}
	}

	class OrOperatorExpression:ConditionExpression
	{
		public ExpressionType Type
		{
			get
			{
				return ExpressionType.Or;
			}
		}

		public ConditionExpression Operand1{get;set;}
		public ConditionExpression Operand2{get;set;}

		public bool IsMatch(string text)
		{
			return Operand1.IsMatch(text)||Operand2.IsMatch(text);
		}

		public OrOperatorExpression(ConditionExpression op1,ConditionExpression op2)
		{
			Operand1=op1;
			Operand2=op2;
		}
	}

	class NotOperatorExpression:ConditionExpression
	{
		public ExpressionType Type
		{
			get
			{
				return ExpressionType.Not;
			}
		}

		public ConditionExpression Operand{get;set;}

		public bool IsMatch(string text)
		{
			return !Operand.IsMatch(text);
		}

		public NotOperatorExpression(ConditionExpression op)
		{
			Operand=op;
		}
	}

	class DummyExpression:ConditionExpression
	{
		public ExpressionType Type
		{
			get
			{
				return ExpressionType.Dummy;
			}
		}

		public bool Result{get;set;}

		public bool IsMatch(string text)
		{
			return Result;
		}

		public DummyExpression(bool result)
		{
			Result=result;
		}
	}

	static class ExpressionBuilder
	{

		//public static ConditionExpression Parse(string str)
		//{
		//	var quoting=0;
		//	var literal=false;
		//	var words=new List<string>();
		//	str=str.Trim();
		//	var startIndex=0;
		//	for(int i=0;i<str.Length;++i){
		//		if(quoting==0&&!literal){
		//			if(!char.IsWhiteSpace(str[i])){
		//				startIndex=i;
		//				literal=true;
		//				if(str[i]=='"'){
		//					++quoting;
		//					++startIndex;
		//				}
		//			}
		//		}else if(literal){
		//			if(quoting>0&&str[i]=='"'&&char.IsWhiteSpace(str[i+1])){

		//				quoting=false;
		//				literal=false;
		//				words.Add(str.Substring(startIndex,i-startIndex-1);
		//			}else if(char.IsWhiteSpace(str[i])){
		//				literal=false;
		//			}
		//		}

		//		if(str[i]=='"'&&((quoting&&char.IsWhiteSpace(str[i+1]))||!quoting)){
		//			quoting=!quoting;
		//		}
		//	}
		//}

		public static ConditionExpression Create(string str)
		{
			var literal=false;
			var words=new List<string>();
			str=str.Trim();
			if(str=="") return new DummyExpression(true);
			var startIndex=0;
			for(int i=0;i<str.Length;++i){
				if(!literal&&!char.IsWhiteSpace(str[i])){
					literal=true;
					startIndex=i;
				}else if(literal&&char.IsWhiteSpace(str[i])){
					words.Add(str.Substring(startIndex,i-startIndex));
					literal=false;
				}
			}
			if(literal) words.Add(str.Substring(startIndex,str.Length-startIndex));
			words=words.Select(w=>CharWidthNormalizer.Normalize(w)).ToList();
			ConditionExpression root=null;
			if(words.Count==1) root=new LiteralExpression(words.First());
			else{
				var last=new AndOperatorExpression(null,null);
				root=last;
				for(int i=0;i<words.Count;++i){
					last.Operand1=new LiteralExpression(words[i]);
					if(words.Count==i+2){
						last.Operand2=new LiteralExpression(words[i+1]);
						++i;
					}else{
						var newExpr=new AndOperatorExpression(null,null);
						last.Operand2=newExpr;
						last=newExpr;
					}
				}
			}
			return root;
		}

	}

	class SearchQuery
	{
		public ConditionExpression Condition{get;set;}
		public bool SearchFromSelectedDate{get;set;}
		public DateTime? StartDate{get;set;}
		public DateTime? EndDate{get;set;}
		public bool SearchFromSelectedParts{get;set;}
		public int[] Parts{get;set;}
		public bool SearchFromSelectedPeople{get;set;}
		public string[] People{get;set;}

		public event Action<ResponseViewModel[]> SearchCompleted;

		public SearchQuery()
		{
			Condition=new DummyExpression(true);
			SearchFromSelectedParts=false;
			Parts=new int[0];
			SearchFromSelectedPeople=false;
			People=new string[0];
			SearchFromSelectedDate=false;
			StartDate=null;
			EndDate=null;
		}

		public ResponseViewModel[] Search(ResponseViewModel[] source)
		{
			var searchTask1=new Task<ResponseViewModel[]>(SearchInternal,source.Take(source.Length/2).ToArray());
			var searchTask2=new Task<ResponseViewModel[]>(SearchInternal,source.Skip(source.Length/2).ToArray());
			searchTask1.Start();
			searchTask2.Start();
			Task.WaitAll(searchTask1,searchTask2);
			return searchTask1.Result.Concat(searchTask2.Result).ToArray();
		}

		public void SearchAsync(ResponseViewModel[] source)
		{
			if(SearchCompleted!=null) ThreadPool.QueueUserWorkItem(_=>SearchCompleted(Search(source)));
		}

		private ResponseViewModel[] SearchInternal(object source)
		{
			var result=(IEnumerable<ResponseViewModel>)source;
			if(SearchFromSelectedParts) result=result.Where(r=>Parts.Any(p=>p==r.PartNumber));
			if(SearchFromSelectedPeople) result=result.Where(r=>People.Any(p=>p==r.Tag));
			if(SearchFromSelectedDate&&(StartDate!=null||EndDate!=null)){
				var start=StartDate??DateTime.MinValue;
				var end=EndDate==null?DateTime.MaxValue:EndDate.Value.Date.Add(new TimeSpan(23,59,59));
				result=result.Where(r=>r.Date>=start&&r.Date<=end);
			}
			var buffer=new StringBuilder(1000);
			return result.Where(r=>
			{
				CharWidthNormalizer.NormalizeFast(r.Text,buffer);
				return Condition.IsMatch(CharWidthNormalizer.Normalize(buffer.ToString()));
			}).ToArray();
		}
	}
}
