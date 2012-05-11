using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace MIL.Visitors
{
    public class HandlerDeclarationSyntaxVisitor : SyntaxVisitor<IEnumerable<GenericNameSyntax>>
    {
        private readonly string _handlerInterfaceName;

        public HandlerDeclarationSyntaxVisitor(string handlerInterfaceName)
        {
            _handlerInterfaceName = handlerInterfaceName;
        }

        protected override IEnumerable<GenericNameSyntax> VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return node.BaseListOpt != null ? node.BaseListOpt.Types.OfType<GenericNameSyntax>().Where(x => x.PlainName == _handlerInterfaceName) : Enumerable.Empty<GenericNameSyntax>();
        }
        
    }
}