using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MIL.Visitors;
using Roslyn.Compilers.CSharp;
using Xunit;

namespace MilVisitorsTestFixture
{
    public class MilSyntaxWalkerTestFixture
    {
        public class given_a_syntax_tree
        {
            private readonly MilSyntaxWalker sut; 

            public given_a_syntax_tree()
            {
                sut = new MilSyntaxWalker();
            }

            [Fact]
            public void when_walker_visits_command_class_declaration_adds_to_command_list()
            {
                var tree = SyntaxTree.ParseCompilationUnit("public class Foomand : ICommand {}");
                SyntaxNode node = tree.Root;
                sut.Visit(node);

                Assert.NotEmpty(sut.Commands);
                Assert.True(sut.Commands.Count == 1);
                Assert.True(sut.Commands.First().Identifier.GetText() == "Foomand");
            }

            [Fact]
            public void when_walker_visits_command_handler_class_declaration_added_to_cmd_handler_list()
            {
                var tree = SyntaxTree.ParseCompilationUnit("public class FooHandler : ICommandHandler<Foomand> {}");
                var node = tree.Root;
                sut.Visit(node);

                Assert.NotEmpty(sut.CommandHandlers);
                Assert.True(sut.CommandHandlers.Count() == 1);
                Assert.True(sut.CommandHandlers.First().Identifier.GetText() == "FooHandler");
            }

            [Fact]
            public void when_walker_visits_node_with_publish_operation_adds_to_publication_list()
            {
                var tree = SyntaxTree.ParseCompilationUnit("public void Bar() { var a = new Foomand(); messageBus.Send(a); }");
                var node = tree.Root;
                sut.Visit(node);

                Assert.NotEmpty(sut.PublicationCalls);
                Assert.True(sut.PublicationCalls.Count == 1);
            }

            [Fact]
            public void when_walker_visits_event_class_declaration_adds_to_event_list()
            {
                var tree = SyntaxTree.ParseCompilationUnit("public class Foovent : IEvent {}");
                var node = tree.Root;
                sut.Visit(node);

                Assert.NotEmpty(sut.Events);

            }
        }
    }
}
