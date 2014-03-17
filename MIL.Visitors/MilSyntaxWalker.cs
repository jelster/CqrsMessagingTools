using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;
using SyntaxHelperUtilities;

namespace MIL.Visitors
{
    public class MilSyntaxWalker : SyntaxWalker
    {
        protected const string CommandIfx = "ICommand";
        protected const string PublishKeyword = "Send";
        protected const string EventIfx = "IEvent";
        protected const string EventHandlerPlainIfx = "IEventHandler";
        protected const string CommandHandlerPlainIfx = "ICommandHandler";
        protected const string AggregateRootPlainIfx = "EventSourced";

        private readonly HandlerDeclarationSyntaxVisitor _cmdHandlerDeclarationVisitor = new HandlerDeclarationSyntaxVisitor(CommandHandlerPlainIfx);
        private readonly HandlerDeclarationSyntaxVisitor _eventHandlerDeclarationVisitor = new HandlerDeclarationSyntaxVisitor(EventHandlerPlainIfx);
        private readonly HandlerDeclarationSyntaxVisitor _aggregateDeclarationVisitor = new HandlerDeclarationSyntaxVisitor(AggregateRootPlainIfx);

        public MilSyntaxWalker() : base(visitIntoStructuredTrivia: true)
        {
            Commands = new List<ClassDeclarationSyntax>();
            Events = new List<ClassDeclarationSyntax>();
            PublicationCalls = new List<MemberAccessExpressionSyntax>();
            EventHandlers = new List<ClassDeclarationSyntax>();
            CommandHandlers = new List<ClassDeclarationSyntax>();
            Publications = new List<InvocationExpressionSyntax>();
            AggregateRoots = new List<ClassDeclarationSyntax>();
        }

        public List<InvocationExpressionSyntax> Publications { get; private set; }
        public List<ClassDeclarationSyntax> Commands { get; private set; }
        public List<ClassDeclarationSyntax> Events { get; private set; }
        public List<ClassDeclarationSyntax> EventHandlers { get; private set; } 
        public List<ClassDeclarationSyntax> CommandHandlers { get; private set; }
        public List<MemberAccessExpressionSyntax> PublicationCalls { get; private set; }
        public List<ClassDeclarationSyntax> AggregateRoots { get; private set; } 

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Name.GetText() != PublishKeyword) return;

            PublicationCalls.Add(node);
            Publications.Add(node.Ancestors().OfType<InvocationExpressionSyntax>().Last());
        }
        //protected override void VisitInvocationExpression(InvocationExpressionSyntax node)
        //{
           
        //}
        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            foreach (var type in node.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                Visit(type);
            }
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
             
                LookForAggregates(node);
                LookForCommands(node);
                LookForCommandHandlers(node);
                LookForEvents(node);
                LookForEventHandlers(node);

                var methods = node.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var method in methods.Where(x => x.Body != null))
                {
                    Visit(method);
                }
            
        }

        private void LookForAggregates(ClassDeclarationSyntax classNode)
        {
            if (classNode.Modifiers.Any(SyntaxKind.AbstractKeyword) || classNode.BaseList == null || !classNode.BaseList.Types.Any()) return;

            if (classNode.BaseList.Types.Any(x => AggregateRootPlainIfx.Contains(x.PlainName))) AggregateRoots.Add(classNode);
            
        }

        private void LookForEvents(ClassDeclarationSyntax node)
        {
            if (node.BaseList != null && node.BaseList.Types.Any(t => t.PlainName == EventIfx))
            {
                Events.Add(node);
            }
        }

        private void LookForCommands(ClassDeclarationSyntax node)
        {
            if (node.BaseList != null && node.BaseList.Types.Any(t => t.PlainName == CommandIfx))
            {
                Commands.Add(node);
            }
        }

        private void LookForEventHandlers(ClassDeclarationSyntax node)
        {
            var handles = _eventHandlerDeclarationVisitor.Visit(node);
            
            if (handles.Any())
            {
                EventHandlers.Add(node);
            }
        }

        private void LookForCommandHandlers(ClassDeclarationSyntax node)
        {
            var handles = _cmdHandlerDeclarationVisitor.Visit(node);

            if (handles.Any())
            {
                CommandHandlers.Add(node);
            }
        }

        public IEnumerable<MilToken> DumpCommandData()
        {
            foreach (var cmd in Commands)
            {
                yield return TokenFactory.GetCommand(cmd.GetClassName());
                yield return TokenFactory.GetPublish();

                var t1 = CommandHandlers.FirstOrDefault(x => x.BaseList.Types.OfType<GenericNameSyntax>()
                                                                 .Any(y => y.TypeArgumentList.Arguments.Any(z => z.GetClassName().Contains(cmd.GetClassName()))));

                yield return TokenFactory.GetCommandHandler(t1 == null ? TokenFactory.GetEmptyToken().ToString() : t1.GetClassName());
                yield return TokenFactory.GetStatementTerminator();
            }
        }

        public IEnumerable<MilToken> DumpEventData()
        {
            foreach (var ev in Events)
            {
                var eventClassName = ev.GetClassName();
                yield return TokenFactory.GetEvent(ev.GetClassName());
                yield return TokenFactory.GetPublish();
                yield return TokenFactory.GetStatementTerminator();

                var t1 = EventHandlers.Where(x => x.BaseList.Types.OfType<GenericNameSyntax>()
                                                           .Any(y => y.TypeArgumentList.Arguments.Any(z => z.GetClassName().Contains(eventClassName)))).ToList();
                foreach (var evHand in t1)
                {
                    var handleName = evHand.GetClassName();
                    yield return TokenFactory.GetIndentationToken();
                    yield return TokenFactory.GetReceive();
                    yield return TokenFactory.GetEventHandler(handleName);
                    yield return TokenFactory.GetStatementTerminator();
                }
            }
        }

        public IEnumerable<string> DumpPublicationData()
        {
            if (!PublicationCalls.Any()) yield break;

            foreach (var pub in Publications)
            {
                yield return string.Format("{0}.{1}.{2}:{3}{4}", 
                    pub.FirstAncestorOrSelf<NamespaceDeclarationSyntax>().Name.GetText(), 
                    pub.FirstAncestorOrSelf<ClassDeclarationSyntax>().Identifier.GetText(),
                    pub.FirstAncestorOrSelf<MethodDeclarationSyntax>().Identifier.GetFullText(),
                    pub.Span.ToString(), 
                    Environment.NewLine);
            }
        }

        public IEnumerable<MilToken> DumpAggregateRoots()
        {
            var r = AggregateRoots.Select(x => TokenFactory.GetAggregateRoot(x.Identifier.GetText()));
            foreach (var agg in r)
            {
                yield return agg;
                yield return TokenFactory.GetStatementTerminator();
            }
        }
    }
}