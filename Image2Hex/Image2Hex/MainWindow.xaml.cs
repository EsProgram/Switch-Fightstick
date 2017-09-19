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
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Reflection;

namespace Image2Hex
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		private const int WIDTH = 320;
		private const int HEIGHT = 120;

		private BitmapImage img;
		private string path;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			try
			{
				string[] drop = e.Data.GetData(DataFormats.FileDrop) as string[];
				path = drop[0];
				var uri = new Uri(path);
				img = new BitmapImage(uri);

				Image2Hex(img, path);
			}
			catch(Exception error)
			{
				MessageBox.Show(error.Message + Environment.NewLine + error.StackTrace,
					error.GetType().ToString(),
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private void invertColor_Clicked(object sender, RoutedEventArgs e)
		{
			if(img != null && !string.IsNullOrEmpty(path))
				Image2Hex(img, path);
		}

		private void Image2Hex(BitmapImage img, string path)
		{
			DeleteBuildedFiles();

			//縦横サイズのチェック
			if(img.PixelWidth != WIDTH || img.PixelHeight != HEIGHT)
			{
				MessageBox.Show("An image that is not 320 x 120 pixels was specified.", "Image size error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			var bitmap = new Bitmap(path);
			image.Source = img;

			//2値化して配列に格納
			int[] binary = Bitmap2Binary(bitmap);

			// ピクセルデータをimage.cに出力
			var code = Binary2C(binary);

			//image.cを出力
			using(TextWriter tw = new StreamWriter(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\image.c"))
			{
				tw.Write(code);
			}

			//makeコマンドを実行
			string cmdPath = Environment.GetEnvironmentVariable("ComSpec");
			var make = new Process();
			make.StartInfo.FileName = cmdPath;
			make.StartInfo.Arguments = string.Format(@"/c {0}\image2hex.bat", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
			make.StartInfo.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			make.Start();
			make.WaitForExit();
			make.Close();

			var success = (File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Joystick.hex") ? "Success" : "Faild");
			MessageBox.Show(success, "Result", MessageBoxButton.OK, MessageBoxImage.None);
		}

		/// <summary>
		/// makeコマンドの実行により生成される成果物の削除
		/// </summary>
		private static void DeleteBuildedFiles()
		{
			string imgSourcesPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\image.c";
			string hexFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Joystick.hex";
			if(File.Exists(imgSourcesPath))
				File.Delete(imgSourcesPath);
			if(File.Exists(hexFilePath))
				File.Delete(hexFilePath);
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