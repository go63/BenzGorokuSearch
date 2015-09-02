open System
open System.Net
open System.Net.Sockets
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Windows.Forms
open System.Drawing
open System.Collections.Generic
open System.Linq
open System.Xml



type Response = {resNumber : int; name : string; id : string; date : DateTime; text : string; tag : string}

type Thread = {partNumber : int; url : string; mutable responses : Response[]}

let normalizationTable = [|
    ('0', '０');
    ('1', '１');
    ('2', '２');
    ('3', '３');
    ('4', '４');
    ('5', '５');
    ('6', '６');
    ('7', '７');
    ('8', '８');
    ('9', '９');
    ('(', '（');
    (')', '）');
    ('?', '？')|]



let normalize (str : string) =
    str.ToCharArray()
    |> Array.map (fun c ->
                  if normalizationTable.Any(fun (_, invalid) -> invalid = c) then
                      normalizationTable.First(fun (_, invalid) -> invalid = c) |> fst
                  else c)
    |> Array.foldBack
        (fun c (result, isPrevCharQuestion) ->
            match (c, isPrevCharQuestion) with
            | ('?', false) -> (c :: result, true)
            | ('?', true) -> (result, true)
            | (_, _) -> (c :: result, false))
    <| ([], false)
    |> (fun (result, _) -> new String(List.toArray result))



let rewriteAnchor partNumber (anchor : HtmlElement) =
    if String.IsNullOrEmpty(anchor.InnerText) = false &&
        Regex.IsMatch(anchor.InnerText, @">>\d{1,4}(-\d{1,4})?") &&
        partNumber <> -1 then

        anchor.InnerText <-
            Regex.Replace(
                anchor.InnerText,
                @">>(\d{1,4}(-\d{1,4})?)",
                ">>Part" + partNumber.ToString() + "-$1")
    else anchor.InnerText <- anchor.GetAttribute("href")



let parse (threads : Thread[]) (document : HtmlDocument) (url : string) (tag : string) =
    let url = Regex.Match(url, "http://.+\\.open2ch\\.net/test/read\\.cgi/[^/]+/\\d+").Captures.[0].Value
    let title = document.GetElementsByTagName("title").[0].InnerText
    let partNumber =
        if Regex.IsMatch(title, "ベンツ君.*隔離スレッド.*Part\\d+") then
            Regex.Match(title, "\\d+").Captures.[0].Value |> int
        else -1

    if partNumber = -1 ||
        document.GetElementsByTagName("div").Cast<HtmlElement>()
        |> Seq.filter (fun d -> d.InnerText <> null)
        |> Seq.exists (fun d -> d.InnerText.IndexOf("レス数が1000を超えています。") <> -1) then

        let threads =
            if threads.Any(fun t -> t.partNumber = partNumber && t.url = url) then threads
            else Array.append threads [|{partNumber = partNumber; url = url; responses = [||]}|]
        let thread = threads.First(fun t -> t.partNumber = partNumber && t.url = url)
        document.GetElementsByTagName("ares").Cast<HtmlElement>()
        |> Seq.iter (fun e -> e.InnerHtml <- "")
        let htmlElements =
            document.GetElementsByTagName("div")
                .Cast<HtmlElement>()
                .First(fun e -> e.GetAttribute("className") = "thread")
                .Children
                .Cast<HtmlElement>()
                .ToArray()

        for response in htmlElements do
            let resHeader = response.Children.[0]
            let resBody = response.Children.[1]

            let resNumber = resHeader.Children.[0].GetAttribute("val") |> int
            let name = resHeader.Children.[1].InnerText
            let date =
                Regex.Match(
                    resHeader.InnerHtml,
                    @"：.+：(\d{4}/\d\d/\d\d\([月火水木金土日]\)\d\d:\d\d:\d\d)").Groups.[1].Value
                |> DateTime.Parse
            let id = resHeader.Children.[2].GetAttribute("val")

            resBody.GetElementsByTagName("a").Cast<HtmlElement>().ToArray()
            |> Array.iter (rewriteAnchor partNumber)

            let text =
                String.Join(
                    "\n",
                    resBody.OuterText.Split('\n')
                    |> Array.map (fun l -> Regex.Replace(l, "^ +", "")))
            thread.responses <-
                Array.append
                    thread.responses
                    [|{resNumber = resNumber; name = name; id = id; date = date; text = text; tag = tag}|]

        thread.responses <-
            Array.sortWith
                (fun r1 r2 ->
                    if r1.resNumber > r2.resNumber then 1
                    else if r1.resNumber < r2.resNumber then -1
                    else 0) thread.responses
        threads
    else [||]
        


