using System.CommandLine;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var inputFile = new Option<FileInfo?>(
            name: "-i",
            description: "input file") {IsRequired = true};
        var outputFile = new Argument<FileInfo?>("output", () => { return null; }, "output file");

        var rootCommand = new RootCommand("Karaoke formats encoder/decoder/converter");
        rootCommand.AddOption(inputFile);
        rootCommand.AddArgument(outputFile);
        rootCommand.SetHandler((ifile, ofile) => {
            ReadFile(ifile!, ofile!);
        }, inputFile, outputFile);

        return await rootCommand.InvokeAsync(args);
    }

    static void ReadFile(FileInfo ifile, FileInfo ofile)
    {
        if (!ifile.Exists) {
            ConsoleColor PrevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: input file not exists - " + ifile.FullName);
            Console.ForegroundColor = PrevColor;
            Environment.Exit(0);
        }
        if (Karamad.Karamad.DetectType(ifile) && ofile != null)
        {
            Karamad.Karamad.Convert(ofile);
        }
    }
}
