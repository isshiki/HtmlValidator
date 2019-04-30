using System;
using System.IO;
using System.Windows;

namespace HtmlValidator
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO： 自分の環境に合わせて、以下のURLはカスタマイズしてください
        private const string _urlHtmlSource = "https://localhost/" + HtmlValidation.HtmlValidator.SourceHtmlFile;      // 検証対象のHTMLソース（URL）
        private const string _pathHtmlSource = @"C:\WebSites\" + HtmlValidation.HtmlValidator.SourceHtmlFile;          // 検証対象のHTMLソース（Path）
        private const string _pathErrorWebPage = @"C:\WebSites\" + HtmlValidation.HtmlValidator.ErrorWebPageHtml;      // 「HTMLエラー情報ページ」ファイルのパス（※このファイルは、下記のURLからWebブラウザーで開けること）
        private const string _urlErrorWebPage = "https://localhost/" + HtmlValidation.HtmlValidator.ErrorWebPageHtml;  // 「HTMLエラー情報ページ」ファイルのURL（※このファイルは、このURLからWebブラウザーで開けること）

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

            var val = new HtmlValidation.HtmlValidator(_urlHtmlSource, _pathHtmlSource);
            if (val.ValidationOfHtmlText(html, _pathErrorWebPage, _urlErrorWebPage))
            {
                MessageBox.Show(this, "HTMLソースのタグ構造は正常です。");
            }
            else
            {
                HtmlValidation.CmsUtility.Application.DoEvents();
                HtmlValidation.CmsUtility.OpenByTextEditor(_pathHtmlSource);
                HtmlValidation.CmsUtility.RunUrlorPath(_urlHtmlSource);
                HtmlValidation.CmsUtility.RunUrlorPath(_urlErrorWebPage);

                MessageBox.Show(this,
                    "ご指定のHTMLソースには「タグの書き損じ」や「タグ階層の破たん」といった問題が存在します。\n\n" +
                    "先ほど、「検証対象のHTMLソース」をデフォルトのテキストエディターとブラウザーで、\n" + 
                    "「HTMLエラー情報ページ」をデフォルトのブラウザーで表示しましたのでご確認ください。");
            }
        }
    }
}
