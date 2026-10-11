module Views

open Oxpecker.Htmx
open Oxpecker.ViewEngine
open Domain

let private describe =
    function
    | Validation message -> message
    | Discord message -> message

let private placeholder (name: string) (message: string) =
    let node = select (name = name) { option (value = "") { message } }

    node.attr ("disabled", "disabled")

let private channelReset =
    let slot = div (id = "channel-slot") { placeholder "ChannelId" "先選擇伺服器" }

    slot.hxSwapOob "innerHTML"

let private guildSelect (guilds: GuildSummary list) =
    if List.isEmpty guilds then
        placeholder "GuildId" "這個機器人沒有伺服器"
    else
        (select (name = "GuildId") {
            option (value = "") { "選擇伺服器" }

            for guild in guilds do
                option (value = string guild.Id) { guild.Name }
        })
            .hxPost("/channels")
            .hxInclude("#token")
            .hxTarget
            "#channel-slot"

let guildsFragment (guilds: GuildSummary list) =
    Fragment() {
        guildSelect guilds
        channelReset
    }

let guildsError (error: ApiError) =
    Fragment() {
        p (class' = "error") { describe error }
        channelReset
    }

let channelsFragment (channels: ChannelSummary list) =
    if List.isEmpty channels then
        placeholder "ChannelId" "這個伺服器沒有文字頻道"
    else
        select (name = "ChannelId") {
            option (value = "") { "選擇頻道" }

            for channel in channels do
                option (value = string channel.Id) { channel.Name }
        }

let channelsError (error: ApiError) = p (class' = "error") { describe error }

let page =
    html (lang = "zh-Hant") {
        head () {
            meta (charset = "utf-8")
            meta (name = "viewport", content = "width=device-width, initial-scale=1")
            title () { "Discord 嵌入訊息" }
            link (rel = "stylesheet", href = "/app.css")
            script (src = "https://cdn.jsdelivr.net/npm/htmx.org@4.0.0/dist/htmx.min.js")
        }

        body () {
            (main () {
                h1 () { "Discord 嵌入訊息" }
                label (for' = "token") { "Token" }
                input (id = "token", name = "Token", type' = "password", autocomplete = "off")
                p (class' = "hint") { "Token 只隨這次請求送出，不會寫進設定檔。" }

                (button (type' = "button") { "載入伺服器" }).hxPost("/guilds").hxInclude("#token").hxTarget "#guild-slot"

                label () { "伺服器" }
                div (id = "guild-slot") { placeholder "GuildId" "先輸入 Token" }
                label () { "頻道" }
                div (id = "channel-slot") { placeholder "ChannelId" "先選擇伺服器" }
                div (id = "loading", class' = "loading") { span (class' = "spinner") }
            })
                .hxIndicator("#loading", HxModifier.inherited)
        }
    }
