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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Samples.SyntaxVisualizer.Control
{
    // A control for visually displaying the contents of a SyntaxTree.
    public partial class SyntaxVisualizerControl : UserControl
    {
        // Instances of this class are stored in the Tag field of each item in the treeview.
        private class SyntaxTag
        {
            internal TextSpan Span { get; set; }
            internal TextSpan FullSpan { get; set; }
            internal TreeViewItem ParentItem { get; set; }
            internal string Kind { get; set; }
            internal CommonSyntaxNode SyntaxNode { get; set; }
            internal CommonSyntaxToken SyntaxToken { get; set; }
            internal CommonSyntaxTrivia SyntaxTrivia { get; set; }
            internal bool IsSyntaxNode { get; set; }
            internal bool IsSyntaxToken { get; set; }
            internal bool IsSyntaxTrivia { get; set; }
        }

        #region Private State
        private TreeViewItem currentSelection = null;
        private Brush currentSelectionForeground = null;
        private bool isNavigatingFromSourceToTree = false;
        private bool isNavigatingFromTreeToSource = false;
        private System.Windows.Forms.PropertyGrid propertyGrid;
        private static readonly Thickness DefaultBorderThickness = new Thickness(1);
        #endregion

        #region Public Properties, Events
        public bool DirectedSyntaxGraphContextMenuEnabled { get; set; }
        public string SourceLanguage { get; private set; }
        public CommonSyntaxTree SyntaxTree { get; private set; }
        public bool IsLazy { get; private set; }

        public delegate void SyntaxNodeDelegate(CommonSyntaxNode node);
        public event SyntaxNodeDelegate SyntaxNodeDirectedGraphRequested;
        public event SyntaxNodeDelegate SyntaxNodeNavigationToSourceRequested;

        public delegate void SyntaxTokenDelegate(CommonSyntaxToken token);
        public event SyntaxTokenDelegate SyntaxTokenDirectedGraphRequested;
        public event SyntaxTokenDelegate SyntaxTokenNavigationToSourceRequested;

        public delegate void SyntaxTriviaDelegate(CommonSyntaxTrivia trivia);
        public event SyntaxTriviaDelegate SyntaxTriviaDirectedGraphRequested;
        public event SyntaxTriviaDelegate SyntaxTriviaNavigationToSourceRequested;
        #endregion

        #region Public Methods
        public SyntaxVisualizerControl()
        {
            InitializeComponent();

            propertyGrid = new System.Windows.Forms.PropertyGrid();
            propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            propertyGrid.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
            propertyGrid.HelpVisible = false;
            propertyGrid.ToolbarVisible = false;
            propertyGrid.CommandsVisibleIfAvailable = false;
            windowsFormsHost.Child = propertyGrid;
        }

        public void Clear()
        {
            treeView.Items.Clear();
            propertyGrid.SelectedObject = null;
            typeTextLabel.Visibility = Visibility.Hidden;
            kindTextLabel.Visibility = Visibility.Hidden;
            typeValueLabel.Content = string.Empty;
            kindValueLabel.Content = string.Empty;
            legendButton.Visibility = Visibility.Hidden;
        }

        // If lazy is true then treeview items are populated on-demand. In other words, when lazy is true
        // the children for any given item are only populated when the item is selected. If lazy is
        // false then the entire tree is populated at once (and this can result in bad performance when
        // displaying large trees).
        public void DisplaySyntaxTree(CommonSyntaxTree tree, string language, bool lazy = true)
        {
            if (tree != null && !string.IsNullOrEmpty(language))
            {
                SourceLanguage = language;
                IsLazy = lazy;
                SyntaxTree = tree;
                AddNode(null, SyntaxTree.Root);
                legendButton.Visibility = Visibility.Visible;
            }
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose position best matches the supplied position.
        public bool NavigateToBestMatch(int position, string kind = null, bool highlightMatch = false)
        {
            TreeViewItem match = null;

            if (treeView.HasItems && !isNavigatingFromTreeToSource)
            {
                isNavigatingFromSourceToTree = true;
                match = NavigateToBestMatch((TreeViewItem)treeView.Items[0], position, kind);
                isNavigatingFromSourceToTree = false;
            }

            var matchFound = match != null;

            if (highlightMatch && matchFound)
            {
                match.Background = Brushes.Yellow;
                match.BorderBrush = Brushes.Black;
                match.BorderThickness = DefaultBorderThickness;
                highlightLegendTextLabel.Visibility = Visibility.Visible;
                highlightLegendDescriptionLabel.Visibility = Visibility.Visible;
            }

            return matchFound;
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose span best matches the supplied span.
        public bool NavigateToBestMatch(int start, int length, string kind = null, bool highlightMatch = false)
        {
            return NavigateToBestMatch(new TextSpan(start, length), kind, highlightMatch);
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose span best matches the supplied span.
        public bool NavigateToBestMatch(TextSpan span, string kind = null, bool highlightMatch = false)
        {
            TreeViewItem match = null;

            if (treeView.HasItems && !isNavigatingFromTreeToSource)
            {
                isNavigatingFromSourceToTree = true;
                match = NavigateToBestMatch((TreeViewItem)treeView.Items[0], span, kind);
                isNavigatingFromSourceToTree = false;
            }

            var matchFound = match != null;

            if (highlightMatch && matchFound)
            {
                match.Background = Brushes.Yellow;
                match.BorderBrush = Brushes.Black;
                match.BorderThickness = DefaultBorderThickness;
                highlightLegendTextLabel.Visibility = Visibility.Visible;
                highlightLegendDescriptionLabel.Visibility = Visibility.Visible;
            }

            return matchFound;
        }
        #endregion

        #region Private Helpers - TreeView Navigation
        // Collapse all items in the treeview except for the supplied item. The supplied item
        // is also expanded, selected and scrolled into view.
        private void CollapseEverythingBut(TreeViewItem item)
        {
            if (item != null)
            {
                DeepCollapse((TreeViewItem)treeView.Items[0]);
                ExpandPathTo(item);
                item.IsSelected = true;
                item.BringIntoView();
            }
        }

        // Collapse the supplied treeview item including all its descendents.
        private void DeepCollapse(TreeViewItem item)
        {
            if (item != null)
            {
                item.IsExpanded = false;
                foreach (TreeViewItem child in item.Items)
                {
                    DeepCollapse(child);
                }
            }
        }

        // Ensure that the supplied treeview item and all its ancsestors are expanded.
        private void ExpandPathTo(TreeViewItem item)
        {
            if (item != null)
            {
                item.IsExpanded = true;
                ExpandPathTo(((SyntaxTag)item.Tag).ParentItem);
            }
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose position best matches the supplied position.
        private TreeViewItem NavigateToBestMatch(TreeViewItem current, int position, string kind = null)
        {
            TreeViewItem match = null;

            if (current != null)
            {
                SyntaxTag currentTag = (SyntaxTag)current.Tag;
                if (currentTag.FullSpan.Contains(position))
                {
                    CollapseEverythingBut(current);

                    foreach (TreeViewItem item in current.Items)
                    {
                        match = NavigateToBestMatch(item, position, kind);
                        if (match != null)
                        {
                            break;
                        }
                    }

                    if (match == null)
                    {
                        if (kind == null)
                        {
                            match = current;
                        }
                        else if (currentTag.Kind == kind)
                        {
                            match = current;
                        }
                    }
                }
            }

            return match;
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose span best matches the supplied span.
        private TreeViewItem NavigateToBestMatch(TreeViewItem current, TextSpan span, string kind = null)
        {
            TreeViewItem match = null;

            if (current != null)
            {
                SyntaxTag currentTag = (SyntaxTag)current.Tag;
                if (currentTag.FullSpan.Contains(span))
                {
                    if (currentTag.Span == span || currentTag.FullSpan == span)
                    {
                        if (kind == null)
                        {
                            CollapseEverythingBut(current);
                            match = current;
                        }
                        else if (currentTag.Kind == kind)
                        {
                            CollapseEverythingBut(current);
                            match = current;
                        }
                    }
                    else
                    {
                        CollapseEverythingBut(current);

                        foreach (TreeViewItem item in current.Items)
                        {
                            match = NavigateToBestMatch(item, span, kind);
                            if (match != null)
                            {
                                break;
                            }
                        }

                        if (match == null)
                        {
                            if (kind == null)
                            {
                                match = current;
                            }
                            else if (currentTag.Kind == kind)
                            {
                                match = current;
                            }
                        }
                    }
                }
            }

            return match;
        }
        #endregion

        #region Private Helpers - TreeView Population
        // Helpers for populating the treeview.

        private void AddNodeOrToken(TreeViewItem parentItem, CommonSyntaxNodeOrToken nodeOrToken)
        {
            if (nodeOrToken.IsNode)
            {
                AddNode(parentItem, nodeOrToken.AsNode());
            }
            else
            {
                AddToken(parentItem, nodeOrToken.AsToken());
            }
        }

        private void AddNode(TreeViewItem parentItem, CommonSyntaxNode node)
        {
            var kind = node.GetKind(SourceLanguage);
            var tag = new SyntaxTag()
            {
                SyntaxNode = node,
                IsSyntaxNode = true,
                IsSyntaxToken = false,
                IsSyntaxTrivia = false,
                Span = node.Span,
                FullSpan = node.FullSpan,
                Kind = kind,
                ParentItem = parentItem
            };

            var item = new TreeViewItem()
            {
                Tag = tag,
                IsExpanded = true,
                Foreground = Brushes.Blue,
                Background = node.HasDiagnostics ? Brushes.Pink : Brushes.White,
                Header = tag.Kind + " " + node.Span.ToString()
            };

            if (node.HasDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(node))
                {
                    item.ToolTip += diagnostic.ToString(null) + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                if (!e.Handled)
                {
                    isNavigatingFromTreeToSource = true;

                    typeTextLabel.Visibility = Visibility.Visible;
                    kindTextLabel.Visibility = Visibility.Visible;
                    typeValueLabel.Content = node.GetType().Name;
                    kindValueLabel.Content = kind;
                    propertyGrid.SelectedObject = node;

                    // If IsLazy is true then populate only child items.
                    if (IsLazy && item.Items.Count == 0 && node.HasChildren)
                    {
                        foreach (var child in node.ChildNodesAndTokens())
                        {
                            AddNodeOrToken(item, child);
                        }
                    }

                    item.IsExpanded = true;

                    if (!isNavigatingFromSourceToTree && SyntaxNodeNavigationToSourceRequested != null)
                    {
                        SyntaxNodeNavigationToSourceRequested(node);
                    }

                    isNavigatingFromTreeToSource = false;
                    e.Handled = true;
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
            }
            else
            {
                parentItem.Items.Add(item);
            }

            // If IsLazy is false then recursively populate all descendent items.
            if (!IsLazy && node.HasChildren)
            {
                foreach (var child in node.ChildNodesAndTokens())
                {
                    AddNodeOrToken(item, child);
                }
            }
        }

        private void AddToken(TreeViewItem parentItem, CommonSyntaxToken token)
        {
            var kind = token.GetKind(SourceLanguage);
            var tag = new SyntaxTag()
            {
                SyntaxToken = token,
                IsSyntaxNode = false,
                IsSyntaxToken = true,
                IsSyntaxTrivia = false,
                Span = token.Span,
                FullSpan = token.FullSpan,
                Kind = kind,
                ParentItem = parentItem
            };

            var item = new TreeViewItem()
            {
                Tag = tag,
                IsExpanded = true,
                Foreground = Brushes.DarkGreen,
                Background = token.HasDiagnostics ? Brushes.Pink : Brushes.White,
                Header = tag.Kind + " " + token.Span.ToString()
            };

            if (token.HasDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(token))
                {
                    item.ToolTip += diagnostic.ToString(null) + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                if (!e.Handled)
                {
                    isNavigatingFromTreeToSource = true;

                    typeTextLabel.Visibility = Visibility.Visible;
                    kindTextLabel.Visibility = Visibility.Visible;
                    typeValueLabel.Content = token.GetType().Name;
                    kindValueLabel.Content = kind;
                    propertyGrid.SelectedObject = token;

                    // If IsLazy is true then populate only child items.
                    if (IsLazy && item.Items.Count == 0)
                    {
                        if (token.HasLeadingTrivia)
                        {
                            foreach (var trivia in token.LeadingTrivia)
                            {
                                AddTrivia(item, trivia, true);
                            }
                        }

                        if (token.HasTrailingTrivia)
                        {
                            foreach (var trivia in token.TrailingTrivia)
                            {
                                AddTrivia(item, trivia, false);
                            }
                        }
                    }

                    item.IsExpanded = true;

                    if (!isNavigatingFromSourceToTree && SyntaxTokenNavigationToSourceRequested != null)
                    {
                        SyntaxTokenNavigationToSourceRequested(token);
                    }

                    isNavigatingFromTreeToSource = false;
                    e.Handled = true;
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
            }
            else
            {
                parentItem.Items.Add(item);
            }

            // If IsLazy is false then recursively populate all descendent items.
            if (!IsLazy)
            {
                if (token.HasLeadingTrivia)
                {
                    foreach (var trivia in token.LeadingTrivia)
                    {
                        AddTrivia(item, trivia, true);
                    }
                }

                if (token.HasTrailingTrivia)
                {
                    foreach (var trivia in token.TrailingTrivia)
                    {
                        AddTrivia(item, trivia, false);
                    }
                }
            }
        }

        private void AddTrivia(TreeViewItem parentItem, CommonSyntaxTrivia trivia, bool isLeadingTrivia)
        {
            var kind = trivia.GetKind(SourceLanguage);
            var tag = new SyntaxTag()
            {
                SyntaxTrivia = trivia,
                IsSyntaxNode = false,
                IsSyntaxToken = false,
                IsSyntaxTrivia = true,
                Span = trivia.Span,
                FullSpan = trivia.FullSpan,
                Kind = kind,
                ParentItem = parentItem
            };

            var item = new TreeViewItem()
            {
                Tag = tag,
                IsExpanded = true,
                Foreground = Brushes.Maroon,
                Background = trivia.HasDiagnostics ? Brushes.Pink : Brushes.White,
                Header = (isLeadingTrivia ? "Lead: " : "Trail: ") + tag.Kind + " " + trivia.Span.ToString()
            };

            if (trivia.HasDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(trivia))
                {
                    item.ToolTip += diagnostic.ToString(null) + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                if (!e.Handled)
                {
                    isNavigatingFromTreeToSource = true;

                    typeTextLabel.Visibility = Visibility.Visible;
                    kindTextLabel.Visibility = Visibility.Visible;
                    typeValueLabel.Content = trivia.GetType().Name;
                    kindValueLabel.Content = kind;
                    propertyGrid.SelectedObject = trivia;

                    // If IsLazy is true then populate only child items.
                    if (IsLazy && item.Items.Count == 0 && trivia.HasStructure)
                    {
                        AddNode(item, trivia.GetStructure());
                    }

                    item.IsExpanded = true;

                    if (!isNavigatingFromSourceToTree && SyntaxTriviaNavigationToSourceRequested != null)
                    {
                        SyntaxTriviaNavigationToSourceRequested(trivia);
                    }

                    isNavigatingFromTreeToSource = false;
                    e.Handled = true;
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
                typeTextLabel.Visibility = Visibility.Hidden;
                kindTextLabel.Visibility = Visibility.Hidden;
                typeValueLabel.Content = string.Empty;
                kindValueLabel.Content = string.Empty;
            }
            else
            {
                parentItem.Items.Add(item);
            }

            // If IsLazy is false then recursively populate all descendent items.
            if (!IsLazy && trivia.HasStructure)
            {
                AddNode(item, trivia.GetStructure());
            }
        }
        #endregion

        #region Private Helpers - Other
        private static TreeViewItem FindTreeViewItem(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            return (TreeViewItem)source;
        }
        #endregion

        #region Event Handlers
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (currentSelection != null && currentSelectionForeground != null)
            {
                currentSelection.Foreground = currentSelectionForeground;
            }

            if (treeView.SelectedItem != null)
            {
                currentSelection = (TreeViewItem)treeView.SelectedItem;
                currentSelectionForeground = currentSelection.Foreground;
                currentSelection.Foreground = Brushes.White;
            }
        }

        private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindTreeViewItem((DependencyObject)e.OriginalSource);

            if (item != null)
            {
                item.Focus();
            }
        }

        private void TreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!DirectedSyntaxGraphContextMenuEnabled)
            {
                e.Handled = true;
            }
        }

        private void DirectedSyntaxGraphMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelection != null)
            {
                var currentTag = (SyntaxTag)currentSelection.Tag;

                if (currentTag.IsSyntaxNode && SyntaxNodeDirectedGraphRequested != null)
                {
                    SyntaxNodeDirectedGraphRequested(currentTag.SyntaxNode);
                }
                else if (currentTag.IsSyntaxToken && SyntaxTokenDirectedGraphRequested != null)
                {
                    SyntaxTokenDirectedGraphRequested(currentTag.SyntaxToken);
                }
                else if (currentTag.IsSyntaxTrivia && SyntaxTriviaDirectedGraphRequested != null)
                {
                    SyntaxTriviaDirectedGraphRequested(currentTag.SyntaxTrivia);
                }
            }
        }

        private void LegendButton_Click(object sender, RoutedEventArgs e)
        {
            legendPopup.IsOpen = true;
        }
        #endregion
    }
}
