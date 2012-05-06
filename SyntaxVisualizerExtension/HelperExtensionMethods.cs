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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Roslyn.Samples.SyntaxVisualizer.Extension
{
    internal static class HelperExtensionMethods
    {
        internal static IWpfTextView ToWpfTextView(this IVsWindowFrame vsWindowFrame)
        {
            IWpfTextView wpfTextView = null;
            var vsTextView = VsShellUtilities.GetTextView(vsWindowFrame);

            if (vsTextView != null)
            {
                object textViewHost;
                Guid guidTextViewHost = DefGuidList.guidIWpfTextViewHost;
                if (((IVsUserData)vsTextView).GetData(ref guidTextViewHost, out textViewHost) == VSConstants.S_OK && 
                    textViewHost != null)
                {
                    wpfTextView = ((IWpfTextViewHost)textViewHost).TextView;
                }
            }

            return wpfTextView;
        }

        internal static SnapshotSpan ToSnapshotSpan(this Roslyn.Compilers.TextSpan textSpan, ITextSnapshot snapshot)
        {
            return new SnapshotSpan(snapshot, new Span(textSpan.Start, textSpan.Length));
        }
    }
}
