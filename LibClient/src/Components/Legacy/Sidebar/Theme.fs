module LibClient.Components.Legacy.SidebarTheme

open LibClient
open LibClient.Components

type (* class to enable named parameters *) Theme() =
    static member All
        (
            primaryBackgroundColor:           Color,
            primaryTextColor:                 Color,
            primarySelectedBackgroundColor:   Color,
            primarySelectedTextColor:         Color,
            secondaryBackgroundColor:         Color,
            secondaryTextColor:               Color,
            secondarySelectedBackgroundColor: Color,
            secondarySelectedTextColor:       Color,
            bottomBorderColor:                Color,
            countBackgroundColor:             Color,
            countTextColor:                   Color
        ) : unit =

        Themes.Set<LC.Legacy.Sidebar.Item.Theme>(
            {
                PrimaryBackgroundColor           = primaryBackgroundColor
                PrimaryTextColor                 = primaryTextColor
                PrimarySelectedBackgroundColor   = primarySelectedBackgroundColor
                PrimarySelectedTextColor         = primarySelectedTextColor
                SecondaryBackgroundColor         = secondaryBackgroundColor
                SecondaryTextColor               = secondaryTextColor
                SecondarySelectedBackgroundColor = secondarySelectedBackgroundColor
                SecondarySelectedTextColor       = secondarySelectedTextColor
                BottomBorderColor                = bottomBorderColor
                CountBackgroundColor             = countBackgroundColor
                CountTextColor                   = countTextColor
            }
        )

        Themes.Set<LC.Legacy.Sidebar.Filler.Theme>(
            {
                BottomBorderColor = bottomBorderColor
            }
        )
