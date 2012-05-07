using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Roslyn.Samples.SyntaxVisualizer.Extension
{
    /// <summary>
    /// Interaction logic for MessagingVisualizerContainer.xaml
    /// </summary>
    public partial class MessagingVisualizerContainer : UserControl
    {
        //private readonly SyntaxVisualizerContainer syntaxVisualizerContainer;
        //private readonly CommandVisualizerContainer commandVisualizerContainer;

        private readonly SyntaxVisualizerToolWindow parent;

        public MessagingVisualizerContainer(SyntaxVisualizerToolWindow parentWindow)
        {
            parent = parentWindow;
            InitializeComponent();

            //syntaxVisualizerContainer = new SyntaxVisualizerContainer();
            //commandVisualizerContainer = new CommandVisualizerContainer();

            //var syntaxTab = new TabItem
            //                    {
            //                        Header = "Syntax Visualizer",
            //                        HorizontalAlignment = HorizontalAlignment.Stretch,
            //                        VerticalAlignment = VerticalAlignment.Top,
            //                        Content = syntaxVisualizerContainer
            //                    };

            //var commandTab = new TabItem
            //                     {
            //                         Header = "Command Visualizer", 
            //                         Content = commandVisualizerContainer,
            //                         HorizontalAlignment = HorizontalAlignment.Stretch,
            //                         VerticalAlignment = VerticalAlignment.Top,
            //                     };

            //((TabItem)visualizerTabs.Items.GetItemAt(0)).Content = syntaxTab;
            //((TabItem)visualizerTabs.Items.GetItemAt(1)).Content = commandTab;
        }

        internal TServiceInterface GetVsService<TServiceInterface, TService>() where TServiceInterface : class where TService : class
        {
            return parent.GetVsService<TServiceInterface, TService>();
        }
    }
}
