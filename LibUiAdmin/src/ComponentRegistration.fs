// AUTO-GENERATED DO NOT EDIT
module LibUiAdmin.ComponentRegistration

let registerAllTheThings () : unit =
    LibClient.ComponentRegistry.RegisterRender "LibUiAdmin.Components.Legacy.QueryGrid" LibUiAdmin.Components.Legacy.QueryGridRender.render

    LibClient.ComponentRegistry.RegisterStyles ("LibUiAdmin.Components.Legacy.QueryGrid", LibUiAdmin.Components.Legacy.QueryGridStyles.styles)
