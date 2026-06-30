[<AutoOpen>]
module AppEggShellGallery.Components.ComponentContent

open Fable.React
open Fable.React.Props
open LibClient
open LibClient.Components
open ReactXP.Components
open ReactXP.Styles

module dom = Fable.React.Standard

type PropsConfig =
| ForFullyQualifiedName of string
| Manual of ReactElement

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        makeViewStyles {
            Overflow.Visible
        }

    let headingSecondary =
        makeViewStyles {
            marginTop 40
            marginBottom 10
        }

do
    ReactXP.LegacyStyles.Css.addCss """

.aesg-ContentComponent-table {
    border-collapse: collapse;
    width:           auto;
    max-width:       100%;
    align-self:      flex-start;
    flex-shrink:     0;
}

.aesg-ContentComponent-table td {
    max-width: 600px;
}

.aesg-ContentComponent-table > tbody > tr {
    border-top: 1px solid #cccccc;
}

"""

let private sectionHeading (label: string) =
    RX.View(
        styles = [| Styles.headingSecondary |],
        children =
            [|
                AppEggShellGallery.Components.GalleryHeadings.secondaryHeading label
            |]
    )

let private renderPropsSection (propsConfig: PropsConfig) =
    castAsElementAckingKeysWarning
        [|
            sectionHeading "Props"
            match propsConfig with
            | Manual children ->
                children
            | ForFullyQualifiedName fullyQualifiedName ->
                Ui.ScrapedComponentProps(fullyQualifiedName = fullyQualifiedName)
        |]

let private renderSamplesTable (samples: ReactElement) =
    #if EGGSHELL_PLATFORM_IS_WEB
    dom.table
        [ ClassName "aesg-ContentComponent-table" ]
        [|
            dom.tbody [] [| samples |]
        |]
    #else
    RX.View(children = [| samples |])
    #endif

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member ComponentContent(
            displayName:  string,
            samples:      ReactElement,
            ?children:    ReactChildrenProp,
            ?isResponsive: bool,
            ?notes:        ReactElement,
            ?a11y:         ReactElement,
            ?themeSamples: ReactElement,
            ?props:        PropsConfig,
            ?key:          string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        ignore (children, key, xLegacyStyles)

        let isResponsive = defaultArg isResponsive false
        let notes = defaultArg notes noElement
        let a11y = defaultArg a11y noElement
        let themeSamples = defaultArg themeSamples noElement

        RX.View(
            styles = [| Styles.view |],
            children =
                [|
                    Ui.ComponentContentHeading(
                        displayName = displayName,
                        isResponsive = isResponsive
                    )

                    if notes <> noElement then
                        castAsElementAckingKeysWarning
                            [|
                                sectionHeading "Notes"
                                RX.View(children = [| notes |])
                            |]
                    else
                        noElement

                    if a11y <> noElement then
                        castAsElementAckingKeysWarning
                            [|
                                sectionHeading "Accessibility"
                                RX.View(children = [| a11y |])
                            |]
                    else
                        noElement

                    props
                    |> Option.map renderPropsSection
                    |> Option.defaultValue noElement

                    sectionHeading "Samples"

                    RX.ScrollView(
                        horizontal = true,
                        children = [| renderSamplesTable samples |]
                    )

                    if themeSamples <> noElement then
                        castAsElementAckingKeysWarning
                            [|
                                sectionHeading "Theme"
                                RX.ScrollView(
                                    horizontal = true,
                                    children = [| renderSamplesTable themeSamples |]
                                )
                            |]
                    else
                        noElement
                |]
        )
