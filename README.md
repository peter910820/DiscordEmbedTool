# DiscordEmbedTool

在本機網頁挑選 Discord 機器人所在的伺服器與文字頻道，填一份嵌入訊息後送出，並可加上表情反應。

Token 只跟著那一次請求送出，不會寫進設定檔。

## 需求

- [.NET SDK 10](https://dotnet.microsoft.com/download)（`global.json` 指定 10.0.401，可向前使用同功能帶的較新 SDK）

## 啟動

```powershell
dotnet run --project src/DiscordEmbedTool.Web --launch-profile http
```

瀏覽器打開 [http://localhost:5000](http://localhost:5000) 。

換連接埠：

```powershell
dotnet run --project src/DiscordEmbedTool.Web --urls http://localhost:8080
```

`Properties/launchSettings.json` 只影響本機 `dotnet run`。發布後的程式不會帶這份檔案，改以環境變數 `ASPNETCORE_URLS` 指定網址。

## 獨立執行檔

在有 .NET SDK 的機器上發布。預設打成單一執行檔，並自帶執行環境，目標電腦不必再安裝 .NET。可在 Windows 上直接發布 Linux 或 macOS 的檔案。換系統時先清掉舊的 `publish`，或改用不同的輸出目錄。

Windows：

```powershell
dotnet publish src/DiscordEmbedTool.Web -c Release -r win-x64 --self-contained true -o publish
```

```powershell
$env:ASPNETCORE_URLS = "http://localhost:5000"
.\publish\DiscordEmbedTool.Web.exe
```

Linux：

```powershell
dotnet publish src/DiscordEmbedTool.Web -c Release -r linux-x64 --self-contained true -o publish
```

```bash
chmod +x ./publish/DiscordEmbedTool.Web
ASPNETCORE_URLS=http://localhost:5000 ./publish/DiscordEmbedTool.Web
```

macOS：

```powershell
dotnet publish src/DiscordEmbedTool.Web -c Release -r osx-arm64 --self-contained true -o publish
```

```bash
chmod +x ./publish/DiscordEmbedTool.Web
ASPNETCORE_URLS=http://localhost:5000 ./publish/DiscordEmbedTool.Web
```

Apple Silicon 用 `osx-arm64`，Intel Mac 用 `osx-x64`。Linux 的 ARM 機器用 `linux-arm64`。

只要帶走該系統的那一個執行檔。`wwwroot` 與設定已包在裡面。第一次啟動會解到暫存目錄，會比之後慢一點。換連接埠就改 `ASPNETCORE_URLS`。發布與啟動是兩段指令，不要貼在同一行。

## 使用

1. 貼上機器人 Token，按「載入伺服器」。
2. 選擇伺服器，等待文字頻道出現。非文字頻道不會列出。
3. 選擇頻道，填嵌入內容，按「送出」。


| 欄位  | 說明                                                      |
| --- | ------------------------------------------------------- |
| 標題  | 必填。有填連結時，標題會變成可點的連結。                                    |
| 描述  | 可留空。支援 Discord markdown，例如 `**粗體**`、``程式碼``、`[文字](網址)`。 |
| 顏色  | 可留空，或填 `#RRGGBB`。                                       |
| 連結  | 可留空。必須是 `http` 或 `https` 網址。                            |
| 表情  | 可留空。以空白分隔，最多 20 個。                                      |


Unicode 表情直接輸入，例如 `🤡`。群組表情要貼完整標記：靜態為 `<:名稱:id>`，動態為 `<a:名稱:id>`。在 Discord 訊息框先打 `\` 再選表情，即可看到這串標記。機器人必須在擁有該表情的伺服器裡。

沒填 Token、沒選頻道、沒填標題、顏色格式不對，或表情超過 20 個時，結果區會顯示錯誤，不會對 Discord 送出。