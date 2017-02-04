using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReferencePathManager
{
    public static class ClipboardManager
    {
        [STAThread]
        public static string GetTextFromClipboard()
        {
            if (!Clipboard.ContainsText(TextDataFormat.Text)) return null;

            string clipboardText = Clipboard.GetText(TextDataFormat.Text);
            return clipboardText;
        }
    }
}
