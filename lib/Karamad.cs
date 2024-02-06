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
        public LyricWord[] words = { };
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
            // for Advanced Sub Station
            public string resolutionx {get; set;}
            public string resolutiony {get; set;}
            public string timer {get; set;}
        }
        public class LyricWord {}		
	}
}
