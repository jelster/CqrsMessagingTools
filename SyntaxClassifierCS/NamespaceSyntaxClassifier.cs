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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;

namespace SyntaxClassifierCS
{
    [ExportSyntaxNodeClassifier("CSharpNamespaceClassifier", LanguageNames.CSharp,
        typeof(NamespaceDeclarationSyntax),
        typeof(IdentifierNameSyntax))]
    internal class NamespaceSyntaxClassifier : ISyntaxClassifier
    {
        private readonly IClassificationType namespaceClassification;

        [ImportingConstructor]
        public NamespaceSyntaxClassifier(IClassificationTypeRegistryService classificationService)
        {
            this.namespaceClassification = classificationService.GetClassificationType(ClassificationTypes.NamespaceClassificationTypeName);
        }

        public IEnumerable<SyntaxClassification> ClassifyNode(IDocument document, CommonSyntaxNode syntax, CancellationToken cancellationToken)
        {
            // If this node is a namespace declaration, return syntax classifications for the
            // identifier tokens that make up its name.
            //
            // For example, "A", "B" and "C" would be classified in the following code:
            // 
            // namespace A.B.C
            // {
            //     class D
            //     {
            //     }
            // }
            var namespaceDeclaration = syntax as NamespaceDeclarationSyntax;
            if (namespaceDeclaration != null)
            {
                return from t in namespaceDeclaration.Name.DescendentTokens()
                       where t.Kind == SyntaxKind.IdentifierToken
                       select new SyntaxClassification(t.Span, namespaceClassification);
            }

            // If this node is an identifier, use the binding API to retrieve its symbol and return a
            // syntax classification for the node if that symbol is a namespace.
            if (syntax is IdentifierNameSyntax)
            {
                var semanticModel = document.GetSemanticModel(cancellationToken);
                var symbol = semanticModel.GetSemanticInfo(syntax).Symbol;

                if (symbol != null && symbol.Kind == CommonSymbolKind.Namespace)
                {
                    return new[] { new SyntaxClassification(syntax.Span, namespaceClassification) };
                }
            }

            return null;
        }

        #region Unimplemented methods

        public IEnumerable<SyntaxClassification> ClassifyToken(IDocument document, CommonSyntaxToken syntax, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
