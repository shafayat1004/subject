[<AutoOpen>]
module AppEggShellGallery.GallerySampleImages

open ReactXP.Styles

/// Explicit dimensions for gallery ImageCard demos. ImageCard sizes from parent layout;
/// without these, cards inside Draggable and other zero-height parents render empty.
let sampleImageCardStyles =
    makeViewStyles {
        width  300
        height 200
    }
