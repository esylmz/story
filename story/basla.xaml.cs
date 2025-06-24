using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace story
{
    public partial class Basla : Window
    {
        public bool IsMusicOn { get; private set; } = false;
        private readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=story;Integrated Security=True;";

        public Basla()
        {
            InitializeComponent();
            SettingsPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Collapsed;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.Properties.Contains("UserId"))
            {
                int startSceneId = 1;
                MainWindow mainWindow = new MainWindow(startSceneId, IsMusicOn);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Lütfen önce giriş yapın.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.Properties.Contains("UserId") && App.Current.Properties["UserId"] is int userId)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        SqlCommand cmd = new SqlCommand("SELECT LastSceneId, IsMusicOn FROM Users WHERE Id = @UserId", connection);
                        cmd.Parameters.AddWithValue("@UserId", userId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int lastSceneId = reader.GetInt32(0);
                                bool musicOn = reader.GetBoolean(1);

                                IsMusicOn = musicOn;
                                MusicToggleCheckBox.IsChecked = musicOn;
                                App.Current.Properties["LastSceneId"] = lastSceneId;

                                MainWindow mainWindow = new MainWindow(lastSceneId, musicOn);
                                mainWindow.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Kullanıcı bilgisi bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Devam ederken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Lütfen önce giriş yapın.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!App.Current.Properties.Contains("UserId"))
            {
                MessageBox.Show("Lütfen önce giriş yapınız.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }


        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterPanel.Visibility = Visibility.Visible;
            LoginPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowLoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Visible;
            RegisterPanel.Visibility = Visibility.Collapsed;
        }

        private void HideLoginPanel_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
        }

        private void CancelRegister_Click(object sender, RoutedEventArgs e)
        {
            RegisterPanel.Visibility = Visibility.Collapsed;
            RegisterUsernameTextBox.Text = "";
            RegisterPasswordBox.Password = "";
        }

        private void CancelLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            LoginUsernameTextBox.Text = "";
            LoginPasswordBox.Password = "";
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = RegisterUsernameTextBox.Text.Trim();
            string password = RegisterPasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Lütfen kullanıcı adı ve şifreyi doldurun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
                
            }

            string hashedPassword = HashPassword(password);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Username = @Username", connection);
                    checkCmd.Parameters.AddWithValue("@Username", username);
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        MessageBox.Show("Bu kullanıcı adı zaten alınmış.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    SqlCommand command = new SqlCommand(
                        "INSERT INTO Users (Username, PasswordHash, IsMusicOn, LastSceneId, Role) VALUES (@Username, @PasswordHash, 1, 1, 'User')", connection);
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    command.ExecuteNonQuery();

                    SqlCommand getIdCmd = new SqlCommand("SELECT Id FROM Users WHERE Username = @Username", connection);
                    getIdCmd.Parameters.AddWithValue("@Username", username);
                    int newUserId = (int)getIdCmd.ExecuteScalar();

                    App.Current.Properties["UserId"] = newUserId;
                    App.Current.Properties["UserRole"] = "User";

                    MessageBox.Show("Kayıt başarılı!", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    RegisterPanel.Visibility = Visibility.Collapsed;
                    ShowLoginButton.Visibility = Visibility.Collapsed;
                    ShowRegisterButton.Visibility = Visibility.Collapsed;
                    SettingsPanel.Visibility = Visibility.Collapsed;

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt sırasında bir hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = LoginUsernameTextBox.Text.Trim();
            string password = LoginPasswordBox.Password.Trim();
            string hashedPassword = HashPassword(password);

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Lütfen kullanıcı adı ve şifreyi girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand("SELECT Id, Role, LastSceneId, IsMusicOn FROM Users WHERE Username = @Username AND PasswordHash = @PasswordHash", connection);
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int userId = reader.GetInt32(0);
                            string role = reader.GetString(1);
                            int lastSceneId = reader.GetInt32(2);
                            bool musicOn = reader.GetBoolean(3);

                            App.Current.Properties["UserId"] = userId;
                            App.Current.Properties["UserRole"] = role;
                            App.Current.Properties["LastSceneId"] = lastSceneId;

                            IsMusicOn = musicOn;
                            MusicToggleCheckBox.IsChecked = musicOn;

                            MessageBox.Show("Giriş başarılı! Müzik ayarını kontrol edebilir ve 'Oyuna Başla'ya tıklayabilirsin.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoginPanel.Visibility = Visibility.Collapsed;
                            SettingsPanel.Visibility = Visibility.Collapsed;
                            ShowLoginButton.Visibility = Visibility.Collapsed;
                            ShowRegisterButton.Visibility = Visibility.Collapsed;

                        }

                        else
                        {
                            MessageBox.Show("Kullanıcı adı veya şifre hatalı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Giriş sırasında hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MusicToggleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetMusicSetting(true);
        }

        private void MusicToggleCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetMusicSetting(false);
        }

        private void SetMusicSetting(bool musicOn)
        {
            IsMusicOn = musicOn;

            if (App.Current.Properties.Contains("UserId") && App.Current.Properties["UserId"] is int userId)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand("UPDATE Users SET IsMusicOn = @musicOn WHERE Id = @userId", conn);
                        cmd.Parameters.AddWithValue("@musicOn", musicOn);
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Müzik ayarı güncellenemedi: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        private bool IsUserAdmin()
        {
            if (App.Current.Properties.Contains("UserRole"))
            {
                string role = App.Current.Properties["UserRole"].ToString();
                return role == "Admin";
            }
            return false;
        }
    }
}
