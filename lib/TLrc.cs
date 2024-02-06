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
		private Regex _lrcText = LrcTextRegex();

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
			{"ve", "version"}
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
			// KMLyric.LyriMmeta meta = new();
			// _fileEncoding = Encoding.GetEncoding(1251);
			using (StreamReader sr = new(file.FullName, _fileEncoding, _hasBom))
			{
				while (sr.Peek() >= 0)
                {
                    string? line = sr.ReadLine();
					if (line == null) continue;

					Match matchLineTag = _lrcTag.Match(line);
					Match matchLineText = _lrcText.Match(line);
					// meta
					if (matchLineTag.Groups.Count > 1)
					{
						string metaName = _meta[matchLineTag.Groups[1].Value];
						string tagValue = matchLineTag.Groups[2].Value;
						kmlyric.meta.GetType().GetProperty(metaName).SetValue(kmlyric.meta, tagValue);
					}
                }

				sr.Close();
			}

			return true;
		}

		[GeneratedRegex(@"^\[([a-z][a-z0-9]+):([^\]]+)\]\s*")]
		private static partial Regex LrcTagRegex();
		[GeneratedRegex(@"^\[([0-9][0-9:\.]+)](.+)\s*")]
		private static partial Regex LrcTextRegex();
	}
}
