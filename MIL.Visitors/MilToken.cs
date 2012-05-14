using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MIL.Visitors
{
    public enum AssociationType
    {
        Origin,
        Destination
    }

    public sealed class MilTypeConstant
    {
        public readonly MilTokenType MilTokenType;
        public readonly string MilToken;

        private readonly string tokenFormat;

        public static IEnumerable<MilTypeConstant> AllTokens;

        public static readonly MilTypeConstant AggregateRootToken;
        public static readonly MilTypeConstant EndOfStatementToken;
        public static readonly MilTypeConstant ScopeToken;
        public static readonly MilTypeConstant OriginAssociationToken;
        public static readonly MilTypeConstant DesintationAssociationToken;
        public static readonly MilTypeConstant EventHandlerToken;
        public static readonly MilTypeConstant EventToken;
        public static readonly MilTypeConstant CommandHandlerToken;
        public static readonly MilTypeConstant CommandToken;
        public static readonly MilTypeConstant PublishToken;
        public static readonly MilTypeConstant ReceiveToken;
        public static readonly MilTypeConstant HandlerToken;
        public readonly static MilTypeConstant StateChangeToken;

        public static readonly MilTypeConstant DelaySend;

        static MilTypeConstant()
        {
            // TODO: initialize these services in some other way...
            
            var tokenList = new List<MilTypeConstant>();
            
            // Note on convention: first arg ({0}) will always be MemberName. Second ({1}) is the token string

            CommandToken = new MilTypeConstant(MilTokenType.Command, strCommandToken, "{0}{1}");
            tokenList.Add(CommandToken);
            
            EventToken = new MilTypeConstant(MilTokenType.Event, strEventToken, "{0}{1}");
            tokenList.Add(EventToken);

            PublishToken = new MilTypeConstant(MilTokenType.Publisher, strPublishReceiveToken, "{0}{1}");
            tokenList.Add(PublishToken);

            ReceiveToken = new MilTypeConstant(MilTokenType.Publisher, strPublishReceiveToken, "{1}{0}");
            tokenList.Add(ReceiveToken);

            EndOfStatementToken = new MilTypeConstant(MilTokenType.LanguageElement, strStatementToken, "{0}{1}");
            tokenList.Add(EndOfStatementToken);

            CommandHandlerToken = new MilTypeConstant(MilTokenType.CommandHandler, strHandlerToken, "{1}{0}{1}");
            tokenList.Add(CommandHandlerToken);

            AggregateRootToken = new MilTypeConstant(MilTokenType.AggregateRoot, strAggregateRootToken, "{1}{0}");
            tokenList.Add(AggregateRootToken);

            ScopeToken = new MilTypeConstant(MilTokenType.Scope, strContextScopeToken, "{0}{0}");
            tokenList.Add(ScopeToken);

            OriginAssociationToken = new MilTypeConstant(MilTokenType.Association, strContextScopeToken, "{0}{1}");
            tokenList.Add(OriginAssociationToken);
            
            DesintationAssociationToken = new MilTypeConstant(MilTokenType.Association, strContextScopeToken, "{1}{0}");
            tokenList.Add(DesintationAssociationToken);

            EventHandlerToken = new MilTypeConstant(MilTokenType.EventHandler, strHandlerToken, "{1}{0}{1}");
            tokenList.Add(EventHandlerToken);

            StateChangeToken = new MilTypeConstant(MilTokenType.StateObject, strStateChangeToken, "{1}{0}");
            tokenList.Add(StateChangeToken);

            DelaySend = new MilTypeConstant(MilTokenType.Delay, strDelayToken, "{0}{1}");
            tokenList.Add(DelaySend);

            AllTokens = tokenList;
        }

        private MilTypeConstant(MilTokenType type, string token, string format)
        {
            MilToken = token;
            MilTokenType = type;
            tokenFormat = format;
        }

        private MilTypeConstant() {}
        private static readonly string strStatementToken = Environment.NewLine;
        private static readonly string strHandlerToken = "";
        private const string strCommandToken = "?";
        private const string strEventToken = "!";
        private const string strPublishReceiveToken = " -> ";
        private const string strAggregateRootToken = "@";
        private const string strStateChangeToken = "*";
        private const string strDelayToken = " [Delay] ";
        private const string strContextScopeToken = ":";
        private const string strProcessingHintToken = "%";
        private const string strStateValueOpenToken = "[";
        private const string strStateValueSeparatorToken = ",";
        private const string strStateValueCloseToken = "]";
        private const string strStateAssignmentToken = " = ";

        public static string GetFormat(MilTypeConstant typeConstant)
        {
            return typeConstant.tokenFormat;
        }
    }

    public class MilToken
    {
        public MilTypeConstant Token;
        public string MemberName;

        public MilToken(MilTypeConstant token) : this(token, "") {}
        public MilToken(MilTypeConstant token, string name)
        {
            Token = token;
            MemberName = name;
        }

        public override string ToString()
        {
            return string.Format(MilTypeConstant.GetFormat(Token), MemberName, Token.MilToken);
        }
    }

    public static class TokenFactory
    {
        public static MilToken GetStatementTerminator()
        {
            return new MilToken(MilTypeConstant.EndOfStatementToken);
        }

        public static MilToken GetScope()
        {
            return new MilToken(MilTypeConstant.ScopeToken);
        }

        public static MilToken GetReceive()
        {
            return new MilToken(MilTypeConstant.ReceiveToken);
        }

        public static MilToken GetPublish()
        {
            return new MilToken(MilTypeConstant.PublishToken);
        }

        public static MilToken GetEvent(string foovent)
        {
            return new MilToken(MilTypeConstant.EventToken, foovent);
        }

        public static MilToken GetAggregateRoot(string aggregateRoot)
        {
            return new MilToken(MilTypeConstant.AggregateRootToken, aggregateRoot);
        }

        public static MilToken GetCommandHandler(string handler)
        {
            return new MilToken(MilTypeConstant.CommandHandlerToken, handler);
        }

        public static MilToken GetCommand(string command)
        {
            return new MilToken(MilTypeConstant.CommandToken, command);
        }

        public static MilToken GetAssociation(AssociationType direction)
        {
            return new MilToken(direction == AssociationType.Origin 
                ? MilTypeConstant.OriginAssociationToken
                : MilTypeConstant.DesintationAssociationToken);
        }

        public static MilToken GetEventHandler(string handler)
        {
            return new MilToken(MilTypeConstant.EventHandlerToken, handler);
        }

        public static MilToken GetStateChangeExpression(string statePropertyPath, string newState)
        {
            return new MilToken(MilTypeConstant.StateChangeToken, string.Format("{0} = {1}", statePropertyPath, newState));
        }

        public static MilToken GetDelay()
        {
            return new MilToken(MilTypeConstant.DelaySend);
        }
    }
}
