using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MdModManager.Models;

/// <summary>表示配置文件中的一条配置项（键值对 + 注释）</summary>
public partial class CfgEntry : ObservableObject
{
    /// <summary>该配置项上方的原始注释（可能多行，以 # 开头）</summary>
    public string RawComment { get; set; } = string.Empty;

    /// <summary>翻译后的注释，仅在开启自动翻译后填充</summary>
    [ObservableProperty]
    private string _displayComment = string.Empty;

    /// <summary>配置项的键名，例如 "Layout"</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>配置项的当前值（可编辑）</summary>
    [ObservableProperty]
    private string _value = string.Empty;

    /// <summary>该配置项所属的 Section 名称（如果没有则为空），例如 "Bnfour_SongInfo"</summary>
    public string SectionName { get; set; } = string.Empty;

    /// <summary>是否是一个仅用于在 UI 上显示 Section 标题的纯静态条目</summary>
    public bool IsSectionHeader { get; set; } = false;

    /// <summary>
    /// 从注释中解析出的可选值列表（如果注释里出现了 2+ 个带引号的字符串）。
    /// 若为空，则说明该项需要用户自定义输入（TextBox）。
    /// 若有内容，则渲染为下拉框（ComboBox）。
    /// </summary>
    public List<string> AvailableOptions { get; set; } = new();

    /// <summary>该配置项是否有预定义的可选值</summary>
    public bool HasOptions => AvailableOptions.Count > 0;

    /// <summary>标记该配置项是否已被用户修改（用于高亮显示）</summary>
    [ObservableProperty]
    private bool _isModified;

    partial void OnValueChanged(string value)
    {
        // 当值改变时标记为已修改
        IsModified = true;
    }
}
