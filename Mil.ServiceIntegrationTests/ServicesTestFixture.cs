using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MIL.Services;
using MIL.Visitors;
using Xunit;

namespace Mil.ServiceIntegrationTests
{
    public class ServicesTestFixture
    {
        public class given_a_compiled_application
        {
            protected Compilation AppCompilation;
            private const string infraCode = @"
namespace Foo.Infrastructure
{
    using System.Collections.Generic;
    
    public interface ICommandBus { void Send(ICommand cmd); void Send(IEnumerable<ICommand> cmds); }
    public interface ICommand {}
    public interface IProcess {}
    public interface ICommandHandler<T> where T : ICommand { void Handles(T cmd); }

}";
            private const string code = @"
namespace Foo.Message
{
    using System.Collections.Generic;
    using Foo.Infrastructure;    

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
        public ShortProcess() {  }
        public void Handles(BarMessage cmd)
        {
            if (State == ShortState.NoState)
            {
                State = ShortState.StateA;   
            }
        }
    }
    
}
namespace Foo.Web
    {
        using System;
        using Foo.Message;
        using Foo.Infrastructure;

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
            private readonly ProcessAnalysisService sut;
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
                AppCompilation = CSharpCompilation.Create("test")
                    .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(infraCode))
                    .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(code))
                    .AddReferences(new MetadataFileReference(typeof(object).Assembly.Location))
                    .AddReferences(new MetadataFileReference(typeof(IEnumerable<>).Assembly.Location));
                //  .AddReferences(assemblies.Select(x => new AssemblyBytesReference(x)));

                var diag = AppCompilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);
                Assert.Empty(diag);

                sut = new ProcessAnalysisService();
            }

            [Fact]
            public void when_process_analyzed_gets_state_flags()
            {
                expectedStates = new[] { "NoState", "StateA", "DifferentState" };
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
            public void service_throws_when_compilation_null()
            {
                Assert.Throws(typeof(ArgumentNullException), () => sut.GetProcessToken(null, null));
            }

            [Fact]
            public void when_process_name_null_returns_first_type_implementing_ifx()
            {
                var token = sut.GetProcessToken(AppCompilation, null);

                Assert.True(token.MemberName == "ShortProcess, State:[NoState, StateA, DifferentState]");
            }

            [Fact]
            public void when_no_state_definition_found_does_not_throw()
            {
                var otherComp = AppCompilation
                    .RemoveSyntaxTrees(AppCompilation.SyntaxTrees.AsEnumerable())
                    .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(""));
                var otherSut = new ProcessAnalysisService();
                ProcessDefinition definition = null;
                Assert.DoesNotThrow(() => definition = otherSut.GetProcessDefinition(otherComp));
                Assert.Null(definition);
            }
        }
    }

    public class TypeSymbolWalkerFixture
    {
        Walk sut = new Walk();

        [Fact]
        public void when_walk_nested_namespaces_with_types_returns_all_unnested_classes()
        {
            var code = @"
namespace Bar { 
    public class FooA {} 
    public class BarB {} 
    namespace BarB { 
        public class FooB { public class NestedFoo {} } 
        public class FooC {}
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            var comp = CSharpCompilation.Create("testA").AddSyntaxTrees(tree);

            var result = sut.Visit(comp.GlobalNamespace);

            //Assert.NotEmpty(result);
            //Assert.True(result.Count() == 4);
        }
    }
}
