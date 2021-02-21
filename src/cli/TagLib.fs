namespace b0wter.Fbrary

open Metadata
open System
open System.IO
    
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
        
    let private setIf name (isActive: Config.WriteConfig -> bool) (set: 'a -> TagLib.File -> unit) (value: 'a option)
                           (config: Config.WriteConfig) (file: TagLib.File) =
        if config |> isActive then
            do match value with
               | Some v ->
                   do printfn "Updating value for field '%s'." name
                   set v file
               | None -> ()
            file
        else file
        
    let setArtist = setIf "Arrist" (confContains artistPlaceholder) (fun v t -> t.Tag.Performers <- [| v |])
    let setAlbum = setIf "Album" (confContains albumPlaceholder) (fun v t -> t.Tag.Album <- v)
    let setAlbumArtist = setIf "Album Artist" (confContains albumArtistPlaceholder) (fun v t -> t.Tag.AlbumArtists <- [| v |])
    let setGenre = setIf "Genre" (confContains genrePlaceholder) (fun v t -> t.Tag.Genres <- [| v |])
    let setTitle = setIf "Title" (confContains titlePlaceholder) (fun v t -> t.Tag.Title <- v)
    let setComment = setIf "Comment" (confContains commentPlaceholder) (fun v t -> t.Tag.Comment <- v)
        
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
