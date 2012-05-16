using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace MIL.Visitors
{
    public static class SyntaxExtensions
    {
        public static string GetClassName(this BaseTypeDeclarationSyntax classDec)
        {
            return classDec.Identifier.GetText();
        }

        public static bool CollectionContainsClassName<T>(this IEnumerable<T> collection, string name) where T : BaseTypeDeclarationSyntax
        {
            return collection.Any(x => x.GetClassName() == name);
        }
         
    }
}