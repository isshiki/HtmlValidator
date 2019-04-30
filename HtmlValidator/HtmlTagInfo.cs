using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlValidation
{
    public class HtmlTagInfo
    {

        // タグの名前
        public string Name { get; set; }

        // タグ種別
        public HtmlTagType TagType { get; set; }


        private HtmlTagInfo()
        {
            // コンストラクターの使用方法を制限
        }

        public HtmlTagInfo(string tagName, HtmlTagType tagType)
        {
            this.Name = tagName;
            this.TagType = tagType;
        }

        public static HtmlTagType GetHtmlTagType(string tagName, bool isOpening)
        {
            switch (tagName)
            {
                case "area":
                case "base":
                case "br":
                case "col":
                case "command":
                case "embed":
                case "hr":
                case "img":
                case "input":
                case "keygen":
                case "link":
                case "meta":
                case "param":
                case "source":
                case "track":
                case "wbr":
                    if (isOpening)
                    {
                        return HtmlTagType.SingleOpening;
                    }
                    else
                    {
                        return HtmlTagType.SingleClosing; // 違反
                    }
                //break;

                case "html":
                case "head":
                case "title":
                case "style":
                case "script":
                case "noscript":
                case "body":
                case "section":
                case "nav":
                case "article":
                case "aside":
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                case "hgroup":
                case "header":
                case "footer":
                case "address":
                case "p":
                case "pre":
                case "blockquote":
                case "ol":
                case "ul":
                case "li":
                case "dl":
                case "dt":
                case "dd":
                case "figure":
                case "figcaption":
                case "div":
                case "a":
                case "em":
                case "strong":
                case "small":
                case "s":
                case "cite":
                case "q":
                case "dfn":
                case "abbr":
                case "time":
                case "code":
                case "var":
                case "samp":
                case "kbd":
                case "sub":
                case "sup":
                case "i":
                case "b":
                case "u":
                case "mark":
                case "ruby":
                case "rb":
                case "rt":
                case "rp":
                case "bdo":
                case "bdi":
                case "span":
                case "ins":
                case "del":
                case "iframe":
                case "object":
                case "video":
                case "audio":
                case "canvas":
                case "map":
                case "table":
                case "caption":
                case "colgroup":
                case "tbody":
                case "thead":
                case "tfoot":
                case "tr":
                case "td":
                case "th":
                case "form":
                case "fieldset":
                case "legend":
                case "label":
                case "button":
                case "select":
                case "datalist":
                case "optgroup":
                case "option":
                case "textarea":
                case "output":
                case "progress":
                case "meter":
                case "details":
                case "summary":
                case "menu":
                case "font":
                    if (isOpening)
                    {
                        return HtmlTagType.SetOpening;
                    }
                    else
                    {
                        return HtmlTagType.SetClosing;
                    }
                //break;

                default:
                    if (isOpening)
                    {
                        return HtmlTagType.UnknownOpening;
                    }
                    else
                    {
                        return HtmlTagType.UnknownClosing;
                    }
                    //break;
            }
        }
    }
}
