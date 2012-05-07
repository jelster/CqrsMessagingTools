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
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Samples.SyntaxVisualizer.DgmlHelper;
using Roslyn.Services;
using Roslyn.Services.Editor;

namespace Roslyn.Samples.SyntaxVisualizer.Extension
{
    // Control that hosts SyntaxVisualizerControl inside a Visual Studio ToolWindow. This control implements all the
    // logic necessary for interaction with Visual Studio's code documents and directed syntax graph documents.
    public partial class SyntaxVisualizerContainer : IVsRunningDocTableEvents, IDisposable
    {
        //private readonly MessagingVisualizerContainer parent;
        private IWpfTextView activeWpfTextView;
        private CommonSyntaxTree activeSyntaxTree;

        public SyntaxVisualizerContainer()
        {
            InitializeComponent();

            //this.parent = parent;

            InitializeRunningDocumentTable();

            var shellService = GetService<IVsShell, SVsShell>(GlobalServiceProvider);
            if (shellService != null)
            {
                int canDisplayDirectedSyntaxGraph;

                // DGML directed graphs are not supported in all Visual Studio SKUs. Only enable this feature
                // if the Visual Studio package for DGML is installed.
                shellService.IsPackageInstalled(GuidList.GuidProgressionPkg, out canDisplayDirectedSyntaxGraph);
                syntaxVisualizer.DirectedSyntaxGraphContextMenuEnabled = Convert.ToBoolean(canDisplayDirectedSyntaxGraph);
            }

            syntaxVisualizer.SyntaxNodeNavigationToSourceRequested += node => NavigateToSource(node.Span);
            syntaxVisualizer.SyntaxTokenNavigationToSourceRequested += token => NavigateToSource(token.Span);
            syntaxVisualizer.SyntaxTriviaNavigationToSourceRequested += trivia => NavigateToSource(trivia.Span);

            syntaxVisualizer.SyntaxNodeDirectedGraphRequested += DisplaySyntaxNodeDgml;
            syntaxVisualizer.SyntaxTokenDirectedGraphRequested += DisplaySyntaxTokenDgml;
            syntaxVisualizer.SyntaxTriviaDirectedGraphRequested += DisplaySyntaxTriviaDgml;
        }

        internal void Clear()
        {
            if (activeWpfTextView != null)
            {
                activeWpfTextView.Selection.SelectionChanged -= HandleSelectionChanged;
                activeWpfTextView.TextBuffer.Changed -= HandleTextBufferChanged;
                activeWpfTextView = null;
            }

            activeSyntaxTree = null;
            syntaxVisualizer.Clear();
        }

        #region Helpers - GetService
        private static Microsoft.VisualStudio.OLE.Interop.IServiceProvider globalServiceProvider;
        private static Microsoft.VisualStudio.OLE.Interop.IServiceProvider GlobalServiceProvider
        {
            get
            {
                return globalServiceProvider ??
                       (globalServiceProvider =
                        (Microsoft.VisualStudio.OLE.Interop.IServiceProvider) Package.GetGlobalService(
                            typeof (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)));
            }
        }

        private TServiceInterface GetService<TServiceInterface, TService>() where TServiceInterface : class where TService : class
        {
            //TServiceInterface service = null;

            //if (parent != null)
            //{
            //    service = parent.GetVsService<TServiceInterface, TService>();
            //}

            return GetService<TServiceInterface, TService>(GlobalServiceProvider);
        }
        
        private static object GetService(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider, Guid guidService, bool unique)
        {
            var guidInterface = VSConstants.IID_IUnknown;
            var ptr = IntPtr.Zero;
            object service = null;

            if (serviceProvider.QueryService(ref guidService, ref guidInterface, out ptr) == 0 && ptr != IntPtr.Zero)
            {
                try
                {
                    service = unique ? Marshal.GetUniqueObjectForIUnknown(ptr) : Marshal.GetObjectForIUnknown(ptr);
                }
                finally
                {
                    Marshal.Release(ptr);
                }
            }

            return service;
        }

        private static TServiceInterface GetService<TServiceInterface, TService>(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider)
            where TServiceInterface : class
            where TService : class
        {
            return (TServiceInterface)GetService(serviceProvider, typeof(TService).GUID, false);
        }

        private static TServiceInterface GetMefService<TServiceInterface>() where TServiceInterface : class
        {
            TServiceInterface service = null;
            var componentModel = GetService<IComponentModel, SComponentModel>(GlobalServiceProvider);

            if (componentModel != null)
            {
                service = componentModel.GetService<TServiceInterface>();
            }

            return service;
        }
        #endregion

