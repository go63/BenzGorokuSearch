using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BenzGorokuSearch
{
	static class CharWidthNormalizer
	{
		public static readonly char[][] KanaTable=new[]{
			new[]{'ア','ｱ'},
			new[]{'イ','ｲ'},
			new[]{'ウ','ｳ'},
			new[]{'エ','ｴ'},
			new[]{'オ','ｵ'},
			
			new[]{'カ','ｶ'},
			new[]{'キ','ｷ'},
			new[]{'ク','ｸ'},
			new[]{'ケ','ｹ'},
			new[]{'コ','ｺ'},

			new[]{'サ','ｻ'},
			new[]{'シ','ｼ'},
			new[]{'ス','ｽ'},
			new[]{'セ','ｾ'},
			new[]{'ソ','ｿ'},

			new[]{'タ','ﾀ'},
			new[]{'チ','ﾁ'},
			new[]{'ツ','ﾂ'},
			new[]{'テ','ﾃ'},
			new[]{'ト','ﾄ'},
			
			new[]{'ナ','ﾅ'},
			new[]{'ニ','ﾆ'},
			new[]{'ヌ','ﾇ'},
			new[]{'ネ','ﾈ'},
			new[]{'ノ','ﾉ'},

			new[]{'ハ','ﾊ'},
			new[]{'ヒ','ﾋ'},
			new[]{'フ','ﾌ'},
			new[]{'ヘ','ﾍ'},
			new[]{'ホ','ﾎ'},

			new[]{'マ','ﾏ'},
			new[]{'ミ','ﾐ'},
			new[]{'ム','ﾑ'},
			new[]{'メ','ﾒ'},
			new[]{'モ','ﾓ'},

			new[]{'ラ','ﾗ'},
			new[]{'リ','ﾘ'},
			new[]{'ル','ﾙ'},
			new[]{'レ','ﾚ'},
			new[]{'ロ','ﾛ'},

			new[]{'ヤ','ﾔ'},
			new[]{'ユ','ﾕ'},
			new[]{'ヨ','ﾖ'},

			new[]{'ワ','ﾜ'},
			new[]{'ヲ','ｦ'},
			new[]{'ン','ﾝ'},

			new[]{'ァ','ｧ'},
			new[]{'ィ','ｨ'},
			new[]{'ゥ','ｩ'},
			new[]{'ェ','ｪ'},
			new[]{'ォ','ｫ'},

			new[]{'ャ','ｬ'},
			new[]{'ュ','ｭ'},
			new[]{'ョ','ｮ'},
			new[]{'ッ','ｯ'},
		};

		public static readonly char[][] VoicedKanaTable=new[]{
			new[]{'ガ','ｶ','ﾞ'},
			new[]{'ギ','ｷ','ﾞ'},
			new[]{'グ','ｸ','ﾞ'},
			new[]{'ゲ','ｹ','ﾞ'},
			new[]{'ゴ','ｺ','ﾞ'},

			new[]{'ザ','ｻ','ﾞ'},
			new[]{'ジ','ｼ','ﾞ'},
			new[]{'ズ','ｽ','ﾞ'},
			new[]{'ゼ','ｾ','ﾞ'},
			new[]{'ゾ','ｿ','ﾞ'},

			new[]{'ダ','ﾀ','ﾞ'},
			new[]{'ヂ','ﾁ','ﾞ'},
			new[]{'ヅ','ﾂ','ﾞ'},
			new[]{'デ','ﾃ','ﾞ'},
			new[]{'ド','ﾄ','ﾞ'},

			new[]{'バ','ﾊ','ﾞ'},
			new[]{'ビ','ﾋ','ﾞ'},
			new[]{'ブ','ﾌ','ﾞ'},
			new[]{'ベ','ﾍ','ﾞ'},
			new[]{'ボ','ﾎ','ﾞ'},

			new[]{'ヴ','ｳ','ﾞ'},
		};

		public static readonly char[][] SemiVoicedKanaTable=new[]{
			new[]{'パ','ﾊ','ﾟ'},
			new[]{'ピ','ﾋ','ﾟ'},
			new[]{'プ','ﾌ','ﾟ'},
			new[]{'ペ','ﾍ','ﾟ'},
			new[]{'ポ','ﾎ','ﾟ'},
		};

		static char ToHalfWidthAscii(char a)
		{
			if(a=='＼') return a;
			else if(a>='！'&&a<='～') return (char)(a-0xFEE0);
			else return a;
		}

		static char ToFullWidthKana(char a1,char a2=Char.MinValue)
		{
			char[] result=null;
			if(a2=='ﾞ'){
				result=VoicedKanaTable.FirstOrDefault(k=>k[1]==a1)??KanaTable.FirstOrDefault(k=>k[1]==a1);
				return result==null?a1:result[0];
			}else if(a2=='ﾟ'){
				result=SemiVoicedKanaTable.FirstOrDefault(k=>k[1]==a1)??KanaTable.FirstOrDefault(k=>k[1]==a1);
				return result==null?a1:result[0];
				//半角長音記号'ｰ'も変換対象にする？
			}else if(a1=='ﾞ'||a1=='ﾟ'){
				return a1=='ﾞ'?'゛':'゜';
			}else{
				result=KanaTable.FirstOrDefault(k=>k[1]==a1);
				return result==null?a1:result[0];
			}
		}

		public static string Normalize(string str)
		{
			var buffer=new StringBuilder();
			return NormalizeFast(str,buffer).ToString();
		}

		public static StringBuilder NormalizeFast(string str,StringBuilder buffer)
		{
			buffer.Clear();
			buffer.EnsureCapacity(str.Length);
			for(int i=0;i<str.Length;++i){
				if(str[i]>='ｦ'&&str[i]<='ﾟ'){
					if(i<str.Length-1&&((VoicedKanaTable.Any(k=>k[1]==str[i])&&str[i+1]=='ﾞ')||(SemiVoicedKanaTable.Any(k=>k[1]==str[i])&&str[i+1]=='ﾟ'))){
						buffer.Append(ToFullWidthKana(str[i],str[i+1]));
						++i;
					}else buffer.Append(ToFullWidthKana(str[i]));
				}else if(str[i]>='！'&&str[i]<='～') buffer.Append(ToHalfWidthAscii(str[i]));
				else buffer.Append(str[i]);
			}
			return buffer;
		}
	}
}
