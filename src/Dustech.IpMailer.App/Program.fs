open System
open System.Net
open System.Net.Mail
open System.Net.Http

type EmailConfig = {
    FromAddress: string
    ToAddress: string
    Password: string
}

let createEmailConfig f t p =  { FromAddress = f; ToAddress = t; Password = p }

let getPublicIp () =
    async {
        use client = new HttpClient()

        let! ip =
            client.GetStringAsync("https://api.ipify.org")
            |> Async.AwaitTask

        return ip.Trim()
    }

let sendEmail emailConfig lastIp currentIp isCheck =
    let fromAddress = emailConfig.FromAddress
    let toAddress = emailConfig.ToAddress
    
    let subject = "IP change/check"
    
    let body = if isCheck then
                   $"Check - Your IP is %s{currentIp}"
               else
                   $"Your ip is changed from %s{lastIp} to %s{currentIp}"
    
    let password = emailConfig.Password

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

let fiveMinutes = 300000
let halfMinute = 1000
let rec workerTask emailConfig lastIp counter =
    async {        
        let! currentIp = getPublicIp ()
        
        printfn $"Counter: %d{counter}"
        
        if checkCondition (lastIp, currentIp) then
            printfn $"%A{DateTime.Now} Ip changed, send email: %s{lastIp} %s{currentIp}"
            sendEmail emailConfig lastIp currentIp false
            printfn "Press Enter to exit the program..."
        elif counter % 6 = 0 then
            printfn $"%A{DateTime.Now} check ip, send email: %s{lastIp} %s{currentIp}"
            sendEmail emailConfig lastIp currentIp true
            printfn "Press Enter to exit the program..."
        else            
            printfn $"%A{DateTime.Now} Ip not changed: %s{lastIp} %s{currentIp}"
            
        do! Async.Sleep(fiveMinutes)
        return! workerTask emailConfig currentIp (counter+1)
    }

let readPassword () =
    let rec readChars chars =
        let keyInfo = Console.ReadKey(true)
        if keyInfo.Key = ConsoleKey.Enter then
            printfn ""
            String(Array.ofList (List.rev chars))
        elif keyInfo.Key = ConsoleKey.Backspace && chars <> [] then
            printf "\b \b"
            readChars (List.tail chars)
        elif keyInfo.Key <> ConsoleKey.Backspace then
            Console.Write("*")
            readChars (keyInfo.KeyChar :: chars)
        else
            readChars chars
    readChars []

[<EntryPoint>]
let main argv =
    printfn "Please enter From Address"
    let fromAddress = Console.ReadLine()
    printfn "Please enter To Address"
    let toAddress = Console.ReadLine()
    printfn "Please enter password"
    let password = readPassword()
    
    let emailConfig = createEmailConfig fromAddress toAddress password 
    
    printfn "Press Enter to exit the program..."
    let startingIp = "1.1.1.1"

    let cts = new System.Threading.CancellationTokenSource()
    Async.Start(workerTask emailConfig startingIp 0, cts.Token)

    Console.ReadLine() |> ignore

    cts.Cancel()
    0
