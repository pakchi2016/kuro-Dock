using Kuro_Dock.Core.Services;
using Kuro_Dock.Features.AddressBar;
using Kuro_Dock.Features.FileList;
using Kuro_Dock.Features.FolderTree;
using Kuro_Dock.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Kuro_Dock
{
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<CommunityToolkit.Mvvm.Messaging.IMessenger>(CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default);

            // Serviceはアプリケーション全体で共有します
            services.AddSingleton<DirectoryService>();
            services.AddSingleton<FileService>();

            // MainViewModelはアプリケーションの主役なので、一つだけ生成します
            services.AddSingleton<MainViewModel>();

            // タブで使われるViewModelは、タブが作られるたびに新しいインスタンスが必要です
            services.AddTransient<TabViewModel>();
            services.AddTransient<AddressBarViewModel>();
            services.AddTransient<FileListViewModel>();
            services.AddTransient<FolderTreeViewModel>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainViewModel = ServiceProvider.GetRequiredService<MainViewModel>();

            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            // MainViewModelの準備が全て整ってから、初期化（最初のタブ作成）を実行します
            mainViewModel.Initialize();

            mainWindow.Show();
        }
    }
}