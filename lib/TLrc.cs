using System.Text;
using System.Text.RegularExpressions;

namespace KTypeClass
{
	public partial class KTypeLrc : Karamad.IKType
    {
		private readonly string[] _extensions = [".lrc"];
		private bool _hasBom = false;
		private Encoding _fileEncoding = Encoding.ASCII;

		private Regex _lrcTag = LrcTagRegex();
		private Regex _lrcText = LrcTextRegex();

		private Dictionary<string, Karamad.LyricMeta> _meta = new()
		{
			{"ar", Karamad.LyricMeta.album},
			// {"ar", KMLyric.meta.artist.name},
			// {"al", KMLyric.meta.album.name},
			// {"ti", KMLyric.meta.title.name},
			// {"au", KMLyric.meta.author.name},
			// {"length", KMLyric.meta.length.name},
			// {"by", KMLyric.meta.creator.name},
			// {"offset", KMLyric.meta.offset.name},
			// {"re", KMLyric.meta.application.name},
			// {"ve", KMLyric.meta.version.name}
		};

		// public bool DetectTypeByExtension(FileInfo file)
		// {
		// 	return _extensions.Contains(file.Extension);
		// }
		public string[] fileExtensions {get => _extensions;}

		public bool DetectTypeByContent(FileInfo file)
		{
			// check BOM
			using (FileStream fs = new(file.FullName, FileMode.Open, FileAccess.Read))
			{
				foreach (EncodingInfo enc in Encoding.GetEncodings())
				{
					fs.Position = 0;
					byte[] encBom = enc.GetEncoding().GetPreamble();
					byte[] fileBom = new byte[encBom.Length];
					fs.Read(fileBom, 0, fileBom.Length);
					if (Enumerable.SequenceEqual(encBom, fileBom))
					{
						_hasBom = true;
						_fileEncoding = enc.GetEncoding();
						break;
					}
				}
				fs.Close();
			}

			using (StreamReader sr = new(file.FullName, _fileEncoding, _hasBom))
			{
				while (sr.Peek() >= 0)
                {
                    string? line = sr.ReadLine();
					if (line == null) continue;

					Match matchLineTag = _lrcTag.Match(line);
					Match matchLineText = _lrcText.Match(line);
					if (matchLineTag.Groups.Count > 1)
					{
						KaramadLyric.
						string tagName = matchLineTag.Groups[1].Value;
						string tagValue = matchLineTag.Groups[2].Value;
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
