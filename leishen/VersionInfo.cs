namespace leishen;

/// <summary>
/// 中央版本号 — 所有版本引用都指向这里
/// 发布时只需修改此处，version.txt 由 CI 自动读取
/// </summary>
public static class VersionInfo
{
    /// <summary>语义化版本号（不含 v 前缀）</summary>
    public const string Version = "2.1.0";

    /// <summary>Git tag（含 v 前缀）</summary>
    public const string Tag = "v" + Version;

    /// <summary>应用名称</summary>
    public const string Title = "PUBG助手";

    /// <summary>应用描述</summary>
    public const string Description = "智能时长暂停工具";

    /// <summary>完整标题行</summary>
    public static readonly string AppTitle = $"{Title} v{Version}";

    /// <summary>GitHub 仓库信息</summary>
    public const string GitHubOwner = "Bade-Gusi";
    public const string GitHubRepo = "leishen-pause";
    public const string GitHubApi = "https://api.github.com";
    public static readonly string ReleaseApiUrl = $"{GitHubApi}/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
    public static readonly string DownloadBaseUrl = $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases";

    /// <summary>User-Agent</summary>
    public static readonly string UserAgent = $"{Title}/{Version}";
}
