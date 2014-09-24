using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using MIL.Visitors;
using NDesk.Options;

namespace MilGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var resetEvent = new ManualResetEvent(false);
            Action<ManualResetEvent, string> exitMethod = (rest, msg) =>
                                                                    {
                                                                        Console.WriteLine();
                                                                        Console.WriteLine(msg);
                                                                        rest.Set();
                                                                    };
            var runner = new CommandRunner();
            runner.MessagePump.Subscribe(HandleRunnerMessage,
                ex => exitMethod(resetEvent, ex.Message),
                () => exitMethod(resetEvent, ""));

            Initialize(args, runner);
            runner.Run();

            resetEvent.WaitOne(10000);
        }

        private static void Initialize(IEnumerable<string> args, CommandRunner runner)
        {
            bool showHelp = false;

            var clArgs = new OptionSet
                            {
                                {
                                    "s|sln=",
                                    "path to target Solution file (.sln) to be analyzed",
                                    v => { runner.slnPath = Path.GetFullPath(v); }
                                },
                                {
                                    "p|proccessIfx:",
                                    "name of the interface used to mark a Process",
                                    x => { runner.processIName = x; }
                                },
                                {
                                    "x|externalNs:",
                                    "namespace for eXternal-facing component (used to aid discovery of command/event initiators and sequences"
                                    ,
                                    x => { runner.externalNs = x; }
                                },
                                {
                                     "?|h|help",
                                     "this message",
                                     v => showHelp = (v != null)
                                },
                                {
                                    "t|processType:",
                                    "name of a concrete class implementing a process",
                                    x => runner.processTypeName = x
                                },
                                {
                                    "v|verbose",
                                    "Output more detail during execution",
                                    v => runner.Verbose = (v != null)
                                },
                                {
                                    "<>",
                                    "List of assembly names to exclude from discovery and analysis.",
                                    v => runner.ExcludedAssemblies.AddRange(v.Split(' '))
                                },

            };

            try
            {
                var otherArgs = clArgs.Parse(args);
                runner.ValidateData();
            }
            catch (Exception ex)
            {
                Console.WriteLine("MilGenerator ");
                Console.WriteLine(ex.Message);
                PrintHelp(clArgs);
                Exit();
            }

            if (showHelp)
            {
                PrintHelp(clArgs);
                Exit();
            }
        }
        private static void HandleRunnerMessage(RunnerEventArg msg)
        {
            Console.Write(msg.Message);
        }
        private static void Exit(string message = null, bool failCondition = false)
        {
            Console.WriteLine(message);
            Console.WriteLine();
            Environment.Exit(failCondition ? -1 : 0);
        }
        private static void PrintHelp(OptionSet opts)
        {
            Console.WriteLine("Usage: milgenerator -s <path to .sln> -ex Conference.Web.Public");
            Console.WriteLine("Analyze and generate MIL documents from a C# solution");
            Console.WriteLine("If no process interface is specified, the default name of {0} will be used", CommandRunner.DefaultProcessIfxName);
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            opts.WriteOptionDescriptions(Console.Out);
        }
    }

    internal class CommandRunner
    {
        public const string DefaultProcessIfxName = "IProcess";
        public string processIName = DefaultProcessIfxName;
        public string processTypeName = null;
        public string slnPath = null;
        public string externalNs = null;
        public readonly List<string> ExcludedAssemblies = new List<string>();
        public IObservable<RunnerEventArg> MessagePump { get { return messagePump.AsObservable(); } }
        private readonly ISubject<RunnerEventArg> messagePump = new Subject<RunnerEventArg>();

        public bool Verbose;

        public void Run()
        {
            Solution sln = LoadSolution();

            //var externalProject = sln.Projects.FirstOrDefault(x => x.AssemblyName == externalNs);
            //if (externalProject == null)
            //{
            //    var msg = string.Format("Project for given external-facing namespace of {0} not found.", externalNs);
            //    messagePump.OnError(new ArgumentException(msg));
            //    return;
            //}

            SendMessage(string.Format("{1}Using {0} for process discovery{1}", processTypeName ?? DefaultProcessIfxName, Environment.NewLine));

            var token = new CancellationToken();
            var analyzer = new MIL.Services.ProcessAnalysisService(processIName);

            //ProcessDefinition processDefinition = null;
            var excludeList = sln.Projects.Where(x => ExcludedAssemblies.Any(e => x.AssemblyName.Contains(e))).ToList();
            if (excludeList.Any())
            {
                SendMessage(string.Format("Ignoring {0} Assemblies{1}", excludeList.Count, Environment.NewLine),
                    () => "Ignored assemblies: " + Environment.NewLine + string.Join(Environment.NewLine, excludeList.Select(x => x.AssemblyName)) + Environment.NewLine);
            }

            MilSyntaxWalker treeData = new MilSyntaxWalker();
            foreach (var proj in sln.Projects.Except(excludeList))
            {
                SendMessage(".", () => Environment.NewLine + "# Processing assembly " + proj.AssemblyName + Environment.NewLine);

                MilSemanticAnalyzer semantics = null;
                MetadataFileReferenceProvider provider = new MetadataFileReferenceProvider();
                Compilation compilation = (Compilation)proj.GetCompilationAsync(token).Result
                    .AddReferences(new MetadataFileReference(typeof(object).Assembly.Location))
                    .AddReferences(new MetadataFileReference(typeof(IEnumerable<>).Assembly.Location)); ;

                try
                {
                    semantics = new MilSemanticAnalyzer(compilation);
                }
                catch (InvalidOperationException ex)
                {
                    SendMessage("x", () => string.Join(Environment.NewLine, compilation.GetDeclarationDiagnostics().Select(x => x.ToString())) + Environment.NewLine);
                    continue;
                }

                semantics.ExtractMessagingSyntax(treeData);

                //if (proj.AssemblyName == externalProject.AssemblyName)
                //    continue;

                //processDefinition = analyzer.GetProcessDefinition(compilation, processTypeName);


                //if (processDefinition != null)
                //{
                //    var procToke = ProcessDefinition.GetTokenFromDefinition(processDefinition);

                //    if (procToke.Token != MilTypeConstant.EmptyToken)
                //        SendMessage(procToke.ToString());
                //}
                //foreach (var pubCall in semantics.GetMessagePublicationData())
                //{
                //    SendMessage(pubCall.ToString());
                //}
            }

            DumpSyntaxData(treeData);

            messagePump.OnCompleted();
        }

        private Solution LoadSolution()
        {
            SendMessage(string.Format("Loading solution {0}", slnPath));

            Dictionary<string, string> dict = new Dictionary<string, string>(2);
            dict.Add("Configuration", "Debug");
            dict.Add("Platform", "Any CPU");

            MSBuildWorkspace workspace = MSBuildWorkspace.Create(dict);

            Solution sln = null;
            try
            {
                sln = workspace.OpenSolutionAsync(slnPath).Result;
            }
            catch (Exception ex)
            {
                messagePump.OnError(ex);
            }

            return sln;
        }

        private void DumpSyntaxData(MilSyntaxWalker treeData)
        {
            SendMessage(Environment.NewLine + "# Aggregate roots" + Environment.NewLine);
            SendMessage((treeData.AggregateRoots.Any() ? treeData.DumpAggregateRoots() : new[] { "-- none --" } as dynamic));
            SendMessage(Environment.NewLine + "# Commands" + Environment.NewLine);
            SendMessage((treeData.Commands.Any() || treeData.CommandHandlers.Any() ? treeData.DumpCommandData() : new[] { "-- none --" } as dynamic));
            SendMessage(Environment.NewLine + "# Events" + Environment.NewLine);
            SendMessage((treeData.Events.Any() || treeData.EventHandlers.Any() ? treeData.DumpEventData() : new[] { "-- none --" } as dynamic));
            SendMessage(Environment.NewLine + "# Message publications" + Environment.NewLine);
            SendMessage(treeData.PublicationCalls.Any() ? treeData.DumpPublicationData() : new[] { "-- none --" });
        }

        private void SendMessage(IEnumerable<MilToken> messageLines)
        {
            SendMessage(string.Join("", messageLines.Select(x => x.ToString())));
        }

        public void ValidateData()
        {
            if (slnPath == null) throw new OptionException("Missing path to sln file", "-s");
        }

        private void SendMessage(string message, Func<string> detailSelector = null)
        {
            string txt = null;
            if (Verbose && detailSelector != null)
            {
                txt = detailSelector() ?? message;
            }
            else
            {
                txt = message;
            }
            if (string.IsNullOrEmpty(txt)) return;

            messagePump.OnNext(new RunnerEventArg(txt));
        }

        private void SendMessage(IEnumerable<string> messageLines)
        {
            foreach (var msg in messageLines)
            {
                SendMessage(msg);
            }
        }
    }

    internal class RunnerEventArg
    {
        public string Message;
        public bool ShouldExit;

        public RunnerEventArg(string message, bool shouldExit = false)
        {
            Message = message;
            ShouldExit = shouldExit;
        }
    }
}
