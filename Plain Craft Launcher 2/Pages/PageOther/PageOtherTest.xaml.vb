Imports System.IO
Imports System.Security.Principal

''' <summary>
''' 百宝箱页面：今日人品、内存优化、清理垃圾、自定义下载、回声洞。
''' </summary>
Public Class PageOtherTest

    '==================================================================
    ' 回声洞语录
    '==================================================================

    ''' <summary>
    ''' 社区留言语录库，用于「回声洞」随机展示。
    ''' </summary>
    Private Shared ReadOnly EchoCaveQuotes As String() = {
        "比钻石更强！",
        "BUGJANG!",
        "猪灵劲曲！",
        "床里面藏着 TNT！",
        "这么多留言中你偏偏看见我的，爱你么么哒",
        "我铸剑，你冰球，我们都有光明的未来！",
        "Deadline 是第一生产力！",
        "核电，轻而易举啊！",
        "今天也努力当咸鱼了呢",
        "MC 是我们的青春啊！",
        "烧烤炉制作教程：机械动力+瓦尔基里+我的世界+低配电脑",
        "Also try 新闻主页！",
        "由于目标计算机积极拒绝，无法连接。",
        "菌变，极限模式，随机种子，直接开始！",
        "龙猫得了 MVP！",
        "我的世界，启动！",
        "Look familiar？见此消息者，视为自愿加入超级地球！",
        "龙猫你玩 MC 吗",
        "正于此地，愿你找到想要的书。",
        "Also try SCP-CN-660-J!",
        "正于此地，愿你找到想要的 Mod 和整合包！",
        "Also try SCP-CN-048-J!",
        "愿光之种在每个人的心中萌发！",
        "任天堂就是世界的主宰！",
        "To be continued.",
        "脑子放假去了！",
        "黄瓜鸡蛋粥，好吃又美味！",
        "刷到此条的，一切都会如愿，所想所念一定能成功！",
        "当你觉得我咕了，但是我没咕，这也是一种咕！",
        "没有钻石块的可以用下界合金块代替！",
        "心脏停跳文学社！",
        "Vamos!",
        "50382 警告！",
        "Priority Crash Launcher!",
        "自己都不相信自己，那怎么会成功？",
        "Minecraft saves the world!",
        "那就当做没有 Bug 好了！",
        "试试泰拉瑞亚！",
        "向骰子低头！",
        "隐藏主题解锁给点提示行不行啊 QAQ！",
        "破产了启动器！",
        "泡菜龙启动器！",
        "劈柴驴启动器！",
        "碰瓷狼启动器！",
        "等泰拉瑞亚 2、半条命 3、巫师 4、辐射 5、老滚 6 发售了我就能玩上了，我真幸运！",
        "Keep your DETERMINATION.",
        "我东方永不过气！",
        "想拉萌新朋友一起玩 MC 的话，选一些简单的整合包更好入手哦！",
        "陌生人，虽然不认识你，但祝你早安，午安，晚安，事事平安 :)",
        "希望这个游戏可以给你带来更多的快乐！",
        "Also try Celeste!",
        "Also try The Witness!",
        "结束了？结束了。真结束了吗？如结束。",
        "一定不要贪小便宜去非微软平台购买 MC 账号，百分之九十五是黑卡账户！",
        "要恰饭的嘛！",
        "人生没有标准答案，去做自己认为正确的事！",
        "帅得体面，猫得明白！",
        "王泪天下第一！",
        "FLAG IS WIN!",
        "凌波微步，快乐的舞步。",
        "这个叫 TAS 的人是不是开挂了？",
        "For Super Earth!",
        "爽啊！",
        "支持正版 MC，加油！",
        "失之东隅，收之桑榆。",
        "让我们拜请白日铸炉。",
        "我再发回声洞我就是狗。",
        "骗你的，我本来就是 qwq",
        "开学哩。",
        "别点了，手不累吗？",
        "很好的 MOD，使我存档损坏。",
        "叶底藏花一度，梦里踏雪几回。",
        "另类本身就是一种出类拔萃。",
        "麻辣鸡翅真好吃！",
        "你好，已深度思考，用时 0 秒。",
        "服务器繁忙，请稍后再试。",
        "For the Emperor!",
        "如果你看到了这句话，一定会认真地读下去，读到最后就会发现这句话没有任何意义，恭喜浪费了一分钟。",
        "Wake up. It's time to go to school.",
        "反复下载的是游戏，回不到的是童年。",
        "有的人毕业了，有的人还没开学。",
        "在 38 度的太阳底下放寒假。",
        "Made in Abyss!",
        "这就是被央视点名表扬的游戏吗？",
        "吃水不忘挖井人，不要忘记这超赞的 PCL 是龙猫在支撑。",
        "我的世界我做主！",
        "早上好，中午好，晚上好。",
        "希望能在一些地方留下自己的一点痕迹。",
        "如果你看到这个，说明我的存在是有意义的。",
        "MC，启动！",
        "各个国家有各个国家的国歌。",
        "挖矿免费玩！",
        "宁可没用，不能没有。",
        "龙猫是龙猫，龙猫是龙猫，但龙猫不是龙猫。",
        "拜托，反复点击回声洞来看自己的投稿也太逊了。",
        "埋骨何须桑梓地。",
        "这很科学，但不 MC。",
        "我上我也行，但我不上。",
        "世间所有的相遇，都是久别重逢。",
        "做个快乐的矿工吧。",
        "钻石恒久远，一颗永流传。",
        "你玩游戏，不是游戏玩你。",
        "别熬夜玩游戏，小心被苦力怕盯上。",
        "一格水，万般情。",
        "此面向敌。",
        "我们终将在钻石层相遇。",
        "如果你不知道玩什么，那就玩 MC 吧。",
        "服务器在线人数：1，那个人是你。",
        "这不是 Bug，这是特性。",
        "红石使我快乐。",
        "泰拉瑞亚也很好玩，真的。",
        "会当凌绝顶，一览众山小。",
        "记得备份存档。",
        "存档丢失的痛苦，只有经历过才懂。",
        "你永远可以相信 MC。",
        "路漫漫其修远兮，吾将上下而求索。",
        "种田使人快乐。",
        "没有什么是挖矿解决不了的，如果有，就挖两次。",
        "挖到最后，挖到了自己。",
        "保持热爱，奔赴山海。",
        "今天的月亮真圆，适合出门冒险。",
        "请不要在服务器里放置 TNT。",
        "愿你所求皆如愿，所行化坦途。",
        "慢慢来，比较快。",
        "这个启动器很好用，谢谢你，龙猫。",
        "感谢每一位为 MC 社区做出贡献的人。",
        "玩得开心最重要。",
        "世界很大，地图很小。",
        "你的存档，你的世界。",
        "重启试试？",
        "一切都还来得及。",
        "愿你被这个世界温柔以待。",
        "回头看看，你已经走了很远。",
        "别怕，苦力怕也会怕水。",
        "下矿记得带火把。",
        "钻石是给有准备的人的。",
        "一切都会好起来的。",
        "保持好奇心。",
        "有时候，慢一点反而更好。",
        "珍重，再会。"
    }

    ''' <summary>
    ''' 随机返回一条社区留言。
    ''' </summary>
    Public Shared Function GetRandomCave() As String
        Return EchoCaveQuotes(RandomInteger(0, EchoCaveQuotes.Length - 1))
    End Function

    ''' <summary>
    ''' 随机返回一条提示文本。
    ''' </summary>
    Public Shared Function GetRandomHint() As String
        Return EchoCaveQuotes(RandomInteger(0, EchoCaveQuotes.Length - 1))
    End Function

    ''' <summary>
    ''' 随机返回一条预设提示（与 GetRandomHint 一致，保留以兼容既有调用）。
    ''' </summary>
    Public Shared Function GetRandomPresetHint() As String
        Return EchoCaveQuotes(RandomInteger(0, EchoCaveQuotes.Length - 1))
    End Function

    '==================================================================
    ' 今日人品
    '==================================================================

    ''' <summary>
    ''' 弹出附加使用条款，同意后给出今日人品值。
    ''' 结果由「用户名 + 日期」哈希决定，同一天同一账号结果一致。
    ''' </summary>
    Public Shared Sub Jrrp(Optional ShowHint As Boolean = True)
        Dim TermsText As String =
            $"在查看结果前，请先同意以下附加使用条款：{vbCrLf}{vbCrLf}" &
            "1. 我知晓并了解 PCL 的今日人品功能完全没有出 Bug。" & vbCrLf &
            "2. PCL 不对使用本软件所间接造成的一切财产损失（如砸电脑等）负责。"
        If MyMsgBox(TermsText, "今日人品 - 附加使用条款", "同意并查看结果", "再见") <> 0 Then Return
        Dim Result As Integer = CalcJrrp()
        Dim Comment As String = JrrpComment(Result)
        Dim Message As String = $"你今天的人品值是：{Result} / 100{vbCrLf}{vbCrLf}{Comment}"
        If ShowHint Then Hint($"今日人品：{Result} / 100", HintType.Blue)
        MyMsgBox(Message, "今日人品", "好的")
    End Sub

    ''' <summary>
    ''' 计算今日人品值（0-100），同一天同一用户名结果稳定。
    ''' </summary>
    Private Shared Function CalcJrrp() As Integer
        Try
            '优先用离线用户名，其次用固定值；同一天同一账号结果稳定
            Dim UserName As String = Settings.Get(Of String)("LoginLegacyName")
            If String.IsNullOrEmpty(UserName) Then UserName = "PCL"
            Dim Seed As String = UserName & "|" & Date.Now.ToString("yyyy-MM-dd")
            Dim Hash As Integer = 17
            For Each C As Char In Seed
                Hash = Hash * 31 + AscW(C)
                Hash = Hash And &H7FFFFFFF
            Next
            Return Hash Mod 101
        Catch ex As Exception
            Logger.Warn(ex, "计算今日人品失败")
            Return RandomInteger(0, 100)
        End Try
    End Function

    ''' <summary>
    ''' 根据人品值给出评语。
    ''' </summary>
    Private Shared Function JrrpComment(Value As Integer) As String
        Select Case True
            Case Value >= 95 : Return "天选之子！今天下矿必出钻石。"
            Case Value >= 85 : Return "运气极佳，适合挑战新内容。"
            Case Value >= 70 : Return "诸事顺利，可以放心开新档。"
            Case Value >= 50 : Return "平稳的一天，稳扎稳打即可。"
            Case Value >= 30 : Return "运气一般，记得多带点火把。"
            Case Value >= 10 : Return "今天不太顺，建议先备份存档。"
            Case Else : Return "出门记得带盾牌，别碰 TNT。"
        End Select
    End Function

    '==================================================================
    ' 内存优化
    '==================================================================

    Private Shared _MemoryOptimizing As Boolean = False

    ''' <summary>
    ''' 执行内存优化，返回 (是否成功, 可用内存变化量字节, 提示文本)。
    ''' 说明：本实现完全在当前进程内完成，不再通过 UAC 提权另起进程，
    '''       避免弹出权限框后主界面卡在「正在优化中」。
    '''       无管理员权限时同样可以清理自己可见进程的工作集。
    ''' </summary>
    Public Shared Function MemoryOptimize(Optional ShowHint As Boolean = False) As (Success As Boolean, DeltaBytes As Long, Message As String)
        If _MemoryOptimizing Then
            Return (False, 0, "内存优化正在进行中，请稍候……")
        End If
        _MemoryOptimizing = True
        Try
            Dim Before As Long = GetAvailableMemory()
            '1. 清空各进程工作集
            Dim Handled As Integer = EmptyWorkingSetAll()
            '2. 自身垃圾回收
            Try
                GC.Collect()
                GC.WaitForPendingFinalizers()
                GC.Collect()
                GC.WaitForPendingFinalizers()
            Catch ex As Exception
                Logger.Warn(ex, "垃圾回收失败")
            End Try
            '3. 给系统一点时间回收
            Thread.Sleep(800)
            Dim After As Long = GetAvailableMemory()
            Dim Delta As Long = After - Before
            Dim Text As String
            If Delta > 1024 * 1024 Then
                Text = $"优化完成，可用内存增加了 {StringUtils.FormatByteSize(Delta)}。{vbCrLf}当前可用内存：{StringUtils.FormatByteSize(After)}（已处理 {Handled} 个进程）"
            Else
                Text = $"优化完成，已经优化到最佳状态。{vbCrLf}当前可用内存：{StringUtils.FormatByteSize(After)}（已处理 {Handled} 个进程）"
            End If
            If ShowHint Then Hint(Text.Replace(vbCrLf, " "), HintType.Green)
            Logger.Info($"内存优化完成：{StringUtils.FormatByteSize(Before)} → {StringUtils.FormatByteSize(After)}，处理 {Handled} 个进程")
            Return (True, Delta, Text)
        Catch ex As Exception
            Logger.Error(ex, "内存优化失败")
            If ShowHint Then Hint("内存优化失败：" & ex.Message, HintType.Red)
            Return (False, 0, "内存优化失败：" & ex.Message)
        Finally
            _MemoryOptimizing = False
        End Try
    End Function

    ''' <summary>
    ''' 兼容既有调用的无返回值版本。
    ''' </summary>
    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
        MemoryOptimize(ShowHint)
    End Sub

    Private Shared Function GetAvailableMemory() As Long
        Try
            Return CLng(My.Computer.Info.AvailablePhysicalMemory)
        Catch
            Return 0
        End Try
    End Function

    '------------------------------------------------------------------
    ' 工作集清理（把各进程占用的物理内存还回系统）
    ' 原理：对每个可打开的进程调用 EmptyWorkingSet，使其工作集被裁剪，
    '       从而释放物理内存。需要管理员权限才能处理其他用户的进程。
    '------------------------------------------------------------------

    Private Declare Function OpenProcess Lib "kernel32" (dwDesiredAccess As Integer, bInheritHandle As Boolean, dwProcessId As Integer) As IntPtr
    Private Declare Function CloseHandle Lib "kernel32" (hObject As IntPtr) As Boolean
    Private Declare Function EmptyWorkingSet Lib "psapi" (hProcess As IntPtr) As Boolean

    Private Const PROCESS_QUERY_INFORMATION As Integer = &H400
    Private Const PROCESS_SET_QUOTA As Integer = &H100

    ''' <summary>
    ''' 清空所有可访问进程的工作集，返回成功处理的进程数量。
    ''' </summary>
    Private Shared Function EmptyWorkingSetAll() As Integer
        Dim Handled As Integer = 0
        Dim ProcessList As Process() = Nothing
        Try
            ProcessList = Process.GetProcesses()
        Catch ex As Exception
            Logger.Warn(ex, "获取进程列表失败")
            Return 0
        End Try
        For Each P As Process In ProcessList
            Dim Handle As IntPtr = IntPtr.Zero
            Try
                Handle = OpenProcess(PROCESS_QUERY_INFORMATION Or PROCESS_SET_QUOTA, False, P.Id)
                If Handle <> IntPtr.Zero Then
                    If EmptyWorkingSet(Handle) Then Handled += 1
                End If
            Catch
                '单个进程失败不影响其它进程
            Finally
                If Handle <> IntPtr.Zero Then CloseHandle(Handle)
                P.Dispose()
            End Try
        Next
        Logger.Info($"已清空 {Handled} 个进程的工作集")
        Return Handled
    End Function


    '==================================================================
    ' 清理垃圾
    '==================================================================

    ''' <summary>
    ''' 清理 PCL 运行产生的临时文件、日志与缓存。
    ''' 只删除缓存目录与旧日志，不会触碰存档、Mod 或游戏本体。
    ''' </summary>
    Public Shared Sub RubbishClear()
        Try
            Dim RemovedSize As Long = 0
            Dim Failed As New List(Of String)

            '1. 清理 PCL 缓存目录
            Try
                Dim CacheDir As String = PathTemp & "Cache\"
                RemovedSize += ClearDirectory(CacheDir)
                Logger.Info($"已清理缓存目录：{CacheDir}")
            Catch ex As Exception
                Failed.Add("缓存目录")
                Logger.Warn(ex, "清理缓存目录失败")
            End Try

            '2. 清理 PCL 临时文件夹中的任务残留
            Try
                Dim TaskDir As String = PathTemp & "PCL\"
                RemovedSize += ClearDirectory(TaskDir)
            Catch ex As Exception
                Logger.Warn(ex, "清理任务目录失败")
            End Try

            '3. 清理旧日志（保留最近 3 个）
            Try
                RemovedSize += ClearOldLogs(Paths.BaseThenName, 3)
            Catch ex As Exception
                Failed.Add("日志文件")
                Logger.Warn(ex, "清理日志失败")
            End Try

            '4. 清理皮肤缓存
            Try
                Dim SkinDir As String = PathTemp & "Cache\Skin\"
                RemovedSize += ClearDirectory(SkinDir)
            Catch ex As Exception
                Logger.Warn(ex, "清理皮肤缓存失败")
            End Try

            If Failed.Count > 0 Then
                Hint($"清理完成，但有 {Failed.Count} 项未能清理（{String.Join("、", Failed)}）。", HintType.Blue)
            Else
                Hint($"垃圾已经清理好啦，共释放 {StringUtils.FormatByteSize(RemovedSize)}。", HintType.Green)
            End If
            Logger.Info($"清理垃圾完成，释放 {StringUtils.FormatByteSize(RemovedSize)}")
        Catch ex As Exception
            Logger.Error(ex, "清理垃圾失败")
            Hint("清理垃圾失败：" & ex.Message, HintType.Red)
        End Try
    End Sub

    ''' <summary>
    ''' 清空目录内容（保留目录本身），返回释放的字节数。
    ''' </summary>
    Private Shared Function ClearDirectory(Dir As String) As Long
        If Not Directory.Exists(Dir) Then Return 0
        Dim Total As Long = 0
        For Each FilePath As String In Directory.GetFiles(Dir, "*", SearchOption.AllDirectories)
            Try
                Dim Length As Long = New FileInfo(FilePath).Length
                File.Delete(FilePath)
                Total += Length
            Catch
                '单个文件失败忽略，继续清理其它文件
            End Try
        Next
        '删除空目录（自底向上）
        Try
            For Each SubDir As String In Directory.GetDirectories(Dir, "*", SearchOption.AllDirectories).
                OrderByDescending(Function(d) d.Length)
                Try
                    If Not Directory.EnumerateFileSystemEntries(SubDir).Any() Then Directory.Delete(SubDir)
                Catch
                End Try
            Next
        Catch
        End Try
        Return Total
    End Function

    ''' <summary>
    ''' 保留最近 KeepCount 个日志文件，删除更早的，返回释放的字节数。
    ''' </summary>
    Private Shared Function ClearOldLogs(Dir As String, KeepCount As Integer) As Long
        If Not Directory.Exists(Dir) Then Return 0
        Dim Total As Long = 0
        Dim Logs As FileInfo() = New DirectoryInfo(Dir).GetFiles("*.txt").
            Where(Function(f) f.Name.StartsWithF("Log")).
            OrderByDescending(Function(f) f.LastWriteTime).
            ToArray()
        For i As Integer = KeepCount To Logs.Length - 1
            Try
                Dim Length As Long = Logs(i).Length
                Logs(i).Delete()
                Total += Length
            Catch
            End Try
        Next
        Return Total
    End Function

    '==================================================================
    ' 自定义下载
    '==================================================================

    ''' <summary>
    ''' 兼容既有调用：第三个参数为保存目录，文件名与目录拼接后作为完整路径。
    ''' </summary>
    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Folder As String)
        Dim Target As String
        If String.IsNullOrWhiteSpace(Folder) Then
            Target = FileName
        Else
            Target = Path.Combine(Folder, FileName)
        End If
        StartCustomDownload(Url, Target)
    End Sub

    ''' <summary>
    ''' 从指定网址下载文件到指定路径。
    ''' SavePath 为完整的目标文件路径；留空则下载到 PCL 默认下载文件夹并使用网址推断的文件名。
    ''' </summary>
    Public Shared Sub StartCustomDownload(Url As String, SavePath As String)
        If String.IsNullOrWhiteSpace(Url) Then
            Hint("请填写下载网址。", HintType.Red)
            Return
        End If
        Dim Check As Uri = Nothing
        If Not Uri.TryCreate(Url.Trim(), UriKind.Absolute, Check) OrElse
           (Check.Scheme <> "http" AndAlso Check.Scheme <> "https") Then
            Hint("下载网址格式不正确，需要以 http 或 https 开头。", HintType.Red)
            Return
        End If
        '从网址推断文件名
        Dim UrlFileName As String = ""
        Try
            UrlFileName = Path.GetFileName(Check.LocalPath)
        Catch
        End Try
        If String.IsNullOrWhiteSpace(UrlFileName) Then UrlFileName = "download_" & GetTimeMs() & ".bin"

        '确定最终保存路径
        Dim Target As String
        If String.IsNullOrWhiteSpace(SavePath) Then
            Dim DefaultFolder As String = Settings.Get(Of String)("CacheDownloadFolder")
            If String.IsNullOrWhiteSpace(DefaultFolder) Then DefaultFolder = Paths.AppData & "PCL\Downloads\"
            Target = Path.Combine(DefaultFolder, UrlFileName)
        Else
            Target = SavePath.Trim().Trim(New Char() {""""c})
            '如果给的是目录（以 \ 结尾，或指向已存在的文件夹），则自动拼接文件名
            Try
                If Target.EndsWith("\") OrElse Target.EndsWith("/") OrElse
                   (Directory.Exists(Target) AndAlso Not File.Exists(Target)) Then
                    Target = Path.Combine(Target, UrlFileName)
                End If
            Catch
            End Try
        End If

        Dim Folder As String = ""
        Try
            Folder = Path.GetDirectoryName(Target)
        Catch
        End Try
        If String.IsNullOrWhiteSpace(Folder) Then
            Hint("保存路径无效，请检查后重试。", HintType.Red)
            Return
        End If
        Try
            DirectoryUtils.Create(Folder)
        Catch ex As Exception
            Hint("无法创建目标文件夹：" & ex.Message, HintType.Red)
            Return
        End Try

        Dim DisplayName As String = Path.GetFileName(Target)
        RunInThread(Sub()
            Try
                Hint($"开始下载 {DisplayName} ……", HintType.Blue)
                Logger.Info($"自定义下载：{Url} → {Target}")
                Dim Request As Net.HttpWebRequest = CType(Net.WebRequest.Create(Url.Trim()), Net.HttpWebRequest)
                Request.Timeout = 60000
                Request.ReadWriteTimeout = 60000
                Request.UserAgent = $"PCL2/{VersionBaseName}"
                Request.AllowAutoRedirect = True
                Using Response As Net.HttpWebResponse = CType(Request.GetResponse(), Net.HttpWebResponse)
                    Dim TotalLen As Long = Response.ContentLength
                    Using InStream As IO.Stream = Response.GetResponseStream()
                        Using OutStream As IO.FileStream = IO.File.Create(Target)
                            Dim Buffer(81919) As Byte
                            Dim Received As Long = 0
                            Dim LastReport As Long = 0
                            Do
                                Dim ReadCount As Integer = InStream.Read(Buffer, 0, Buffer.Length)
                                If ReadCount <= 0 Then Exit Do
                                OutStream.Write(Buffer, 0, ReadCount)
                                Received += ReadCount
                                '每 500KB 汇报一次进度
                                If Received - LastReport > 512000 Then
                                    LastReport = Received
                                    Dim Percent As String = If(TotalLen > 0, $"，已下载 {Received * 100 \ TotalLen}%", "")
                                    Hint($"正在下载 {DisplayName}{Percent} ……", HintType.Blue)
                                End If
                            Loop
                        End Using
                    End Using
                    Logger.Info($"自定义下载完成：{Target}（{StringUtils.FormatByteSize(New FileInfo(Target).Length)}）")
                    Hint($"下载完成：{DisplayName}", HintType.Green)
                    RunInUi(Sub() OpenExplorer(Folder & "\"))
                End Using
            Catch ex As Exception
                Logger.Error(ex, $"自定义下载失败：{Url}")
                Hint("下载失败：" & ex.Message, HintType.Red)
                Try
                    If File.Exists(Target) Then File.Delete(Target)
                Catch
                End Try
            End Try
        End Sub)
    End Sub

    '==================================================================
    ' 页面事件
    '==================================================================

    Private Sub PageOtherTest_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        PanBack.ScrollToHome()
        '初始化一句随机留言
        LabRandomHint.Text = GetRandomCave()
    End Sub

    Private Sub BtnJrrp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnJrrp.Click
        Jrrp(ShowHint:=True)
        Try
            LabJrrpResult.Text = $"你的人品值是 {CalcJrrp()} / 100。{vbCrLf}{JrrpComment(CalcJrrp())}"
        Catch ex As Exception
            Logger.Warn(ex, "刷新今日人品显示失败")
        End Try
    End Sub

    Private Sub BtnMemoryOptimize_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnMemoryOptimize.Click
        LabMemoryResult.Text = "正在优化中，请稍候……"
        BtnMemoryOptimize.IsEnabled = False
        RunInThread(Sub()
            Dim Result = MemoryOptimize(ShowHint:=True)
            RunInUi(Sub()
                LabMemoryResult.Text = Result.Message
                BtnMemoryOptimize.IsEnabled = True
            End Sub)
        End Sub)
    End Sub

    Private Sub BtnRubbishClear_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnRubbishClear.Click
        RubbishClear()
        LabRubbishResult.Text = "清理完成。"
    End Sub

    Private Sub BtnCustomDownload_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnCustomDownload.Click
        StartCustomDownload(TextCustomUrl.Text, TextCustomPath.Text)
    End Sub

    ''' <summary>
    ''' 选择保存位置。
    ''' </summary>
    Private Sub BtnBrowsePath_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnBrowsePath.Click
        Try
            Dim Dialog As New Microsoft.Win32.SaveFileDialog() With {
                .Title = "选择保存位置",
                .Filter = "所有文件|*.*",
                .FileName = GetFileNameFromUrl(TextCustomUrl.Text)
            }
            If Dialog.ShowDialog(FrmMain) = True Then TextCustomPath.Text = Dialog.FileName
        Catch ex As Exception
            Logger.Warn(ex, "打开保存对话框失败")
            Hint("打开保存对话框失败：" & ex.Message, HintType.Red)
        End Try
    End Sub

    ''' <summary>
    ''' 从网址推断文件名，失败时返回默认值。
    ''' </summary>
    Private Shared Function GetFileNameFromUrl(Url As String) As String
        Try
            Dim U As New Uri(Url.Trim())
            Dim Name As String = Path.GetFileName(U.LocalPath)
            If Not String.IsNullOrWhiteSpace(Name) Then Return Name
        Catch
        End Try
        Return "download.bin"
    End Function

    Private Sub BtnRandomHint_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnRandomHint.Click
        LabRandomHint.Text = GetRandomCave()
    End Sub

End Class
