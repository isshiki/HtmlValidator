using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;

namespace HtmlValidation
{
    public static class CmsUtility
    {
        #region 内部で使用しているメソッドや変数

        private static string defaultTextEditorExePath;

        private static string InternalGetDefaultTextEditorExePath()
        {
            // 「HKEY_CLASSES_ROOT\.txt」の既定値（＜.txt値＞）を取得する
            string txtEditorName = "";

            // レジストリ・キーを開く
            string keyDotTxt = ".txt";
            RegistryKey rKeyDotTxt =
              Registry.ClassesRoot.OpenSubKey(keyDotTxt);
            if (rKeyDotTxt != null)
            {

                // レジストリの値を取得する
                string defaultValue =
                  (string)rKeyDotTxt.GetValue(String.Empty);

                // レジストリ・キーを閉じる
                rKeyDotTxt.Close();

                txtEditorName = defaultValue;
            }
            if (txtEditorName == null || txtEditorName == "")
            {
                return "";
            }

            // 「HKEY_CLASSES_ROOT\＜.txt値＞\shell\open\command」
            // の既定値を取得する
            string path = "";

            // レジストリ・キーを開く
            string keyTxtEditor = txtEditorName + @"\shell\open\command";
            RegistryKey rKey =
              Registry.ClassesRoot.OpenSubKey(keyTxtEditor);
            if (rKey != null)
            {

                // レジストリの値を取得する
                string command = (string)rKey.GetValue(String.Empty);

                // レジストリ・キーを閉じる
                rKey.Close();

                if (command == null)
                {
                    return path;
                }

                // 前後の余白を削る
                command = command.Trim();
                if (command.Length == 0)
                {
                    return path;
                }

                // 「"」で始まるパス形式かどうかで処理を分ける
                if (command[0] == '"')
                {
                    // 「"～"」間の文字列を抽出
                    int endIndex = command.IndexOf('"', 1);
                    if (endIndex != -1)
                    {
                        // 抽出開始を「1」ずらす分、長さも「1」引く
                        path = command.Substring(1, endIndex - 1);
                    }
                }
                else
                {
                    // 「（先頭）～（スペース）」間の文字列を抽出
                    int endIndex = command.IndexOf(' ');
                    if (endIndex != -1)
                    {
                        path = command.Substring(0, endIndex);
                    }
                    else
                    {
                        path = command;
                    }
                }
            }

            return path;
        }

        #endregion

        public static bool OpenByTextEditor(string path)
        {
            if (String.IsNullOrEmpty(path)) return false;
            if (File.Exists(path) == false) return false;
            try
            {
                // 既定のテキスト・エディタのパスを取得する
                if (String.IsNullOrEmpty(defaultTextEditorExePath))
                {
                    defaultTextEditorExePath = InternalGetDefaultTextEditorExePath();
                }

                // 既定のテキスト・エディタを立ち上げる
                Process.Start(defaultTextEditorExePath, path);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool RunUrlorPath(string url)
        {
            if (String.IsNullOrEmpty(url))
            {
                return false;
            }
            url = url.Trim();
            if (url.Length < 3)
            {
                return false;
            }
            if (url.StartsWith("http://", StringComparison.Ordinal) || url.StartsWith("https://", StringComparison.Ordinal) || url.StartsWith(@"\\", StringComparison.Ordinal) || url.Substring(1).StartsWith(@":\", StringComparison.Ordinal))
            {
                try
                {
                    Process.Start(url);
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        #region アプリケーション全体機能をまとめたクラス

        public static class Application
        {
            public static string UserAppDataPath {
                get {
                    return GetFileSystemPath(Environment.SpecialFolder.ApplicationData);
                }
            }

            public static string CommonAppDataPath {
                get {
                    return GetFileSystemPath(Environment.SpecialFolder.CommonApplicationData);
                }
            }

            public static string LocalUserAppDataPath {
                get {
                    return GetFileSystemPath(Environment.SpecialFolder.LocalApplicationData);
                }
            }

            public static RegistryKey CommonAppDataRegistry {
                get {
                    return GetRegistryPath(Registry.LocalMachine);
                }
            }

            public static RegistryKey UserAppDataRegistry {
                get {
                    return GetRegistryPath(Registry.CurrentUser);
                }
            }

            private static string GetFileSystemPath(Environment.SpecialFolder folder)
            {
                // パスを取得
                string path = String.Format(@"{0}\{1}\{2}",
                    Environment.GetFolderPath(folder),              // ベース・パス
                    System.Windows.Forms.Application.CompanyName,   // 会社名
#if DA_MODE
                    "KomabaBrowser"                                 // 製品名
#else
                    System.Windows.Forms.Application.ProductName                         // 製品名
#endif
                    );

                // パスのフォルダを作成
                lock (typeof(Application))
                {
                    if (Directory.Exists(path) == false)
                    {
                        Directory.CreateDirectory(path);
                    }
                }
                return path;
            }

            private static RegistryKey GetRegistryPath(RegistryKey key)
            {
                // パスを取得
                string basePath;
                if (key == Registry.LocalMachine)
                    basePath = "SOFTWARE";
                else
                    basePath = "Software";
                string path = String.Format(@"{0}\{1}\{2}",
                    basePath,                           // ベース・パス
                    System.Windows.Forms.Application.CompanyName,            // 会社名
                    System.Windows.Forms.Application.ProductName);           // 製品名

                // パスのレジストリ・キーの取得（および作成）
                return key.CreateSubKey(path);
            }

            // WPF用
            public static void DoEvents()
            {
                DispatcherFrame frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrames), frame);
                Dispatcher.PushFrame(frame);
            }

            private static object ExitFrames(object f)
            {
                ((DispatcherFrame)f).Continue = false;
                return null;
            }
        }

        #endregion
    }
}
