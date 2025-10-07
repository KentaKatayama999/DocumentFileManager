namespace DocumentFileManager.UI.Configuration;

/// <summary>
/// UI表示に関する設定
/// </summary>
public class UISettings
{
    /// <summary>
    /// チェックボックスの設定
    /// </summary>
    public CheckBoxSettings CheckBox { get; set; } = new();

    /// <summary>
    /// グループボックスの設定
    /// </summary>
    public GroupBoxSettings GroupBox { get; set; } = new();

    /// <summary>
    /// レイアウトの設定
    /// </summary>
    public LayoutSettings Layout { get; set; } = new();

    /// <summary>
    /// 色の設定
    /// </summary>
    public ColorSettings Colors { get; set; } = new();
}

/// <summary>
/// チェックボックスの設定
/// </summary>
public class CheckBoxSettings
{
    /// <summary>
    /// 最小幅
    /// </summary>
    public double MinWidth { get; set; } = 150;

    /// <summary>
    /// フォントサイズ
    /// </summary>
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// マージンの基準値（深さに応じて変動）
    /// </summary>
    public double MarginDepthMultiplier { get; set; } = 10;

    /// <summary>
    /// 左マージンの追加値
    /// </summary>
    public double MarginLeft { get; set; } = 5;

    /// <summary>
    /// 上マージン
    /// </summary>
    public double MarginTop { get; set; } = 3;

    /// <summary>
    /// 右マージン
    /// </summary>
    public double MarginRight { get; set; } = 5;

    /// <summary>
    /// 下マージン
    /// </summary>
    public double MarginBottom { get; set; } = 3;
}

/// <summary>
/// グループボックスの設定
/// </summary>
public class GroupBoxSettings
{
    /// <summary>
    /// ルート項目の最小幅
    /// </summary>
    public double RootMinWidth { get; set; } = 350;

    /// <summary>
    /// 子グループボックスの最小幅（チェック項目の場合）
    /// </summary>
    public double ChildItemMinWidth { get; set; } = 300;

    /// <summary>
    /// 子グループボックスの最小幅（分類の場合）
    /// </summary>
    public double ChildCategoryMinWidth { get; set; } = 400;

    /// <summary>
    /// 内側のパディング
    /// </summary>
    public double Padding { get; set; } = 10;

    /// <summary>
    /// 枠線の太さ
    /// </summary>
    public double BorderThickness { get; set; } = 2;

    /// <summary>
    /// マージンの基準値（深さに応じて変動）
    /// </summary>
    public double MarginDepthMultiplier { get; set; } = 10;

    /// <summary>
    /// 上マージン
    /// </summary>
    public double MarginTop { get; set; } = 5;

    /// <summary>
    /// 右マージン
    /// </summary>
    public double MarginRight { get; set; } = 5;

    /// <summary>
    /// 下マージン
    /// </summary>
    public double MarginBottom { get; set; } = 5;
}

/// <summary>
/// レイアウトの設定
/// </summary>
public class LayoutSettings
{
    /// <summary>
    /// チェック項目をWrapPanelで横並びにする最小個数
    /// </summary>
    public int WrapPanelItemThreshold { get; set; } = 5;

    /// <summary>
    /// 分類をWrapPanelで横並びにする最小個数
    /// </summary>
    public int WrapPanelCategoryThreshold { get; set; } = 2;

    /// <summary>
    /// 1行あたりの最大列数
    /// </summary>
    public int MaxColumnsPerRow { get; set; } = 3;

    /// <summary>
    /// 1列あたりの幅
    /// </summary>
    public double WidthPerColumn { get; set; } = 200;

    /// <summary>
    /// グループボックスの追加パディング
    /// </summary>
    public double GroupBoxExtraPadding { get; set; } = 80;

    /// <summary>
    /// 計算された幅の最大値
    /// </summary>
    public double MaxCalculatedWidth { get; set; } = 700;
}

/// <summary>
/// 色の設定
/// </summary>
public class ColorSettings
{
    /// <summary>
    /// 大分類（深さ0）の枠線色
    /// </summary>
    public BorderColor Depth0 { get; set; } = new() { R = 44, G = 62, B = 80, Description = "大分類: 濃いグレー" };

    /// <summary>
    /// 中分類（深さ1）の枠線色
    /// </summary>
    public BorderColor Depth1 { get; set; } = new() { R = 52, G = 152, B = 219, Description = "中分類: 青" };

    /// <summary>
    /// 小分類（深さ2）の枠線色
    /// </summary>
    public BorderColor Depth2 { get; set; } = new() { R = 46, G = 204, B = 113, Description = "小分類: 緑" };

    /// <summary>
    /// それ以下の深さの枠線色
    /// </summary>
    public BorderColor DepthDefault { get; set; } = new() { R = 149, G = 165, B = 166, Description = "それ以下: 薄いグレー" };
}

/// <summary>
/// 枠線の色設定
/// </summary>
public class BorderColor
{
    /// <summary>
    /// 赤成分（0-255）
    /// </summary>
    public byte R { get; set; }

    /// <summary>
    /// 緑成分（0-255）
    /// </summary>
    public byte G { get; set; }

    /// <summary>
    /// 青成分（0-255）
    /// </summary>
    public byte B { get; set; }

    /// <summary>
    /// 説明
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
