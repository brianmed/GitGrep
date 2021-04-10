using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GitGrep
{
    class Program
    {
        // TODO: Mono.Options
        public static string[] GrepArguments { get; set; }

        async static Task Main(string[] args)
        {
            if (args.Length < 2) {
                Console.WriteLine("Usage: GitGrep file|--all grepArgument1..grepArgumentN");
            }

            GrepArguments = args.Skip(1).ToArray();

            List<string> argumentListGitCommits = new()
            {
                "rev-list",
                "--all",
            };

            if (args[0] != "--all") {
                argumentListGitCommits.Add(args[0]);
            }

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
            // Console.WriteLine(String.Join(" ", startInfoGitCommits.ArgumentList));

            using (Process processGitCommits = Process.Start(startInfoGitCommits))
            {
                while (await processGitCommits.StandardOutput.ReadLineAsync() is string commit && commit != null)
                {
                    List<string> files = new List<string>();

                    // git ls-tree -r --name-only --full-tree 2aeb7d4b140f9b94bf4488d5f7a3e9b596dbedb0
                    if (args[0] == "--all") {
                        files.AddRange(await ProcessGitLsFullTreeAsync(commit));
                    } else {
                        files.Add(args[0]);
                    }

                    foreach (string file in files)
                    {
                        await ProcessGitShowAsync($"{commit}:{file}");
                    }
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

        public static async Task<List<string>> ProcessGitLsFullTreeAsync(string commit)
        {
            List<string> argumentListGitLs = new()
            {
                "ls-tree",
                "-r",
                "--name-only",
                "--full-tree",
                commit
            };

            ProcessStartInfo startInfoGitLs = new ProcessStartInfo("git")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (string arg in argumentListGitLs)
            {
                startInfoGitLs.ArgumentList.Add(arg);
            }

            // Console.WriteLine(String.Join(" ", argumentListGitLs));

            using (Process processGitLs = Process.Start(startInfoGitLs))
            {
                List<string> files = new List<string>();

                while (await processGitLs.StandardOutput.ReadLineAsync() is string file && file != null)
                {
                    files.Add(file);
                }

                await processGitLs.WaitForExitAsync();

                return files;
            }
        }

        public static async Task ProcessGitShowAsync(string line)
        {
            List<string> argumentListGitShow = new()
            {
                "show",
                line
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
                startInfoGrep.ArgumentList.Add($"--label={line}");

                foreach (string grepArgument in GrepArguments)
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