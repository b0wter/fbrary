namespace b0wter.Audiobook

module TagLib =
    open Metadata
    open System
    open System.IO
    
    let readMetaData (file: string) : Result<Metadata, string> =
        try
            do printfn "Reading metadata from %s" file
            let artists (d: TagLib.File) : string =
                let items = seq { d.Tag.JoinedComposers; d.Tag.JoinedPerformers; d.Tag.JoinedAlbumArtists }
                            |> Seq.filter (not << String.IsNullOrWhiteSpace)
                            |> Seq.distinct
                String.Join(", ", items)
                
            let asStringOption (s: string) =
                if String.IsNullOrWhiteSpace(s) then None else Some s
                
            let d = TagLib.File.Create(file)
            {
                Filename = d.Name
                Artists = d |> artists |> asStringOption 
                Album = d.Tag.Album |> asStringOption
                Title = d.Tag.Title |> asStringOption
                Duration = d.Properties.Duration
                HasPicture = d.Tag.Pictures.Length > 0
            } |> Ok
        with
        | :? TagLib.CorruptFileException as ex ->
            sprintf "%s - %s" file ex.Message |> Error
        | :? FileNotFoundException ->
            sprintf "%s - %s" file "The given file could not be read by TagLib" |> Error
        | ex ->
            sprintf "%s - %s" file ex.Message |> Error

