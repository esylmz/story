using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using static System.Formats.Asn1.AsnWriter;

namespace story
{
    public partial class MainWindow : Window
    {
        public static string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=story;Integrated Security=True;";

        private Dictionary<int, Scene> scenes = new Dictionary<int, Scene>();
        private int currentSceneId = 1;
        private MediaPlayer backgroundPlayer = new MediaPlayer();
        private bool isMusicOn = true;

        private CancellationTokenSource typingCancellation;
        private string currentFullText = "";

        private const double BaseWidth = 800;
        private const double BaseHeight = 450;

        public MainWindow(int startSceneId, bool isMusicOn)
        {
            InitializeComponent();
            this.isMusicOn = isMusicOn;
            currentSceneId = startSceneId;

            LoadScenesFromDatabase();
            SizeChanged += MainWindow_SizeChanged;

            if (this.isMusicOn)
                PlayBackgroundMusic();

            _ = ShowSceneAsync(startSceneId);

            MessageBox.Show("İpucu: Metni daha hızlı yüklemek için Space tuşuna basabilirsiniz.",
                    "Bilgilendirme",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
        }

        public MainWindow() : this(1, true) { }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double scaleX = e.NewSize.Width / BaseWidth;
            double scaleY = e.NewSize.Height / BaseHeight;
            double scale = Math.Min(scaleX, scaleY);

            if (this.Content is FrameworkElement content)
            {
                content.LayoutTransform = new ScaleTransform(scale, scale);
            }
        }

        private void PlayBackgroundMusic()
        {
            backgroundPlayer.Open(new Uri("Assets/soundtrack.mp3", UriKind.Relative));
            backgroundPlayer.MediaEnded += (s, e) =>
            {
                backgroundPlayer.Position = TimeSpan.Zero;
                backgroundPlayer.Play();
            };
            backgroundPlayer.Volume = 0.3;
            backgroundPlayer.Play();
        }

        private void PlayClickSound()
        {
            // Tek seferde yaratılan MediaPlayer kullanımı için öneri
            MediaPlayer clickPlayer = new MediaPlayer();
            clickPlayer.Open(new Uri("click.mp3", UriKind.Relative));
            clickPlayer.Play();
        }