        #region Helpers - Initialize and Dispose IVsRunningDocumentTable
        private uint runningDocumentTableCookie;

        private IVsRunningDocumentTable runningDocumentTable;
        private IVsRunningDocumentTable RunningDocumentTable
        {
            get
            {
                if (runningDocumentTable == null)
                {
                    runningDocumentTable = GetService<IVsRunningDocumentTable, SVsRunningDocumentTable>(GlobalServiceProvider);
                }

                return runningDocumentTable;
            }
        }

        private void InitializeRunningDocumentTable()
        {
            if (RunningDocumentTable != null)
            {
                RunningDocumentTable.AdviseRunningDocTableEvents(this, out runningDocumentTableCookie);
            }
        }

        void IDisposable.Dispose()
        {
            if (runningDocumentTableCookie != 0)
            {
                runningDocumentTable.UnadviseRunningDocTableEvents(runningDocumentTableCookie);
                runningDocumentTableCookie = 0;
            }
        }
        #endregion

        #region Event Handlers - Editor / IVsRunningDocTableEvents Events
        private IWorkspaceDiscoveryService workspaceDiscoveryService;
        private IWorkspaceDiscoveryService WorkspaceDiscoveryService
        {
            get
            {
                // Get workspace discovery service.
                if (workspaceDiscoveryService == null)
                {
                    workspaceDiscoveryService = GetMefService<IWorkspaceDiscoveryService>();
                }

                return workspaceDiscoveryService;
            }
        }

        // Populate the treeview with the contents of the SyntaxTree corresponding to the code document
        // that is currently active in the editor.
        protected virtual void RefreshSyntaxVisualizer()
        {
            if (IsVisible && activeWpfTextView != null && WorkspaceDiscoveryService != null)
            {
                var snapshot = activeWpfTextView.TextBuffer.CurrentSnapshot;
                var contentType = snapshot.ContentType;

                if (contentType.IsOfType(ContentTypeNames.VisualBasicContentType) ||
                    contentType.IsOfType(ContentTypeNames.CSharpContentType))
                {
                    var text = snapshot.AsText();

                    // Get the Workspace corresponding to the currently active text snapshot.
                    var workspace = WorkspaceDiscoveryService.GetWorkspace(text.Container);

                    if (workspace != null)
                    {
                        IDocument document;

                        // Get the Document corresponding to the currently active text snapshot.
                        if (workspace.TryGetDocumentFromInProgressSolution(text, out document) && document != null)
                        {
                            // Get the SyntaxTree corresponding to the Document.
                            activeSyntaxTree = document.GetSyntaxTree();

                            // Display the SyntaxTree.
                            if (contentType.IsOfType(ContentTypeNames.VisualBasicContentType))
                            {
                                syntaxVisualizer.DisplaySyntaxTree(activeSyntaxTree, LanguageNames.VisualBasic);
                            }
                            else if (contentType.IsOfType(ContentTypeNames.CSharpContentType))
                            {
                                syntaxVisualizer.DisplaySyntaxTree(activeSyntaxTree, LanguageNames.CSharp);
                            }

                            NavigateFromSource();
                        }
                    }
                }
            }
        }

        // When user clicks / selects text in the editor select the corresponding item in the treeview.
        private void NavigateFromSource()
        {
            if (IsVisible && activeWpfTextView != null)
            {
                var span = activeWpfTextView.Selection.StreamSelectionSpan.SnapshotSpan.Span;
                syntaxVisualizer.NavigateToBestMatch(span.Start, span.Length);
            }
        }

        // When user clicks on a particular item in the treeview select the corresponding text in the editor.
        private void NavigateToSource(TextSpan span)
        {
            if (IsVisible && activeWpfTextView != null)
            {
                var snapShotSpan = span.ToSnapshotSpan(activeWpfTextView.TextBuffer.CurrentSnapshot);

                // See SyntaxVisualizerToolWindow_GotFocus and SyntaxVisualizerToolWindow_LostFocus
                // for some notes about selection opacity and why it needs to be manipulated.
                activeWpfTextView.Selection.Select(snapShotSpan, false);
                activeWpfTextView.ViewScroller.EnsureSpanVisible(snapShotSpan);
            }
        }

        private void HandleSelectionChanged(object sender, EventArgs e)
        {
            NavigateFromSource();
        }

        private void HandleTextBufferChanged(object sender, EventArgs e)
        {
            RefreshSyntaxVisualizer();
        }

