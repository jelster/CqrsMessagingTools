using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace SyntaxHelperUtilities
{
    public static class SyntaxExtensions
    {
        public static string GetClassName(this BaseTypeDeclarationSyntax classDec)
        {
            return classDec.Identifier.GetText();
        }

        public static bool CollectionContainsClassDeclaration<T>(this IEnumerable<T> collection, string name) where T : BaseTypeDeclarationSyntax
        {
            return collection.Any(x => GetClassName(x) == name);
        }

        public static string GetClassName(this TypeSyntax classDec)
        {
            return classDec.PlainName;
        }

        public static bool CollectionContainsClass<T>(this IEnumerable<T> collection, string name) where T : TypeSyntax
        {
            return collection.Any(x => GetClassName(x) == name);
        }
         
    }
}