        private void LoadScenesFromDatabase()
        {
            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();
                string query = "SELECT SceneID, SceneText, Choice1Text, Choice1Next, Choice2Text, Choice2Next, isEnding, ImageName FROM Scene";

                using SqlCommand cmd = new SqlCommand(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    scenes[reader.GetInt32(0)] = new Scene
                    {
                        Id = reader.GetInt32(0),
                        Description = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        Choice1Text = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Choice1Next = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        Choice2Text = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Choice2Next = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                        IsEnding = reader.IsDBNull(6) ? false : reader.GetBoolean(6),
                        ImageName = reader.IsDBNull(7) ? "" : reader.GetString(7)
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veritabanı hatası: " + ex.Message);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                typingCancellation?.Cancel();
            }
        }

        private async Task ShowSceneAsync(int sceneId)
        {
            if (!scenes.ContainsKey(sceneId))
            {
                MessageBox.Show("Bu sahne bulunamadı.");
                return;
            }

            Scene scene = scenes[sceneId];

            await FadeOut(txtSceneText);
            await FadeOut(imgBackground);

            txtSceneText.Text = "";

            if (!string.IsNullOrEmpty(scene.ImageName))
            {
                try
                {
                    imgBackground.Source = new BitmapImage(new Uri($"Assets/{scene.ImageName}", UriKind.Relative));
                }
                catch
                {
                    imgBackground.Source = null;
                }
            }
            else
            {
                imgBackground.Source = null;
            }

            await FadeIn(imgBackground);
            await FadeIn(txtSceneText);

            await ShowTextWithTypingEffect(scene.Description);

            btnChoice1.Content = scene.Choice1Text;
            btnChoice2.Content = scene.Choice2Text;

            btnChoice1.IsEnabled = !string.IsNullOrEmpty(scene.Choice1Text);
            btnChoice2.IsEnabled = !string.IsNullOrEmpty(scene.Choice2Text);
            btnChoice1.Visibility = btnChoice1.IsEnabled ? Visibility.Visible : Visibility.Hidden;
            btnChoice2.Visibility = btnChoice2.IsEnabled ? Visibility.Visible : Visibility.Hidden;

            currentSceneId = sceneId;

            // Ayarlara kaydet (opsiyonel)
            Properties.Settings.Default.LastSceneId = currentSceneId;
            Properties.Settings.Default.Save();

            // Veritabanına kaydet (async)
            await UpdateUserProgressAndMusicSettingAsync();

            if (scene.IsEnding)
            {
                btnChoice1.IsEnabled = false;
                btnChoice2.IsEnabled = false;
                MessageBox.Show("Hikaye burada sona erdi.");
            }
        }
        private async Task UpdateUserProgressAndMusicSettingAsync()
        {
            if (App.Current.Properties.Contains("UserId") && App.Current.Properties["UserId"] is int userId)
            {
                try
                {
                    using SqlConnection conn = new SqlConnection(connectionString);
                    await conn.OpenAsync();

                    SqlCommand cmd = new SqlCommand("UPDATE Users SET LastSceneId = @lastSceneId, IsMusicOn = @musicOn WHERE Id = @userId", conn);
                    cmd.Parameters.AddWithValue("@lastSceneId", currentSceneId);
                    cmd.Parameters.AddWithValue("@musicOn", isMusicOn);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("İlerleme güncellenemedi: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ShowTextWithTypingEffect(string fullText)
        {
            typingCancellation?.Cancel();
            typingCancellation = new CancellationTokenSource();
            var token = typingCancellation.Token;

            var builder = new StringBuilder();
            currentFullText = fullText;

            for (int i = 0; i < fullText.Length; i++)
            {
                if (token.IsCancellationRequested)
                {
                    txtSceneText.Text = fullText;
                    return;
                }

                builder.Append(fullText[i]);

                await Dispatcher.InvokeAsync(() =>
                {
                    txtSceneText.Text = builder.ToString();
                });

                await Task.Delay(25);
            }
        }

        private async void btnChoice1_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
            DisableChoices();

            if (scenes.TryGetValue(currentSceneId, out var scene) && scene.Choice1Next != 0)
                await ShowSceneAsync(scene.Choice1Next);
        }

        private async void btnChoice2_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
            DisableChoices();

            if (scenes.TryGetValue(currentSceneId, out var scene) && scene.Choice2Next != 0)
                await ShowSceneAsync(scene.Choice2Next);
        }

        private void DisableChoices()
        {
            btnChoice1.IsEnabled = false;
            btnChoice2.IsEnabled = false;
            btnChoice1.Visibility = Visibility.Hidden;
            btnChoice2.Visibility = Visibility.Hidden;
        }

        private async void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            await ShowSceneAsync(1);
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            Basla anaEkran = new Basla();
            anaEkran.Show();
            this.Close();
        }

        public void ToggleMusic()
        {
            IsMusicOn = !IsMusicOn;
        }

        public bool IsMusicOn
        {
            get => isMusicOn;
            set
            {
                if (isMusicOn != value)
                {
                    isMusicOn = value;
                    if (isMusicOn)
                        backgroundPlayer.Play();
                    else
                        backgroundPlayer.Pause();

                    // Async çağrı zor olacağından senkron versiyonu çağırıyoruz
                    UpdateUserProgressAndMusicSetting();
                }
            }
        }

        private void UpdateUserProgressAndMusicSetting()
        {
            if (App.Current.Properties.Contains("UserId") && App.Current.Properties["UserId"] is int userId)
            {
                try
                {
                    using SqlConnection conn = new SqlConnection(connectionString);
                    conn.Open();

                    SqlCommand cmd = new SqlCommand("UPDATE Users SET LastSceneId = @sceneId, IsMusicOn = @musicOn WHERE Id = @userId", conn);
                    cmd.Parameters.AddWithValue("@sceneId", currentSceneId);
                    cmd.Parameters.AddWithValue("@musicOn", isMusicOn);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Kullanıcı ilerlemesi güncellenemedi: " + ex.Message);
                }
            }
        }

        private Task FadeIn(UIElement element, double duration = 0.5)
        {
            var tcs = new TaskCompletionSource<bool>();
            var animation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(duration));
            animation.Completed += (s, e) => tcs.SetResult(true);
            element.BeginAnimation(UIElement.OpacityProperty, animation);
            return tcs.Task;
        }

        private Task FadeOut(UIElement element, double duration = 0.5)
        {
            var tcs = new TaskCompletionSource<bool>();
            var animation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(duration));
            animation.Completed += (s, e) => tcs.SetResult(true);
            element.BeginAnimation(UIElement.OpacityProperty, animation);
            return tcs.Task;
        }

        public class Scene
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public string Choice1Text { get; set; }
            public int Choice1Next { get; set; }
            public string Choice2Text { get; set; }
            public int Choice2Next { get; set; }
            public bool IsEnding { get; set; }
            public string ImageName { get; set; }
        }
    }
}
