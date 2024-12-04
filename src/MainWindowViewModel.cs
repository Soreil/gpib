using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.IO.Ports;

namespace ar488;

public partial class MainWindowViewModel : ObservableObject
{

    [ObservableProperty]
    public partial List<TextBlock> Output_Log { get; set; } = [];

    //List of COM Ports stored in this
    [ObservableProperty]
    public partial List<string> PortList { get; set; } = [];

    [ObservableProperty]
    public partial List<int> BaudRates { get; set; } = [9600, 19200, 38400, 57600, 115200];

    [ObservableProperty]
    public partial List<int> DataBitsOptions { get; set; } = [9, 8, 7, 6, 5];

    //<ComboBoxItem Content = IsSelected="False" IsEnabled="True"/>
    //<ComboBoxItem Content = IsSelected="False" IsEnabled="True"/>
    //<ComboBoxItem Content = IsSelected="True" IsEnabled="True"/>
    //<ComboBoxItem Content = IsSelected="False" IsEnabled="True"/>
    //<ComboBoxItem Content =  IsSelected="False" IsEnabled="True"/>

    [ObservableProperty]
    public partial List<Parity> ParityOptions { get; set; } = [Parity.Even, Parity.Odd, Parity.None, Parity.Mark, Parity.Space];

    [ObservableProperty]
    public partial List<StopBits> StopBitsOptions { get; set; } = [StopBits.One,StopBits.OnePointFive,StopBits.Two];

    //COM Port Information, updated by GUI                              
    [ObservableProperty]
    public partial string COM_Port_Name { get; set; } = "";
    [ObservableProperty]
    public partial int COM_BaudRate_Value { get; set; } = 115200;
    [ObservableProperty]
    public partial Parity COM_Parity_Value { get; set; } = Parity.None;
    [ObservableProperty]
    public partial StopBits COM_StopBits_Value { get; set; } = StopBits.One;
    [ObservableProperty]
    public partial int COM_DataBits_Value { get; set; } = 8;
    [ObservableProperty]
    public partial int COM_Handshake_Value { get; set; } = 0;
    [ObservableProperty]
    public partial int COM_WriteTimeout_Value { get; set; } = 3000;
    [ObservableProperty]
    public partial int COM_ReadTimeout_Value { get; set; } = 3000;
    [ObservableProperty]
    public partial bool COM_RtsEnable { get; set; } = false;
    [ObservableProperty]
    public partial int COM_GPIB_Address_Value { get; set; } = 1;

    //Save Data Directory
    [ObservableProperty]
    public partial string folder_Directory { get; set; } = "";

    [ObservableProperty]
    public partial BlockingCollection<string> Calibration_Data { get; set; } = [];

    [RelayCommand]
    private void ClearInfoLog()
    {
        Output_Log.Clear();
    }
}
