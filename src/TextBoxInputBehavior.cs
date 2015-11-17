using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace WpfApplication1
{
    /********************************************************************************

    ** 作者： 张钢

    ** 创始时间：2015-11-12

    ** 修改人：

    ** 修改时间：

    ** 描述：
     *主要采用正则匹配模式
     *支持Decimal(十进制)的整数部分长度和小数部分长度
     *输入过滤和输入完成之后的匹配
     *输入长度控制

    *********************************************************************************/
    public class TextBoxInputBehavior : Behavior<TextBox>
    {
        #region 构造函数 匹配模式
        public TextBoxInputBehavior()
        {
            this.InputMode = TextBoxInputMode.None;
        }
        /// <summary>
        /// TextBox 验证模式
        /// </summary>
        public TextBoxInputMode InputMode { get; set; }

        #endregion
        
        #region 依赖属性定义
        public static readonly DependencyProperty RegexPatternProperty =
            DependencyProperty.Register("RegexPattern", typeof(string),
         typeof(TextBoxInputBehavior), new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty AllRegexPatternProperty =
            DependencyProperty.Register("AllRegexPattern", typeof(string),
         typeof(TextBoxInputBehavior), new FrameworkPropertyMetadata(".+"));

        public static readonly DependencyProperty IsFilterSpaceProperty =
            DependencyProperty.Register("IsFilterSpace", typeof(bool),
         typeof(TextBoxInputBehavior), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register("MaxLength", typeof(int),
         typeof(TextBoxInputBehavior), new FrameworkPropertyMetadata(int.MaxValue));

        public static readonly DependencyProperty IntegersLengthProperty =
            DependencyProperty.Register("IntegersLength", typeof(int),
         typeof(TextBoxInputBehavior), new FrameworkPropertyMetadata(13));

        public static readonly DependencyProperty PointLengthProperty =
            DependencyProperty.Register("PointLength", typeof(int),
         typeof(TextBoxInputBehavior), new FrameworkPropertyMetadata(2));
        

        /// <summary>
        /// 所有的text匹配原则
        /// </summary>
        public string AllRegexPattern
        {
            get { return (string)GetValue(AllRegexPatternProperty); }
            set { SetValue(AllRegexPatternProperty, value); }
        }

        /// <summary>
        /// 输入匹配字符串
        /// </summary>
        public string RegexPattern
        {
            get { return (string)GetValue(RegexPatternProperty); }
            set { SetValue(RegexPatternProperty, value); }
        }
        /// <summary>
        /// 是否过滤空格(默认为true)
        /// </summary>
        public bool IsFilterSpace
        {
            get { return (bool)GetValue(IsFilterSpaceProperty); }
            set { SetValue(IsFilterSpaceProperty, value); }
        }
        /// <summary>
        /// 最大长度
        /// </summary>
        public int MaxLength
        {
            get { return (int)GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }
        /// <summary>
        /// TextBoxInputMode为Decimal模式下才起作用的实数长度
        /// </summary>
        public int IntegersLength
        {
            get { return (int)GetValue(IntegersLengthProperty); }
            set { SetValue(IntegersLengthProperty, value); }
        }
        /// <summary>
        /// TextBoxInputMode为Decimal模式下才起作用的小数部分长度
        /// </summary>
        public int PointLength
        {
            get { return (int)GetValue(PointLengthProperty); }
            set { SetValue(PointLengthProperty, value); }
        }
        #endregion
        
        #region 事件（装载、卸载、粘贴、按键、输入、失去焦点）
        protected override void OnAttached()
        {
            base.OnAttached();
            if (InputMode == TextBoxInputMode.Decimal && InputMethod.GetIsInputMethodEnabled(AssociatedObject))
                InputMethod.SetIsInputMethodEnabled(AssociatedObject, false);

            AssociatedObject.PreviewTextInput += AssociatedObjectPreviewTextInput;
            AssociatedObject.PreviewKeyDown += AssociatedObjectPreviewKeyDown;
            AssociatedObject.LostFocus += AssociatedObject_LostFocus;

            DataObject.AddPastingHandler(AssociatedObject, Pasting);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            
            AssociatedObject.PreviewTextInput -= AssociatedObjectPreviewTextInput;
            AssociatedObject.PreviewKeyDown -= AssociatedObjectPreviewKeyDown;
            AssociatedObject.LostFocus -= AssociatedObject_LostFocus;

            DataObject.RemovePastingHandler(AssociatedObject, Pasting);
        }

        private void Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var pastedText = (string)e.DataObject.GetData(typeof(string));

                if (!this.validInput(pastedText))
                {
                    System.Media.SystemSounds.Beep.Play();
                    e.CancelCommand();
                }
                if(GetText(pastedText).Length>MaxLength)
                {
                    System.Media.SystemSounds.Beep.Play();
                    e.CancelCommand();
                }
            }
            else
            {
                System.Media.SystemSounds.Beep.Play();
                e.CancelCommand();
            }
        }

        private void AssociatedObjectPreviewKeyDown(object sender, KeyEventArgs e)
        {
            
                if (e.Key == Key.Space)
                {
                    //如果不过滤空格，并且没有到达最大长度，则加入空格
                    if (!IsFilterSpace && GetText(" ").Length <= MaxLength)
                        return;
                    System.Media.SystemSounds.Beep.Play();
                    e.Handled = true;
                }
        }

        private void AssociatedObjectPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            
            if (!this.validInput(e.Text)|GetText(e.Text).Length > MaxLength)
            {
                System.Media.SystemSounds.Beep.Play();
                
                if (InputMode != TextBoxInputMode.Decimal && InputMethod.GetIsInputMethodEnabled(AssociatedObject))
                    remove(e.Text, GetText(e.Text).Length > MaxLength);
                e.Handled = true;
                return;
            }
            if (InputMode == TextBoxInputMode.Decimal) 
            {
                string input = e.Text;
                int selectionStart = AssociatedObject.SelectionStart;

                AssociatedObject.Text = GetText(input);
                //光标后移
                AssociatedObject.Select(selectionStart + e.Text.Length, 0);
                e.Handled = true;
            }
        }

        private void AssociatedObject_LostFocus(object sender, RoutedEventArgs e)
        {
           
            if (InputMode == TextBoxInputMode.Decimal)
            {
                if (AssociatedObject.Text.Equals("-") | AssociatedObject.Text.Equals("."))
                    AssociatedObject.Text = decimal.Zero.ToString("f" + PointLength);
                if (AssociatedObject.Text.Contains("."))
                    AssociatedObject.Text = decimal.Parse(AssociatedObject.Text).ToString("f" + PointLength);
               
                
            }else
                if (InputMode == TextBoxInputMode.AllRegex) 
                {
                    string Pattern="";
                    if (AllRegexPattern != "")
                        Pattern = AllRegexPattern;
                    Regex rex3 = new Regex(Pattern);
                    if (!rex3.IsMatch(AssociatedObject.Text))
                    {
                        MessageBox.Show("");
                        e.Handled = true;
                        AssociatedObject.Text = "";
                    }
                }
        }
        #endregion

        #region 验证 移除
        
        //判断输入的字符串是否符合
        private bool validInput(string input)
        {
            bool result = false;
            string Pattern = @"^[A-Za-z0-9]+$";

            switch (InputMode)
            {
                case TextBoxInputMode.InputRegex:
                    if (RegexPattern != "")
                        Pattern = RegexPattern;
                    Regex rex = new Regex(Pattern);
                    result = rex.IsMatch(input) && !input.Equals("");
                    break;
                case TextBoxInputMode.AllRegex:
                    if (RegexPattern != "")
                        Pattern = RegexPattern;
                    Regex rex3 = new Regex(Pattern);
                    result = rex3.IsMatch(input) && !input.Equals("");
                    break;
                case TextBoxInputMode.None:
                    result = true;
                    break;
                case TextBoxInputMode.Decimal:
                    Pattern = @"^-?[0-9]?[0-9]{0," + (IntegersLength - 1) + @"}(\.[0-9]{0," + PointLength + @"})?$";
                    if (RegexPattern != "")
                        Pattern = RegexPattern;
                    Regex rex2 = new Regex(Pattern);
                    Decimal d = 0;
                    bool parse = Decimal.TryParse(GetText(input), out d);
                    
                    if (GetText("-").Equals("-"))
                        parse = true;
                    result = rex2.IsMatch(GetText(input)) && parse && !input.Equals("");
                    break;
                default: throw new ArgumentException("没有支持的TextBoxInputMode模式");
            }
            return result;
        }
        
        /// <summary>
        /// 获取整个text
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string GetText(string input)
        {
            var txt = this.AssociatedObject;

            int selectionStart = txt.SelectionStart;
            if (txt.Text.Length < selectionStart)
                selectionStart = txt.Text.Length;

            int selectionLength = txt.SelectionLength;
            if (txt.Text.Length < selectionStart + selectionLength)
                selectionLength = txt.Text.Length - selectionStart;

            var realtext = txt.Text.Remove(selectionStart, selectionLength);

            int caretIndex = txt.CaretIndex;
            if (realtext.Length < caretIndex)
                caretIndex = realtext.Length;

            var newtext = realtext.Insert(caretIndex, input);

            return newtext;
        }
        
        /// <summary>
        /// 移除的字符串
        /// </summary>
        /// <param name="input">即将输入或者已经输入的字符串（即将输入和已经输入，主要是输入法造成的）</param>
        /// <param name="IsMaxlenRemove">是否超出长度判断</param>
        private void remove(string input,bool IsMaxlenRemove) 
        {
            int start = AssociatedObject.SelectionStart;
            //
            if(start - input.Length>=0)
                if (IsMaxlenRemove)
                {
                    //二次过滤有必要，主要是考虑到启用输入法的特殊情况
                    if (AssociatedObject.Text.Length>MaxLength)
                    AssociatedObject.Text = AssociatedObject.Text.Remove(start - input.Length, input.Length);
                }
                else {
                    AssociatedObject.Text = AssociatedObject.Text.Remove(start - input.Length, input.Length);
                }         
            AssociatedObject.SelectionStart = start;
        }
        #endregion
    }
    
    
    /// <summary>
    /// 匹配模式
    /// </summary>
    public enum TextBoxInputMode
    {
        /// <summary>
        /// 空模式
        /// </summary>
        None,
        /// <summary>
        /// 输入验证
        /// </summary>
        InputRegex,
        /// <summary>
        /// 十进制
        /// </summary>
        Decimal,
        /// <summary>
        /// 完整匹配验证
        /// </summary>
        AllRegex
    }
}
