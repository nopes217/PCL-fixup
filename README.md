# PCL-fixup

> **第三方作品声明**
>
> PCL-fixup 是第三方基于 PCL 独立进行二次创作的产物，不是 PCL 官方版本，与龙腾猫跃（LTCat）以及 Plain Craft Launcher 官方团队没有任何关联。
>
> 上游项目地址：https://github.com/Meloong-Git/PCL
>
> 本项目遵循上游的《PCL 分发有限许可》和《PCL 存储库合理使用指南》，详见仓库根目录的 LICENCE 文件。

这个项目把社区开源版 PCL 缺的东西补了回来，同时把下载速度想办法拉上去。

具体干了两件事。第一件是让原版游戏下载变快，办法是多镜像自动测速、按真实带宽挑源、DNS 出问题时自己兜底、支持分段并发。第二件是把被砍掉的功能恢复回来，包括原版游戏下载入口、百宝箱、微软正版登录。

## 一、改了什么

### （1）多镜像自动选源

内置 18 路候选源，按 Minecraft 的文件类型分成四组，每组单独测速、单独择优。

| 类别 | 源数 | 对应内容 |
|---|---|---|
| Meta | 6 | 版本列表与版本 JSON |
| Objects | 3 | 客户端 jar 等对象文件 |
| Maven | 5 | 游戏支持库 |
| Assets | 4 | 游戏资源文件 |

测速分两步走。第一步测延迟，用 HEAD 请求或者 Range 只读 1 个字节，把已经挂掉的源快速筛掉。第二步对排在前面 8 名做真实带宽测试，直接拉 256KB 算实际 KB/s。

最后排序的时候带宽占九成权重，延迟只占一成。这样能避开那种延迟只有 89ms 但带宽只有 12KB/s 的坑源。以前只看延迟就会踩这种坑。

### （2）下载前动态挑源

每次要下载的时候都会取最近的测速结果来选源，结果缓存 3 分钟。这样既保证用的是当时最快的源，又不会因为测得太频繁把镜像站惹毛。

### （3）DNS 出事自己兜底

有些代理软件会把系统 DNS 指到 127.0.0.1，然后拦掉特定域名。表现出来就是镜像站解析失败，但那个站其实活得好好的。

处理办法有三个。第一，系统 DNS 失败的时候自动换成公共 DNS，用的是 223.5.5.5、119.29.29.29、114.114.114.114、1.1.1.1 这几个。第二，测速之前先把所有域名批量预解析好存起来，后面直接走 IP 连。第三，自己实现了 DNS over UDP 查询，不依赖系统解析器。

### （4）GitHub 加速

内置 10 个 GitHub 代理，用真实的仓库归档文件测速，然后挑最快的那个用。

### （5）恢复原版游戏下载

社区开源版用 RestrictMcDownload 这个开关把原版游戏下载入口藏起来了，牵涉到 14 处代码调用。本项目默认把这个开关放开。

### （6）微软正版登录

补齐了设备代码流登录需要的客户端 ID，登录的时候点微软那个按钮就能用。如果你自己有 Azure 应用，可以设环境变量 PCL_MS_CLIENT_ID 覆盖掉。

### （7）百宝箱

开源版把整个百宝箱页面换成了一句提示文字。本版本重新实现了里面的全部功能。

（1）今日人品。用用户名加日期做哈希，算出 0 到 100 的人品值，同一天同一个账号结果不变，配了分级评语。

（2）内存优化。把能访问到的进程工作集清空，再触发一次 GC，整个流程在进程内完成，不需要管理员权限。

（3）清理垃圾。清缓存目录、任务残留、皮肤缓存和旧日志，日志保留最近 3 个。

（4）自定义下载。填下载网址和保存路径就能下，支持浏览按钮选位置，有进度提示。

（5）回声洞。收了 140 条社区留言随机展示。

### （8）分段下载不再被锁

上游 ModNet.vb 里有一段硬编码的限制，只要地址里带 bmclapi 或者 github.com 就直接禁掉多线程，只能单线程跑。

本版本改成默认允许分段，只对确实不支持 Range 的站点保留单线程。另外分片拆分阈值从 256KB 降到 64KB，并发数爬升更快，默认线程数定成 32。

同一个源站实测的成绩，文件 21MB。

