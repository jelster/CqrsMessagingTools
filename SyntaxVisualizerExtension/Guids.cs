// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System;

namespace Roslyn.Samples.SyntaxVisualizer.Extension
{
    internal static class GuidList
    {
        internal const string GuidSyntaxVisualizerExtensionPkgString = "1e4ce699-d626-42c3-9803-cac3cd73ed77";
        internal const string GuidSyntaxVisualizerExtensionCmdSetString = "8443a724-465d-4706-a46c-3f7bc8eb7588";
        internal const string GuidToolWindowPersistanceString = "da7e21aa-da94-452d-8aa1-d1b23f73f576";

        internal static readonly Guid GuidSyntaxVisualizerExtensionCmdSet = new Guid(GuidSyntaxVisualizerExtensionCmdSetString);
        internal static readonly Guid GuidProgressionPkg = new Guid("AD1A73B0-C489-4C9C-B1FE-EEA54CD19A4F");
        internal static readonly Guid GuidVsDesignerViewKind = new Guid(EnvDTE.Constants.vsViewKindDesigner);
    }
}