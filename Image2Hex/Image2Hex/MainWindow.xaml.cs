using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace Image2Hex
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		private const int WIDTH = 320;
		private const int HEIGHT = 120;

		private Uri path;
		private BitmapImage img;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			string[] drop = e.Data.GetData(DataFormats.FileDrop) as string[];
			path = new Uri(drop[0]);
			img = new BitmapImage(path);

			Image2Hex(path, img);
		}

		private void invertColor_Clicked(object sender, RoutedEventArgs e)
		{
			if(path != null && img != null)
				Image2Hex(path, img);
		}

		private void Image2Hex(Uri path, BitmapImage img)
		{
			File.Delete(Directory.GetCurrentDirectory() + @"\image.c");
			File.Delete(Directory.GetCurrentDirectory() + @"\Joystick.hex");

			//縦横サイズのチェック
			if(img.PixelWidth != WIDTH || img.PixelHeight != HEIGHT)
			{
				MessageBox.Show("An image that is not 320 x 120 pixels was specified.", "Image size error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			var bitmap = new Bitmap(path.AbsolutePath);
			image.Source = img;

			//2値化して配列に格納
			int[] binary = Bitmap2Binary(bitmap);

			// ピクセルデータをimage.cに出力
			var code = Binary2C(binary);

			//image.cを出力
			using(TextWriter tw = new StreamWriter(Directory.GetCurrentDirectory() + @"\image.c"))
			{
				tw.Write(code);
			}

			//makeコマンドを実行
			string cmdPath = Environment.GetEnvironmentVariable("ComSpec");
			var make = new Process();
			make.StartInfo.FileName = cmdPath;
			make.StartInfo.Arguments = string.Format(@"/c {0}\image2hex.bat", Directory.GetCurrentDirectory());
			make.StartInfo.UseShellExecute = false;
			make.StartInfo.CreateNoWindow = true;
			make.StartInfo.RedirectStandardOutput = true;
			make.Start();
			make.WaitForExit();

			var success = (File.Exists(Directory.GetCurrentDirectory() + @"\Joystick.hex") ? "Success" : "Faild");

			//結果に文字列を表示させる
			result.Text = make.StandardOutput.ReadToEnd();
			make.Close();

			MessageBox.Show(success, "Result", MessageBoxButton.OK, MessageBoxImage.None);
		}

		/// <summary>
		/// Bitmapを2値化された直列データに変換する
		/// </summary>
		private static int[] Bitmap2Binary(Bitmap bitmap)
		{
			int[] binary = new int[WIDTH * HEIGHT];
			for(int y = 0; y < HEIGHT; ++y)
			{
				for(int x = 0; x < WIDTH; ++x)
				{
					var color = bitmap.GetPixel(x, y);
					//明度が0.5以上かどうかで白黒判定
					binary[WIDTH * y + x] = color.GetBrightness() > 0.5f ? 0 : 1;
				}
			}

			return binary;
		}

		/// <summary>
		/// 直列データをC文字列に変換する
		/// </summary>
		private string Binary2C(int[] data)
		{
			StringBuilder sb = new StringBuilder("#include <stdint.h>\n#include <avr/pgmspace.h>\n\nconst uint8_t image_data[0x12c1] PROGMEM = {");
			for(int i = 0; i < 320 * 120 / 8; ++i)
			{
				int val = 0;
				for(int j = 0; j < 8; ++j)
				{
					val |= data[(i * 8) + j] << j;
				}

				if(invertColor.IsChecked.Value)
					val = ~val & 0xFF;
				else
					val = val & 0xFF;

				sb.Append("0x" + val.ToString("x") + ", ");
			}
			sb.Append("0x0};\n");
			return sb.ToString();
		}
	}
}