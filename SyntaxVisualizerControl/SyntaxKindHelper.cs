// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using CSharp = Roslyn.Compilers.CSharp;
using VisualBasic = Roslyn.Compilers.VisualBasic;

namespace Roslyn.Samples.SyntaxVisualizer.Control
{
    public static class SyntaxKindHelper
    {
        // Helpers that return the language-sepcific (C# / VB) SyntaxKind of a language-agnostic
        // CommonSyntaxNode / CommonSyntaxToken / CommonSyntaxTrivia.

        public static string GetKind(this CommonSyntaxNodeOrToken nodeOrToken, string language)
        {
            var kind = string.Empty;

            if (nodeOrToken.IsNode)
            {
                kind = nodeOrToken.AsNode().GetKind(language);
            }
            else
            {
                kind = nodeOrToken.AsToken().GetKind(language);
            }

            return kind;
        }

        public static string GetKind(this CommonSyntaxNode node, string language)
        {
            var kind = string.Empty;

            if (language == LanguageNames.CSharp)
            {
                kind = ((CSharp.SyntaxKind)node.Kind).ToString();
            }
            else 
            {
                kind = ((VisualBasic.SyntaxKind)node.Kind).ToString();
            }

            return kind;
        }

        public static string GetKind(this CommonSyntaxToken token, string language)
        {
            var kind = string.Empty;

            if (language == LanguageNames.CSharp)
            {
                kind = ((CSharp.SyntaxKind)token.Kind).ToString();
            }
            else 
            {
                kind = ((VisualBasic.SyntaxKind)token.Kind).ToString();
            }

            return kind;
        }

        public static string GetKind(this CommonSyntaxTrivia trivia, string language)
        {
            var kind = string.Empty;

            if (language == LanguageNames.CSharp)
            {
                kind = ((CSharp.SyntaxKind)trivia.Kind).ToString();
            }
            else
            {
                kind = ((VisualBasic.SyntaxKind)trivia.Kind).ToString();
            }

            return kind;
        }
    }
}