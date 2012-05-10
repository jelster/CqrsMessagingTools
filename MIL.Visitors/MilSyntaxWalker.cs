using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace MIL.Visitors
{
    public class MilSyntaxWalker : SyntaxWalker
    {
        public IList<ClassDeclarationSyntax> Commands { get; private set; }
        public IList<ClassDeclarationSyntax> Events { get; private set; }

        public IEnumerable<ClassDeclarationSyntax> EventHandlers { get { return EventToEventHandlersMapping.Keys.AsEnumerable(); } }
        protected IDictionary<ClassDeclarationSyntax, IEnumerable<ClassDeclarationSyntax>> EventToEventHandlersMapping { get; set; }

        public IEnumerable<ClassDeclarationSyntax> CommandHandlers { get { return CommandHandlerToCommandMapping.Keys.AsEnumerable(); } }
        protected IDictionary<ClassDeclarationSyntax, IEnumerable<GenericNameSyntax>> CommandHandlerToCommandMapping { get; set; }

        public IList<SyntaxNodeOrToken> PublicationCalls { get; private set; }

        public MilSyntaxWalker()
        {
            Commands = new List<ClassDeclarationSyntax>();
            Events = new List<ClassDeclarationSyntax>();
            CommandHandlerToCommandMapping = new Dictionary<ClassDeclarationSyntax, IEnumerable<GenericNameSyntax>>();
            PublicationCalls = new List<SyntaxNodeOrToken>();
            EventToEventHandlersMapping = new Dictionary<ClassDeclarationSyntax, IEnumerable<ClassDeclarationSyntax>>();
        }

        protected const string CommandIfx = "ICommand";
        protected const string PublishKeyword = "Send";
        protected const string EventIfx = "IEvent";
        protected const string EventHandlerPlainIfx = "IEventHandler";

        private readonly CommandHandlerSyntaxVisitor cmdHandlerVisitor = new CommandHandlerSyntaxVisitor();
        
        protected override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var memberExpression = node.Expression as MemberAccessExpressionSyntax;
            if (memberExpression != null && memberExpression.Name.GetText() == PublishKeyword)
            {
                PublicationCalls.Add(node);
                // TODO: gather more info about who invoked method
            }

        }

        protected override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.BaseListOpt != null && node.BaseListOpt.Types.Any(t => t.PlainName == CommandIfx))
            {
                Commands.Add(node);
                return;
            }
            var handles = cmdHandlerVisitor.Visit(node);
            if (handles.Any())
            {
                CommandHandlerToCommandMapping.Add(node, handles);
                return;
            }
        }
    }

    public interface IMilToken
    {
        MilTokenType Kind { get; }
        string Name { get; }
    }

    public struct MilCommandMessage : IMilToken
    {
        private const MilTokenType kind = MilTokenType.Command;
        public string CommandName;

        public MilTokenType Kind
        {
            get { return kind; }
        }

        public string Name { get; set; }
    }

    public struct MilCommandHandler : IMilToken
    {
        private const MilTokenType kind = MilTokenType.CommandHandler;

        public MilTokenType Kind
        {
            get { return kind; }
        }

        public string Name { get; set; }
    }

    public enum MilTokenType
    {
        Indeterminate = 0,
        Command,
        CommandHandler,
        Event,
        EventHandler,
        AggregateRoot,
        StateObject,
        Publisher,
        Scope,
        Delay
    }
}
