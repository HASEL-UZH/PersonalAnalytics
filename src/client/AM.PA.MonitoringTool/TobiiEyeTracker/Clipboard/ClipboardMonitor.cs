using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Forms;
using EyeCatcher.Native;
using EyeCatcherDatabase.Records;
using EyeCatcherLib;

namespace EyeCatcher.Clipboard
{
    // Must inherit Control, not Component, in order to have Handle
    [DefaultEvent("ClipboardChanged")]
    public sealed class ClipboardMonitor : Control, IObservable<CopyPasteRecord>
    {
        private readonly IWindowRecordProvider _windowRecordProvider;
        private readonly Subject<CopyPasteRecord> _subject = new Subject<CopyPasteRecord>();
        private IntPtr _nextClipboardViewer;

        public ClipboardMonitor(IWindowRecordProvider windowRecordProvider)
        {
            _windowRecordProvider = windowRecordProvider ?? throw new ArgumentNullException(nameof(windowRecordProvider));
            BackColor = Color.Black;
            Visible = false;

            _nextClipboardViewer = (IntPtr)NativeMethods.SetClipboardViewer(Handle);
        }

        protected override void WndProc(ref Message m)
        {
            // defined in winuser.h
            const int WM_DRAWCLIPBOARD = 0x308;
            // Sent to the first window in the clipboard viewer chain when a window is being removed from the chain.
            const int WM_CHANGECBCHAIN = 0x030D;

            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    if (System.Windows.Forms.Clipboard.ContainsData(DataFormats.Html))
                    {
                        OnClipboardChanged("HTML", GetUrl());
                    }
                    else if (System.Windows.Forms.Clipboard.ContainsText())
                    {
                        OnClipboardChanged("TEXT");
                    }
                    else if (System.Windows.Forms.Clipboard.ContainsImage())
                    {
                        OnClipboardChanged("IMAGE");
                    }
                    else if (System.Windows.Forms.Clipboard.ContainsAudio())
                    {
                        OnClipboardChanged("AUDIO");
                    }
                    else if (System.Windows.Forms.Clipboard.ContainsFileDropList())
                    {
                        OnClipboardChanged("FILEDROPLIST");
                    }
                    NativeMethods.SendMessage(_nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (m.WParam == _nextClipboardViewer)
                    {
                        _nextClipboardViewer = m.LParam;
                    }
                    else if (_nextClipboardViewer != IntPtr.Zero)
                    {
                        NativeMethods.SendMessage(_nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    }
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private static string GetUrl()
        {
            using (var sr = new StringReader(System.Windows.Forms.Clipboard.GetText(TextDataFormat.Html)))
            {
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    if (s.StartsWith("SourceURL:"))
                    {
                        return s.Substring(10);
                    }
                }
            }
            return null;
        }


        private void OnClipboardChanged(string content, string url = null)
        {
            Task.Run(() =>
            {
                _subject.OnNext(new CopyPasteRecord
                {
                    Url = url,
                    CopyContent = content,
                    Window = _windowRecordProvider.GetWindowRecord(NativeMethods.GetForegroundWindow())
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            NativeMethods.ChangeClipboardChain(Handle, _nextClipboardViewer);
        }

        public IDisposable Subscribe(IObserver<CopyPasteRecord> observer)
        {
            return _subject.Subscribe(observer);
        }
    }
}
