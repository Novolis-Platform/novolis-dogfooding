using Novolis.IO.Mobile.Android;

namespace AdbLab;

/// <summary>Headless read of adb + optional Books Mobile package presence.</summary>
internal static class AdbSmoke
{
    public const string BooksMobilePackage = "com.novolis.booksmobile";

    public static int Run()
    {
        try
        {
            var adb = new AndroidDebugBridge();
            Console.WriteLine($"transport: {adb.Transport}");
            Console.WriteLine($"adb: {adb.AdbPath}");
            if (!string.Equals(adb.Transport, "protocol", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("AdbSmoke: expected protocol transport.");
                return 1;
            }

            if (!File.Exists(adb.AdbPath))
            {
                Console.Error.WriteLine($"AdbSmoke: adb path missing: {adb.AdbPath}");
                return 1;
            }

            var devices = adb.ListDevices();
            Console.WriteLine($"devices: {devices.Count}");
            foreach (var d in devices)
                Console.WriteLine($"  {d.Serial}\t{d.State}\t{d.Model}");

            var ready = devices.FirstOrDefault(d => d.State == AdbDeviceState.Device);
            if (ready is null)
            {
                Console.Error.WriteLine("AdbSmoke: no device in 'device' state.");
                return 1;
            }

            var installer = new AndroidAppInstaller(adb);
            var waited = installer.WaitForReadyDevice(TimeSpan.FromSeconds(5), ready.Serial);
            Console.WriteLine($"wait: {waited.Serial} {waited.State}");

            var pkg = installer.TryGetPackage(BooksMobilePackage, ready.Serial);
            if (pkg is { IsInstalled: true })
                Console.WriteLine($"package-info: {pkg.PackageName} {pkg.VersionName} ({pkg.VersionCode}) {pkg.ApkPath}");
            else
                Console.WriteLine($"package-info: {BooksMobilePackage} not installed");

            var bad = installer.ValidateApk(Path.Combine(Path.GetTempPath(), "missing-novolis.apk"));
            if (bad.Ok)
            {
                Console.Error.WriteLine("AdbSmoke: expected missing APK validation to fail.");
                return 1;
            }

            Console.WriteLine($"validate-missing: {bad.Errors[0]}");

            var info = adb.GetDeviceInfo(ready.Serial);
            Console.WriteLine(info.FormatReport());
            Console.WriteLine();

            // Protocol sync round-trip (tmp file).
            var local = Path.Combine(Path.GetTempPath(), $"novolis-adb-{Guid.NewGuid():N}.txt");
            var remote = "/data/local/tmp/novolis-adb-lab.txt";
            try
            {
                File.WriteAllText(local, "novolis-protocol-ok");
                var push = adb.Push(local, remote, ready.Serial);
                if (!push.Ok)
                {
                    Console.Error.WriteLine($"AdbSmoke: push failed: {push.Message}");
                    return 1;
                }

                var pulled = Path.Combine(Path.GetTempPath(), $"novolis-adb-pull-{Guid.NewGuid():N}.txt");
                var pull = adb.Pull(remote, pulled, ready.Serial);
                if (!pull.Ok)
                {
                    Console.Error.WriteLine($"AdbSmoke: pull failed: {pull.Message}");
                    return 1;
                }

                var text = File.ReadAllText(pulled);
                if (!text.Contains("novolis-protocol-ok", StringComparison.Ordinal))
                {
                    Console.Error.WriteLine("AdbSmoke: pull content mismatch.");
                    return 1;
                }

                Console.WriteLine($"sync: push/pull OK ({remote})");
                try { File.Delete(pulled); } catch { /* ignore */ }
            }
            finally
            {
                try { File.Delete(local); } catch { /* ignore */ }
                adb.Shell($"rm -f {remote}", ready.Serial);
            }

            var path = adb.Shell($"pm path {BooksMobilePackage}", ready.Serial);
            if (path.Ok && path.StdOut.Contains(BooksMobilePackage, StringComparison.Ordinal))
                Console.WriteLine($"package: {path.StdOut.Trim()}");
            else
                Console.WriteLine($"package: {BooksMobilePackage} not installed (ok for smoke)");

            Console.WriteLine("AdbSmoke OK");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AdbSmoke: {ex.Message}");
            return 1;
        }
    }
}