let escape (s : string) = s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;").Replace("'","&apos;")



let write (writer : StreamWriter) (thread : Thread) =
    writer.Write("<Thread partNumber=\"{0}\" url=\"{1}\">",thread.partNumber,thread.url)
    thread.responses
    |> Array.iter
        (fun r ->
            writer.Write(
                "<Response resNumber=\"{0}\" name=\"{1}\" id=\"{2}\" date=\"{3}\" tag=\"{4}\">",
                r.resNumber,
                escape r.name,
                r.id,
                r.date.ToString("yyyy/MM/dd HH:mm:ss"),
                escape r.tag)
            writer.Write(escape r.text)
            writer.WriteLine("</Response>"))
    writer.WriteLine("</Thread>")
    writer.Flush()

(*
除外リストの書き方
・エンコードはUTF-8
・excludeTagListは除外したいタグを改行区切りで
　直接そのまま書けばよい　部分一致で比較して一致すると除外される
・excludeIDListは除外したいIDを改行区切りで
　そのまま書く　大文字小文字は区別される　完全一致で除外
・現在IDが3桁の為、ID被りが発生する可能性があるので
　"スレ番号,ID"という形式で特定のスレ内のIDのみを除外するように指定できる
　IDだけ書かれている場合は全スレで除外
※スレ番号…例: http://awabi.open2ch.net/test/read.cgi/news4plus/1395142861/の場合は1395142861
*)

let extractResponse (urls : (string * string)[]) =
    let excludeTagList =
        if File.Exists("excludeTagList.txt") then
            File.ReadAllLines("excludeTagList.txt", Encoding.UTF8)
            |> Array.filter (fun s -> String.IsNullOrEmpty(s) = false && String.IsNullOrWhiteSpace(s) = false)
        else [||]

    let excludeIDList =
        if File.Exists("excludeIDList.txt") then
            File.ReadAllLines("excludeIDList.txt", Encoding.UTF8)
            |> Array.filter (fun s -> String.IsNullOrEmpty(s) = false && String.IsNullOrWhiteSpace(s) = false)
            |> Array.map
                (fun s ->
                    let param = s.Split(',')
                    if param.Length = 2 then int param.[0], param.[1]
                    else Int32.MinValue, param.[0])
        else [||]

    let form = new Form()
    let browser = new WebBrowser()
    let timer = new Timer()
    let enumerator = urls.Cast<string * string>().GetEnumerator()
    let result = new List<Thread>()

    let rec startDownload() =
        let url, tag = enumerator.Current
        let matchResult = Regex.Match(url, "\\?id=(\\w{9}|\\w{3})").Groups;
        let currentID = if matchResult.Count < 2 then "" else matchResult.[1].Value
        let threadNumber = Regex.Match(url, "/(\\d+)/").Groups.[1].Value |> int

        if excludeTagList |> Array.exists (tag.IndexOf >> ((<>) -1)) = false &&
             excludeIDList |> Array.exists (fun (num, id) -> id = currentID && (num = Int32.MinValue || num = threadNumber)) = false then
            printf "Downloading %s..." url
            browser.Navigate(url)
        else if enumerator.MoveNext() = false then
            printfn "%s has been excluded." url
            (browser.Parent :?> Form).Close()
        else
            printfn "%s has been excluded." url
            startDownload()

    let shown e =
        if enumerator.MoveNext() = false then (browser.Parent :?> Form).Close()
        else startDownload()

    let tick e =
        timer.Enabled <- false
        startDownload()
        
    let complete e =
        if browser.ReadyState = WebBrowserReadyState.Complete then
            printf "Done.\nParsing..."

            let newResult = enumerator.Current ||> parse (result.ToArray()) browser.Document
            if newResult = [||] then
                printfn "Skipped."
            else
                result.Clear()
                result.AddRange(newResult)
                printfn "Done."

            if enumerator.MoveNext() = false then
                (browser.Parent :?> Form).Close()
            else
                timer.Enabled <- true
                GC.Collect(0)

    browser.Location <- new Point(0, 0)
    browser.Size <- new Size(100, 100)
    browser.ScriptErrorsSuppressed <- true
    browser.DocumentCompleted.Add(complete)
    timer.Interval <- 2000
    timer.Tick.Add(tick)
    form.Controls.Add(browser)
    form.Location <- new Point(0, 0)
    form.Size <- new Size(100, 100)
    form.Shown.Add(shown)
    form.ShowDialog() |> ignore
    result.ToArray()
    


