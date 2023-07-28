using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace application.src.attachedProperties
{
    public class ScanToggleButtonProperties
    {
        public static readonly DependencyProperty ActionTextProperty =
            DependencyProperty.RegisterAttached(
              "ActionText",
              typeof(string),
              typeof(ScanToggleButtonProperties),
              new FrameworkPropertyMetadata(defaultValue: "",
                  flags: FrameworkPropertyMetadataOptions.AffectsRender)
            );

        public static readonly DependencyProperty HasEndedProperty =
            DependencyProperty.RegisterAttached(
              "HasEnded",
              typeof(bool),
              typeof(ScanToggleButtonProperties),
              new FrameworkPropertyMetadata(defaultValue: false,
                  flags: FrameworkPropertyMetadataOptions.AffectsRender)
            );

        public static string GetActionText(UIElement target) =>
            (string)target.GetValue(ActionTextProperty);

        public static void SetActionText(UIElement target, string value) =>
            target.SetValue(ActionTextProperty, value);

        public static bool GetHasEnded(UIElement target) =>
            (bool)target.GetValue(HasEndedProperty);

        public static void SetHasEnded(UIElement target, bool value) =>
            target.SetValue(HasEndedProperty, value);
    }


}
