using Accord.Video.FFMPEG;
using AForge;
using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

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

            pictureBox1.Visible = true;
            pictureBox1.BringToFront();

            motionDetector = new MotionDetector(
            new TwoFramesDifferenceDetector(), // Algorytm detekcji ruchu
            new MotionAreaHighlighting()       // Opcjonalne podświetlenie obszaru ruchu
            );
            SetTrackBarProperties();

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


        AForge.Vision.Motion.MotionDetector motionDetector = null;

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();

            bool isMotionDetected = false;

            if (previousFrame != null)
            {
                isMotionDetected = DetectMotion(bitmap, previousFrame);
            }

            if (isMotionDetected)
            {
                this.Invoke(new Action(() =>
                {
                    labelMotionDetected.Text = "Wykryto ruch";
                    labelMotionDetected.ForeColor = Color.Red;
                }));
            }
            else
            {
                this.Invoke(new Action(() =>
                {
                    labelMotionDetected.Text = "Brak ruchu";
                    labelMotionDetected.ForeColor = Color.Green;
                }));
            }

            // Zapis biezacej klatki jako poprzedniej
            if (previousFrame != null)
            {
                previousFrame.Dispose();
            }
            previousFrame = (Bitmap)bitmap.Clone();

            BrightnessCorrection br = new BrightnessCorrection(brightness);
            bitmap = br.Apply((Bitmap)bitmap.Clone());
            ContrastCorrection cr = new ContrastCorrection(contrast);
            bitmap = cr.Apply((Bitmap)bitmap.Clone());
            SaturationCorrection sr = new SaturationCorrection(saturation);
            bitmap = sr.Apply((Bitmap)bitmap.Clone());

            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }

            pictureBox1.Image = (Bitmap)bitmap.Clone();

            if (isRecording && videoWriter != null)
            {
                videoWriter.WriteVideoFrame(bitmap);
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
            if (pictureBox1.Image != null)
            {
                Bitmap image = new Bitmap(pictureBox1.Image);
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

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
            if (isRecording)
            {
                isRecording = false;
                videoWriter?.Close();
                videoWriter = null;
            }
            if (previousFrame != null)
            {
                previousFrame.Dispose();
                previousFrame = null;
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

            trackBar1.TickFrequency = 5;
            trackBar2.TickFrequency = 5;
            trackBar3.TickFrequency = 5;
        }

        private Bitmap previousFrame = null;          // poprzednia klatka potrzebna do wykrywania ruchu

        private bool DetectMotion(Bitmap currentFrame, Bitmap previousFrame)
        {
            if (previousFrame == null || currentFrame == null)
            {
                return false;
            }

            int motionPixels = 0; // licznik pikseli wskazujących ruch
            int grid = 30; // co ile pikseli analizować (większy = mniej szczegółowe, szybsze)
            int motionThreshold = 50; // minimalna różnica kolorów, by uznać piksel za "ruchomy"
            double motionPercentThreshold = 10.0; // minimalny procent pikseli wskazujących ruch

            // Iteracja po pikselach w siatce
            for (int y = 0; y < previousFrame.Height; y += grid)
            {
                for (int x = 0; x < previousFrame.Width; x += grid)
                {
                    // Pobierz kolory pikseli
                    Color pixelCurrent = currentFrame.GetPixel(x, y);
                    Color pixelPrevious = previousFrame.GetPixel(x, y);

                    // Oblicz różnicę dla każdego kanału RGB
                    int diffR = Math.Abs(pixelCurrent.R - pixelPrevious.R);
                    int diffG = Math.Abs(pixelCurrent.G - pixelPrevious.G);
                    int diffB = Math.Abs(pixelCurrent.B - pixelPrevious.B);

                    int totalDifference = diffR + diffG + diffB;

                    // Jeśli różnica przekracza próg, uznaj piksel za ruchomy
                    if (totalDifference > motionThreshold)
                    {
                        motionPixels++;
                    }
                }
            }

            // Oblicz procent pikseli wskazujących ruch
            double motionPercent = (double)motionPixels /
                ((previousFrame.Width / grid) * (previousFrame.Height / grid)) * 100;

            // Zwróć true jeśli ruch przekroczył próg procentowy
            return motionPercent > motionPercentThreshold;
        }


        private void trackBarRed_Scroll(object sender, EventArgs e)
        {
            brightness = trackBar1.Value;
        }

        private void trackBarGreen_Scroll(object sender, EventArgs e)
        {
            saturation = trackBar2.Value;
        }

        private void trackBarBlue_Scroll(object sender, EventArgs e)
        {
            contrast = trackBar3.Value;
        }

        public int brightness = 0;
        public int contrast = 0;
        public int saturation = 0;

        private void labelMotionDetected_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}