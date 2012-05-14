using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.Editor;

namespace SyntaxClassifierCS
{
    [ExportSyntaxNodeClassifier("CSharpMessagingClassifier", LanguageNames.CSharp, typeof(ObjectCreationExpressionSyntax), typeof(InvocationExpressionSyntax))]
    public class MessageSendSyntaxClassifier : ISyntaxClassifier
    {
        private readonly IClassificationType invokeClassification;

        public MessageSendSyntaxClassifier(IClassificationType classification)
        {
            invokeClassification = classification;
        }

        [ImportingConstructor]
        public MessageSendSyntaxClassifier(IClassificationTypeRegistryService classificationService)
        {
            this.invokeClassification = classificationService.GetClassificationType(ClassificationTypes.MessageSendClassificationTypeName);

        }
        #region Implementation of ISyntaxClassifier

        public IEnumerable<SyntaxClassification> ClassifyNode(IDocument document, CommonSyntaxNode syntax, CancellationToken cancellationToken = new CancellationToken())
        {
            var classList = new List<SyntaxClassification>(2);
            var treeRef = ((SyntaxTree) document.GetSyntaxTree(cancellationToken)).GetReference((SyntaxNode)syntax);
            var model = document.GetSemanticModel(cancellationToken);

            if (GetClassificationForCommandCreation(model, treeRef.GetSyntax() as ObjectCreationExpressionSyntax, cancellationToken))
            {
                classList.Add(new SyntaxClassification(syntax.Span, invokeClassification));
            }
            if (GetClassificationForCommandPublication(model, treeRef.GetSyntax() as InvocationExpressionSyntax, cancellationToken))
            {
                // TODO: create separate classification type?
                classList.Add(new SyntaxClassification(syntax.Span, invokeClassification));
            }

            return classList.AsEnumerable();
        }

        public IEnumerable<SyntaxClassification> ClassifyToken(IDocument document, CommonSyntaxToken syntax, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        #endregion

        public static bool GetClassificationForCommandPublication(ISemanticModel model, InvocationExpressionSyntax syntax, CancellationToken cancellationToken)
        {
            if (syntax == null) return false;

            var memberExpression = syntax.Expression as MemberAccessExpressionSyntax;
            if (memberExpression == null)
                return false;

            return memberExpression.Name.Identifier.ValueText == "Send";
        }

        public static bool GetClassificationForCommandCreation(ISemanticModel model, ObjectCreationExpressionSyntax syntax, CancellationToken cancellationToken = new CancellationToken())
        {
            if (syntax == null) return false;

            var cmdType = model.GetSemanticInfo(syntax, cancellationToken);
            //var cmdType = model.GetSemanticInfo(syntax, cancellationToken);

            return cmdType.Type.AllInterfaces.AsList().Select(x => x.Name).Any(x => x == "ICommand");
        }
    }
}