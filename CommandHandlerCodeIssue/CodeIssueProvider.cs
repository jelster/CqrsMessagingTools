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
            if (document.Project == null) return null;

            var classNode = (ClassDeclarationSyntax) node;

            if (classNode.BaseListOpt == null) return null;

            var commandHandlerVisitor = new CommandHandlerSyntaxVisitor();

            var baseTypes = commandHandlerVisitor.Visit(classNode);

            if (baseTypes == null || !baseTypes.Any()) return null;

            var allCommandHandlersInProject = (from allTrees in document.Project.GetCompilation(cancellationToken).SyntaxTrees
                                                from classes in allTrees.Root.DescendentNodes().OfType<ClassDeclarationSyntax>()
                                                    where commandHandlerVisitor.Visit(classes).Any()
                                               select new KeyValuePair<ClassDeclarationSyntax, IEnumerable<GenericNameSyntax>>(classes, commandHandlerVisitor.Visit(classes))).ToList();
            var dic = allCommandHandlersInProject.ToDictionary(x => x.Key, pair => pair.Value);

            if (!dic.Any() || !dic.ContainsKey(classNode)) return null;

            var dupes = dic.SelectMany(x => x.Value, (c, s) => s.GetText()).FindDuplicates().ToList();

            if (dupes.Any())
            {
                var desc = "{0} is implemented by multiple handlers:{1}{2}";
                var issues = new List<CodeIssue>();
                foreach (var dupe in dupes)
                {
                    var listing = FormatHandlerListing(dupe, allCommandHandlersInProject);
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
