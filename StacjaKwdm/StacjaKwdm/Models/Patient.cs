using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StacjaKwdm.Models
{
	public class Patient
	{
		public string ID { get; set; }
		public MainDicomTags MainDicomTags { get; set; }
		public List<string> Studies { get; set; }
		public string Type { get; set; }

	}

	public class MainDicomTags
	{
		public string OtherPatientIDs { get; set; }
		public string PatientBirthDate { get; set; }
		public string PatientID { get; set; }
		public string PatientName { get; set; }
		public string PatientSex { get; set; }
	}
}
