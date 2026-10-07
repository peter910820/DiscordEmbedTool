module DiscordApi

open System.Threading.Tasks
open Discord
open Discord.Rest
open Domain

let private nameOf (value: string) = if isNull value then "" else value

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

let listGuilds (token: string) =
    task {
        if System.String.IsNullOrWhiteSpace token then
            return Error(Validation "請輸入 Token")
        else
            return!
                run token (fun client ->
                    task {
                        let discord = client :> IDiscordClient
                        let! guilds = discord.GetGuildsAsync()

                        return
                            guilds
                            |> Seq.map (fun guild ->
                                { Id = guild.Id
                                  Name = nameOf guild.Name })
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
                                |> Seq.map (fun channel ->
                                    { Id = channel.Id
                                      Name = nameOf channel.Name })
                                |> Seq.sortBy (fun channel -> channel.Name)
                                |> Seq.toList
                                |> Ok
                        | _ -> return Error(Discord "找不到伺服器")
                    })
    }
