using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text.Classification;
using Moq;
using Roslyn.Compilers.CSharp;
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

        public given_a_document()
        {
            var workspace = Solution.LoadStandAloneProject(@"..\..\..\MessagingToolsRoslynTest\MessagingToolsRoslynTest.csproj");
            Document = workspace.Documents.FirstOrDefault(x => x.DisplayName == "Class1.cs");
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
