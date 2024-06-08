open System
open System.Net
open System.Net.Mail
open System.Net.Http

let getPublicIp () =
    async {
        use client = new HttpClient()

        let! ip =
            client.GetStringAsync("https://api.ipify.org")
            |> Async.AwaitTask

        return ip.Trim()
    }

let sendEmail lastIp currentIp =
    let fromAddress = ""
    let toAddress = ""
    let subject = "IP change"
    let body = $"Your ip is changed from %s{lastIp} to %s{currentIp}"
    let password = ""

    let smtpClient = new SmtpClient("smtp.office365.com", 587)
    smtpClient.Credentials <- new NetworkCredential(fromAddress, password)
    smtpClient.EnableSsl <- true

    let mailMessage = new MailMessage()
    mailMessage.From <- MailAddress(fromAddress)
    mailMessage.To.Add(toAddress)
    mailMessage.Subject <- subject
    mailMessage.Body <- body

    try
        smtpClient.Send(mailMessage)
        printfn "Email successfully sent."
    with
    | ex -> printfn $"Sending email error: %s{ex.Message}"


let checkCondition (lastIp, currentIp) = not (lastIp.Equals currentIp)

let rec workerTask lastIp =
    async {
        do! Async.Sleep(300000)
        let! currentIp = getPublicIp ()

        if checkCondition (lastIp, currentIp) then
            printfn $"Ip changed, send email: %s{lastIp} %s{currentIp}"
            sendEmail lastIp currentIp
            printfn "Press Enter to exit the program..."
        return! workerTask currentIp
    }



[<EntryPoint>]
let main argv =
    printfn "Press Enter to exit the program..."
    let startingIp = "1.1.1.1"

    let cts = new System.Threading.CancellationTokenSource()
    Async.Start(workerTask startingIp, cts.Token)

    Console.ReadLine() |> ignore

    cts.Cancel()
    0
