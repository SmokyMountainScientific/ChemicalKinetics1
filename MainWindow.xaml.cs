/// GUI to control Thorlabs CCS100 spectrometer in kinetics experiment
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//using System.Windows.Forms;
using System.IO;  // for writting and reading files
//using Thorlabs.CCS_Series;
using Thorlabs.ccs.interop;  // this works, different from initial code
using System.Management;  // for querying usb port
using System.IO.Ports;    // for communication with pump
using System.Collections.Generic;

namespace SpectrometerControl1
{
    public partial class MainWindow : Window
    {
        Boolean connected = false;
        //Boolean triggerMode = false;  // trigger with software
        String resourceName = "";  // spectrometer ID
        String specDesc;   // spectrometer description
        int status;
        int res;
        int scanNo;  // number of scans, defined when run button pressed
        int[,] data;  // file for spectral data, first var is scan number
        int[] temp = new int[3700];  //3648];
        double[] wavelengths = new double[3700]; //3648];
        string[] connStatus = { "Not Connected", "Connected" }; 
        private TLCCS spectrometer;
        SerialPort cPort; // = new SerialPort();
        int mode = 0;
        Boolean valveD = false;
        Boolean dirB = false;
        int[] times = new int[20];

        // closing window
        private void MainWindow_WindowClosing(object sender, EventArgs e)
        { if(spectrometer != null)
            {
                 spectrometer.Dispose(); 
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            scans.Visibility = Visibility.Collapsed;
            label3.Visibility = Visibility.Collapsed;
            totalTime.Visibility = Visibility.Collapsed;
            label5.Visibility = Visibility.Collapsed;

            halfT.Visibility = Visibility.Collapsed;
            tHalflabel.Visibility = Visibility.Collapsed;
            volume.Visibility = Visibility.Visible;
            volLabel.Visibility = Visibility.Visible;
            direction.Visibility = Visibility.Visible;
            dirText.Visibility = Visibility.Visible;
            valve.Visibility = Visibility.Visible;
            valveText.Visibility = Visibility.Visible;
            box1.Visibility = Visibility.Collapsed;
            //   Task.Run(() => Application.Run(new Form1()));
        }
        // connect button
        public void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if(connected == true)
            {
                conText.Text = "Not Connected";
                spectrometer.Dispose();
            }
            else
            {
                conText.Text = "Connecting";
                var usbDevices = GetUSBDevices();  //  query attached devices
                foreach (var usbDevice in usbDevices)
                {
                //    MessageBox.Show("spectrometer: " + usbDevice.DeviceID);
                    resourceName = usbDevice.DeviceID;
                    specDesc = usbDevice.Description;
               }
                spectrometer = new TLCCS(resourceName, false, false);
                res = spectrometer.getDeviceStatus(out status);
                String ErrText = "No error";
                if (res != 0)
                {
                    ErrText = res.ToString();
                }
             //   MessageBox.Show("spectrometer status: " + ErrText);
                conText.Text = "Connected: "+specDesc;
            }
                connected = !connected;
        }
        public void portButton_Click(object sender, RoutedEventArgs e)
        {
            Boolean portCon = false; 
            String[] ports = SerialPort.GetPortNames();
            try
            {
                System.Diagnostics.Debug.WriteLine("In try loop");
                foreach (String ComPort in ports)
                {
                    if (ComPort == "COM5") { }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("In ComPort " + ComPort + " loop");
                        //cPort.portName = 
                        if (portCon == false)  // is port connected?                   
                        {
                            cPort = new SerialPort(ComPort, 9600);
                            cPort.Open();
                            System.Diagnostics.Debug.WriteLine("port Opened");
                            cPort.Write("*");
                            System.Diagnostics.Debug.WriteLine("* sent");
                            String c = "999";
                            try
                            {
                                c = cPort.ReadLine();
                            }
                            catch (Exception fug) { }
                            System.Diagnostics.Debug.WriteLine("shit");
                            for (int j = 0; j < 40; j++)
                            {
                                if (c == "999")
                                {
                                    c = cPort.ReadLine();
                                }
                                if (c == "999")
                                {
                                    System.Diagnostics.Debug.WriteLine("tried to read 40 times, move on");
                                }
                            }
                            System.Diagnostics.Debug.WriteLine("out of j loop");
                            //while (c == "999") { c = cPort.ReadLine(); }
                            System.Diagnostics.Debug.WriteLine("Input from Tiva: " + c);
                            if (c.Contains('&'))
                            {
                                //   MessageBox.Show("got the thing");
                                portCon = true;   // what do do if port is correct
                                portText.Text = "Connected: " + ComPort;
                                System.Diagnostics.Debug.WriteLine("Com port " + ComPort + " responsive");
                            }
                            else
                            {
                                cPort.Close();
                                System.Diagnostics.Debug.WriteLine("Com port " + ComPort + " not responsive");
                            }
                        }
                    }
                }
            }
            catch (Exception g)
            {
                // what to do if no port responds appropriately
            }
            if(portCon == false)
            {
                portText.Text = "Port Not Connected";
            }
            //cPort.PortName = SetPortName(port.PortName);
            //cPort.BaudRate = SetPortBaudRate(port.BaudRate);
        }
        public void modeButton_Click(object sender, RoutedEventArgs e)
        {
            mode = mode+1;
           // System.Diagnostics.Debug.Write("mode a: " + mode);
            mode = mode% 3;
           // System.Diagnostics.Debug.WriteLine(", mode b: "+mode);
            //triggerMode = !triggerMode;
            if (mode == 2)
            {
            //    Label.Visible = true;
                trigText.Text = "Kinetic Run";
                scans.Visibility = Visibility.Visible;
                label3.Visibility = Visibility.Visible;
                totalTime.Visibility = Visibility.Visible;
                label5.Visibility = Visibility.Visible;

                halfT.Visibility = Visibility.Visible;
                tHalflabel.Visibility = Visibility.Visible;
                volume.Visibility = Visibility.Visible;
                volLabel.Visibility = Visibility.Visible;
                direction.Visibility = Visibility.Collapsed;
                dirText.Visibility = Visibility.Collapsed;
                valve.Visibility = Visibility.Collapsed;
                valveText.Visibility = Visibility.Collapsed;
                box1.Visibility = Visibility.Visible;
            }
            else if(mode == 1)
            {
                trigText.Text = "Software Trigger";
                scans.Visibility = Visibility.Collapsed;
                label3.Visibility = Visibility.Collapsed;
                totalTime.Visibility = Visibility.Collapsed;
                label5.Visibility = Visibility.Collapsed;
                halfT.Visibility = Visibility.Collapsed;
                tHalflabel.Visibility = Visibility.Collapsed;
                volume.Visibility = Visibility.Collapsed;
                volLabel.Visibility = Visibility.Collapsed;
                direction.Visibility = Visibility.Collapsed;
                dirText.Visibility = Visibility.Collapsed;
                valve.Visibility = Visibility.Collapsed;
                valveText.Visibility = Visibility.Collapsed;
                box1.Visibility = Visibility.Collapsed;
            }
            else {
                trigText.Text = "Pump";
                scans.Visibility = Visibility.Collapsed;
                label3.Visibility = Visibility.Collapsed;
                totalTime.Visibility = Visibility.Collapsed;
                label5.Visibility = Visibility.Collapsed;
                halfT.Visibility = Visibility.Collapsed;
                tHalflabel.Visibility = Visibility.Collapsed;
                volume.Visibility = Visibility.Visible;
                volLabel.Visibility = Visibility.Visible;
                direction.Visibility = Visibility.Visible;
                dirText.Visibility = Visibility.Visible;
                valve.Visibility = Visibility.Visible;
                valveText.Visibility = Visibility.Visible;
                box1.Visibility = Visibility.Collapsed;
            }
        }
        public void valveButton_Click(object sender, RoutedEventArgs e)
        {
            if (valveD == false) {
                valveText.Text = "To Reservoir";
            }
            else {
                valveText.Text = "To Cell";
            }
        }
        public void dirButton_Click(object sender, RoutedEventArgs e)
        {
            if (dirB == false) {
                dirText.Text = "Withdraw";
            }
            else {
                dirText.Text = "Inject";
            }
        }
        // run button
        public void runButton_Click(object sender, RoutedEventArgs e)
        {
            if (mode == 1 || mode == 2)  // start of spectrometry loop
            {
                res = spectrometer.getDeviceStatus(out status);
                //MessageBox.Show("integration time: "+integrationTime.Text);
                //this.Cursor = Cursors.WaitCursor;

                // set integration time
                double tau = double.Parse(integrationTime.Text);
                tau /= 1000;  // convert from ms to s
                res = spectrometer.setIntegrationTime(tau);
                // get wavelength data
                //double[] wavelengths;
                double min;
                double max;
                res = spectrometer.getWavelengthData(0, wavelengths, out min, out max);
            }  // end of spectrometery mode loop
            //  if mode is kinetic run 
            if (mode == 2)  // kinetic loop
            {
                scanNo = int.Parse(scans.Text);
                double halfLife = double.Parse(halfT.Text);
                for(int p = 0; p<scanNo-1; p++)
                {

                    int scn = scanNo - 1;
                //    System.Diagnostics.Debug.Write("counter: " + p + ", scans-1: " + scn);
                    double fract = (double)p/(double)scn;
                 //   System.Diagnostics.Debug.Write(", fraction: " + fract);
                    fract = 1 - fract;
                    double dxp = -Math.Log(fract, Math.E)*halfLife/0.693*1000;
                    int xp = (int)dxp;
                  //  System.Diagnostics.Debug.WriteLine("time: " + xp);
                    times[p] = xp;
                }
                int tTime = int.Parse(totalTime.Text);
                tTime = 1000 * tTime;

                
                times[scanNo-1] = tTime;
                for (int y = 0; y<scanNo; y++)
                {
                    System.Diagnostics.Debug.WriteLine("time: " + times[y]);
                }
                runPump();
                
                data = new int[scanNo, 3701];
                int r = 1;
                for (int j = 0; j < scanNo; j++)
                {
                    data[j, 0] = times[j]; // put time at top
                    int query = 0;
                    res = spectrometer.startScanExtTrg();
                    System.Diagnostics.Debug.WriteLine("StartScan: " + j);
                    int counter = 0;
                    while (query != 529)  //  supposed to be 0x0010 but ...
                    {
                        res = spectrometer.getDeviceStatus(out query);
                        counter = counter + 1;
                        if(query != r)
                        {
                            System.Diagnostics.Debug.WriteLine("code: " + query +", counter: "+counter); }
                        r = query;
                        counter = 0;
                        //    MessageBox.Show("status: " + query);
                    }
                    System.Diagnostics.Debug.WriteLine("outside timing loop");
                    res = spectrometer.getRawScanData(temp);  // data in temp file
                    int baseline = 0;
                    for(int w = 0; w < 32; w++)   // CCD has 32 early dummy scans and 14 late dummy scans
                    {
                        baseline = baseline + temp[w];  //get average of dummy scan data
                    }
                    baseline = baseline / 32;
                    System.Diagnostics.Debug.WriteLine("background: " + baseline);
                    data[j, 1] = baseline;
                    for(int p = 0; p<3648; p++)
                    {
                        data[j, p+2] = temp[p+32]-data[j,1];
                    }
                }
            }
            // start single scan loop
            else if (mode == 1) // spectrometer mode
            {
                res = spectrometer.startScan();   // initiate scan
                int query = 0;
                while (query != 0x0011)  //  supposed to be 0x0010 but ...
                {
                    res = spectrometer.getDeviceStatus(out query);
                    //    MessageBox.Show("status: " + query);
                }
                res = spectrometer.getRawScanData(temp);  // data in temp file
            }
            else
            {
                runPump();// injection mode
            }
            MessageBox.Show("Acquisition Complete");

            /*   for (int i = 0; i < scanNo; i++) {
                   int query = 0;
                   while (query != 0x0010)
                   {
                       res = spectrometer.getDeviceStatus(out query);
                   }
                   if ((status & 0x0010) > 0)
                   {
                       res = spectrometer.getScanData(temp);
                       for (int j = 0; j < 3700; j++){
                           data[i,j] = temp[j];
                       }
                   }
               } */
        }
        public void runPump() {
            try
            {
              //  String sMode = Convert.ToString(mode);


                //cPort.WriteLine("&");  // start communicating with pump
                //cPort.WriteLine(Convert.ToString(mode)); // pump mode
                //cPort.WriteLine(volume.Text); // injection volume
                String commandString = "&,"+ Convert.ToString(mode)+","+Convert.ToString(volume.Text); // injection volume
                //cPort.WriteLine(commandString);
                System.Diagnostics.Debug.WriteLine("command 1: " + commandString);
                if (mode == 0)
                {
                    if(dirB == false)
                    { cPort.WriteLine("0"); }
                    else { cPort.WriteLine("1"); }
                    if(valveD == false)
                    { cPort.WriteLine("0"); }
                    else { cPort.WriteLine("1"); }
                    cPort.WriteLine(commandString);
                    cPort.WriteLine("999");  // start accumulation
                }
                if (mode == 2)
                {  // kinetic run

                    // valve to cell
                    // inject
                    String sScans = Convert.ToString(scanNo);
                    cPort.WriteLine(sScans);  // number of scans
                    String commandString2 = sScans;
                    for (int q = 0; q < scanNo - 1; q++)
                    {
                        int delayT = times[q + 1] - times[q] - 10;
                        String sDelay = Convert.ToString(delayT);
                        commandString2 = commandString2 + ","+sDelay;
                        //cPort.WriteLine(Convert.ToString(delayT));  // delays
                        System.Diagnostics.Debug.WriteLine("delay: "+delayT);
                    }
                    commandString2 = commandString+","+ commandString2 + ",999";
                    System.Diagnostics.Debug.WriteLine("command string: " + commandString2);
                    cPort.WriteLine(commandString2);  // start accumulation
                    //cPort.WriteLine("999");
                }
                
 
            }
            catch (Exception f) { }
        }
        public void saveData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                String[] lines = new String[3651];
                //System.Diagnostics.Debug.WriteLine("writing data for spectrum: ");
                lines[0] = "wavelength";
                lines[1] = "background";
                for(int u = 0; u<scanNo; u++) { lines[0] = lines[0] + "," + Convert.ToString(times[u]); }
                System.Diagnostics.Debug.WriteLine(lines[0]);
                for (int n = 1; n < 3650; n++) {
                    if (n != 1)
                    {
                        lines[n] = Convert.ToString(wavelengths[n - 2]);
                    }
                                                                    //  Convert.ToString(temp[n]);
                                                                    /* if (n <= 10) // && n>996)
                                                                     {
                                                                         System.Diagnostics.Debug.WriteLine("Writting data for pixel " + n);
                                                                     } */

                    for (int p = 0; p < scanNo; p++)
                    {
                        lines[n] = lines[n] + "," + Convert.ToString(data[p, n]);
                        /* if (n <= 4) {
                             System.Diagnostics.Debug.Write(p+", ");
                       */
                    }
                    //    System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine(lines[n]);
                    //}
                }
            String f = filePath.Text + fileName.Text+".csv";
            String file = @f;// "c:\fileName2"; // filePath.Text + fileName;
 
