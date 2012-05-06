' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Xml.Linq
Imports Microsoft.VisualBasic
Imports Roslyn.Compilers.Common
Imports <xmlns="http://schemas.microsoft.com/vs/2009/dgml">

Public Class SyntaxDgmlOptions
    Public Property ShowTrivia = True
    Public Property ShowSpan = True
    Public Property ShowErrors = True
    Public Property ShowText = False
    Public Property ShowGroups = False
End Class

Public Module SyntaxDgmlHelper
    Private ReadOnly DefaultOptions As New SyntaxDgmlOptions
    Private Const MAX_LABEL_LENGTH = 30

    'Helpers that return the DGML representation of a SyntaxNode / SyntaxToken / SyntaxTrivia.
    'DGML is an XML-based format for directed graphs that can be rendered by Visual Studio.

#Region "ToDgml"
    <Extension()>
    Public Function ToDgml(nodeOrToken As CommonSyntaxNodeOrToken, language As String,
                           Optional syntaxTree As CommonSyntaxTree = Nothing,
                           Optional options As SyntaxDgmlOptions = Nothing) As XElement
        Dim dgml As XElement = Nothing

        If nodeOrToken.IsNode Then
            dgml = ToDgml(nodeOrToken.AsNode, language, syntaxTree, options)
        Else
            dgml = ToDgml(nodeOrToken.AsToken, language, syntaxTree, options)
        End If

        Return dgml
    End Function

    <Extension()>
    Public Function ToDgml(node As CommonSyntaxNode, language As String,
                           Optional syntaxTree As CommonSyntaxTree = Nothing,
                           Optional options As SyntaxDgmlOptions = Nothing) As XElement
        If options Is Nothing Then
            options = DefaultOptions
        End If

        Dim dgml = GetDgmlTemplate(options)
        ProcessNode(options, node, language, dgml, syntaxTree)
        Return dgml
    End Function

    <Extension()>
    Public Function ToDgml(token As CommonSyntaxToken, language As String,
                           Optional syntaxTree As CommonSyntaxTree = Nothing,
                           Optional options As SyntaxDgmlOptions = Nothing) As XElement
        If options Is Nothing Then
            options = DefaultOptions
        End If

        Dim dgml = GetDgmlTemplate(options)
        ProcessToken(options, token, language, dgml, syntaxTree)
        Return dgml
    End Function

    <Extension()>
    Public Function ToDgml(trivia As CommonSyntaxTrivia, language As String,
                           Optional syntaxTree As CommonSyntaxTree = Nothing,
                           Optional options As SyntaxDgmlOptions = Nothing) As XElement
        If options Is Nothing Then
            options = DefaultOptions
        End If

        Dim dgml = GetDgmlTemplate(options)
        ProcessTrivia(options, trivia, language, dgml, syntaxTree)
        Return dgml
    End Function
#End Region

