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
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Samples.SyntaxVisualizer.Control
{
    /// <summary>
    /// Interaction logic for CommandVisualizerControl.xaml
    /// </summary>
    public partial class CommandVisualizerControl
    {
        #region Private State

        private ListBoxItem currentSelection = null;
        private Brush currentSelectionForeground = null;

        #endregion

        #region Public Properties, Events

        public bool DirectedSyntaxGraphContextMenuEnabled { get; set; }
        
        public ISymbol TargetSymbol { get; private set; }
        public bool IsLazy { get; private set; }

        public delegate void SyntaxNodeDelegate(CommonSyntaxNode node);
        public event SyntaxVisualizerControl.SyntaxNodeDelegate SyntaxNodeDirectedGraphRequested;
        public event SyntaxVisualizerControl.SyntaxNodeDelegate SyntaxNodeNavigationToSourceRequested;

        public delegate void SyntaxTokenDelegate(CommonSyntaxToken token);
        public event SyntaxVisualizerControl.SyntaxTokenDelegate SyntaxTokenDirectedGraphRequested;
        public event SyntaxVisualizerControl.SyntaxTokenDelegate SyntaxTokenNavigationToSourceRequested;

        public delegate void SyntaxTriviaDelegate(CommonSyntaxTrivia trivia);
        public event SyntaxVisualizerControl.SyntaxTriviaDelegate SyntaxTriviaDirectedGraphRequested;
        public event SyntaxVisualizerControl.SyntaxTriviaDelegate SyntaxTriviaNavigationToSourceRequested;

        #endregion

        public CommandVisualizerControl()
        {
            InitializeComponent();
        }

        private void ListBoxSelectedItemChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            if (currentSelection != null && currentSelectionForeground != null)
            {
                currentSelection.Foreground = currentSelectionForeground;
            }

            if (listBox.SelectedItem != null)
            {
                currentSelection = (ListBoxItem)listBox.SelectedItem;
                currentSelectionForeground = currentSelection.Foreground;
                currentSelection.Foreground = Brushes.White;
            }
        }

        public void Clear()
        {
            listBox.Items.Clear();
            listBox.Items.Add(new ListBoxItem() {Content = "Placeholder"});
        }

        public void BuildMessageList(IEnumerable<ISymbol> messageTypes)
        {
            foreach (var messageType in messageTypes)
            {
                AddItem(messageType);
            }
        }
        
        private void AddItem(ISymbol symbol)
        {
            var item = new ListBoxItem()
                           {
                               Tag = symbol,
                               Visibility = Visibility.Visible,
                               IsEnabled = true,
                               Foreground = Brushes.Blue,
                               Background = Brushes.White,
                               Content = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                           };

            // TODO: hook up selection event handler to render/update associated events, handlers, etc

            listBox.Items.Add(item);
        }

        
    }
}
