namespace Fbrary.Cli

open System
open Fbrary.Core

module Assets =
    module RatingSymbols =
        let empty = '░'
        let filled = '█'

        //let emptyCircle = '○'
        let emptyCircle = '⚬'
        let filledCircle = '●'
        //let emptyCircle = '⭕'
        //let filledCircle = '⬤'
        let dottedCircle = '◌'

    module CompletedSymbols =
        let completed = "✓"
        let notCompleted = "✗"
        let aborted = "!"

    module SimpleBorder =
        let hLine = '─'
        let vLine = '│'
        let cUpperLeft = '┌'
        let cLowerLeft = '└'
        let cLowerRight = '┘'
        let cUpperRight = '┐'
        let center = '┼'
        let tUpper = '┬'
        let tLower = '┴'
        let tLeft = '├'
        let rRight = '┤'

    module FatBorder =
        let hLine = '━'
        let vLine = '┃'
        let cUpperLeft = '┏'
        let cUpperRight = '┓'
        let cLowerLeft = '┗'
        let cLowerRight = '┛'
        let center = '╋'
        let tUpper = '┳'
        let tLower = '┻'
        let tLeft = '┣'
        let tRight = '┫'


    module TwinBorder =
        let hLine = '═'
        let vLine = '║'
        let cUpperLeft = '╔'
        let cUpperRight = '╗'
        let cLowerLeft = '╚'
        let cLowerRight = '╝'
        let center = '╬'
        let tUpper = '╦'
        let tLower = '╩'
        let tLeft = '╠'
        let tRight = '╣'

    module HighlightBorder =
        let forPlatform (a, b) =
            if System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
            then b
            else a

        let hLine = ('━', '═') |> forPlatform
        let vLine = ('┃', '║') |> forPlatform
        let cUpperLeft = ('┏', '╔') |> forPlatform
        let cUpperRight = ('┓', '╗') |> forPlatform
        let cLowerLeft = ('┗', '╚') |> forPlatform
        let cLowerRight = ('┛', '╝') |> forPlatform
        let center = ('╋', '╬') |> forPlatform
        let tUpper = ('┳', '╦') |> forPlatform
        let tLower = ('┻', '╩') |> forPlatform
        let tLeft = ('┣', '╠') |> forPlatform
        let tRight = ('┫', '╣') |> forPlatform

    module FatToSimpleConnectors =
        let doubleVTLeft = '┠'
        let doubleVTRight = '┨'
        let doubleHTUpper = '┯'
        let doubleHTLower = '┷'
        let cross = '╇'

    module TwinToSimpleConnectors =
        let doubleVTLeft = '╟'
        let doubleVTRight = '╢'
        let doubleHTUpper = '╤'
        let doubleHTLower = '╧'
        let cross = '╬'

    module HighlightConnectors =
        let forPlatform (a, b) =
            if System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
            then b
            else a

        let doubleVTLeft =
            (FatToSimpleConnectors.doubleVTLeft, TwinToSimpleConnectors.doubleVTLeft)
            |> forPlatform

        let doubleVTRight =
            (FatToSimpleConnectors.doubleVTRight, TwinToSimpleConnectors.doubleVTRight)
            |> forPlatform

        let doubleHTLower =
            (FatToSimpleConnectors.doubleHTLower, TwinToSimpleConnectors.doubleHTLower)
            |> forPlatform

        let doubleHTUpper =
            (FatToSimpleConnectors.doubleHTUpper, TwinToSimpleConnectors.doubleHTUpper)
            |> forPlatform

        let cross =
            (FatToSimpleConnectors.cross, TwinToSimpleConnectors.cross)
            |> forPlatform

    module Fields =
        type FieldType =
            | StringField of string
            | DurationFiled of TimeSpan
            | IntField of int
        
        type Field = {
            Abbreviation: string
            Name: string
            LongName: string
            Cli: string
            Table: string
            SortKey: string
            Replacer: Audiobook.Audiobook -> string option
        }
        
        // The sort key is extracted as a literal so you can use it for pattern matching.
        [<Literal>]
        let artistSortKey = "artist"
        let ArtistField = {
            Abbreviation = "Art"
            Name = "Artist"
            LongName = "Artist"
            Cli = "%artist%"
            Table = "%artist%"
            SortKey = artistSortKey
            Replacer = fun (a: Audiobook.Audiobook) -> a.Artist
        }
        
        [<Literal>]
        let albumSortKey = "album"
        let AlbumField = {
            Abbreviation = "Alb"
            Name = "Album"
            LongName = "Album"
            Cli = "%album%"
            Table = "%album%"
            SortKey = albumSortKey
            Replacer = fun (a: Audiobook.Audiobook) -> a.Album
        }
        
        [<Literal>]
        let albumArtistSortKey = "albumartist" 
        let AlbumArtistField = {
            Abbreviation = "A.Art"
            Name = "Album Artist"
            LongName = "Album Artist"
            Cli = "%albumartist%"
            Table = "%albumartist%"
            SortKey = albumArtistSortKey
            Replacer = fun (a: Audiobook.Audiobook) -> a.AlbumArtist
        }
        
        [<Literal>]
        let titleSortKey = "title"
        let TitleField = {
            Abbreviation = "Ttl"
            Name = "Title"
            LongName = "Title"
            Cli = "%title%"
            Table = "%title%"
            SortKey = titleSortKey
            Replacer = fun (a: Audiobook.Audiobook) -> a.Title
        }
        
        [<Literal>]
        let durationSortKey = "duration"
        let DurationField = {
            Abbreviation = "Dur"
            Name = "Duration"
            LongName = "Duration"
            Cli = "%duration%"
            Table = "%duration%"
            SortKey = durationSortKey
            Replacer = fun (a: Audiobook.Audiobook) -> a.Duration.ToString("h\:mm") |> Some
        }
        
        [<Literal>]
        let idSortKey = "id"
        let IdField = {
            Abbreviation = "Id"
            Name = "Id"
            LongName = "Id"
            Cli = "%id%"
            Table = "%id%"
            SortKey = idSortKey
            Replacer = fun (a: Audiobook.Audiobook) -> a.Id |> string |> Some
        }
        
        [<Literal>]
        let ratingSortKey = "rating"
        let RatingAsStringField = {
            Abbreviation = "Rtng"
            Name = "Rating"
            LongName = "Rating (string)"
            Cli = "%rating_string%"
            Table = "%rating_string%"
            SortKey = ratingSortKey
            Replacer = fun (a: Audiobook.Audiobook) ->
                        match a.Rating |> Option.map Rating.value with
                        | Some i -> Some <| sprintf "%i/%i" i Rating.maxValue
                        | None -> Some <| sprintf "-/%i" Rating.maxValue
        }
        
        let RatingAsSymbolField = {
            Abbreviation = "Rtng"
            Name = "Rating"
            LongName = "Rating (symbol)"
            Cli = "%rating_symbol%"
            Table = "%rating_symbol%"
            SortKey = ratingSortKey
            Replacer = fun (a: Audiobook.Audiobook) ->
                        match a.Rating |> Option.map Rating.value with
                        | Some i -> Some <| String(RatingSymbols.filledCircle, i) + String(RatingSymbols.emptyCircle, Rating.maxValue - i)
                        | None -> Some <| String(RatingSymbols.dottedCircle, Rating.maxValue)
        }
        
        [<Literal>]
        let commentSortKey = "comment"
        let CommentField = {
            Abbreviation = "Cmt"
            Name = "Comment"
            LongName = "Comment"
            Cli = "%comment%"
            Table = "%comment%"
            SortKey = commentSortKey
            Replacer = fun (a: Audiobook.Audiobook) -> a.Comment
        }
        
        [<Literal>]
        let genreSortKey = "genre"
        let GenreField = {
            Abbreviation = "Gnr"
            Name = "Genre"
            LongName = "Genre"
            Cli = "%genre%"
            Table = "%genre%"
            SortKey = genreSortKey
            Replacer = fun (a: Audiobook.Audiobook) -> a.Genre
        }
        
        [<Literal>]
        let completedSortKey = "completed" 
        let CompletedAsStringField = {
            Abbreviation = "Cmpl"
            Name = "Completed"
            LongName = "Completed (string)"
            Cli = "%completed_string%"
            Table = "%completed_string%"
            SortKey = completedSortKey
            Replacer = fun (a: Audiobook.Audiobook) ->
                        match a.State with
                        | Audiobook.State.Aborted -> "abrt"
                        | Audiobook.State.Completed -> "yes"
                        | Audiobook.State.NotCompleted -> "not"
                        |> Some
        }
        let CompletedAsSymbolField = {
            Abbreviation = "Cmpl"
            Name = "Completed"
            LongName = "Completed (symbol)"
            Cli = "%completed_symbol%"
            Table = "%completed_symbol%"
            SortKey = completedSortKey
            Replacer = fun (a: Audiobook.Audiobook) ->
                        match a.State with
                        | Audiobook.State.Aborted -> CompletedSymbols.aborted
                        | Audiobook.State.Completed -> CompletedSymbols.completed
                        | Audiobook.State.NotCompleted -> CompletedSymbols.notCompleted
                        |> Some
        }
        
        let allFields = [
            ArtistField; AlbumField; AlbumArtistField; TitleField; DurationField; IdField; RatingAsStringField
            RatingAsSymbolField; CommentField; GenreField; CompletedAsStringField; CompletedAsSymbolField
        ]
        
        let name (f: Field) = f.LongName
        let cli (f: Field) = f.Cli
        let table (f: Field) = f.Table
        let sortKey (f: Field) = f.SortKey
        let replacer (f: Field) = f.Replacer
        
        let cliFieldNames = allFields |> List.map cli
        let tableFieldNames = allFields |> List.map table
        let sortKeyFieldNames = allFields |> List.map sortKey
        