        // Handle the case where the user opens a new code document / switches to a different code document.
        int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int isFirstShow, IVsWindowFrame vsWindowFrame)
        {
            if (IsVisible && isFirstShow == 0)
            {
                var wpfTextView = vsWindowFrame.ToWpfTextView();
                if (wpfTextView != null)
                {
                    var contentType = wpfTextView.TextBuffer.ContentType;
                    if (contentType.IsOfType(ContentTypeNames.VisualBasicContentType) ||
                        contentType.IsOfType(ContentTypeNames.CSharpContentType))
                    {
                        Clear();
                        activeWpfTextView = wpfTextView;
                        activeWpfTextView.Selection.SelectionChanged += HandleSelectionChanged;
                        activeWpfTextView.TextBuffer.Changed += HandleTextBufferChanged;
                        RefreshSyntaxVisualizer();
                    }
                }
            }

            return VSConstants.S_OK;
        }

        // Handle the case where the user closes the current code document / switches to a different code document.
        int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame vsWindowFrame)
        {
            if (IsVisible && activeWpfTextView != null)
            {
                var wpfTextView = vsWindowFrame.ToWpfTextView();
                if (wpfTextView == activeWpfTextView)
                {
                    Clear();
                }
            }

            return VSConstants.S_OK;
        }

        #region Unused IVsRunningDocTableEvents Events
        int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint lockType, uint readLocksRemaining, uint editLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint lockType, uint readLocksRemaining, uint editLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }
        #endregion
        #endregion

        #region Event Handlers - Directed Syntax Graph
        private IVsFileChangeEx fileChangeService;
        private IVsFileChangeEx FileChangeService
        {
            get
            {
                if (fileChangeService == null)
                {
                    fileChangeService = GetService<IVsFileChangeEx, SVsFileChangeEx>();
                }

                return fileChangeService;
            }
        }

        private IServiceProvider dte2ServiceProvider;
        private IServiceProvider DTE2ServiceProvider
        {
            get
            {
                if (dte2ServiceProvider == null)
                {
                    var dte2 = GetService<DTE2, DTE>();
                    dte2ServiceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte2);
                }

                return dte2ServiceProvider;
            }
        }

        private void DisplayDgml(XElement dgml)
        {
            var filePath = Path.Combine(Path.GetTempPath(), "Syntax.dgml");

            uint docItemId;
            IVsUIHierarchy docUIHierarchy;
            IVsWindowFrame docWindowFrame;

            // Check whether the file is already open in the 'design' view.
            // If the file is already open in the desired view then we will update the 
            // contents of the file on disk with the new directed syntax graph and load 
            // this new graph into the already open view of the file.
            if (VsShellUtilities.IsDocumentOpen(
                DTE2ServiceProvider, filePath, GuidList.GuidVsDesignerViewKind,
                out docUIHierarchy, out docItemId, out docWindowFrame) && docWindowFrame != null)
            {
                IVsHierarchy docHierarchy;
                uint docCookie;
                IntPtr docDataIUnknownPointer;

                if (RunningDocumentTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, filePath,
                                                             out docHierarchy, out docItemId,
                                                             out docDataIUnknownPointer,
                                                             out docCookie) == VSConstants.S_OK &&
                    docDataIUnknownPointer != null)
                {
                    IntPtr persistDocDataServicePointer;
                    var persistDocDataServiceGuid = typeof(IVsPersistDocData).GUID;

                    if (Marshal.QueryInterface(docDataIUnknownPointer, ref persistDocDataServiceGuid,
                                               out persistDocDataServicePointer) == 0 &&
                        persistDocDataServicePointer != null)
                    {
                        try
                        {
                            IVsPersistDocData persistDocDataService =
                                (IVsPersistDocData)Marshal.GetObjectForIUnknown(persistDocDataServicePointer);

                            if (persistDocDataService != null)
                            {
                                const int TRUE = -1, FALSE = 0;

                                // The below call ensures that there are no pop-ups from Visual Studio
                                // prompting the user to reload the file each time it is changed.
                                FileChangeService.IgnoreFile(0, filePath, TRUE);

                                // Update the file on disk with the new directed syntax graph.
                                dgml.Save(filePath);

                                // The below calls ensure that the file is refreshed inside Visual Studio
                                // so that the latest contents are displayed to the user.
                                FileChangeService.SyncFile(filePath);
                                persistDocDataService.ReloadDocData((uint)_VSRELOADDOCDATA.RDD_IgnoreNextFileChange);

                                // Re-enable pop-ups from Visual Studio prompting the user to reload the file 
                                // in case the file is ever changed by some other process.
                                FileChangeService.IgnoreFile(0, filePath, FALSE);

                                // Make sure the directed syntax graph window is visible but don't give it focus.
                                docWindowFrame.ShowNoActivate();
                            }
                        }
                        finally
                        {
                            Marshal.Release(persistDocDataServicePointer);
                        }
                    }
                }
            }
            else
            {
                // File is not open in the 'design' view. But it may be open in the 'xml' view.
                // If the file is open in any other view than 'design' view then we will close it
                // so that there are no pop-ups from Visual Studio about the file already being open.
                if (VsShellUtilities.IsDocumentOpen(
                    DTE2ServiceProvider, filePath, Guid.Empty,
                    out docUIHierarchy, out docItemId, out docWindowFrame) && docWindowFrame != null)
                {
                    docWindowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                }

                // Update the file on disk with the new directed syntax graph.
                dgml.Save(filePath);

                // Open the new directed syntax graph in the 'design' view.
                VsShellUtilities.OpenDocument(
                    DTE2ServiceProvider, filePath, GuidList.GuidVsDesignerViewKind,
                    out docUIHierarchy, out docItemId, out docWindowFrame);
            }
        }

        private void DisplaySyntaxNodeDgml(CommonSyntaxNode node)
        {
            if (activeWpfTextView != null)
            {
                var snapshot = activeWpfTextView.TextBuffer.CurrentSnapshot;
                var contentType = snapshot.ContentType;
                XElement dgml = null;

                if (contentType.IsOfType(ContentTypeNames.CSharpContentType))
                {
                    dgml = node.ToDgml(LanguageNames.CSharp, activeSyntaxTree);
                }
                else if (contentType.IsOfType(ContentTypeNames.VisualBasicContentType))
                {
                    dgml = node.ToDgml(LanguageNames.VisualBasic, activeSyntaxTree);
                }

                DisplayDgml(dgml);
            }
        }

        private void DisplaySyntaxTokenDgml(CommonSyntaxToken token)
        {
            if (activeWpfTextView != null)
            {
                var snapshot = activeWpfTextView.TextBuffer.CurrentSnapshot;
                var contentType = snapshot.ContentType;
                XElement dgml = null;

                if (contentType.IsOfType(ContentTypeNames.CSharpContentType))
                {
                    dgml = token.ToDgml(LanguageNames.CSharp, activeSyntaxTree);
                }
                else if (contentType.IsOfType(ContentTypeNames.VisualBasicContentType))
                {
                    dgml = token.ToDgml(LanguageNames.VisualBasic, activeSyntaxTree);
                }

                DisplayDgml(dgml);
            }
        }

        private void DisplaySyntaxTriviaDgml(CommonSyntaxTrivia trivia)
        {
            if (activeWpfTextView != null)
            {
                var snapshot = activeWpfTextView.TextBuffer.CurrentSnapshot;
                var contentType = snapshot.ContentType;
                XElement dgml = null;

                if (contentType.IsOfType(ContentTypeNames.CSharpContentType))
                {
                    dgml = trivia.ToDgml(LanguageNames.CSharp, activeSyntaxTree);
                }
                else if (contentType.IsOfType(ContentTypeNames.VisualBasicContentType))
                {
                    dgml = trivia.ToDgml(LanguageNames.VisualBasic, activeSyntaxTree);
                }

                DisplayDgml(dgml);
            }
        }
        #endregion

        #region Event Handlers - Other
        private void SyntaxVisualizerToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HandleTextBufferChanged(sender, e);
        }

        private void SyntaxVisualizerToolWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void SyntaxVisualizerToolWindow_GotFocus(object sender, RoutedEventArgs e)
        {
            if (activeWpfTextView != null && !activeWpfTextView.Properties.ContainsProperty("BackupOpacity"))
            {
                var selectionLayer = activeWpfTextView.GetAdornmentLayer(PredefinedAdornmentLayers.Selection);

                // Backup current selection opacity value.
                activeWpfTextView.Properties.AddProperty("BackupOpacity", selectionLayer.Opacity);

                // Set selection opacity to a high value. This ensures that the text selection is visible
                // even when the code editor loses focus (i.e. when user is changing the text selection by
                // clicking on nodes in the TreeView).
                selectionLayer.Opacity = 1;
            }
        }

        private void SyntaxVisualizerToolWindow_LostFocus(object sender, RoutedEventArgs e)
        {
            if (activeWpfTextView != null && activeWpfTextView.Properties.ContainsProperty("BackupOpacity"))
            {
                var selectionLayer = activeWpfTextView.GetAdornmentLayer(PredefinedAdornmentLayers.Selection);

                // Restore backed up selection opacity value.
                selectionLayer.Opacity = (double)activeWpfTextView.Properties.GetProperty("BackupOpacity");
                activeWpfTextView.Properties.RemoveProperty("BackupOpacity");
            }
        }
        #endregion
    }
}