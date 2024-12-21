namespace SteamPrefill
{
    public static class Program
    {
        public static async Task<int> Main()
        {
            try
            {
                // Checking to see if the user double-clicked the exe in Windows, and display a message on how to use the app
                OperatingSystemUtils.DetectDoubleClickOnWindows("SteamPrefill");

                var cliArgs = ParseHiddenFlags();
                var description = """
                                  Automatically fills a Lancache with games from Steam, so that subsequent downloads will be
                                    served from the Lancache, improving speeds and reducing load on your internet connection.

                                    Start by selecting apps for prefill with the 'select-apps' command, then start the prefill using 'prefill'
                                  """;

                return await new CliApplicationBuilder()
                             .AddCommandsFromThisAssembly()
                             .SetTitle("SteamPrefill")
                             .SetExecutableNamePlatformAware("SteamPrefill")
                             .SetDescription(description)
                             .SetVersion($"v{ThisAssembly.Info.InformationalVersion}")
                             .Build()
                             .RunAsync(cliArgs);
            }
            catch (TimeoutException e)
            {
                if (e.StackTrace.Contains(nameof(UserAccountStore.GetUsernameAsync)))
                {
                    AnsiConsole.Console.LogMarkupError("Timed out while waiting for username entry");
                }
                if (e.StackTrace.Contains(nameof(MiscExtensions.ReadPasswordAsync)))
                {
                    AnsiConsole.Console.LogMarkupError("Timed out while waiting for password entry");
                }
                AnsiConsole.Console.LogException(e);
            }
            catch (TaskCanceledException e)
            {
                if (e.StackTrace.Contains(nameof(AppInfoHandler.RetrieveAppMetadataAsync)))
                {
                    AnsiConsole.Console.LogMarkupError("Unable to load latest App metadata! An unexpected error occurred! \n" +
                                                       "This could possibly be due to transient errors with the Steam network. \n" +
                                                       "Try again in a few minutes.");
                }
                AnsiConsole.Console.LogException(e);
            }
            catch (Exception e)
            {
                AnsiConsole.Console.LogException(e);
            }

            // Return failed status code, since you can only get to this line if an exception was handled
            return 1;
        }

        /// <summary>
        /// Adds hidden flags that may be useful for debugging/development, but shouldn't be displayed to users in the help text
        /// </summary>
        private static List<string> ParseHiddenFlags()
        {
            // Have to skip the first argument, since it is the path to the executable
            var args = Environment.GetCommandLineArgs().Skip(1).ToList();

            // Enables SteamKit2 debugging as well as SteamPrefill verbose logs
            if (args.Any(e => e.Contains("--debug")))
            {
                AnsiConsole.Console.LogMarkupLine($"Using {LightYellow("--debug")} flag.  Displaying debug only logging...");
                AnsiConsole.Console.LogMarkupLine($"Additional debugging files will be output to {Magenta(AppConfig.DebugOutputDir)}");
                AppConfig.DebugLogs = true;
                args.Remove("--debug");
            }

            // Will skip over downloading logic.  Will only download manifests
            if (args.Any(e => e.Contains("--no-download")))
            {
                AnsiConsole.Console.LogMarkupLine($"Using {LightYellow("--no-download")} flag.  Will skip downloading chunks...");
                AppConfig.SkipDownloads = true;
                args.Remove("--no-download");
            }

            // Skips using locally cached manifests. Saves disk space, at the expense of slower subsequent runs.
            // Useful for debugging since the manifests will always be re-downloaded.
            if (args.Any(e => e.Contains("--nocache")) || args.Any(e => e.Contains("--no-cache")))
            {
                AnsiConsole.Console.LogMarkupLine($"Using {LightYellow("--nocache")} flag.  Will always re-download manifests...");
                AppConfig.NoLocalCache = true;
                args.Remove("--nocache");
                args.Remove("--no-cache");
            }

            if (args.Any(e => e.Contains("--cellid")))
            {
                var flagIndex = args.IndexOf("--cellid");
                var id = args[flagIndex + 1];
                AppConfig.CellIdOverride = uint.Parse(id);

                AnsiConsole.Console.LogMarkupLine($"Using {LightYellow("--cellid")} flag.  Will force the usage of cell id {Magenta(id)}");
                args.Remove("--cellid");
                args.Remove(id);
            }

            if (args.Any(e => e.Contains("--max-threads")))
            {
                var flagIndex = args.IndexOf("--max-threads");
                var count = args[flagIndex + 1];
                AppConfig.MaxConcurrencyOverride = int.Parse(count);

                AnsiConsole.Console.LogMarkupLine($"Using {LightYellow("--max-threads")} flag.  Will download using at most {Magenta(count)} threads");
                args.Remove("--max-threads");
                args.Remove(count);
            }

            // Adding some formatting to logging to make it more readable + clear that these flags are enabled
            if (AppConfig.DebugLogs || AppConfig.SkipDownloads || AppConfig.NoLocalCache)
            {
                AnsiConsole.Console.WriteLine();
                AnsiConsole.Console.Write(new Rule());
            }

            return args;
        }
    }
}