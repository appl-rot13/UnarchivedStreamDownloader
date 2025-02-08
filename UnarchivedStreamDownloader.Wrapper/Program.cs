
using System.Diagnostics;

using UnarchivedStreamDownloader.Wrapper.Utilities.Extensions;

if (args.Length < 2)
{
    return -1;
}

try
{
    var filePath = args[0];
    var arguments = string.Join(' ', args[1..].Select(s => s.Contains(' ') ? s.DoubleQuoted() : s));

    var process =
        new Process
        {
            StartInfo =
                new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                },
        };

    var keyboardInterrupted = false;
    process.ErrorDataReceived += (_, e) =>
    {
        if (string.IsNullOrEmpty(e.Data))
        {
            return;
        }

        if (e.Data.Contains("ERROR: Interrupted by user"))
        {
            keyboardInterrupted = true;
            return;
        }

        Console.WriteLine(e.Data);
    };

    if (!process.Start())
    {
        throw new InvalidOperationException($"'{filePath}' could not be started.");
    }

    Console.CancelKeyPress += (_, e) => e.Cancel = true;

    process.BeginErrorReadLine();
    process.WaitForExit();

    if (keyboardInterrupted)
    {
        return 0;
    }

    if (process.ExitCode != 0)
    {
        Console.ReadLine();
    }

    return process.ExitCode;
}
catch (Exception e)
{
    Console.WriteLine(e);
    Console.ReadLine();
    return -1;
}
