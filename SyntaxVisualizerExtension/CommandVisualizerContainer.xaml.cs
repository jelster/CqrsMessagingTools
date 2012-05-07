using System;
using System.Runtime.InteropServices;
using System.Windows;

using System.Linq;
using System.IO;
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
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.Editor;
using Roslyn.Samples.SyntaxVisualizer.DgmlHelper;
 

namespace Roslyn.Samples.SyntaxVisualizer.Extension
{
    /// <summary>
    /// Interaction logic for CommandVisualizerContainer.xaml
    /// </summary>
    public partial class CommandVisualizerContainer : IVsBuildStatusCallback, IDisposable
    {
        private readonly SyntaxVisualizerToolWindow parent;

        internal CommandVisualizerContainer(SyntaxVisualizerToolWindow parent)
        {
            InitializeComponent();

            this.parent = parent;
        }

        internal void Clear()
        {
             
            syntaxVisualizer.Clear();
        }

        #region Helpers - GetService
        private static Microsoft.VisualStudio.OLE.Interop.IServiceProvider globalServiceProvider;
        private static Microsoft.VisualStudio.OLE.Interop.IServiceProvider GlobalServiceProvider
        {
            get
            {
                if (globalServiceProvider == null)
                {
                    globalServiceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)Package.GetGlobalService(
                        typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider));
                }

                return globalServiceProvider;
            }
        }

        private TServiceInterface GetService<TServiceInterface, TService>() where TServiceInterface : class where TService : class
        {
            TServiceInterface service = null;

            if (parent != null)
            {
                service = parent.GetVsService<TServiceInterface, TService>();
            }

            return service;
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

        private static TServiceInterface GetService<TServiceInterface, TService>(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider) where TServiceInterface : class where TService : class
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
            syntaxVisualizer.Clear();
            if (!IsVisible ||   WorkspaceDiscoveryService == null) return;

            // Get the Workspace corresponding to the currently active text snapshot.
            var workspace = WorkspaceDiscoveryService.PrimaryWorkspace;

            if (workspace != null)
            {

                var compiled = (from p in workspace.CurrentSolution.Projects
                                from d in p.Documents
                                let model = d.GetSemanticModel()
                                let tree = d.GetSyntaxTree().Root.DescendentNodes().OfType<TypeDeclarationSyntax>()
                                select new {model, tree}).ToList();

                if (!compiled.Any())
                {
                    return;
                }

                var interfaceType = syntaxVisualizer.TargetSymbol; // TODO: if TargetSymbol ref is used in the queries here, exceptions may be thrown since it comes from a different syntax tree
                syntaxVisualizer.BuildMessageList(compiled.SelectMany(x => x.tree, (comp, syn) => comp.model.GetDeclaredSymbol(syn)).Where(x => ((INamedTypeSymbol)x).Interfaces.Any(i => i.Name == interfaceType.Name)));
            }
        }
 
        private void SyntaxVisualizerToolWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void RefreshClick(object sender, RoutedEventArgs e)
        {
            PopulateInterfaces();
            RefreshSyntaxVisualizer();
        }

        private void PopulateInterfaces()
        {
            commandInterfaceSelect.Items.Clear();
            // Get the Workspace corresponding to the currently active text snapshot.
            var workspace = WorkspaceDiscoveryService.PrimaryWorkspace;

            if (workspace != null)
            {
                var compiled = (from p in workspace.CurrentSolution.Projects
                                from d in p.Documents
                                let model = d.GetSemanticModel()
                                let tree = d.GetSyntaxTree().Root.DescendentNodes().OfType<InterfaceDeclarationSyntax>()
                                select new {model, tree}).ToList();

                if (!compiled.Any())
                {
                    return;
                }

                var interfaces = compiled.SelectMany(x => x.tree,
                                                     (comp, ifx) => comp.model.GetDeclaredSymbol(ifx)).Cast<INamedTypeSymbol>();
                commandInterfaceSelect.DataContext = interfaces;

            }
        }

        #region Implementation of IVsBuildStatusCallback

        public int BuildBegin(ref int pfContinue)
        {
            return VSConstants.S_OK;
        }

        public int BuildEnd(int fSuccess)
        {
            // TODO: figure out what code means build succeeded 

            RefreshSyntaxVisualizer();
            return VSConstants.S_OK;
        }

        public int Tick(ref int pfContinue)
        {
            return VSConstants.S_OK;
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            Clear();
        }

        #endregion

        private void commandInterfaceSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            syntaxVisualizer.TargetSymbol = (INamedTypeSymbol)commandInterfaceSelect.SelectedItem;
            RefreshSyntaxVisualizer();
        }
    }
}
