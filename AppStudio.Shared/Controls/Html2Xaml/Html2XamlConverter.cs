using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace AppStudio.Controls.Html2Xaml
{
    public class Html2XamlConverter
    {
        private readonly string[] prependParagraph = { "img", "b", "strong", "em", "i", "u", "a", "br", "table", "span", "div", "blockquote", "#text" };

        private StringBuilder stringBuilder;

        public Html2XamlConverter()
        {
            stringBuilder = new StringBuilder();
        }

        public static string ConvertToXaml(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }

            html = PreprocessEntities(html);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            return FormatXml(new Html2XamlConverter().ConvertHtml(document));
        }

        internal string ConvertHtml(HtmlDocument document)
        {
            stringBuilder.Append(@"<RichTextBlock xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">"
                + @"<RichTextBlock.Resources><Style x:Key=""ViewboxImageStyle"" TargetType=""Viewbox"">"
                + @"<Setter Property=""Stretch""  Value=""Uniform""/><Setter Property=""StretchDirection"" Value=""DownOnly"" /><Setter Property=""MaxHeight""  Value=""200""/>"
                + @"<Setter Property=""MaxWidth""  Value=""300""/></Style></RichTextBlock.Resources>");

            foreach (var node in document.DocumentNode.ChildNodes)
            {
                ProcessNode(node, true);
            }

            stringBuilder.Append("</RichTextBlock>");

            return stringBuilder.ToString();
        }

        private void ProcessNode(HtmlNode node, bool isRoot = false)
        {
            string nodeName = node.Name.ToLower();
            if (isRoot && prependParagraph.Contains(nodeName))
            {
                stringBuilder.Append("<Paragraph LineStackingStrategy=\"MaxHeight\">");
            }

            switch (node.Name.ToLower())
            {
                case "#text":
                    stringBuilder.Append(EncodeText(node.InnerText));
                    break;
                case "span":
                    CreateSpan(node);
                    break;
                case "div":
                    CreateDiv(node);
                    break;
                case "p":
                    CreateParagraph(node, isRoot);
                    break;
                case "blockquote":
                    CreateBlockquote(node);
                    break;
                case "ol":
                    CreateOrderedList(node, isRoot);
                    break;
                case "ul":
                    CreateUnorderedList(node, isRoot);
                    break;
                case "table":
                    CreateTable(node);
                    break;
                case "em":
                case "b":
                case "strong":
                    CreateBold(node);
                    break;
                case "i":
                    CreateItalic(node);
                    break;
                case "u":
                    CreateUnderline(node);
                    break;
                case "a":
                    CreateAnchor(node);
                    break;
                case "img":
                    CreateImage(node);
                    break;
                case "br":
                    CreateLineBreak();
                    break;
                case "h1":
                    CreateHeading(node, 36, isRoot);
                    break;
                case "h2":
                    CreateHeading(node, 33, isRoot);
                    break;
                case "h3":
                    CreateHeading(node, 29, isRoot);
                    break;
                case "h4":
                    CreateHeading(node, 26, isRoot);
                    break;
                case "h5":
                    CreateHeading(node, 24, isRoot);
                    break;
                case "h6":
                    CreateHeading(node, 22, isRoot);
                    break;
            }

            if (isRoot && prependParagraph.Contains(nodeName))
            {
                stringBuilder.Append("</Paragraph>");
            }
        }

        private void ProcessChildNodes(IEnumerable<HtmlNode> childNodes, bool isRoot = false)
        {
            foreach (var childNode in childNodes)
            {
                ProcessNode(childNode, isRoot);
            }
        }

        #region Node creation methods
        private void CreateSpan(HtmlNode node)
        {
            stringBuilder.Append("<Span>");
            ProcessChildNodes(node.ChildNodes);
            stringBuilder.Append("</Span>");
        }

        private void CreateHeading(HtmlNode node, int fontSize, bool isRoot)
        {
            if (isRoot)
            {
                stringBuilder.AppendFormat("<Paragraph LineStackingStrategy=\"MaxHeight\" FontSize=\"{0}\">", fontSize);
                ProcessChildNodes(node.ChildNodes);
                stringBuilder.Append("</Paragraph>");
            }
            else
            {
                stringBuilder.AppendFormat("<LineBreak/><Run FontSize=\"{0}\">", fontSize);
                ProcessChildNodes(node.Descendants("#text"));
                stringBuilder.Append("</Run><LineBreak/>");
            }
        }

        private void CreateLineBreak()
        {
            stringBuilder.Append("<LineBreak />");
        }

        private void CreateUnderline(HtmlNode node)
        {
            stringBuilder.Append("<Underline>");
            ProcessChildNodes(node.ChildNodes);
            stringBuilder.Append("</Underline>");
        }

        private void CreateItalic(HtmlNode node)
        {
            stringBuilder.Append("<Italic>");
            ProcessChildNodes(node.ChildNodes);
            stringBuilder.Append("</Italic>");
        }

        private void CreateBold(HtmlNode node)
        {
            stringBuilder.Append("<Bold>");
            ProcessChildNodes(node.ChildNodes);
            stringBuilder.Append("</Bold>");
        }

        private void CreateBlockquote(HtmlNode node)
        {
            stringBuilder.Append("<LineBreak/><InlineUIContainer><RichTextBlock Margin=\"20,0,0,0\" FontStyle=\"Italic\">");
            ProcessChildNodes(node.ChildNodes, true);
            stringBuilder.Append("</RichTextBlock></InlineUIContainer>");
        }

        private void CreateParagraph(HtmlNode node, bool isRoot)
        {
            if (isRoot)
            {
                stringBuilder.Append("<Paragraph LineStackingStrategy=\"MaxHeight\">");
                ProcessChildNodes(node.ChildNodes);
                stringBuilder.Append("</Paragraph><Paragraph/>");
            }
            else
            {
                stringBuilder.Append("<LineBreak/>");
                ProcessChildNodes(node.ChildNodes);
                stringBuilder.Append("<LineBreak/>");
            }
        }

        private void CreateDiv(HtmlNode node)
        {
            stringBuilder.Append("<Span>");
            ProcessChildNodes(node.ChildNodes);
            stringBuilder.Append("</Span>");
        }

        private void CreateImage(HtmlNode node)
        {
            var navigateUrl = node.GetAttributeValue("src", string.Empty);
            if (navigateUrl != null)
            {
                stringBuilder.Append("<InlineUIContainer><Viewbox Style=\"{StaticResource ViewboxImageStyle}\">");
                stringBuilder.AppendFormat("<Image Source=\"{0}\" />", EncodeUrl(navigateUrl));
                stringBuilder.Append("</Viewbox></InlineUIContainer>");
            }
        }

        private void CreateAnchor(HtmlNode node)
        {
            var navigateUrl = node.GetAttributeValue("href", string.Empty);
            if (!string.IsNullOrEmpty(navigateUrl))
            {
                var img = node.Descendants("img").FirstOrDefault();
                if (img != null)
                {
                    stringBuilder.AppendFormat("<InlineUIContainer><HyperlinkButton NavigateUri=\"{0}\">", EncodeUrl(navigateUrl));
                    stringBuilder.Append("<Viewbox Style=\"{StaticResource ViewboxImageStyle}\">");
                    stringBuilder.AppendFormat("<Image Source=\"{0}\"/>", EncodeUrl(img.GetAttributeValue("src", string.Empty)));
                    stringBuilder.Append("</Viewbox></HyperlinkButton></InlineUIContainer>");
                }
                else if (node.Descendants("#text").Count() > 0)
                {
                    stringBuilder.AppendFormat("<Hyperlink NavigateUri=\"{0}\" ", EncodeUrl(navigateUrl));
                    stringBuilder.Append(" FontWeight=\"Bold\" Foreground=\"{StaticResource AppForegroundColor}\"><Underline>");
                    ProcessChildNodes(node.Descendants("#text"));
                    stringBuilder.Append("</Underline></Hyperlink>");
                }
            }
        }

        private void CreateOrderedList(HtmlNode node, bool isRoot)
        {
            if (!isRoot)
            {
                stringBuilder.Append("<InlineUIContainer><RichTextBlock>");
            }

            int i = node.GetAttributeValue("start", 1);
            string generalType = node.GetAttributeValue("type", "1");
            foreach (var li in node.Descendants("li"))
            {
                i = li.GetAttributeValue("value", i);
                string countValue = i.ToString();
                switch (li.GetAttributeValue("type", generalType))
                {
                    case "A":
                        countValue = NumberToString(i).ToUpper();
                        break;
                    case "a":
                        countValue = NumberToString(i).ToLower();
                        break;
                    case "I":
                        countValue = NumberToRoman(i).ToUpper();
                        break;
                    case "i":
                        countValue = NumberToRoman(i).ToLower();
                        break;
                }
                stringBuilder.AppendFormat("<Paragraph Margin=\"20,0,0,0\"><Span><Bold>{0}.&#xA0;&#xA0;&#xA0;</Bold>", countValue);
                ProcessChildNodes(li.ChildNodes);
                stringBuilder.AppendFormat("</Span></Paragraph>");
                i++;
            }

            if (!isRoot)
            {
                stringBuilder.Append("<Paragraph/></RichTextBlock></InlineUIContainer>");
            }
            else
            {
                stringBuilder.AppendFormat("<Paragraph/>");
            }
        }

        private void CreateUnorderedList(HtmlNode node, bool isRoot)
        {
            if (!isRoot)
            {
                stringBuilder.Append("<InlineUIContainer><RichTextBlock>");
            }

            string generalType = node.GetAttributeValue("type", "disc");
            foreach (var li in node.Descendants("li"))
            {
                string typeValue = "&#x2022;";
                switch (li.GetAttributeValue("type", generalType))
                {
                    case "circle":
                        typeValue = "&#x25CB;";
                        break;
                    case "square":
                        typeValue = "&#x25A0;";
                        break;
                    case "disc":
                        typeValue = "&#x25CF;";
                        break;
                }
                stringBuilder.AppendFormat("<Paragraph Margin=\"20,0,0,0\"><Span>{0}&#xA0;&#xA0;&#xA0;", typeValue);
                ProcessChildNodes(li.ChildNodes);
                stringBuilder.AppendFormat("</Span></Paragraph>");
            }

            if (!isRoot)
            {
                stringBuilder.Append("<Paragraph/></RichTextBlock></InlineUIContainer>");
            }
            else
            {
                stringBuilder.AppendFormat("<Paragraph/>");
            }
        }

        private void CreateTable(HtmlNode node)
        {
            int maxColumns = 0;
            stringBuilder.Append("<InlineUIContainer><Grid>");
            stringBuilder.Append("<Grid.RowDefinitions>");
            var rowNodes = node.Descendants("tr");
            foreach (var rowNode in rowNodes)
            {
                int columns = rowNode.Descendants("th").Count();
                if (maxColumns < columns)
                {
                    maxColumns = columns;
                }
                columns = rowNode.Descendants("td").Count();
                if (maxColumns < columns)
                {
                    maxColumns = columns;
                }
                stringBuilder.Append("<RowDefinition/>");
            }
            stringBuilder.Append("</Grid.RowDefinitions>");
            stringBuilder.Append("<Grid.ColumnDefinitions>");
            for (int k = 0; k < maxColumns; k++)
            {
                stringBuilder.Append("<ColumnDefinition/>");
            }
            stringBuilder.Append("</Grid.ColumnDefinitions>");

            int i = 0;
            foreach (var rowNode in rowNodes)
            {
                int j = 0;
                foreach (var cellHeaderNode in rowNode.Descendants("th"))
                {
                    if (cellHeaderNode.HasChildNodes && cellHeaderNode.FirstChild.NodeType == HtmlNodeType.Text)
                    {
                        stringBuilder.AppendFormat("<TextBlock Grid.Row=\"{0}\" Grid.Column=\"{1}\" Text=\"{2}\" FontWeight=\"Bold\"/>", i, j, EncodeText(cellHeaderNode.FirstChild.InnerText.Trim()));
                    }
                    j++;
                }
                foreach (var cellNode in rowNode.Descendants("td"))
                {
                    if (cellNode.HasChildNodes && cellNode.FirstChild.NodeType == HtmlNodeType.Text)
                    {
                        stringBuilder.AppendFormat("<TextBlock Grid.Row=\"{0}\" Grid.Column=\"{1}\" Text=\"{2}\"/>", i, j, EncodeText(cellNode.FirstChild.InnerText.Trim()));
                    }
                    j++;
                }
                i++;
            }
            stringBuilder.Append("</Grid></InlineUIContainer>");
        }
        #endregion

        #region Auxiliar methods
        private string NumberToString(int number)
        {
            const int ColumnBase = 26;
            const int DigitMax = 7;
            const string Digits = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (number <= 0)
                return string.Empty;
            if (number <= ColumnBase)
                return Digits[number - 1].ToString();

            var sb = new StringBuilder().Append(' ', DigitMax);
            var current = number;
            var offset = DigitMax;
            while (current > 0)
            {
                sb[--offset] = Digits[--current % ColumnBase];
                current /= ColumnBase;
            }
            return sb.ToString(offset, DigitMax - offset);
        }

        private string NumberToRoman(int number)
        {
            StringBuilder result = new StringBuilder();
            int[] digitsValues = { 1, 4, 5, 9, 10, 40, 50, 90, 100, 400, 500, 900, 1000 };
            string[] romanDigits = { "I", "IV", "V", "IX", "X", "XL", "L", "XC", "C", "CD", "D", "CM", "M" };
            while (number > 0)
            {
                for (int i = digitsValues.Count() - 1; i >= 0; i--)
                    if (number / digitsValues[i] >= 1)
                    {
                        number -= digitsValues[i];
                        result.Append(romanDigits[i]);
                        break;
                    }
            }
            return result.ToString();
        }

        private static string PreprocessEntities(string html)
        {
            Dictionary<string, string> entities = new Dictionary<string, string>
                {
                    {"&quot;", "&#x0022;"}, {"&amp;", "&#x0026;"}, {"&apos;", "&#x0027;"}, {"&lt;", "&#x003C;"}, {"&gt;", "&#x003E;"}, {"&nbsp;", "&#x00A0;"}, {"&iexcl;", "&#x00A1;"},
                    {"&cent;", "&#x00A2;"}, {"&pound;", "&#x00A3;"}, {"&curren;", "&#x00A4;"}, {"&yen;", "&#x00A5;"}, {"&brvbar;", "&#x00A6;"}, {"&sect;", "&#x00A7;"}, {"&uml;", "&#x00A8;"},
                    {"&copy;", "&#x00A9;"}, {"&ordf;", "&#x00AA;"}, {"&laquo;", "&#x00AB;"}, {"&not;", "&#x00AC;"}, {"&shy;", "&#x00AD;"}, {"&reg;", "&#x00AE;"}, {"&macr;", "&#x00AF;"},
                    {"&deg;", "&#x00B0;"}, {"&plusmn;", "&#x00B1;"}, {"&sup2;", "&#x00B2;"}, {"&sup3;", "&#x00B3;"}, {"&acute;", "&#x00B4;"}, {"&micro;", "&#x00B5;"}, {"&para;", "&#x00B6;"}, 
                    {"&middot;", "&#x00B7;"}, {"&cedil;", "&#x00B8;"}, {"&sup1;", "&#x00B9;"}, {"&ordm;", "&#x00BA;"}, {"&raquo;", "&#x00BB;"}, {"&frac14;", "&#x00BC;"}, {"&frac12;", "&#x00BD;"},
                    {"&frac34;", "&#x00BE;"}, {"&iquest;", "&#x00BF;"}, {"&Agrave;", "&#x00C0;"}, {"&Aacute;", "&#x00C1;"}, {"&Acirc;", "&#x00C2;"}, {"&Atilde;", "&#x00C3;"}, {"&Auml;", "&#x00C4;"},
                    {"&Aring;", "&#x00C5;"}, {"&AElig;", "&#x00C6;"}, {"&Ccedil;", "&#x00C7;"}, {"&Egrave;", "&#x00C8;"}, {"&Eacute;", "&#x00C9;"}, {"&Ecirc;", "&#x00CA;"}, {"&Euml;", "&#x00CB;"}, 
                    {"&Igrave;", "&#x00CC;"}, {"&Iacute;", "&#x00CD;"}, {"&Icirc;", "&#x00CE;"}, {"&Iuml;", "&#x00CF;"}, {"&ETH;", "&#x00D0;"}, {"&Ntilde;", "&#x00D1;"}, {"&Ograve;", "&#x00D2;"}, 
                    {"&Oacute;", "&#x00D3;"}, {"&Ocirc;", "&#x00D4;"}, {"&Otilde;", "&#x00D5;"},{"&Ouml;", "&#x00D6;"}, {"&times;", "&#x00D7;"}, {"&Oslash;", "&#x00D8;"}, {"&Ugrave;", "&#x00D9;"},
                    {"&Uacute;", "&#x00DA;"}, {"&Ucirc;", "&#x00DB;"}, {"&Uuml;", "&#x00DC;"}, {"&Yacute;", "&#x00DD;"}, {"&THORN;", "&#x00DE;"}, {"&szlig;", "&#x00DF;"}, {"&agrave;", "&#x00E0;"},
                    {"&aacute;", "&#x00E1;"}, {"&acirc;", "&#x00E2;"}, {"&atilde;", "&#x00E3;"}, {"&auml;", "&#x00E4;"}, {"&aring;", "&#x00E5;"}, {"&aelig;", "&#x00E6;"}, {"&ccedil;", "&#x00E7;"}, 
                    {"&egrave;", "&#x00E8;"},  {"&eacute;", "&#x00E9;"}, {"&ecirc;", "&#x00EA;"}, {"&euml;", "&#x00EB;"}, {"&igrave;", "&#x00EC;"}, {"&iacute;", "&#x00ED;"}, {"&icirc;", "&#x00EE;"},
                    {"&iuml;", "&#x00EF;"}, {"&eth;", "&#x00F0;"}, {"&ntilde;", "&#x00F1;"}, {"&ograve;", "&#x00F2;"}, {"&oacute;", "&#x00F3;"}, {"&ocirc;", "&#x00F4;"}, {"&otilde;", "&#x00F5;"},
                    {"&ouml;", "&#x00F6;"}, {"&divide;", "&#x00F7;"}, {"&oslash;", "&#x00F8;"}, {"&ugrave;", "&#x00F9;"}, {"&uacute;", "&#x00FA;"}, {"&ucirc;", "&#x00FB;"}, {"&uuml;", "&#x00FC;"},
                    {"&yacute;", "&#x00FD;"}, {"&thorn;", "&#x00FE;"}, {"&yuml;", "&#x00FF;"}, {"&OElig;", "&#x0152;"}, {"&oelig;", "&#x0153;"}, {"&Scaron;", "&#x0160;"}, {"&scaron;", "&#x0161;"},
                    {"&Yuml;", "&#x0178;"}, {"&fnof;", "&#x0192;"}, {"&circ;", "&#x02C6;"},  {"&tilde;", "&#x02DC;"}, {"&Alpha;", "&#x0391;"}, {"&Beta;", "&#x0392;"}, {"&Gamma;", "&#x0393;"}, {"&Delta;", "&#x0394;"},
                    {"&Epsilon;", "&#x0395;"}, {"&Zeta;", "&#x0396;"}, {"&Eta;", "&#x0397;"}, {"&Theta;", "&#x0398;"}, {"&Iota;", "&#x0399;"}, {"&Kappa;", "&#x039A;"}, {"&Lambda;", "&#x039B;"}, {"&Mu;", "&#x039C;"},
                    {"&Nu;", "&#x039D;"}, {"&Xi;", "&#x039E;"}, {"&Omicron;", "&#x039F;"}, {"&Pi;", "&#x03A0;"},  {"&Rho;", "&#x03A1;"},  {"&Sigma;", "&#x03A3;"}, {"&Tau;", "&#x03A4;"}, {"&Upsilon;", "&#x03A5;"},
                    {"&Phi;", "&#x03A6;"}, {"&Chi;", "&#x03A7;"}, {"&Psi;", "&#x03A8;"}, {"&Omega;", "&#x03A9;"}, {"&alpha;", "&#x03B1;"}, {"&beta;", "&#x03B2;"}, {"&gamma;", "&#x03B3;"}, {"&delta;", "&#x03B4;"},
                    {"&epsilon;", "&#x03B5;"}, {"&zeta;", "&#x03B6;"}, {"&eta;", "&#x03B7;"}, {"&theta;", "&#x03B8;"}, {"&iota;", "&#x03B9;"}, {"&kappa;", "&#x03BA;"}, {"&lambda;", "&#x03BB;"}, {"&mu;", "&#x03BC;"},
                    {"&nu;", "&#x03BD;"}, {"&xi;", "&#x03BE;"}, {"&omicron;", "&#x03BF;"}, {"&pi;", "&#x03C0;"}, {"&rho;", "&#x03C1;"}, {"&sigmaf;", "&#x03C2;"}, {"&sigma;", "&#x03C3;"}, {"&tau;", "&#x03C4;"},
                    {"&upsilon;", "&#x03C5;"}, {"&phi;", "&#x03C6;"}, {"&chi;", "&#x03C7;"}, {"&psi;", "&#x03C8;"}, {"&omega;", "&#x03C9;"}, {"&thetasym;", "&#x03D1;"}, {"&upsih;", "&#x03D2;"}, {"&piv;", "&#x03D6;"},
                    {"&ensp;", "&#x2002;"}, {"&emsp;", "&#x2003;"}, {"&thinsp;", "&#x2009;"}, {"&zwnj;", "&#x200C;"}, {"&zwj;", "&#x200D;"}, {"&lrm;", "&#x200E;"}, {"&rlm;", "&#x200F;"}, {"&ndash;", "&#x2013;"},
                    {"&mdash;", "&#x2014;"}, {"&lsquo;", "&#x2018;"}, {"&rsquo;", "&#x2019;"}, {"&sbquo;", "&#x201A;"}, {"&ldquo;", "&#x201C;"}, {"&rdquo;", "&#x201D;"}, {"&bdquo;", "&#x201E;"}, {"&dagger;", "&#x2020;"},
                    {"&Dagger;", "&#x2021;"}, {"&bull;", "&#x2022;"}, {"&hellip;", "&#x2026;"}, {"&permil;", "&#x2030;"}, {"&prime;", "&#x2032;"}, {"&Prime;", "&#x2033;"}, {"&lsaquo;", "&#x2039;"}, {"&rsaquo;", "&#x203A;"},
                    {"&oline;", "&#x203E;"}, {"&frasl;", "&#x2044;"}, {"&euro;", "&#x20AC;"}, {"&image;", "&#x2111;"}, {"&weierp;", "&#x2118;"}, {"&real;", "&#x211C;"}, {"&trade;", "&#x2122;"}, {"&alefsym;", "&#x2135;"},
                    {"&larr;", "&#x2190;"}, {"&uarr;", "&#x2191;"}, {"&rarr;", "&#x2192;"}, {"&darr;", "&#x2193;"}, {"&harr;", "&#x2194;"}, {"&crarr;", "&#x21B5;"}, {"&lArr;", "&#x21D0;"}, {"&uArr;", "&#x21D1;"},
                    {"&rArr;", "&#x21D2;"}, {"&dArr;", "&#x21D3;"}, {"&hArr;", "&#x21D4;"}, {"&forall;", "&#x2200;"}, {"&part;", "&#x2202;"}, {"&exist;", "&#x2203;"}, {"&empty;", "&#x2205;"}, {"&nabla;", "&#x2207;"},
                    {"&isin;", "&#x2208;"}, {"&notin;", "&#x2209;"}, {"&ni;", "&#x220B;"},  {"&prod;", "&#x220F;"}, {"&sum;", "&#x2211;"}, {"&minus;", "&#x2212;"}, {"&lowast;", "&#x2217;"}, {"&radic;", "&#x221A;"},
                    {"&prop;", "&#x221D;"}, {"&infin;", "&#x221E;"}, {"&ang;", "&#x2220;"}, {"&and;", "&#x2227;"}, {"&or;", "&#x2228;"},  {"&cap;", "&#x2229;"}, {"&cup;", "&#x222A;"}, {"&int;", "&#x222B;"},
                    {"&there4;", "&#x2234;"}, {"&sim;", "&#x223C;"}, {"&cong;", "&#x2245;"}, {"&asymp;", "&#x2248;"}, {"&ne;", "&#x2260;"}, {"&equiv;", "&#x2261;"}, {"&le;", "&#x2264;"}, {"&ge;", "&#x2265;"}, 
                    {"&sub;", "&#x2282;"}, {"&sup;", "&#x2283;"}, {"&nsub;", "&#x2284;"}, {"&sube;", "&#x2286;"}, {"&supe;", "&#x2287;"}, {"&oplus;", "&#x2295;"}, {"&otimes;", "&#x2297;"}, {"&perp;", "&#x22A5;"},
                    {"&sdot;", "&#x22C5;"}, {"&vellip;", "&#x22EE;"}, {"&lceil;", "&#x2308;"}, {"&rceil;", "&#x2309;"}, {"&lfloor;", "&#x230A;"}, {"&rfloor;", "&#x230B;"}, {"&lang;", "&#x2329;"}, {"&rang;", "&#x232A;"},
                    {"&loz;", "&#x25CA;"},  {"&spades;", "&#x2660;"}, {"&clubs;", "&#x2663;"}, {"&hearts;", "&#x2665;"}, {"&diams;", "&#x2666;"}};

            foreach (var entity in entities)
            {
                html = html.Replace(entity.Key, entity.Value);
            }

            return html;
        }

        private static string EncodeUrl(string url)
        {
            int pos = url.IndexOf("?");
            if (pos > 0)
            {
                url = url.Substring(0, pos);
            }
            return EncodeText(url);
        }

        private static string EncodeText(string text)
        {
            return Regex.Replace(text, "&(?!(amp)|(lt)|(apos)|(gt)|(quot)|(#x.{1,4})|(#.{1,4});)", "&amp;")
                .Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        private static string FormatXml(string xml)
        {
            try
            {
                XDocument doc = XDocument.Parse(xml);
                return doc.ToString();
            }
            catch (Exception)
            {
                return xml;
            }
        }
        #endregion
    }
}
