namespace b0wter.Audiobook

module Metadata =

    open System
    open System.Linq

    [<Serializable>]
    type UnreadableFile = {
        Filename: string
        Error: string
    }

    [<Serializable>]
    type Metadata
        = Readable of Audiobook.Audiobook
        | Unreadable of UnreadableFile
        
    let propertyList = function
        | Readable r -> [ r.Album; r.Artists; r.Title ] |> List.choose id
        | Unreadable _ -> []
        
    let maxPropertyLength (m: Metadata) =
        match m with
        | Readable r -> Audiobook.maxPropertyLength r
        | Unreadable u -> u.Error.Length
        
    let maxFilenameLength (m: Metadata) =
        match m with
        | Readable r -> r.Filename.Length
        | Unreadable u -> u.Filename.Length
        
    let duration = function
        | Readable r -> r.Duration
        | Unreadable _ -> TimeSpan.Zero
        
    let filename = function
        | Readable r -> r.Filename
        | Unreadable u -> u.Filename
        
    let durationLength (format: string) (m: Metadata) =
        (m |> duration).ToString(format).Length
        
    let onlyReadable (mm: Metadata list) : Audiobook.Audiobook list =
        mm |> List.map (function Readable r -> Some r | Unreadable _ -> None) |> List.choose id
        
    let onlyUnreadable (mm: Metadata list) : UnreadableFile list =
        mm |> List.map (function Readable _ -> None | Unreadable f -> Some f) |> List.choose id
        
    let printAudiobook (longestTag: int) (longestFilename: int) (longestDuration: int) (timespanFormat: string) (separatorRow: string) (trimFilename: string -> string) (a: Audiobook.Audiobook) =
        let nonEmptyProperties = [ a.Title; a.Album; a.Artists ] |> List.choose id
        if nonEmptyProperties.IsEmpty then
            do printfn "| %s | %s " ((a.Filename |> trimFilename).PadRight(longestFilename)) (String(' ', longestTag))
        else
            do nonEmptyProperties |> List.iteri (fun row property ->
                    let first = if row = 0 then ((a.Filename |> trimFilename).PadRight(longestFilename)) else (String(' ', longestFilename))
                    let second = property.PadRight(longestTag)
                    let third = if row = 0 then a.Duration.ToString(timespanFormat).PadLeft(longestDuration) else String(' ', longestDuration)
                    do printfn "| %s | %s | %s |" first second third
                )
        printfn "%s" separatorRow

    let printUnreadableFile (longestTag: int) (longestFilename: int) (longestDuration: int) (separatorRow: string) (trimFilename: string -> string) (f: UnreadableFile) =
        let rows = Math.Ceiling((f.Error.Length |> float) / (longestTag |> float)) |> int
        let errorRows = seq { for i in 0..(rows-1) do
                                yield String.Concat((f.Error.Skip(i * longestTag).Take(longestTag)))
                            }
        let indexedErrorRows = errorRows |> Seq.mapi (fun i s -> (i, s))
        
        let third = String(' ', longestDuration)
        for index, row in indexedErrorRows do
            let first = if index = 0 then ((f.Filename |> trimFilename).PadRight(longestFilename)) else String(' ', longestFilename)
            let second = row.PadRight(longestTag)
            do printfn "| %s | %s | %s |" first second third
        do printfn "%s" separatorRow

    let printMetadata (longestTag: int) (longestFilename: int) (longestDuration: int) (timespanFormat: string) (separatorRow: string) (trimFilename: string -> string) (m: Metadata) =
        match m with
        | Readable r ->
            printAudiobook longestTag longestFilename longestDuration timespanFormat separatorRow trimFilename r
        | Unreadable u ->
            printUnreadableFile longestTag longestFilename longestDuration separatorRow trimFilename u