''' <summary>
''' 镜像源自动测速与排序模块。
''' 原理：对所有候选镜像站并发发起 HEAD/Range 探测请求，按首字节延迟排序，
''' 把最快的镜像放到下载源列表最前面，从而绕开已经挂掉的固定镜像域名。
''' </summary>
Public Module ModMirror

    ''' <summary>
    ''' 单个镜像站的探测配置。
    ''' </summary>
    Public Class MirrorProbe
        ''' <summary>
        ''' 镜像站显示名称（仅用于日志展示，**不可**当作网址使用）。
        ''' </summary>
        Public Property Name As String
        ''' <summary>
        ''' 镜像站基址，如 https://bmclapi.bangbang93.com。
        ''' 拼装真实下载地址时必须用这个字段，不能用 Name。
        ''' </summary>
        Public Property Base As String
        ''' <summary>
        ''' 该源支持的路径类别。
        ''' </summary>
        Public Property Kind As MirrorKind
        ''' <summary>
        ''' 用于测速的探测地址。
        ''' </summary>
        Public Property ProbeUrl As String
        ''' <summary>
        ''' 测速得到的毫秒延迟，-1 表示不可用。
        ''' </summary>
        Public Property Latency As Long = -1
        ''' <summary>
        ''' 实测下载速度（KB/s），-1 表示未测或失败。
        ''' 这是选源的**主要依据**：首字节快不等于下载快，
        ''' 有些镜像首字节 50ms 但带宽只有 10KB/s，用延迟排序会被坑。
        ''' </summary>
        Public Property Throughput As Long = -1
        ''' <summary>
        ''' 综合得分，越大越好。用于最终排序。
        ''' 权重设计：带宽是决定下载快慢的主因（占 90%），延迟只作参考（占 10%）。
        ''' 未测出带宽的源一律给很低的分，排在已测出带宽的源之后。
        ''' </summary>
        Public ReadOnly Property Score As Double
            Get
                If Latency < 0 Then Return -1
                '有有效实测带宽：带宽主导
                If Throughput > 0 Then
                    Return Throughput * 0.9 + (1000.0 / Math.Max(Latency, 1)) * 100
                End If
                '没测出带宽（多半是无效源或太慢）：给很低的分数，排到后面
                Return 0.1 + (1000.0 / Math.Max(Latency, 1))
            End Get
        End Property
        ''' <summary>
        ''' 探测是否成功。
        ''' </summary>
        Public ReadOnly Property IsAlive As Boolean
            Get
                Return Latency >= 0
            End Get
        End Property
        Public Sub New(name As String, base_ As String, kind_ As MirrorKind, probeUrl As String)
            Me.Name = name
            Me.Base = base_
            Me.Kind = kind_
            Me.ProbeUrl = probeUrl
        End Sub
        ''' <summary>
        ''' 兼容旧构造：只有名称与探测地址时，从探测地址反推基址。
        ''' </summary>
        Public Sub New(name As String, probeUrl As String)
            Me.Name = name
            Me.ProbeUrl = probeUrl
            Me.Base = probeUrl
            Me.Kind = MirrorKind.Meta
        End Sub
        Public Overrides Function ToString() As String
            Return $"{Name}（{Base}）"
        End Function
    End Class

    ''' <summary>
    ''' 镜像源的「路径类别」。
    ''' 不同镜像站支持的文件结构不同：有的只镜像 assets，有的只镜像 maven，
    ''' 所以必须按类别分别探测，否则会把能用的站误判为不可用。
    ''' </summary>
    Public Enum MirrorKind
        ''' <summary>版本列表 / 版本 JSON（launchermeta、piston-meta）。</summary>
        Meta = 0
        ''' <summary>客户端 jar 等对象文件（piston-data、objects）。</summary>
        Objects = 1
        ''' <summary>支持库与 Loader 的 Maven 仓库。</summary>
        Maven = 2
        ''' <summary>游戏资源文件（assets）。</summary>
        Assets = 3
    End Enum

    ''' <summary>
    ''' 完整的候选镜像源定义：一个基址 + 它支持的路径类别。
    ''' 同一个站可能出现在多个类别里，从而得到多路可用备份。
    ''' </summary>
    Public ReadOnly Property MirrorSourceList As List(Of (Base As String, Kind As MirrorKind, Name As String))
        Get
            Dim Result As New List(Of (Base As String, Kind As MirrorKind, Name As String))

            '—— 版本列表 / 版本 JSON ——
            Result.Add(("https://launchermeta.mojang.com", MirrorKind.Meta, "Mojang 官方 launchermeta"))
            Result.Add(("https://piston-meta.mojang.com", MirrorKind.Meta, "Mojang 官方 piston-meta"))
            Result.Add(("https://bmclapi2.bangbang93.com", MirrorKind.Meta, "BMCLAPI 主站"))
            Result.Add(("https://bmclapi.bangbang93.com", MirrorKind.Meta, "BMCLAPI 备用"))
            Result.Add(("https://mirror.hkmc.online", MirrorKind.Meta, "HMCL 光速镜像"))
            Result.Add(("https://bsmirror.hkmc.online", MirrorKind.Meta, "HMCL 光速镜像 主线路"))

            '—— 客户端 jar / 对象文件 ——
            Result.Add(("https://piston-data.mojang.com", MirrorKind.Objects, "Mojang 官方 piston-data"))
            Result.Add(("https://bmclapi2.bangbang93.com", MirrorKind.Objects, "BMCLAPI 主站"))
            Result.Add(("https://bmclapi.bangbang93.com", MirrorKind.Objects, "BMCLAPI 备用"))

            '—— Maven 支持库 ——
            Result.Add(("https://libraries.minecraft.net", MirrorKind.Maven, "Mojang 官方 libraries"))
            Result.Add(("https://bmclapi2.bangbang93.com/maven", MirrorKind.Maven, "BMCLAPI maven"))
            Result.Add(("https://bmclapi.bangbang93.com/maven", MirrorKind.Maven, "BMCLAPI maven 备用"))
            Result.Add(("https://bmclapi2.bangbang93.com/libraries", MirrorKind.Maven, "BMCLAPI libraries"))
            Result.Add(("https://mirror.hkmc.online/maven", MirrorKind.Maven, "HMCL 光速镜像 maven"))

            '—— 游戏资源 ——
            Result.Add(("https://resources.download.minecraft.net", MirrorKind.Assets, "Mojang 官方 resources"))
            Result.Add(("https://bmclapi2.bangbang93.com/assets", MirrorKind.Assets, "BMCLAPI assets"))
            Result.Add(("https://bmclapi.bangbang93.com/assets", MirrorKind.Assets, "BMCLAPI assets 备用"))
            Result.Add(("https://mirror.hkmc.online/assets", MirrorKind.Assets, "HMCL 光速镜像 assets"))

            '—— 用户自定义镜像（自动识别为通用站，所有类别都加一路）——
            Dim Extra As String = Settings.Get(Of String)("ToolMirrorCustomBase")
            If Not String.IsNullOrWhiteSpace(Extra) Then
                For Each Line In Extra.Split({ControlChars.Cr, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
                    Dim Item As String = Line.Trim().TrimEnd("/"c)
                    If Not Item.StartsWithF("http") Then Continue For
                    Result.Insert(0, (Item, MirrorKind.Meta, $"自定义 {Item}"))
                    Result.Insert(1, (Item, MirrorKind.Objects, $"自定义 {Item}"))
                    Result.Insert(2, (Item & "/maven", MirrorKind.Maven, $"自定义 {Item} maven"))
                    Result.Insert(3, (Item & "/assets", MirrorKind.Assets, $"自定义 {Item} assets"))
                Next
            End If
            Return Result
        End Get
    End Property

    ''' <summary>
    ''' 所有候选镜像的基址（去重），用于日志展示与兼容旧调用。
    ''' </summary>
    Public ReadOnly Property MirrorBaseList As List(Of String)
        Get
            Return MirrorSourceList.Select(Function(s) s.Base).Distinct().ToList()
        End Get
    End Property

    ''' <summary>
    ''' 该基址归属的路径类别（用于挑选探测文件）。
    ''' </summary>
    Private Function MirrorKindOf(Base As String) As MirrorKind
        If Base.Contains("/assets") OrElse Base.Contains("resources.download.minecraft.net") Then Return MirrorKind.Assets
        If Base.Contains("/maven") OrElse Base.Contains("/libraries") OrElse Base.Contains("libraries.minecraft.net") Then Return MirrorKind.Maven
        If Base.Contains("piston-data") Then Return MirrorKind.Objects
        Return MirrorKind.Meta
    End Function

    ''' <summary>
    ''' 公共 DNS 列表，用于在系统 DNS（如代理软件的本地解析）失效时兜底解析。
    ''' </summary>
    Private ReadOnly Property PublicDnsList As List(Of String)
        Get
            Return New List(Of String) From {"223.5.5.5", "119.29.29.29", "114.114.114.114", "1.1.1.1"}
        End Get
    End Property

    Private _DnsCache As New ConcurrentDictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    ''' <summary>
    ''' 解析域名，系统 DNS 失败时自动改用公共 DNS。
    ''' 为什么需要：部分代理软件会把系统 DNS 指向 127.0.0.1 并屏蔽特定域名，
    ''' 导致镜像站「解析失败」，但镜像服务本身完全正常。改用公共 DNS 即可拿到真实 IP。
    ''' </summary>
    Public Function MirrorResolveHost(Host As String) As String
        Dim Cached As String = Nothing
        If _DnsCache.TryGetValue(Host, Cached) AndAlso Not String.IsNullOrEmpty(Cached) Then Return Cached
        '先用公共 DNS 查询（快且不受代理软件干扰），4 个公共 DNS 并发取最先返回的
        Dim Bag As New ConcurrentBag(Of String)
        Dim DnsTasks As New List(Of Threading.Tasks.Task)
        For Each DnsServer As String In PublicDnsList
            Dim LocalDns As String = DnsServer
            DnsTasks.Add(Threading.Tasks.Task.Run(
                Sub()
                    Try
                        Dim Found As String = MirrorDnsQueryUdp(Host, LocalDns)
                        If Not String.IsNullOrEmpty(Found) Then Bag.Add(Found)
                    Catch
                    End Try
                End Sub))
        Next
        Try
            Threading.Tasks.Task.WaitAll(DnsTasks.ToArray(), 4000)
        Catch
        End Try
        If Bag.Count > 0 Then
            Dim S As String = Bag.First()
            _DnsCache(Host) = S
            Return S
        End If
        '公共 DNS 都没结果时，退回系统 DNS
        Try
            Dim Ips = Net.Dns.GetHostAddresses(Host)
            Dim V4 = Ips.FirstOrDefault(Function(ip) ip.AddressFamily = Net.Sockets.AddressFamily.InterNetwork)
            If V4 IsNot Nothing Then
                Dim S As String = V4.ToString()
                _DnsCache(Host) = S
                Return S
            End If
        Catch
        End Try
        Return Nothing
    End Function

    ''' <summary>
    ''' 旧实现保留：系统 DNS 优先的解析（未被主流程调用）。
    ''' </summary>
    Private Function MirrorResolveHostBySystem(Host As String) As String
        Dim Cached As String = Nothing
        If _DnsCache.TryGetValue(Host, Cached) AndAlso Not String.IsNullOrEmpty(Cached) Then Return Cached
        '先用系统 DNS
        Try
            Dim Ips = Net.Dns.GetHostAddresses(Host)
            Dim V4 = Ips.FirstOrDefault(Function(ip) ip.AddressFamily = Net.Sockets.AddressFamily.InterNetwork)
            If V4 IsNot Nothing Then
                Dim S As String = V4.ToString()
                _DnsCache(Host) = S
                Return S
            End If
        Catch
        End Try
        '系统 DNS 失败，改用公共 DNS 直接查询
        For Each DnsServer As String In PublicDnsList
            Try
                Dim Found As String = MirrorDnsQueryUdp(Host, DnsServer)
                If Not String.IsNullOrEmpty(Found) Then
                    Logger.Info($"系统 DNS 解析 {Host} 失败，已通过公共 DNS {DnsServer} 解析为 {Found}")
                    _DnsCache(Host) = Found
                    Return Found
                End If
            Catch
            End Try
        Next
        Return Nothing
    End Function

    ''' <summary>
    ''' 通过 UDP 向指定 DNS 服务器查询 A 记录，返回第一个 IPv4 地址。
    ''' </summary>
    Private Function MirrorDnsQueryUdp(Host As String, DnsServer As String) As String
        Dim Query As New List(Of Byte)
        Query.AddRange({CByte(0), CByte(1)})          ' 事务 ID
        Query.AddRange({CByte(&H1), CByte(0)})        ' 标准查询，递归
        Query.AddRange({CByte(0), CByte(1)})          ' QDCOUNT = 1
        Query.AddRange({CByte(0), CByte(0)})          ' ANCOUNT = 0
        Query.AddRange({CByte(0), CByte(0)})          ' NSCOUNT = 0
        Query.AddRange({CByte(0), CByte(0)})          ' ARCOUNT = 0
        For Each Part As String In Host.Split("."c)
            Dim Bytes As Byte() = Text.Encoding.ASCII.GetBytes(Part)
            Query.Add(CByte(Bytes.Length))
            Query.AddRange(Bytes)
        Next
        Query.Add(0)                                   ' 域名结束
        Query.AddRange({CByte(0), CByte(1)})          ' TYPE = A
        Query.AddRange({CByte(0), CByte(1)})          ' CLASS = IN
        Using Client As New Net.Sockets.UdpClient()
            Client.Client.ReceiveTimeout = 3000
            Dim Packet As Byte() = Query.ToArray()
            Client.Send(Packet, Packet.Length, DnsServer, 53)
            Dim Remote As New Net.IPEndPoint(Net.IPAddress.Any, 0)
            Dim Response As Byte() = Client.Receive(Remote)
            If Response Is Nothing OrElse Response.Length < 16 Then Return Nothing
            '跳过问题段
            Dim Pos As Integer = 12
            Do While Pos < Response.Length AndAlso Response(Pos) <> 0
                Pos += Response(Pos) + 1
            Loop
            Pos += 5                                   ' 结束符 + QTYPE + QCLASS
            Dim AnswerCount As Integer = (Response(6) << 8) Or Response(7)
            For i As Integer = 1 To AnswerCount
                If Pos + 12 > Response.Length Then Exit For
                '跳过名称（可能是压缩指针）
                If (Response(Pos) And &HC0) = &HC0 Then
                    Pos += 2
                Else
                    Do While Pos < Response.Length AndAlso Response(Pos) <> 0
                        Pos += Response(Pos) + 1
                    Loop
                    Pos += 1
                End If
                If Pos + 10 > Response.Length Then Exit For
                Dim Type As Integer = (Response(Pos) << 8) Or Response(Pos + 1)
                Dim DataLen As Integer = (Response(Pos + 8) << 8) Or Response(Pos + 9)
                Pos += 10
                If Type = 1 AndAlso DataLen = 4 AndAlso Pos + 4 <= Response.Length Then
                    Return $"{Response(Pos)}.{Response(Pos + 1)}.{Response(Pos + 2)}.{Response(Pos + 3)}"
                End If
                Pos += DataLen
            Next
            Return Nothing
        End Using
    End Function

    ''' <summary>
    ''' 根据镜像基址与其路径类别，返回一个「该站真实存在」的探测地址。
    ''' 关键点：assets / maven / objects 类站点不提供 version_manifest.json，
    ''' 必须用各类型的真实文件路径探测，否则会被误判为不可用。
    ''' </summary>
    Private Function MirrorProbeUrlFor(Base As String, Kind As MirrorKind) As String
        Dim Root As String = Base.TrimEnd("/"c)
        Select Case Kind
            Case MirrorKind.Assets
                '资源：用 1.20.1 资源索引里真实存在的小对象探测（延迟用）
                Return Root & "/b6/b62ca8ec10d07e6bf5ac8dae0c8c1d2e6a1e3356"
            Case MirrorKind.Maven
                '支持库：用小体积的官方 jna 库探测
                Return Root & "/net/java/dev/jna/jna/5.9.0/jna-5.9.0.jar"
            Case MirrorKind.Objects
                '对象存储：用官方 client.jar 的对象路径探测（23MB，适合测带宽）
                Return Root & "/v1/objects/0c3ec587af28e5a785c0b4a7b8a30f9a8f78f838/client.jar"
            Case Else
                '版本列表：直接探测 manifest
                Return Root & "/mc/game/version_manifest.json"
        End Select
    End Function

    ''' <summary>
    ''' 返回该源用于「带宽实测」的地址。
    ''' 与延迟探测分开：带宽需要足够大的文件才能测准。
    ''' - Objects / Meta 用 23MB 的官方 client.jar（各大镜像都有）
    ''' - Maven 用体积较大的 guava（约 3MB）
    ''' - Assets 用多个资源对象（资源本身都很小，用较大的 sound 文件）
    ''' </summary>
    Private Function MirrorBandwidthUrlFor(Base As String, Kind As MirrorKind) As String
        Dim Root As String = Base.TrimEnd("/"c)
        Select Case Kind
            Case MirrorKind.Maven
                'guava 约 3MB，足够测带宽
                Return Root & "/com/google/guava/guava/31.0.1-jre/guava-31.0.1-jre.jar"
            Case MirrorKind.Assets
                '用一个较大的音效资源（约 200KB）
                Return Root & "/b6/b62ca8ec10d07e6bf5ac8dae0c8c1d2e6a1e3356"
            Case Else
                'Meta / Objects 都回退到 client.jar；若该站不支持 objects，
                '实测读不到足够字节会自动判为无效，不会造成误判。
                Return Root & "/v1/objects/0c3ec587af28e5a785c0b4a7b8a30f9a8f78f838/client.jar"
        End Select
    End Function

    ''' <summary>
    ''' 兼容旧签名的探测地址推导。
    ''' </summary>
    Private Function MirrorProbePath(Base As String) As String
        Return MirrorProbeUrlFor(Base, MirrorKindOf(Base))
    End Function

    ''' <summary>
    ''' 候选镜像的测速探测地址（按类别自动选择正确的探测文件）。
    ''' </summary>
    Private Function MirrorProbeUrls() As List(Of MirrorProbe)
        Dim Result As New List(Of MirrorProbe)
        For Each Src In MirrorSourceList
            Dim ProbeUrl As String = MirrorProbeUrlFor(Src.Base, Src.Kind)
            Result.Add(New MirrorProbe(Src.Name, Src.Base, Src.Kind, ProbeUrl))
        Next
        Return Result
    End Function

    Private _MirrorCache As List(Of MirrorProbe) = Nothing
    Private _MirrorCacheTime As Long = 0
    Private ReadOnly _MirrorLock As New Object()
    ''' <summary>
    ''' 测速结果的缓存有效期（毫秒）。
    ''' 取 3 分钟：既保证「下载前用的是较新的结果」，又避免频繁测速
    ''' 把 BMCLAPI 这类有限流的站惹毛（实测高频请求会返回 429 并被限速到 2KB/s）。
    ''' </summary>
    Private Const MirrorCacheLifetime As Long = 180000
    ''' <summary>
    ''' 缓存的测速结果排序列表，包含所有镜像（含不可用项）。
    ''' </summary>
    Public ReadOnly Property MirrorSorted As List(Of MirrorProbe)
        Get
            SyncLock _MirrorLock
                Return If(_MirrorCache, New List(Of MirrorProbe))
            End SyncLock
        End Get
    End Property

    ''' <summary>
    ''' 对所有候选镜像并发测速，返回按延迟升序的列表。
    ''' 结果只缓存 1 分钟；每次下载动作都会以 ForceRefresh 调用，确保用的是当下最快的源。
    ''' </summary>
    Public Function MirrorMeasure(Optional ForceRefresh As Boolean = False) As List(Of MirrorProbe)
        SyncLock _MirrorLock
            Dim Now As Long = GetTimeMs()
            If Not ForceRefresh AndAlso _MirrorCache IsNot Nothing AndAlso Now - _MirrorCacheTime < MirrorCacheLifetime Then Return _MirrorCache
        End SyncLock
        Dim Probes As List(Of MirrorProbe) = MirrorProbeUrls()
        '第一步：批量预解析所有域名并写入缓存。
        '为什么需要：并发测速时若每个源都各自去撞系统 DNS，在 Clash 这类软件下会随机失败，
        '导致同一站不同路径有的通有的不通。先并发解析一次并缓存，后续都用缓存结果走 IP 直连。
        Dim Hosts As New List(Of String)
        For Each Probe As MirrorProbe In Probes
            Try
                Dim U As New Uri(Probe.ProbeUrl)
                If Not Hosts.Contains(U.Host) Then Hosts.Add(U.Host)
            Catch
            End Try
        Next
        Dim DnsTasks As New List(Of Threading.Tasks.Task)
        For Each H As String In Hosts
            Dim LocalHost As String = H
            '已有缓存的跳过
            If _DnsCache.ContainsKey(LocalHost) Then Continue For
            DnsTasks.Add(Threading.Tasks.Task.Run(
                Sub()
                    Try
                        MirrorResolveHost(LocalHost)
                    Catch
                    End Try
                End Sub))
        Next
        If DnsTasks.Count > 0 Then
            Try
                Threading.Tasks.Task.WaitAll(DnsTasks.ToArray(), 6000)
            Catch
            End Try
        End If
        '并发测速
        '关键：不能用 Task.WaitAll 的整体超时来掐断，否则源多了之后还没测完就被判「全部不可用」。
        '每个 MirrorPing 内部已有自己的单源超时，这里只做「等待全部完成」，并额外只在极长的时间后放弃，避免卡死。
        Dim Tasks As New List(Of Threading.Tasks.Task)
        For Each Probe As MirrorProbe In Probes
            Dim LocalProbe As MirrorProbe = Probe
            Tasks.Add(Threading.Tasks.Task.Run(
                Sub()
                    Try
                        LocalProbe.Latency = MirrorPing(LocalProbe.ProbeUrl)
                    Catch
                        LocalProbe.Latency = -1
                    End Try
                End Sub))
        Next
        Try
            Dim AllDone As Boolean = Threading.Tasks.Task.WaitAll(Tasks.ToArray(), 60000)
            If Not AllDone Then
                '极少数情况下仍有源卡住：只把还没跑完的标为不可用，已完成的保留结果
                For i = 0 To Probes.Count - 1
                    If Tasks(i).IsCompleted = False Then Probes(i).Latency = -1
                Next
                Logger.Warn("镜像测速等待超时（60s），部分源未完成，已采用已完成的结果")
            End If
        Catch ex As Exception
            Logger.Warn(ex, "镜像测速等待出错，采用已获得的结果")
        End Try

        '第二阶段：对「延迟合格」的源实测下载带宽。
        '为什么必须做：延迟只反映首字节快慢，很多镜像首字节 50ms 但带宽只有十几 KB/s。
        '若只按延迟排序，会把这种「秒连但龟速」的站排到最前面，导致下载极慢。
        '策略：按延迟取前若干名（最多 8 个）做带宽实测，避免对 BMCLAPI 这类有限流的站
        '      发起过多请求而触发 429。
        Dim BandwidthTargets As List(Of MirrorProbe) = Probes.
            Where(Function(p) p.IsAlive).
            OrderBy(Function(p) p.Latency).
            Take(8).ToList()
        If BandwidthTargets.Count > 0 Then
            Dim BwTasks As New List(Of Threading.Tasks.Task)
            For Each Probe As MirrorProbe In BandwidthTargets
                Dim LocalProbe As MirrorProbe = Probe
                BwTasks.Add(Threading.Tasks.Task.Run(
                    Sub()
                        Try
                            '用专门的大文件测带宽，而不是延迟探测用的小文件
                            Dim BwUrl As String = MirrorBandwidthUrlFor(LocalProbe.Base, LocalProbe.Kind)
                            LocalProbe.Throughput = MirrorBenchmark(BwUrl, 262144, 8000)
                            '若该路径在本站不存在，退回用延迟探测地址再试一次
                            If LocalProbe.Throughput < 0 Then
                                LocalProbe.Throughput = MirrorBenchmark(LocalProbe.ProbeUrl, 131072, 6000)
                            End If
                        Catch
                            LocalProbe.Throughput = -1
                        End Try
                    End Sub))
            Next
            Try
                Threading.Tasks.Task.WaitAll(BwTasks.ToArray(), 20000)
            Catch
            End Try
            '对 BMCLAPI 系源做适量间隔，减少触发 429 的概率
            For Each Probe As MirrorProbe In BandwidthTargets
                If Probe.Base IsNot Nothing AndAlso Probe.Base.Contains("bmclapi") Then
                    Thread.Sleep(150)
                End If
            Next
        End If

        '综合排序：有实测带宽的按带宽得分，没有的按延迟
        Dim Sorted As List(Of MirrorProbe) = Probes.
            OrderByDescending(Function(p) p.Score).
            ToList()
        SyncLock _MirrorLock
            _MirrorCache = Sorted
            _MirrorCacheTime = GetTimeMs()
        End SyncLock
        '日志
        Dim AliveCount As Integer = 0
        For Each Probe As MirrorProbe In Sorted
            If Probe.IsAlive Then
                AliveCount += 1
                Dim SpeedText As String = If(Probe.Throughput > 0, $"，{Probe.Throughput} KB/s", "，带宽未测")
                Logger.Info($"镜像测速：{Probe.Name} 延迟 {Probe.Latency}ms{SpeedText}")
            Else
                Logger.Info($"镜像测速：{Probe.Name} 不可用")
            End If
        Next
        Return Sorted
    End Function

    ''' <summary>
    ''' 实测一个地址的下载带宽，返回 KB/s；失败返回 -1。
    ''' 只读取最多 MaxBytes 字节就断开，避免测速本身占用大量流量。
    ''' 关键：断流（读到一半连接被掐）时按「已读字节/已用时间」计算，
    '''       这正好能识别出「只给 44KB 就掐断」的限流站。
    ''' </summary>
    Public Function MirrorBenchmark(Url As String, Optional MaxBytes As Integer = 262144, Optional TimeoutMs As Integer = 8000) As Long
        Try
            Dim U As New Uri(Url)
            '优先使用预解析的 IP，绕开 DNS 抽风
            Dim UseUrl As String = Url
            Dim Cached As String = Nothing
            If _DnsCache.TryGetValue(U.Host, Cached) AndAlso Not String.IsNullOrEmpty(Cached) Then
                UseUrl = New UriBuilder(U) With {.Host = Cached}.Uri.ToString()
            End If
            Dim Request As Net.HttpWebRequest = CType(Net.WebRequest.Create(UseUrl), Net.HttpWebRequest)
            Request.Method = "GET"
            Request.Timeout = TimeoutMs
            Request.ReadWriteTimeout = TimeoutMs
            Request.UserAgent = "PCL2/2.99 MirrorBench"
            Request.AllowAutoRedirect = True
            If UseUrl <> Url Then
                Request.Host = U.Host
                Request.ServerCertificateValidationCallback = Function(sender, cert, chain, errors) True
            End If
            Request.AddRange(0, MaxBytes - 1)
            Dim Watch As New Diagnostics.Stopwatch()
            Watch.Start()
            Using Response As Net.HttpWebResponse = CType(Request.GetResponse(), Net.HttpWebResponse)
                Dim Code As Integer = CInt(Response.StatusCode)
                If Code < 200 OrElse Code >= 400 Then Return -1
                Dim Stream As IO.Stream = Response.GetResponseStream()
                Dim Buffer(16383) As Byte
                Dim Total As Long = 0
                Do While Total < MaxBytes
                    Dim Read As Integer = Stream.Read(Buffer, 0, Buffer.Length)
                    If Read <= 0 Then Exit Do
                    Total += Read
                    If Watch.ElapsedMilliseconds >= TimeoutMs Then Exit Do
                Loop
                Watch.Stop()
                Stream.Close()
                '可靠性要求：必须真的读到足够多的字节，才认为是有效带宽样本。
                '为什么：有些站对 Range 请求返回 200 + 一个很小的 HTML 错误页，
                '       若只按「读到的字节 / 极短时间」计算，会得出「几千 KB/s」的假高速，
                '       排序时把这种无效源顶到最前面，实际下载立刻失败。
                '门槛取 MaxBytes 的一半，且不少于 32KB。
                Dim MinAccept As Long = Math.Max(32768, MaxBytes \ 2)
                If Total < MinAccept Then Return -1
                Dim Seconds As Double = Math.Max(Watch.ElapsedMilliseconds / 1000.0, 0.001)
                Dim Kbps As Long = CLng(Total / 1024.0 / Seconds)
                '再做一个合理性上限：超过 50MB/s 基本可判定为异常（本地缓存或错误页），丢弃
                If Kbps > 51200 Then Return -1
                Return Kbps
            End Using
        Catch
            Return -1
        End Try
    End Function

    ''' <summary>
    ''' 探测单个地址的首字节延迟，失败返回 -1。
    ''' 实现要点：
    ''' 1. 优先 HEAD，很多 CDN 对 HEAD 直接返回 4xx；
    ''' 2. HEAD 失败或返回 4xx 时，改用 GET + Range 只读 1 字节，避免下载整个文件；
    ''' 3. 404 视为「该站存在但不含此文件」，由调用方用真实路径避免，这里统一判为失败。
    ''' </summary>
    Public Function MirrorPing(Url As String) As Long
        '若该域名已有预解析的 IP，直接走 IP 直连，绕开 Clash 之类软件对 DNS 的随机拦截
        Try
            Dim U As New Uri(Url)
            Dim Cached As String = Nothing
            If _DnsCache.TryGetValue(U.Host, Cached) AndAlso Not String.IsNullOrEmpty(Cached) Then
                Dim ViaIp As Long = MirrorPingViaIp(Url, "HEAD", 4000)
                If ViaIp >= 0 Then Return ViaIp
                ViaIp = MirrorPingViaIp(Url, "GET", 4000)
                If ViaIp >= 0 Then Return ViaIp
                'IP 直连失败时继续走常规流程兜底
            End If
        Catch
        End Try
        '第一轮：HEAD（快，且不下载内容）
        Dim HeadResult As Long = MirrorPingInternal(Url, "HEAD", 4000)
        If HeadResult >= 0 Then Return HeadResult
        '第二轮：GET + Range 1 字节
        Return MirrorPingInternal(Url, "GET", 4000)
    End Function

    Private Function MirrorPingInternal(Url As String, Method As String, TimeoutMs As Integer) As Long
        'DNS 偶发解析失败在国内网络很常见
        For Attempt As Integer = 1 To 2
            Try
                Dim Request As Net.HttpWebRequest = CType(Net.WebRequest.Create(Url), Net.HttpWebRequest)
                Request.Method = Method
                Request.Timeout = TimeoutMs
                Request.ReadWriteTimeout = TimeoutMs
                Request.UserAgent = "PCL2-MirrorProbe"
                Request.AllowAutoRedirect = True
                If Method = "GET" Then
                    '只读 1 字节，避免测速时把整个文件拉下来
                    Request.AddRange(0, 0)
                End If
                Dim Watch As New Diagnostics.Stopwatch()
                Watch.Start()
                Using Response As Net.HttpWebResponse = CType(Request.GetResponse(), Net.HttpWebResponse)
                    '读 1 字节，确保真正建立了数据通道（仅统计首字节耗时）
                    If Method = "GET" AndAlso Response.GetResponseStream() IsNot Nothing Then
                        Response.GetResponseStream().ReadByte()
                    End If
                    Watch.Stop()
                    Dim Code As Integer = CInt(Response.StatusCode)
                    If Code >= 200 AndAlso Code < 400 Then Return Watch.ElapsedMilliseconds
                    Return -1
                End Using
            Catch ex As Net.WebException
                'DNS 解析失败：交给公共 DNS 兜底，用 IP 直连再试一次
                If ex.Status = Net.WebExceptionStatus.NameResolutionFailure Then
                    Dim DirectResult As Long = MirrorPingViaIp(Url, Method, TimeoutMs)
                    If DirectResult >= 0 Then Return DirectResult
                End If
                If Attempt < 2 Then
                    Continue For
                End If
                Return -1
            Catch
                Return -1
            End Try
        Next
        Return -1
    End Function

    ''' <summary>
    ''' 系统 DNS 失效时的兜底测速：用公共 DNS 解析出 IP，再带 Host 头直连。
    ''' </summary>
    Private Function MirrorPingViaIp(Url As String, Method As String, TimeoutMs As Integer) As Long
        Try
            Dim Uri As New Uri(Url)
            Dim Address As String = MirrorResolveHost(Uri.Host)
            If String.IsNullOrEmpty(Address) Then Return -1
            Dim NewUrl As String = New UriBuilder(Uri) With {.Host = Address}.Uri.ToString()
            Dim Request As Net.HttpWebRequest = CType(Net.WebRequest.Create(NewUrl), Net.HttpWebRequest)
            Request.Method = Method
            Request.Timeout = TimeoutMs
            Request.ReadWriteTimeout = TimeoutMs
            Request.UserAgent = "PCL2-MirrorProbe"
            Request.AllowAutoRedirect = True
            '保留原始 Host 头与 SNI，否则 CDN 会返回错误页面
            Request.Host = Uri.Host
            Request.ServerCertificateValidationCallback = Function(sender, cert, chain, errors) True
            If Method = "GET" Then Request.AddRange(0, 0)
            Dim Watch As New Diagnostics.Stopwatch()
            Watch.Start()
            Using Response As Net.HttpWebResponse = CType(Request.GetResponse(), Net.HttpWebResponse)
                If Method = "GET" AndAlso Response.GetResponseStream() IsNot Nothing Then
                    Response.GetResponseStream().ReadByte()
                End If
                Watch.Stop()
                Dim Code As Integer = CInt(Response.StatusCode)
                If Code >= 200 AndAlso Code < 400 Then
                    Logger.Info($"镜像 {Uri.Host} 经公共 DNS 解析为 {Address} 后测速成功：{Watch.ElapsedMilliseconds}ms")
                    Return Watch.ElapsedMilliseconds
                End If
                Return -1
            End Using
        Catch
            Return -1
        End Try
    End Function

    ''' <summary>
    ''' 安装全局 DNS 兜底。
    ''' 原理：挂钩 ServicePointManager 无法直接改 DNS，因此改用「预解析 + 连接时替换」策略：
    ''' 对即将访问的主机，若系统 DNS 解析失败，则用公共 DNS 预取 IP 并写入系统 Hosts 的进程级缓存，
    ''' 从而让 PCL2 原有的所有下载代码（NetTask、WebClient 等）都能正常工作。
    ''' </summary>
    Public Sub MirrorInstallDnsFallback()
        If _DnsFallbackInstalled Then Return
        _DnsFallbackInstalled = True
        Try
            '预热：把已知镜像域名先按公共 DNS 解析一遍并缓存
            Threading.Tasks.Task.Run(
                Sub()
                    For Each Base As String In MirrorBaseList
                        Try
                            Dim Host As String = New Uri(Base).Host
                            MirrorResolveHost(Host)
                        Catch
                        End Try
                    Next
                End Sub)
        Catch ex As Exception
            Logger.Warn(ex, "安装 DNS 兜底失败")
        End Try
    End Sub
    Private _DnsFallbackInstalled As Boolean = False

    ''' <summary>
    ''' 把一个 URL 转换为可用的 URL：若系统 DNS 解析不了，则换成 IP 直连并保留 Host 头。
    ''' 供下载层在捕获到解析失败时调用。
    ''' </summary>
    Public Function MirrorUrlWithDnsFallback(Url As String) As String
        Try
            Dim Uri As New Uri(Url)
            Dim Address As String = MirrorResolveHost(Uri.Host)
            If String.IsNullOrEmpty(Address) Then Return Url
            Return New UriBuilder(Uri) With {.Host = Address}.Uri.ToString()
        Catch
            Return Url
        End Try
    End Function

    ''' <summary>
    ''' 把官方地址改写为指定镜像基址的地址。
    ''' 镜像站要求保持 Minecraft 官方的路径结构，因此只替换主机名。
    ''' </summary>
    Public Function MirrorRewrite(Original As String, MirrorBase As String) As String
        If String.IsNullOrEmpty(Original) OrElse String.IsNullOrEmpty(MirrorBase) Then Return Original
        Dim Result As String = Original
        '按主机名逐项替换，保持路径不变
        For Each Host As String In {
            "https://launchermeta.mojang.com",
            "https://piston-meta.mojang.com",
            "https://piston-data.mojang.com",
            "https://launcher.mojang.com",
            "https://libraries.minecraft.net",
            "https://resources.download.minecraft.net",
            "http://resources.download.minecraft.net"
        }
            If Result.StartsWithF(Host) Then
                Return MirrorBase.TrimEnd("/"c) & Result.Substring(Host.Length)
            End If
        Next
        Return Result
    End Function

    ''' <summary>
    ''' 判断一个官方文件地址属于哪一类，决定该用哪些候选源。
    ''' </summary>
    Public Function MirrorKindOfUrl(Url As String) As MirrorKind
        If Url Is Nothing Then Return MirrorKind.Meta
        If Url.Contains("resources.download.minecraft.net") Then Return MirrorKind.Assets
        If Url.Contains("libraries.minecraft.net") Then Return MirrorKind.Maven
        If Url.Contains("piston-data") Then Return MirrorKind.Objects
        Return MirrorKind.Meta
    End Function

    ''' <summary>
    ''' 返回一个地址的全部可用候选（按实测带宽排序），原始地址放在合适位置。
    ''' 这是「下载前选最快源」的核心入口。
    ''' 实现要点：
    ''' 1. 先判断该地址属于哪类文件，只挑支持该类的候选源，避免白试 404；
    ''' 2. 用缓存的测速结果（含实测带宽）排序，3 分钟内有效；
    '''    —— 不每次强制测速：高频请求会被 BMCLAPI 判 429 并限速到 2KB/s，反而更慢；
    ''' 3. 同一个站若在多个类别里出现，会自动产生多路备份。
    ''' </summary>
    Public Function MirrorCandidates(Original As String, Optional IncludeOfficial As Boolean = True) As List(Of String)
        Dim Result As New List(Of String)
        If String.IsNullOrEmpty(Original) Then Return Result
        '地址归一化
        Original = Original.Replace("http://resources.download.minecraft.net", "https://resources.download.minecraft.net")
        Dim Kind As MirrorKind = MirrorKindOfUrl(Original)
        '取测速结果（命中 3 分钟缓存时不会重新发请求）
        Dim Ranked As List(Of MirrorProbe) = MirrorMeasure()
        '建立 名称→(基址,类别) 的映射，便于按类别筛选
        Dim SourceMap As New Dictionary(Of String, (Base As String, Kind As MirrorKind))(StringComparer.Ordinal)
        For Each Src In MirrorSourceList
            If Src.Kind = Kind AndAlso Not SourceMap.ContainsKey(Src.Name) Then SourceMap(Src.Name) = (Src.Base, Src.Kind)
        Next
        For Each Probe As MirrorProbe In Ranked
            If Not Probe.IsAlive Then Continue For
            Dim Src As (Base As String, Kind As MirrorKind) = (Nothing, Kind)
            If Not SourceMap.TryGetValue(Probe.Name, Src) Then Continue For
            Dim Base As String = Src.Base
            Dim IsOfficialHost As Boolean = Base.Contains("mojang.com") OrElse Base.Contains("minecraft.net")
            If IsOfficialHost AndAlso Not IncludeOfficial Then Continue For
            Dim Url As String = MirrorRewrite(Original, Base)
            If Not Result.Contains(Url) Then Result.Add(Url)
        Next
        '兜底：确保原始地址在列表内
        If Not Result.Contains(Original) Then Result.Add(Original)
        Return Result.Distinct().ToList()
    End Function

    ''' <summary>
    ''' 启动时预热测速（后台执行，不阻塞 UI）。
    ''' </summary>
    Public Sub MirrorWarmUp()
        Threading.Tasks.Task.Run(
            Sub()
                Try
                    MirrorMeasure()
                Catch ex As Exception
                    Logger.Warn(ex, "镜像预热测速失败")
                End Try
            End Sub)
    End Sub

    ''' <summary>
    ''' 返回当前可用的、提供 Loader 专属 API 的镜像基址。
    ''' 参数 loader 用于挑选「确实支持该 Loader」的站，避免把请求发到不支持它的站上换来 404。
    ''' 已知情况：
    '''   - NeoForge 的 API 只有 BMCLAPI 系支持（路径 /neoforge/meta/api/maven/...）；
    '''     HMCL 镜像虽快但不提供，必须在选站时排除。
    '''   - OptiFine / Forge / Fabric / LiteLoader 走 BMCLAPI 同构路径。
    ''' </summary>
    Public Function MirrorApiBaseGet(Optional loader As String = "") As String
        Dim Ranked As List(Of MirrorProbe) = MirrorMeasure()
        Dim NameMap As New Dictionary(Of String, String)(StringComparer.Ordinal)
        For Each Src In MirrorSourceList
            If Src.Kind <> MirrorKind.Meta Then Continue For
            If Src.Base.Contains("maven") OrElse Src.Base.Contains("/assets") Then Continue For
            If Not NameMap.ContainsKey(Src.Name) Then NameMap(Src.Name) = Src.Base
        Next
        Dim IsNeoForge As Boolean = loader.ToLower = "neoforge"
        '第一优先：真正支持该 Loader 的镜像（按测速顺序）
        For Each Probe As MirrorProbe In Ranked
            If Not Probe.IsAlive Then Continue For
            Dim Base As String = Nothing
            If Not NameMap.TryGetValue(Probe.Name, Base) Then Continue For
            Dim IsBmcl As Boolean = Base.Contains("bmclapi")
            Dim IsHkmc As Boolean = Base.Contains("hkmc")
            'NeoForge 只认 BMCLAPI
            If IsNeoForge Then
                If IsBmcl Then Return Base.TrimEnd("/"c)
                Continue For
            End If
            '其它 Loader：BMCLAPI 与 HMCL 都支持
            If IsBmcl OrElse IsHkmc Then Return Base.TrimEnd("/"c)
        Next
        '第二优先：NeoForge 场景下没有 BMCLAPI 时，退回官方 nuget.org 风格地址
        If IsNeoForge Then Return "https://maven.neoforged.net"
        '第三优先：任意可用的 Meta 类源
        For Each Probe As MirrorProbe In Ranked
            If Not Probe.IsAlive Then Continue For
            Dim Base As String = Nothing
            If NameMap.TryGetValue(Probe.Name, Base) Then Return Base.TrimEnd("/"c)
        Next
        Return "https://launchermeta.mojang.com"
    End Function

    ''' <summary>
    ''' GitHub 加速代理列表。
    ''' 用于下载 PCL2 自身更新、Forge 安装器等托管在 GitHub 的资源。
    ''' 注意：实测只有部分代理可用，且随时间变化，所以必须现场测速。
    ''' </summary>
    Public ReadOnly Property GitHubProxyList As List(Of (Proxy As String, Name As String))
        Get
            Return New List(Of (Proxy As String, Name As String)) From {
                ("", "GitHub 直连"),
                ("https://gh-proxy.org/", "gh-proxy.org"),
                ("https://ghfast.top/", "ghfast.top"),
                ("https://ghproxy.net/", "ghproxy.net"),
                ("https://gh.llkk.cc/", "gh.llkk.cc"),
                ("https://github.moeyy.xyz/", "moeyy"),
                ("https://hub.gitmirror.com/", "gitmirror"),
                ("https://ghproxy.cc/", "ghproxy.cc"),
                ("https://gh.xxooo.cf/", "xxooo"),
                ("https://ghp.ci/", "ghp.ci"),
                ("https://gh.zwy.one/", "zwy")
            }
        End Get
    End Property

    ''' <summary>
    ''' GitHub 测速用的真实探测资源。
    ''' 为什么不用 favicon：多数加速站只反代 raw/releases/archive，
    ''' 对 favicon 之类的静态站点资源会返回 404 或不响应，导致误判全部不可用。
    ''' 这里用一个体积很小、长期存在的官方仓库归档链接。
    ''' </summary>
    Private Const GitHubProbeTarget As String = "https://github.com/Meloong-Git/PCL/archive/refs/heads/main.zip"

    ''' <summary>
    ''' 对 GitHub 地址生成按测速排序的加速候选。
    ''' 原理：每个代理都是「前缀 + 原始 GitHub 地址」的形式，逐个测速后用最快的。
    ''' </summary>
    Public Function GitHubCandidates(Original As String) As List(Of String)
        Dim Result As New List(Of String)
        If String.IsNullOrEmpty(Original) Then Return Result
        If Not (Original.Contains("github.com") OrElse Original.Contains("githubusercontent.com")) Then
            Result.Add(Original)
            Return Result
        End If
        '命中缓存
        SyncLock _GitHubLock
            Dim Now As Long = GetTimeMs()
            If _GitHubProxyOrder IsNot Nothing AndAlso Now - _GitHubCacheTime < MirrorCacheLifetime Then
                For Each Proxy In _GitHubProxyOrder
                    Result.Add(If(Proxy = "", Original, Proxy & Original))
                Next
                If Not Result.Contains(Original) Then Result.Add(Original)
                Return Result
            End If
        End SyncLock
        '并发测速所有代理（用真实资源探测）
        Dim Bag As New ConcurrentBag(Of (Proxy As String, Latency As Long))
        Dim Tasks As New List(Of Threading.Tasks.Task)
        For Each Item In GitHubProxyList
            Dim LocalProxy As String = Item.Proxy
            Dim ProbeUrl As String = If(LocalProxy = "", GitHubProbeTarget, LocalProxy & GitHubProbeTarget)
            Tasks.Add(Threading.Tasks.Task.Run(
                Sub()
                    Dim Lat As Long = MirrorPing(ProbeUrl)
                    Bag.Add((LocalProxy, Lat))
                End Sub))
        Next
        Try
            Threading.Tasks.Task.WaitAll(Tasks.ToArray(), 15000)
        Catch ex As Exception
            Logger.Warn(ex, "GitHub 加速源测速超时")
        End Try
        '按延迟排序，只保留可用的
        Dim Ordered As List(Of String) = Bag.Where(Function(x) x.Latency >= 0).
            OrderBy(Function(x) x.Latency).
            Select(Function(x) x.Proxy).ToList()
        '直连永远作为最后兜底
        If Not Ordered.Contains("") Then Ordered.Add("")
        For Each Proxy In Ordered
            Dim Url As String = If(Proxy = "", Original, Proxy & Original)
            If Not Result.Contains(Url) Then Result.Add(Url)
        Next
        If Not Result.Contains(Original) Then Result.Add(Original)
        '日志
        For Each Item In Bag.OrderBy(Function(x) If(x.Latency >= 0, x.Latency, Long.MaxValue))
            Dim ShowName As String = GitHubProxyList.FirstOrDefault(Function(p) p.Proxy = Item.Proxy).Name
            If String.IsNullOrEmpty(ShowName) Then ShowName = If(Item.Proxy = "", "直连", Item.Proxy)
            If Item.Latency >= 0 Then
                Logger.Info($"GitHub 加速测速：{ShowName} 延迟 {Item.Latency}ms")
            Else
                Logger.Info($"GitHub 加速测速：{ShowName} 不可用")
            End If
        Next
        SyncLock _GitHubLock
            _GitHubProxyOrder = Ordered
            _GitHubCacheTime = GetTimeMs()
        End SyncLock
        Return Result
    End Function
    Private _GitHubProxyOrder As List(Of String) = Nothing
    Private _GitHubCacheTime As Long = 0
    Private ReadOnly _GitHubLock As New Object()

    ''' <summary>
    ''' 拼接 Loader 专属 API 的完整地址。
    ''' loader 参数用于挑选真正支持该 Loader 的镜像站。
    ''' </summary>
    Public Function MirrorApiUrl(Path As String, Optional loader As String = "") As String
        Return MirrorApiBaseGet(loader).TrimEnd("/"c) & "/" & Path.TrimStart("/"c)
    End Function


    ''' <summary>
    ''' 返回测速最快的官方结构镜像的版本列表地址（Mojang 官方或等价镜像）。
    ''' 注意：必须使用 Probe.Base（真实网址），不能用 Probe.Name（显示名）。
    ''' </summary>
    Public Function MirrorOfficialManifestGet() As String
        Dim Ranked As List(Of MirrorProbe) = MirrorMeasure()
        For Each Probe As MirrorProbe In Ranked
            If Not Probe.IsAlive Then Continue For
            If Probe.Kind <> MirrorKind.Meta Then Continue For
            Dim Base As String = Probe.Base
            If String.IsNullOrEmpty(Base) OrElse Not Base.StartsWithF("http") Then Continue For
            If Base.Contains("launchermeta") Then
                Return Base.TrimEnd("/"c) & "/mc/game/version_manifest.json"
            End If
        Next
        Return MirrorManifestGet()
    End Function

    ''' <summary>
    ''' 返回测速最快的可用镜像的版本列表地址。
    ''' 这一函数替代了原先写死的 bmclapi2 地址，避免镜像宕机后整体不可用。
    ''' 注意：必须使用 Probe.Base（真实网址），不能用 Probe.Name（显示名）。
    ''' </summary>
    Public Function MirrorManifestGet() As String
        Dim Ranked As List(Of MirrorProbe) = MirrorMeasure()
        For Each Probe As MirrorProbe In Ranked
            If Not Probe.IsAlive Then Continue For
            '只挑选 Meta 类（提供版本列表）的源
            If Probe.Kind <> MirrorKind.Meta Then Continue For
            Dim Base As String = Probe.Base
            '防御：基址必须是合法网址，避免把中文显示名拼进 URL
            If String.IsNullOrEmpty(Base) OrElse Not Base.StartsWithF("http") Then Continue For
            If Base.Contains("libraries") OrElse Base.Contains("resources") OrElse Base.Contains("/maven") OrElse Base.Contains("/assets") Then Continue For
            Return Base.TrimEnd("/"c) & "/mc/game/version_manifest.json"
        Next
        '全部不可用时退回官方
        Return "https://launchermeta.mojang.com/mc/game/version_manifest.json"
    End Function

    ''' <summary>
    ''' 按测速顺序依次尝试候选地址，返回第一个成功响应的内容。
    ''' Func 参数用于执行单次请求；全部失败时抛出最后一个异常。
    ''' </summary>
    Public Function MirrorTryGet(Urls As IEnumerable(Of String), Fetcher As Func(Of String, String)) As String
        Dim Errors As New List(Of Exception)
        For Each Url As String In Urls
            Try
                Dim Result As String = Fetcher(Url)
                If Result IsNot Nothing Then Return Result
            Catch ex As Exception
                Errors.Add(New Exception($"镜像 {Url} 请求失败：{ex.Message}", ex))
            End Try
        Next
        Throw New AggregateException("所有候选镜像均请求失败", Errors)
    End Function

End Module
