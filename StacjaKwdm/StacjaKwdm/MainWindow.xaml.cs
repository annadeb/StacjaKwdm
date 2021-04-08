using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.RightsManagement;
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
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Extensions;
using RestSharp.Serialization.Json;
using StacjaKwdm.Models;
//using Aspose.Imaging;
//using Aspose.Imaging.FileFormats.Dicom;
//using Aspose.Imaging.Sources;
using Dicom;
using Dicom.Imaging;
using segmentation;
using MathWorks.MATLAB.NET.Arrays;
using ColorPickerControls;

namespace StacjaKwdm
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	
	public partial class MainWindow : Window
	{
		public RestClient _client;
		public int sliderValue;
		public string seriesUID;
		public System.Windows.Point _position;

		public MainWindow()
		{
			InitializeComponent();
			_client = new RestClient("http://localhost:8042");
			var request = new RestRequest("patients/", Method.GET);
			//var queryResult = client.Execute(request).Content.ToList();
			var query = _client.Execute<List<string>>(request);
			if (query.StatusCode == HttpStatusCode.OK)
			{
				serverLabel.Content = "Połączono z serwerem.";
			}
			patientListBox.ItemsSource = query.Data;
		}
	

		private void patientListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//ListBoxItem lbi = ((sender as ListBox).SelectedItem as ListBoxItem);
			var patientUID = (sender as ListBox).SelectedItem.ToString();

			var request = new RestRequest("patients/"+ patientUID, Method.GET);
			//var queryResult = client.Execute(request).Content.ToList();
			var query = _client.Execute<Patient>(request);
		
			studyListBox.ItemsSource = query.Data.Studies;
		}

		private void studyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if((sender as ListBox).SelectedItem == null)
			{
				return;
			}
			var studyUID = (sender as ListBox).SelectedItem.ToString();

			var request = new RestRequest("studies/" + studyUID, Method.GET);
			var query = _client.Execute<Study>(request);

			instanceListBox.ItemsSource = query.Data.Series;
		}

		private void instanceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if ((sender as ListBox).SelectedItem == null)
			{
				return;
			}
			seriesUID = (sender as ListBox).SelectedItem.ToString();

			var request = new RestRequest("series/" + seriesUID, Method.GET);
			var query = _client.Execute<Serie>(request);
			System.IO.Directory.CreateDirectory(seriesUID);
			var instances = query.Data.Instances;
			foreach (var item in instances)
			{

				var request2 = new RestRequest("instances/" + item + "/file", Method.GET); // /preview do .png
				request2.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
				//var query2 = _client.Execute(request2);
				_client.DownloadData(request2).SaveAs(seriesUID+ "/" + item + ".dcm"); //asd.png
			}
			pictureSlider.Maximum = instances.Count() - 1;
			DisplayDicom(seriesUID, sliderValue);
			//string[] filesInDirectory = Directory.GetFiles(seriesUID).ToArray();
			//for (int i = 0; i < filesInDirectory.Length; i++)
			//{
			//	string path = filesInDirectory[i];
			//	var dicomImg = new DicomImage(@path);
			//	//var cos = dicomImg.Dataset.GetSequence(new DicomTag(0010, 0010));
			//}
		}

		private void pictureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			sliderValue = (int) pictureSlider.Value;
			DisplayDicom(seriesUID, sliderValue);
		
		}
		public void DisplayDicom(string seriesUID,int sliderValue)
		{
			string[] filesInDirectory = Directory.GetFiles(seriesUID).ToArray();
			string path =  filesInDirectory[sliderValue];	
			var dicomImg = new DicomImage(@path);
			Bitmap renderedImage = dicomImg.RenderImage().As<Bitmap>();
			var ScreenCapture = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
			renderedImage.GetHbitmap(),
			IntPtr.Zero,
			System.Windows.Int32Rect.Empty,
			BitmapSizeOptions.FromWidthAndHeight(renderedImage.Width, renderedImage.Height));
			image1.Source = ScreenCapture;

		}

		private void autoSegmentButton_Click(object sender, RoutedEventArgs e)
		{
			_position = image1.Position;
			if (_position.X==0 && _position.Y==0)
			{
				MessageBox.Show("Wybierz punkt startowy!","Ostrzeżenie", MessageBoxButton.OK,MessageBoxImage.Warning);
				return;
			}
			
			//var klasa = new Class1();
			//MWArray folderpath = "C:\\Users\\Anna\\Desktop\\Studia\\MAGISTERKA_3\\KomputeroweWspomaganieDiagnostykiMedycznej\\Projekt\\StacjaKwdm\\StacjaKwdm\\StacjaKwdm\\bin\\Debug\\c71658e3-68b7c35c-5216242c-fb200b08-aa56b7d0";
			//var cos = klasa.segmentation(folderpath);
			int a = 0;
		}
	}
}
