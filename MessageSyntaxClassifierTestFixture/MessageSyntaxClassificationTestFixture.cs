using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.Text.Classification;
using Moq;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.Editor;
using SyntaxClassifierCS;
using Xunit;

namespace MessageSyntaxClassifierTestFixture
{
    public class given_a_document
    {
        protected readonly IDocument Document;

        private readonly MockRepository mockRepos = new MockRepository(MockBehavior.Default);

        private readonly MessageSendSyntaxClassifier sut;
        private const string code = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagingToolsRoslynTest.Interfaces;

namespace MessagingToolsRoslynTest.Interfaces
{
    public interface ICommand
    {
        string Name { get; }
    }

    public interface ICommandHandler<T> where T : ICommand
    {
        void Send(T command);
    }
}

namespace MessagingToolsRoslynTest
{
    public class Foo : ICommand
    {
        #region Implementation of ICommand

        public string Name { get; set; }

        #endregion
    }
    public class FooHandler : ICommandHandler<Foo>
    {
        public void Send(Foo command)
        {
            Console.WriteLine(""Sent Command!"");
        }
    }

    public class MainApp
    {
        public static void Main()
        {
            #region TestsLookHere
            var f = new Foo();
            var h = new FooHandler();
            h.Send(f);
            #endregion
        }
    }
}";

        public given_a_document()
        {
            var tree = SyntaxTree.ParseCompilationUnit(code);
            var comp = Compilation.Create("test.dll").AddSyntaxTrees(tree);
            
            var mockDoc = mockRepos.Create<IDocument>();
            
            mockDoc.Setup(x => x.GetSyntaxTree(It.IsAny<CancellationToken>())).Returns(comp.SyntaxTrees.Single());
            mockDoc.Setup(x => x.GetSemanticModel(It.IsAny<CancellationToken>())).Returns(() => comp.GetSemanticModel(tree));
            Document = mockDoc.Object;

            var mockDef = mockRepos.Create<IClassificationType>().SetupAllProperties();
            mockDef.Setup(x => x.IsOfType(It.IsAny<string>())).Returns(true);
            sut = new MessageSendSyntaxClassifier(mockDef.Object);
        }

        [Fact]
        public void when_object_creation_node_passed_classifies_expression()
        {
            var result = sut.ClassifyNode(Document, Document.GetSyntaxTree().Root.DescendentNodes()
                    .OfType<ObjectCreationExpressionSyntax>().First());

            Assert.NotNull(result);
            Assert.True(result.Count() == 1);
            Assert.False(result.First().TextSpan.IsEmpty);
        }

        [Fact]
        public void when_send_invoked_passed_classifies_expression()
        {
            var testSyntaxAarea = Document.GetSyntaxTree().Root.DescendentNodes().OfType<BlockSyntax>().Single(x => x.GetFullText().Contains("TestsLookHere"));
            var result = sut.ClassifyNode(Document, testSyntaxAarea.DescendentNodes().OfType<InvocationExpressionSyntax>().First());

            Assert.NotNull(result);
            Assert.True(result.Count() == 1);
            Assert.False(result.First().TextSpan.IsEmpty);
        }
        
    }
}