let createDatabaseFromUrls (urls : (string * string)[]) (fileName : string) =
    let stream = new FileStream(fileName, FileMode.Create, FileAccess.Write)
    let writer = new StreamWriter(stream)
    writer.WriteLine("<?xml version=\"1.0\"?>")
    writer.WriteLine("<Threads published=\"{0}\">", DateTime.Now.ToString("yyyy/MM/dd"))
    extractResponse urls |> Array.iter (write writer)
    writer.Write("</Threads>")
    writer.Flush()
    stream.Flush()
    stream.Close()



let parseLinks2 (html : string, result : (string * string)[]) (element : HtmlElement) =
    let link = element.GetAttribute("href").Replace("/l50","/").Replace("/l10","/")
    let linkHtml = element.OuterHtml
    let offset = html.IndexOf(linkHtml) + linkHtml.Length
    let tagPosition = html.IndexOfAny([|'('; '（'|], offset)
    let strongPosition = html.IndexOf("<strong>", offset)
    let brPosition = html.IndexOf("<br", offset)
    let endLinePosition =
        match (strongPosition, brPosition) with
        | (-1, -1) -> -1
        | (-1, br) -> br
        | (strong, -1) -> strong
        | (strong, br) -> min strong br
    
    if tagPosition = -1 then (html, Array.append result [|(link, "ベンツ君")|])
    else if tagPosition < endLinePosition || endLinePosition = -1 then
        let tag = html.Substring(tagPosition, html.IndexOfAny([|'<'|], tagPosition) - tagPosition)
        (html, Array.append result [|(link, Regex.Replace(tag, "\\s*[（\\(](.+)[）\\)][\\s・･]*", "$1") |> normalize)|])
    else (html, Array.append result [|(link, "ベンツ君")|])



let parseLinks (html : string, unknownAddresses : string[], result : (string * string)[]) (element : HtmlElement) =
    if element.TagName = "BR" && unknownAddresses.Length <> 0 then
        (html, [||], unknownAddresses |> Array.map (fun s -> (s, "ベンツ君")) |> Array.append result)
    elif element.TagName = "A" && Regex.IsMatch(element.InnerText, "^\\d+$") = false then
        let link = element.GetAttribute("href").Replace("/l50","/").Replace("/l10","/")
        let tag = new String(html.Substring(html.IndexOf(element.OuterHtml) + element.OuterHtml.Length).TakeWhile(fun c -> c <> '<').ToArray())
        if tag.All(fun c -> c = '・' || c = '<' || c = '･' || Char.IsWhiteSpace c) then
            (html, Array.append unknownAddresses [|link|], result)
        else
            (html, [||], Array.append unknownAddresses [|link|] |> Array.map ((fun t s -> (s, t)) (Regex.Replace(tag, "\\s*[（\\(](.+)[）\\)][\\s・･]*", "$1") |> normalize)) |> Array.append result)
    else (html, unknownAddresses, result)



