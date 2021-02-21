namespace b0wter.Fbrary

open System
    
module Metadata =

    type Metadata = {
        Filename: string
        Artist: string option
        AlbumArtist: string option
        Album: string option
        Title: string option
        Genre: string option
        Duration: TimeSpan
        HasPicture: bool
        Comment: string option
    }
    
    let create (filename: string) (artist: string option) (albumArtist: string option) (album: string option)
               (title: string option) (genre: string option) (duration: TimeSpan) (hasPicture: bool) (comment: string option) =
        {
            Filename = filename
            Artist = artist
            AlbumArtist = albumArtist
            Album = album
            Title = title
            Genre = genre
            Duration = duration
            HasPicture = hasPicture
            Comment = comment
        }
    
    let propertyList (m: Metadata) =
        [ m.Artist; m.Album; m.Title ] |> List.choose id
        
    let maxPropertyLength (m: Metadata) =
        // Add an empty string so that there will be at least a single entry.
        "" :: (m |> propertyList) |> List.maxBy (fun s -> s.Length) |> String.length
    
    let filenameLength (m: Metadata) =
        m.Filename.Length