using NAudio.Wave;
using NAudio.Wave.Asio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PianoSoundPlayer
{
    public partial class Form1 : Form
    {
        private static readonly object Locker = new object();
        private static readonly double DecayTimeSeconds = 0.6;

        private static string[] StaticKeyString = Datas.Keylogger.Replace("\r\n", "\r").Split('\r');
        private static List<AudioDataSet> KeyData = new List<AudioDataSet>();
        private static WaveFormat WaveFormat = new WaveFormat();
        private static AsioOut WaveOut = null;
        private static BufferedWaveProvider BufferedWaveProvider = null;
        private static List<BufferDataSet> BuffersLR = new List<BufferDataSet>();

        public Form1()
        {
            InitializeComponent();
        }

        private void KeyDownHandler(int index, double velocityNormalized)
        {
            var tempData = KeyData[index].LR.Select(s => (int)(s * velocityNormalized * velocityNormalized)).ToList();
            lock (Locker)
            {
                for (int i = 0; i < BuffersLR.Count; i++)
                {
                    if (BuffersLR[i].KeyIndex == index)
                    {
                        BuffersLR.RemoveAt(i--);
                        break;
                    }
                }
                BuffersLR.Add(new BufferDataSet() { LR = tempData, Offset = 0, KeyIndex = index });
            }
        }

        private void KeyUpHandler(int index)
        {
            lock (Locker)
            {
                for (int i = 0; i < BuffersLR.Count; i++)
                {
                    if (BuffersLR[i].KeyIndex == index)
                    {
                        BuffersLR[i].KeyReleased = true;
                        break;
                    }
                }
            }
        }

        private void FeedIn()
        {
            new Thread(() =>
            {
                int delayTimeMs = 50;
                int bits = KeyData.FirstOrDefault()?.waveFormat.BitsPerSample ?? 16;
                int samples = KeyData.FirstOrDefault()?.waveFormat.SampleRate * delayTimeMs / 1000 ?? 0;
                int bytesOffset = samples * bits / 8;
                samples /= 8;
                samples = samples % 2 == 0 ? samples : samples + 1;

                while (true)
                {
                    if (BufferedWaveProvider.BufferedBytes < bytesOffset && BuffersLR.Count > 0)
                    {
                        int[] lrTemp = new int[samples];
                        lock (Locker)
                        {
                            for (int i = 0; i < BuffersLR.Count; i++)
                            {
                                if (BuffersLR[i].ReleaseEnd != -1)
                                {
                                    for (int j = 0; j < samples && j + BuffersLR[i].Offset < BuffersLR[i].LR.Count; j++)
                                    {
                                        double x = BuffersLR[i].ReleaseStart + j;
                                        BuffersLR[i].LR[j + BuffersLR[i].Offset] = (int)(BuffersLR[i].LR[j + BuffersLR[i].Offset] * Math.Exp(-9.21 * x / BuffersLR[i].ReleaseEnd));
                                    }
                                    BuffersLR[i].ReleaseStart += samples;
                                }
                                for (int j = 0; j < samples && j + BuffersLR[i].Offset < BuffersLR[i].LR.Count; j++)
                                {
                                    lrTemp[j] += BuffersLR[i].LR[j + BuffersLR[i].Offset];
                                }
                                BuffersLR[i].Offset += samples;
                            }
                        }

                        if (!KeyScan.Sustain)
                        {
                            for (int i = 0; i < BuffersLR.Count; i++)
                            {
                                if (BuffersLR[i].KeyReleased && BuffersLR[i].ReleaseEnd == -1)
                                {
                                    BuffersLR[i].ReleaseEnd = (int)(DecayTimeSeconds * WaveFormat.SampleRate * 2d);
                                }
                            }
                        }

                        lock (Locker)
                        {
                            for (int i = 0; i < BuffersLR.Count; i++)
                            {
                                if (BuffersLR[i].Offset >= BuffersLR[i].LR.Count)
                                {
                                    BuffersLR.RemoveAt(i--);
                                }
                            }
                        }

                        var bytes = WaveReader.ToBytes(lrTemp);
                        BufferedWaveProvider.AddSamples(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }).Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            KeyScan.Init(button1);
            AudioDataSet[] dataTemp = new AudioDataSet[StaticKeyString.Length];
            Parallel.For(0, StaticKeyString.Length, (i) =>
            {
                string fileName = StaticKeyString[i].Split('-')[1];
                if (fileName.Contains('/')) fileName = fileName.Split('/')[0];
                dataTemp[i] = WaveReader.ReadWaveFile(@"KeyData\" + fileName + ".wav");
            });
            KeyData = dataTemp.ToList();

            WaveFormat = KeyData.FirstOrDefault()?.waveFormat;
            BufferedWaveProvider = new BufferedWaveProvider(WaveFormat);
            WaveOut = new AsioOut();
            WaveOut.ShowControlPanel();
            WaveOut.Init(BufferedWaveProvider);
            WaveOut.Play();
            FeedIn();

            var pictureBox = pictureBox1;
            var keyString = StaticKeyString;
            double widthPercentage = (pictureBox.Width - 20) / (double)keyString.Count(s => !s.Contains("#"));
            double heightPercentage = pictureBox.Height - 10;

            double xOffset = 0;
            for (int i = 0; i < keyString.Length; i++)
            {
                PictureBox pb = new PictureBox();
                Brush brush = null;

                if (!keyString[i].Contains("#"))
                {
                    if (keyString[i].Contains("A4")) brush = new SolidBrush(Color.FromArgb(155, 155, 199));
                    else if (keyString[i].Contains("C")) brush = new SolidBrush(Color.FromArgb(199, 155, 155));
                    else brush = new SolidBrush(Color.FromArgb(199, 199, 199));
                    pb.Width = (int)(widthPercentage * 0.99);
                    pb.Height = (int)heightPercentage;
                    pb.Location = new Point((int)xOffset, 0);
                    xOffset += widthPercentage;
                }
                else
                {
                    brush = new SolidBrush(Color.FromArgb(33, 33, 33));
                    pb.Width = (int)(widthPercentage * 0.99);
                    pb.Height = (int)heightPercentage / 2;
                    pb.Location = new Point((int)(xOffset - widthPercentage / 2d), 0);
                }
                pb.Tag = i;

                pb.Image = new Bitmap(pb.Width, pb.Height);
                using (Graphics graphics = Graphics.FromImage(pb.Image))
                {
                    graphics.FillRectangle(brush, 0, 0, pb.Width, pb.Height);
                    graphics.DrawString(keyString[i].Replace("/", "-").Split('-')[0], new Font("Arial", 10), Brushes.DarkRed, new PointF(0, pb.Height - 16));
                    graphics.DrawString(double.Parse(keyString[i].Split('-')[1]).ToString("f1"), new Font("Arial", 10), Brushes.DarkRed, new PointF(0, pb.Height - 32));

                    pb.Refresh();
                }

                pb.MouseDown += (sender1, e1) =>
                {
                    double velocity = e1.Location.Y / (double)pb.Height;
                    using (Graphics graphics = Graphics.FromImage(pb.Image))
                    {
                        Brush fillBrush = new SolidBrush(Color.FromArgb(255, (int)(velocity * 255), 55, 55));
                        graphics.FillRectangle(fillBrush, 0, 0, pb.Width, pb.Height);
                        fillBrush.Dispose();
                        pb.Refresh();
                    }
                    KeyDownHandler((int)pb.Tag, velocity);
                };
                pb.MouseUp += (sender1, e1) =>
                {
                    pb.Image = new Bitmap(pb.Width, pb.Height);
                    using (Graphics graphics = Graphics.FromImage(pb.Image))
                    {
                        graphics.FillRectangle(brush, 0, 0, pb.Width, pb.Height);
                        graphics.DrawString(StaticKeyString[(int)pb.Tag].Replace("/", "-").Split('-')[0], new Font("Arial", 10), Brushes.DarkRed, new PointF(0, pb.Height - 16));
                        graphics.DrawString(double.Parse(StaticKeyString[(int)pb.Tag].Split('-')[1]).ToString("f1"), new Font("Arial", 10), Brushes.DarkRed, new PointF(0, pb.Height - 32));

                        pb.Refresh();
                    }
                    KeyUpHandler((int)pb.Tag);
                };

                pictureBox.Controls.Add(pb);
                if (keyString[i].Contains("#")) pictureBox.Controls[pictureBox.Controls.Count - 1].BringToFront();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            KeyScan.Stop();
            System.Environment.Exit(0);
        }
    }
}
