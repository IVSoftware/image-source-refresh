using Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace IVSoftware.Maui.Proto.Views.Camera
{
    public partial class MainPage : ContentPage
    {
        private ConfigForTest _cfg = ConfigForTest.Load();
        public MainPage()
        {
            InitializeComponent();
            Loaded += (sender, e) =>
            {
                labelInfo.Text = 
                    _cfg.IsPhoto
                    ? $"Expecting Photo"
                    : $"Expecting {_cfg?.ColorRotationValue}";
                imagePhoto.Source = _cfg?.FixedPathForTest.Refresh();
            };
        }
        private void OnRotateColor(object sender, EventArgs e)
        {
            _cfg.Rotate();
            _cfg.IsPhoto = false;
            labelInfo.Text = $"Expecting {_cfg.ColorRotationValue}";
            imagePhoto.Source = _cfg.FixedPathForTest.Refresh();
        }
        private async void OnCaptureClicked(object sender, EventArgs e)
        {
            var fileResult = await MediaPicker.Default.CapturePhotoAsync();
            if (fileResult == null) return;
            _cfg.IsPhoto = true;
            labelInfo.Text = $"Expecting Photo";

            // We want to do this asynchronous.
            using var memoryStream = new MemoryStream();
            using (Stream input = await fileResult.OpenReadAsync())
            {
                await input.CopyToAsync(memoryStream);
            }

            // We want to do this synchronous.
            File.WriteAllBytes(_cfg.FixedPathForTest, memoryStream.ToArray());
            imagePhoto.Source = _cfg.FixedPathForTest.Refresh();
        }
        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
    static partial class Extensions
    {
        public static ImageSource? Refresh(this string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                return ImageSource.FromStream(() => new MemoryStream(File.ReadAllBytes(path)));
            }
            else return null;
        }
    }

    enum ColorRotation { Red, Green, Yellow, Blue }

    class ConfigForTest : INotifyPropertyChanged
    {
        private ConfigForTest() { }
        public static ConfigForTest Load()
        {
            ConfigForTest? load = null;
            if (File.Exists(ConfigPathForTest))
            {
                try
                {
                    load = JsonConvert.DeserializeObject<ConfigForTest>(File.ReadAllText(ConfigPathForTest));
                }
                catch
                {
                    if (load is not null)
                    {
                        load.IsLoading = false;
                    }
                    Debug.WriteLine($"ADVISORY: Load failure will fall back to new config.");
                }
            }
            if (load is null) // Either new install or failed load.
            {
                load = new ConfigForTest();
                load.IsLoading = false;
                load.ColorRotationValue = ColorRotation.Red;
                load.Save();
            }
            if (load is null) throw new NullReferenceException("Load failed.");
            load.IsLoading = false;
            return load;
        }
        public bool IsLoading { get; private set; } = true;
        public ColorRotation ColorRotationValue
        {
            get => _colorRotationValue;
            set
            {
                if (!Equals(_colorRotationValue, value))
                {
                    _colorRotationValue = value;
                    if (!IsLoading)
                    {
                        localCopyEmbeddedResourceToFile();
                        OnPropertyChanged();
                    }
                }
                void localCopyEmbeddedResourceToFile()
                {
                    var assembly = typeof(ConfigForTest).Assembly;

                    var shortFileName = $"{ColorRotationValue}.png".ToLower();
                    string resourceName = typeof(ConfigForTest).Assembly.GetManifestResourceNames().First(_ => _.EndsWith(shortFileName));

                    using var memoryStream = new FileStream(
                        FixedPathForTest,
                        FileMode.OpenOrCreate,
                        FileAccess.Write);
                    using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (resourceStream == null) throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
                        resourceStream.CopyTo(memoryStream);
                    }
                }
            }
        }
        ColorRotation _colorRotationValue = (ColorRotation)(-1);
        public bool IsPhoto
        {
            get => _isPhoto;
            set
            {
                if (!Equals(_isPhoto, value))
                {
                    _isPhoto = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _isPhoto = default;

        public void Rotate()
        {
            ColorRotationValue = (ColorRotation)((((int)ColorRotationValue) + 1) % 4);
            Save();
        }
        public string ColorRotationMauiAsset
        {
            get
            {
                string fileName = $"{ColorRotationValue}.png".ToLower();
                return Path.Combine("Resources", "Images", fileName);
            }
        }
        public void Save() =>
                File.WriteAllText(
                    ConfigPathForTest,
                    JsonConvert.SerializeObject(this, Formatting.Indented));
        public static string ConfigPathForTest
        {
            get
            {
                if (_configPathForTest is null)
                {
                    string appData;
                    if (DeviceInfo.Platform == DevicePlatform.WinUI)
                    {
                        appData = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "StackOverflow",
                            "Image Bound");
                    }
                    else
                    {
                        appData = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
                    }
                    if (!Directory.Exists(appData))
                    {
                        Directory.CreateDirectory(appData);
                    }
                    _configPathForTest = Path.Combine(
                        appData,
                       "config-for-test.json");
                }
                return _configPathForTest;
            }
        }
        static string? _configPathForTest = default;

        public string FixedPathForTest { get; } = Path.Combine(
                    FileSystem.CacheDirectory,
                   "kqeah1iq.yih");

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (!IsLoading)
            {
                Save();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
