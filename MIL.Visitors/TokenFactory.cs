using System.Collections.Generic;

namespace MIL.Visitors
{
    public static class TokenFactory
    {
        public static MilToken GetStatementTerminator()
        {
            return new MilToken(MilTypeConstant.EndOfStatementToken);
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

        public static MilToken GetStateDefinition(string processName, string statePropertyName, IEnumerable<string> stateFlags)
        {
            return new MilToken(MilTypeConstant.StateDefinitionToken, string.Format("{0}, {1}:[{2}]", processName, statePropertyName, string.Join(", ", stateFlags)));
        }

        public static MilToken GetEmptyToken()
        {
            return new MilToken(MilTypeConstant.EmptyToken);
        }
    }
}