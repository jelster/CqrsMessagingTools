using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using MIL.Visitors;

namespace MIL.Services
{
    public class ProcessDefinition
    {
        internal ProcessDefinition(INamedTypeSymbol process, string ifxName)
        {
            ProcessInterface = process.Interfaces.ToList().FirstOrDefault(x => x.Name == ifxName);
            if (ProcessInterface == null)
            {
                throw new ArgumentException(string.Format("No classes implementing {0} found in type {1}", ifxName, process.Name));
            }
            ProcessName = process.Name;
            ProcessInterfaceName = ifxName;
            ProcessType = process;
        }

        public bool IsDefinitionComplete { get { return StateEnum != null && StateProperty != null && ProcessType != null && ProcessInterface != null; } }
        public INamedTypeSymbol StateEnum { get; private set; }
        public IPropertySymbol StateProperty { get; private set; }
        public INamedTypeSymbol ProcessType { get; private set; }
        public INamedTypeSymbol ProcessInterface { get; private set; }

        public string ProcessName { get; private set; }
        public string ProcessInterfaceName { get; private set; }

        public void SetStateEnumUsingStrategy(Func<INamedTypeSymbol, INamedTypeSymbol> strategy)
        {
            StateEnum = strategy(ProcessType);
            if (StateEnum == null)
                return;

            StateProperty = ProcessType.GetMembers().OfType<IPropertySymbol>().First(x => x.Type.Name == StateEnum.Name);
        }

        public static MilToken GetTokenFromDefinition(ProcessDefinition definition)
        {
            return (definition == null || !definition.IsDefinitionComplete) ?
                                                                                TokenFactory.GetStatementTerminator()
                       : TokenFactory.GetStateDefinition(definition.ProcessName, definition.StateProperty.Name, definition.StateEnum.MemberNames);
        }
    }
}