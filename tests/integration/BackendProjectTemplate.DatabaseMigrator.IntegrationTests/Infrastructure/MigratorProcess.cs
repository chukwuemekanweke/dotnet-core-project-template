using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;

/// <summary>
/// Launches the real, built DatabaseMigrator executable as a child process (the same entry point the container
/// runs) and reports its exit code. A process that does not exit within the timeout fails the test, which is how
/// these tests prove the migrator no longer keeps running after its work is done.
/// </summary>
public static class MigratorProcess
{
    public const string TestEnvironment = "IntegrationTest";

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

    public static string BuildOutputDirectory { get; } = ResolveBuildOutputDirectory();

    public static async Task<Result> RunAsync(
        string? connectionString,
        string? workingDirectory = null,
        string environment = TestEnvironment)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            WorkingDirectory = workingDirectory ?? BuildOutputDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(Path.Combine(BuildOutputDirectory, "BackendProjectTemplate.DatabaseMigrator.dll"));

        foreach (var inherited in startInfo.Environment.Keys
                     .Where(key => key.StartsWith("ConnectionStrings__", StringComparison.OrdinalIgnoreCase)
                                   || key.Equals("ASPNETCORE_ENVIRONMENT", StringComparison.OrdinalIgnoreCase)
                                   || key.Equals("DOTNET_ENVIRONMENT", StringComparison.OrdinalIgnoreCase))
                     .ToArray())
        {
            startInfo.Environment.Remove(inherited);
        }

        startInfo.Environment["DOTNET_ENVIRONMENT"] = environment;
        if (connectionString is not null)
        {
            startInfo.Environment["ConnectionStrings__PostgresWrite"] = connectionString;
        }

        var output = new StringBuilder();
        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, line) => Append(output, line.Data);
        process.ErrorDataReceived += (_, line) => Append(output, line.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeout = new CancellationTokenSource(DefaultTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"DatabaseMigrator did not exit within {DefaultTimeout}. Output:{Environment.NewLine}{Read(output)}");
        }

        return new Result(process.ExitCode, Read(output));
    }

    public static string CreateContentRoot(string stage, string scriptFileName, string sql)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), $"migrator-content-root-{Guid.CreateVersion7():N}");
        var stageDirectory = Directory.CreateDirectory(Path.Combine(contentRoot, "Scripts", stage));
        File.WriteAllText(Path.Combine(stageDirectory.FullName, scriptFileName), sql);
        return contentRoot;
    }

    private static void Append(StringBuilder output, string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (output)
        {
            output.AppendLine(line);
        }
    }

    private static string Read(StringBuilder output)
    {
        lock (output)
        {
            return output.ToString();
        }
    }

    private static string ResolveBuildOutputDirectory()
    {
        var configuration = typeof(MigratorProcess).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Debug";
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BackendProjectTemplate.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not locate the repository root (BackendProjectTemplate.slnx).");
        }

        var output = Path.Combine(directory.FullName, "src", "BackendProjectTemplate.DatabaseMigrator", "bin", configuration, "net10.0");
        if (!File.Exists(Path.Combine(output, "BackendProjectTemplate.DatabaseMigrator.dll")))
        {
            throw new InvalidOperationException($"DatabaseMigrator build output was not found at '{output}'. Build the solution first.");
        }

        return output;
    }

    public sealed record Result(int ExitCode, string Output);
}
