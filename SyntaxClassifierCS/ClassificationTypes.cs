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

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace SyntaxClassifierCS
{
    internal static class ClassificationTypes
    {
        public const string MessageSendClassificationTypeName = "CSharpMessagingClassificationTypeName";
        
        [Export] 
        [Name(MessageSendClassificationTypeName)] 
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)] 
        internal static ClassificationTypeDefinition MessagingClassificationTypeDefinition = null;


        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = MessageSendClassificationTypeName)]
        [Name("MessagingClassificationFormatDefinition")]
        [Order(After = Priority.Default, Before = Priority.High)]
        [UserVisible(true)]
        private class MessagingClassificationFormatDefinition : ClassificationFormatDefinition
        {
            private MessagingClassificationFormatDefinition()
            {
                this.DisplayName = "Command/event publisher";
                this.BackgroundColor = Colors.DodgerBlue;
            }
        }
    }
}
