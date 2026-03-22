using IlScanner;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: IlScanner <plugin.dll> [plugin2.dll ...]");
    return 1;
}

var scanner = new PluginScanner(ScanPolicy.GameEditorDefault);

int totalErrors = 0;

foreach (string dllPath in args)
{
    Console.WriteLine($"Scanning: {dllPath}");

    if (!File.Exists(dllPath))
    {
        Console.Error.WriteLine($"  File not found: {dllPath}");
        totalErrors++;
        continue;
    }

    var violations = scanner.Scan(dllPath);

    if (violations.Count == 0)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  PASS — no violations found");
        Console.ResetColor();
        continue;
    }

    foreach (var v in violations)
    {
        Console.ForegroundColor = v.Severity == Severity.Error ? ConsoleColor.Red : ConsoleColor.Yellow;
        Console.Write($"  [{v.Severity}] ");
        Console.ResetColor();
        Console.WriteLine($"in {v.Method}");
        Console.WriteLine($"         {v.Description}");
    }

    int errors = violations.Count(v => v.Severity == Severity.Error);
    int warnings = violations.Count(v => v.Severity == Severity.Warning);
    Console.WriteLine($"  Total: {errors} error(s), {warnings} warning(s)");
    totalErrors += errors;
}

Console.WriteLine();
if (totalErrors > 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"REJECTED — {totalErrors} error(s) found");
    Console.ResetColor();
    return 1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("ACCEPTED — all plugins passed");
Console.ResetColor();
return 0;
