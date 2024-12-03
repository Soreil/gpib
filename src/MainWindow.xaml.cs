using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ar488;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    //List of COM Ports stored in this
    List<string> portList = [];

    //COM Port Information, updated by GUI
    private string COM_Port_Name = "";
    private int COM_BaudRate_Value = 115200;
    private int COM_Parity_Value = 0;
    private int COM_StopBits_Value = 1;
    private int COM_DataBits_Value = 8;
    private int COM_Handshake_Value = 0;
    private int COM_WriteTimeout_Value = 3000;
    private int COM_ReadTimeout_Value = 3000;
    private bool COM_RtsEnable = false;
    private int COM_GPIB_Address_Value = 1;

    //Save Data Directory
    string folder_Directory = "";

    public BlockingCollection<string> Calibration_Data = [];

    public ObservableCollection<TextBlock> Output_Log
    {
        get { return (ObservableCollection<TextBlock>)GetValue(Output_Log_Property); }
        set { SetValue(Output_Log_Property, value); }
    }

    public static readonly DependencyProperty Output_Log_Property = DependencyProperty.Register(nameof(Output_Log), typeof(ObservableCollection<TextBlock>), typeof(TextBlock), new PropertyMetadata(default(ObservableCollection<TextBlock>)));

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Output_Log = [];
        Get_COM_List();
        GetSoftwarePath();
        Insert_Log("Make sure GPIB Address is correct.", 4);
        Insert_Log("Choose the correct COM port from the list.", 4);
        var isFolderCreated = FolderCreation(folder_Directory);
        if (!isFolderCreated)
        {
            Insert_Log("Failed to create Calibration Data Folder, Try Again.", 1);
        }
        Insert_Log("Enter integer value into Start and Stop Address Input Field.", 6);
        Insert_Log("Old Revision, Start Address: 64  Stop Address: 512", 6);
        Insert_Log("New Revision, Start Address: 20480  Stop Address: 22527", 6);
    }

    private void GetSoftwarePath()
    {
        try
        {
            folder_Directory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" + "Calibration Data (HP3457A)";
            Insert_Log("Calibration Data will be saved inside the software directory.", 3);
            Insert_Log(folder_Directory, 3);
        }
        catch (Exception)
        {
            Insert_Log("Cannot get software directory path. Move Software to different location and Try Again.", 1);
        }
    }

    private static bool FolderCreation(string folderPath)
    {
        try
        {
            System.IO.Directory.CreateDirectory(folderPath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void Get_COM_List()
    {
        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'");
        var portnames = SerialPort.GetPortNames();
        var ports = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Caption"].ToString());
        portList = portnames.Select(n => n + " - " + ports.FirstOrDefault(s => s.Contains('(' + n + ')'))).ToList();
        foreach (string p in portList)
        {
            UpdateList(p);
        }
    }

    private void UpdateList(string data)
    {
        ListBoxItem COM_itm = new()
        {
            Content = data
        };
        COM_List.Items.Add(COM_itm);
    }

    private void COM_Refresh_Click(object sender, RoutedEventArgs e)
    {
        COM_List.Items.Clear();
        Get_COM_List();
    }

    private void COM_List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        try
        {
            string temp = COM_List.SelectedItem.ToString().Split([": "], StringSplitOptions.None).Last();
            string COM = temp[..temp.IndexOf(" -")];
            COM_Port.Text = COM;
            COM_Open_Check();

        }
        catch (Exception)
        {
            Insert_Log("Select a Valid COM Port.", 2);
        }
    }

    private bool COM_Open_Check()
    {
        try
        {
            using var sp = new SerialPort(COM_Port.Text, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            sp.WriteTimeout = 500;
            sp.ReadTimeout = 500;
            sp.Handshake = Handshake.None;
            sp.RtsEnable = true;
            sp.Open();
            Thread.Sleep(100);
            sp.Close();
            Insert_Log(COM_Port.Text + " is open and ready for communication.", 0);
        }
        catch (Exception Ex)
        {
            COM_Port.Text = string.Empty;
            Insert_Log(Ex.ToString(), 1);
            Insert_Log(COM_Port.Text + " is closed. Probably being used by a software.", 1);
            Insert_Log("Try another COM Port or check if COM is already used by another software.", 4);
            return false;
        }
        return true;
    }

    private bool COM_Config_Updater()
    {
        COM_Port_Name = COM_Port.Text.ToUpper().Trim();

        string BaudRate = COM_Bits.SelectedItem.ToString().Split([": "], StringSplitOptions.None).Last();
        COM_BaudRate_Value = Int32.Parse(BaudRate);

        string DataBits = COM_DataBits.SelectedItem.ToString().Split([": "], StringSplitOptions.None).Last();
        COM_DataBits_Value = Int32.Parse(DataBits);

        bool isNum = int.TryParse(COM_write_timeout.Text.Trim(), out int Value);
        if (isNum & Value > 0)
        {
            COM_WriteTimeout_Value = Value;
            COM_write_timeout.Text = Value.ToString();
        }
        else
        {
            COM_write_timeout.Text = "1000";
            Insert_Log("Write Timeout must be a positive integer.", 1);
            return false;
        }

        isNum = int.TryParse(COM_read_timeout.Text.Trim(), out Value);
        if (isNum & Value > 0)
        {
            COM_ReadTimeout_Value = Value;
            COM_read_timeout.Text = Value.ToString();
        }
        else
        {
            COM_read_timeout.Text = "1000";
            Insert_Log("Read Timeout must be a positive integer.", 1);
        }

        isNum = int.TryParse(GPIB_Address.Text.Trim(), out Value);
        if (isNum & Value > 0)
        {
            COM_GPIB_Address_Value = Value;
            GPIB_Address.Text = Value.ToString();
        }
        else
        {
            GPIB_Address.Text = "1";
            Insert_Log("GPIB Address must be a positive integer.", 1);
            return false;
        }

        string Parity = COM_Parity.SelectedItem.ToString().Split([": "], StringSplitOptions.None).Last();
        COM_Parity_Value = Parity switch
        {
            "Even" => 2,
            "Odd" => 1,
            "None" => 0,
            "Mark" => 3,
            "Space" => 4,
            _ => 0,
        };

        string StopBits = COM_Stop.SelectedItem.ToString().Split([": "], StringSplitOptions.None).Last();
        COM_StopBits_Value = StopBits switch
        {
            "1" => 1,
            "1.5" => 3,
            "2" => 2,
            _ => 1,
        };

        string Flow = COM_Flow.SelectedItem.ToString().Split([": "], StringSplitOptions.None).Last();
        COM_Handshake_Value = Flow switch
        {
            "Xon/Xoff" => 1,
            "Hardware" => 2,
            "None" => 0,
            _ => 1,
        };
        string rts = COM_rtsEnable.SelectedItem.ToString().Split([": "], StringSplitOptions.None).Last();
        COM_RtsEnable = rts switch
        {
            "True" => true,
            "False" => false,
            _ => true,
        };
        return true;
    }

    //Inserts a message into the output log control
    private void Insert_Log(string Message, int Code)
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt");

        (var Status, var Color) = Code switch
        {
            0 => ("[Success]", Brushes.Green),
            1 => ("[Error]", Brushes.Red),
            2 => ("[Warning]", Brushes.Orange),
            3 => ("", Brushes.DodgerBlue),
            4 => ("", Brushes.BlueViolet),
            5 => ("", Brushes.Black),
            6 => ("", Brushes.Green),
            _ => ("", Brushes.Magenta),
        };

        Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
        {
            TextBlock Output_Log_Text = new()
            {
                Foreground = Color,
                Text = $"[{date}] {Status} {Message.Trim()}"
            };
            Output_Log.Add(Output_Log_Text);
            Output_Log_Scroll.ScrollToBottom();
        }));
    }

    private void Info_Clear_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Output_Log.Clear();
        }
        catch (Exception)
        {

        }
    }

    private (bool, string) Serial_Query_AR488(string command)
    {
        try
        {
            Lock_controls();
            using var serial = new SerialPort(COM_Port_Name, COM_BaudRate_Value, (Parity)COM_Parity_Value, COM_DataBits_Value, (StopBits)COM_StopBits_Value);
            serial.WriteTimeout = COM_WriteTimeout_Value;
            serial.ReadTimeout = COM_ReadTimeout_Value;
            serial.RtsEnable = COM_RtsEnable;
            serial.Handshake = (Handshake)COM_Handshake_Value;
            serial.Open();
            serial.WriteLine("++addr " + COM_GPIB_Address_Value);
            Thread.Sleep(100);
            serial.WriteLine(command);
            string data = serial.ReadLine();
            serial.Close();
            Unlock_controls();
            return (true, data);
        }
        catch (Exception)
        {
            Insert_Log("Serial Query Failed, check COM settings or connection.", 1);
            Unlock_controls();
            return (false, "");
        }
    }

    private bool Serial_Write(string command)
    {
        try
        {
            Lock_controls();
            using var serial = new SerialPort(COM_Port_Name, COM_BaudRate_Value, (Parity)COM_Parity_Value, COM_DataBits_Value, (StopBits)COM_StopBits_Value);
            serial.WriteTimeout = COM_WriteTimeout_Value;
            serial.ReadTimeout = COM_ReadTimeout_Value;
            serial.RtsEnable = COM_RtsEnable;
            serial.Handshake = (Handshake)COM_Handshake_Value;
            serial.Open();
            serial.WriteLine("++addr " + COM_GPIB_Address_Value);
            Thread.Sleep(100);
            serial.WriteLine(command);
            serial.Close();
            Unlock_controls();
            return true;
        }
        catch (Exception)
        {
            Insert_Log("Serial Write Failed, check COM settings or connection.", 1);
            Unlock_controls();
            return false;
        }
    }

    private (bool, string) Serial_Query_HP3457A(string command)
    {
        try
        {
            Lock_controls();
            using var serial = new SerialPort(COM_Port_Name, COM_BaudRate_Value, (Parity)COM_Parity_Value, COM_DataBits_Value, (StopBits)COM_StopBits_Value);
            serial.WriteTimeout = COM_WriteTimeout_Value;
            serial.ReadTimeout = COM_ReadTimeout_Value;
            serial.RtsEnable = COM_RtsEnable;
            serial.Handshake = (Handshake)COM_Handshake_Value;
            serial.Open();
            serial.WriteLine("++addr " + COM_GPIB_Address_Value);
            serial.WriteLine("++ren 1");
            serial.WriteLine("++auto 2");
            Thread.Sleep(100);
            serial.WriteLine(command);
            string data = serial.ReadLine();
            serial.Close();
            Unlock_controls();
            return (true, data);
        }
        catch (Exception)
        {
            Insert_Log("Serial Query Failed, check COM settings or connection.", 1);
            Unlock_controls();
            return (false, "");
        }
    }

    private void AR488_Version_Click(object sender, RoutedEventArgs e)
    {
        if (COM_Config_Updater() == true)
        {
            (bool check, string return_data) = Serial_Query_AR488("++ver");
            if (check == true)
            {
                Insert_Log(return_data, 0);
            }
        }
        else
        {
            Insert_Log("COM Info is invalid. Correct any errors and try again.", 1);
        }
    }

    private void AR488_Reset_Click(object sender, RoutedEventArgs e)
    {
        if (COM_Config_Updater() == true)
        {
            if (Serial_Write("++rst") == true)
            {
                Insert_Log("Reset command was send successfully.", 0);
            }
        }
    }

    private void Verify_3457A_Click(object sender, RoutedEventArgs e)
    {
        if (COM_Config_Updater() == true)
        {
            (bool check, string return_data) = Serial_Query_HP3457A("ID?");
            if (check == true)
            {
                Insert_Log(return_data, 0);
                if (string.Equals(return_data.Trim(), "HP3457A") == true)
                {
                    Insert_Log("Verify Successful.", 0);
                }
                else
                {
                    Insert_Log("Verify Failed. Expected ID? query is HP3457A.", 1);
                    Insert_Log("Try Again.", 1);
                }
            }
        }
        else
        {
            Insert_Log("COM Info is invalid. Correct any errors and try again.", 1);
        }
    }

    private void Reset_3457A_Click(object sender, RoutedEventArgs e)
    {
        if (COM_Config_Updater() == true)
        {
            if (Serial_Write("RESET") == true)
            {
                Insert_Log("Reset command was send successfully. Please wait for few seconds.", 0);
            }
        }
    }

    private void Lock_controls()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
        {
            COM_Port_List.IsEnabled = false;
            HP3457A_COM_Config.IsEnabled = false;
            HP3457A_Cal_Config.IsEnabled = false;
        }));
    }

    private void Unlock_controls()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
        {
            COM_Port_List.IsEnabled = true;
            HP3457A_COM_Config.IsEnabled = true;
            HP3457A_Cal_Config.IsEnabled = true;
        }));
    }

    //converts a string into a number
    private static (bool, double) Text_Num(string text, bool allowNegative, bool isInteger)
    {
        if (isInteger == true)
        {
            bool isValid = int.TryParse(text, out int value);
            if (isValid == true)
            {
                if (allowNegative == false)
                {
                    if (value < 0)
                    {
                        return (false, 0);
                    }
                    else
                    {
                        return (true, value);
                    }
                }
                else
                {
                    return (true, value);
                }
            }
            else
            {
                return (false, 0);
            }
        }
        else
        {
            bool isValid = double.TryParse(text, out double value);
            if (isValid == true)
            {
                if (allowNegative == false)
                {
                    if (value < 0)
                    {
                        return (false, 0);
                    }
                    else
                    {
                        return (true, value);
                    }
                }
                else
                {
                    return (true, value);
                }
            }
            else
            {
                return (false, 0);
            }
        }
    }

    private void Get_Calibration_Data(object sender, RoutedEventArgs e)
    {
        while (Calibration_Data.TryTake(out _)) { }
        (bool isValid_Start_Address, double Start_Address) = Text_Num(Start_Address_Input_Field.Text, false, true);
        (bool isValid_Stop_Address, double Stop_Address) = Text_Num(Stop_Address_Input_Field.Text, false, true);
        if (isValid_Start_Address == true & isValid_Stop_Address == true)
        {
            try
            {
                if (COM_Config_Updater() == true)
                {
                    Lock_controls();
                    Task.Run(() =>
                    {
                        using var serial = new SerialPort(COM_Port_Name, COM_BaudRate_Value, (Parity)COM_Parity_Value, COM_DataBits_Value, (StopBits)COM_StopBits_Value);
                        serial.WriteTimeout = COM_WriteTimeout_Value;
                        serial.ReadTimeout = COM_ReadTimeout_Value;
                        serial.RtsEnable = COM_RtsEnable;
                        serial.Handshake = (Handshake)COM_Handshake_Value;
                        serial.Open();
                        serial.WriteLine("++addr " + COM_GPIB_Address_Value);
                        serial.WriteLine("++ren 1");
                        serial.WriteLine("++auto 2");
                        Thread.Sleep(100);
                        serial.WriteLine("RESET");
                        Thread.Sleep(4000);
                        serial.WriteLine("TRIG 4");
                        Thread.Sleep(100);

                        for (int i = (int)Start_Address; i <= (int)Stop_Address; i = i + 2)
                        {
                            serial.WriteLine("PEEK " + i);
                            serial.WriteLine("++read");
                            string Serial_Data = serial.ReadLine();
                            Insert_Log("PEEK " + i + "," + "0x" + i.ToString("X4") + "," + Serial_Data, 6);
                            Calibration_Data.Add("0x" + i.ToString("X4") + "," + Serial_Data.Trim());
                        }

                        serial.Close();
                        Insert_Log("HP 3457A Calibration Dump Finished.", 0);
                        Write_Calibration_Data_To_File();
                        Unlock_controls();
                    });
                }
            }
            catch (Exception Ex)
            {
                while (Calibration_Data.TryTake(out _)) { }
                Insert_Log(Ex.Message, 1);
                Insert_Log("Failed to get Calibration Data.", 1);
                Unlock_controls();
            }
        }
        else
        {
            Insert_Log("Start and Stop Address values must be real positive numbers. Try Again.", 1);
        }
    }

    private void Write_Calibration_Data_To_File()
    {
        int Calibration_Data_Count = Calibration_Data.Count;
        try
        {
            using (TextWriter datatotxt = new StreamWriter(folder_Directory + @"\" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss tt") + "_Cal.txt", true))
            {
                for (int i = 0; i < Calibration_Data_Count; i++)
                {
                    datatotxt.WriteLine(Calibration_Data.Take());
                }
            }
        }
        catch (Exception)
        {
            Insert_Log("Cannot save Calibration Data to text file.", 1);
        }
    }
}
