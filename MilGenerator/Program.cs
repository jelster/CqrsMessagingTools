using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using MIL.Services;
using MIL.Visitors;
using NDesk.Options;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using SyntaxHelperUtilities;

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
            bool? dumpInfo;

            var clArgs = new OptionSet
                            {
                                {
                                    "s|sln=",
                                    "full path of target Solution file (.sln) to be analyzed",
                                    x => { runner.slnPath = Path.GetFullPath(x); }
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
                                    "d|dump:",
                                    "Default action. Performs discovery and dumps results without attempting analysis (use -d- to disable).",
                                    (bool d) => runner.dumpInfo = d
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
            //Environment.Exit(failCondition ? -1 : 0);
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
        public bool dumpInfo;

        public void Run()
        {
            SendMessage(string.Format("Loading solution {0}", slnPath));
            ISolution sln = null;
            try
            {
                sln = Solution.Load(slnPath, "Debug", "AnyCPU");
            }
            catch (Exception ex)
            {
                messagePump.OnError(ex);
                return;
            }

            //var externalProject = sln.Projects.FirstOrDefault(x => x.AssemblyName == externalNs);
            //if (externalProject == null)
            //{
            //    var msg = string.Format("Project for given external-facing namespace of {0} not found.", externalNs);
            //    messagePump.OnError(new ArgumentException(msg));
            //    return;
            //}
            SendMessage(string.Format("Using {0} for process discovery{1}", processTypeName ?? processIName, Environment.NewLine));

            var token = new CancellationToken();
            var analyzer = new MIL.Services.ProcessAnalysisService(processIName);

            ProcessDefinition processDefinition = null;
            List<MilSyntaxWalker> analytics = new List<MilSyntaxWalker>();

            if (ExcludedAssemblies.Any())
            {
                SendMessage(string.Format("Ignoring {0} Assemblies{1}", ExcludedAssemblies.Count, Environment.NewLine));
            }
            foreach (var proj in sln.Projects.ToList().Where(x => !ExcludedAssemblies.Contains(x.AssemblyName)))
            {
                SendMessage(Environment.NewLine + "# Processing assembly " + proj.AssemblyName + Environment.NewLine);

                Compilation compilation = null;
                MilSemanticAnalyzer semantics = null;

                try
                {
                    compilation = (Compilation)proj.GetCompilation(token);
                    semantics = new MilSemanticAnalyzer(compilation);
                }
                catch (InvalidOperationException ex)
                {
                    continue;
                }

                var treeData = semantics.ExtractMessagingSyntax();
                analytics.Add(treeData);
                SendMessage(Environment.NewLine + "# Aggregate roots" + Environment.NewLine);

                SendMessage((treeData.AggregateRoots.Any() ? treeData.DumpAggregateRoots() : new[] { "-- none --" } as dynamic));
                SendMessage(Environment.NewLine + "# Commands" + Environment.NewLine);
                SendMessage((treeData.Commands.Any() || treeData.CommandHandlers.Any() ? treeData.DumpCommandData() : new[] { "-- none --" } as dynamic));
                SendMessage(Environment.NewLine + "# Events" + Environment.NewLine);
                SendMessage((treeData.Events.Any() || treeData.EventHandlers.Any() ? treeData.DumpEventData() : new[] { "-- none --" } as dynamic));
                SendMessage(Environment.NewLine + "# Message publications" + Environment.NewLine);
                SendMessage(treeData.PublicationCalls.Any() ? treeData.DumpPublicationData() : new[] { "-- none --" });

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


            messagePump.OnCompleted();
        }

        private void SendMessage(IEnumerable<MilToken> messageLines)
        {
            SendMessage(string.Join("", messageLines.Select(x => x.ToString())));
        }

        public void ValidateData()
        {
            if (slnPath == null) throw new OptionException("Missing path to sln file", "-s");
            //if (externalNs == null) throw new OptionException("Missing external-facing component namespace", "-ex");
        }

        private void SendMessage(string message, bool signalError = false)
        {
            if (string.IsNullOrEmpty(message)) return;
            messagePump.OnNext(new RunnerEventArg(message, signalError));
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
