namespace b0wter.Fbrary

open Metadata
open System
open System.IO
open b0wter.FSharp
    
module TagLib =
    let private artistPlaceholder = "artist"
    let private albumPlaceholder = "album"
    let private albumArtistPlaceholder = "albumArtist"
    let private titlePlaceholder = "title"
    let private genrePlaceholder = "genre"
    let private commentPlaceholder = "comment"
    let private allPlaceholders = [
        artistPlaceholder; albumPlaceholder; albumArtistPlaceholder; titlePlaceholder; genrePlaceholder; commentPlaceholder
    ]
    
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
            
    let private confContains placeholder (config: Config.WriteConfig) =
        config.Fields |> List.contains placeholder
        
    let private setIf name _ (empty: 'a) (isActive: Config.WriteConfig -> bool) (set: 'a -> TagLib.File -> unit)
                           (get: TagLib.File -> string) (value: 'a option)
                           (config: Config.WriteConfig) (file: TagLib.File) =
        // The third parameter `_` is actually the field name and is reserved for future use.
        if config |> isActive then
            do match value with
               | Some v ->
                   do printfn "Updating value for field '%s' from '%s' to '%s'." name (get file) (value |> Option.map string |> Option.getOrElse "<not set>")
                   set v file
               | None ->
                   do printfn "Setting empty value for '%s', was '%s' previously.." name (get file)
                   set empty file
            file
        else file
        
    let setArtist = setIf "Artist" "TPE1" String.Empty (confContains artistPlaceholder) (fun v t -> t.Tag.Performers <- [| v |]) (fun t -> String.Join(", ", t.Tag.Performers))
    let setAlbum = setIf "Album" "TALB" String.Empty (confContains albumPlaceholder) (fun v t -> t.Tag.Album <- v) (fun t -> t.Tag.Album)
    let setAlbumArtist = setIf "Album Artist" "TPE2" String.Empty (confContains albumArtistPlaceholder) (fun v t -> t.Tag.AlbumArtists <- [| v |]) (fun t -> String.Join(", ", t.Tag.AlbumArtists))
    let setGenre = setIf "Genre" "TCON" String.Empty (confContains genrePlaceholder) (fun v t -> t.Tag.Genres <- [| v |]) (fun t -> String.Join(", ", t.Tag.Genres))
    let setTitle = setIf "Title" "TIT2" String.Empty (confContains titlePlaceholder) (fun v t -> t.Tag.Title <- v) (fun t -> t.Tag.Title)
    let setComment = setIf "Comment" "COMM" String.Empty (confContains commentPlaceholder) (fun v t -> t.Tag.Comment <- v) (fun t -> t.Tag.Comment)
        
    let writeMetaData (config: Config.WriteConfig) (m: Metadata.Metadata) =
        let config = if config.Fields.IsEmpty then { config with Fields = allPlaceholders } else config
        if File.Exists(m.Filename) then
            try
                do printfn "Writing tags for %s" m.Filename
                let file = (TagLib.File.Create(m.Filename)
                            |> (setArtist m.Artist config)
                            |> (setAlbum m.Album config)
                            |> (setAlbumArtist m.AlbumArtist config)
                            |> (setTitle m.Title config)
                            |> (setGenre m.Genre config)
                            |> (setComment m.Comment config))
                if config.DryRun then
                    do printfn "This was a dry run. Nothing has been written to the files."
                else
                    file.Save()
                Ok ()
            with
            | :? TagLib.CorruptFileException as ex ->
                sprintf "%s - %s" m.Filename ex.Message |> Error
            | :? FileNotFoundException ->
                sprintf "%s - %s" m.Filename "The given file could not be read by TagLib" |> Error
            | ex ->
                sprintf "%s - %s" m.Filename ex.Message |> Error
        else
            Error <| sprintf "The given file '%s' does not exist." m.Filename
