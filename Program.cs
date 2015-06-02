using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CheckProjFileRefs
{
    class Program
    {
        static void Main(string[] args)
        {
            Options options = new Options(args);
            if (options.DisplayHelp)
                return;

            if (options.OutputToFile)
            {
                string tempFile = String.Format("{0}ProjectDifferences_{1:yyyy-MM-dd_HH-mm-ss}.txt", Path.GetTempPath(), DateTime.Now);
                using (StreamWriter writer = File.CreateText(tempFile))
                {
                    Scan(options, writer);
                }
                Process.Start(tempFile);
            }
            else
            {
                Scan(options, Console.Out);
            }
            Console.WriteLine("Done.\n");
        }

        private static void Scan(Options options, TextWriter writer)
        {
            var scanner = new Scanner(writer);

            if (Directory.Exists(options.ProjectOrDirectoryPath))
            {
                var foundAFile = false;
                foreach (string file in Directory.EnumerateFiles(options.ProjectOrDirectoryPath, "*.csproj", SearchOption.AllDirectories))
                {
                    foundAFile = true;
                    ScanSingle(options, scanner, file);
                }

                if (!foundAFile)
                    Console.WriteLine("No project files found to check!");
            }
            else
            {
                ScanSingle(options, scanner, options.ProjectOrDirectoryPath);
            }
        }

        private static void ScanSingle(Options options, Scanner scanner, string path)
        {
            var ignorePatterns = new HashSet<string>(options.IgnorePatterns);
            if (File.Exists(path + ".ignore"))
                ignorePatterns.UnionWith(File.ReadAllLines(path + ".ignore")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim()));
            scanner.Scan(path, ignorePatterns);
        }



        private class Scanner
        {
            private static readonly HashSet<string> NodesToScan = new HashSet<string>
            {
                "Content",
                "None",
                "Compile",
            };
            private static readonly Regex DetectRegexRegex = new Regex("[!a-zA-Z0-9_/\\\\-]");


            private readonly TextWriter writer;

            public Scanner(TextWriter writer)
            {
                this.writer = writer;
            }

            public void Scan(string projectFilePath, HashSet<string> ignorePatterns)
            {
                this.writer.WriteLine(projectFilePath);
                if (Console.Out != this.writer)
                    Console.WriteLine(projectFilePath);

                // Convert ignore patterns into regexes
                Regex[] ignoreRes = ignorePatterns.Select(pattern =>
                    DetectRegexRegex.IsMatch(pattern) ? new Regex(pattern) : new Regex(String.Format("^{0}$", pattern))
                ).ToArray();

                string projDir = Path.GetDirectoryName(projectFilePath);
                HashSet<string> filesToFind = new HashSet<string>();
                HashSet<string> duplicateEntries = new HashSet<string>();
                HashSet<string> extensionsToExpect = new HashSet<string>();
                HashSet<string> directoriesWithFiles = new HashSet<string>();
                HashSet<string> suspectDirectories = new HashSet<string>();

                var proj = XDocument.Load(projectFilePath);
                foreach (var node in proj.Descendants())
                {
                    var childIncludePath = node.Attribute("Include");
                    if (childIncludePath == null || !NodesToScan.Contains(node.Name.LocalName))
                        continue;

                    string childPath = CorrectFilePath(Path.Combine(projDir, childIncludePath.Value));
                    if (!filesToFind.Add(childPath))
                        duplicateEntries.Add(childPath);

                    extensionsToExpect.Add(Path.GetExtension(childPath));
                    directoriesWithFiles.UnionWith(GetDirectories(projDir, childPath));
                }

                List<string> notInProject = new List<string>();
                foreach (string childPath in Directory.EnumerateFiles(projDir, "*", SearchOption.AllDirectories))
                {
                    if (filesToFind.Remove(childPath))
                        continue;

                    if (extensionsToExpect.Contains(Path.GetExtension(childPath)))
                    {
                        // Only scan directories containing files in the solution
                        if (!directoriesWithFiles.Contains(Path.GetDirectoryName(childPath)))
                        {
                            // Find the root-most directory not in the project file
                            string lastDir = null;
                            foreach (string dir in GetDirectories(projDir, childPath))
                            {
                                if (directoriesWithFiles.Contains(dir))
                                    break;
                                lastDir = dir;
                            }
                            if (lastDir != null)
                                suspectDirectories.Add(lastDir);
                        }
                        else
                        {
                            // If the file shouldn't be ignored, mark it as missing from the project
                            if (!ignoreRes.Any(re => re.IsMatch(childPath, projDir.Length)))
                                notInProject.Add(childPath);
                        }
                    }
                }

                if (notInProject.Count > 0)
                {
                    this.writer.WriteLine("\n  Files not in the project:");
                    foreach (string path in notInProject)
                        this.writer.WriteLine("    {0}", path);
                }

                // Filter out any directories that should be ignored
                suspectDirectories = new HashSet<string>(suspectDirectories.Where(dir =>
                    !ignoreRes.Any(re => re.IsMatch(dir, projDir.Length))
                ));

                if (suspectDirectories.Count > 0)
                {
                    this.writer.WriteLine("\n  Directories not in the project:");
                    foreach (string path in suspectDirectories)
                        this.writer.WriteLine("    {0}", path);
                }

                if (filesToFind.Count > 0)
                {
                    this.writer.WriteLine("\n  References not in the file system:");
                    foreach (string path in filesToFind)
                        this.writer.WriteLine("    {0}", path);
                }

                if (duplicateEntries.Count > 0)
                {
                    this.writer.WriteLine("\n  References in the project more than once:");
                    foreach (string path in duplicateEntries)
                        this.writer.WriteLine("    {0}", path);
                }

                this.writer.WriteLine();
                this.writer.WriteLine();
            }

            private static IEnumerable<string> GetDirectories(string parentDirectory, string path)
            {
                string dir = Path.GetDirectoryName(path);
                while (dir != null && dir != parentDirectory)
                {
                    yield return dir;
                    dir = Path.GetDirectoryName(dir);
                }
            }

            // Corrects path casing issues by matching the case in the file system if possible,
            // otherwise returns the given path unmodified.
            private static string CorrectFilePath(string path)
            {
                path = Path.GetFullPath(path);
                if (!File.Exists(path) && !Directory.Exists(path))
                    return path;

                string sTmp = "";
                foreach (string sPth in path.Split(new[] { '\\' }))
                {
                    if (string.IsNullOrEmpty(sTmp))
                    {
                        sTmp = sPth + "\\";
                        continue;
                    }
                    sTmp = Directory.GetFileSystemEntries(sTmp, sPth)[0];
                }
                return sTmp;
            }
        }



        private class Options
        {
            public string ProjectOrDirectoryPath = Directory.GetCurrentDirectory();
            public bool OutputToFile = true;
            public bool DisplayHelp = false;
            public HashSet<string> IgnorePatterns = new HashSet<string>();

            public Options(string[] args)
            {
                if (args.Length < 1)
                    return;

                for (int i = 0; i < args.Length; i += 1)
                {
                    switch (args[i])
                    {
                        case "-?":
                        case "--help":
                        case "/?":
                        case "/help":
                            this.DisplayHelp = true;
                            break;
                        case "-f":
                        case "--file":
                            this.OutputToFile = true;
                            break;
                        case "-i":
                        case "--ignore":
                            if (i + 1 >= args.Length)
                            {
                                Console.WriteLine("Missing ignore pattern!\n");
                                this.DisplayHelp = true;
                                break;
                            }
                            i += 1;
                            this.IgnorePatterns.Add(args[i]);
                            break;
                        default:
                            if (i == args.Length - 1 && (Directory.Exists(args[i]) || File.Exists(args[i])))
                            {
                                this.ProjectOrDirectoryPath = args[i];
                                break;
                            }
                            Console.WriteLine("Unexpected argument: {0}\n", args[i]);
                            this.DisplayHelp = true;
                            break;
                    }
                }

                if (this.DisplayHelp)
                {
                    Console.WriteLine(@"{0} [-f] [-i <ignore pattern>] [<path to project file or directory>]

Scans Visual Studio project files for missing, duplicate, or dead references.

  -f/file   (Optional) Writes issues to file and opens it after scanning,
          otherwise writes output to the console.

  -i/ignore (Optional) Adds a path to ignore if missing. Ignore patterns can
          be regexes, full file/folder names, or path substrings.

          Patterns can also be saved on separate lines in a file adjacent to
          the project file, with the name ""<ProjectName.extension>.ignore"",
          i.e. ""Foo.csproj.ignore"".

  <path>  (Optional) Project file or directory to scan for issues. Scans the
          current working directory if absent.
", Process.GetCurrentProcess().ProcessName, args[0]);
                }
            }
        }
    }
}
