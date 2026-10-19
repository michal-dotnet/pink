using System.CommandLine;
var rootCommand = new RootCommand("root command for file bundle CLI");
var bundlecommand = new Command("bundle", "bundle code file to a single file");
var outputOption = new Option<FileInfo>("--output", "-o"); 
var noteOption = new Option<bool>("--note","-n");
var languageOption = new Option<string>("--language","-l"){Required = true};
var sourceOption = new Option<DirectoryInfo>("--source","-s"){Required = true};
var sortOption = new Option<string>("--sort","-t");
var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines","-r");
var authorOption = new Option<string>("--author","-a");
var createRspCommand = new Command( "create-rsp");

bundlecommand.Options.Add(languageOption);
bundlecommand.Options.Add(sourceOption);
bundlecommand.Options.Add(outputOption);
bundlecommand.Options.Add(noteOption);
bundlecommand.Options.Add(sortOption);
bundlecommand.Options.Add(removeEmptyLinesOption);
bundlecommand.Options.Add(authorOption);
rootCommand.Subcommands.Add(createRspCommand);
rootCommand.Subcommands.Add(bundlecommand);


bundlecommand.SetAction(parseResult =>
{
    DirectoryInfo source = parseResult.GetValue(sourceOption);
    string language = parseResult.GetValue(languageOption);
    FileInfo output = parseResult.GetValue(outputOption)!;
    bool note = parseResult.GetValue(noteOption);
    string sort = parseResult.GetValue(sortOption) ?? "name";
    bool removeEmptyLines = parseResult.GetValue(removeEmptyLinesOption);
    string author = parseResult.GetValue(authorOption);
    Bundle(language, source, output, note,sort, removeEmptyLines, author);
});
static void Bundle(string language, DirectoryInfo source,FileInfo output,bool note,string sort,bool removeEmptyLines,string author)
{
    //ולידציה 
    if (!source.Exists)
    {
        Console.WriteLine("Error: Source directory does not exist.");
        return;
    }

    if (language != "all" &&
        language != "cs" &&
        language != "py" &&
        language != "js")
    {
        Console.WriteLine("Error: Unsupported language.");
        return;
    }

    if (sort != "name" && sort != "type")
    {
        Console.WriteLine("Error: Sort must be 'name' or 'type'.");
        return;
    }





    string[] files = Directory.GetFiles(
     source.FullName,
     "*",
     SearchOption.AllDirectories
 )
 .Where(f => !f.Split(Path.DirectorySeparatorChar)
     .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase)
               || part.Equals("obj", StringComparison.OrdinalIgnoreCase)
               || part.Equals("debug", StringComparison.OrdinalIgnoreCase)))
 .ToArray();
    if (sort == "name")
        files = files.OrderBy(f => Path.GetFileName(f)).ToArray();

    else if (sort == "type")
        files = files.OrderBy(f => Path.GetExtension(f)).ToArray();
    using StreamWriter fileoutput = new StreamWriter(output.FullName);
    if (!string.IsNullOrWhiteSpace(author))
    {
        fileoutput.WriteLine($"// Author: {author}");
        fileoutput.WriteLine();
    }

    foreach (string file in files)
    {
        string extension = Path.GetExtension(file);
        if (language == "all")
        {
            if (extension == ".cs" || extension == ".py" || extension == ".js")
            {
                string content = File.ReadAllText(file);
                if (removeEmptyLines)
                {
                    content = string.Join(
                        Environment.NewLine,
                        content.Split(
                            new[] { "\r\n", "\n", "\r" },
                            StringSplitOptions.None
                        )
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                    );
                }
                fileoutput.WriteLine(content);
            }
        }
        else if (extension == "." + language)
        {
            string content = File.ReadAllText(file);
            if (removeEmptyLines)
            {
                content = string.Join(
                    Environment.NewLine,
                    content.Split(
                        new[] { "\r\n", "\n", "\r" },
                        StringSplitOptions.None
                    )
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                );
            }
            fileoutput.WriteLine(content);
        }
        if (note) {
            string comment = "";

            if (extension == ".cs" || extension == ".js")
                comment = "//";
            else if (extension == ".py")
                comment = "#";

            fileoutput.WriteLine($"{comment} File: {Path.GetFileName(file)}");
            fileoutput.WriteLine(($" Path: {file}"));
        }
    }
}

createRspCommand.SetAction(parseResult =>
{

    string fileName = Ask("Response file name (e.g. settings.rsp): ");
    string language = Ask("Language (cs/py/js/all): ");
    string source = Ask("Source directory: ");
    string output = Ask("Output file path: ");

    string sort = Ask("Sort (name/type, default name): ");
    if (string.IsNullOrWhiteSpace(sort))
        sort = "name";

    string noteAnswer = Ask("Add notes? (y/n): ");
    string emptyAnswer = Ask("Remove empty lines? (y/n): ");
    string author = Ask("Author (optional): ");

    // Validation
    if (string.IsNullOrWhiteSpace(fileName))
    {
        Console.WriteLine("Error: Response file name is required.");
        return;
    }

    if (language != "all" &&
        language != "cs" &&
        language != "py" &&
        language != "js")
    {
        Console.WriteLine("Error: Unsupported language.");
        return;
    }

    if (!Directory.Exists(source))
    {
        Console.WriteLine("Error: Source directory does not exist.");
        return;
    }

    if (sort != "name" && sort != "type")
    {
        Console.WriteLine("Error: Sort must be 'name' or 'type'.");
        return;
    }

    if (string.IsNullOrWhiteSpace(output))
    {
        Console.WriteLine("Error: Output path is required.");
        return;
    }

    var argsList = new List<string>
    {
        "bundle",
        "--language", language,
        "--source", source,
        "--output", output,
        "--sort", sort
    };

    if (noteAnswer.Trim().ToLower() == "y")
        argsList.Add("--note");

    if (emptyAnswer.Trim().ToLower() == "y")
        argsList.Add("--remove-empty-lines");

    if (!string.IsNullOrWhiteSpace(author))
    {
        argsList.Add("--author");
        argsList.Add(author);
    }

    try
    {
        File.WriteAllLines(
            fileName,
            argsList.Select(QuoteArgument)
        );

        Console.WriteLine($"Response file created: {fileName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating response file: {ex.Message}");
    }
    Console.WriteLine("Reached create-rsp setup");
});

static string Ask(string question)
{
    Console.Write(question);
    return Console.ReadLine()?.Trim() ?? "";
}

static string QuoteArgument(string value)
{
    return "\"" + value.Replace("\\", "\\\\")
                       .Replace("\"", "\\\"") + "\"";
}



rootCommand.Parse(args).Invoke();
