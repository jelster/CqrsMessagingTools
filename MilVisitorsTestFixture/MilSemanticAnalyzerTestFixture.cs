using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MIL.Visitors;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Xunit;

namespace MilVisitorsTestFixture
{
    public class MilSemanticAnalyzerTestFixture : MilSyntaxWalkerTestFixture.given_a_syntax_tree
    {
        private readonly MilSemanticAnalyzer sut;

        public MilSemanticAnalyzerTestFixture()
        {
            sut = new MilSemanticAnalyzer(compilation);
        }

        [Fact]
        public void semantic_analysis_of_outer_pub_action_yields_mil_statement()
        {
            var pubOps = sut.GetMessagePublicationData();
            pubOps.ToList().ForEach(x => Console.WriteLine(x.ToString()));
            Assert.NotEmpty(pubOps);
            Assert.True(pubOps.Count() == 4);
            Assert.NotNull(pubOps.FirstOrDefault(x => x.Token.MilTokenType == MilTokenType.Command));
            Assert.NotNull(pubOps.Skip(1).FirstOrDefault(x => x.Token.MilTokenType == MilTokenType.Publisher));
            Assert.NotNull(pubOps.Skip(2).FirstOrDefault(x => x.Token.MilTokenType == MilTokenType.CommandHandler));
            Assert.NotNull(pubOps.Skip(3).First(x => x.Token.MilTokenType == MilTokenType.LanguageElement));
        }

        [Fact]
        public void does_not_throw_for_unimplemented_roslyn_elements()
        {
            const string errorCode = "CS8000";
            var newComp = compilation.AddSyntaxTrees(SyntaxTree.ParseCompilationUnit("namespace cs8000test { using System; }"));
            MilSemanticAnalyzer newSut = null;
            Assert.DoesNotThrow(() => newSut = new MilSemanticAnalyzer(newComp));

            var result = sut.ExtractMessagingSyntax();
        }

        [Fact]
        public void when_compilation_errors_in_code_throws()
        {
            var newComp = compilation.AddSyntaxTrees(SyntaxTree.ParseCompilationUnit("private class Jar { public string Name { get; set; }}"));
            MilSemanticAnalyzer newSut = null;
            var ex = Assert.Throws(typeof (InvalidOperationException), () => newSut = new MilSemanticAnalyzer(newComp));
        }

        [Fact]
        public void when_valid_compilation_includes_all_references()
        {
            var newComp =
                compilation.AddSyntaxTrees(
                    SyntaxTree.ParseCompilationUnit(
                        @"
namespace refTest 
{ 
    using System.ComponentModel.DataAnnotations; 
    public class RefTest { 
        [Required]
        public string Name { get; set;}
    }
}"))
                    .AddReferences(new AssemblyFileReference(typeof (RequiredAttribute).Assembly.Location));

            MilSemanticAnalyzer newSut;
            
            Assert.DoesNotThrow(() =>  newSut = new MilSemanticAnalyzer(newComp));
        }

    }
}