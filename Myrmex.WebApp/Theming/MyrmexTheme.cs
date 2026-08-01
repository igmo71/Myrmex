using MudBlazor;

namespace Myrmex.WebApp.Theming;

public static class MyrmexTheme
{
    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            // Core brand
            Primary = "#0D6EFD",
            PrimaryDarken = "#0A58CA",
            PrimaryLighten = "#3D8BFD",
            PrimaryContrastText = Colors.Shades.White,

            Secondary = "#6C757D",
            SecondaryDarken = "#565E64",
            SecondaryLighten = "#8A9298",

            Tertiary = "#495057",

            // Layout
            AppbarBackground = "#FFFFFF",
            AppbarText = "#212529",

            DrawerBackground = "#F8F9FA",
            DrawerText = "#495057",
            DrawerIcon = "#6C757D",

            Background = "#F8F9FA",
            BackgroundGray = "#E9ECEF",
            Surface = Colors.Shades.White,

            // Text
            TextPrimary = "#212529",
            TextSecondary = "#6C757D",
            TextDisabled = "#ADB5BD",

            // Actions
            ActionDefault = "#6C757D",
            ActionDisabled = "#ADB5BD",
            ActionDisabledBackground = "#E9ECEF",

            // Status colors, deliberately not Accent
            Info = "#0DCAF0",
            Success = "#198754",
            Warning = "#FFC107",
            Error = "#DC3545",

            // Lines / tables / borders
            LinesDefault = "#DEE2E6",
            TableLines = "#DEE2E6",
            Divider = "#DEE2E6",

            // Basics
            Black = "#212529",
            White = Colors.Shades.White,

            // Overlays
            OverlayLight = "rgba(33, 37, 41, 0.08)",
            OverlayDark = "rgba(33, 37, 41, 0.52)"
        },

        PaletteDark = new PaletteDark
        {
            // Core brand
            Primary = "#6EA8FE",
            PrimaryDarken = "#3D8BFD",
            PrimaryLighten = "#9EC5FE",
            PrimaryContrastText = "#052C65",

            Secondary = "#A7ACB1",
            SecondaryDarken = "#8A9298",
            SecondaryLighten = "#C4C8CB",

            Tertiary = "#CED4DA",

            // Layout
            AppbarBackground = "#212529",
            AppbarText = "#F8F9FA",

            DrawerBackground = "#2B3035",
            DrawerText = "#DEE2E6",
            DrawerIcon = "#ADB5BD",

            Background = "#212529",
            BackgroundGray = "#343A40",
            Surface = "#2B3035",

            // Text
            TextPrimary = "#F8F9FA",
            TextSecondary = "#ADB5BD",
            TextDisabled = "#6C757D",

            // Actions
            ActionDefault = "#CED4DA",
            ActionDisabled = "#6C757D",
            ActionDisabledBackground = "#495057",

            // Status colors, readable but still calm
            Info = "#6EDFF6",
            Success = "#75B798",
            Warning = "#FFDA6A",
            Error = "#EA868F",

            // Lines / tables / borders
            LinesDefault = "#495057",
            TableLines = "#495057",
            Divider = "#495057",

            // Basics
            Black = "#000000",
            White = "#F8F9FA",

            // Overlays
            OverlayLight = "rgba(248, 249, 250, 0.10)",
            OverlayDark = "rgba(0, 0, 0, 0.68)"
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