let extractThreadAddresses (urls : string[]) =
    let form = new Form()
    let browser = new WebBrowser()
    let enumerator = urls.Cast<string>().GetEnumerator()
    let result = new List<string * string>()
    
    let shown _ =
        if enumerator.MoveNext() = false then
            (browser.Parent :?> Form).Close()
        else
            printf "Downloading %s..." enumerator.Current
            browser.Navigate(enumerator.Current)
    
    let complete _ =
        if browser.ReadyState = WebBrowserReadyState.Complete then
            printf "Done.\nParsing..."
            let content = browser.Document.GetElementById("wikibody").GetElementsByTagName("p").Cast<HtmlElement>().Last();
            content.GetElementsByTagName("a").Cast<HtmlElement>().ToArray()
            |> Array.filter (fun e -> e.Parent.TagName <> "STRONG" && Regex.IsMatch(e.GetAttribute("href"), "^http://.+\\.open2ch\\.net/test/read\\.cgi/.+/\\d+/?$") = false)
            |> Array.fold parseLinks2 (content.OuterHtml, [||])
            |> (fun (_, r) -> r)
            |> result.AddRange
            printfn "Done."
            if enumerator.MoveNext() = false then
                (browser.Parent :?> Form).Close()
            else
                printf "Downloading %s..." enumerator.Current
                browser.Navigate(enumerator.Current)

    browser.Location <- new Point(0, 0)
    browser.Size <- new Size(100, 100)
    browser.ScriptErrorsSuppressed <- true
    browser.DocumentCompleted.Add(complete)
    form.Controls.Add(browser)
    form.Location <- new Point(0, 0)
    form.Size <- new Size(100, 100)
    form.Shown.Add(shown)
    form.ShowDialog() |> ignore
    result.ToArray()



let extractWholeThreadAddress (urls : string[]) =
    let form = new Form()
    let browser = new WebBrowser()
    let enumerator = urls.Cast<string>().GetEnumerator()
    let result = new List<string * string>()
    
    let shown _ =
        if enumerator.MoveNext() = false then
            (browser.Parent :?> Form).Close()
        else
            printf "Downloading %s..." enumerator.Current
            browser.Navigate(enumerator.Current)
    
    let complete _ =
        if browser.ReadyState = WebBrowserReadyState.Complete then
            printf "Done.\nParsing..."
            let content = browser.Document.GetElementById("wikibody").GetElementsByTagName("p").Cast<HtmlElement>().Last();
            content.GetElementsByTagName("a").Cast<HtmlElement>().ToArray()
            |> Array.filter (fun e -> Regex.IsMatch(e.GetAttribute("href"), "^http://.+\\.open2ch\\.net/test/read\\.cgi/.+/\\d+/?$"))
            |> Array.map (fun e -> e.GetAttribute("href"), e.InnerText |> normalize)
            |> result.AddRange
            printfn "Done."
            if enumerator.MoveNext() = false then
                (browser.Parent :?> Form).Close()
            else
                printf "Downloading %s..." enumerator.Current
                browser.Navigate(enumerator.Current)

    browser.Location <- new Point(0, 0)
    browser.Size <- new Size(100, 100)
    browser.ScriptErrorsSuppressed <- true
    browser.DocumentCompleted.Add(complete)
    form.Controls.Add(browser)
    form.Location <- new Point(0, 0)
    form.Size <- new Size(100, 100)
    form.Shown.Add(shown)
    form.ShowDialog() |> ignore
    result.ToArray()

    

