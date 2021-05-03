using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StacjaKwdm
{
	/// <summary>
	/// Interaction logic for LoginWindow.xaml
	/// </summary>
	public partial class LoginWindow : Window
	{
		private string connString = ConfigurationManager.ConnectionStrings["StacjaKwdm.Properties.Settings.DatabaseConnectionString"].ConnectionString;// "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\\Database.mdf;Integrated Security=True";
		public LoginWindow()
		{
			InitializeComponent();
		}

		private void submitBt_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(loginTb.Text)&&string.IsNullOrEmpty(passwordTb.Password))
			{
				MessageBox.Show("Login i hasło są wymagane", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			if (string.IsNullOrEmpty(loginTb.Text))
			{
				MessageBox.Show("Login jest wymagany", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			if (string.IsNullOrEmpty(passwordTb.Password))
			{
				MessageBox.Show("Hasło jest wymagane", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			using (var conn = new SqlConnection(connString))
			{
				string sqlString = "select [Password] from [dbo].[Users] where [Login]=\'" + loginTb.Text+"\'";
				using (var command = new SqlCommand(sqlString, conn))
				{
					conn.Open();
					var result = command.ExecuteScalar();
					if (result == null)
					{
						MessageBox.Show("Podano błędny login", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
					}
					if (Regex.Replace(result.ToString(), @"\s+", "")!=passwordTb.Password)
					{
						MessageBox.Show("Podano błędne hasło","Błąd!",MessageBoxButton.OK,MessageBoxImage.Error);
						return;
					}	
				}
			}
			var mainWindow = new MainWindow(loginTb.Text);
			mainWindow.Show();
			this.Close(); 
		}
	}
}
