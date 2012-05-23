using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;

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

        public MilSyntaxWalker ExtractMessagingSyntax()
        {
            walker = new MilSyntaxWalker();

            foreach (var tree in _compilation.SyntaxTrees)
            {
                walker.Visit(tree.Root);
            }

            return walker;
        }

        public IEnumerable<MilToken> GetMessagePublicationData()
        {
            ExtractMessagingSyntax();
            var pubs = walker.PublicationCalls;
            
            List<MilToken> tokens = new List<MilToken>();
            foreach (var stmt in pubs)
            {
                var model = _compilation.GetSemanticModel(_compilation.SyntaxTrees.Single(x => x.Root.DescendentNodesAndSelf().Contains(stmt)));
                var info = model.GetSemanticInfo(stmt.Expression);

                
                var sendSymbols = info.Type;
               
                    var cmdDef = walker.Commands.FirstOrDefault(x => x.Identifier.GetText() == sendSymbols.Name);
                    if (cmdDef == null) continue;

                    var handler = walker.CommandHandlersWithCommands.First(x => x.Value.Any(g => g.TypeArgumentList.Arguments.Select(ar => ar.PlainName).Contains(sendSymbols.Name))).Key;
                    if (handler == null) continue;

                    tokens.Add(TokenFactory.GetCommand(sendSymbols.Name));
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