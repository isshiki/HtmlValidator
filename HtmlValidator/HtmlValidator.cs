using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace HtmlValidation
{
    public class HtmlValidator
    {
        private string URL { get; set; }  // 検証対象のHTMLソースのURL
        private string Path { get; set; }  // 検証対象のHTMLソースのファイルパス

        public const string SourceHtmlFile = "@html-source.html";
        public const string ErrorWebPageHtml = "@html-validation.html";

        private const string breakMark = "【BR】";

        private HtmlValidator()
        {
            // コンストラクターの使用方法を制限
        }

        public HtmlValidator(string url, string path)
        {
            this.URL = url;
            this.Path = path;
        }

        // 「 」＝「\u0020;」＝通常のスペース（SP）＝HTMLではこれしか使えないので、以下は無視してよい
        // 「 」＝「\u00A0;」＝ノーブレークスペース（&nbsp;）
        // 「 」＝「\u2002;」＝半角スペース（&ensp;）
        // 「 」＝「\u2003;」＝全角スペース（&emsp;）
        private static readonly char[] END_CHAR_OF_TAG_NAME = { '>', '\u0020', '\t', '\n', '"', '\'' };

        //private static bool IsOpenLetterOfTag(char ch)
        //{
        //    switch (ch)
        //    {
        //        case '>':
        //            return true;
        //        default:
        //            return false;
        //    }
        //}

        private static bool IsCloseLetterOfTag(char ch)
        {
            switch (ch)
            {
                case '>':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsEndLetterOfTagTerm(char ch)
        {
            switch (ch)
            {
                case '\u0020':
                case '\t':
                case '\n':
                case '"':
                case '\'':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsEndLetterOfAttrName(char ch)
        {
            switch (ch)
            {
                case '>':
                case '\u0020':
                case '\t':
                case '\n':
                case '"':
                case '\'':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsSpaceLetterOfInsideTag(char ch)
        {
            switch (ch)
            {
                case '\u0020':
                case '\t':
                case '\n':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsCompletionLetterOfTag(char ch)
        {
            switch (ch)
            {
                case '/':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAssignLetterOfTag(char ch)
        {
            switch (ch)
            {
                case '=':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAttrValueDoubleLetterOfTag(char ch)
        {
            switch (ch)
            {
                case '"':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAttrValueSingleLetterOfTag(char ch)
        {
            switch (ch)
            {
                case '\'':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAttrValueEscapeLetterOfTag(char ch)
        {
            switch (ch)
            {
                case '\\':
                    return true;
                default:
                    return false;
            }
        }

        private static string EscapeTextForHtml(string htmlCode)
        {
            if (String.IsNullOrEmpty(htmlCode)) return String.Empty;
            return htmlCode.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r\n", "\n").Replace("\n", "<br>");
        }

        private static bool IsNumber(char ch)
        {
            switch (ch)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAlphabet(char ch)
        {
            switch (ch)
            {
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAllowedTagTermToken(char ch)
        {
            switch (ch)
            {
                case '-':
                case '_':
                case ':':
                case '.':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAllowedAttrNameToken(char ch)
        {
            switch (ch)
            {
                case '-':
                case '_':
                case ':':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsFirstLetterTagTerm(char ch)
        {
            return (IsAlphabet(ch));
        }

        private static bool IsNextLetterTagTerm(char ch)
        {
            return (IsAlphabet(ch) || IsNumber(ch) || IsAllowedTagTermToken(ch));
        }

        private static bool IsFirstLetterAttrName(char ch)
        {
            return (IsAlphabet(ch) || IsAllowedTagTermToken(ch));
        }

        private static bool IsNextLetterAttrName(char ch)
        {
            return (IsAlphabet(ch) || IsNumber(ch) || IsAllowedTagTermToken(ch));
        }

        private static void AddToErrorInfo(StringBuilder sbErrorInfo, int currentLineNumber, int currentColumnNumber, string errorTitle, string errorMessage, string errorHtmlCode)
        {
            sbErrorInfo.Append(
                $"\r\n<p style=\"color: blue; font-weight: bold;\">" +
                $"{currentLineNumber}行{currentColumnNumber}文字目： " +
                $"{EscapeTextForHtml(errorTitle)}<br>\r\n" +
                $"　→ {EscapeTextForHtml(errorMessage)}</p>\r\n");

            sbErrorInfo.Append("\r\n<!-------------------------------------------><hr>\r\n");
            sbErrorInfo.Append("<p style=\"color: red;\">### ▼問題箇所のHTMLコード内容（文字表示・確認用）▼ ###</p>\r\n");
            sbErrorInfo.Append($"<!-------------------------------------------><p>\r\n");

            sbErrorInfo.Append($"\r\n\r\n{EscapeTextForHtml(errorHtmlCode.Replace(breakMark, "\n"))}\r\n\r\n\r\n");
            // タグの属性部で、"..." の中ではダブルクォーテーション(")を &quot;、'...' の中ではシングルクォーテーション(')を &#39; と記述する必要があるが、ここでは不要

            sbErrorInfo.Append("<!-------------------------------------------></p><hr>\r\n");
            sbErrorInfo.Append("<p style=\"color: red;\">### ▼問題箇所のHTMLコード内容（F12開発者ツール用）▼ ###</p>\r\n");
            sbErrorInfo.Append("<!------------------------------------------->\r\n\r\n\r\n");

            errorHtmlCode = Regex.Replace(errorHtmlCode, "<html([^>]*?)>", "<div class=\"元は「html」タグ\"$1>", RegexOptions.IgnoreCase);
            errorHtmlCode = Regex.Replace(errorHtmlCode, "</html([^>]*?)>", "</div><!-- class=\"元は「/html」タグ\"$1 -->", RegexOptions.IgnoreCase);
            errorHtmlCode = Regex.Replace(errorHtmlCode, "<head([^>]*?)>", "<div class=\"元は「head」タグ\"$1>", RegexOptions.IgnoreCase);
            errorHtmlCode = Regex.Replace(errorHtmlCode, "</head([^>]*?)>", "</div><!-- class=\"元は「/head」タグ\"$1 -->", RegexOptions.IgnoreCase);
            errorHtmlCode = Regex.Replace(errorHtmlCode, "<body([^>]*?)>", "<div class=\"元は「body」タグ\"$1>", RegexOptions.IgnoreCase);
            errorHtmlCode = Regex.Replace(errorHtmlCode, "</body([^>]*?)>", "</div><!-- class=\"元は「/body」タグ\"$1 -->", RegexOptions.IgnoreCase);
            sbErrorInfo.Append(errorHtmlCode.Replace(breakMark, "<br>\r\n"));

            sbErrorInfo.Append("\r\n\r\n\r\n");
        }

        public bool ValidationOfHtmlText(string input, string errorsFilePath, string errorsFileUrl)
        {
            var retValue = true;

            var sbErrorInfo = new StringBuilder();
            sbErrorInfo.Append("<!DOCTYPE html>\r\n");
            sbErrorInfo.Append("<html lang=\"ja\">\r\n");
            sbErrorInfo.Append("<head>\r\n");
            sbErrorInfo.Append("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">\r\n");
            sbErrorInfo.Append("<title>HTMLエラー情報ページ</title>\r\n");
            sbErrorInfo.Append("</head>\r\n");
            sbErrorInfo.Append("<body>\r\n");
            sbErrorInfo.Append("\r\n<p style=\"color: red;\">【HTMLエラー情報ページ】 HTMLソースには「タグの書き損じ」や「タグ階層の破たん」といった問題が存在します。<br>\r\n" + 
                "検証対象のHTMLソース（URL）： " + this.URL + "<br>\r\n" +
                "検証対象のHTMLソース（ファイルパス）： " + this.Path + "<br>\r\n" +
                "</p>\r\n");
            sbErrorInfo.Append("\r\n<!-------------------------------------------><hr>\r\n");

            var htmlCode = input.Replace("\r\n", "\n").Replace("\r", "\n"); // 改行コードをLFに統一して改行検出を楽にする

            var tagHierarchy = new Stack<HtmlTagInfo>();        // タグの階層を管理するためのスタック
            var listUnknownTag = new List<UnkownHtmlTag>();     // 未知のタグを保存しておくリスト（分析後に警告）

            var currentLineNumber = 1;          // 現在の行番号（扱いやすいように、実際に合わせて「1」スタート）
            var currentColumnNumber = 1;        // 現在の行番号（扱いやすいように、実際に合わせて「1」スタート）
            var indexFirstOfCurLine = 0;        // 改行位置のインデックス
            var length = htmlCode.Length;       // HTMLコードの長さ、つまり最終インデックス

            var indexTemp = 0;                  // 処理用の一時インデックス

            var isInsideTag = false;            // タグの中か
            var curTagName = String.Empty;      // 現在のタグ名（開始タグで設定、終了タグで解除）
            var tempTagContent = String.Empty;  // 一時的に取得するタグコンテンツ（ある程度の正確性で良しとする）

            var isTagAttrName = false;          // タグの属性名か
            var isTagAttrAssign = false;        // タグの属性の割り当て「=」の後か
            var isTagAttrValue = false;         // タグの値か（属性名と「=」を省略できる）
            var curAttrName = String.Empty;     // 現在の属性名（左辺の属性名で設定、右辺の属性値で解除）

            // 一番外のHTMLコード文字単位解析のループ
            for (int i = 0; i < length; i++)
            {
                // 現在のインデックスにある文字を取得
                var curChar = htmlCode[i];

                switch (curChar)
                {
                    // タグ開始のswitch-case
                    case '<':
                        var modeSkip = false;
                        switch (curTagName)
                        {
                            case "script":          // <script>タグ内か（内部では「<」を処理しないため）
                            case "style":           // <style>タグ内か（内部では「<」を処理しないため）
                                modeSkip = true;    // これらのタグ内は基本的にスキップする

                                var tempNextChar = curChar;
                                // タグ処理のループ
                                for (int n = i + 1; n < length; n++)
                                {
                                    // その次のインデックスにある文字を取得
                                    tempNextChar = htmlCode[n];

                                    if (IsSpaceLetterOfInsideTag(tempNextChar))
                                    {
                                        // 後続の文字が空白文字であれば、スキップする
                                        continue;

                                    }
                                    else if (IsCompletionLetterOfTag(tempNextChar))
                                    {
                                        // JavaScriptでも、CSSでも、「<」のあとに「/ ...」と続く文法は考えにくいので閉じタグとして処理する
                                        modeSkip = false;    // この場合は、以下の処理をスキップせずに実行する
                                        break; // タグ処理のループを抜ける
                                    }
                                    else
                                    {
                                        // それ以外の文字なら、確実にJavaScriptコードか、CSSスタイルシートの文字なので、それ以上の処理は不要
                                        break; // タグ処理のループを抜ける
                                    }
                                }
                                break;
                        }
                        if (modeSkip) break; // タグ開始のswitch-caseを抜ける

                        if (i + 1 == length)
                        {
                            // 最後がタグ開始で終わってるエラー（例：<）
                            var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                            AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                "タグ名の開始でHTMLが終わっています。",
                                "正しくタグを記入してください。",
                                errorHtmlCode);
                            retValue = false;
                            break; // タグ開始のswitch-caseを抜ける
                        }

                        isInsideTag = true; // タグ開始
                        indexTemp = htmlCode.IndexOfAny(END_CHAR_OF_TAG_NAME, i + 1);

                        if (indexTemp == -1)
                        {
                            // タグ名が閉じられていないエラー（例：<abc、<!--、</）
                            var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                            AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                "タグ（もしくはコメント）の終わりが見つかりません。",
                                "正しくタグ（もしくはコメント）の終わりを記入してください。",
                                errorHtmlCode);
                            retValue = false;
                            break; // タグ開始のswitch-caseを抜ける

                        }
                        else if (indexTemp == i + 1)
                        {
                            var nextChar = htmlCode[indexTemp];

                            if (IsCloseLetterOfTag(nextChar))
                            {
                                // タグ名が空のエラー（例：<>）
                                var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                    "タグ名が空です。",
                                    "正しくタグ名を記入してください。",
                                    errorHtmlCode);
                                retValue = false;
                                break; // タグ開始のswitch-caseを抜ける

                            }
                            else
                            {
                                // タグ名が「スペース」や「"」「'」などで開始されているエラー（例：<"）
                                var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                    "タグ名が無効な文字（スペースやタグ、引用符など）で始まっています。",
                                    "正しくタグ名を記入してください。",
                                    errorHtmlCode);
                                retValue = false;
                                break; // タグ開始のswitch-caseを抜ける

                            }
                        }
                        else
                        {
                            // 次の1文字を調べて、処理を分岐する
                            var nextChar = htmlCode[i + 1];

                            if (nextChar == '!')
                            {
                                // コメントの可能性が大
                                if ((i + 7 <= length) && (htmlCode[i + 2] == '-') && (htmlCode[i + 3] == '-'))
                                {
                                    indexTemp = htmlCode.IndexOf("-->", i + 4);
                                    if (indexTemp != -1)
                                    {
                                        i = indexTemp + 2;                  // 処理を「-->」の最後の文字「>」まで進める。
                                        curChar = htmlCode[indexTemp + 2];  // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                                        isInsideTag = false;                // タグ終了

                                    }
                                    else
                                    {
                                        // コメントが閉じられていないエラー（例：<!--abc->）
                                        var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                                        AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                            "「<!--」で始まるコメントが閉じられていません。",
                                            "「-->」でコメントを閉じてください。",
                                            errorHtmlCode);
                                        retValue = false;
                                        break; // タグ開始のswitch-caseを抜ける

                                    }
                                }
                                else if ((i + 10 <= length) && (htmlCode.Substring(i + 2, 7).StartsWith("DOCTYPE", StringComparison.OrdinalIgnoreCase)))
                                {
                                    indexTemp = htmlCode.IndexOf(">", i + 9); // <!DOCTYPE>
                                    if (indexTemp != -1)
                                    {
                                        i = indexTemp;                  // 処理を「-->」の最後の文字「>」まで進める。
                                        curChar = htmlCode[indexTemp];  // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                                        isInsideTag = false;                // タグ終了

                                    }
                                    else
                                    {
                                        // コメントが閉じられていないエラー（例：<!DOCTYPE html>）
                                        var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                                        AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                            "「<!DOCTYPE」で始まるドキュメント宣言が閉じられていません。",
                                            "「<!DOCTYPE html>」のようにドキュメント宣言を閉じてください。",
                                            errorHtmlCode);
                                        retValue = false;
                                        break; // タグ開始のswitch-caseを抜ける

                                    }
                                }

                                else
                                {
                                    // コメントの開始がおかしいエラー（例：<!-abc->）
                                    var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                                    AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                        "コメントらしきものが「<!」で開始されましたが、「<!-- -->」の形式になっていません。",
                                        "正しくコメントを記載してください。",
                                        errorHtmlCode);
                                    retValue = false;
                                    break; // タグ開始のswitch-caseを抜ける

                                }
                            }
                            else if (IsFirstLetterTagTerm(nextChar))
                            {
                                // 「<」の次の1文字目がアルファベットなら、
                                // 通常のタグの可能性が大

                                // タグ処理のループ
                                for (int n = i + 1; n < length; n++)
                                {
                                    // その次のインデックスにある文字を取得
                                    nextChar = htmlCode[n];

                                    if (IsNextLetterTagTerm(nextChar))
                                    {
                                        // 後続の文字がアルファベットか数字である限り、タグ語だとして処理する
                                        continue;

                                    }
                                    else if (IsCloseLetterOfTag(nextChar))
                                    {
                                        // 後続の文字が「>」になったら、タグ語を完成させる
                                        curTagName = htmlCode.Substring(i + 1, n - i - 1);
                                        tempTagContent = htmlCode.Substring(i, n - i + 1);
                                        retValue = CheckTagHierarchy(true, htmlCode, tagHierarchy, listUnknownTag, currentLineNumber, currentColumnNumber, curTagName, tempTagContent, i, n, sbErrorInfo);
                                        //break; // 属性「値」処理のループを抜ける
                                        i = n;                     // 処理を最後の文字「>」まで進める
                                        curChar = nextChar;        // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                                        isInsideTag = false;       // タグ終了
                                        // 開始タグなので「curTagName」にタグ名を保存したまま
                                        break; // タグ処理のループを抜ける

                                    }
                                    else if (IsEndLetterOfTagTerm(nextChar))
                                    {
                                        // 後続の文字が終了文字のいずれかになったら、タグ語を完成させる
                                        curTagName = htmlCode.Substring(i + 1, n - i - 1);
                                        indexTemp = htmlCode.IndexOf(">", i + 1);
                                        if (indexTemp == -1)
                                        {
                                            tempTagContent = htmlCode.Substring(i);
                                        }
                                        else
                                        {
                                            tempTagContent = htmlCode.Substring(i, indexTemp - i + 1);
                                        }
                                        var trimedClosingToken = tempTagContent.TrimEnd(new char[] { '>', ' ' });
                                        if (trimedClosingToken[trimedClosingToken.Length - 1] == '/')
                                        {
                                            retValue = CheckTagHierarchy(false, htmlCode, tagHierarchy, listUnknownTag, currentLineNumber, currentColumnNumber, curTagName, tempTagContent, i, n, sbErrorInfo);
                                        }
                                        else
                                        {
                                            retValue = CheckTagHierarchy(true, htmlCode, tagHierarchy, listUnknownTag, currentLineNumber, currentColumnNumber, curTagName, tempTagContent, i, n, sbErrorInfo);
                                        }
                                        //i = n;                   // エラー時は、タグ名から取得したいので、開始インデックスは進めない
                                        curChar = nextChar;        // 現在の文字を念のため再設定

                                        // タグの属性（Attribute）部分処理のループ
                                        for (n = n + 1; n < length; n++)
                                        {
                                            // その次のインデックスにある文字を取得
                                            nextChar = htmlCode[n];

                                            if (IsSpaceLetterOfInsideTag(nextChar))
                                            {
                                                // 後続の文字が空白文字であれば、スキップする
                                                continue;

                                            }
                                            else if (IsCloseLetterOfTag(nextChar))
                                            {
                                                // 後続の文字が「>」になったら、タグ語を完成させる
                                                i = n;                     // 処理を最後の文字「>」まで進める
                                                curChar = nextChar;        // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                                                isInsideTag = false;       // タグ終了
                                                // 開始タグもしくは単体タグなので「curTagName」にタグ名を保存したまま
                                                break; // タグの属性（Attribute）部分処理のループを抜ける

                                            }
                                            else if (IsFirstLetterAttrName(nextChar))
                                            {
                                                // タグ名の最終文字の次の1文字目（空白文字を除く）がアルファベットなら、
                                                // 通常の属性名の可能性が大

                                                isTagAttrName = true; // 属性名開始

                                                var indexAttrName = n;

                                                // 属性「名」処理のループ
                                                for (n = n + 1; n < length; n++)
                                                {
                                                    // その次のインデックスにある文字を取得
                                                    nextChar = htmlCode[n];

                                                    if ((isTagAttrName == false) && IsFirstLetterAttrName(nextChar))
                                                    {
                                                        // <script async script="path/to">のように複数の属性があるケース
                                                        isTagAttrName = true; // 属性名開始
                                                        indexAttrName = n;
                                                        continue;  // 後続の処理はスキップ

                                                    }
                                                    else if (isTagAttrName && IsNextLetterAttrName(nextChar))
                                                    {
                                                        // 後続の文字がアルファベットか数字である限り、属性名だとして処理する
                                                        continue;  // 後続の処理はスキップ

                                                    }
                                                    else if (isTagAttrName && IsSpaceLetterOfInsideTag(nextChar))
                                                    {
                                                        // スキップ（例：<img src = "path/to">）
                                                        isTagAttrName = false;     // 属性名終了

                                                        for (n = n + 1; n < length; n++)
                                                        {
                                                            var tempChar = htmlCode[n];
                                                            if (IsSpaceLetterOfInsideTag(tempChar))
                                                            {
                                                                // スペースが続く限りスキップ
                                                                continue;
                                                            }
                                                            else
                                                            {
                                                                // スペースでなくなったら、次の処理ループに戻す
                                                                n = n - 1; // 1つ先に進み過ぎているのでマイナス
                                                                break;
                                                            }
                                                        }
                                                        continue;

                                                    }
                                                    else if (IsCloseLetterOfTag(nextChar))
                                                    {
                                                        // 後続の文字が「>」になったら、タグ語を完成させる
                                                        curAttrName = htmlCode.Substring(indexAttrName, n - indexAttrName);
                                                        i = n;                     // 処理を最後の文字「>」まで進める（タグ完了後に初めて進める）
                                                        curChar = nextChar;        // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                                                        isTagAttrName = false;     // 属性名終了
                                                        isInsideTag = false;       // タグ終了
                                                        break; // 属性「名」処理のループを抜ける

                                                    }
                                                    else if (IsAssignLetterOfTag(nextChar))
                                                    {
                                                        // 後続の文字が「>」になったら、属性名を完成させる
                                                        curAttrName = htmlCode.Substring(indexAttrName, n - indexAttrName);
                                                        //i = n;                   // エラー時は、タグ名から取得したいので、開始インデックスは進めない
                                                        curChar = nextChar;        // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                                                        isTagAttrName = false;     // 属性名終了
                                                        isTagAttrAssign = true;    // 属性割り当て

                                                        // 属性「値」処理のループ
                                                        for (n = n + 1; n < length; n++)
                                                        {
                                                            // その次のインデックスにある文字を取得
                                                            nextChar = htmlCode[n];

                                                            if (IsSpaceLetterOfInsideTag(nextChar))
                                                            {
                                                                // 後続の文字が空白文字であれば、スキップする
                                                                continue;

                                                            }
                                                            else if (IsAttrValueDoubleLetterOfTag(nextChar))
                                                            {
                                                                ParseAttributeValue(ref retValue, sbErrorInfo, htmlCode, nextChar, currentLineNumber, currentColumnNumber, length, curTagName, out isTagAttrAssign, out isTagAttrValue, ref curAttrName, i, ref curChar, ref nextChar, ref n);
                                                                break; // 属性「値」処理のループを抜ける

                                                            }
                                                            else if (IsAttrValueSingleLetterOfTag(nextChar))
                                                            {
                                                                ParseAttributeValue(ref retValue, sbErrorInfo, htmlCode, nextChar, currentLineNumber, currentColumnNumber, length, curTagName, out isTagAttrAssign, out isTagAttrValue, ref curAttrName, i, ref curChar, ref nextChar, ref n);
                                                                break; // 属性「値」処理のループを抜ける

                                                            }
                                                            else
                                                            {
                                                                // 不正な文字で属性値が始まっているエラー（例：<img src=000>）
                                                                var errorHtmlCode = htmlCode.Substring(i, n - i + 1);  // 現在の文字まで
                                                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                                                    $"{curTagName}タグの属性値が「\"」や「'」による値ではなく不正な文字「{nextChar}」で始まっています。",
                                                                    "正しくタグの属性値を記載してください。",
                                                                    errorHtmlCode);
                                                                retValue = false;
                                                                break; // 属性「値」処理のループを抜ける
                                                            }
                                                        }

                                                        if (retValue && isTagAttrAssign)
                                                        {
                                                            // 属性値の閉じがないエラー（例：<img src=）
                                                            var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                                                            AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                                                $"{curTagName}タグの{curAttrName}属性の値が「=」で割り当てられましたが、実際の値が指定されていません。",
                                                                "正しく属性値を記載してください。",
                                                                errorHtmlCode);
                                                            retValue = false;
                                                            break; // 属性「名」処理のループを抜ける

                                                        }

                                                    }
                                                    else
                                                    {
                                                        // 属性名に不正な文字が使われているエラー（例：<img src+）
                                                        var errorHtmlCode = htmlCode.Substring(i, n - i + 1);  // 現在の文字まで
                                                        AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                                            $"{curTagName}タグの属性名がアルファベットと数字だけではなく不正な文字「{nextChar}」が使われています。",
                                                            "正しくタグの属性名を記載してください。",
                                                            errorHtmlCode);
                                                        retValue = false;
                                                        break; // 属性「名」処理のループを抜ける

                                                    }

                                                    if ((retValue == false) ||
                                                        ((isTagAttrName == false) && (isTagAttrAssign == false) && (isTagAttrValue == false)))
                                                    {
                                                        break; // 属性「名」処理のループを抜ける
                                                    }
                                                }

                                                if (retValue && isTagAttrName)
                                                {
                                                    // タグが閉じられていないのに最後まで来たエラー（例：<script async src）
                                                    var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                                                    AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                                        $"{curTagName}タグが閉じられていません。",
                                                        "正しくタグを記載してください（※終了タグに属性を記載するのは正しい書式ではありません）。",
                                                        errorHtmlCode);
                                                    retValue = false;
                                                    break; // タグの属性（Attribute）部分処理のループを抜ける
                                                }

                                            }
                                            else if (IsAttrValueDoubleLetterOfTag(nextChar))
                                            {
                                                ParseAttributeValue(ref retValue, sbErrorInfo, htmlCode, nextChar, currentLineNumber, currentColumnNumber, length, curTagName, out isTagAttrAssign, out isTagAttrValue, ref curAttrName, i, ref curChar, ref nextChar, ref n);
                                                break; // タグの属性（Attribute）部分処理のループを抜ける

                                            }
                                            else if (IsAttrValueSingleLetterOfTag(nextChar))
                                            {
                                                ParseAttributeValue(ref retValue, sbErrorInfo, htmlCode, nextChar, currentLineNumber, currentColumnNumber, length, curTagName, out isTagAttrAssign, out isTagAttrValue, ref curAttrName, i, ref curChar, ref nextChar, ref n);
                                                break; // タグの属性（Attribute）部分処理のループを抜ける

                                            }
                                            else if (IsCompletionLetterOfTag(nextChar))
                                            {
                                                // シングル閉じタグ（例：<img />）
                                                // 階層処理はせずに無視する（HTML5ではなくXHTMLのような書き方だが無視する）
                                                //retValue = CheckTagHierarchy(false, htmlCode, tagHierarchy, listUnknownTag, currentLineNumber, currentColumnNumber, curTagName, tempTagContent, i, n, sbErrorInfo);

                                                // タグを確定させるループ
                                                for (n = n + 1; n < length; n++)
                                                {
                                                    // その次のインデックスにある文字を取得
                                                    nextChar = htmlCode[n];

                                                    if (IsSpaceLetterOfInsideTag(nextChar))
                                                    {
                                                        // 後続の文字が空白文字であれば、スキップする
                                                        continue;

                                                    }
                                                    else if (IsCloseLetterOfTag(nextChar))
                                                    {
                                                        // 後続の文字が「>」になったら、タグは完成
                                                        i = n;                     // 処理を最後の文字「>」まで進める
                                                        curChar = nextChar;        // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                                                        isInsideTag = false;       // タグ終了
                                                        break; // タグを確定させるループを抜ける

                                                    }
                                                    else
                                                    {
                                                        // 不正な文字たタグ内にあるエラー（例：<img / a>）
                                                        var errorHtmlCode = htmlCode.Substring(i, n - i + 1);
                                                        AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                                            $"{curTagName}タグ内に不正な文字「{nextChar}」が存在します。",
                                                            "正しくタグを記載してください。",
                                                            errorHtmlCode);
                                                        retValue = false;
                                                        break; // タグを確定させるループを抜ける

                                                    }
                                                }
                                                break; // タグの属性（Attribute）部分処理のループを抜ける

                                            }
                                            else
                                            {
                                                // 不正な文字で属性名が始まっているエラー（例：<html ="ja">）
                                                var errorHtmlCode = htmlCode.Substring(i, n - i + 1);  // 現在の文字まで
                                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                                    $"{curTagName}タグの属性がアルファベットによる名前でも「\"」や「'」による値でもなく不正な文字「{nextChar}」で始まっています。",
                                                    "正しくタグの属性を記載してください。",
                                                    errorHtmlCode);
                                                retValue = false;
                                                break; // タグの属性（Attribute）部分処理のループを抜ける

                                            }

                                            if ((retValue == false) ||
                                                (isInsideTag == false))
                                            {
                                                break; // タグの属性（Attribute）部分処理のループを抜ける
                                            }
                                        }

                                    }
                                    else if (IsCompletionLetterOfTag(nextChar))
                                    {
                                        // 「<html />」のようなシングル閉じたタグ（HTML5では通常不要だが、柔軟性を持たせて許可する）

                                        // タグを確定させるループ
                                        for (n = n + 1; n < length; n++)
                                        {
                                            // その次のインデックスにある文字を取得
                                            nextChar = htmlCode[n];

                                            if (IsSpaceLetterOfInsideTag(nextChar))
                                            {
                                                // 後続の文字が空白文字であれば、スキップする
                                                continue;

                                            }
                                            else if (IsCloseLetterOfTag(nextChar))
                                            {
                                                // 後続の文字が「>」になったら、タグ語を完成させる
                                                curTagName = htmlCode.Substring(i + 1, n - i - 2).Trim();
                                                tempTagContent = htmlCode.Substring(i, n - i + 1);
                                                retValue = CheckTagHierarchy(false, htmlCode, tagHierarchy, listUnknownTag, currentLineNumber, currentColumnNumber, curTagName, tempTagContent, i, n, sbErrorInfo);
                                                i = n;                     // 処理を最後の文字「>」まで進める
                                                curChar = nextChar;        // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                                                isInsideTag = false;       // タグ終了
                                                break; // タグを確定させるループを抜ける

                                            }
                                            else
                                            {
                                                // 不正な文字たタグ内にあるエラー（例：<html/ a>）
                                                var errorHtmlCode = htmlCode.Substring(i, n - i + 1);
                                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                                    $"{curTagName}タグ内に不正な文字「{nextChar}」が存在します。",
                                                    "正しくタグを記載してください。",
                                                    errorHtmlCode);
                                                retValue = false;
                                                break; // タグを確定させるループを抜ける
                                            }
                                        }


                                    }
                                    else
                                    {
                                        // 不正な文字で属性名が始まっているエラー（例：<abc+>）
                                        var errorHtmlCode = htmlCode.Substring(i, n - i + 1);  // 現在の文字まで
                                        AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                            $"{curTagName}タグの属性がアルファベットによる名前でも「\"」や「'」による値でもなく不正な文字「{nextChar}」で始まっています。",
                                            "正しくタグの属性を記載してください。",
                                            errorHtmlCode);
                                        retValue = false;
                                        break; // タグ処理のループを抜ける

                                    }

                                    if ((retValue == false) ||
                                        (isInsideTag == false))
                                    {
                                        break; // タグ処理のループを抜ける
                                    }
                                }

                            }
                            else if (IsCompletionLetterOfTag(nextChar))
                            {
                                // セット利用の終了タグ（別メソッド化した。複雑さを少しでお軽減するため）
                                retValue = CheckClosingTag(htmlCode, tagHierarchy, listUnknownTag,
                                    currentLineNumber, currentColumnNumber, length, indexTemp,
                                    ref curChar, ref i, ref isInsideTag, ref curTagName, sbErrorInfo);
                                curTagName = String.Empty;  // 終了タグなので「curTagName」のタグ名を消去する
                                break; // タグ開始のswitch-caseを抜ける
                            }
                            else
                            {
                                // 不正な文字でタグ名が始まっているエラー（例：<+abc>）
                                var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                    $"タグがアルファベットでも「/」でもなく不正な文字「{nextChar}」で始まっています。",
                                    "正しくタグを記載してください。",
                                    errorHtmlCode);
                                retValue = false;
                                break; // タグ開始のswitch-caseを抜ける

                            }

                        }
                        break;

                    // <script>タグと<style>タグの文字列部分はスキップ
                    case '"':
                    case '\'':
                        switch (curTagName)
                        {
                            case "script":          // <script>タグ内か（内部では「<」を処理しないため）
                            case "style":           // <style>タグ内か（内部では「<」を処理しないため）
                                // JavaScriptコードやCSSスタイルシートの中でも`"文字列"`や`'文字列'`のような形で「</script>」のように書けるので、文字列中は無視する
                                var tempCurrentChar = curChar;
                                var tempNextChar = curChar;
                                // タグ処理のループ
                                for (int n = i + 1; n < length; n++)
                                {
                                    // その次のインデックスにある文字を取得
                                    tempNextChar = htmlCode[n];

                                    if ((tempNextChar == curChar) && (tempCurrentChar != '\\')) // 「\"」「\'」といったエスケープを除く
                                    {
                                        // 引用符の閉じが来たら、スキップを確定する
                                        i = n;                     // 処理を最後の文字「"」「'」まで進める
                                        curChar = tempNextChar;    // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                                        break;  // タグ処理のループを抜ける
                                    }
                                    else
                                    {
                                        // それ以外の文字なら、確実にJavaScriptコードか、CSSスタイルシートの文字なので、それ以上の処理は不要
                                        tempCurrentChar = tempNextChar;
                                        continue;
                                    }
                                }
                                break;
                        }
                        break;

                    // タグの開始以外
                    default:
                        break;
                }

                // ループごとの最終処理
                if (retValue == false)
                {
                    // エラーがあるなら処理終了
                    break; // 一番外のHTMLコード文字単位解析のループを抜ける
                }
                else
                {
                    // エラーがないなら行番号と列番号を設定
                    switch (curChar)
                    {
                        case '\n':
                            currentLineNumber++;         // 行番号を＋１する
                            currentColumnNumber = 1;     // 列番号をリセット
                            indexFirstOfCurLine = i + 1; // 改行されたら、現在行の1文字目のインデックスを保存しておく
                            break;
                        default:
                            // 行番号は変えずに、列番号のみ＋１する
                            currentColumnNumber = (i - indexFirstOfCurLine + 1);
                            break;
                    }
                }
            }

            // 最終チェック
            if (retValue && (tagHierarchy.Count != 0))
            {
                // タグ階層が完璧か
                var errorHtmlCode = new StringBuilder();
                errorHtmlCode.Append($"以下のタグに対応するタグが見つかりませんでした： {breakMark}");
                var initLength = errorHtmlCode.Length;
                while (true)
                {
                    var tagInfo = tagHierarchy.Pop();
                    switch (tagInfo.TagType)
                    {
                        case HtmlTagType.UnknownOpening:
                            // 未知のタグなのであえてスルーする
                            //errorHtmlCode.Append($"　・ 未知のタグなので単体利用かセット利用か不明： ＜{tagInfo.Name}＞開始タグに対応する「＜/{tagInfo.Name}＞終了タグ」 {breakMark}");
                            break;
                        case HtmlTagType.UnknownClosing:
                            errorHtmlCode.Append($"　・ 未知のタグなので単体利用かセット利用か不明： ＜/{tagInfo.Name}＞終了タグに対応する「＜{tagInfo.Name}＞開始タグ」 {breakMark}");
                            break;
                        case HtmlTagType.SingleOpening:
                            Debug.Assert(false, "ここに来ることは考えられない");
                            errorHtmlCode.Append($"　・ 単体利用： ＜{tagInfo.Name}＞タグ {breakMark}");
                            break;
                        case HtmlTagType.SingleClosing:
                            Debug.Assert(false, "ここに来ることは考えられない");
                            errorHtmlCode.Append($"　・ 単体利用： ＜/{tagInfo.Name}＞終了タグ（※文法違反です） {breakMark}");
                            break;
                        case HtmlTagType.SetOpening:
                            errorHtmlCode.Append($"　・ セット利用： ＜{tagInfo.Name}＞開始タグに対応する「＜/{tagInfo.Name}＞終了タグ」 {breakMark}");
                            break;
                        case HtmlTagType.SetClosing:
                            errorHtmlCode.Append($"　・ セット利用： ＜{tagInfo.Name}＞終了タグに対応する「＜{tagInfo.Name}＞開始タグ」 {breakMark}");
                            break;
                    }
                    if (tagHierarchy.Count <= 0) break;
                }

                if (errorHtmlCode.Length > initLength)
                {
                    AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                        $"タグ階層が崩れています。開始タグと終了タグがマッチしていません。",
                        "タグ階層を正常に修正してください。",
                        errorHtmlCode.ToString());
                    retValue = false;
                }
            }

            // 未知のタグリストも出力する
            if (retValue && (listUnknownTag.Count != 0))
            {
                // タグ階層が完璧か
                var errorHtmlCode = new StringBuilder();
                errorHtmlCode.Append($"以下のタグは、未知のタグでした。念のため間違いがないかご確認ください： {breakMark}");
                errorHtmlCode.Append($"未知のタグは、単体利用かセット利用か不明なので、開始タグに対する終了タグをチェックしません。ご自分でチェックしてください： {breakMark}");
                foreach (var tagInfo in listUnknownTag)
                {
                    errorHtmlCode.Append($"　・{tagInfo.LineNumber}行{tagInfo.ColumnNumber}文字目： ＜{(tagInfo.IsOpening ? "" : "/")}{tagInfo.Name}＞{(tagInfo.IsOpening ? "開始" : "終了")}タグ {breakMark}");
                }
                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                    $"未知のタグが存在します。このHTMLでは適切に解析できない可能性があります。",
                    "問題なければそのまま続行してください。。",
                    errorHtmlCode.ToString());
                retValue = false;
            }

            // エラー表示
            if (retValue == false)
            {
                sbErrorInfo.Append("\r\n\r\n\r\n</body>\r\n");
                sbErrorInfo.Append("</html>");
                try
                {
                    File.WriteAllText(errorsFilePath, sbErrorInfo.ToString(), Encoding.UTF8);
                }
                catch (Exception)
                {
                    // 基本的にエラーはおきないはずなので、とりあえず無視する
                }
            }

            return retValue;
        }

        private static bool CheckClosingTag(
            string htmlCode, Stack<HtmlTagInfo> tagHierarchy, List<UnkownHtmlTag> listUnknownTag,
            int currentLineNumber, int currentColumnNumber, int length, int indexTagNameEnd,
            ref char curChar, ref int i, ref bool isInsideTag, ref string curTagName, StringBuilder sbErrorInfo)
        {
            var retValue = true;

            var indexTemp = 0;                  // 処理用の一時インデックス

            isInsideTag = true;                 // タグの中か
            curTagName = String.Empty;          // 現在のタグ名（開始タグで設定、終了タグで解除）
            var tempTagContent = String.Empty;  // 一時的に取得するタグコンテンツ（ある程度の正確性で良しとする）

            // 終了タグには属性設定はないが、内容を無視して許容する（※よって属性情報はいっさい呼び出し元に返さずにここで消費するのみ）
            var isTagAttrName = false;          // タグの属性名か
            var isTagAttrAssign = false;        // タグの属性の割り当て「=」の後か
            var isTagAttrValue = false;         // タグの値か（属性名と「=」を省略できる）
            var curAttrName = String.Empty;     // 現在の属性名（左辺の属性名で設定、右辺の属性値で解除）

            // 呼び出し元で処理済みのため絶対に以下は処理されないのでコメントアウト
            if (indexTagNameEnd <= i + 2)
            {
                var nextChar = htmlCode[indexTagNameEnd];

                if (IsCloseLetterOfTag(nextChar))
                {
                    // タグ名が空のエラー（例：</>）
                    var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                    AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                        "終了タグ名が空です。",
                        "正しく終了タグ名を記入してください。",
                        errorHtmlCode);
                    retValue = false;
                    //break; // タグ開始のswitch-caseを抜ける

                }
                else
                {
                    // タグ名が「スペース」や「"」「'」などで開始されているエラー（例：</"）
                    var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                    AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                        "終了タグ名が無効な文字（スペースやタグ、引用符など）で始まっています。",
                        "正しく終了タグ名を記入してください。",
                        errorHtmlCode);
                    retValue = false;
                    //break; // タグ開始のswitch-caseを抜ける

                }

            }
            else
            {
                // 次の1文字を調べて、処理を分岐する
                var nextChar = htmlCode[i + 2];

                if (IsFirstLetterTagTerm(nextChar))
                {
                    // 「<」の次の1文字目がアルファベットなら、
                    // 通常のタグの可能性が大

                    // タグ処理のループ
                    for (int n = i + 3; n < length; n++)
                    {
                        // その次のインデックスにある文字を取得
                        nextChar = htmlCode[n];

                        if (IsNextLetterTagTerm(nextChar))
                        {
                            // 後続の文字がアルファベットか数字である限り、タグ語だとして処理する
                            continue;

                        }
                        else if (IsCloseLetterOfTag(nextChar))
                        {
                            // 後続の文字が「>」になったら、タグ語を完成させる
                            curTagName = htmlCode.Substring(i + 2, n - i - 2);
                            tempTagContent = htmlCode.Substring(i, n - i + 1);
                            retValue = CheckTagHierarchy(false, htmlCode, tagHierarchy, listUnknownTag, currentLineNumber, currentColumnNumber, curTagName, tempTagContent, i, n, sbErrorInfo);
                            //break; // 属性「値」処理のループを抜ける
                            i = n;                     // 処理を最後の文字「>」まで進める
                            curChar = nextChar;        // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                            isInsideTag = false;       // タグ終了
                            break; // タグ処理のループを抜ける

                        }
                        else if (IsEndLetterOfTagTerm(nextChar))
                        {
                            // 後続の文字が終了文字のいずれかになったら、タグ語を完成させる
                            curTagName = htmlCode.Substring(i + 2, n - i - 2);
                            indexTemp = htmlCode.IndexOf(">", i + 1);
                            if (indexTemp == -1)
                            {
                                tempTagContent = htmlCode.Substring(i);
                            }
                            else
                            {
                                tempTagContent = htmlCode.Substring(i, indexTemp - i + 1);
                            }
                            retValue = CheckTagHierarchy(false, htmlCode, tagHierarchy, listUnknownTag, currentLineNumber, currentColumnNumber, curTagName, tempTagContent, i, n, sbErrorInfo);
                            //i = n;                   // エラー時は、タグ名から取得したいので、開始インデックスは進めない
                            curChar = nextChar;        // 現在の文字を念のため再設定

                            // タグの属性（Attribute）部分処理のループ
                            for (n = n + 1; n < length; n++)
                            {
                                // その次のインデックスにある文字を取得
                                nextChar = htmlCode[n];

                                if (IsSpaceLetterOfInsideTag(nextChar))
                                {
                                    // 後続の文字が空白文字であれば、スキップする
                                    continue;

                                }
                                else if (IsCloseLetterOfTag(nextChar))
                                {
                                    // 後続の文字が「>」になったら、タグ語を完成させる
                                    i = n;                     // 処理を最後の文字「>」まで進める
                                    curChar = nextChar;        // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                                    isInsideTag = false;       // タグ終了
                                    break; // タグの属性（Attribute）部分処理のループを抜ける

                                }
                                else if (IsFirstLetterAttrName(nextChar))
                                {
                                    // タグ名の最終文字の次の1文字目（空白文字を除く）がアルファベットなら、
                                    // 通常の属性名の可能性が大

                                    isTagAttrName = true; // 属性名開始

                                    var indexAttrName = n;

                                    // 属性「名」処理のループ
                                    for (n = n + 1; n < length; n++)
                                    {
                                        // その次のインデックスにある文字を取得
                                        nextChar = htmlCode[n];

                                        if ((isTagAttrName == false) && IsFirstLetterAttrName(nextChar))
                                        {
                                            // <div></div async script="path/to">のように複数の属性があるケース
                                            isTagAttrName = true; // 属性名開始
                                            indexAttrName = n;
                                            continue;  // 後続の処理はスキップ

                                        }
                                        else if (isTagAttrName && IsNextLetterAttrName(nextChar))
                                        {
                                            // 後続の文字がアルファベットか数字である限り、属性名だとして処理する
                                            continue;  // 後続の処理はスキップ

                                        }
                                        else if (isTagAttrName && IsSpaceLetterOfInsideTag(nextChar))
                                        {
                                            // スキップ（例：<div></div src = "path/to">）
                                            isTagAttrName = false;     // 属性名終了

                                            for (n = n + 1; n < length; n++)
                                            {
                                                var tempChar = htmlCode[n];
                                                if (IsSpaceLetterOfInsideTag(tempChar))
                                                {
                                                    // スペースが続く限りスキップ
                                                    continue;
                                                }
                                                else
                                                {
                                                    // スペースでなくなったら、次の処理ループに戻す
                                                    n = n - 1; // 1つ先に進み過ぎているのでマイナス
                                                    break;
                                                }
                                            }
                                            continue;

                                        }
                                        else if (IsCloseLetterOfTag(nextChar))
                                        {
                                            // 後続の文字が「>」になったら、タグ語を完成させる
                                            curAttrName = htmlCode.Substring(indexAttrName, n - indexAttrName);
                                            i = n;                     // 処理を最後の文字「>」まで進める（タグ完了後に初めて進める）
                                            curChar = nextChar;        // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                                            isTagAttrName = false;     // 属性名終了
                                            isInsideTag = false;       // タグ終了
                                            break; // 属性「名」処理のループを抜ける

                                        }
                                        else if (IsAssignLetterOfTag(nextChar))
                                        {
                                            // 後続の文字が「>」になったら、属性名を完成させる
                                            curAttrName = htmlCode.Substring(indexAttrName, n - indexAttrName);
                                            //i = n;                   // エラー時は、タグ名から取得したいので、開始インデックスは進めない
                                            curChar = nextChar;        // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                                            isTagAttrName = false;     // 属性名終了
                                            isTagAttrAssign = true;    // 属性割り当て

                                            // 属性「値」処理のループ
                                            for (n = n + 1; n < length; n++)
                                            {
                                                // その次のインデックスにある文字を取得
                                                nextChar = htmlCode[n];

                                                if (IsSpaceLetterOfInsideTag(nextChar))
                                                {
                                                    // 後続の文字が空白文字であれば、スキップする
                                                    continue;

                                                }
                                                else if (IsAttrValueDoubleLetterOfTag(nextChar))
                                                {
                                                    ParseAttributeValue(ref retValue, sbErrorInfo, htmlCode, nextChar, currentLineNumber, currentColumnNumber, length, curTagName, out isTagAttrAssign, out isTagAttrValue, ref curAttrName, i, ref curChar, ref nextChar, ref n);
                                                    break; // 属性「値」処理のループを抜ける

                                                }
                                                else if (IsAttrValueSingleLetterOfTag(nextChar))
                                                {
                                                    ParseAttributeValue(ref retValue, sbErrorInfo, htmlCode, nextChar, currentLineNumber, currentColumnNumber, length, curTagName, out isTagAttrAssign, out isTagAttrValue, ref curAttrName, i, ref curChar, ref nextChar, ref n);
                                                    break; // 属性「値」処理のループを抜ける

                                                }
                                                else
                                                {
                                                    // 不正な文字で属性値が始まっているエラー（例：<div></div src=000>）
                                                    var errorHtmlCode = htmlCode.Substring(i, n - i + 1);  // 現在の文字まで
                                                    AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                                        $"/{curTagName}終了タグの属性値が「\"」や「'」による値ではなく不正な文字「{nextChar}」で始まっています。",
                                                        "正しく終了タグの属性値を記載してください（※終了タグに属性を記載するのは正しい書式ではありません）。",
                                                        errorHtmlCode);
                                                    retValue = false;
                                                    break; // 属性「値」処理のループを抜ける
                                                }
                                            }

                                            if (retValue && isTagAttrAssign)
                                            {
                                                // 属性値の閉じがないエラー（例：<div></div src=）
                                                var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                                    $"/{curTagName}終了タグの{curAttrName}属性の値が「=」で割り当てられましたが、実際の値が指定されていません。",
                                                    "正しく属性値を記載してください（※終了タグに属性を記載するのは正しい書式ではありません）。",
                                                    errorHtmlCode);
                                                retValue = false;
                                                break; // 属性「名」処理のループを抜ける

                                            }

                                        }
                                        else
                                        {
                                            // 属性名に不正な文字が使われているエラー（例：<div></div src+）
                                            var errorHtmlCode = htmlCode.Substring(i, n - i + 1);  // 現在の文字まで
                                            AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                                $"/{curTagName}終了タグの属性名がアルファベットと数字だけではなく不正な文字「{nextChar}」が使われています。",
                                                "正しく終了タグの属性名を記載してください（※終了タグに属性を記載するのは正しい書式ではありません）。",
                                                errorHtmlCode);
                                            retValue = false;
                                            break; // 属性「名」処理のループを抜ける

                                        }

                                        if ((retValue == false) ||
                                            ((isTagAttrName == false) && (isTagAttrAssign == false) && (isTagAttrValue == false)))
                                        {
                                            break; // 属性「名」処理のループを抜ける
                                        }
                                    }

                                    if (retValue && isTagAttrName)
                                    {
                                        // タグが閉じられていないのに最後まで来たエラー（例：<div></div async src）
                                        var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                                        AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                            $"/{curTagName}終了タグが閉じられていません。",
                                            "正しく終了タグを記載してください（※終了タグに属性を記載するのは正しい書式ではありません）。",
                                            errorHtmlCode);
                                        retValue = false;
                                        break; // タグの属性（Attribute）部分処理のループを抜ける
                                    }

                                }
                                else if (IsAttrValueDoubleLetterOfTag(nextChar))
                                {
                                    ParseAttributeValue(ref retValue, sbErrorInfo, htmlCode, nextChar, currentLineNumber, currentColumnNumber, length, curTagName, out isTagAttrAssign, out isTagAttrValue, ref curAttrName, i, ref curChar, ref nextChar, ref n);
                                    break; // タグの属性（Attribute）部分処理のループを抜ける

                                }
                                else if (IsAttrValueSingleLetterOfTag(nextChar))
                                {
                                    ParseAttributeValue(ref retValue, sbErrorInfo, htmlCode, nextChar, currentLineNumber, currentColumnNumber, length, curTagName, out isTagAttrAssign, out isTagAttrValue, ref curAttrName, i, ref curChar, ref nextChar, ref n);
                                    break; // タグの属性（Attribute）部分処理のループを抜ける

                                }
                                else
                                {
                                    // 不正な文字で属性名が始まっているエラー（例：<div></div ="ja">）
                                    var errorHtmlCode = htmlCode.Substring(i, n - i + 1);  // 現在の文字まで
                                    AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                        $"/{curTagName}終了タグの属性がアルファベットによる名前でも「\"」や「'」による値でもなく不正な文字「{nextChar}」で始まっています。",
                                        "正しく終了タグの属性を記載してください（※終了タグに属性を記載するのは正しい書式ではありません）。",
                                        errorHtmlCode);
                                    retValue = false;
                                    break; // タグの属性（Attribute）部分処理のループを抜ける

                                }

                                if ((retValue == false) ||
                                    (isInsideTag == false))
                                {
                                    break; // タグの属性（Attribute）部分処理のループを抜ける
                                }
                            }

                        }
                        else
                        {
                            // 不正な文字で属性名が始まっているエラー（例：<div></div+>）
                            var errorHtmlCode = htmlCode.Substring(i, n - i + 1);  // 現在の文字まで
                            AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                $"/{curTagName}終了タグの属性がアルファベットによる名前でも「\"」や「'」による値でもなく不正な文字「{nextChar}」で始まっています。",
                                "正しく終了タグの属性を記載してください（※終了タグに属性を記載するのは正しい書式ではありません）。",
                                errorHtmlCode);
                            retValue = false;
                            break; // タグ処理のループを抜ける

                        }

                        if ((retValue == false) ||
                            (isInsideTag == false))
                        {
                            break; // タグ処理のループを抜ける
                        }
                    }

                }
                else
                {
                    // 不正な文字でタグ名が始まっているエラー（例：<div></+abc>）
                    var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                    AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                        $"終了タグがアルファベットではなく不正な文字「{nextChar}」で始まっています。",
                        "正しく終了タグを記載してください（※終了タグに属性を記載するのは正しい書式ではありません）。",
                        errorHtmlCode);
                    retValue = false;
                    //break; // タグ終了の関数を抜ける

                }

            }

            return retValue;
        }

        private static bool CheckTagHierarchy(bool isOpeningTag, string htmlCode, Stack<HtmlTagInfo> tagHierarchy, List<UnkownHtmlTag> listUnknownTag, int currentLineNumber, int currentColumnNumber, string curTagName, string curTagContent, int i, int n, StringBuilder sbErrorInfo)
        {
            var retValue = true;

            var curTagType = HtmlTagInfo.GetHtmlTagType(curTagName, isOpeningTag);
            // タグ判定のswitch-case
            switch (curTagType)
            {
                case HtmlTagType.UnknownOpening: // 未知の開始タグ
                    if (isOpeningTag)
                    {
                        // 未知の開始タグ名は未知リストに追加する
                        listUnknownTag.Add(new UnkownHtmlTag(curTagName, isOpeningTag, currentLineNumber, currentColumnNumber));

                        // タグ階層に追加する
                        tagHierarchy.Push(new HtmlTagInfo(curTagName, curTagType, curTagContent));

                    }
                    else
                    {
                        Debug.Assert(false, "ロジック的にはここには来ないはず");

                    }
                    break;

                case HtmlTagType.UnknownClosing: // 未知の終了タグ
                    if (isOpeningTag)
                    {
                        Debug.Assert(false, "ロジック的にはここには来ないはず");
                    }
                    else
                    {
                        // 別の未知の終了タグ名は未知リストに追加する
                        listUnknownTag.Add(new UnkownHtmlTag(curTagName, isOpeningTag, currentLineNumber, currentColumnNumber));

                        // タグ階層から1つ前のタグを取得して削除する
                        var existsHierarchy = (tagHierarchy.Count > 0);
                        var prevTag = existsHierarchy ? tagHierarchy.Pop() : null;
                        if ((existsHierarchy) && (prevTag != null) && (prevTag.Name.Equals(curTagName, StringComparison.OrdinalIgnoreCase)))
                        {
                            // 未知なタグが、正常に開始タグと終了タグのセットになっているなら、何もなかったように振る舞う

                        }
                        else
                        {
                            // 未知な終了タグが現れたエラー
                            //var prevTypeNamea = GetPrevTypeName(prevTag);
                            //var errorHtmlCodea = htmlCode.Substring(i, n - i);
                            //AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                            //    $"{prevTypeName}開始タグ<{prevTag.InsideContent}＞に対応する未知の終了タグ</{prevTag.Name}>があるべき場所に、" +
                            //    $"未知の終了タグ</{curTagName}>が存在し、タグ階層が破たんしています。" +
                            //    $"（＝適切なタグ階層に、終了タグに対応する「開始タグ」が存在しないようです）。",
                            //    "正しく開始タグと終了タグを記載してください。",
                            //    errorHtmlCode);
                            //retValue = false;
                            //break; // タグ判定のswitch-caseを抜ける


                            // 別の終了タグが現れたエラー
                            var trimedClosingToken = curTagContent.TrimEnd(new char[] { '>', ' ' });
                            if (trimedClosingToken[trimedClosingToken.Length - 1] == '/')
                            {
                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                    $"未知のタグ <{curTagName}> が単独で使われています。\n" +
                                    "（＝例えば、「<unknown/>」のようにして、XHTMLの単独タグのように使われています。）",
                                    "正しく開始タグと終了タグを記載してください。\n" +
                                    $"単体で利用するタグの場合は、<{curTagName}> のように「/」で閉じずに使ってください。",
                                    curTagContent);
                            }
                            else if (prevTag == null)
                            {
                                var prevTypeName = GetPrevTypeName(prevTag);
                                var errorHtmlCode = htmlCode.Substring(0, n + 1);
                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                    $"未知の「終了タグ </{curTagName}> 」に対応する\n" +
                                    $"　「開始タグ <{curTagName}> 」が存在しません。タグ階層が破たんしています。\n" +
                                    "（＝適切なタグ階層に、「開始タグ」と「終了タグ」が存在しないようです。）",
                                    "正しく開始タグと終了タグを記載してください。",
                                    errorHtmlCode);
                            }
                            else
                            {
                                var prevTypeName = GetPrevTypeName(prevTag);
                                var errorHtmlCode = htmlCode.Substring(0, n + 1);
                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                    $"{prevTypeName}、「開始タグ {prevTag.InsideContent} 」に対応する\n" +
                                    $"　「終了タグ </{prevTag.Name}> 」があるべき場所に、\n" +
                                    $"　「終了タグ </{curTagName}> 」が存在し、タグ階層が破たんしています。\n" +
                                    "（＝適切なタグ階層に、「開始タグ」と「終了タグ」が存在しないようです。）",
                                    "正しく開始タグと終了タグを記載してください。\n" +
                                    $"「開始タグ {prevTag.InsideContent} 」～「終了タグ </{curTagName}> 」の間に、\n" +
                                    $"　不要な「開始タグ <{prevTag.Name}> 」か「開始タグ <{curTagName}> 」が存在する、\n" +
                                    $"　不要な「終了タグ </{prevTag.Name}> 」か「終了タグ </{curTagName}> 」が存在する、\n" +
                                    $"　必要な「終了タグ </{prevTag.Name}> 」を書き忘れている、\n" +
                                    "といった可能性が高いです。まずはこれらを中心に問題箇所を探してください。",
                                    errorHtmlCode);
                            }
                            retValue = false;
                            break; // タグ判定のswitch-caseを抜ける

                        }

                    }
                    break;

                case HtmlTagType.SingleOpening:  // 単体の開始タグ
                    if (isOpeningTag)
                    {
                        // シングルタグは、タグ階層には残さないので、ここでは何もしない

                    }
                    else
                    {
                        Debug.Assert(false, "ロジック的にはここには来ないはず");

                    }
                    break;

                case HtmlTagType.SingleClosing:  // 単体の終了タグ（違反）
                    if (isOpeningTag)
                    {
                        Debug.Assert(false, "ロジック的にはここには来ないはず");

                    }
                    else
                    {
                        // <br /> のようにXHTMLのような書き方だが、許容し、何もなかったように振る舞う

                    }
                    break;

                case HtmlTagType.SetOpening:     // 開始タグと終了タグのセットにおける、開始タグ
                    if (isOpeningTag)
                    {
                        // タグ階層に追加する
                        tagHierarchy.Push(new HtmlTagInfo(curTagName, curTagType, curTagContent));

                    }
                    else
                    {
                        Debug.Assert(false, "ロジック的にはここには来ないはず");

                    }
                    break;

                case HtmlTagType.SetClosing:      // 開始タグと終了タグのセットにおける、終了タグ
                    if (isOpeningTag)
                    {
                        Debug.Assert(false, "ロジック的にはここには来ないはず");

                    }
                    else
                    {

                        // タグ階層から1つ前のタグを取得して削除する
                        var existsHierarchy = (tagHierarchy.Count > 0);
                        var prevTag = existsHierarchy ? tagHierarchy.Pop() : null;
                        if ((existsHierarchy) && (prevTag != null) && (prevTag.Name.Equals(curTagName, StringComparison.OrdinalIgnoreCase)))
                        {
                            // セットのタグが、正常に開始タグと終了タグのセットになっているなら、何もなかったように振る舞う

                        }
                        else
                        {
                            // 別の終了タグが現れたエラー
                            var trimedClosingToken = curTagContent.TrimEnd(new char[] { '>', ' ' });
                            if (trimedClosingToken[trimedClosingToken.Length - 1] == '/')
                            {
                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                    $"「開始タグ」と「終了タグ」をセットで利用するタグ <{curTagName}> が単独で使われています。\n" +
                                    "（＝例えば、「<div/>」のようにして、XHTMLの単独タグのように使われています。）",
                                    "正しく開始タグと終了タグを記載してください。",
                                    curTagContent);
                            }
                            else if (prevTag == null)
                            {
                                var prevTypeName = GetPrevTypeName(prevTag);
                                var errorHtmlCode = htmlCode.Substring(0, n + 1);
                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                    $"セット利用の「終了タグ </{curTagName}> 」に対応する\n" +
                                    $"　「開始タグ <{curTagName}> 」が存在しません。タグ階層が破たんしています。\n" +
                                    "（＝適切なタグ階層に、「開始タグ」と「終了タグ」が存在しないようです。）",
                                    "正しく開始タグと終了タグを記載してください。",
                                    errorHtmlCode);
                            }
                            else
                            {
                                var prevTypeName = GetPrevTypeName(prevTag);
                                var errorHtmlCode = htmlCode.Substring(0, n + 1);
                                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                                    $"{prevTypeName}、「開始タグ {prevTag.InsideContent} 」に対応する\n" +
                                    $"　「終了タグ </{prevTag.Name}> 」があるべき場所に、\n" +
                                    $"　「終了タグ </{curTagName}> 」が存在し、タグ階層が破たんしています。\n" +
                                    "（＝適切なタグ階層に、「開始タグ」と「終了タグ」が存在しないようです。）",
                                    "正しく開始タグと終了タグを記載してください。\n" +
                                    $"「開始タグ {prevTag.InsideContent} 」～「終了タグ </{curTagName}> 」の間に、\n" +
                                    $"　不要な「開始タグ <{prevTag.Name}> 」か「開始タグ <{curTagName}> 」が存在する、\n" +
                                    $"　不要な「終了タグ </{prevTag.Name}> 」か「終了タグ </{curTagName}> 」が存在する、\n" +
                                    $"　必要な「終了タグ </{prevTag.Name}> 」を書き忘れている、\n" +
                                    "といった可能性が高いです。まずはこれらを中心に問題箇所を探してください。",
                                    errorHtmlCode);
                            }
                            retValue = false;
                            break; // タグ判定のswitch-caseを抜ける

                        }

                    }
                    break;
            }

            return retValue;
        }

        public static string GetPrevTypeName(HtmlTagInfo prevTag)
        {
            if (prevTag == null) return "＜前階層のタグはありません＞";
            var prevTypeName = String.Empty;
            switch (prevTag.TagType)
            {
                case HtmlTagType.UnknownOpening: // 未知の開始タグ
                case HtmlTagType.UnknownClosing: // 未知の終了タグ
                    prevTypeName = "未知の";
                    break;

                case HtmlTagType.SingleOpening:  // 単体の開始タグ
                case HtmlTagType.SingleClosing:  // 単体の終了タグ（違反）
                    prevTypeName = "単体利用の";
                    break;

                case HtmlTagType.SetOpening:     // 開始タグと終了タグのセットにおける、開始タグ
                case HtmlTagType.SetClosing:      // 開始タグと終了タグのセットにおける、終了タグ
                    prevTypeName = "セット利用の";
                    break;
            }
            return prevTypeName;
        }

        private static void ParseAttributeValue(ref bool retValue, StringBuilder sbErrorInfo, string htmlCode, char targetChar, int currentLineNumber, int currentColumnNumber, int length, string curTagName, out bool isTagAttrAssign, out bool isTagAttrValue, ref string curAttrName, int i, ref char curChar, ref char nextChar, ref int n)
        {
#if DEBUG
            var curAttrValue = String.Empty; // 現在の属性値（使用しないのでカット）
#endif
            isTagAttrAssign = false;  //  属性割り当て解除
            isTagAttrValue = true;    // 属性値開始

            // 属性値の取得
            var toEscapeNextChar = false;
            var indexAttrValue = n;    // 開始「"」を含める
            for (n = indexAttrValue + 1; n < length; n++)
            {
                // その次のインデックスにある文字を取得
                nextChar = htmlCode[n];

                if (toEscapeNextChar)
                {
                    toEscapeNextChar = false;
                    continue;  // 後続の処理はスキップ
                }
                else if (IsAttrValueEscapeLetterOfTag(nextChar))
                {
                    toEscapeNextChar = true;
                    continue;  // 後続の処理はスキップ
                }
                else if (((targetChar == '"') && IsAttrValueDoubleLetterOfTag(nextChar)) ||
                         ((targetChar == '\'') && IsAttrValueSingleLetterOfTag(nextChar)))
                {
                    // 後続の文字が「"」になったら、属性値を完成させる
#if DEBUG
                    curAttrValue = htmlCode.Substring(indexAttrValue, n - indexAttrValue + 1);  // 終了「"」を含める
#endif
                    curAttrName = String.Empty;  // 属性名を空にしてリセット
                                                 //i = n;                     // 処理を最後の文字「"」まで進める
                    curChar = nextChar;          // 現在の文字を念のため再設定。その次の文字は、次のループで処理する
                    isTagAttrValue = false;      // 属性値終了
                    break;
                }
                else
                {
                    continue;  // 後続の処理はスキップ
                }
            }

            if (isTagAttrValue)
            {
                // 属性値の閉じがないエラー（例：<img src="aaa>...）
                var errorHtmlCode = htmlCode.Substring(i);  // 最後まで
                AddToErrorInfo(sbErrorInfo, currentLineNumber, currentColumnNumber,
                    $"{curTagName}タグの{curAttrName}属性の値指定が「{targetChar}」で開始されましたが、「{targetChar}」で終了していません。",
                    "正しく属性値を記入してください。",
                    errorHtmlCode);
                retValue = false;

            }
        }

    }
}