let createDatabaseFromWiki (urls : string[]) = extractThreadAddresses urls |> createDatabaseFromUrls

(*
簡易的なダイアログを表示する
最初から生成
差分を取得する→OpenFileDialog表示してデータベース選択→取得開始
差分取得＆結合→OpenFileDialog表示してデータベース選択→取得→マージして上書き保存
<Threads lastUpdated="yyyy/MM/dd HH:mm:ss">
<Thread partNumber url>
<Response resNumber name date id tag>
</Response>...
</Threads>
*)



let getThreadAddresses() =
    let logIndexUrl = "http://www63.atwiki.jp/kaiben100/pages/5.html"
    let form = new Form()
    let browser = new WebBrowser()
    let complete _ =
        if browser.ReadyState = WebBrowserReadyState.Complete then
            printf "Done.\nParsing..."
            form.Tag <- browser.Document.GetElementById("wikibody").Children.Cast<HtmlElement>().ToArray()
            |> Array.map (fun e -> e.GetElementsByTagName("a").[0].GetAttribute("href"))
            form.Close()
            printfn "Done."
    let shown _ = browser.Navigate(logIndexUrl)
    printf "Downloading %s..." logIndexUrl
    browser.Location <- new Point(0, 0)
    browser.Size <- new Size(100, 100)
    browser.ScriptErrorsSuppressed <- true
    browser.DocumentCompleted.Add(complete)
    form.Controls.Add(browser)
    form.Location <- new Point(0, 0)
    form.Size <- new Size(100, 100)
    form.Shown.Add(shown)
    form.ShowDialog() |> ignore
    form.Tag :?> string[]
    |> extractThreadAddresses



let createThreadIDIndex (threads : seq<XmlNode>) =
    let createIDIndex (res : seq<XmlNode>) =
        res
        |> Seq.map (fun n -> n.Attributes.[2].Value)
        |> Seq.distinct
        |> Seq.toArray

    threads
    |> Seq.map (fun n -> (n.Attributes.[1].Value, createIDIndex(n.ChildNodes.Cast<XmlNode>())))
    |> Seq.toArray



let extractDownloadUrls (threadIDIndex : (string * string[])[]) (newThreads : (string * string)[]) =
    let existsInIndex url id =
        threadIDIndex
        |> Array.exists (fun (threadUrl, ids) -> url = threadUrl && ids |> Array.exists ((=) id))
    
    newThreads
    |> Array.filter
        (fun (url, _) ->
            let threadUrl = Regex.Match(url, "http://.+\\.open2ch\\.net/test/read\\.cgi/[^/]+/\\d+").Captures.[0].Value
            let id = Regex.Match(url, "\\?id=(\\w{9}|\\w{3})").Groups.[1].Value
            existsInIndex threadUrl id = false)



let merge (master : XmlDocument) (diff : XmlDocument) =
    let isSameThread (thread1 : XmlNode) (thread2 : XmlNode) =
        thread1.Attributes.[0].Value = thread2.Attributes.[0].Value &&
        thread1.Attributes.[1].Value = thread2.Attributes.[1].Value

    let insertResponse (master : XmlNode) (response : XmlNode) =
        if int master.LastChild.Attributes.[0].Value < int response.Attributes.[0].Value then
            master.AppendChild response |> ignore
        else 
            let insertBeforeNode =
                master.ChildNodes.Cast<XmlNode>()
                |> Seq.pick
                    (fun n ->
                        if int response.Attributes.[0].Value < int n.Attributes.[0].Value then Some(n)
                        else None)
            master.InsertBefore(response, insertBeforeNode) |> ignore
        
    let masterThreads = master.DocumentElement.ChildNodes.Cast<XmlNode>()
    let diffThreads = diff.DocumentElement.ChildNodes.Cast<XmlNode>()
    master.DocumentElement.Attributes.[0].Value <- diff.DocumentElement.Attributes.[0].Value
    for thread in diffThreads do
        if Seq.exists (isSameThread thread) masterThreads then
            let masterThread = Seq.pick (fun t -> if isSameThread thread t then Some(t) else None) masterThreads
            thread.ChildNodes.Cast<XmlNode>()
            |> Seq.map (fun r -> master.ImportNode(r, true))
            |> Seq.iter (insertResponse masterThread)
        else
            master.ImportNode(thread, true)
            |> master.DocumentElement.AppendChild
            |> ignore



