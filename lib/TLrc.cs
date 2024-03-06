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
		// private Regex _lrcWText2 = LrcWTextRegex2();
		private Regex _lrcEText1 = LrcETextRegex1();
		// private Regex _lrcEText2 = LrcETextRegex2();

		private Dictionary<string, string> _meta = new()
		{
			{"ar", "artist"},
			{"al", "album"},
			{"ti", "title"},
			{"au", "author"},
			{"lr", "lyricists"},
			{"length", "length"},
			{"by", "creator"},
			{"offset", "offset"},
			{"re", "application"},
			{"tool", "tool"},
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
				// TODO: change library to https://github.com/CharsetDetector/UTF-unknown?
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
				uint? eol_time = null;
				while (sr.Peek() >= 0)
                {
                    string? line = sr.ReadLine();
					if (line == null) continue;

					if (eol_time != null)
					{
						kmlyric.words.Last().etime = eol_time;
						eol_time = null;
					}

					Match matchLineTag = _lrcTag.Match(line);
					// meta
					if (matchLineTag.Groups.Count > 1)
					{
						string metaName = _meta[matchLineTag.Groups[1].Value];
						string tagValue = matchLineTag.Groups[2].Value;
						kmlyric.meta.GetType().GetProperty(metaName)?.SetValue(kmlyric.meta, tagValue);
					}
					// words
					else
					{
						KMLyric.LyricWord startWord = new(){times = []};
						// KMLyric.Gender lineGender = KMLyric.Gender.none;

						// ???
						// [00:00.00] <00:00.04> When <00:00.16> the <00:05.92> lies ",
						// enhanced1+w2: [02:13.12]M:<02:14.00>text<02:14.17>text<02:14.19>
						// enhanced2:   [02:13.12]<02:14.00>text<02:14.17>text<02:14.19>
						foreach (string block in line.Split(']', StringSplitOptions.RemoveEmptyEntries))
						{
							Match matchTime = _lrcSTime.Match(block);
							// times:
							// [02:13.12]text
							// or
							// [02:13.12][03:13.12][04:13.12]text
							if (matchTime.Groups.Count > 1)
							{
								uint ms = time2ms(matchTime.Groups[1].Value,
									matchTime.Groups[2].Value, matchTime.Groups[3].Value);
								if (startWord.stime == null) {
									startWord.stime = ms;
								} else {
									startWord.times.Add(ms);
								}
							}
							// minilyrics+enhanced: impossible !
							// [02:13.12][02:15.12][03:13.12]text<02:14.00>text<02:14.17>text<02:14.19>

							// [02:13.12][02:15.12][03:13.12]text
							// or
							// [02:13.12][02:15.12][03:13.12]F:text
							else if (startWord.times.Count > 0)
							{
								Match matchWT1 = _lrcWText1.Match(block);
								if (matchWT1.Groups.Count > 1)
								{
									startWord.gender = setGender(matchWT1.Groups[1].Value);
									startWord.word = matchWT1.Groups[2].Value;
								}
								else {
									startWord.word = block;
								}
								// startWord.raw = block;
								kmlyric.words.Add(startWord);
							}
							// enhanced: [02:13.12]text<02:14.00>text<02:14.17>text
							// or
							// enhanced: [02:13.12]<02:14.00>text<02:14.17>text
							// or
							// standard: [02:13.12]text
							else
							{
								uint? prev_time = null;
								foreach (string tblock in block.Split('>', StringSplitOptions.RemoveEmptyEntries))
								{
									// Match matchWText1 = _lrcWText1.Match(tblock);
									// Match matchWText2 = _lrcWText2.Match(tblock);
									Match matchEText1 = _lrcEText1.Match(tblock);
									// Match matchEText2 = _lrcEText2.Match(tblock);
									if (matchEText1.Groups.Count > 1)
									{
                                        // KMLyric.LyricWord mediumWord = new();
                                        // {
                                        //     stime = time2ms(matchEText1.Groups[2].Value,
										// 		matchEText1.Groups[3].Value, matchEText1.Groups[4].Value)
                                        // };
										if (matchEText1.Groups[1].Value != "")
										{
											kmlyric.words.Add(new KMLyric.LyricWord {
												word = matchEText1.Groups[1].Value,
												stime = (prev_time == null) ? startWord.stime : prev_time,
												// raw = tblock
											});
											prev_time = time2ms(matchEText1.Groups[2].Value, matchEText1.Groups[3].Value,
												matchEText1.Groups[4].Value);
											eol_time = prev_time;
										}
										else
										{
											prev_time = time2ms(matchEText1.Groups[2].Value, matchEText1.Groups[3].Value,
												matchEText1.Groups[4].Value);
										}
									}
									else if (prev_time != null)
									{
										kmlyric.words.Add(new KMLyric.LyricWord {
											word = tblock,
											stime = prev_time,
											// raw = tblock
										});
									}
									else
									{
										Match matchWT1 = _lrcWText1.Match(tblock);
										if (matchWT1.Groups.Count > 1)
										{
											startWord.gender = setGender(matchWT1.Groups[1].Value);
											startWord.word = matchWT1.Groups[2].Value;
										}
										else {
											startWord.word = block;
										}
										// startWord.word = tblock;
										// startWord.raw = tblock;
										kmlyric.words.Add(startWord);
									}
								}
							}
							// TODO: +45 msec in end time tag
							// enhanced: [02:13.12]text<02:14.00>text<02:14.17>text<02:14.21>
							// kmlyric.words.Add(startWord);
						}
					}
                }

				sr.Close();
				if (eol_time != null) {
					kmlyric.words.Last().etime = eol_time;
				}
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

		[GeneratedRegex(@"^\[(#|[a-z][a-z0-9]+):\s*([^\]]+)\]\s*")]
		private static partial Regex LrcTagRegex();

		// simple1: [02:13]text
		// simple2: [02:13.12]text
		// minilyrics: [02:13.12][02:15.12][03:13.12]text
		[GeneratedRegex(@"^\[([0-9]{2}):([0-9]{2})\.?([0-9]{0,2})$")]
		private static partial Regex LrcSTimeRegex();
		// walaoke: [02:13.12]F:text
		[GeneratedRegex(@"^([FMD]):\s*(.+)")]
		private static partial Regex LrcWTextRegex1();
		// walaoke: [02:13.12]F:<02:14.00>text
		// [GeneratedRegex(@"^([FMD]):$")]
		// private static partial Regex LrcWTextRegex2();
		// enhanced1: [02:13.12]text<02:14.00>text<02:14.17>text<02:14.19>
		// enhanced2: [02:13.12]<02:14.00>text<02:14.17>text<02:14.19>
		// [GeneratedRegex(@"^([^<]*)<([0-9]{2}):([0-9]{2})\.?([0-9]{,2})$")]
		[GeneratedRegex(@"^([^<]*)<([0-9]{2}):([0-9]{2})\.?([0-9]?[0-9]?)$")]
		private static partial Regex LrcETextRegex1();
		// [GeneratedRegex(@"^<([0-9]{2}):([0-9]{2})\.?([0-9]{,2})$")]
		// private static partial Regex LrcETextRegex2();
	}
}
