using System;
using System.Linq;
using MIL.Visitors;
using Roslyn.Compilers.CSharp;

namespace MIL.Services
{
    public class ProcessDefinition
    {
        internal ProcessDefinition(NamedTypeSymbol process, string ifxName)
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
        public NamedTypeSymbol StateEnum { get; private set; }
        public PropertySymbol StateProperty { get; private set; }
        public NamedTypeSymbol ProcessType { get; private set; }
        public NamedTypeSymbol ProcessInterface { get; private set; }

        public string ProcessName { get; private set; }
        public string ProcessInterfaceName { get; private set; }

        public void SetStateEnumUsingStrategy(Func<NamedTypeSymbol, NamedTypeSymbol> strategy)
        {
            StateEnum = strategy(ProcessType);
            if (StateEnum == null)
                return;

            StateProperty = ProcessType.GetMembers().OfType<PropertySymbol>().First(x => x.Type.Name == StateEnum.Name);
        }

        public static MilToken GetTokenFromDefinition(ProcessDefinition definition)
        {
            return (definition == null || !definition.IsDefinitionComplete) ?
                                                                                TokenFactory.GetStatementTerminator()
                       : TokenFactory.GetStateDefinition(definition.ProcessName, definition.StateProperty.Name, definition.StateEnum.MemberNames);
        }
    }
}