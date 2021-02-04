namespace b0wter.AudiobookLibrary

open System
open b0wter.AudiobookLibrary.Utilities

module Audiobook =
    
    type AudiobookSource
        = MultiFile of string list
        | SingleFile of string
    
    type Audiobook = {
        Source: AudiobookSource
        Artists: string option
        Album: string option
        Title: string option
        Duration: TimeSpan
        HasPicture: bool
    }
    
    let create source artists album title duration hasPicture =
        {
            Source = source
            Artists = artists
            Album = album
            Title = title
            Duration = duration
            HasPicture = hasPicture
        }
        
    /// Creates an audiobook for a single file.
    let createFromSingle (m: Metadata.Metadata) =
        create (SingleFile m.Filename) m.Artists m.Album m.Title m.Duration m.HasPicture
        
    /// Creates an audiobook for a list of files.
    let createFromMultiple (mm: Metadata.Metadata list) : Result<Audiobook, string> =
        match mm with
        | [] -> Error "Cannot create an audiobook from an empty source."
        | head :: _ ->
            let duration = mm |> List.fold (fun (aggregator: TimeSpan) (next: Metadata.Metadata) -> aggregator.Add(next.Duration)) TimeSpan.Zero
            create (mm |> List.map (fun m -> m.Filename) |> MultiFile) head.Artists head.Album head.Title duration head.HasPicture |> Ok
        
    let properties (a: Audiobook) =
        [ a.Album; a.Artists; a.Title ] |> List.choose id
    
    let maxPropertyLength (a: Audiobook) =
        a
        |> properties
        |> List.map (string >> String.length)
        |> (fun list -> if list.IsEmpty then 0 else list |> List.max)

    let serialize (a: Audiobook) : string =
        Microsoft.FSharpLu.Json.Default.serialize a
        
    let deserialize (jsonString: string) : Result<Audiobook, string> =
        let result = Microsoft.FSharpLu.Json.Default.tryDeserialize<Audiobook> jsonString
        match result with
        | Choice1Of2 a -> Ok a
        | Choice2Of2 e -> Error e
        
    let containsString pattern (a: Audiobook) =
        a |> properties |> List.exists (String.contains pattern)
        
    let isSameSource (a: Audiobook) (b: Audiobook) : bool =
        match a.Source, b.Source with
        | SingleFile _, MultiFile _ -> false
        | MultiFile _, SingleFile _ -> false
        | SingleFile s1, SingleFile s2 -> s1 = s2
        | MultiFile m1, MultiFile m2 -> m1 = m2