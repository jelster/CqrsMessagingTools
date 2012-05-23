using System;
using System.Linq;
using System.Text;
using MIL.Visitors;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using SyntaxHelperUtilities;
using Xunit;

namespace MilVisitorsTestFixture
{
    public class MilSyntaxWalkerTestFixture
    {
        #region Code string

        private const string Code =
            @"
namespace TestCode 
{
    using System;  
    
    public interface ICommand {}
    public interface IEvent {}
    public interface ICommandHandler<T> where T : ICommand
    {
        void Handles(T command);
    }
    public interface IEventHandler<T> where T : IEvent {}

    public class Foo : ICommand { }	                           
    public class FoomandHandler : ICommandHandler<Foo>
    {
        public void Handles(Foo command)
        {
            Console.Write(""Foomand handled {0}"", command.Name);
        }

        public void Send(ICommand cmd) { Handles(cmd); }
    }   
    public class BadFooHandler : ICommandHandler<Foo> 
    { 
        public void Handles(Foo command) { throw new NotImplementedException(); }
    } 
    public class Bar : IEvent {}
    public class Foobar : IEvent {}
    public class BarventHandler : IEventHandler<Bar> {}
    public class OtherventHandler : IEventHandler<Bar>, IEventHandler<Foobar> {}               
    public class Program
    {
        IBus bus;
        public Program()
        {
            bus = new MyBus();
        }
        public static void Main()
        {
            var handler = new FoomandHandler();
            var cmd = new Foo();
            
            bus.Send(cmd);
            Send(cmd);
        }
        public void Send(ICommand cmd) { }		
    }

    public interface IBus { void Send(ICommand cmd); }
    public class MyBus : IBus
    {
        public void Send(ICommand cmd) {}
    }
                            
}";
        #endregion

        public class given_a_syntax_tree
        {
            protected SyntaxTree tree;
            private readonly MilSyntaxWalker sut;
            protected Compilation compilation;
            public given_a_syntax_tree()
            {
                tree = SyntaxTree.ParseCompilationUnit(Code);
                compilation = Compilation.Create("test.exe")
                    .AddSyntaxTrees(tree)
                    .UpdateOptions(new CompilationOptions("TestCode.Program", "Program"))
                    .AddReferences(new AssemblyFileReference(typeof(object).Assembly.Location));

                sut = new MilSyntaxWalker();
            }

            [Fact]
            public void when_walker_visits_command_class_declaration_adds_to_command_list()
            {
                SyntaxNode node = tree.Root;
                sut.Visit(node);

                Assert.NotEmpty(sut.Commands);
                Assert.True(sut.Commands.Count == 1);
                Assert.True(sut.Commands.First().GetClassName() == "Foo");
            }

            [Fact]
            public void when_walker_visits_command_handler_class_declaration_added_to_cmd_handler_list()
            {
                var node = tree.Root;
                sut.Visit(node);

                Assert.NotEmpty(sut.CommandHandlers);
                Assert.True(sut.CommandHandlers.Count() == 2);
                Assert.True(sut.CommandHandlers.CollectionContainsClassName("FoomandHandler"));
            }

            [Fact]
            public void when_walker_visits_node_with_publish_operation_adds_to_publication_list()
            {
                var node = tree.Root;
                sut.Visit(node);

                Assert.NotEmpty(sut.PublicationCalls);
                Assert.True(sut.PublicationCalls.Count == 1);
            }

            [Fact]
            public void when_walker_visits_event_class_declaration_adds_to_event_list()
            {
                var node = tree.Root;
                sut.Visit(node);

                Assert.NotEmpty(sut.Events);
                Assert.True(sut.Events.Count == 2);
                Assert.True(sut.Events.First().GetClassName() == "Bar");
            }

            [Fact]
            public void when_walker_visits_event_handler_class_adds_to_event_handlers_list()
            {
                var node = tree.Root;
                sut.Visit(node);

                Assert.NotEmpty(sut.EventHandlers);
                Assert.True(sut.EventHandlers.CollectionContainsClassName("BarventHandler"));
            }

            [Fact]
            public void walker_correctly_finds_publications_in_complete_program()
            {
                var analysis = new MilSemanticAnalyzer(compilation);
                var walker = analysis.ExtractMessagingSyntax();
                Assert.NotEmpty(walker.PublicationCalls);
            }

            [Fact]
            public void when_multiple_event_handlers_for_same_event_walker_finds_all()
            {
                var node = tree.Root;
                sut.Visit(node);
                
                var handles = sut.EventToEventHandlersMapping.Single(x => x.Key == "Bar");
                Assert.True(handles.Value.Count == 2);
                Assert.True(handles.Value.CollectionContainsClassName("BarventHandler"));
                Assert.True(handles.Value.CollectionContainsClassName("OtherventHandler"));
            }

            [Fact]
            public void when_send_is_not_prefixed_by_member_access_walker_ignores()
            {
                var analysis = new MilSemanticAnalyzer(compilation);
                var walker = analysis.ExtractMessagingSyntax();
                Assert.True(walker.PublicationCalls.Count == 1);
                 
            }
        }
    }
}
