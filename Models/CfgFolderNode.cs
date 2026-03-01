using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MdModManager.Models;

/// <summary>代表配置文件夹节点，可以折叠/展开</summary>
public partial class CfgFolderNode : ObservableObject
{
    [ObservableProperty]
    private string _folderName = string.Empty;

    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>是否在根目录。如果是根目录的文件，不展示展开箭头，直接展示单个文件</summary>
    [ObservableProperty]
    private bool _isFileNode;

    [ObservableProperty]
    private CfgFile? _fileItem;

    /// <summary>子节点（可能是文件夹也可能是文件）</summary>
    [ObservableProperty]
    private ObservableCollection<CfgFolderNode> _children = new();

    /// <summary>用于在 UI 中排序：文件夹排在前面，文件排在后面</summary>
    public int SortOrder => IsFileNode ? 1 : 0;

    [RelayCommand]
    private void ToggleExpand()
    {
        if (!IsFileNode)
        {
            IsExpanded = !IsExpanded;
        }
    }
}
