
using System.Text;
using Mozilla.NUniversalCharDet;

namespace KTypeClass
{
    public class KTypeKarafun : IKType
    {
        private readonly string[] _extensions = [".kfn"];
        // KFNB
        private readonly byte[] _signatures = [0x4B, 0x46, 0x4E, 0x42];
        private Dictionary<string, string> properties = new();
        private List<ResourceFile> resources = new();
        private List<string> unknownProperties = new();
        private long endOfHeaderOffset;
        private long endOfPropsOffset;
        // US-ASCII
        private int resourceNamesEncodingAuto = 20127;
        private string autoDetectEncoding;
        private Dictionary<string, string> propsDesc = new()
        {
            {"DIFM", "Man difficult"},
            {"DIFW", "Woman difficult"},
            {"GNRE", "Genre"},
            {"SFTV", "SFTV"},
            {"MUSL", "MUSL"},
            {"ANME", "ANME"},
            {"TYPE", "TYPE"},
            {"FLID", "AES-ECB-128 Key"},
            {"TITL", "Title"},
            {"ARTS", "Artist"},
            {"ALBM", "Album"},
            {"COMP", "Composer"},
            {"SORC", "Source"},
            {"TRAK", "Track number"},
            {"RGHT", "RGHT"},
            {"COPY", "Copyright"},
            {"COMM", "Comment"},
            {"PROV", "PROV"},
            {"IDUS", "IDUS"},
            {"LANG", "Language"},
            {"KFNZ", "KFN Author"},
            {"YEAR", "Year"},
            {"KARV", "Karaoke version"},
            {"VOCG", "Lead vocal"}
        };
        private Dictionary<int, string> fileTypes = new()
        {
            {0, "Text"},
            {1, "Config"},
            {2, "Audio"},
            {3, "Image"},
            {4, "Font"},
            {5, "Video"},
            {6, "Visualization"}
        };
        public bool CheckType(FileInfo file)
        {        
            int filesEncoding = 0;
            using (FileStream fs = new(file.FullName, FileMode.Open, FileAccess.Read))
            {
                byte[] signature = new byte[4];
                fs.Read(signature, 0, signature.Length);
                if (!Enumerable.SequenceEqual(signature, _signatures))
                {
                    return false;
                }

                byte[] prop = new byte[5];
                byte[] propValue = new byte[4];
                int maxProps = 100;
                while (maxProps > 0)
                {
                    fs.Read(prop, 0, prop.Length);
                    string propName = new(Encoding.UTF8.GetChars(new ArraySegment<byte>(prop, 0, 4).ToArray()));
                    if (propName == "ENDH")
                    {
                        fs.Position += 4;
                        break;
                    }
                    string SpropName = GetPropDesc(propName);
                    
                    fs.Read(propValue, 0, propValue.Length);
                    if (prop[4] == 1)
                    {
                        if (SpropName == "Genre" && BitConverter.ToUInt32(propValue, 0) == 0xffffffff)
                        {
                            this.properties.Add(SpropName, "Not set");
                        }
                        else
                        {
                            if (SpropName.Contains("unknown"))
                            {
                                this.unknownProperties.Add(SpropName + ": " + BitConverter.ToUInt32(propValue, 0));
                            }
                            if (propName != SpropName)
                            {
                                this.properties.Add(SpropName, BitConverter.ToUInt32(propValue, 0).ToString());
                            }
                        }
                    }
                    else if (prop[4] == 2)
                    {
                        byte[] value = new byte[BitConverter.ToUInt32(propValue, 0)];
                        fs.Read(value, 0, value.Length);
                        if (SpropName == "AES-ECB-128 Key")
                        {
                            string val = (value.Select(b => (int)b).Sum() == 0)
                                ? "Not present"
                                : value.Select(b => b.ToString("X2")).Aggregate((s1, s2) => s1 + s2);
                            this.properties.Add(SpropName, val);
                        }
                        else
                        {
                            if (SpropName.Contains("unknown"))
                            {
                                this.unknownProperties.Add(SpropName + ": " + new string(Encoding.UTF8.GetChars(value)));
                            }
                            if (propName != SpropName)
                            {
                                this.properties.Add(SpropName, new string(Encoding.UTF8.GetChars(value)));
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Karafun format error: unknown property block type - " + prop[4]);
                        return true;
                    }
                    maxProps--;
                }
                this.endOfPropsOffset = fs.Position;
        
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                byte[] numOfResources = new byte[4];
                fs.Read(numOfResources, 0, numOfResources.Length);
                int resourcesCount = BitConverter.ToInt32(numOfResources, 0);
                while (resourcesCount > 0)
                {
                    byte[] resourceNameLenght = new byte[4];
                    byte[] resourceType = new byte[4];
                    byte[] resourceLenght = new byte[4];
                    byte[] resourceEncryptedLenght = new byte[4];
                    byte[] resourceOffset = new byte[4];
                    byte[] resourceEncrypted = new byte[4];

                    fs.Read(resourceNameLenght, 0, resourceNameLenght.Length);
                    byte[] resourceName = new byte[BitConverter.ToUInt32(resourceNameLenght, 0)];
                    fs.Read(resourceName, 0, resourceName.Length);
                    fs.Read(resourceType, 0, resourceType.Length);
                    fs.Read(resourceLenght, 0, resourceLenght.Length);
                    fs.Read(resourceOffset, 0, resourceOffset.Length);
                    fs.Read(resourceEncryptedLenght, 0, resourceEncryptedLenght.Length);
                    fs.Read(resourceEncrypted, 0, resourceEncrypted.Length);
                    int encrypted = BitConverter.ToInt32(resourceEncrypted, 0);

                    if (filesEncoding == 0 && resourceNamesEncodingAuto == 20127)
                    {
                        UniversalDetector Det = new(null);
                        Det.HandleData(resourceName, 0, resourceName.Length);
                        Det.DataEnd();
                        string enc = Det.GetDetectedCharset();
                        if (enc != null && enc != "Not supported")
                        {
                            // fix encoding for 1251 upper case and MAC
                            if (enc == "KOI8-R" || enc == "X-MAC-CYRILLIC") { enc = "WINDOWS-1251"; }
                            Encoding denc = Encoding.GetEncoding(enc);
                            resourceNamesEncodingAuto = denc.CodePage;
                            this.autoDetectEncoding = denc.CodePage + ": " + denc.EncodingName;
                        }
                        else if (enc == null)
                        {
                            Encoding denc = Encoding.GetEncoding(resourceNamesEncodingAuto);
                            this.autoDetectEncoding = denc.CodePage + ": " + denc.EncodingName;
                        }
                        else
                        {
                            this.autoDetectEncoding = "No supported: use " + Encoding.GetEncoding(resourceNamesEncodingAuto).EncodingName;
                        }
                    }

                    int useEncoding = (filesEncoding != 0) ? filesEncoding : resourceNamesEncodingAuto;
                    string fName = new(Encoding.GetEncoding(useEncoding).GetChars(resourceName));

                    this.resources.Add(new ResourceFile(
                        this.GetFileType(resourceType),
                        fName,
                        BitConverter.ToInt32(resourceEncryptedLenght, 0),
                        BitConverter.ToInt32(resourceLenght, 0),
                        BitConverter.ToInt32(resourceOffset, 0),
                        (encrypted == 0) ? false : true,
                        (fName == this.GetAudioSourceName()) ? true : false
                    ));

                    resourcesCount--;
                }
                this.endOfHeaderOffset = fs.Position;

                Console.WriteLine("Format: KaraFun");
                Console.WriteLine("Properties:");
                foreach (var p in this.properties)
                {
                    Console.WriteLine("\t" + p.Key + ": " + p.Value);
                }
                Console.WriteLine("Resources:");
                foreach (var p in this.resources)
                {
                    Console.WriteLine("\t" + p.FileType + ": " + p.FileName);
                }
            }
            return true;
        }

        public string GetPropDesc(string PropName)
        {
            if (propsDesc.ContainsKey(PropName)) { return propsDesc[PropName]; }
            return "(unknown) " + PropName;
        }
        public string GetFileType(byte[] type)
        {
            int ftype = BitConverter.ToInt32(type, 0);
            if (fileTypes.ContainsKey(ftype)) { return fileTypes[ftype]; }
            return "Unknown (" + ftype + ")";
        }
        public string? GetAudioSourceName()
        {
            if (this.properties.Count == 0) { return null; }
            //1,I,ddt_-_chto_takoe_osen'.mp3
            KeyValuePair<string, string> sourceProp = this.properties.Where(kv => kv.Key == "Source").FirstOrDefault();
            return sourceProp.Value.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Last();
        }
    }

    public class ResourceFile
    {
        private string Type;
        private string Name;
        private int EncryptedLength;
        private int Length;
        private int Offset;
        private bool Encrypted;

        private bool Exported;
        private bool IsAudioSource;

        public string FileType
        {
            get { return this.Type; }
        }
        public string FileName
        {
            get { return this.Name; }
        }
        public int EncLength
        {
            get { return this.EncryptedLength; }
        }
        public int FileLength
        {
            get { return this.Length; }
        }
        public string FileSize
        {
            get
            {
                string[] Suffixes = { "b", "Kb", "Mb" };
                int i = 0;
                decimal dVal = (decimal)this.Length;
                while (Math.Round(dVal, 1) >= 1000)
                {
                    dVal /= 1024;
                    i++;
                }
                return String.Format("{0:n" + 1 + "} {1}", dVal, Suffixes[i]);
            }
        }
        public int FileOffset
        {
            get { return this.Offset; }
        }
        public bool IsEncrypted
        {
            get { return this.Encrypted; }
        }
        public bool IsExported
        {
            get {
                return (this.FileType == "Config" || this.IsAudioSource) ? true : this.Exported;
            }
            set {
                if (this.FileType != "Config" && !this.IsAudioSource) { this.Exported = value; }
            }
        }

        public ResourceFile(string type, string name, int enclength, int length, int offset, bool encrypted, bool aSource = false)
        {
            this.Type = type;
            this.Name = name;
            this.EncryptedLength = enclength;
            this.Length = length;
            this.Offset = offset;
            this.Encrypted = encrypted;
            this.IsAudioSource = aSource;
        }
    }
}