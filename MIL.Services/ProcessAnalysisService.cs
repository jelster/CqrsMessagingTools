using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using MIL.Visitors;

namespace MIL.Services
{
    public class ProcessAnalysisService
    {
        private readonly string ProcessIfxName;

        private readonly Func<INamedTypeSymbol, INamedTypeSymbol> defaultStateDiscoveryStrategy = procSym => procSym.GetMembers().OfType<INamedTypeSymbol>().FirstOrDefault(x => x.TypeKind == TypeKind.Enum);

        public ProcessAnalysisService(string processInterfaceName = "IProcess")
        {
            ProcessIfxName = processInterfaceName;
        }

        public IEnumerable<string> GetProcessStateNames(Compilation appCompilation, string process)
        {
            var processDefinition = ExtractProcessFromCompiledSource(appCompilation, process);

            if (processDefinition == null) return null;

            return processDefinition.StateEnum.MemberNames;
        }

        public MilToken GetProcessToken(Compilation appCompilation, string process = "")
        {
            if (appCompilation == null) throw new ArgumentNullException("appCompilation");

            var p = ExtractProcessFromCompiledSource(appCompilation, process);
            if (p == null) return TokenFactory.GetEmptyToken();

            return ProcessDefinition.GetTokenFromDefinition(p);
        }

        public ProcessDefinition GetProcessDefinition(Compilation compilation, string processName = "")
        {
            return ExtractProcessFromCompiledSource(compilation, processName);
        }

        private ProcessDefinition ExtractProcessFromCompiledSource(Compilation compilation, string processName)
        {
            var w = new Walk();
            var processType = w.Visit(compilation.SourceModule.GlobalNamespace);

            if (!processType.Any()) return null;

            INamedTypeSymbol processSymbol = null;
            if (string.IsNullOrWhiteSpace(processName))
            {
                processSymbol = processType.FirstOrDefault(x => x.Interfaces.Any(i => i.Name == ProcessIfxName));
            }
            else
            {
                processSymbol = processType.FirstOrDefault(x => x.Name.Contains(processName) && x.Interfaces.Any(i => i.Name == ProcessIfxName));
            }

            if (processSymbol == null)
                return null;

            var p = new ProcessDefinition(processSymbol, ProcessIfxName);
            p.SetStateEnumUsingStrategy(defaultStateDiscoveryStrategy);

            return p;
        }
    }
}
