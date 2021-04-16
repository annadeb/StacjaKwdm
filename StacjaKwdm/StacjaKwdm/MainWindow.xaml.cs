using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using RestSharp;
using RestSharp.Extensions;
using StacjaKwdm.Models;
using Dicom;
using Dicom.Imaging;
using MathWorks.MATLAB.NET.Arrays;
using segment_tumor;
using System.Reflection;
using System.ComponentModel;

namespace StacjaKwdm
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>

	public partial class MainWindow : Window
	{
		public RestClient _client;
		public int sliderValue;
		public string _seriesUID;
		public System.Windows.Point _position;
		public bool masksAvailable = false;
		BackgroundWorker segmentationWorker = new BackgroundWorker();
		private delegate void UpdateMyDelegatedelegate(string text);
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
			segmentationWorker.DoWork += SegmentationWorker_DoWork;
		}
	

		private void patientListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var patientUID = (sender as ListBox).SelectedItem.ToString();
			var request = new RestRequest("patients/"+ patientUID, Method.GET);
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
			_seriesUID = (sender as ListBox).SelectedItem.ToString();

			var request = new RestRequest("series/" + _seriesUID, Method.GET);
			var query = _client.Execute<Serie>(request);
			var executableDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
			System.IO.Directory.CreateDirectory(executableDirectory+"\\"+_seriesUID);
			var instances = query.Data.Instances;
			foreach (var item in instances)
			{
				var request2 = new RestRequest("instances/" + item + "/file", Method.GET); // /preview do .png
				request2.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
				//var query2 = _client.Execute(request2);
				_client.DownloadData(request2).SaveAs(executableDirectory+"\\"+_seriesUID + "\\" + item + ".dcm"); //asd.png
			}
			pictureSlider.Maximum = instances.Count() - 1;
			DisplayDicom(_seriesUID, sliderValue);
		}

		private void pictureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			sliderValue = (int) pictureSlider.Value;
			DisplayDicom(_seriesUID, sliderValue);
			if (masksAvailable)
			{
				DisplayMasks(_seriesUID, sliderValue);
			}
		}
		public void DisplayDicom(string seriesUID, int sliderValue)
		{
			var executableDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
			string[] filesInDirectory = Directory.GetFiles(executableDirectory + "\\" + seriesUID).ToArray();
			Dictionary<int, string> keyValuepair = new Dictionary<int, string>();
			string path;
			DicomImage dicomImg = null;
			int instanceNumber;
			for (int i = 0; i < filesInDirectory.Length; i++)
			{
				path = filesInDirectory[i];
				dicomImg = new DicomImage(@path);
				instanceNumber = dicomImg.Dataset.Get(DicomTag.InstanceNumber, 0);
				keyValuepair.Add(instanceNumber, path);
			}

			string pathtoImage = keyValuepair[sliderValue + 1];
			var dicomImage = new DicomImage(@pathtoImage);

			Bitmap renderedImage = dicomImage.RenderImage().As<Bitmap>();
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
			segmentationWorker.RunWorkerAsync();
		}

		private void SegmentationWorker_DoWork(object Sender, System.ComponentModel.DoWorkEventArgs e)
		{
			UpdateMyDelegatedelegate UpdateMyDelegate = new UpdateMyDelegatedelegate(UpdateMyDelegateLabel);
			autoSegmentButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, UpdateMyDelegate, "Czekaj...");

			UpdateMyDelegatedelegate UpdateMySpinner = new UpdateMyDelegatedelegate(UpdateMyDelegateSpinner);
			busyImage.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, UpdateMySpinner, "V");

			var klasa = new Class1();
			var executableDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
			MWArray folderpath = executableDirectory + "\\" + _seriesUID;
			System.IO.Directory.CreateDirectory(executableDirectory + "\\" + _seriesUID + "_mask");
			MWArray outputPath = executableDirectory + "\\" + _seriesUID + "_mask";
			var positionY = Math.Round(_position.Y);
			var positionX = Math.Round(_position.X);
			var output = klasa.segment_tumor(folderpath, positionY, positionX, sliderValue + 1, outputPath);

			masksAvailable = true;

			autoSegmentButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, UpdateMyDelegate,"Segmentuj");
			busyImage.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, UpdateMySpinner, "H");
			UpdateMyDelegatedelegate UpdateMyImage = new UpdateMyDelegatedelegate(UpdateMyDelegateImage);
			image2.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, UpdateMyImage, "a");
		}
		private void UpdateMyDelegateLabel(string text)
		{
			autoSegmentButton.Content = text;
		}
		private void UpdateMyDelegateImage(string text)
		{
			DisplayMasks(_seriesUID, sliderValue);
		}
		private void UpdateMyDelegateSpinner(string text)
		{
			if (text == "V")
			{
				busyImage.Visibility = Visibility.Visible;
			}
			else
			{
				busyImage.Visibility = Visibility.Hidden;
			}
		}

		public void DisplayMasks(string seriesUID, int sliderValue)
		{
			var executableDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
			string[] filesInDirectory = Directory.GetFiles(executableDirectory + "\\" + seriesUID + "_mask").ToArray();
			Dictionary<int, string> keyValuepair = new Dictionary<int, string>();
			string path;
			DicomImage dicomImg = null;
			int instanceNumber;
			for (int i = 0; i < filesInDirectory.Length; i++)
			{
				path = filesInDirectory[i];
				dicomImg = new DicomImage(@path);
				instanceNumber = dicomImg.Dataset.Get(DicomTag.InstanceNumber, 0);
				keyValuepair.Add(instanceNumber, path);
			}

			string pathtoImage = keyValuepair[sliderValue + 1];
			var dicomImage = new DicomImage(@pathtoImage);

			Bitmap renderedImage = dicomImage.RenderImage().As<Bitmap>();
			var ScreenCapture = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
			renderedImage.GetHbitmap(),
			IntPtr.Zero,
			System.Windows.Int32Rect.Empty,
			BitmapSizeOptions.FromWidthAndHeight(renderedImage.Width, renderedImage.Height));
			image2.Source = ScreenCapture;
		}
	}
}
