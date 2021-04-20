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
			GetPatients();

			segmentationWorker.DoWork += SegmentationWorker_DoWork;
		}
	
		private void GetPatients()
		{
			var request = new RestRequest("patients/", Method.GET);
			var query = _client.Execute<List<string>>(request);
			if (query.StatusCode == HttpStatusCode.OK)
			{
				serverLabel.Content = "Połączono z serwerem.";
				var iter = query.Data.Count();
				patientListBox.ItemsSource = query.Data.Take(iter - 1);
			}
		}

		private void patientListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var patientUID = (sender as ListBox).SelectedItem.ToString();
			var request = new RestRequest("patients/"+ patientUID, Method.GET);
			var query = _client.Execute<Patient>(request);
		
			studyListBox.ItemsSource = query.Data.Studies;
			BitmapImage startImg = new BitmapImage();
			startImg.BeginInit();
			startImg.UriSource = new Uri("pack://application:,,,/StacjaKwdm;component/Assets/image.JPG");
			startImg.EndInit();
			image1.Source = startImg;
			image2.Source = null;
			saveMasksButton.IsEnabled=false;
			tbDescription.Text = "";
			instanceListBox.ItemsSource = null;
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
			saveMasksButton.IsEnabled = true;
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
			if (System.IO.Directory.Exists(executableDirectory + "\\" + _seriesUID + "_mask")) 
			{
				System.IO.Directory.Delete(executableDirectory + "\\" + _seriesUID + "_mask",true);
			};
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
			
			if (Directory.Exists(executableDirectory + "\\" + seriesUID + "_mask"))
			{
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

		private void saveMasksButton_Click(object sender, RoutedEventArgs e)
		{
			var executableDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
			var pathToFolder = executableDirectory + "\\" + _seriesUID + "_mask";
			
			string[] filesInDirectory = Directory.GetFiles(pathToFolder).ToArray();
			var desc = tbDescription.Text;
			foreach (var item in filesInDirectory)
			{

				var request = new RestRequest("instances", Method.POST);
				string path= Path.Combine(pathToFolder, item);
				DicomFile dicomFile = DicomFile.Open(path, FileReadOption.ReadAll);
				dicomFile.Dataset.AddOrUpdate<string>(DicomTag.StudyDescription, desc);
				dicomFile.Save(path);
				request.AddFile("content", @path);
				var query = _client.Execute(request);
			}
			MessageBox.Show("Pomyślnie wysłano maski na serwer.", "Powodzenie!", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void readMasksButton_Click(object sender, RoutedEventArgs e)
		{
			var executableDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
			string[] filesInDirectory = Directory.GetFiles(executableDirectory + "\\" + _seriesUID).ToArray();
			string path = filesInDirectory[1];
			var dicomImg = new DicomImage(@path);
			string StudyNumber = dicomImg.Dataset.GetString(DicomTag.StudyInstanceUID);
			var request = new RestRequest("tools/find", Method.POST);
			request.AddHeader("Accept-Encoding", "gzip, deflate, br");
			request.AddJsonBody(new
			{
				Level = "Series",
				Query = new
				{
					StudyInstanceUID = StudyNumber+"_mask",
					PatientID = "*"
				}
			}
			);
			var query = _client.Execute(request);
			if (query.Content != "[]")
			{
				var maskSeriesUID = query.Content.Replace("\n", "").Replace("[", "").Replace("]", "").Replace("\"", "").Replace(" ", "");

				var newRequest = new RestRequest("series/" + maskSeriesUID, Method.GET);
				var newQuery = _client.Execute<Serie>(newRequest);
				var newExecutableDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
				if (System.IO.Directory.Exists(executableDirectory + "\\" + maskSeriesUID + "_mask"))
				{
					System.IO.Directory.Delete(executableDirectory + "\\" + maskSeriesUID + "_mask", true);
				}
				System.IO.Directory.CreateDirectory(executableDirectory + "\\" + maskSeriesUID + "_mask");
				var instances = newQuery.Data.Instances;
				var exampleitem = instances.First();
				string path2 = newExecutableDirectory + "\\" + maskSeriesUID + "_mask\\" + exampleitem.ToString() + ".dcm";
				foreach (var item in instances)
				{
					var request2 = new RestRequest("instances/" + item + "/file", Method.GET); // /preview do .png
					request2.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
					//var query2 = _client.Execute(request2);
					_client.DownloadData(request2).SaveAs(newExecutableDirectory + "\\" + maskSeriesUID + "_mask\\" + item + ".dcm"); //asd.png
				}
				dicomImg = new DicomImage(@path2);
				var description = dicomImg.Dataset.Get(DicomTag.StudyDescription, "").ToString();
				tbDescription.Text = description;
				DisplayMasks(maskSeriesUID, sliderValue);
				masksAvailable = true;
			}
			else
			{
				MessageBox.Show("Brak masek na serwerze.", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void refreshBt_Click(object sender, RoutedEventArgs e)
		{
			GetPatients();
		}
	}
}
