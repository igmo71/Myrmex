using MudBlazor;

namespace Myrmex.WebApp.Theming;

public static class MyrmexTheme
{
    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.BlueGray.Darken3,
            Secondary = Colors.Amber.Darken3,
            Tertiary = Colors.BlueGray.Darken1,

            AppbarBackground = Colors.BlueGray.Darken4,
            AppbarText = Colors.Shades.White,

            DrawerBackground = Colors.BlueGray.Darken4,
            DrawerText = Colors.BlueGray.Lighten4,
            DrawerIcon = Colors.BlueGray.Lighten3,

            Background = Colors.BlueGray.Lighten5,
            Surface = Colors.Shades.White,

            TextPrimary = Colors.BlueGray.Darken4,
            TextSecondary = Colors.BlueGray.Darken1,

            Info = Colors.Blue.Darken2,
            Success = Colors.Green.Darken2,
            Warning = Colors.Amber.Darken2,
            Error = Colors.Red.Darken2,

            LinesDefault = Colors.BlueGray.Lighten4,
            Divider = Colors.BlueGray.Lighten4
        },

        PaletteDark = new PaletteDark
        {
            Primary = Colors.BlueGray.Lighten2,
            Secondary = Colors.Amber.Accent3,
            Tertiary = Colors.BlueGray.Lighten1,

            AppbarBackground = Colors.BlueGray.Darken4,
            AppbarText = Colors.Shades.White,

            DrawerBackground = Colors.BlueGray.Darken4,
            DrawerText = Colors.BlueGray.Lighten4,
            DrawerIcon = Colors.BlueGray.Lighten3,

            Background = Colors.BlueGray.Darken4,
            Surface = Colors.BlueGray.Darken3,

            TextPrimary = Colors.Shades.White,
            TextSecondary = Colors.BlueGray.Lighten3,

            Info = Colors.Blue.Lighten2,
            Success = Colors.Green.Lighten1,
            Warning = Colors.Amber.Lighten1,
            Error = Colors.Red.Lighten1,

            LinesDefault = Colors.BlueGray.Darken2,
            Divider = Colors.BlueGray.Darken2
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px"
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Roboto", "Arial", "sans-serif"],
                FontSize = "0.875rem"
            },

            Button = new ButtonTypography
            {
                FontWeight = "600",
                TextTransform = "none"
            },

            H4 = new H4Typography
            {
                FontWeight = "600",
                FontSize = "1.65rem"
            },

            H5 = new H5Typography
            {
                FontWeight = "600",
                FontSize = "1.2rem"
            }
        }
    };
}