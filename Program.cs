using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GitGrep
{
    class Program
    {
        public static string[] Args { get; set; }

        async static Task Main(string[] args)
        {
            Args = args;

            List<string> argumentListGitCommits = new()
            {
                "rev-list",
                "--all",
                Args[0]
            };

            ProcessStartInfo startInfoGitCommits = new ProcessStartInfo("git")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (string arg in argumentListGitCommits)
            {
                startInfoGitCommits.ArgumentList.Add(arg);
            }

            using (Process processGitCommits = Process.Start(startInfoGitCommits))
            {
                while (await processGitCommits.StandardOutput.ReadLineAsync() is string line && line != null)
                {
                    await ProcessGitCommitLineAsync(line);
                }

                await processGitCommits.WaitForExitAsync();
            }
        }

        public static void OnReceivedGrepOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null) {
                Console.WriteLine(e.Data.Replace("\r", "").Replace("\n", ""));
            }
        }

        public static async Task ProcessGitCommitLineAsync(string line)
        {
            List<string> argumentListGitShow = new()
            {
                "show",
                $"{line}:{Args[0]}"
            };

            ProcessStartInfo startInfoGitShow = new ProcessStartInfo("git")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (string arg in argumentListGitShow)
            {
                startInfoGitShow.ArgumentList.Add(arg);
            }

            // Console.WriteLine(String.Join(" ", argumentListGitShow));

            using (Process processGitShow = Process.Start(startInfoGitShow))
            {
                ProcessStartInfo startInfoGrep = new ProcessStartInfo("grep")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                startInfoGrep.ArgumentList.Add($"--line-buffered");
                startInfoGrep.ArgumentList.Add($"--with-filename");
                startInfoGrep.ArgumentList.Add($"--label={line}:{Args[0]}");

                foreach (string grepArgument in Args.Skip(1))
                {
                    startInfoGrep.ArgumentList.Add(grepArgument);
                }

                // Console.WriteLine(String.Join(" ", startInfoGrep.ArgumentList));

                using Process processGitGrep = Process.Start(startInfoGrep);

                processGitGrep.StandardInput.AutoFlush = true;
                processGitGrep.OutputDataReceived += OnReceivedGrepOutput;
                processGitGrep.BeginOutputReadLine();

                while (await processGitShow.StandardOutput.ReadLineAsync() is string lineGitShow && lineGitShow != null)
                {
                    await processGitGrep.StandardInput
                        .WriteLineAsync(lineGitShow);
                }

                await processGitShow.WaitForExitAsync();

                // TODO: does this wait for grep to finish
                await processGitGrep.StandardInput.FlushAsync();
                processGitGrep.StandardInput.Close();
                await processGitGrep.WaitForExitAsync();
            }
        }
    }
}