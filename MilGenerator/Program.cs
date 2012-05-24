﻿using System;
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
            bool dumpInfo = false;
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
                                              "d|dump",
                                              "Perform discovery and simply dump results without analysis",
                                              d => runner.dumpInfo = (d != null)
                                              }
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
            Console.WriteLine(msg.Message);
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
            foreach (var proj in sln.Projects)
            {
                var compilation = (Compilation)proj.GetCompilation(token);
                var semantics = new MilSemanticAnalyzer(compilation);
                var treeData = semantics.ExtractMessagingSyntax();

                if (dumpInfo)
                {
                    if (treeData.Commands.Any())
                    {
                        foreach (var cmd in treeData.Commands)
                        {
                            var t = TokenFactory.GetCommand(cmd.GetClassName()).ToString() +
                                    TokenFactory.GetPublish();

                            var t1 = treeData.CommandHandlers.FirstOrDefault(x => x.BaseListOpt.Types.OfType<GenericNameSyntax>()
                                                                             .Any(y => y.TypeArgumentList.Arguments.Any(z => z.GetClassName().Contains(cmd.GetClassName()))));
                            var h = TokenFactory.GetCommandHandler(t1 == null ? TokenFactory.GetEmptyToken().ToString() : t1.GetClassName()).ToString();
                                
                            SendMessage(t + h);
                        }
                    }
                   
                    if (treeData.Events.Any())
                    {
                        foreach (var cmd in treeData.Events)
                        {
                            
                         
                            var t = TokenFactory.GetEvent(cmd.GetClassName()).ToString() +
                                    TokenFactory.GetPublish() + TokenFactory.GetStatementTerminator() + "    ";

                            var t1 = treeData.EventHandlers.Where(x => x.BaseListOpt.Types.OfType<GenericNameSyntax>()
                                                                             .Any(y => y.TypeArgumentList.Arguments.Any(z => z.GetClassName().Contains(cmd.GetClassName()))));
                            foreach (var evHand in t1)
                            {
                                var h = TokenFactory.GetEventHandler(evHand.GetClassName()).ToString();
                                SendMessage(t + h);
                            }
                        }
                    }
                    continue;
                }
                //if (proj.AssemblyName == externalProject.AssemblyName)
                //    continue;

                processDefinition = analyzer.GetProcessDefinition(compilation, processTypeName);


                if (processDefinition != null)
                {
                    var procToke = ProcessDefinition.GetTokenFromDefinition(processDefinition);

                    if (procToke.Token != MilTypeConstant.EmptyToken)
                        SendMessage(procToke.ToString());
                }
                foreach (var pubCall in semantics.GetMessagePublicationData())
                {
                    SendMessage(pubCall.ToString());
                }
            }

            messagePump.OnCompleted();

        }

        public void ValidateData()
        {
            if (slnPath == null) throw new OptionException("Missing path to sln file", "-s");
            //if (externalNs == null) throw new OptionException("Missing external-facing component namespace", "-ex");
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