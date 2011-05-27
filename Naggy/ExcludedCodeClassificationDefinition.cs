using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Classification;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using System.Windows.Media;

namespace Naggy
{
    class Constants
    {
        public const string ClassificationName = "Preprocessor Excluded Code";
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(ClassificationName)]
        public static ClassificationTypeDefinition ExcludeCodeTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [Name(ClassificationName)]
        [DisplayName(ClassificationName)]
        [UserVisible(true)]
        [ClassificationType(ClassificationTypeNames = ClassificationName)]
        class ExcludedCodeMarkerDefinition : ClassificationFormatDefinition
        {
            public ExcludedCodeMarkerDefinition()
            {
                this.DisplayName = ClassificationName;
                this.ForegroundOpacity = 0.5;
            }
        }
    }
}
