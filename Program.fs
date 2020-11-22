open System
open System.IO
open System.Threading.Tasks
open System.Text
open System.Text.RegularExpressions
open DSharpPlus
open DSharpPlus.EventArgs
open FSharp.Data
open FSharp.Data.HttpRequestHeaders
open FSharp.Json

let webhookURL =
    Environment.GetEnvironmentVariable "DISCORD_WEBHOOK_URL"

if webhookURL = null
then failwith "Set envvar DISCORD_WEBHOOK_URL"

type DiscordRequest = { username: string; content: string }

let sendMessage content =
    // curl -H "Content-Type: application/json" -X POST -d '{"username": "Factorio Server Watcher", "content": "はろー"}' url
    let req =
        { username = "Factorio Server Watcher"
          content = content }

    Http.AsyncRequestString
        (webhookURL,
         headers = [ ContentTypeWithEncoding(HttpContentTypes.Json, Encoding.UTF8) ],
         body = TextRequest(Json.serialize req))

[<EntryPoint>]
let main argv =
    if Array.length argv <> 1
    then failwith "Usage: factorio-notify-bot LOGFILE"

    let logfile = argv.[0]

    printfn "Starting to read log file..."
    use reader = File.OpenText(logfile)

    reader.BaseStream.Seek(0L, SeekOrigin.End)
    |> ignore

    let regex =
        new Regex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2} \[(JOIN|LEAVE)\] (.+) (?:joined the game|left the game)$")

    let rec loop () =
        async {
            let! line = Async.AwaitTask <| reader.ReadLineAsync()

            if line = null then
                //do! async { return () }
                do! Async.Sleep(1000)
            else
                let m = regex.Match line

                if m.Success then
                    printfn "%s" line
                    sendMessage line |> Async.Ignore |> Async.Start

            return! loop ()
        }

    printfn "Done."

    loop () |> Async.RunSynchronously |> ignore

    0 // return an integer exit code
