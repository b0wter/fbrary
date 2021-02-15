namespace b0wter.Fbrary

open Metadata
open System
open System.IO
    
module TagLib =
    
    let readMetaData (file: string) : Result<Metadata, string> =
        try
            do printfn "Reading metadata from %s" file
            
            let joinField (fields: string []) : string option =
                let filteredFields = fields |> Array.filter (not << String.IsNullOrWhiteSpace)
                if filteredFields.Length = 0 then None
                else String.Join(", ", filteredFields) |> Some
                
            let artists (d: TagLib.File) : string option = joinField d.Tag.Performers
                
            let albumArtists (d: TagLib.File) : string option = joinField d.Tag.AlbumArtists
                
            let genres (d: TagLib.File) : string option = joinField d.Tag.Genres
                
            let asStringOption (s: string) =
                if String.IsNullOrWhiteSpace(s) then None else Some s
                
            let d = TagLib.File.Create(file)
            {
                Filename = d.Name
                Artist = d |> artists
                Album = d.Tag.Album |> asStringOption
                AlbumArtist = d |> albumArtists
                Title = d.Tag.Title |> asStringOption
                Genre = d |> genres
                Duration = d.Properties.Duration
                HasPicture = d.Tag.Pictures.Length > 0
                Comment = d.Tag.Comment |> asStringOption
            } |> Ok
        with
        | :? TagLib.CorruptFileException as ex ->
            sprintf "%s - %s" file ex.Message |> Error
        | :? FileNotFoundException ->
            sprintf "%s - %s" file "The given file could not be read by TagLib" |> Error
        | ex ->
            sprintf "%s - %s" file ex.Message |> Error

