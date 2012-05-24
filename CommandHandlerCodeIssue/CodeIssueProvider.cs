using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using MIL.Visitors;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;
using SyntaxHelperUtilities;

namespace CommandHandlerCodeIssue
{
    [ExportSyntaxNodeCodeIssueProvider("CommandHandlerCodeIssue", LanguageNames.CSharp, typeof(ClassDeclarationSyntax))]
    public class CodeIssueProvider : ICodeIssueProvider
    {
        private readonly ICodeActionEditFactory editFactory;


        // TODO: extract to resource or config
        private const string CommandHandlerInterfaceName = "ICommandHandler";

        [ImportingConstructor]
        public CodeIssueProvider(ICodeActionEditFactory editFactory)
        {
            this.editFactory = editFactory;
        }

        #region Unimplemented ICodeIssueProvider members

        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxNode node, CancellationToken cancellationToken)
        {
            var classNode = (ClassDeclarationSyntax) node;

            if (classNode.BaseListOpt == null) return null;

            var walker = new MilSyntaxWalker();
            walker.Visit((SyntaxNode) document.GetSyntaxTree(cancellationToken).Root);
            var allCommandHandlersInProject = walker.CommandHandlers;

           
            if (!allCommandHandlersInProject.Any() && !allCommandHandlersInProject.Contains(node)) return null;

            var dupes = walker.CommandHandlers.SelectMany(x => x.BaseListOpt.Types).FindDuplicates();

            if (!dupes.Any())
            {
                var desc = "{0} is implemented by multiple handlers:{1}{2}";
                var issues = new List<CodeIssue>();
                foreach (var dupe in dupes)
                {
                    var listing = FormatHandlerListing(dupe.GetClassName(), allCommandHandlersInProject.ToDictionary(x => x, syntax => syntax.BaseListOpt.Types.OfType<GenericNameSyntax>()));
                    var text = string.Format(desc, dupe, Environment.NewLine, string.Join(Environment.NewLine, listing));
                    issues.Add(new CodeIssue(CodeIssue.Severity.Warning, classNode.Identifier.FullSpan, text));
                }
                return issues;
            }
            return null;

            //if (allCommandHandlersInProject.Contains(node as ClassDeclarationSyntax))
            //{
            //    return new CodeIssue[] { new CodeIssue(CodeIssue.Severity.Warning, node.FullSpan, "Command Handler detected") };
            //}
        }

        private static IEnumerable<string> FormatHandlerListing(string genericInterfaceString, IEnumerable<KeyValuePair<ClassDeclarationSyntax, IEnumerable<GenericNameSyntax>>> handlers)
        {
            return
                handlers.Where(y => y.Value.Any(v => v.GetFullText().Contains(genericInterfaceString))).Select(
                    x => x.Key.Identifier.GetFullText());
        }

         
        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxToken token, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxTrivia trivia, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
