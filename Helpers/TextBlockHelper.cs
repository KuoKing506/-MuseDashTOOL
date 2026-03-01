using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace MdModManager.Helpers;

/// <summary>
/// TextBlock 辅助类，通过附加属性实现关键词高亮显示
/// </summary>
public static class TextBlockHelper
{
    // 需要显示的原始文本
    public static readonly AttachedProperty<string?> HighlightTextProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, string?>("HighlightText", typeof(TextBlockHelper));

    // 需要匹配并高亮的搜索关键词
    public static readonly AttachedProperty<string?> SearchTextProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, string?>("SearchText", typeof(TextBlockHelper));

    public static string? GetHighlightText(TextBlock element) => element.GetValue(HighlightTextProperty);
    public static void SetHighlightText(TextBlock element, string? value) => element.SetValue(HighlightTextProperty, value);

    public static string? GetSearchText(TextBlock element) => element.GetValue(SearchTextProperty);
    public static void SetSearchText(TextBlock element, string? value) => element.SetValue(SearchTextProperty, value);

    static TextBlockHelper()
    {
        // 监听属性变更
        HighlightTextProperty.Changed.AddClassHandler<TextBlock>(OnPropertyChanged);
        SearchTextProperty.Changed.AddClassHandler<TextBlock>(OnPropertyChanged);
    }

    private static void OnPropertyChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        UpdateHighlighting(textBlock);
    }

    /// <summary>
    /// 根据搜索文本更新 TextBlock 的 Inlines 实现高亮
    /// </summary>
    private static void UpdateHighlighting(TextBlock textBlock)
    {
        var text = GetHighlightText(textBlock);
        var search = GetSearchText(textBlock);

        textBlock.Inlines?.Clear();

        if (string.IsNullOrEmpty(text)) return;

        // 如果没有搜索关键词，直接显示原始文本
        if (string.IsNullOrEmpty(search))
        {
            textBlock.Inlines?.Add(new Run { Text = text });
            return;
        }

        int index = 0;
        while (index < text.Length)
        {
            // 不区分大小写查找匹配位置
            int foundIndex = text.IndexOf(search, index, StringComparison.OrdinalIgnoreCase);
            if (foundIndex == -1)
            {
                textBlock.Inlines?.Add(new Run { Text = text.Substring(index) });
                break;
            }

            // 添加匹配前的普通文本
            if (foundIndex > index)
            {
                textBlock.Inlines?.Add(new Run { Text = text.Substring(index, foundIndex - index) });
            }

            // 添加匹配到的高亮文本（粉色 & 加粗）
            textBlock.Inlines?.Add(new Run
            {
                Text = text.Substring(foundIndex, search.Length),
                Foreground = Brushes.HotPink,
                FontWeight = FontWeight.Bold
            });

            index = foundIndex + search.Length;
        }
    }
}