#Region "Process*"
    Private Sub ProcessNodeOrToken(options As SyntaxDgmlOptions, nodeOrToken As CommonSyntaxNodeOrToken,
                                   language As String, dgml As XElement,
                                   Optional syntaxTree As CommonSyntaxTree = Nothing,
                                   Optional ByRef count As Integer = 0,
                                   Optional parent As XElement = Nothing,
                                   Optional parentGroup As XElement = Nothing,
                                   Optional properties As HashSet(Of String) = Nothing)
        If nodeOrToken.IsNode Then
            ProcessNode(options, nodeOrToken.AsNode, language, dgml, syntaxTree, count, parent, parentGroup, properties)
        Else
            ProcessToken(options, nodeOrToken.AsToken, language, dgml, syntaxTree, count, parent, parentGroup, properties)
        End If
    End Sub

    Private Sub ProcessNode(options As SyntaxDgmlOptions, node As CommonSyntaxNode,
                            language As String, dgml As XElement,
                            Optional syntaxTree As CommonSyntaxTree = Nothing,
                            Optional ByRef count As Integer = 0,
                            Optional parent As XElement = Nothing,
                            Optional parentGroup As XElement = Nothing,
                            Optional properties As HashSet(Of String) = Nothing)
        count += 1

        Dim current = <Node Id=<%= count %> Label=<%= GetLabelForNode(node, language) %>/>
        Dim currentID = count, parentID = -1, currentGroupID = -1, parentGroupID = -1
        Initialize(options, dgml, parent, parentGroup, current, properties, currentID, parentID, currentGroupID, parentGroupID)
        AddNodeInfo(options, node, language, current, dgml, properties, syntaxTree)
        Dim currentGroup As XElement = parentGroup

        current.@Category = "0"

        If options.ShowGroups Then
            count += 1
            currentGroup = <Node Group="Expanded" Id=<%= count %> Label=<%= GetLabelForNode(node, language) %>/>
            AddNodeInfo(options, node, language, currentGroup, dgml, properties, syntaxTree)
            dgml.<Nodes>.First.Add(currentGroup)
            currentGroupID = count
            dgml.<Links>.First.Add(<Link Source=<%= currentGroupID %> Target=<%= currentID %> Category="7"></Link>)
            If parentGroupID <> -1 Then
                dgml.<Links>.First.Add(<Link Source=<%= parentGroupID %> Target=<%= currentGroupID %> Category="7"></Link>)
            End If
        End If

        Dim kind = node.GetKind(language)

        If (node.IsMissing OrElse node.Span.Length = 0) AndAlso Not kind = "CompilationUnit" Then
            current.@Category = "4"
        End If

        If kind.Contains("Bad") OrElse kind.Contains("Skipped") Then
            current.@Category = "5"
        End If

        If options.ShowErrors AndAlso node.HasDiagnostics Then
            AddErrorIcon(current)
        End If

        For Each childSyntaxNode In node.ChildNodesAndTokens()
            ProcessNodeOrToken(options, childSyntaxNode, language, dgml, syntaxTree, count, current, currentGroup, properties)
        Next
    End Sub

    Private Sub ProcessToken(options As SyntaxDgmlOptions, token As CommonSyntaxToken,
                             language As String, dgml As XElement,
                             Optional syntaxTree As CommonSyntaxTree = Nothing,
                             Optional ByRef count As Integer = 0,
                             Optional parent As XElement = Nothing,
                             Optional parentGroup As XElement = Nothing,
                             Optional properties As HashSet(Of String) = Nothing)
        count += 1

        Dim current = <Node Id=<%= count %> Label=<%= GetLabelForToken(token, language) %>/>
        Initialize(options, dgml, parent, parentGroup, current, properties, count, 0, 0, 0)
        AddTokenInfo(options, token, language, current, dgml, properties, syntaxTree)
        Dim currentGroup As XElement = parentGroup

        current.@Category = "1"

        Dim kind = token.GetKind(language)

        If (token.IsMissing OrElse token.Span.Length = 0) AndAlso Not kind = "EndOfFileToken" Then
            current.@Category = "4"
        End If

        If kind.Contains("Bad") OrElse kind.Contains("Skipped") Then
            current.@Category = "5"
        End If

        If options.ShowErrors AndAlso token.HasDiagnostics Then
            AddErrorIcon(current)
        End If

        If options.ShowTrivia Then
            For Each triviaNode In token.LeadingTrivia
                ProcessTrivia(options, triviaNode, language, dgml, syntaxTree, count, True, current, currentGroup, properties)
            Next
            For Each triviaNode In token.TrailingTrivia
                ProcessTrivia(options, triviaNode, language, dgml, syntaxTree, count, False, current, currentGroup, properties)
            Next
        End If
    End Sub

    Private Sub ProcessTrivia(options As SyntaxDgmlOptions, trivia As CommonSyntaxTrivia,
                              language As String, dgml As XElement,
                              Optional syntaxTree As CommonSyntaxTree = Nothing,
                              Optional ByRef count As Integer = 0,
                              Optional isProcessingLeadingTrivia As Boolean = False,
                              Optional parent As XElement = Nothing,
                              Optional parentGroup As XElement = Nothing,
                              Optional properties As HashSet(Of String) = Nothing)
        count += 1

        Dim current = <Node Id=<%= count %> Label=<%= GetLabelForTrivia(trivia, language) %>/>
        Initialize(options, dgml, parent, parentGroup, current, properties, count, 0, 0, 0)
        AddTriviaInfo(options, trivia, language, current, dgml, properties, syntaxTree)
        Dim currentGroup As XElement = parentGroup

        If isProcessingLeadingTrivia Then
            current.@Category = "2"
        Else
            current.@Category = "3"
        End If

        Dim kind = trivia.GetKind(language)

        If kind.Contains("Bad") OrElse kind.Contains("Skipped") Then
            current.@Category = "5"
        End If

        If options.ShowErrors AndAlso trivia.HasDiagnostics Then
            AddErrorIcon(current)
        End If

        If options.ShowTrivia Then
            If trivia.HasStructure Then
                ProcessNode(options, trivia.GetStructure, language, dgml, syntaxTree, count, current, currentGroup, properties)
            End If
        End If
    End Sub
