using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Models;
public class Song
{
    //used to play the file
    public string FilePath { get; set; } = string.Empty;
    //metadata read from ID3 tags
    public string Title { get; set; } = "Unknown Title";
    public string Artist { get; set; } = "Unknown Artist";
    public string Album { get; set; } = "Unknown Album";
    //displays how much of the song is left
    public TimeSpan Duration { get; set; }
    //cover art
    public string? AlbumArtUrl { get; set; }
}
