// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-03
// 
// Licensed under the MIT License.
//
// Based on: http://stackoverflow.com/questions/18772035/is-it-possible-to-style-the-contents-of-a-dockpanel-to-fill-the-last-visible-c

using System;
using System.Windows;
using System.Windows.Controls;

namespace GoalSetting.Views
{
    public class DockPanelWithLastVisibleChildFill : DockPanel
    {
        public static readonly DependencyProperty LastVisibleChildFillProperty = DependencyProperty.Register(
            "LastVisibleChildFill",
            typeof(bool),
            typeof(DockPanelWithLastVisibleChildFill),
            new UIPropertyMetadata(true, (s, e) => {

                var dockPanelEx = (DockPanelWithLastVisibleChildFill)s;
                var newValue = (bool)e.NewValue;

                if (newValue)
                    dockPanelEx.LastChildFill = true; // Implicitly enable LastChildFill
                                                      // Note: For completeness, we may consider putting in code to set
                                                      // LastVisibileChildFill to false if LastChildFill is set to false

        }));

        /// <summary>
        /// Indicates that LastChildFill should fill the last visible child
        /// Note: When set to true, automatically also sets LastChildFill to true as well.
        /// </summary>
        public bool LastVisibleChildFill
        {
            get { return (bool)GetValue(LastVisibleChildFillProperty); }
            set { SetValue(LastVisibleChildFillProperty, value); }
        }

        protected override Size ArrangeOverride(Size totalAvailableSize)
        {
            UIElementCollection internalChildren = base.InternalChildren;
            int count = internalChildren.Count;

            int firstFilledIndex = count;

            if (LastChildFill)
            {
                for (firstFilledIndex = count - 1; firstFilledIndex >= 0; firstFilledIndex--)
                {
                    if (!LastVisibleChildFill || internalChildren[firstFilledIndex].IsVisible)
                        break;
                }
            }

            double usedLeftEdge = 0.0;
            double usedTopEdge = 0.0;
            double usedRightEdge = 0.0;
            double usedBottomEdge = 0.0;

            for (int i = 0; i < count; i++)
            {
                UIElement element = internalChildren[i];
                if (element != null)
                {
                    Size desiredSize = element.DesiredSize;

                    var finalRect = new Rect(
                        usedLeftEdge,
                        usedTopEdge,
                        Math.Max(0.0, (totalAvailableSize.Width - (usedLeftEdge + usedRightEdge))),
                        Math.Max(0.0, (totalAvailableSize.Height - (usedTopEdge + usedBottomEdge))));

                    if (i < firstFilledIndex)
                    {
                        switch (GetDock(element))
                        {
                            case Dock.Left:
                                usedLeftEdge += desiredSize.Width;
                                finalRect.Width = desiredSize.Width;
                                break;

                            case Dock.Top:
                                usedTopEdge += desiredSize.Height;
                                finalRect.Height = desiredSize.Height;
                                break;

                            case Dock.Right:
                                usedRightEdge += desiredSize.Width;
                                finalRect.X = Math.Max((double)0.0, (double)(totalAvailableSize.Width - usedRightEdge));
                                finalRect.Width = desiredSize.Width;
                                break;

                            case Dock.Bottom:
                                usedBottomEdge += desiredSize.Height;
                                finalRect.Y = Math.Max((double)0.0, (double)(totalAvailableSize.Height - usedBottomEdge));
                                finalRect.Height = desiredSize.Height;
                                break;
                        }
                    }
                    element.Arrange(finalRect);
                }
            }
            return totalAvailableSize;
        }
    }
}
