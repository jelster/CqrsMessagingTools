using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace MIL.Visitors
{
    public class MilSemanticAnalyzer
    {
        private readonly Compilation _compilation;
        private readonly Func<NamespaceOrTypeSymbol, IEnumerable<Symbol>> nameExtractor;
        private MilSyntaxWalker walker;
        public MilSemanticAnalyzer(Compilation compilation)
        {
            _compilation = compilation;
            nameExtractor = name =>
                                {
                                    var members = name.GetMembers().ToList();
                                    return members.Concat(members.OfType<NamespaceSymbol>()
                                                              .SelectMany(x => nameExtractor(x)));
                                };
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
            if (walker == null)
            {
                ExtractMessagingSyntax();
            }
            var pubs = walker.PublicationCalls;
            var model = _compilation.GetSemanticModel(_compilation.SyntaxTrees.First());
            List<MilToken> tokens = new List<MilToken>();
            foreach (var stmt in pubs)
            {
                var dataFlow = model.AnalyzeRegionDataFlow(stmt.FullSpan);
                var send = (LocalSymbol)dataFlow.ReadOutside.First();

                var cmdDef = walker.Commands.FirstOrDefault(x => x.Identifier.GetText().Contains(send.Type.Name));
                if (cmdDef != null)
                {
                    var handler = walker.CommandHandlersWithCommands.First(x => x.Value.Any(g => g.TypeArgumentList.Arguments.Select(ar => ar.PlainName).Contains(send.Type.Name))).Key;
                    if (handler != null)
                    {
                        tokens.Add(TokenFactory.GetCommand(send.Type.Name));
                        tokens.Add(TokenFactory.GetPublish());
                        tokens.Add(TokenFactory.GetCommandHandler(handler.Identifier.GetText()));
                        tokens.Add(TokenFactory.GetStatementTerminator());
                    }
                }

                // TODO: add discrimination logic for events/commands
                // TODO: find handler from command

            }
            return tokens;
        }
    }
}