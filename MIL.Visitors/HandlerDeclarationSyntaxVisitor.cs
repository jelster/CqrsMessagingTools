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

        public override IEnumerable<GenericNameSyntax> VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return node.BaseList != null ? node.BaseList.Types.OfType<GenericNameSyntax>().Where(x => x.PlainName == _handlerInterfaceName) : Enumerable.Empty<GenericNameSyntax>();
        }
        
    }
}