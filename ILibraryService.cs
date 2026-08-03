using MusicPlayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Services;
public interface ILibraryService
{
    //current list of songs the user has added to their library
    List<Song> Songs { get; }

    //lets the user pick individual audio files from their device
    Task PickAndAddFilesAsync();

    //lets the user pick a folder and adds every audio file to it
    Task ScanFolderAndAddAsync();

    //loads the saved library on startup
    Task LoadLibraryAsync();

    //saves the current library after change is made
    Task SaveLibraryAsync();
}