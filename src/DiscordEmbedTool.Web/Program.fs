open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Oxpecker
open DiscordApi

let guilds: EndpointHandler =
    fun ctx ->
        task {
            let! form = ctx.Request.ReadFormAsync()
            let token = form["Token"].ToString()
            let! result = listGuilds token

            match result with
            | Ok found -> return! htmlView (Views.guildsFragment found) ctx
            | Error error -> return! htmlView (Views.guildsError error) ctx
        }

let channels: EndpointHandler =
    fun ctx ->
        task {
            let! form = ctx.Request.ReadFormAsync()
            let token = form["Token"].ToString()

            let guildId =
                match System.UInt64.TryParse(form["GuildId"].ToString()) with
                | true, id -> id
                | false, _ -> 0UL

            let! result = listChannels token guildId

            match result with
            | Ok found -> return! htmlView (Views.channelsFragment found) ctx
            | Error error -> return! htmlView (Views.channelsError error) ctx
        }

let endpoints =
    [ GET [ route "/" <| htmlView Views.page ]
      POST [ route "/guilds" guilds; route "/channels" channels ] ]

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    builder.Services.AddRouting().AddOxpecker() |> ignore
    let app = builder.Build()
    app.UseStaticFiles() |> ignore
    app.UseRouting().Use(Default.exceptionMiddleware).UseOxpecker(endpoints).Run(Default.notFoundHandler)
    app.Run()
    0 // Exit code
