using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SyntaxHelperUtilities
{
    public static class SyntaxExtensions
    {
        public static string GetClassName(this BaseTypeDeclarationSyntax classDec)
        {
            return classDec.Identifier.Text;
        }

        public static bool CollectionContainsClassDeclaration<T>(this IEnumerable<T> collection, string name) where T : BaseTypeDeclarationSyntax
        {
            return collection.Any(x => GetClassName(x) == name);
        }

        public static string GetClassName(this TypeSyntax classDec)
        {
            // Dong Xie: not really sure what's this for? REVIEW

            return classDec.ToString();

            //return classDec.PlainName;
        }

        public static bool CollectionContainsClass<T>(this IEnumerable<T> collection, string name) where T : TypeSyntax
        {
            return collection.Any(x => GetClassName(x) == name);
        }
    }
}