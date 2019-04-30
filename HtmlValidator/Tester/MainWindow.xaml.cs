using System;
using System.Windows;

namespace HtmlValidator
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string _urlTragetHtmlPage = "https://localhost/article.html"; // 記事のURL
        private const string _pathErrorWebPage = @"C:\WebSites\test.html";         // エラー表示用HTMLファイルのパス（※このファイルは、下記のURLからWebブラウザーで開けること）
        private const string _urlErrorWebPage = "https://localhost/test.html";    // エラー表示用HTMLファイルのURL（※このファイルは、このURLからWebブラウザーで開けること）

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonValidate_Click(object sender, RoutedEventArgs e)
        {
            var html = this.textboxHtmlSource.Text;
            if (String.IsNullOrWhiteSpace(html))
            {
                MessageBox.Show("HTMLソースを入力してください。");
                return;
            }

            HtmlValidation.HtmlValidator val = new HtmlValidation.HtmlValidator(_urlTragetHtmlPage);
            if (val.ValidationOfHtmlText(html, _pathErrorWebPage, _urlErrorWebPage))
            {
                MessageBox.Show(this, "HTMLソースのタグ構造は正常です。");
            }
            else
            {
                MessageBox.Show(this, "HTMLソースには「タグの書き損じ」や「タグ階層の破たん」が存在します。\n\nWebブラウザーが自動的に起動してエラー情報ページが表示されていますのでご確認ください。");
            }
        }
    }
}
