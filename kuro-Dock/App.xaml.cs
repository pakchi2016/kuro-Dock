using CommunityToolkit.Mvvm.Messaging;
using Kuro_Dock.Features.AddressBar;
using Kuro_Dock.Features.FileList;
using Kuro_Dock.Features.FolderTree;
using Kuro_Dock.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
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
            // IMessengerをシングルトン（単一のインスタンス）として登録しますの
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

            // 各ViewModelを登録しますわ
            services.AddTransient<MainViewModel>();
            services.AddTransient<AddressBarViewModel>();
            services.AddTransient<FileListViewModel>();
            services.AddTransient<FolderTreeViewModel>();

            // 本来はServiceもここで登録いたしますが、それはまた次の機会に
            // services.AddSingleton<DirectoryService>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow
            {
                // DIコンテナからMainViewModelを取得して設定しますわ
                DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
            };
            mainWindow.Show();
        }
    }
}