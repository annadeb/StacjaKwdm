using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
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
using RestSharp.Serialization.Json;
using StacjaKwdm.Models;

namespace StacjaKwdm
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public RestClient _client;
		public MainWindow()
		{
			InitializeComponent();
			_client = new RestClient("http://localhost:8042");
			var request = new RestRequest("patients/", Method.GET);
			//var queryResult = client.Execute(request).Content.ToList();
			var query = _client.Execute<List<string>>(request);
			if (query.StatusCode == HttpStatusCode.OK)
			{
				serverLabel.Content = "Połączono z serwerem";
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
			var studyUID = (sender as ListBox).SelectedItem.ToString();

			var request = new RestRequest("studies/" + studyUID, Method.GET);
			var query = _client.Execute<Study>(request);

			instanceListBox.ItemsSource = query.Data.Series;
		}

		private void instanceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var seriesUID = (sender as ListBox).SelectedItem.ToString();

			var request = new RestRequest("series/" + seriesUID, Method.GET);
			var query = _client.Execute<Serie>(request);
			var instances = query.Data.Instances;

			foreach (var item in instances)
			{
				var request2 = new RestRequest("instances/" + item+"/file", Method.GET);
				var query2 = _client.Execute<Serie>(request2);

				//query2.RawBytes
			}
		}
		//private static BitmapImage LoadImage(byte[] imageData)
		//{
		//	if (imageData == null || imageData.Length == 0) return null;
		//	var image = new BitmapImage();
		//	using (var mem = new MemoryStream(imageData))
		//	{
		//		mem.Position = 0;
		//		image.BeginInit();
		//		image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
		//		image.CacheOption = BitmapCacheOption.OnLoad;
		//		image.UriSource = null;
		//		image.StreamSource = mem;
		//		image.EndInit();
		//	}
		//	image.Freeze();
		//	return image;
		//}
	}
}
