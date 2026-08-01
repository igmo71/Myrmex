using MudBlazor;

namespace Myrmex.WebApp.Theming;

public static class MyrmexTheme
{
    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            // Core brand
            Primary = "#B86600",
            PrimaryDarken = "#874800",
            PrimaryLighten = "#D98300",
            PrimaryContrastText = Colors.Shades.White,

            Secondary = "#41596B",
            SecondaryDarken = "#2C414F",
            SecondaryLighten = "#607789",

            Tertiary = "#526C7D",

            // Layout
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1C2A33",

            DrawerBackground = "#F8FAFB",
            DrawerText = "#344B5A",
            DrawerIcon = "#526B7A",

            Background = "#F3F6F8",
            BackgroundGray = "#E6EBEF",
            Surface = Colors.Shades.White,

            // Text
            TextPrimary = "#1C2A33",
            TextSecondary = "#526774",
            TextDisabled = "#8998A1",

            // Actions
            ActionDefault = "#41596B",
            ActionDisabled = "#8A99A2",
            ActionDisabledBackground = "#E2E8EC",

            // Status colors, deliberately not Accent
            Info = "#1769AA",
            Success = "#2E7D4F",
            Warning = "#B86600",
            Error = "#B3261E",

            // Lines / tables / borders
            LinesDefault = "#CDD7DE",
            TableLines = "#DCE4E9",
            Divider = "#D7E0E6",

            // Basics
            Black = "#1C2A33",
            White = Colors.Shades.White,

            // Overlays
            OverlayLight = "rgba(28, 42, 51, 0.08)",
            OverlayDark = "rgba(20, 31, 38, 0.52)"
        },

        PaletteDark = new PaletteDark
        {
            // Core brand
            Primary = "#F2A51D",
            PrimaryDarken = "#C97600",
            PrimaryLighten = "#FFC04D",
            PrimaryContrastText = "#17232B",

            Secondary = "#A5BBC8",
            SecondaryDarken = "#78909E",
            SecondaryLighten = "#C4D4DD",

            Tertiary = "#9AB1BF",

            // Layout
            AppbarBackground = "#172630",
            AppbarText = "#F1F5F7",

            DrawerBackground = "#14212A",
            DrawerText = "#D7E2E8",
            DrawerIcon = "#9EB4C1",

            Background = "#101A21",
            BackgroundGray = "#16242D",
            Surface = "#1C2C36",

            // Text
            TextPrimary = "#F1F5F7",
            TextSecondary = "#B2C2CB",
            TextDisabled = "#71838D",

            // Actions
            ActionDefault = "#C0D0D8",
            ActionDisabled = "#71838D",
            ActionDisabledBackground = "#293943",

            // Status colors, readable but still calm
            Info = "#70B8F5",
            Success = "#75C794",
            Warning = "#F2A51D",
            Error = "#FF8A80",

            // Lines / tables / borders
            LinesDefault = "#40525D",
            TableLines = "#354852",
            Divider = "#30424C",

            // Basics
            Black = "#0C1318",
            White = "#F1F5F7",

            // Overlays
            OverlayLight = "rgba(214, 230, 238, 0.10)",
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