#End Region

#Region "GetLabel*"
    Private Function GetLabelForNode(node As CommonSyntaxNode, language As String) As String
        Return node.GetKind(language).ToString()
    End Function

    Private Function GetLabelForToken(token As CommonSyntaxToken, language As String) As String
        Dim label = token.GetKind(language).ToString()
        Dim text = token.GetText()

        If text.Trim <> String.Empty Then
            If text.Length <= MAX_LABEL_LENGTH Then
                label = text
            Else
                label = text.Remove(MAX_LABEL_LENGTH) & "..."
            End If
        End If

        Return label
    End Function

    Private Function GetLabelForTrivia(trivia As CommonSyntaxTrivia, language As String) As String
        Return trivia.GetKind(language).ToString()
    End Function
#End Region

#Region "Add*"
    Private Sub AddNodeInfo(options As SyntaxDgmlOptions, node As CommonSyntaxNode,
                            language As String, current As XElement, dgml As XElement,
                            properties As HashSet(Of String),
                            Optional syntaxTree As CommonSyntaxTree = Nothing)
        Dim nodeInfo = GetObjectInfo(node)
        AddDgmlProperty("Type", properties, dgml)
        current.@Type = nodeInfo.TypeName
        AddDgmlProperty("Kind", properties, dgml)
        current.@Kind = node.GetKind(language)

        If options.ShowSpan Then
            AddDgmlProperty("Span", properties, dgml)
            current.@Span = String.Format("{0} Length: {1}",
                                       node.Span.ToString,
                                       node.Span.Length)
            AddDgmlProperty("FullSpan", properties, dgml)
            current.@FullSpan = String.Format("{0} Length: {1}",
                                           node.FullSpan.ToString,
                                           node.FullSpan.Length)
        End If

        For Each field In nodeInfo.PropertyInfos
            Dim name = field.Name
            If Not (name.Contains("Span") OrElse name.Contains("Kind") OrElse name.Contains("Text")) Then
                AddDgmlProperty(name, properties, dgml)
                current.Add(New XAttribute(name, field.Value.ToString))
            End If
        Next

        If syntaxTree IsNot Nothing AndAlso options.ShowErrors Then
            AddDgmlProperty("Errors", properties, dgml)
            current.@Errors = String.Format("Count: {0}", syntaxTree.GetDiagnostics(node).Count)
            For Each syntaxError In syntaxTree.GetDiagnostics(node)
                current.@Errors &= vbCrLf & syntaxError.ToString(Nothing)
            Next
        End If

        If options.ShowText Then
            AddDgmlProperty("Text", properties, dgml)
            current.@Text = node.GetText()
            AddDgmlProperty("FullText", properties, dgml)
            current.@FullText = node.GetFullText()
        End If
    End Sub

    Private Sub AddTokenInfo(options As SyntaxDgmlOptions, token As CommonSyntaxToken,
                             language As String, current As XElement, dgml As XElement,
                             properties As HashSet(Of String),
                             Optional syntaxTree As CommonSyntaxTree = Nothing)
        Dim tokenInfo = GetObjectInfo(token)
        AddDgmlProperty("Type", properties, dgml)
        current.@Type = tokenInfo.TypeName
        AddDgmlProperty("Kind", properties, dgml)
        current.@Kind = token.GetKind(language)

        If options.ShowSpan Then
            AddDgmlProperty("Span", properties, dgml)
            current.@Span = String.Format("{0} Length: {1}",
                                       token.Span.ToString,
                                       token.Span.Length)
            AddDgmlProperty("FullSpan", properties, dgml)
            current.@FullSpan = String.Format("{0} Length: {1}",
                                           token.FullSpan.ToString,
                                           token.FullSpan.Length)
        End If

        For Each field In tokenInfo.PropertyInfos
            Dim name = field.Name
            If Not (name.Contains("Span") OrElse name.Contains("Kind") OrElse name.Contains("Text")) Then
                AddDgmlProperty(name, properties, dgml)
                current.Add(New XAttribute(name, field.Value.ToString))
            End If
        Next

        If syntaxTree IsNot Nothing AndAlso options.ShowErrors Then
            AddDgmlProperty("Errors", properties, dgml)
            current.@Errors = String.Format("Count: {0}", syntaxTree.GetDiagnostics(token).Count)
            For Each syntaxError In syntaxTree.GetDiagnostics(token)
                current.@Errors &= vbCrLf & syntaxError.ToString(Nothing)
            Next
        End If

        If options.ShowText Then
            AddDgmlProperty("Text", properties, dgml)
            current.@Text = token.GetText()
            AddDgmlProperty("FullText", properties, dgml)
            current.@FullText = token.GetFullText()
        End If
    End Sub

    Private Sub AddTriviaInfo(options As SyntaxDgmlOptions, trivia As CommonSyntaxTrivia,
                              language As String, current As XElement, dgml As XElement,
                              properties As HashSet(Of String),
                              Optional syntaxTree As CommonSyntaxTree = Nothing)
        Dim triviaInfo = GetObjectInfo(trivia)
        AddDgmlProperty("Type", properties, dgml)
        current.@Type = triviaInfo.TypeName
        AddDgmlProperty("Kind", properties, dgml)
        current.@Kind = trivia.GetKind(language)

        If options.ShowSpan Then
            AddDgmlProperty("Span", properties, dgml)
            current.@Span = String.Format("{0} Length: {1}",
                                       trivia.Span.ToString,
                                       trivia.Span.Length)
            AddDgmlProperty("FullSpan", properties, dgml)
            current.@FullSpan = String.Format("{0} Length: {1}",
                                           trivia.FullSpan.ToString,
                                           trivia.FullSpan.Length)
        End If

        For Each field In triviaInfo.PropertyInfos
            Dim name = field.Name
            If Not (name.Contains("Span") OrElse name.Contains("Kind") OrElse name.Contains("Text")) Then
                AddDgmlProperty(name, properties, dgml)
                current.Add(New XAttribute(name, field.Value.ToString))
            End If
        Next

        If syntaxTree IsNot Nothing AndAlso options.ShowErrors Then
            AddDgmlProperty("Errors", properties, dgml)
            current.@Errors = String.Format("Count: {0}", syntaxTree.GetDiagnostics(trivia).Count)
            For Each syntaxError In syntaxTree.GetDiagnostics(trivia)
                current.@Errors &= vbCrLf & syntaxError.ToString(Nothing)
            Next
        End If

        If options.ShowText Then
            AddDgmlProperty("Text", properties, dgml)
            current.@Text = trivia.GetText()
            AddDgmlProperty("FullText", properties, dgml)
            current.@FullText = trivia.GetFullText()
        End If
    End Sub
