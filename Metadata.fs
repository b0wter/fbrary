namespace b0wter.AudiobookLibrary

open System
    
module Metadata =

    type Metadata = {
        Filename: string
        Artists: string option
        Album: string option
        Title: string option
        Duration: TimeSpan
        HasPicture: bool
    }
    
    let propertyList (m: Metadata) =
        [ m.Artists; m.Album; m.Title ] |> List.choose id
        
    let maxPropertyLength (m: Metadata) =
        // Add an empty string so that there will be at least a single entry.
        "" :: (m |> propertyList) |> List.maxBy (fun s -> s.Length) |> String.length
    
    let filenameLength (m: Metadata) =
        m.Filename.Length