                System.IO.File.WriteAllLines(file, lines);
                MessageBox.Show("Data Saved");
            }
            catch (IOException ef)
            { MessageBox.Show("IO Exception: "+ef.ToString()); }
        }
        // query usb devices
        static List<USBDeviceInfo> GetUSBDevices() {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();
            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity"))// USBHub"))
                collection = searcher.Get();
            foreach (var device in collection)
            {
                String Descr = (string)device.GetPropertyValue("Description");
                String DevIdent = (string)device.GetPropertyValue("DeviceID");
                if (Descr != null && Descr.Contains(value: "Thorlabs"))
                {
                    int j = DevIdent.IndexOf("808");  // CCS100 spectrometer: 8081
                    int k = DevIdent.IndexOf("M00");  // start of serial number
                    String SpecID = DevIdent.Substring(j, 4);
                    String SerNo = DevIdent.Substring(k, 9);
                    String Ident = "USB::0x1313::0x" + SpecID + "::" + SerNo + "::RAW";
                    devices.Add(new USBDeviceInfo(Ident, Descr));
                }
            }
            collection.Dispose();
            return devices;
        }
    }
    }

class USBDeviceInfo
{
    public USBDeviceInfo(string deviceID, string description) 
    {
        this.DeviceID = deviceID;
        this.Description = description;
    }
    public string DeviceID { get; private set; }
    public string Description { get; private set; }
}
