using System.Collections.Generic;
using Roslyn.Compilers.CSharp;

namespace MIL.Services
{
    public class Walk : SymbolVisitor<NamespaceSymbol, IEnumerable<NamedTypeSymbol>>
    {

        public override IEnumerable<NamedTypeSymbol> VisitNamespace(NamespaceSymbol symbol, NamespaceSymbol argument)
        {
            var types = new List<NamedTypeSymbol>();

            foreach (var ns in symbol.GetNamespaceMembers())
            {
                var closeTypes = types;
                closeTypes.AddRange(Visit(ns));
                closeTypes.AddRange(ns.GetTypeMembers().AsEnumerable());
            }
            return types;
        }
    }
}