using System.Diagnostics;
using System.Globalization;
using Recall.Core.Ports;

namespace Recall.Maui.Platform.Resources;

public sealed class WindowsGpuTemperature : IGpuTemperature
{
    public int? ReadCelsius()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
#pragma warning disable S4036
                FileName = "nvidia-smi",
#pragma warning restore S4036
                Arguments = "--query-gpu=temperature.gpu --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            if (!process.Start())
            {
                return null;
            }

            if (!process.WaitForExit(2000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best effort.
                }

                return null;
            }

            var line = process.StandardOutput.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            return int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
        }
        catch
        {
            return null;
        }
    }
}

public sealed class AbsentGpuTemperature : IGpuTemperature
{
    public int? ReadCelsius() => null;
}