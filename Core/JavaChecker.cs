using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cynnet.Core;

public class JavaCheckResult
{
    public bool IsInstalled { get; set; }
    public string DisplayVersion { get; set; } = "Not found";
    public int MajorVersion { get; set; }
    public string RawOutput { get; set; } = "";
}

public static class JavaChecker
{
    public static async Task<JavaCheckResult> CheckAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = new Process();
                process.StartInfo = startInfo;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                string combined = (output + Environment.NewLine + error).Trim();

                if (string.IsNullOrWhiteSpace(combined))
                {
                    return new JavaCheckResult
                    {
                        IsInstalled = false,
                        DisplayVersion = "Not found",
                        MajorVersion = 0,
                        RawOutput = ""
                    };
                }

                int majorVersion = ParseMajorVersion(combined);

                return new JavaCheckResult
                {
                    IsInstalled = true,
                    DisplayVersion = majorVersion > 0 ? $"Java {majorVersion}" : "Java found",
                    MajorVersion = majorVersion,
                    RawOutput = combined
                };
            }
            catch
            {
                return new JavaCheckResult
                {
                    IsInstalled = false,
                    DisplayVersion = "Not found",
                    MajorVersion = 0,
                    RawOutput = ""
                };
            }
        });
    }

    private static int ParseMajorVersion(string text)
    {
        Match quotedVersion = Regex.Match(text, "\"(?<version>[0-9]+)(\\.(?<minor>[0-9]+))?");

        if (!quotedVersion.Success)
        {
            return 0;
        }

        string firstNumberText = quotedVersion.Groups["version"].Value;

        if (!int.TryParse(firstNumberText, out int firstNumber))
        {
            return 0;
        }

        if (firstNumber == 1)
        {
            string minorText = quotedVersion.Groups["minor"].Value;

            if (int.TryParse(minorText, out int oldJavaVersion))
            {
                return oldJavaVersion;
            }
        }

        return firstNumber;
    }
}