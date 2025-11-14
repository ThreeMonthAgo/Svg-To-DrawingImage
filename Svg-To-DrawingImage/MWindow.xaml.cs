using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Svg_To_DrawingImage
{
    public partial class MWindow : Window
    {
        public MWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ = Task.Run(() =>
            {
                while (true)
                {
                    _ = Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (CKB_Cpb.IsChecked == true && Clipboard.ContainsText())
                            {
                                string txt = Clipboard.GetText();
                                if (txt.Contains("path d=\""))
                                {
                                    SVG_Text.Text = Clipboard.GetText();
                                    Clipboard.Clear();
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Debug.WriteLine("在监控剪切板时发生异常");
                        }
                    }));
                    Thread.Sleep(500);
                }
            });
        }

        private void SVG_Text_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string text = SVG_Text.Text;
                if (text != "")
                {
                    // 需要注意存在多条路径的情况
                    string[] strs = Regex.Split(text, "path d=\"", RegexOptions.IgnoreCase);
                    int len = strs.Length;
                    string path = "";
                    // 提取 path 属性
                    List<string> paths = new List<string>();
                    // 提取 fill 属性
                    List<string> colors = new List<string>();
                    for (int i = 1; i < strs.Length; i++)
                    {
                        paths.Add(strs[i].Split('\"')[0]);
                        path += " " + strs[i].Split('\"')[0];

                        // 考虑 opacity
                        string s = "opacity=\"";
                        double opacity = 1;
                        if (strs[i].Contains(s))
                        {
                            int pos = strs[i].IndexOf(s);
                            opacity = double.Parse(strs[i].Substring(pos + s.Length, 2));
                        }
                        string op = ((int)(opacity * 255)).ToString("X");

                        // 考虑 fill
                        s = "fill=\"#";
                        string cl = "555555";
                        if (strs[i].Contains(s))
                        {
                            int pos = strs[i].IndexOf(s);
                            cl = strs[i].Substring(pos + s.Length, 6);
                        }

                        colors.Add("#" + op + cl);
                    }
                    Geometry_Text.Text = string.Format("<Geometry x:Key=\"{0}\">{1}</Geometry>", TB_Name.Text, path);

                    #region Draw -> Path
                    TypeConverter converter = TypeDescriptor.GetConverter(typeof(Geometry));
                    Geometry geometry = (Geometry)converter.ConvertFrom(path);
                    if (RaBtn_Geometry.IsChecked == true)
                    {
                        Path_Icon.Data = geometry;
                    }
                    else if (RaBtn_PathGeometry.IsChecked == true)
                    {
                        // 处理一些特殊情况
                        Path_Icon.Data = GeometryMethod.ConvertGeometry(geometry);
                    }
                    else
                    {
                        // 处理一些特殊情况
                        Path_Icon.Data = GeometryMethod.ConvertGeometryFill(geometry);
                    }
                    #endregion

                    // 处理彩色图像
                    DrawingImage_Text.Text = string.Format("<DrawingImage x:Key=\"{0}\">\n  <DrawingImage.Drawing>\n    <DrawingGroup>", TB_Name.Text);
                    // 给每条 Path 设置颜色
                    for (int i = 0; i < colors.Count; i++)
                    {
                        string str = string.Format("\n      <GeometryDrawing Brush=\"{0}\" Geometry=\"{1}\"/>", colors[i], paths[i]);
                        DrawingImage_Text.Text += str;
                    }
                    DrawingImage_Text.Text += "\n    </DrawingGroup>\n  </DrawingImage.Drawing>\n</DrawingImage>";

                    #region Draw -> DrawingImage
                    // DrawingImage
                    DrawingImage image = new DrawingImage();
                    DrawingGroup group = new DrawingGroup();
                    for (int i = 0; i < colors.Count; i++)
                    {
                        GeometryDrawing drawing = new GeometryDrawing
                        {
                            Brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i])),
                            Geometry = (Geometry)converter.ConvertFrom(paths[i]),
                        };
                        group.Children.Add(drawing);
                    }
                    image.Drawing = group;
                    Image_DrawingImage.Source = image;
                    #endregion
                }
            }
            catch (Exception)
            {
                Geometry_Text.Text = "";
                DrawingImage_Text.Text = "";
            }
        }

        private void TB_Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SVG_Text != null)
            {
                SVG_Text_TextChanged(null, null);
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (SVG_Text != null)
            {
                SVG_Text_TextChanged(null, null);
            }
        }

        private void SVG_Text_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void SVG_Text_Drop(object sender, DragEventArgs e)
        {
            try
            {
                var filePath = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
                SVG_Text.Text = File.ReadAllText(filePath);
            }
            catch (Exception)
            {
                Debug.WriteLine("在拖入文件时发生异常");
            }
        }

        private void Button_Cp_Click(object sender, RoutedEventArgs e)
        {
            switch (MyTabControl.SelectedIndex)
            {
                case 0: Clipboard.SetText(Geometry_Text.Text); break;
                case 1: Clipboard.SetText(DrawingImage_Text.Text); break;
            }
        }
    }
}
