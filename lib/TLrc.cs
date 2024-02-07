using System.Text;
using System.Text.RegularExpressions;
using Karamad;
using Mozilla.NUniversalCharDet;

namespace KTypeClass
{
	public partial class KTypeLrc : Karamad.IKType
    {
		private readonly string[] _extensions = [".lrc"];
		private bool _hasBom = false;
		private Encoding _fileEncoding = Encoding.ASCII;

		private Regex _lrcTag = LrcTagRegex();
		private Regex _lrcSTime = LrcSTimeRegex();
		private Regex _lrcWText1 = LrcWTextRegex1();
		private Regex _lrcWText2 = LrcWTextRegex2();
		private Regex _lrcEText1 = LrcETextRegex1();
		private Regex _lrcEText2 = LrcETextRegex2();

		private Dictionary<string, string> _meta = new()
		{
			{"ar", "artist"},
			{"al", "album"},
			{"ti", "title"},
			{"au", "author"},
			{"length", "length"},
			{"by", "creator"},
			{"offset", "offset"},
			{"re", "application"},
			{"ve", "version"},
			{"#", "comment"}
		};

		// public bool DetectTypeByExtension(FileInfo file)
		// {
		// 	return _extensions.Contains(file.Extension);
		// }
		public string[] fileExtensions {get => _extensions;}

		public bool DetectTypeByContent(FileInfo file)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			byte[] autodetectBytes = new byte[64];
			// check BOM
			// TODO: check bom to utilities
			using (FileStream fs = new(file.FullName, FileMode.Open, FileAccess.Read))
			{
				foreach (EncodingInfo enc in Encoding.GetEncodings())
				{
					fs.Position = 0;
					byte[] encBom = enc.GetEncoding().GetPreamble();
					if (encBom.Length == 0) {continue;}
					byte[] fileBom = new byte[encBom.Length];
					fs.Read(fileBom, 0, fileBom.Length);
					if (Enumerable.SequenceEqual(encBom, fileBom))
					{
						_hasBom = true;
						_fileEncoding = enc.GetEncoding();
						break;
					}
				}
				// TODO: try autodetect encoding
				// TODO: autodetect encoding to utilities
				if (_fileEncoding == Encoding.ASCII)
				{
					fs.Position = 0;
					fs.Read(autodetectBytes, 0, autodetectBytes.Length);
					UniversalDetector Det = new(null);
					Det.HandleData(autodetectBytes, 0, autodetectBytes.Length);
					Det.DataEnd();
					string enc = Det.GetDetectedCharset();
					if (enc != null && enc != "Not supported") {
						// TODO: check for enc exists in Encoding
						_fileEncoding = Encoding.GetEncoding(enc);
					}
				}
				fs.Close();
			}

			KMLyric kmlyric = new();
			using (StreamReader sr = new(file.FullName, _fileEncoding, _hasBom))
			{
				while (sr.Peek() >= 0)
                {
                    string? line = sr.ReadLine();
					if (line == null) continue;

					Match matchLineTag = _lrcTag.Match(line);
					// meta
					if (matchLineTag.Groups.Count > 1)
					{
						string metaName = _meta[matchLineTag.Groups[1].Value];
						string tagValue = matchLineTag.Groups[2].Value;
						kmlyric.meta.GetType().GetProperty(metaName).SetValue(kmlyric.meta, tagValue);
					}
					// words
					else
					{
						KMLyric.LyricWord word = new();
						foreach (string block in line.Split(']'))
						{							
							Match matchTime = _lrcSTime.Match(block);
							if (matchTime.Groups.Count > 1)
							{
								uint ms = time2ms(matchTime.Groups[1].Value, 
									matchTime.Groups[2].Value, matchTime.Groups[3].Value);
								if (word.time == null) {
									word.time = ms;
								} else {
									word.times.Add(ms);
								}
							}
							else
							{
								// minilyrics: [02:13.12][02:15.12][03:13.12]F:text
								// enhanced1: [02:13.12]F:text<02:14.00>text<02:14.17>text<02:14.19>
								// enhanced2: [02:13.12]M:<02:14.00>text<02:14.17>text<02:14.19>
								// enhanced3: [02:13.12]<02:14.00>text<02:14.17>text<02:14.19>
								KMLyric.Gender gender = KMLyric.Gender.none;
								foreach (string tblock in block.Split('>'))
								{									
									Match matchWText1 = _lrcWText1.Match(tblock);
									Match matchWText2 = _lrcWText2.Match(tblock);		
									Match matchEText1 = _lrcEText1.Match(tblock);
									Match matchEText2 = _lrcEText2.Match(tblock);
									if (matchEText1.Groups.Count > 1)
									{
                                        KMLyric.LyricWord sword = new()
                                        {
                                            time = time2ms(matchEText1.Groups[2].Value,
												matchEText1.Groups[3].Value, matchEText1.Groups[4].Value)
                                        };
                                        Match matchWT2 = _lrcEText2.Match(matchEText1.Groups[1].Value);
										Match matchWT1 = _lrcEText1.Match(matchEText1.Groups[1].Value);
										if (matchWT2.Groups.Count > 1) {
											gender = setGender(matchWT2.Groups[1].Value);
										}
										else if (matchWT1.Groups.Count > 1)
										{
											gender = setGender(matchWT1.Groups[1].Value);
											sword.word = matchWT1.Groups[2].Value;
										}
									}
								}
							}
							kmlyric.words.Add(word);
						}
					}
                }

				sr.Close();
			}

			return true;
		}

		private KMLyric.Gender setGender (string g)
		{
			KMLyric.Gender gender = (g == "F") ? KMLyric.Gender.female : KMLyric.Gender.male;
			if (g == "D") { gender = KMLyric.Gender.duet; }
			return gender;
		}
		private uint time2ms (string min, string sec, string hs)
		{
			if (hs == "") { hs = "0"; }
			return Convert.ToUInt32(min) * 60000 
				+ Convert.ToUInt32(sec) * 1000 
				+ Convert.ToUInt32(hs) * 10;
		}
		
		[GeneratedRegex(@"^\[(#|[a-z][a-z0-9]+):([^\]]+)\]\s*")]
		private static partial Regex LrcTagRegex();
		
		// simple1: [02:13]text
		// simple2: [02:13.12]text
		// minilyrics: [02:13.12][02:15.12][03:13.12]text
		[GeneratedRegex(@"^\[([0-9]{2}):([0-9]{2})\.?([0-9]{0,2})$")]
		private static partial Regex LrcSTimeRegex();
		// walaoke: [02:13.12]F:text
		[GeneratedRegex(@"^([FMD]):(.+)")]
		private static partial Regex LrcWTextRegex1();
		// walaoke: [02:13.12]F:<02:14.00>text
		[GeneratedRegex(@"^([FMD]):$")]
		private static partial Regex LrcWTextRegex2();
		// enhanced1: [02:13.12]text<02:14.00>text<02:14.17>text<02:14.19>
		// enhanced2: [02:13.12]<02:14.00>text<02:14.17>text<02:14.19>
		[GeneratedRegex(@"^([^<]+)<([0-9]{2}):([0-9]{2})\.?([0-9]{,2})$")]
		private static partial Regex LrcETextRegex1();
		[GeneratedRegex(@"^<([0-9]{2}):([0-9]{2})\.?([0-9]{,2})$")]
		private static partial Regex LrcETextRegex2();
	}
}
