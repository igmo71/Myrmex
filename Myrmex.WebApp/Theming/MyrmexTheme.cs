using MudBlazor;

namespace Myrmex.WebApp.Theming;

public static class MyrmexTheme
{
    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            // Core brand
            Primary = "#374151",          // gray-700, calm enterprise primary
            PrimaryDarken = "#1F2937",
            PrimaryLighten = "#4B5563",

            Secondary = "#B7791F",        // muted amber/brown, less acidic than Amber 600/700
            SecondaryDarken = "#92400E",
            SecondaryLighten = "#D69E2E",

            Tertiary = "#475569",

            // Layout
            AppbarBackground = "#F8FAFC",
            AppbarText = "#1F2937",

            DrawerBackground = "#FFFFFF",
            DrawerText = "#334155",
            DrawerIcon = "#64748B",

            Background = "#F3F4F6",
            BackgroundGray = "#E5E7EB",
            Surface = "#FFFFFF",

            // Text
            TextPrimary = "#111827",
            TextSecondary = "#4B5563",
            TextDisabled = "#9CA3AF",

            // Actions
            ActionDefault = "#4B5563",
            ActionDisabled = "#9CA3AF",
            ActionDisabledBackground = "#E5E7EB",

            // Status colors: intentionally muted
            Info = "#2563EB",
            Success = "#2F855A",
            Warning = "#B7791F",
            Error = "#B91C1C",

            // Lines / tables / borders
            LinesDefault = "#D1D5DB",
            TableLines = "#E5E7EB",
            Divider = "#E5E7EB",

            // App basics
            Black = "#111827",
            White = "#FFFFFF",

            // Overlays
            OverlayLight = "rgba(17, 24, 39, 0.08)",
            OverlayDark = "rgba(17, 24, 39, 0.48)"
        },

        PaletteDark = new PaletteDark
        {
            // Core brand
            Primary = "#94A3B8",          // blue-grey/slate light, not neon
            PrimaryDarken = "#64748B",
            PrimaryLighten = "#CBD5E1",

            Secondary = "#D69E2E",        // muted amber for dark mode
            SecondaryDarken = "#B7791F",
            SecondaryLighten = "#EBCB72",

            Tertiary = "#94A3B8",

            // Layout
            AppbarBackground = "#111827",
            AppbarText = "#E5E7EB",

            DrawerBackground = "#0F172A",
            DrawerText = "#CBD5E1",
            DrawerIcon = "#94A3B8",

            Background = "#111827",
            BackgroundGray = "#0B1120",
            Surface = "#1F2937",

            // Text
            TextPrimary = "#E5E7EB",
            TextSecondary = "#CBD5E1",
            TextDisabled = "#64748B",

            // Actions
            ActionDefault = "#CBD5E1",
            ActionDisabled = "#64748B",
            ActionDisabledBackground = "rgba(100, 116, 139, 0.24)",

            // Status colors: readable but not acidic
            Info = "#60A5FA",
            Success = "#68D391",
            Warning = "#D69E2E",
            Error = "#F87171",

            // Lines / tables / borders
            LinesDefault = "#334155",
            TableLines = "#334155",
            Divider = "#334155",

            // App basics
            Black = "#020617",
            White = "#F8FAFC",

            // Overlays
            OverlayLight = "rgba(248, 250, 252, 0.08)",
            OverlayDark = "rgba(2, 6, 23, 0.72)"
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
        },

        Shadows = new Shadow
        {
            Elevation =
            [
                "none",
                "0 1px 2px rgba(15, 23, 42, 0.08)",
                "0 1px 3px rgba(15, 23, 42, 0.10)",
                "0 2px 6px rgba(15, 23, 42, 0.10)",
                "0 4px 8px rgba(15, 23, 42, 0.10)",
                "0 6px 12px rgba(15, 23, 42, 0.12)",
                "0 8px 16px rgba(15, 23, 42, 0.12)",
                "0 10px 20px rgba(15, 23, 42, 0.14)",
                "0 12px 24px rgba(15, 23, 42, 0.14)",
                "0 14px 28px rgba(15, 23, 42, 0.16)",
                "0 16px 32px rgba(15, 23, 42, 0.16)",
                "0 18px 36px rgba(15, 23, 42, 0.18)",
                "0 20px 40px rgba(15, 23, 42, 0.18)",
                "0 22px 44px rgba(15, 23, 42, 0.20)",
                "0 24px 48px rgba(15, 23, 42, 0.20)",
                "0 26px 52px rgba(15, 23, 42, 0.22)",
                "0 28px 56px rgba(15, 23, 42, 0.22)",
                "0 30px 60px rgba(15, 23, 42, 0.24)",
                "0 32px 64px rgba(15, 23, 42, 0.24)",
                "0 34px 68px rgba(15, 23, 42, 0.26)",
                "0 36px 72px rgba(15, 23, 42, 0.26)",
                "0 38px 76px rgba(15, 23, 42, 0.28)",
                "0 40px 80px rgba(15, 23, 42, 0.28)",
                "0 42px 84px rgba(15, 23, 42, 0.30)",
                "0 44px 88px rgba(15, 23, 42, 0.30)",
                "0 46px 92px rgba(15, 23, 42, 0.32)"
            ]
        }
    };
}