#End Region

#Region "Other Helpers"
    Private Sub Initialize(options As SyntaxDgmlOptions,
                           dgml As XElement,
                           parent As XElement,
                           parentGroup As XElement,
                           current As XElement,
                           ByRef properties As HashSet(Of String),
                           currentID As Integer,
                           ByRef parentID As Integer,
                           ByRef currentGroupID As Integer,
                           ByRef parentGroupID As Integer)
        dgml.<Nodes>.First.Add(current)

        parentGroupID = -1 : currentGroupID = -1
        parentID = -1

        If parent IsNot Nothing Then
            parentID = parent.@Id
        End If

        If options.ShowGroups Then
            If parentGroup IsNot Nothing Then
                parentGroupID = parentGroup.@Id
            End If
            currentGroupID = parentGroupID
        End If

        If parentID <> -1 Then
            dgml.<Links>.First.Add(<Link Source=<%= parentID %> Target=<%= currentID %>></Link>)
        End If

        If options.ShowGroups AndAlso parentGroupID <> -1 Then
            dgml.<Links>.First.Add(<Link Source=<%= parentGroupID %> Target=<%= currentID %> Category="7"></Link>)
        End If

        If properties Is Nothing Then
            properties = New HashSet(Of String)
        End If
    End Sub

    Private Function GetDgmlTemplate(options As SyntaxDgmlOptions) As XElement
        Dim dgml = <DirectedGraph Background="LightGray">
                       <Categories>
                           <Category Id="0" Label="SyntaxNode"/>
                           <Category Id="1" Label="SyntaxToken"/>
                           <Category Id="2" Label="Leading SyntaxTrivia"/>
                           <Category Id="3" Label="Trailing SyntaxTrivia"/>
                           <Category Id="4" Label="Missing / Zero-Width"/>
                           <Category Id="5" Label="Bad / Skipped"/>
                           <Category Id="6" Label="Has Diagnostics"/>
                       </Categories>
                       <Nodes>
                       </Nodes>
                       <Links>
                       </Links>
                       <Properties>
                       </Properties>
                       <Styles>
                           <Style TargetType="Node" GroupLabel="SyntaxNode" ValueLabel="Has category">
                               <Condition Expression="HasCategory('0')"/>
                               <Setter Property="Background" Value="Blue"/>
                               <Setter Property="NodeRadius" Value="5"/>
                           </Style>
                           <Style TargetType="Node" GroupLabel="SyntaxToken" ValueLabel="Has category">
                               <Condition Expression="HasCategory('1')"/>
                               <Setter Property="Background" Value="DarkGreen"/>
                               <Setter Property="FontStyle" Value="Italic"/>
                               <Setter Property="NodeRadius" Value="5"/>
                           </Style>
                           <%= If(options.ShowTrivia,
                               <Style TargetType="Node" GroupLabel="Leading SyntaxTrivia" ValueLabel="Has category">
                                   <Condition Expression="HasCategory('2')"/>
                                   <Setter Property="Background" Value="White"/>
                                   <Setter Property="NodeRadius" Value="5"/>
                               </Style>, Nothing) %>
                           <%= If(options.ShowTrivia,
                               <Style TargetType="Node" GroupLabel="Trailing SyntaxTrivia" ValueLabel="Has category">
                                   <Condition Expression="HasCategory('3')"/>
                                   <Setter Property="Background" Value="DimGray"/>
                                   <Setter Property="NodeRadius" Value="5"/>
                               </Style>, Nothing) %>
                           <Style TargetType="Node" GroupLabel="Missing / Zero-Width" ValueLabel="Has category">
                               <Condition Expression="HasCategory('4')"/>
                               <Setter Property="Background" Value="Black"/>
                               <Setter Property="NodeRadius" Value="5"/>
                           </Style>
                           <Style TargetType="Node" GroupLabel="Bad / Skipped" ValueLabel="Has category">
                               <Condition Expression="HasCategory('5')"/>
                               <Setter Property="Background" Value="Red"/>
                               <Setter Property="FontStyle" Value="Bold"/>
                               <Setter Property="NodeRadius" Value="5"/>
                           </Style>
                           <Style TargetType="Node" GroupLabel="Has Diagnostics" ValueLabel="Has category">
                               <Condition Expression="HasCategory('6')"/>
                               <Setter Property="Icon" Value="CodeSchema_Event"/>
                           </Style>
                       </Styles>
                   </DirectedGraph>

        dgml.AddAnnotation(SaveOptions.OmitDuplicateNamespaces)

        If options.ShowGroups Then
            dgml.<Categories>.First.Add(<Category Id="7" Label="Contains" CanBeDataDriven="False" CanLinkedNodesBeDataDriven="True" IncomingActionLabel="Contained By" IsContainment="True" OutgoingActionLabel="Contains"/>)
        End If
        Return dgml
    End Function

    Private Sub AddDgmlProperty(propertyName As String, properties As HashSet(Of String), dgml As XElement)
        If Not properties.Contains(propertyName) Then
            dgml.<Properties>.First.Add(<Property Id=<%= propertyName %> Label=<%= propertyName %> DataType="System.String"/>)
            properties.Add(propertyName)
        End If
    End Sub

    Private Sub AddErrorIcon(element As XElement)
        element.@Icon = "CodeSchema_Event"
    End Sub
#End Region
End Module

