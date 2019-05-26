﻿using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace JavaScriptPrettier
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("Text")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal sealed class CommandRegistration : IVsTextViewCreationListener
    {
        [Import]
        private IVsEditorAdaptersFactoryService AdaptersFactory { get; set; }

        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        [Import]
        private ITextBufferUndoManagerProvider UndoProvider { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);

            if (!TryGetPrettierTextDocument(view, out ITextDocument doc))
                return;

            ITextBufferUndoManager undoManager = UndoProvider.GetTextBufferUndoManager(view.TextBuffer);
            NodeProcess node = view.Properties.GetOrCreateSingletonProperty(() => new NodeProcess());

            var cmd = new PrettierCommand(view, undoManager, node, doc.Encoding, doc.FilePath);
            view.Properties.AddProperty("prettierCommand", cmd);

            AddCommandFilter(textViewAdapter, cmd);

            if (!node.IsReadyToExecute())
            {
                node.EnsurePackageInstalledAsync().ConfigureAwait(false);
            }
        }
        private void AddCommandFilter(IVsTextView textViewAdapter, BaseCommand command)
        {
            textViewAdapter.AddCommandFilter(command, out IOleCommandTarget next);
            command.Next = next;
        }

        private bool TryGetPrettierTextDocument(IWpfTextView view, out ITextDocument document)
        {
            document = null;

            if (DocumentService.TryGetTextDocument(view.TextDataModel.DocumentBuffer, out ITextDocument doc))
            {
                var extension = Path.GetExtension(doc.FilePath);
                if (extension == ".vue"
                    || extension == ".js"
                    || extension == ".ts"
                    || extension == ".tsx")
                {
                    document = doc;
                    return true;
                }
            }

            return false;
        }
    }
}
