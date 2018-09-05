using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using EyeCatcherDatabase.Enums;
using EyeCatcherDatabase.Records;

namespace EyeCatcherLib
{
    /// <inheritdoc cref="IWindowRecordCollection" />
    /// <summary>
    /// Decorator for WindowRecordCollection that allows it to be Observable 
    /// </summary>
    public class ObservableWindowRecordCollection : IWindowRecordCollection, IObservable<DesktopRecord>
    {
        private readonly IWindowRecordCollection _windowRecordCollection;

        public ObservableWindowRecordCollection(IWindowRecordCollection windowRecordCollection)
        {
            _windowRecordCollection = windowRecordCollection;
        }

        private readonly Subject<DesktopRecord> _subject = new Subject<DesktopRecord>();
        private DesktopRecord _desktopRecord;

        public IDisposable Subscribe(IObserver<DesktopRecord> observer)
        {
            return _subject.Subscribe(observer);
        }

        #region IWindowRecordCollection

        public IList<WindowRecord> Initialize()
        {
            return _windowRecordCollection.Initialize();
        }

        public DesktopRecord GetDesktopRecord()
        {
            return _desktopRecord;
        }

        public WindowRecord GetWindowRecord(IntPtr hWnd)
        {
            return _windowRecordCollection.GetWindowRecord(hWnd);
        }


        public WindowRecord Activate(IntPtr hwnd)
        {
            return UpdateAndPublishDesktop(_windowRecordCollection.Activate(hwnd), DesktopTransitionType.Activate);
        }

        public WindowRecord Destroy(IntPtr hwnd)
        {
            return UpdateAndPublishDesktop(_windowRecordCollection.Destroy(hwnd), DesktopTransitionType.Destory);
        }

        public WindowRecord MoveOrResize(IntPtr hwnd)
        {
            return UpdateAndPublishDesktop(_windowRecordCollection.MoveOrResize(hwnd), DesktopTransitionType.MoveResize);
        }

        public WindowRecord Minimize(IntPtr hwnd)
        {
            return UpdateAndPublishDesktop(_windowRecordCollection.Minimize(hwnd), DesktopTransitionType.Minimize);
        }

        public WindowRecord MinimizeEnd(IntPtr hwnd)
        {
            return UpdateAndPublishDesktop(_windowRecordCollection.MinimizeEnd(hwnd), DesktopTransitionType.MinimizeEnd);
        }

        public WindowRecord Rename(IntPtr hwnd)
        {
            return UpdateAndPublishDesktop(_windowRecordCollection.Rename(hwnd), DesktopTransitionType.Rename);
        }

        #endregion

        private WindowRecord UpdateAndPublishDesktop(WindowRecord window, DesktopTransitionType transition)
        {
            if (window == null)
            {
                return null;
            }

            _desktopRecord = _windowRecordCollection.GetDesktopRecord();
            _desktopRecord.Transition = new DesktopTransitionRecord
            {
                Window = window,
                TransitionType = transition,
            };
            _subject.OnNext(_desktopRecord);
            return window;
        }


    }
}
