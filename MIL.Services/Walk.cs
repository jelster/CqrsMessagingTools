using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MIL.Services
{
    public class Walk : SymbolVisitor<IEnumerable<INamedTypeSymbol>>
    {
        public override IEnumerable<INamedTypeSymbol> VisitNamespace(INamespaceSymbol symbol)
        {
            var types = new List<INamedTypeSymbol>();

            foreach (var ns in symbol.GetNamespaceMembers())
            {
                var closeTypes = types;
                closeTypes.AddRange(Visit(ns));
                closeTypes.AddRange(ns.GetTypeMembers());
            }
            return types;
        }
    }
}