using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DocumentFileManager.Viewer.Viewers;

/// <summary>
/// TextViewer.xaml の相互作用ロジック
/// </summary>
public partial class TextViewer : UserControl
{
    public TextViewer()
    {
        InitializeComponent();
    }

    /// <summary>
    /// テキストファイルを読み込み
    /// </summary>
    public void LoadText(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("テキストファイルが見つかりません", filePath);
            }

            // エンコーディング自動判定
            var encoding = DetectEncoding(filePath);
            EncodingText.Text = encoding.EncodingName;

            // テキスト読み込み
            var text = File.ReadAllText(filePath, encoding);
            TextControl.Text = text;

            // 行数を表示
            var lineCount = text.Split('\n').Length;
            LineCountText.Text = lineCount.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"テキストファイルの読み込みに失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// エンコーディングを自動判定（UTF-8 または Shift-JIS）
    /// </summary>
    private Encoding DetectEncoding(string filePath)
    {
        try
        {
            // まずUTF-8として読み込んでみる
            var bytes = File.ReadAllBytes(filePath);

            // BOM付きUTF-8をチェック
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return Encoding.UTF8;
            }

            // UTF-8として妥当かチェック
            try
            {
                var decoder = Encoding.UTF8.GetDecoder();
                decoder.Fallback = DecoderFallback.ExceptionFallback;

                var chars = new char[decoder.GetCharCount(bytes, 0, bytes.Length)];
                decoder.GetChars(bytes, 0, bytes.Length, chars, 0);

                // UTF-8として正常に読めた
                return Encoding.UTF8;
            }
            catch (DecoderFallbackException)
            {
                // UTF-8として読めなかったのでShift-JISと判定
                return Encoding.GetEncoding("shift_jis");
            }
        }
        catch
        {
            // エラーの場合はUTF-8をデフォルトとする
            return Encoding.UTF8;
        }
    }
}
