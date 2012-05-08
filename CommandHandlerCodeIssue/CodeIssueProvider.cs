using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;

namespace CommandHandlerCodeIssue
{
    [ExportSyntaxNodeCodeIssueProvider("CommandHandlerCodeIssue", LanguageNames.CSharp, typeof(ClassDeclarationSyntax))]
    public class CodeIssueProvider : ICodeIssueProvider
    {
        private readonly ICodeActionEditFactory editFactory;

        private const string CommandHandlerInterfaceName = "ICommandHandler";

        [ImportingConstructor]
        public CodeIssueProvider(ICodeActionEditFactory editFactory)
        {
            this.editFactory = editFactory;
        }

        #region Unimplemented ICodeIssueProvider members

        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxNode node, CancellationToken cancellationToken)
        {
            ICompilation c = null;
            if (document.Project == null) return null;

            c = document.Project.GetCompilation(cancellationToken);
            if (c == null) return null;

            var classNode = (ClassDeclarationSyntax) node;
            if (classNode.BaseListOpt == null)
                return null;

            var baseTypes = classNode.BaseListOpt.Types.Where(y => y.PlainName == CommandHandlerInterfaceName).OfType<GenericNameSyntax>();
            if (!baseTypes.Any()) return null;

            var allCommandHandlersInProject = c.SyntaxTrees
                .SelectMany(x => x.Root.DescendentNodes().OfType<ClassDeclarationSyntax>().Where(s => s.BaseListOpt != null))
                .SelectMany(s => s.BaseListOpt.Types.Where(y => y.PlainName == CommandHandlerInterfaceName).OfType<GenericNameSyntax>(), (tree, syntax) => syntax).ToList();

            if (!allCommandHandlersInProject.Any()) return null;

            var sharedTypes = allCommandHandlersInProject.Union(baseTypes);

            if (sharedTypes.Count() > 1)
            {
                var descr = string.Join(Environment.NewLine, sharedTypes.SelectMany(x => x.Ancestors().OfType<ClassDeclarationSyntax>(), ((syntax, declarationSyntax) => declarationSyntax.Identifier.Value + ":" + syntax.Identifier.Value)));
                return new CodeIssue[] { new CodeIssue(CodeIssue.Severity.Warning, classNode.Identifier.Span, descr) };
            }
            
            return null;
            //if (allCommandHandlersInProject.Contains(node as ClassDeclarationSyntax))
            //{
            //    return new CodeIssue[] { new CodeIssue(CodeIssue.Severity.Warning, node.FullSpan, "Command Handler detected") };
            //}
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
