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

Imports System.Runtime.CompilerServices
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common

Friend Module SyntaxKindHelper
    'Helpers that return the language-sepcific (C# / VB) SyntaxKind of a language-agnostic
    'CommonSyntaxNode / CommonSyntaxToken / CommonSyntaxTrivia.

    <Extension()>
    Friend Function GetKind(nodeOrToken As CommonSyntaxNodeOrToken, language As String) As String
        Dim kind = String.Empty

        If nodeOrToken.IsNode Then
            kind = nodeOrToken.AsNode().GetKind(language)
        Else
            kind = nodeOrToken.AsToken().GetKind(language)
        End If

        Return kind
    End Function

    <Extension()>
    Friend Function GetKind(node As CommonSyntaxNode, language As String) As String
        Dim kind = String.Empty

        If language = LanguageNames.CSharp Then
            kind = CType(node.Kind, CSharp.SyntaxKind).ToString()
        Else
            kind = CType(node.Kind, VisualBasic.SyntaxKind).ToString()
        End If

        Return kind
    End Function

    <Extension()>
    Friend Function GetKind(token As CommonSyntaxToken, language As String) As String
        Dim kind = String.Empty

        If language = LanguageNames.CSharp Then
            kind = CType(token.Kind, CSharp.SyntaxKind).ToString()
        Else
            kind = CType(token.Kind, VisualBasic.SyntaxKind).ToString()
        End If

        Return kind
    End Function

    <Extension()>
    Friend Function GetKind(trivia As CommonSyntaxTrivia, language As String) As String
        Dim kind = String.Empty

        If language = LanguageNames.CSharp Then
            kind = CType(trivia.Kind, CSharp.SyntaxKind).ToString()
        Else
            kind = CType(trivia.Kind, VisualBasic.SyntaxKind).ToString()
        End If

        Return kind
    End Function
End Module
