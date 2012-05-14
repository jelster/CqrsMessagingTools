using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CommandHandlerCodeIssue;
using Moq;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.Editor;
using Xunit;

namespace CommandHandlerCodeIssueTestFixture
{
    public class given_a_project
    {
        protected const string Code = @"
namespace TestCode 
{
    using System;  
    using MessagingToolsRoslynTest;
    using MessagingToolsRoslynTest.Interfaces;

    public interface ICommand {}

    public interface ICommandHandler<T> where T : ICommand
    {
        void Handles(T command);
    }    
    
    public class Foo : ICommand { }                            
    public class FoomandHandler : ICommandHandler<Foo>
    {
        public bool WasCalled { get; private set; }
        public void Handles(Foo command)
        {
            WasCalled = true;
            Console.Write(""Foomand handled {0}"", command.Name);
        }
    }   
    public class BadFooHandler : ICommandHandler<Foo> 
    { 
        public void Handles(Foo command) { throw new NotImplementedException(); }
    }                                        
}";

        private readonly IDocument document;
        private readonly CodeIssueProvider sut;
        private readonly MockRepository mockRepos = new MockRepository(MockBehavior.Loose);
        private readonly SyntaxTree tree;

        public given_a_project()
        {
            tree = SyntaxTree.ParseCompilationUnit(Code);

            var mockDoc = mockRepos.Create<IDocument>();
            mockDoc.Setup(x => 
                x.GetSyntaxTree(It.IsAny<CancellationToken>()))
                .Returns(tree);

            document = mockDoc.Object;
            var mockEditFactory = mockRepos.Create<ICodeActionEditFactory>();
            sut = new CodeIssueProvider(mockEditFactory.Object);
        }

        [Fact]
        public void when_multiple_command_handlers_flags_issue()
        {
            var node = document.GetSyntaxTree().Root.DescendentNodes().OfType<ClassDeclarationSyntax>().First(x => x.Identifier.ValueText == "BadFooHandler");
            var result = sut.GetIssues(document, node, new CancellationToken());

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            foreach (var issue in result)
            {
                Console.WriteLine(issue.Description);
            }
        }

        [Fact]
        public void when_handler_handles_multiple_different_commands_does_not_flag_as_issue()
        {
            
        }

    }
}
