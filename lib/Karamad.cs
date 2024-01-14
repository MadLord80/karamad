using System.Reflection;

public interface IKType
{
    bool CheckType(FileInfo inputFile);
}
public partial class Karamad
{
    public static bool DetectType(FileInfo file)
    {
        foreach (var KClass in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (KClass.IsClass && typeof(IKType).IsAssignableFrom(KClass))
            {
                var obj = (IKType)Activator.CreateInstance(KClass);
                if (obj!.CheckType(file))
                    return true;
            }
        }
        Console.WriteLine("Error: Unknown karaoke type");
        return false;
    }
    
    public static bool Convert(FileInfo outputFile)
    {
        
        return false;
    }
}