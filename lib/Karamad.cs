using System.Reflection;

namespace Karamad
{
	public partial class Karamad
	{
		public static bool DetectType(FileInfo file)
		{
			IKType? DetectedClass = null;
			foreach (var KClass in Assembly.GetExecutingAssembly().GetTypes())
			{
				if (KClass.IsClass && typeof(IKType).IsAssignableFrom(KClass))
				{
					IKType KTClass = (IKType)Activator.CreateInstance(KClass);
					// TODO: в первую очередь проблуем определить тип по расширению
					// 	и, если не удастся, то по содержимому
					// if (KTClass!.DetectTypeByExtension(file) && KTClass!.DetectTypeByContent(file))
					if (KTClass!.fileExtensions.Contains(file.Extension) && KTClass!.DetectTypeByContent(file))
					{
						DetectedClass = KTClass;
						break;
					}
				}
			}
			if (DetectedClass == null)
			{
				foreach (var KClass in Assembly.GetExecutingAssembly().GetTypes())
				{
					if (KClass.IsClass && typeof(IKType).IsAssignableFrom(KClass))
					{
						IKType KTClass = (IKType)Activator.CreateInstance(KClass);
						// TODO: в первую очередь проблуем определить тип по расширению
						// 	и, если не удастся, то по содержимому
						if (KTClass!.DetectTypeByContent(file))
						{
							DetectedClass = KTClass;
							break;
						}
					}
				}
			}

			if (DetectType == null)
				Console.WriteLine("Error: Unknown karaoke type");
			return DetectType == null;
		}

		public static bool Convert(FileInfo outputFile)
		{

			return false;
		}
	}

	public interface IKType
	{
		// bool DetectTypeByExtension(FileInfo inputFile);
		string[] fileExtensions {get;}
		bool DetectTypeByContent(FileInfo inputFile);
	}

	public class KMLyric
	{
        public LyriMmeta meta = new();
        public List<LyricWord> words = [];
		public class LyriMmeta
        {
            public string artist {get; set;}
            public string album {get; set;}
            public string title {get; set;}
            public string author {get; set;}
            public string length {get; set;}
            public string creator {get; set;}
            public string offset {get; set;}
            public string application {get; set;}
            public string version {get; set;}
            public string language {get; set;}
			public string comment {get; set;}
            // for Advanced Sub Station
            public string resolutionx {get; set;}
            public string resolutiony {get; set;}
            public string timer {get; set;}
        }
        public class LyricWord 
		{
			public uint? time {get; set;}
			public string word {get; set;}
			// for MiniLyrics
			public List<uint> times {get; set;}
			// for Walaoke
			public Gender? gender {get; set;}
			// for UltraStar
			public WordType? type { get; set; }
			public WordNote? note { get; set; }
			// for Advanced Sub Station
			public uint? layer {get; set;}
			public string style {get; set;}
			// public string speakerName {get; set;}
			// left, right, vertical
			public uint[] margin {get; set;}
			public string effect {get; set;}
		}		
		public enum Gender { male, female, duet, none }
		// TODO: add more types
		public enum WordType { normal, golden, freestyle }
		// TODO: create notes
		public enum WordNote { c, c_dies, d, d_dies, e_minor}
	}
}
