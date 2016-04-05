// Created by André Meyer (t-anmeye@microsoft.com) working as an Intern at Microsoft Research
// Created: 2015-11-24
// 
// Licensed under the MIT License.

using Shared.Helpers;

namespace Shared
{
    /// <summary>
    /// Currently, only a daily and weekly retrospection is supported.
    /// a monthly retrospection will be added in the future
    /// </summary>
    public enum VisType
    {
        Day,
        Week
    }

    /// <summary>
    /// These are also the sized defined in the CSS
    /// </summary>
    public enum VisSize
    {
        Small,
        Square,
        Wide,
        //Large
    }

    public interface IVisualization
    {
        string Title { get; }
        bool IsEnabled { get; set; }
        VisSize Size { get; }
        VisType Type { get; }
        int Order { get; set; }
        string GetHtml();
    }

    public class BaseVisualization
    {
        public string Title { set; get; }
        public bool IsEnabled { get; set; }
        public VisSize Size { get; set; }
        public VisType Type { get; set; }
        public int Order { get; set; }
        public virtual string GetHtml()
        {
            return VisHelper.Error("Not yet implemented");
        }
    }
}
