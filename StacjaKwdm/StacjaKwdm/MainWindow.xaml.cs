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

				var request2 = new RestRequest("instances/" + item + "/preview", Method.GET);
				request2.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
				var query2 = _client.Execute(request2);
				_client.DownloadData(request2).SaveAs(@"asd.png");
			}
			var bitmap = new BitmapImage();
			bitmap.BeginInit();
			string path = @"asd.png";
			var pathAbs = System.IO.Path.GetFullPath(path);
			bitmap.UriSource = new Uri(pathAbs, UriKind.RelativeOrAbsolute);
			bitmap.EndInit();
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			image1.Source = bitmap;
		}
	}
}
