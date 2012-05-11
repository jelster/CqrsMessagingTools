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

        public IEnumerable<ClassDeclarationSyntax> EventHandlers { get { return EventToEventHandlersMapping.Values.SelectMany(x => x).AsEnumerable(); } }
        protected IDictionary<GenericNameSyntax, List<ClassDeclarationSyntax>> EventToEventHandlersMapping { get; set; }

        public IEnumerable<ClassDeclarationSyntax> CommandHandlers { get { return CommandHandlerToCommandMapping.Keys.AsEnumerable(); } }
        protected IDictionary<ClassDeclarationSyntax, List<GenericNameSyntax>> CommandHandlerToCommandMapping { get; set; }

        public IList<SyntaxNodeOrToken> PublicationCalls { get; private set; }

        public MilSyntaxWalker()
        {
            Commands = new List<ClassDeclarationSyntax>();
            Events = new List<ClassDeclarationSyntax>();
            CommandHandlerToCommandMapping = new Dictionary<ClassDeclarationSyntax, List<GenericNameSyntax>>();
            PublicationCalls = new List<SyntaxNodeOrToken>();
            EventToEventHandlersMapping = new Dictionary<GenericNameSyntax, List<ClassDeclarationSyntax>>();
        }

        protected const string CommandIfx = "ICommand";
        protected const string PublishKeyword = "Send";
        protected const string EventIfx = "IEvent";
        protected const string EventHandlerPlainIfx = "IEventHandler";
        protected const string CommandHandlerPlainIfx = "ICommandHandler";

        private readonly HandlerDeclarationSyntaxVisitor _cmdHandlerDeclarationVisitor = new HandlerDeclarationSyntaxVisitor(CommandHandlerPlainIfx);
        private readonly HandlerDeclarationSyntaxVisitor _eventHandlerDeclarationVisitor = new HandlerDeclarationSyntaxVisitor(EventHandlerPlainIfx);
        
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
            if (LookForCommands(node)) return;

            if (LookForCommandHandlers(node)) return;

            if (LookForEvents(node)) return;

            if (LookForEventHandlers(node)) return;
        }

        private bool LookForEvents(ClassDeclarationSyntax node)
        {
            if (node.BaseListOpt != null && node.BaseListOpt.Types.Any(t => t.PlainName == EventIfx))
            {
                Events.Add(node);
                return true;
            }
            return false;
        }

        private bool LookForCommands(ClassDeclarationSyntax node)
        {
            if (node.BaseListOpt != null && node.BaseListOpt.Types.Any(t => t.PlainName == CommandIfx))
            {
                Commands.Add(node);
                return true;
            }
            return false;
        }

        private bool LookForEventHandlers(ClassDeclarationSyntax node)
        {
            var handles = _eventHandlerDeclarationVisitor.Visit(node);
            if (handles.Any())
            {
                foreach (var eventTypeName in handles)
                {
                    if (EventToEventHandlersMapping.ContainsKey(eventTypeName))
                    {
                        EventToEventHandlersMapping[eventTypeName].Add(node);
                    }
                    else
                    {
                        EventToEventHandlersMapping.Add(eventTypeName, new List<ClassDeclarationSyntax>() { node });
                    }
                }
                return true;
            }
            return false;
        }

        private bool LookForCommandHandlers(ClassDeclarationSyntax node)
        {
            var handles = _cmdHandlerDeclarationVisitor.Visit(node);
            if (handles.Any())
            {
                CommandHandlerToCommandMapping.Add(node, handles.ToList());
                return true;
            }
            return false;
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
