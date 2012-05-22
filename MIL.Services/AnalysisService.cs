using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using SyntaxHelperUtilities;
using MIL.Visitors;

namespace MIL.Services
{
    public class AnalysisService
    {
        // TODO: Parameterize this name
        private const string ProcessIfxName = "IProcess";

        private readonly Func<NamedTypeSymbol, NamedTypeSymbol> defaultStateDiscoveryStrategy = procSym => procSym.GetMembers().OfType<NamedTypeSymbol>().FirstOrDefault(x => x.TypeKind == TypeKind.Enum); 

        public IEnumerable<string> GetProcessStateNames(Compilation appCompilation, string process)
        {
            var nestedEnum = ExtractProcessFromCompiledSource(appCompilation, process);

            if (nestedEnum == null) return null;
             
            return nestedEnum.StateEnum.MemberNames;
        }

        public MilToken GetProcessToken(Compilation appCompilation, string process)
        {
           var p = ExtractProcessFromCompiledSource(appCompilation, process);
            return p.GetToken();

        }

        private ProcessDefinition ExtractProcessFromCompiledSource(Compilation compilation, string processName)
        {
            var processType =
                from glob in compilation.Assembly.GlobalNamespace
                    .GetMembers()
                    .OfType<NamespaceOrTypeSymbol>()
                from childNs in glob
                    .GetMembers()
                    .OfType<NamespaceSymbol>()
                from childTypes in childNs.GetTypeMembers()
                select childTypes;

            if (!processType.Any()) return null;

            var processSymbol = processType.SingleOrDefault(x => x.Name.Contains(processName));

            if (processSymbol == null) return null;

            var p = new ProcessDefinition(processSymbol, ProcessIfxName);
            p.SetStateEnumUsingStrategy(defaultStateDiscoveryStrategy);
            return p;
        }
    }

    class ProcessDefinition
    {
        
        public ProcessDefinition(NamedTypeSymbol process, string ifxName)
        {
            ProcessInterface = process.Interfaces.First(x => x.Name.Contains(ifxName));
            ProcessName = process.Name;
            ProcessInterfaceName = ifxName;
            ProcessType = process;
        }

        public NamedTypeSymbol StateEnum { get; private set; }
        public PropertySymbol StateProperty { get; private set;}
        public NamedTypeSymbol ProcessType { get; private set; }
        public NamedTypeSymbol ProcessInterface { get; private set; }

        public string ProcessName { get; private set; }
        public string ProcessInterfaceName { get; private set; }

        public void SetStateEnumUsingStrategy(Func<NamedTypeSymbol, NamedTypeSymbol> strategy)
        {
            StateEnum = strategy(ProcessType);
            StateProperty = ProcessType.GetMembers().OfType<PropertySymbol>().First(x => x.Type == StateEnum);
        }
        public MilToken GetToken()
        {
            return TokenFactory.GetStateDefinition(ProcessName, StateProperty.Name, StateEnum.MemberNames);
        }
    }
}
