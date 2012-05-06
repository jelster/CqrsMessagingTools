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
    [ExportSyntaxNodeClassifier("FieldSyntaxClassifierCS", LanguageNames.CSharp,
        typeof(FieldDeclarationSyntax),
        typeof(IdentifierNameSyntax))]
    internal class FieldSyntaxClassifier : ISyntaxClassifier
    {
        private readonly IClassificationType fieldClassification;

        [ImportingConstructor]
        public FieldSyntaxClassifier(IClassificationTypeRegistryService classificationService)
        {
            this.fieldClassification = classificationService.GetClassificationType(ClassificationTypes.FieldClassificationTypeName);
        }

        public IEnumerable<SyntaxClassification> ClassifyNode(IDocument document, CommonSyntaxNode syntax, CancellationToken cancellationToken)
        {
            // If this node is a field declaration, return syntax classifications for the
            // identifier token of each field name.
            //
            // For example, "x" and "y" would be classified in the following code:
            // 
            // class C
            // {
            //     int x, y;
            // }
            if (syntax is FieldDeclarationSyntax)
            {
                var field = (FieldDeclarationSyntax)syntax;

                return from v in field.Declaration.Variables
                       select new SyntaxClassification(v.Identifier.Span, fieldClassification);
            }

            // If this node is an identifier, use the binding API to retrieve its symbol and return a 
            // syntax classification for the node if that symbol is a field.
            if (syntax is IdentifierNameSyntax)
            {
                var semanticModel = document.GetSemanticModel(cancellationToken);
                var symbol = semanticModel.GetSemanticInfo(syntax).Symbol;

                if (symbol != null && symbol.Kind == CommonSymbolKind.Field)
                {
                    return new[] { new SyntaxClassification(syntax.Span, fieldClassification) };
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
