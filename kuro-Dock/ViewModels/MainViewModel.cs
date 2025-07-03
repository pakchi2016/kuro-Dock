using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Kuro_Dock.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        public ObservableCollection<TabViewModel> Tabs { get; } = new();

        [ObservableProperty]
        private TabViewModel? selectedTab;

        public MainViewModel(IServiceProvider serviceProvider)
        {
            // XAMLデザイナーによる誤動作を防ぐための、厳格なチェックですわ
            bool inDesignMode = DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()) ||
                                LicenseManager.UsageMode == LicenseUsageMode.Designtime;

            if (inDesignMode) return;

            // コンストラクタでは、DIコンテナを受け取るだけにとどめます
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 最初のタブを作成する、初期化メソッドですわ。
        /// </summary>
        public void Initialize()
        {
            AddNewTab();
        }

        [RelayCommand]
        private void AddNewTab()
        {
            var newTab = _serviceProvider.GetRequiredService<TabViewModel>();
            Tabs.Add(newTab);
            SelectedTab = newTab;
        }

        [RelayCommand]
        private void CloseTab(TabViewModel? tab)
        {
            if (tab != null)
            {
                Tabs.Remove(tab);
            }
        }
    }
}