namespace HtmlValidation
{
    public class UnkownHtmlTag
    {
        // タグの名前と属性群
        public string Name { get; set; }

        // 開始タグかづか
        public bool IsOpening { get; set; }

        // 行番号
        public int LineNumber { get; set; }

        // 列番号
        public int ColumnNumber { get; set; }

        private UnkownHtmlTag()
        {
            // コンストラクターの使用方法を制限
        }

        public UnkownHtmlTag(string tagName, bool isOpeningTag, int currentLineNumber, int currentColumnNumber)
        {
            this.Name = tagName;
            this.IsOpening = isOpeningTag;
            this.LineNumber = currentLineNumber;
            this.ColumnNumber = currentColumnNumber;
        }
    }
}