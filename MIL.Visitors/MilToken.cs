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

        /// <summary>
        /// Represents a named AR (aggregate root), which may also be an event source
        /// </summary>
        public static readonly MilTypeConstant AggregateRootToken;
        
        /// <summary>
        /// Syntactical boundary token. 
        /// </summary>
        public static readonly MilTypeConstant EndOfStatementToken;

        /// <summary>
        /// Left-side (parent) relationship denoting token
        /// </summary>
        public static readonly MilTypeConstant OriginAssociationToken;

        /// <summary>
        /// Right-side (child) relationship denoting token
        /// </summary>
        public static readonly MilTypeConstant DesintationAssociationToken;

        /// <summary>
        /// Token representing logic that responds to domain events. A given event type may have zero or more of these. 
        /// </summary>
        public static readonly MilTypeConstant EventHandlerToken;

        /// <summary>
        /// Token representing the occurrance of something pertinent to the business domain. 
        /// </summary>
        public static readonly MilTypeConstant EventToken;

        /// <summary>
        /// Token that processes a command. A particular <see cref="CommandToken">CommandToken</see> should have one and only one CommandHandler
        /// </summary>
        public static readonly MilTypeConstant CommandHandlerToken;

        /// <summary>
        /// Token representing an individual command message. Handled by an ICommandHandler
        /// </summary>
        public static readonly MilTypeConstant CommandToken;

        /// <summary>
        /// Represents a messaging operation where an event or command is sent (published) via an arbitrary mechanism, usually a Command Bus
        /// </summary>
        public static readonly MilTypeConstant PublishToken;

        /// <summary>
        /// Represents a messaging operation where an event or command is received from some sort of subscription mechanism. 
        /// </summary>
        public static readonly MilTypeConstant ReceiveToken;

        /// <summary>
        /// Actually a composite token, this statement is a signifier that the previous statement resulting in a outwardly-visible change of state for the associated object. 
        /// The new state value is given following the assignment operator ('=')
        /// </summary>
        public static readonly MilTypeConstant StateChangeToken;

        /// <summary>
        /// Used when a command (typically) is pushed out for publication, but not processed immediately. 
        /// </summary>
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

        private static readonly string strStatementToken = Environment.NewLine;
        private const string strHandlerToken = "";
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
}
