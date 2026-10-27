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

        AdbProcess.Shell("run-as com.recall.shell true").ExitCode.Should().Be(0, "install a Debug com.recall.shell apk on the emulator before this proof");

        var fixture = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Fixtures", "tone.m4a"));
        File.Exists(fixture).Should().BeTrue();
        AdbProcess.Shell("am force-stop com.recall.shell").ExitCode.Should().Be(0);
        AdbProcess.Run("adb", "push", fixture, "/data/local/tmp/recall-tone.m4a").ExitCode.Should().Be(0);
        AdbProcess.Shell("run-as com.recall.shell mkdir -p files").ExitCode.Should().Be(0);
        var stage = AdbProcess.Shell("run-as com.recall.shell cp /data/local/tmp/recall-tone.m4a files/tone.m4a");
        stage.ExitCode.Should().Be(0, stage.Output);
        AdbProcess.Shell("monkey -p com.recall.shell -c android.intent.category.LAUNCHER 1").ExitCode.Should().Be(0);
        (int ExitCode, string Output) report = (1, string.Empty);
        for (var attempt = 0; attempt < 60; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            report = AdbProcess.Shell("run-as com.recall.shell cat files/android-audio-decode.txt");
            if (report.ExitCode == 0 && report.Output.Contains("sampleRate=", StringComparison.Ordinal))
            {
                break;
            }
        }

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

        public static (int ExitCode, string Output) Shell(string command) =>
            Run("adb", "shell", command);

        public static (int ExitCode, string Output) Run(string file, params string[] args)
        {
            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = file,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var arg in args)
            {
                start.ArgumentList.Add(arg);
            }

            using var process = System.Diagnostics.Process.Start(start)!;
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, output);
        }
    }
}