(*
ベンツ君の書き込みより荒らしの方が多いので
IDを拾ってリスト化した物を読み込ませる方式に変更

let createDatabase e =
    let saveFileDialog = new SaveFileDialog()
    saveFileDialog.Filter <- "*.xml|*.xml"
    saveFileDialog.AddExtension <- true
    saveFileDialog.InitialDirectory <- Environment.CurrentDirectory
    saveFileDialog.Title <- "保存先選択"
    if saveFileDialog.ShowDialog() = DialogResult.OK then
        createDatabaseFromUrls (getThreadAddresses()) saveFileDialog.FileName
*)



let createDatabase e =
    let openFileDialog = new OpenFileDialog()
    openFileDialog.Filter <- "*.txt|*.txt"
    openFileDialog.AddExtension <- true
    openFileDialog.InitialDirectory <- Environment.CurrentDirectory
    openFileDialog.Title <- "取得スレッド一覧選択"
    if openFileDialog.ShowDialog() = DialogResult.OK then
        let saveFileDialog = new SaveFileDialog()
        saveFileDialog.Filter <- "*.xml|*.xml"
        saveFileDialog.AddExtension <- true
        saveFileDialog.InitialDirectory <- Environment.CurrentDirectory
        saveFileDialog.Title <- "保存先選択"
        if saveFileDialog.ShowDialog() = DialogResult.OK then
            File.ReadAllLines(openFileDialog.FileName, Encoding.UTF8)
            |> Array.map (fun l -> l.Split(','))
            |> Array.map (fun l -> (l.[0], l.[1]))
            |> createDatabaseFromUrls
            <| saveFileDialog.FileName



let getDifference e =
    let openFileDialog = new OpenFileDialog()
    openFileDialog.Filter <- "*.xml|*.xml"
    openFileDialog.AddExtension <- true
    openFileDialog.InitialDirectory <- Environment.CurrentDirectory
    openFileDialog.Title <- "基準データベース選択"
    if openFileDialog.ShowDialog() = DialogResult.OK then
        let master = new XmlDocument()
        master.Load(openFileDialog.FileName)
        let threadIDIndex = createThreadIDIndex(master.DocumentElement.ChildNodes.Cast<XmlNode>())
        let newThreads = getThreadAddresses()
        let downloadUrls = extractDownloadUrls threadIDIndex newThreads
        let tempFileName = Path.GetTempFileName()
        createDatabaseFromUrls downloadUrls tempFileName
        let saveFileDialog = new SaveFileDialog()
        saveFileDialog.Filter <- "*.xml|*.xml"
        saveFileDialog.AddExtension <- true
        saveFileDialog.InitialDirectory <- Environment.CurrentDirectory
        saveFileDialog.Title <- "保存先選択"
        if saveFileDialog.ShowDialog() = DialogResult.OK then
            if File.Exists(saveFileDialog.FileName) then File.Delete(saveFileDialog.FileName)
            File.Move(tempFileName, saveFileDialog.FileName)
        else File.Delete(tempFileName)



