using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
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

namespace StacjaKwdm
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			var client = new RestClient("http://localhost:8042");
			var request = new RestRequest("patients/", Method.GET);
			//var queryResult = client.Execute(request).Content.ToList();
			var query = client.Execute(request);
			if (query.StatusCode == HttpStatusCode.OK)
			{
				// Two ways to get the result:
				//string rawResponse = query.Content;
				//Patient patient = new JsonDeserializer().Deserialize<Patient>(query);
				dynamic jsonResponse = JsonConvert.DeserializeObject(query.Content);
				//JsonModel patientlists = new JsonDeserializer().Deserialize<JsonModel>(query);
				int d = 5;
			}
			

		}
		public class Patient
		{
			public Patient(string patientID)
			{
				patientID = patientID;
			}
			public string patientID { get; }
		}
		public class JsonModel
		{
			public List<Patient> patients { get; set; }
		}

	}
}
