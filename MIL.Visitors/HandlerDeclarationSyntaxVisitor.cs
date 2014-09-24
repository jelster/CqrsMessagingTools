using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MIL.Visitors
{
    public class HandlerDeclarationSyntaxVisitor : CSharpSyntaxVisitor<IEnumerable<GenericNameSyntax>>
    {
        private readonly string _handlerInterfaceName;

        public HandlerDeclarationSyntaxVisitor(string handlerInterfaceName)
        {
            _handlerInterfaceName = handlerInterfaceName;
        }

        public override IEnumerable<GenericNameSyntax> VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // Dong Xie: here replaced x.PlainName, not sure about, REVIEW

            return node.BaseList != null ? node.BaseList.Types.OfType<GenericNameSyntax>().Where(x => x.Identifier.Text == _handlerInterfaceName) : Enumerable.Empty<GenericNameSyntax>();
        }
    }
}