let updateDatabase e =
    let openFileDialog = new OpenFileDialog()
    openFileDialog.Filter <- "*.xml|*.xml"
    openFileDialog.AddExtension <- true
    openFileDialog.InitialDirectory <- Environment.CurrentDirectory
    openFileDialog.Title <- "基準データベース選択"
    if openFileDialog.ShowDialog() = DialogResult.OK then
        let master = new XmlDocument()
        master.Load(openFileDialog.FileName)
        let threadIDIndex = createThreadIDIndex(master.DocumentElement.ChildNodes.Cast<XmlNode>())
        let newThreads = getThreadAddresses()
        let downloadUrls = extractDownloadUrls threadIDIndex newThreads
        let tempFileName = Path.GetTempFileName()
        createDatabaseFromUrls downloadUrls tempFileName
        let diff = new XmlDocument()
        diff.Load(tempFileName)
        merge master diff
        master.PreserveWhitespace <- true
        master.Save(openFileDialog.FileName)
        File.Delete(tempFileName)



let mergeDatabase e =
    let openFileDialog = new OpenFileDialog()
    openFileDialog.Filter <- "*.xml|*.xml"
    openFileDialog.AddExtension <- true
    openFileDialog.InitialDirectory <- Environment.CurrentDirectory
    openFileDialog.Title <- "結合先データベース選択"
    if openFileDialog.ShowDialog() = DialogResult.OK then
        let masterFileName = openFileDialog.FileName
        openFileDialog.Title <- "追加データベース選択"
        openFileDialog.FileName <- ""
        if openFileDialog.ShowDialog() = DialogResult.OK then
            let diffFileName = openFileDialog.FileName
            let master = new XmlDocument()
            let diff = new XmlDocument()
            master.Load(masterFileName)
            diff.Load(diffFileName)
            merge master diff
            master.PreserveWhitespace <- true
            master.Save(masterFileName)



let createSpecialDatabase e =
    let saveFileDialog = new SaveFileDialog()
    saveFileDialog.Filter <- "*.xml|*.xml"
    saveFileDialog.AddExtension <- true
    saveFileDialog.InitialDirectory <- Environment.CurrentDirectory
    saveFileDialog.Title <- "保存先選択"
    if saveFileDialog.ShowDialog() = DialogResult.OK then
        createDatabaseFromUrls
            (extractWholeThreadAddress [|"http://www63.atwiki.jp/kaiben100/pages/47.html"|]
            |> Array.toSeq
            |> Seq.take 30
            |> Seq.toArray)
            saveFileDialog.FileName



[<STAThread>]
[<EntryPoint>]
let main args =
    Application.EnableVisualStyles()
    Application.SetCompatibleTextRenderingDefault(true)
    let form = new Form()
    form.Text <- "GorokuCurator"
    form.ClientSize <- new Size(360, 49)

    let createDatabaseButton = new Button()
    createDatabaseButton.Text <- "生成"
    createDatabaseButton.Location <- new Point(12, 13)
    createDatabaseButton.Click.Add(createDatabase)
    form.Controls.Add(createDatabaseButton)

    let getDifferenceButton = new Button()
    getDifferenceButton.Text <- "差分取得"
    getDifferenceButton.Location <- new Point(99, 13)
    getDifferenceButton.Click.Add(getDifference)
    form.Controls.Add(getDifferenceButton)

    let updateDatabaseButton = new Button()
    updateDatabaseButton.Text <- "更新"
    updateDatabaseButton.Location <- new Point(186, 13)
    updateDatabaseButton.Click.Add(updateDatabase)
    form.Controls.Add(updateDatabaseButton)

    let mergeDatabaseButton = new Button()
    mergeDatabaseButton.Text <- "結合"
    mergeDatabaseButton.Location <- new Point(273, 13)
    mergeDatabaseButton.Click.Add(mergeDatabase)
    form.Controls.Add(mergeDatabaseButton)

    let specialButton = new Button()
    specialButton.Text <- ""
    specialButton.Size <- new Size(10, 10)
    specialButton.Location <- new Point(0, 0)
    specialButton.Click.Add(createSpecialDatabase)
    form.Controls.Add(specialButton)

    Application.Run(form)
    0