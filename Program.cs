using System.CommandLine;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var fileOption = new Option<FileInfo?>(
            name: "-i",
            description: "input file") {IsRequired = true};

        var rootCommand = new RootCommand("Karaoke formats encoder/decoder/converter");
        rootCommand.AddOption(fileOption);

        return await rootCommand.InvokeAsync(args);
    }
}
