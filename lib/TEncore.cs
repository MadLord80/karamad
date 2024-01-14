using System.IO.Compression;
using System.Text;

namespace KTypeClass
{
    public class KTypeEncore()
    {
        private static string[] _extensions = [".emz", ".emp"];
        public bool CheckType(FileInfo file)
        {     
            
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);  

            // sorry
            if (file.Extension == ".emp")
            {
                Console.WriteLine("Format: Encore");
                Console.WriteLine("Error: EMP files are not supported yet, sorry");
                return true;
            }

            if (file.Extension == ".emz")
            {
                try
                {
                    using (var zipFile = ZipFile.Open(file.FullName, ZipArchiveMode.Read, Encoding.GetEncoding(866)))
                    {
                        Console.WriteLine("Format: Encore");    
                        Console.WriteLine("Resources:");
                        foreach (var entrie in zipFile.Entries)
                        {
                            Console.WriteLine("\t" + entrie.Name);
                        }
                        return true;
                    }
                }
                catch (InvalidDataException)
                {
                    Console.WriteLine("Format: Encore");
                    Console.WriteLine("Error: file is encrypted");
                    return false;
                }
            }

            return false;
        }
    }
}