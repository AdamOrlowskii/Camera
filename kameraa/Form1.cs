using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Accord.Video.FFMPEG;
using AForge;
using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Video.DirectShow;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
// DETEKCJA RUCHU MA BYĆ REGULOWANA BO 2 KLATKI ZAWSZE BĘDĄ RÓŻNE, NO I KOLORY DODAĆ

namespace kameraa
{
    public partial class Form1 : Form
    {
        public int red = 0;
        public int green = 0;
        public int blue = 0;
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private VideoFileWriter videoWriter;
        private bool isRecording = false;

        public Form1()
        {
            InitializeComponent();
            LoadAvailableCameras();
        }
        private void LoadAvailableCameras() // Funkcja ładująca listę kamer do okienka rozwijanej listy
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice); // Pobranie listy kamer

            foreach (FilterInfo device in videoDevices)
            {
                comboBoxCameras.Items.Add(device.Name); // Dodanie nazw kamer do ComboBox
            }

            if (comboBoxCameras.Items.Count > 0)
            {
                comboBoxCameras.SelectedIndex = 0; // Wybierz pierwszą kamerę na liście
            }
            else
            {
                MessageBox.Show("Nie znaleziono kamer USB.");
            }
        }


        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {

            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            pictureBoxDisplay.Image = ApplyRGBFilter(bitmap);

            if (pictureBoxDisplay.Image != null)
            {
                pictureBoxDisplay.Image.Dispose();
            }

            pictureBoxDisplay.Image = (Bitmap)bitmap.Clone();

            if (isRecording && videoWriter != null) // KONIECZNIE IF A NIE WHILE
            {
                videoWriter.WriteVideoFrame((Bitmap)bitmap.Clone());
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }

            int selectedCameraIndex = comboBoxCameras.SelectedIndex;
            if (selectedCameraIndex >= 0)
            {
                // Ustawienie źródła wideo na wybraną kamerę
                videoSource = new VideoCaptureDevice(videoDevices[selectedCameraIndex].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                videoSource.Start();
            }
        }

        private void buttonCapture_Click(object sender, EventArgs e)
        {
            if (pictureBoxDisplay.Image != null)
            {
                Bitmap image = new Bitmap(pictureBoxDisplay.Image);
                image.Save(@"C:\Users\aorlo\Desktop\photo.png", ImageFormat.Png);
                image.Dispose();
            }
        }

        private void pictureBoxDisplay_Click(object sender, EventArgs e)
        {

        }

        private void comboBoxCameras_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
            base.OnFormClosing(e);
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
                videoSource = null;
                pictureBoxDisplay.Image = null;
            }
        }

        private void buttonRecord_Click(object sender, EventArgs e)
        {
                if (!isRecording)
                {
                    string videoFilePath = @"C:\Users\aorlo\Desktop\video.avi";

                    videoWriter = new VideoFileWriter();
                    videoWriter.Open(videoFilePath, 450, 300);

                    isRecording = true;
                }
        }

        private void buttonStopRecording_Click(object sender, EventArgs e)
        {
            if (isRecording)
            {
                isRecording = false;
                videoWriter.Close();
                videoWriter = null;
            }
        }
        private void SetTrackBarProperties()
        {
            trackBar1.Maximum = 255;
            trackBar2.Maximum = 255;
            trackBar3.Maximum = 255;

            trackBar1.Minimum = 0;
            trackBar2.Minimum = 0;
            trackBar3.Minimum = 0;

            trackBar1.TickFrequency = 3;
            trackBar2.TickFrequency = 3;
            trackBar3.TickFrequency = 3;
        }

        private Bitmap ApplyRGBFilter(Bitmap sourceImage)
        {
            ColorFiltering filter = new ColorFiltering();
            filter.Red = new IntRange(0, red);      
            filter.Green = new IntRange(0, green);  
            filter.Blue = new IntRange(0, blue);    
            Bitmap processedImage = filter.Apply(sourceImage);
            return processedImage;
        }

        private void trackBarRed_Scroll(object sender, EventArgs e)
        {
            red = trackBar1.Value;
        }

        private void trackBarGreen_Scroll(object sender, EventArgs e)
        {
            green = trackBar1.Value;
        }

        private void trackBarBlue_Scroll(object sender, EventArgs e)
        {
            blue = trackBar1.Value;
        }
    }
}
