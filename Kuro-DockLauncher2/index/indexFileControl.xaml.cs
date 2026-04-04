using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KuroDockLauncher2.Index
{
    /// <summary>
    /// IndexControl.xaml の相互作用ロジック
    /// </summary>
    
    public partial class IndexFileControl : Window
    {
        string index = "IndexFilePanel";
        string middle = "FileMiddlePanel";
        public IndexFileControl()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var mainwindow = Application.Current.MainWindow;
            if (mainwindow == null) { return; }

            var panel = (StackPanel)mainwindow.FindName(index);
            if (panel == null) { return; }

            var middlePanel = (StackPanel)mainwindow.FindName(middle);

            string indexName = IndexNameTextBox.Text.Trim();
            Button IndexButton = new Button
            {
                Width = 90,
                Height = 30,
                Margin = new Thickness(0, 1, 0, 0),
                Content = indexName
            };
            IndexButton.AllowDrop = true;
            IndexButton.Drop += Bookmark_Drop;
            IndexButton.MouseEnter += Bookmark_View;

            ContextMenu menu = new ContextMenu();
            MenuItem removeItem = new MenuItem();
            removeItem.Header = "ファイルインデックスボタン削除";
            removeItem.Click += (s, args) =>
            {
                panel.Children.Remove(IndexButton);
                middlePanel.Children.Clear();
            };
            menu.Items.Add(removeItem);
            IndexButton.ContextMenu = menu;

            int panelIndex = panel.Children.Count - 1;
            panel.Children.Insert(panelIndex, IndexButton);

            this.DialogResult = true; // ダイアログを閉じて、OKが選択されたことを示す
            this.Close();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

            this.DialogResult = true; // ダイアログを閉じて、OKが選択されたことを示す
            this.Close();
        }

        private void Bookmark_Drop(object sender, DragEventArgs e)
        {
            Button button = (Button)sender;

            List<string> pathList = button.Tag as List<string> ?? new List<string>();
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    // ファイルの場合の処理
                    if (File.Exists(file)) pathList.Add(file);
                }
                button.Tag = pathList;
            }

            var mainwindow = Application.Current.MainWindow;
            var panel = (StackPanel)mainwindow.FindName(middle);
            if (panel != null)
            {
                panel.Children.Clear();
                foreach (string entry in pathList)
                {
                    AddFileButton(entry, panel);
                }
            }
        }

        private void Bookmark_View(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) { return; }
            if (sender is Button indexButton && indexButton.Tag is List<string> pathList)
            {
                if (pathList.Count == 0) { return; }
                var mainwindow = Application.Current.MainWindow;
                var panel = (StackPanel)mainwindow.FindName(middle);

                if (panel == null) { return; }

                panel.Children.Clear();
                foreach (string entry in pathList)
                {
                    AddBookmarkButton(entry, panel);
                }
            }
        }

        public static void AddBookmarkButton(string item, StackPanel panel)
        {

            Button newbutton = new Button
            {
                Width = 90,
                Height = 30,
                Margin = new Thickness(0, 1, 0, 0),
                Tag = item,
                Content = System.IO.Path.GetFileName(item),
            };

            newbutton.Click += BookmarkButton_Click;
            panel.Children.Add(newbutton);
        }

        public static void AddFileButton(string item, StackPanel panel)
        {
            Button newbutton = new Button
            {
                Width = 90,
                Height = 30,
                Margin = new Thickness(0, 1, 0, 0),
                Tag = item,
                Content = System.IO.Path.GetFileName(item),
            };
            panel.Children.Add(newbutton);
            newbutton.Click += BookmarkButton_Click;
        }
        public static void BookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = button.Tag.ToString(),
                    UseShellExecute = true
                }
            );
        }
    }
}
