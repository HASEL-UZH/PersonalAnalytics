using System;
using System.Windows.Controls;

namespace Shared
{
    public class FirstStartScreenContainer
    {

        private UserControl content;
        private string title;
        private Action nextCallback;
        private Action previousCallback;

        public FirstStartScreenContainer(UserControl content, string title, Action nextCallback = null, Action previousCallback = null)
        {
            this.content = content;
            this.title = title;
            this.nextCallback = nextCallback;
            this.previousCallback = previousCallback;
        }

        public string Title { get { return title;  } }

        public UserControl Content { get { return content; } }

        public Action NextCallback { get { return nextCallback; } }

        public Action PreviousCallback { get { return previousCallback; } }
    }

}