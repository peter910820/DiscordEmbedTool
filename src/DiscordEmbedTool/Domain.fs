module Domain

type BotCredential = { Token: string }

type ApiError =
    | Validation of string
    | Discord of string

type EmbedDraft =
    { Title: string
      Description: string
      Color: string
      Url: string
      Reactions: string }

type SendRequest =
    { Token: string
      ChannelId: uint64
      Title: string option
      Description: string option
      Color: int option
      Url: string option
      Reactions: string list }

type SendReceipt =
    { ChannelId: uint64; MessageId: uint64 }

type GuildSummary = { Id: uint64; Name: string }

type ChannelSummary = { Id: uint64; Name: string }

module Embed =
    let private clean (value: string) =
        if System.String.IsNullOrWhiteSpace value then
            None
        else
            Some(value.Trim())

    let private parseColor (raw: string) =
        if System.String.IsNullOrWhiteSpace raw then
            Ok None
        else
            let hex = raw.Trim().TrimStart('#')

            match System.UInt32.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null) with
            | true, value when hex.Length = 6 -> Ok(Some(int value))
            | _ -> Error(Validation "顏色必須是 #RRGGBB")

    let private parseReactions (raw: string) =
        if System.String.IsNullOrWhiteSpace raw then
            Ok []
        else
            let parts =
                raw.Split([| ' '; '\t'; '\r'; '\n' |], System.StringSplitOptions.RemoveEmptyEntries)
                |> Array.toList

            if parts.Length > 20 then
                Error(Validation "表情最多 20 個")
            else
                Ok parts

    let validate (credential: BotCredential) (draft: EmbedDraft) (channelId: uint64) =
        if System.String.IsNullOrWhiteSpace credential.Token then
            Error(Validation "請輸入 Token")
        elif channelId = 0UL then
            Error(Validation "請選擇頻道")
        elif System.String.IsNullOrWhiteSpace draft.Title then
            Error(Validation "請填標題")
        else
            match parseColor draft.Color, parseReactions draft.Reactions with
            | Error error, _ -> Error error
            | _, Error error -> Error error
            | Ok color, Ok reactions ->
                Ok
                    { Token = credential.Token.Trim()
                      ChannelId = channelId
                      Title = clean draft.Title
                      Description = clean draft.Description
                      Color = color
                      Url = clean draft.Url
                      Reactions = reactions }
