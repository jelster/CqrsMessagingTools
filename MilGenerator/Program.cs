using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using NDesk.Options;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;

namespace MilGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var resetEvent = new ManualResetEvent(false);
            var runner = new CommandRunner();
            Action<ManualResetEvent, string, bool> exitMethod = (rest, msg, fail) =>
                                                                    {
                                                                        Console.WriteLine(msg);
                                                                        Console.WriteLine();
                                                                        rest.Set();
                                                                    };

            runner.MessagePump.Subscribe(HandleRunnerMessage, 
                ex => exitMethod(resetEvent, ex.Message, true),
                () => exitMethod(resetEvent, "Complete.", false));

            Initialize(args, runner);
            runner.Run();
            
            while (!resetEvent.WaitOne(1500))
            {
                ;
            }
            Exit("Exiting...");
        }

        private static void Initialize(IEnumerable<string> args, CommandRunner runner)
        {
            bool showHelp = false;
            var clArgs = new OptionSet()
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
                                     "x|externalNs=",
                                     "namespace for eXternal-facing component (used to aid discovery of command/event initiators and sequences"
                                     ,
                                     x => { runner.externalNs = x; }
                                     },
                                 {
                                     "?|h|help",
                                     "this message",
                                     (bool v) => showHelp = v
                                     }
                             };
            try
            {
                var otherArgs = clArgs.Parse(args);
                runner.ValidateData();

            }
            catch (OptionException ex)
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
            Console.WriteLine(msg.Message);
        }
        private static void Exit(string message = null, bool failCondition = false)
        {
            Console.WriteLine(message ?? "Exiting...");
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
        public string slnPath = null;
        public string externalNs = null;

        public IObservable<RunnerEventArg> MessagePump { get { return messagePump.AsObservable(); } }
        private readonly ISubject<RunnerEventArg> messagePump = new Subject<RunnerEventArg>();

        public void Run()
        {
            SendMessage(string.Format("Loading solution {0}", slnPath));
            ISolution sln = null;
            try
            {
                sln = Solution.Load(slnPath);
            }
            catch (Exception ex)
            {
                messagePump.OnError(ex);
                return;
            }
            
            var externalProject = sln.Projects.FirstOrDefault(x => x.AssemblyName == externalNs);
            if (externalProject == null)
            {
                var msg = string.Format("Project for given external-facing namespace of {0} not found.", externalNs);
                messagePump.OnError(new ArgumentException(msg));
                return;
            }
            SendMessage(string.Format("Using {0} for process discovery", processIName));
            var token = new CancellationToken();

            var compilation = externalProject.GetCompilation(token);
            token.WaitHandle.WaitOne(10000);
            var analyzer = new MIL.Services.AnalysisService(processIName);
            var processDefinition = analyzer.GetProcessToken((Compilation) compilation);
            
            SendMessage(string.Format("MIL output: {1}{0}", processDefinition.ToString() ?? "(none found)", Environment.NewLine));
            
            messagePump.OnCompleted();

        }

        public void ValidateData()
        {
            if (slnPath == null) throw new OptionException("Missing path to sln file", "-s");
            if (externalNs == null) throw new OptionException("Missing external-facing component namespace", "-ex");
        }

        private void SendMessage(string message, bool signalError = false)
        {
            messagePump.OnNext(new RunnerEventArg(message, signalError));
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
