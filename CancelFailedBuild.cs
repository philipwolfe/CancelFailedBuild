using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;

namespace CancelFailedBuild
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("BuildOutput")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    sealed class OutputWindowCreationListener : IWpfTextViewCreationListener
    {
        [Import]
        SVsServiceProvider GlobalServiceProvider = null;

        private static readonly Regex BuildError = new Regex(": error|: fatal error", RegexOptions.IgnoreCase);

        public void TextViewCreated(IWpfTextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE)GlobalServiceProvider.GetService(typeof(DTE));

            textView.TextBuffer.Changed += (sender, args) =>
            {
                //Output window is friendly and writes full lines at a time, so we only need to look at the changed text.
                foreach (var change in args.Changes)
                {
                    string text = args.After.GetText(change.NewSpan);
                    if (BuildError.IsMatch(text))
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        dte.ExecuteCommand("Build.Cancel");

                        textView.TextBuffer.Insert(textView.TextBuffer.CurrentSnapshot.Length, "Build cancelled by the cancel on first failure extension." + Environment.NewLine);
                    }
                }
            };

        }
    }
}
