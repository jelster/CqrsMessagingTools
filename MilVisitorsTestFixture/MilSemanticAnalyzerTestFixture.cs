using System.Linq;
using MIL.Visitors;
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

            Assert.NotEmpty(pubOps);
            Assert.True(pubOps.Count() == 4);
            Assert.NotNull(pubOps.FirstOrDefault(x => x.Token.MilTokenType == MilTokenType.Command));
            Assert.NotNull(pubOps.Skip(1).FirstOrDefault(x => x.Token.MilTokenType == MilTokenType.Publisher));
            Assert.NotNull(pubOps.Skip(2).FirstOrDefault(x => x.Token.MilTokenType == MilTokenType.CommandHandler));
            Assert.NotNull(pubOps.Skip(3).First());
        }

    }
}