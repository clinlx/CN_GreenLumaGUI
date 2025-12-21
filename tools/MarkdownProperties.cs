using CN_GreenLumaGUI.Models;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CN_GreenLumaGUI.tools
{
    public static class MarkdownProperties
    {
        public static readonly DependencyProperty MarkdownModelProperty =
            DependencyProperty.RegisterAttached(
                "MarkdownModel",
                typeof(TextItemModel),
                typeof(MarkdownProperties),
                new PropertyMetadata(null, OnMarkdownModelChanged));

        public static TextItemModel GetMarkdownModel(DependencyObject obj)
        {
            return (TextItemModel)obj.GetValue(MarkdownModelProperty);
        }

        public static void SetMarkdownModel(DependencyObject obj, TextItemModel value)
        {
            obj.SetValue(MarkdownModelProperty, value);
        }

        private static void OnMarkdownModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock && e.NewValue is TextItemModel model)
            {
                textBlock.Inlines.Clear();

                foreach (var span in model.Spans)
                {
                    Inline inline;
                    if (span.Type == SpanType.Link)
                    {
                        var link = new Hyperlink(new Run(span.Text));
                        try
                        {
                            if (!string.IsNullOrEmpty(span.Url))
                            {
                                link.NavigateUri = new System.Uri(span.Url);
                            }
                        }
                        catch
                        {
                            // Fallback if URL is invalid
                            link.NavigateUri = null;
                        }
                        
                        link.RequestNavigate += (sender, args) =>
                        {
                            try
                            {
                                if (args.Uri != null)
                                {
                                    Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri) { UseShellExecute = true });
                                }
                            }
                            catch { }
                            args.Handled = true;
                        };
                        inline = link;
                    }
                    else
                    {
                        var run = new Run(span.Text);
                        if (span.Type == SpanType.Bold)
                        {
                            run.FontWeight = FontWeights.Bold;
                        }
                        else if (span.Type == SpanType.Italic)
                        {
                            run.FontStyle = FontStyles.Italic;
                        }
                        else if (span.Type == SpanType.StrikeThrough)
                        {
                            run.TextDecorations = TextDecorations.Strikethrough;
                        }
                        inline = run;
                    }
                    textBlock.Inlines.Add(inline);
                }
            }
        }
    }
}
