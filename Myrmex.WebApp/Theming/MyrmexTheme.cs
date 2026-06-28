using MudBlazor;

namespace Myrmex.WebApp.Theming;

public static class MyrmexTheme
{
    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            // Core brand
            Primary = Colors.BlueGray.Darken2,
            PrimaryDarken = Colors.BlueGray.Darken4,
            PrimaryLighten = Colors.BlueGray.Darken1,

            Secondary = Colors.Amber.Darken3,
            SecondaryDarken = Colors.Amber.Darken4,
            SecondaryLighten = Colors.Amber.Darken2,
            //Secondary = Colors.Brown.Darken2,
            //SecondaryDarken = Colors.Brown.Darken3,
            //SecondaryLighten = Colors.Brown.Darken1,

            Tertiary = Colors.BlueGray.Darken1,

            // Layout
            AppbarBackground = Colors.Gray.Lighten5,
            AppbarText = Colors.BlueGray.Darken4,

            DrawerBackground = Colors.Shades.White,
            DrawerText = Colors.BlueGray.Darken3,
            DrawerIcon = Colors.BlueGray.Darken1,

            Background = Colors.Gray.Lighten4,
            BackgroundGray = Colors.Gray.Lighten3,
            Surface = Colors.Shades.White,

            // Text
            TextPrimary = Colors.BlueGray.Darken4,
            TextSecondary = Colors.BlueGray.Darken2,
            TextDisabled = Colors.Gray.Darken1,

            // Actions
            ActionDefault = Colors.BlueGray.Darken2,
            ActionDisabled = Colors.Gray.Darken1,
            ActionDisabledBackground = Colors.Gray.Lighten3,

            // Status colors, deliberately not Accent
            Info = Colors.Blue.Darken2,
            Success = Colors.Green.Darken2,
            Warning = Colors.Amber.Darken3,
            //Warning = Colors.Amber.Darken4,
            Error = Colors.Red.Darken2,

            // Lines / tables / borders
            LinesDefault = Colors.Gray.Lighten2,
            TableLines = Colors.Gray.Lighten3,
            Divider = Colors.Gray.Lighten3,

            // Basics
            Black = Colors.BlueGray.Darken4,
            White = Colors.Shades.White,

            // Overlays
            OverlayLight = "rgba(38, 50, 56, 0.08)",
            OverlayDark = "rgba(38, 50, 56, 0.48)"
        },

        PaletteDark = new PaletteDark
        {
            // Core brand
            Primary = Colors.BlueGray.Lighten2,
            PrimaryDarken = Colors.BlueGray.Darken1,
            PrimaryLighten = Colors.BlueGray.Lighten4,

            Secondary = Colors.Amber.Darken2,
            SecondaryDarken = Colors.Amber.Darken3,
            SecondaryLighten = Colors.Amber.Lighten2,
            //Secondary = Colors.Brown.Darken2,
            //SecondaryDarken = Colors.Brown.Darken3,
            //SecondaryLighten = Colors.Brown.Darken1,

            Tertiary = Colors.BlueGray.Lighten2,

            // Layout
            AppbarBackground = Colors.BlueGray.Darken4,
            AppbarText = Colors.Gray.Lighten2,

            DrawerBackground = Colors.BlueGray.Darken4,
            DrawerText = Colors.BlueGray.Lighten4,
            DrawerIcon = Colors.BlueGray.Lighten2,

            Background = Colors.BlueGray.Darken4,
            BackgroundGray = Colors.Gray.Darken4,
            Surface = Colors.BlueGray.Darken3,

            // Text
            TextPrimary = Colors.Gray.Lighten2,
            TextSecondary = Colors.BlueGray.Lighten3,
            TextDisabled = Colors.BlueGray.Darken1,

            // Actions
            ActionDefault = Colors.BlueGray.Lighten3,
            ActionDisabled = Colors.BlueGray.Darken1,
            ActionDisabledBackground = "rgba(96, 125, 139, 0.24)",

            // Status colors, readable but still calm
            Info = Colors.Blue.Lighten2,
            Success = Colors.Green.Lighten2,
            Warning = Colors.Amber.Darken2,
            //Warning = Colors.Amber.Darken4,
            Error = Colors.Red.Lighten2,

            // Lines / tables / borders
            LinesDefault = Colors.BlueGray.Darken2,
            TableLines = Colors.BlueGray.Darken2,
            Divider = Colors.BlueGray.Darken3,

            // Basics
            Black = Colors.Shades.Black,
            White = Colors.Gray.Lighten5,

            // Overlays
            OverlayLight = "rgba(236, 239, 241, 0.08)",
            OverlayDark = "rgba(0, 0, 0, 0.72)"
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "300px"
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Roboto", "Arial", "sans-serif"],
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.45"
            },

            H4 = new H4Typography
            {
                FontSize = "1.6rem",
                FontWeight = "600",
                LineHeight = "1.25"
            },

            H5 = new H5Typography
            {
                FontSize = "1.2rem",
                FontWeight = "600",
                LineHeight = "1.3"
            },

            H6 = new H6Typography
            {
                FontSize = "1rem",
                FontWeight = "600",
                LineHeight = "1.35"
            },

            Button = new ButtonTypography
            {
                FontSize = "0.875rem",
                FontWeight = "600",
                TextTransform = "none"
            },

            Body1 = new Body1Typography
            {
                FontSize = "0.875rem",
                LineHeight = "1.45"
            },

            Body2 = new Body2Typography
            {
                FontSize = "0.8125rem",
                LineHeight = "1.4"
            },

            Caption = new CaptionTypography
            {
                FontSize = "0.75rem",
                LineHeight = "1.35"
            }
        }
    };
}