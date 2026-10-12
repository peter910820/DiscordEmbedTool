module DiscordApi

open System.Threading.Tasks
open Discord
open Discord.Rest
open Domain

let private nameOf (value: string) = if isNull value then "" else value

let private reactionOf (raw: string) =
    match Emote.TryParse raw with
    | true, emote -> emote :> IEmote
    | _ -> Emoji raw :> IEmote

let private normalizeToken (token: string) =
    let trimmed = token.Trim()
    let prefix = "Bot "

    if trimmed.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase) then
        trimmed.Substring(prefix.Length).Trim()
    else
        trimmed

let private run (token: string) (action: DiscordRestClient -> Task<Result<'T, ApiError>>) =
    task {
        use client = new DiscordRestClient()

        try
            do! client.LoginAsync(TokenType.Bot, normalizeToken token)
            let! result = action client

            try
                do! client.LogoutAsync()
            with _ ->
                ()

            return result
        with ex ->
            return Error(Discord ex.Message)
    }

let private readGuilds
    (pages: System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.IReadOnlyCollection<RestUserGuild>>)
    =
    let rec loop
        (enumerator:
            System.Collections.Generic.IAsyncEnumerator<System.Collections.Generic.IReadOnlyCollection<RestUserGuild>>)
        acc
        =
        task {
            let! more = enumerator.MoveNextAsync()

            if more then
                let acc = enumerator.Current |> Seq.fold (fun acc guild -> guild :: acc) acc

                return! loop enumerator acc
            else
                return List.rev acc
        }

    task {
        use enumerator = pages.GetAsyncEnumerator()
        return! loop enumerator []
    }

let listGuilds (token: string) =
    task {
        if System.String.IsNullOrWhiteSpace token then
            return Error(Validation "請輸入 Token")
        else
            return!
                run token (fun client ->
                    task {
                        let! guilds = readGuilds (client.GetGuildSummariesAsync())

                        return
                            guilds
                            |> Seq.map (fun guild ->
                                ({ Id = guild.Id
                                   Name = nameOf guild.Name }
                                : GuildSummary))
                            |> Seq.sortBy (fun guild -> guild.Name)
                            |> Seq.toList
                            |> Ok
                    })
    }

let listChannels (token: string) (guildId: uint64) =
    task {
        if System.String.IsNullOrWhiteSpace token then
            return Error(Validation "請輸入 Token")
        elif guildId = 0UL then
            return Error(Validation "請選擇伺服器")
        else
            return!
                run token (fun client ->
                    task {
                        let discord = client :> IDiscordClient
                        let! guild = discord.GetGuildAsync guildId

                        match guild with
                        | :? RestGuild as restGuild ->
                            let! channels = restGuild.GetTextChannelsAsync()

                            return
                                channels
                                |> Seq.filter (fun channel -> channel.ChannelType = ChannelType.Text)
                                |> Seq.map (fun channel ->
                                    { Id = channel.Id
                                      Name = nameOf channel.Name })
                                |> Seq.sortBy (fun channel -> channel.Name)
                                |> Seq.toList
                                |> Ok
                        | _ -> return Error(Discord "找不到伺服器")
                    })
    }

let private buildEmbed (request: SendRequest) =
    let builder = EmbedBuilder()

    let builder =
        match request.Title with
        | Some title -> builder.WithTitle title
        | None -> builder

    let builder =
        match request.Description with
        | Some description -> builder.WithDescription description
        | None -> builder

    let builder =
        match request.Url with
        | Some url -> builder.WithUrl url
        | None -> builder

    match request.Color with
    | Some color -> builder.WithColor(uint32 color)
    | None -> builder
    |> _.Build()

let send (request: SendRequest) =
    task {
        if System.String.IsNullOrWhiteSpace request.Token then
            return Error(Validation "請輸入 Token")
        elif request.ChannelId = 0UL then
            return Error(Validation "請選擇頻道")
        else
            return!
                run request.Token (fun client ->
                    task {
                        let discord = client :> IDiscordClient
                        let! channel = discord.GetChannelAsync request.ChannelId

                        match channel with
                        | :? IRestMessageChannel as messageChannel ->
                            let! message = messageChannel.SendMessageAsync(null, false, buildEmbed request)

                            try
                                for reaction in request.Reactions do
                                    do! message.AddReactionAsync(reactionOf reaction)

                                return
                                    Ok
                                        { ChannelId = request.ChannelId
                                          MessageId = message.Id }
                            with ex ->
                                return Error(Discord $"訊息已送出（{message.Id}），點表情失敗：{ex.Message}")
                        | _ -> return Error(Discord "這個頻道不能發訊息")
                    })
    }
