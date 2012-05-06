using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Common;
using Roslyn.Services.Editor;

namespace Roslyn.Samples.SyntaxVisualizer.Control
{
    
    public class FindReferenceHelper
    {
        [Import(typeof(IFindReferencesService))]
        private IFindReferencesService ReferenceService { get; set; }

        public ITypeSymbol SearchTarget { get; set; }

        public FindReferenceHelper()
        {
            
        }
    }
}
