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

	enum LyricMeta {
		artist,
		album
	}
	public class KMLyric
	{
		public string artist {get; set;}
		// public meta MetaInfo
		// {

		// }
		private interface meta
		{
			public interface artist
			{
				const string name = "artist";
				string value {get; set;}
			}
			public interface album
			{
				const string name = "album";
				string value {get; set;}
			}
			public interface title
			{
				const string name = "title";
				string value {get; set;}
			}
			public interface author
			{
				const string name = "author";
				string value {get; set;}
			}
			public interface length
			{
				const string name = "length";
				string value {get; set;}
			}
			public interface creator
			{
				const string name = "creator";
				string value {get; set;}
			}
			public interface offset
			{
				const string name = "offset";
				string value {get; set;}
			}
			public interface application
			{
				const string name = "application";
				string value {get; set;}
			}
			public interface version
			{
				const string name = "version";
				string value {get; set;}
			}
			public interface language
			{
				const string name = "language";
				string value {get; set;}
			}

			// for Advanced Sub Station
			public interface resolutionx
			{
				const string name = "resolutionx";
				string value {get; set;}
			}
			public interface resolutiony
			{
				const string name = "resolutiony";
				string value {get; set;}
			}
			public interface timer
			{
				const string name = "timer";
				string value {get; set;}
			}
		}
	}
}
