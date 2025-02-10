I've been trying to do something similar, and have been seeing the same kinds of wonky behaviors. 

So, you asked if:

> Anyone knows a workaround [...]?

That's what this is actually, a workaround not a canonical answer. But I'm testing it and it seems really stable on these physical devices:

- Android 12
- Android 13
- iPhone11 (iOS 18)
- iPhoneXR (iOS 17)
- iPad
- Windows Machine

It consists of this extension:

```
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
```
___

**Usage (Code-Behind)**

This version came first, because I wanted to elinate any variables having to do with the binding of the ViewModel. In testing where I use _only_ a file path `FixedPathForTest` located (from your original post) at `Path.Combine(FileSystem.CacheDirectory,"kqeah1iq.yih")`. To apply the latest file update, the syntax is:

`imagePhoto.Source = FixedPathForTest.Refresh();`

___


**Usage (ViewModel Binding)**

```xaml
<Image
    Source="{Binding Source}"
    HeightRequest="185"
    Aspect="AspectFit"/>
```
___
_It's important that the type of `Source` is `ImageSource` because even if you think you are setting a string to it, the "real" `ImageSource` is implicitly casted from it._
___

```csharp
class MainPageViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// This needs to be ImageSource and not String.
    /// </summary>
    public ImageSource? Source
    {
        get => _imageSource;
        set
        {
            ImageSource? preview = value;
            if (preview is FileImageSource fileImageSource &&
                fileImageSource.File is string path)
            {
                var fi = new FileInfo(path);
                if(!Equals(fi.LastWriteTime, _fileWriteTime))
                {
                    _imageSource = fileImageSource.File.Refresh();
                    OnPropertyChanged();
                }
            }
            else
            {
                if (!Equals(_imageSource, value))
                {
                    _imageSource = value;
                    OnPropertyChanged();
                }
            }
        }
    }
    ImageSource? _imageSource = default;
    DateTime _fileWriteTime = default;
}
```


