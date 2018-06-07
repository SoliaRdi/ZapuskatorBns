using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Zapuskator
{
    public class RichTextBoxEx : TextBox
    {
        // Using a DependencyProperty as the backing store for AutoAddWhiteSpaceAfterTriggered.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoAddWhiteSpaceAfterTriggeredProperty =
            DependencyProperty.Register("AutoAddWhiteSpaceAfterTriggered", typeof(bool), typeof(RichTextBoxEx), new UIPropertyMetadata(true));

        // Using a DependencyProperty as the backing store for ContentAssistSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContentAssistSourceProperty =
            DependencyProperty.Register("ContentAssistSource", typeof(IList<string>), typeof(RichTextBoxEx), new UIPropertyMetadata(new List<string>()));

        // Using a DependencyProperty as the backing store for ContentAssistSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContentAssistTriggersProperty =
            DependencyProperty.Register("ContentAssistTriggers", typeof(IList<char>), typeof(RichTextBoxEx), new UIPropertyMetadata(new List<char>()));

        public bool AutoAddWhiteSpaceAfterTriggered
        {
            get => (bool) GetValue(AutoAddWhiteSpaceAfterTriggeredProperty);
            set => SetValue(AutoAddWhiteSpaceAfterTriggeredProperty, value);
        }

        public IList<string> ContentAssistSource
        {
            get => (IList<string>) GetValue(ContentAssistSourceProperty);
            set => SetValue(ContentAssistSourceProperty, value);
        }

        public IList<char> ContentAssistTriggers
        {
            get => (IList<char>) GetValue(ContentAssistTriggersProperty);
            set => SetValue(ContentAssistTriggersProperty, value);
        }

        #region Insert Text

        public void InsertText(string text)
        {
            var temp = CaretIndex;
            Text = Text.Insert(CaretIndex, text);
            Focus();
            CaretIndex = temp + text.Length;
            //TextPointer pointer = CaretPosition.GetPositionAtOffset(text.Length);
            //if (pointer != null)
            //{
            //    CaretPosition = pointer;
            //}
        }

        #endregion

        #region constructure

        public RichTextBoxEx()
        {
            Loaded += RichTextBoxEx_Loaded;
        }

        private void RichTextBoxEx_Loaded(object sender, RoutedEventArgs e)
        {
            //init the assist list box
            if (Parent.GetType() != typeof(Grid)) throw new Exception("this control must be put in Grid control");

            if (ContentAssistTriggers.Count == 0) ContentAssistTriggers.Add('@');
            try
            {
                (Parent as Grid).Children.Add(AssistListBox);
            }
            catch (Exception)
            {
            }

            AssistListBox.MaxHeight = 100;
            AssistListBox.MinWidth = 100;
            AssistListBox.HorizontalAlignment = HorizontalAlignment.Left;
            AssistListBox.VerticalAlignment = VerticalAlignment.Top;
            AssistListBox.Visibility = Visibility.Collapsed;
            AssistListBox.MouseDoubleClick += AssistListBox_MouseDoubleClick;
            AssistListBox.PreviewKeyDown += AssistListBox_PreviewKeyDown;
        }

        private void AssistListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if Enter\Tab\Space key is pressed, insert current selected item to richtextbox
            if (e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Space)
            {
                InsertAssistWord();
                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                //Baskspace key is pressed, set focus to richtext box
                if (sbLastWords.Length >= 1) sbLastWords.Remove(sbLastWords.Length - 1, 1);

                Focus();
            }
        }

        private void AssistListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            InsertAssistWord();
        }

        private bool InsertAssistWord()
        {
            var isInserted = false;
            if (AssistListBox.SelectedIndex != -1)
            {
                var selectedString = AssistListBox.SelectedItem.ToString().Remove(0, sbLastWords.Length);
                if (AutoAddWhiteSpaceAfterTriggered) selectedString += " ";

                InsertText(selectedString);
                isInserted = true;
            }

            AssistListBox.Visibility = Visibility.Collapsed;
            sbLastWords.Clear();
            IsAssistKeyPressed = false;
            return isInserted;
        }

        #endregion

        #region check richtextbox's document.blocks is available

        //private void CheckMyDocumentAvailable()
        //{
        //    if (this.Document == null)
        //    {
        //        this.Document = new System.Windows.Documents.FlowDocument();
        //    }
        //    if (Document.Blocks.Count == 0)
        //    {
        //        Paragraph para = new Paragraph();
        //        Document.Blocks.Add(para);
        //    }
        //}

        #endregion

        #region Content Assist

        private bool IsAssistKeyPressed;
        private StringBuilder sbLastWords = new StringBuilder();
        private ListBox AssistListBox = new ListBox();

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!IsAssistKeyPressed)
            {
                base.OnPreviewKeyDown(e);
                return;
            }

            ResetAssistListBoxLocation();

            if (e.Key == Key.Back)
                if (sbLastWords.Length > 0)
                {
                    sbLastWords.Remove(sbLastWords.Length - 1, 1);
                    FilterAssistBoxItemsSource();
                }
                else
                {
                    IsAssistKeyPressed = false;
                    sbLastWords.Clear();
                    AssistListBox.Visibility = Visibility.Collapsed;
                }

            //enter key pressed, insert the first item to richtextbox
            if (e.Key == Key.Enter || e.Key == Key.Space || e.Key == Key.Tab)
            {
                AssistListBox.SelectedIndex = 0;
                if (InsertAssistWord()) e.Handled = true;
            }

            if (e.Key == Key.Down) AssistListBox.Focus();

            base.OnPreviewKeyDown(e);
        }

        private void FilterAssistBoxItemsSource()
        {
            var temp = ContentAssistSource.Where(s => s.StartsWith(sbLastWords.ToString()));
            AssistListBox.ItemsSource = temp;
            AssistListBox.SelectedIndex = 0;
            if (temp.Count() == 0)
                AssistListBox.Visibility = Visibility.Collapsed;
            else
                AssistListBox.Visibility = Visibility.Visible;
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);
            if (IsAssistKeyPressed == false && e.Text.Length == 1)
            {
                if (ContentAssistTriggers.Contains(char.Parse(e.Text)) && char.IsUpper(char.Parse(e.Text)))
                {
                    ResetAssistListBoxLocation();
                    IsAssistKeyPressed = true;
                    sbLastWords.Append(e.Text);
                    FilterAssistBoxItemsSource();
                }
            }
            else
            {
                if (IsAssistKeyPressed)
                {
                    sbLastWords.Append(e.Text);
                    FilterAssistBoxItemsSource();
                }
            }
        }

        private void ResetAssistListBoxLocation()
        {
            var rect = GetRectFromCharacterIndex(CaretIndex, true);
            var left = rect.X >= 20 ? rect.X : 20;
            var top = rect.Y >= 20 ? rect.Y + 20 : 20;
            left += Padding.Left;
            top += Padding.Top;
            AssistListBox.SetCurrentValue(MarginProperty, new Thickness(left, top, 0, 0));
        }

        #endregion
    }
}