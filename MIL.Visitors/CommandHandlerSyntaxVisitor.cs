using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace MIL.Visitors
{
    public class CommandHandlerSyntaxVisitor : SyntaxVisitor<IEnumerable<GenericNameSyntax>>
    {
        private const string CommandHandlerInterfaceName = "ICommandHandler";

        protected override IEnumerable<GenericNameSyntax> VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return node.BaseListOpt != null ? node.BaseListOpt.Types.OfType<GenericNameSyntax>().Where(x => x.PlainName == CommandHandlerInterfaceName) : Enumerable.Empty<GenericNameSyntax>();
        }
        
    }
}