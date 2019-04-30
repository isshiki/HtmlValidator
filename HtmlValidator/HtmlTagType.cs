namespace HtmlValidation
{
    public enum HtmlTagType
    {
        UnknownOpening, // 未知の開始タグ
        UnknownClosing, // 未知の終了タグ
        SingleOpening,  // 単体の開始タグ
        SingleClosing,  // 単体の終了タグ（違反）
        SetOpening,     // 開始タグと終了タグのセットにおける、開始タグ
        SetClosing      // 開始タグと終了タグのセットにおける、終了タグ
    }

}
