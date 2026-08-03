using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Id3;
using System.Text.Json;

namespace MusicPlayer.Services;

public class LibraryService : ILibraryService
{
    // Keep the song list in memory so the UI can bind to it directly.
    public List<Song> Songs { get; private set; } = new();

    // We store the library as a JSON file in the app's own data folder.
    // FileSystem.AppDataDirectory is safe to write to on both Windows and Android.
    private readonly string _libraryFilePath =
        Path.Combine(FileSystem.AppDataDirectory, "library.json");

    public async Task PickAndAddFilesAsync()
    {
        // FilePicker is a MAUI cross-platform API - works the same on
        // Windows and Android without platform-specific code.
        var options = new PickOptions
        {
            PickerTitle = "Select audio files",
            FileTypes = FilePickerFileType.Images // placeholder, replaced below
        };

        // Custom file type filter so only audio files show up.
        var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.WinUI, new[] { ".mp3", ".ogg", ".flac", ".wav" } },
            { DevicePlatform.Android, new[] { "audio/*" } }
        });
        options.FileTypes = customFileType;

        var results = await FilePicker.Default.PickMultipleAsync(options);

        if (results is null) return; // user cancelled

        foreach (var file in results)
        {
            await AddSongFromFileAsync(file.FullPath);
        }

        await SaveLibraryAsync();
    }

    public async Task ScanFolderAndAddAsync()
    {
        // MAUI doesn't have a built-in folder picker on all platforms,
        // so this typically needs a small platform-specific helper
        // (or a community NuGet like CommunityToolkit.Maui's FolderPicker).
        // For now we assume a folder path is returned by that picker.
        string? folderPath = await CommunityToolkit.Maui.Storage.FolderPicker.Default
            .PickAsync(CancellationToken.None)
            .ContinueWith(t => t.Result.Folder?.Path);

        if (string.IsNullOrEmpty(folderPath)) return;

        // Look for common audio extensions inside the chosen folder.
        var audioExtensions = new[] { ".mp3", ".ogg", ".flac", ".wav" };
        var files = Directory.GetFiles(folderPath)
            .Where(f => audioExtensions.Contains(Path.GetExtension(f).ToLower()));

        foreach (var filePath in files)
        {
            await AddSongFromFileAsync(filePath);
        }

        await SaveLibraryAsync();
    }

    // Reads ID3 tags from a file and adds it to the library.
    private Task AddSongFromFileAsync(string filePath)
    {
        // Avoid adding the exact same file twice.
        if (Songs.Any(s => s.FilePath == filePath))
            return Task.CompletedTask;

        var song = new Song { FilePath = filePath };

        try
        {
            // Id3 library reads tag data (title, artist, album, etc.)
            using var mp3 = new Mp3(filePath);
            var tag = mp3.GetTag(Id3TagFamily.Version2);

            if (tag != null)
            {
                song.Title = string.IsNullOrWhiteSpace(tag.Title) ? Path.GetFileNameWithoutExtension(filePath) : tag.Title.Value;
                song.Artist = string.IsNullOrWhiteSpace(tag.Artists) ? "Unknown Artist" : tag.Artists.Value.ToString();
                song.Album = string.IsNullOrWhiteSpace(tag.Album) ? "Unknown Album" : tag.Album.Value;
            }
        }
        catch
        {
            // Not every file will have readable tags - fall back to
            // the filename rather than crashing the app.
            song.Title = Path.GetFileNameWithoutExtension(filePath);
        }

        Songs.Add(song);
        return Task.CompletedTask;
    }

    public async Task LoadLibraryAsync()
    {
        if (!File.Exists(_libraryFilePath))
        {
            Songs = new List<Song>();
            return;
        }

        // Read the saved JSON and turn it back into Song objects.
        var json = await File.ReadAllTextAsync(_libraryFilePath);
        Songs = JsonSerializer.Deserialize<List<Song>>(json) ?? new List<Song>();
    }

    public async Task SaveLibraryAsync()
    {
        // Serialize the in-memory list to JSON and write it to disk.
        var json = JsonSerializer.Serialize(Songs);
        await File.WriteAllTextAsync(_libraryFilePath, json);
    }
}