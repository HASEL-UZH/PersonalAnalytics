using System.Linq;
using System.Reflection;

namespace ActivityMapper
{
    public static class ActivityCategoryExtensions
    {
        public static string GetDescription(this ActivityCategory activityCategory)
        {
            var type = typeof(ActivityCategory);
            var field = type.GetRuntimeField(activityCategory.ToString());
            var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return ((DescriptionAttribute)attributes.First()).Description;
        }
    }
}
