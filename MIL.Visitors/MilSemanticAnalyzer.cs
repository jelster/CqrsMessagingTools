using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using SyntaxHelperUtilities;
namespace MIL.Visitors
{
    public class MilSemanticAnalyzer
    {
        private readonly Compilation _compilation;
        private MilSyntaxWalker walker;

        public MilSemanticAnalyzer(Compilation compilation)
        {
            _compilation = compilation;
            ValidateCompilation();
        }

        private void ValidateCompilation()
        {
            const int CSErrorRoslynUnimplemented = 8000;
            var diags = _compilation.GetDeclarationDiagnostics().Where(x => x.Info.Code != CSErrorRoslynUnimplemented).ToList();
            if (!diags.Any())
            {
                return;
            }
            var msg = string.Format("Errors exist in the compilation. Correct these issues and try again{1}{0}",
                                    string.Join(Environment.NewLine, diags.Select(x => x.ToString())),
                                    Environment.NewLine);
            throw new InvalidOperationException(msg);
        }

        public MilSyntaxWalker ExtractMessagingSyntax(MilSyntaxWalker walk = null)
        {
            if (walk == null && walker == null)
                walker = new MilSyntaxWalker();
            else
                walker = walk;

            foreach (var tree in _compilation.SyntaxTrees)
            {
                walker.Visit(tree.Root);
            }

            return walker;
        }

        public IEnumerable<MilToken> GetMessagePublicationData()
        {
            ExtractMessagingSyntax();
            var pubs = walker.Publications;
            
            List<MilToken> tokens = new List<MilToken>();
            
            foreach (var stmt in pubs)
            {
                var syntax = stmt.Expression;
                var model = _compilation.GetSemanticModel(_compilation.SyntaxTrees.First(x => x.Root.DescendentNodesAndSelf().Any(r => r == syntax)));
                var info = model.GetSemanticInfo(syntax);
                if (info == null || info.Symbol == null) continue;
                
                var methodSymbol = (MethodSymbol)info.Symbol;
                if (methodSymbol.Parameters.IsNullOrEmpty || methodSymbol.MethodKind != MethodKind.Ordinary)
                    continue;

                var dout = model.AnalyzeRegionDataFlow(stmt.Parent.Span);
                var par =
                    dout.ReadOutside.Concat(dout.ReadInside).Concat(dout.DataFlowsOut).Concat(dout.WrittenInside).Select(x => 
                        x.ToMinimalDisplayParts(x.Locations.First(), model).First().GetText(CultureInfo.InvariantCulture));

                var cmdMatches = par.FirstOrDefault(x => walker.Commands.Select(w => w.Identifier.GetText()).Contains(x));
                if (cmdMatches == null) continue;

                var cmdName = cmdMatches;
                 
                var handler = walker.CommandHandlers.FirstOrDefault(x => x.BaseListOpt.Types.Any(a => ((GenericNameSyntax)a).TypeArgumentList.Arguments.CollectionContainsClass(cmdName)));

                    if (handler == null) continue;

                    tokens.Add(TokenFactory.GetCommand(cmdName));
                    tokens.Add(TokenFactory.GetPublish());
                    tokens.Add(TokenFactory.GetCommandHandler(handler.Identifier.GetText()));
                    tokens.Add(TokenFactory.GetStatementTerminator());
                

                // TODO: add discrimination logic for events/commands
                // TODO: find handler from command

            }
            return tokens;
        }
    }
}