| 线程数 | 速度 |
|---|---|
| 1 | 2.31 MB/s |
| 8 | 4.88 MB/s |
| 16 | 6.04 MB/s |
| 32 | 6.26 MB/s |
| 64 | 5.60 MB/s |

32 线程是最优点，再加线程反而因为连接开销和源站限流掉速度。想恢复上游那套保守做法，把设置项 ToolDownloadSafeSplit 打开就行。

### （9）自定义 API

加了两个设置项，可以指定自己的接口。

（1）ToolDownloadSourceApi 用来指定文件下载 API。

（2）ToolDownloadVersionApi 用来指定版本列表 API。

## 二、怎么用

启动参数有两个。

```
"Plain Craft Launcher 2.exe" --download    直接进原版游戏下载页
"Plain Craft Launcher 2.exe" --treasure    直接进百宝箱
```

注意别用 --test 这个参数。那是上游留的自动化测试用的，会把所有页面遍历一遍然后自己退出。

设置页里多了一个「镜像测速与站点选择」卡片，能看到各镜像的实时延迟，也能手动指定 Mod 站点、填自定义镜像。

## 三、编译

先装好这些东西。Windows 系统，Visual Studio 2022 Build Tools 或者更高版本并且带 MSBuild，.NET SDK 8.0 或以上，还有 .NET Framework 4.8 目标包。

依赖库和主程序要分开编译，因为用的编译器不一样。

```powershell
dotnet build MeloongCore\Shared\MeloongCore.csproj -c Release
dotnet build MeloongCore\Wpf\MeloongCore.Wpf.csproj -c Release
dotnet build PCLCS\PCLCS.csproj -c Release

msbuild "Plain Craft Launcher 2\Plain Craft Launcher 2.vbproj" -p:Configuration=Release -p:Platform=AnyCPU
```

编出来的东西在 Plain Craft Launcher 2\bin\Plain Craft Launcher 2.exe。

依赖库请用 dotnet build，主程序用 msbuild。要是拿 BuildTools 的 MSBuild 去编依赖库，会报找不到 Microsoft.NET.Sdk。

关于 MeloongCore 这一块，本仓库是把它当普通目录放进来的，不是 git 子模块，这样 clone 下来就能直接编。想跟着上游更新的话，手动去 Meloong-Git/MeloongCore 同步一下。

## 四、目录结构

```
Plain Craft Launcher 2/
├── Modules/
│   ├── Minecraft/
│   │   ├── ModMirror.vb        镜像测速核心，本项目新增
│   │   ├── ModDownload.vb      下载源构造
│   │   ├── ModJava.vb          Java 运行时下载
│   ├── Base/
│   │   ├── ModNet.vb           网络层，GitHub 加速也接在这
│   └── ModSecret.vb            凭据配置
├── Pages/
│   ├── PageDownload/           下载相关页面
│   └── PageSetup/              设置页面
```

## 五、核心实现

ModMirror.vb 里这几个方法是关键。

（1）MirrorMeasure 并发测速，返回按综合得分排好序的源列表。

（2）MirrorPing 测首字节延迟，支持 IP 直连。

（3）MirrorBenchmark 实测下载带宽，单位 KB/s。

（4）MirrorCandidates 按文件类型筛出候选地址。

（5）MirrorRewrite 把官方地址改写成镜像地址。

（6）MirrorResolveHost 解析域名，带公共 DNS 兜底。

（7）GitHubCandidates 给 GitHub 代理测速择优。

排序的打分是这么算的。有带宽数据的时候，得分等于带宽乘 0.9 加上一千除以延迟再乘 100。没测出带宽的源，得分等于 0.1 加上一千除以延迟。这样没测出带宽的源会被压到列表最后面，不会出现那种连接很快但传数据龟速的源被优先用上。

## 六、已知限制

（1）本仓库基于社区开源版，不带 CurseForge API Key，相关功能要自己申请。

（2）部分镜像站有请求频率限制，测得太频繁会返回 429，所以测速结果带了缓存。

（3）上游更新要手动合并。

（4）下载速度受源站限速影响，单个源实测上限大概在 5 到 6 MB/s。多源同时发请求会被判成异常流量，速度反而掉下去，所以没做多源聚合。

## 七、许可

沿用上游的 LICENCE 文件，修改部分也用同样的许可分发。

## 八、致谢

Meloong-Git/PCL 是上游项目。Meloong-Git/MeloongCore 提供了核心库。bangbang93/openbmclapi 提供了 BMCLAPI 镜像服务。
