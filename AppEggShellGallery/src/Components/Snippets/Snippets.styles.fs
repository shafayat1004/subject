module AppEggShellGallery.Components.SnippetsStyles

open ReactXP.LegacyStyles
open AppEggShellGallery.Colors

let styles = lazy (compile [
    "error" => [
        marginVertical 10
    ]

    "error-text" => [
        color colors.Caution.Main
    ]

    "snippet-row" => [
        marginBottom 24
        paddingBottom 16
        borderBottomWidth 1
        borderColor (Color.Grey "ee")
    ]

    "snippet-name" => [
        marginBottom 4
    ]

    "snippet-prefix" => [
        marginBottom 8
        color (Color.Grey "66")
    ]

    "snippet-scope" => [
        marginTop 8
        color (Color.Grey "99")
    ]
])

addCss (sprintf """
.aesg-Snippets-table {
    border-collapse: collapse;
    width:           100%%;
}

.aesg-Snippets-table th {
    padding:     0px 8px;
    text-align:  left;
    color:       #bbbbbb;
    font-weight: normal;
}

.aesg-Snippets-table tr:nth-child(even) {
    background-color: #fafafa;
}

.aesg-Snippets-table td {
    padding:        1em 8px;
    color:          #666;
    vertical-align: top;
}

.aesg-Snippets-table td.description {
    padding: 0 8px; // markdown component adds a 1em margin to <p> tags
}

.aesg-Snippets-table td.nowrap {
    white-space: nowrap;
}
"""
)
