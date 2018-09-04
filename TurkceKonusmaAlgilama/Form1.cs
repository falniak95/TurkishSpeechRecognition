using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//---------------------------
#region Gerekli Kütüphaneler
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Grpc.Auth;
using Google.Apis.Services;
using System.Net;
using System.Media;
using System.Diagnostics;
using NAudio.Wave;
using System.IO;
#endregion
//---------------------------

namespace TurkceKonusmaAlgilama
{
    public partial class Form1 : Form
    {
        private BufferedWaveProvider bwp;
        WaveIn waveIn;
        WaveOut waveOut;
        WaveFileWriter writer;
        WaveFileReader reader;
        string output = "audio.raw";

        public Form1()
        {
            InitializeComponent();

            waveOut = new WaveOut();
            waveIn = new WaveIn();
            waveIn.DataAvailable += new EventHandler<WaveInEventArgs>(waveIn_DataAvailable);
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
            bwp = new BufferedWaveProvider(waveIn.WaveFormat);
            bwp.DiscardOnBufferOverflow = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
                    if (NAudio.Wave.WaveIn.DeviceCount < 1)
            {
                MessageBox.Show("Etkin bir mikrofon bulamadım.", "Mikrofon Bağlı Değil", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (NAudio.Wave.WaveIn.DeviceCount < 1)
            {
                MessageBox.Show("Etkin bir mikrofon bulamadım.", "Mikrofon Bağlı Değil", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;

            }
            button1.Enabled = false;
            button2.Enabled = true;
            waveIn.StartRecording();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            backgroundWorker1.RunWorkerAsync();

        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;

            waveIn.StopRecording();

            if (File.Exists("audio.raw"))
                File.Delete("audio.raw");


            writer = new WaveFileWriter(output, waveIn.WaveFormat);



            byte[] buffer = new byte[bwp.BufferLength];
            int offset = 0;
            int count = bwp.BufferLength;

            var read = bwp.Read(buffer, offset, count);
            if (count > 0)
            {
                writer.Write(buffer, offset, read);
            }

            waveIn.Dispose();
            waveIn = null;
            writer.Close();
            writer = null;

            reader = new WaveFileReader("audio.raw"); // (new MemoryStream(bytes));
            waveOut.Init(reader);
            waveOut.PlaybackStopped += new EventHandler<StoppedEventArgs>(waveOut_PlaybackStopped);
            //    waveOut.Play();

            reader.Close();

            if (File.Exists("audio.raw"))
            {

                var speech = SpeechClient.Create();

                var response = speech.Recognize(new RecognitionConfig()
                {
                    Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                    SampleRateHertz = 16000,
                    LanguageCode = "tr",

                }, RecognitionAudio.FromFile("audio.raw"));

                // AIzaSyCMVUZen9fupUmN2QOo1fIIvnCjoPPmEUY 

                textBox1.Text = "";

                foreach (var result in response.Results)
                {
                    foreach (var alternative in result.Alternatives)
                    {
                        textBox1.Text = textBox1.Text + " " + alternative.Transcript;
                    }
                }
                MessageBox.Show("Tamamlandı. Algılanan: " + textBox1.Text);
                if (textBox1.Text.Length == 0)
                    textBox1.Text = "Ses kaydı çok uzun ya da hiç ses algılanamadı.";
            }
            else
            {

                textBox1.Text = "Ses Dosyası Bulunamadı";

            }

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button1.Enabled = true;
            this.Cursor = Cursors.Default;
            waveIn = new WaveIn();

            waveIn.DataAvailable += new EventHandler<WaveInEventArgs>(waveIn_DataAvailable);
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
        }

        private void waveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            try
            {
                waveOut.Stop();
                reader.Close();
                reader = null;
            }
            catch
            {

            }
        }
        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);

        }

    }
}
