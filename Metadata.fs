namespace b0wter.Fbrary

open System
    
module Metadata =

    type Metadata = {
        Filename: string
        Artist: string option
        AlbumArtist: string option
        Album: string option
        Title: string option
        Duration: TimeSpan
        HasPicture: bool
        Comment: string option
    }
    
    let propertyList (m: Metadata) =
        [ m.Artist; m.Album; m.Title ] |> List.choose id
        
    let maxPropertyLength (m: Metadata) =
        // Add an empty string so that there will be at least a single entry.
        "" :: (m |> propertyList) |> List.maxBy (fun s -> s.Length) |> String.length
    
    let filenameLength (m: Metadata) =
        m.Filename.Length
