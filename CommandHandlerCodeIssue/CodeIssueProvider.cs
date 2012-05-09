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
            if (document.Project == null) return null;

            var classNode = (ClassDeclarationSyntax) node;
            if (classNode.BaseListOpt == null)
                return null;

            var commandHandlerVisitor = new CommandHandlerSyntaxVisitor();

            var baseTypes = commandHandlerVisitor.Visit(classNode);
            
            if (baseTypes == null || !baseTypes.Any()) return null;

            var allCommandHandlersInProject = document.Project.GetCompilation(cancellationToken).SyntaxTrees
                    .SelectMany(x => x.Root.DescendentNodes().OfType<ClassDeclarationSyntax>().Where(s => s != classNode && s.BaseListOpt != null && commandHandlerVisitor.Visit(s).Any()))
                    .Select(x => new { classDec = x, handles = commandHandlerVisitor.Visit(x) }).ToList();

            if (!allCommandHandlersInProject.Any()) return null;

            var dupes = allCommandHandlersInProject.SelectMany(x => x.handles, (c, s) => s.GetText()).FindDuplicates();

            return dupes.Any() ? 
                dupes.Select(x => new CodeIssue(CodeIssue.Severity.Warning, classNode.Identifier.FullSpan, x)) 
                : null;

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

    public class CommandHandlerSyntaxVisitor : SyntaxVisitor<IEnumerable<GenericNameSyntax>>
    {
        private const string CommandHandlerInterfaceName = "ICommandHandler";

        protected override IEnumerable<GenericNameSyntax> VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return node.BaseListOpt != null ? node.BaseListOpt.Types.OfType<GenericNameSyntax>().Where(x => x.PlainName == CommandHandlerInterfaceName) : null;
        }
        
    }
}
