using FluentAssertions;

namespace Recall.Maui.UnitTests;

public sealed class AndroidCompressedProofTests
{
    [Fact]
    public async Task AndroidCompressed_Tone_Is16kHz()
    {
        if (!AdbProcess.IsAvailable())
        {
            return;
        }

        var devices = AdbProcess.Run("adb", "devices");
        if (!devices.Output.Contains("device", StringComparison.Ordinal) || devices.Output.Trim().Equals("List of devices attached", StringComparison.Ordinal))
        {
            return;
        }

        var fixture = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Fixtures", "tone.m4a"));
        File.Exists(fixture).Should().BeTrue();
        var push = AdbProcess.Run("adb", "push", fixture, "/sdcard/Android/data/com.recall.shell/files/tone.m4a");
        push.ExitCode.Should().Be(0);
        AdbProcess.Run("adb", "shell", "am", "force-stop", "com.recall.shell").ExitCode.Should().Be(0);
        AdbProcess.Run("adb", "shell", "monkey", "-p", "com.recall.shell", "-c", "android.intent.category.LAUNCHER", "1").ExitCode.Should().Be(0);
        await Task.Delay(TimeSpan.FromSeconds(8), TestContext.Current.CancellationToken);
        var report = AdbProcess.Run("adb", "shell", "run-as", "com.recall.shell", "cat", "files/android-audio-decode.txt");
        report.ExitCode.Should().Be(0);
        report.Output.Should().Contain("sampleRate=16000");
        var countLine = report.Output.Split('\n').FirstOrDefault(line => line.StartsWith("sampleCount=", StringComparison.Ordinal));
        countLine.Should().NotBeNull();
        var count = int.Parse(countLine["sampleCount=".Length..], System.Globalization.CultureInfo.InvariantCulture);
        count.Should().BeGreaterThan(16000);
    }

    private static class AdbProcess
    {
        public static bool IsAvailable()
        {
            try
            {
                using var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "adb",
                        Arguments = "version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                    },
                };
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        public static (int ExitCode, string Output) Run(string file, params string[] args)
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = file,
                    Arguments = string.Join(' ', args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                },
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, output);
        }
    }
}
