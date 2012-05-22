using System;
using System.Collections.Generic;
using MIL.Services;
using MIL.Visitors;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Xunit;

namespace Mil.ServiceIntegrationTests
{
    public class ServicesTestFixture
    {
        public class given_a_compiled_application
        {
            protected Compilation AppCompilation;
            private const string code = @"
namespace Foo.Message
{
    using System.Collections.Generic;
    
    public interface ICommandBus { void Send(ICommand cmd); void Send(IEnumerable<ICommand> cmds); }
    public interface ICommand {}
    public interface IProcess {}
    public interface ICommandHandler<T> where T : ICommand { void Handles(T cmd); }

    public class BarMessage : ICommand {}
    
    public class ShortBus : ICommandBus
    {
        public void Send(ICommand cmd) {}
        public void Send(IEnumerable<ICommand> cmds) {}
    }

    public class ShortProcess : IProcess, ICommandHandler<BarMessage>
    {
        public enum ShortState
        {
            NoState = 0,
            StateA = 1,
            DifferentState = 2
        }
    
        public ShortState State { get; set; }
        public ShortProcess() { State = ShortState.StateA; }
        public void Handles(BarMessage cmd)
        {
            
        }
    }
    
}
namespace Foo.Web
    {
        using System;
        using Foo.Message;

        public class ExternalSite
        {
            private readonly ICommandBus commandBus;
            public ExternalSite(ICommandBus cmdBus)
            {
                commandBus = cmdBus;
            }
            public string Register(BarMessage cmd)
            {
                commandBus.Send(cmd);
                return ""Sent Command"";
            }
        }

        public class App
        {
            public static void Main()
            {
                var bus = new ShortBus();
                var site = new ExternalSite(bus);
                
                var cmd = new BarMessage();
                site.Register(cmd);
            }
        }
    }
";
            private const string Shortprocess = "ShortProcess";
            private readonly AnalysisService sut;
            private string[] expectedStates;

            public given_a_compiled_application()
            {
                //var resourceType = typeof (Resources);
                //var assemblies = resourceType.GetProperties(BindingFlags.Static | BindingFlags.Public)
                //    .Select(y => y.GetValue(null, null))
                //    .OfType<byte[]>()
                //    .ToList();

                //Assert.NotEmpty(assemblies);
                
                //Assert.True(assemblies.Count == 7); // # of assemblies in resource file
                var tree = SyntaxTree.ParseCompilationUnit(code);
                AppCompilation = Compilation.Create("test.dll")
                    .AddSyntaxTrees(tree)
                    .AddReferences(new AssemblyFileReference(typeof (object).Assembly.Location))
                    .AddReferences(new AssemblyFileReference(typeof (IEnumerable<>).Assembly.Location));
                  //  .AddReferences(assemblies.Select(x => new AssemblyBytesReference(x)));
                
                var diag = AppCompilation.GetDiagnostics();
                Assert.Empty(diag);
                
                sut = new AnalysisService();
            }

            [Fact]
            public void when_process_analyzed_gets_state_flags()
            {
                expectedStates = new [] { "NoState", "StateA", "DifferentState" };
                var states = sut.GetProcessStateNames(AppCompilation, Shortprocess);

                Assert.NotNull(states);
                Assert.NotEmpty(states);
                Assert.Equal(expectedStates, states);
            }

            [Fact]
            public void when_process_analyzed_gets_state_tokens()
            {
                MilToken token = sut.GetProcessToken(AppCompilation, Shortprocess);
                Assert.NotNull(token);
                 
                Assert.True(token.MemberName == "ShortProcess, State:[NoState, StateA, DifferentState]");
                Assert.True(token.Token == MilTypeConstant.StateDefinitionToken);
                Assert.True(token.ToString() == "%ShortProcess, State:[NoState, StateA, DifferentState]");
            }

            [Fact]
            public void when_process_incomplete_returns_empty_token()
            {
                var otherSut = new AnalysisService();
                var otherToken = otherSut.GetProcessToken(null, null);

                Assert.True(otherToken.Token == MilTypeConstant.EndOfStatementToken);
            }
        }
    }
}
