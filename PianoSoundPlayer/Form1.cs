using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PianoSoundPlayer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        static string[] StaticKeyString = Datas.Keylogger.Replace("\r\n", "\r").Split('\r');
        static List<byte[]>KeyBytes = new List<byte[]>();
        static WaveFormat waveformat = new WaveFormat();
        public static void KeyPlay(int index, double Vel127)
        {
            Task.Run(() =>
            {  
                WaveOutEvent waveOut = new WaveOutEvent();
                var audioStream = new MemoryStream(KeyBytes[index]); 
                waveOut.Init(new RawSourceWaveStream(audioStream, waveformat));
                waveOut.Play(); 
            });
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            for(int i=0;i<StaticKeyString.Length;i++)
            {
                AudioFileReader audioWriter = new AudioFileReader(@"KeyData\" + StaticKeyString[i].Split('-')[1] + ".wav");
                waveformat = audioWriter.WaveFormat;
                var bytes = new byte[audioWriter.Length];
                audioWriter.Read(bytes, 0, (int)audioWriter.Length);
                audioWriter.Close();
                KeyBytes.Add(bytes);
            }

            var Cl = this;
            int Start = 10;
            int End = Cl.Width;
            var KeyString = StaticKeyString.Where((s, i) => i >= Start && i < End).ToArray();
            double WidthP = (Cl.Width - 20) / (double)KeyString.Where(s => s.IndexOf("#") < 0).Count();
            double HeightP = Cl.Height-50;

            double xOffset = 0;
            for (int i = 0; i < KeyString.Length; i++)
            {
                PictureBox PB = new PictureBox();
                Brush brushs = null;

                if (KeyString[i].IndexOf("#") < 0)
                {
                    if (KeyString[i].IndexOf("A4") >= 0) brushs = new SolidBrush(Color.FromArgb(155, 155, 199));
                    else if (KeyString[i].IndexOf("C") >= 0) brushs = new SolidBrush(Color.FromArgb(199, 155, 155));
                    else brushs = new SolidBrush(Color.FromArgb(199, 199, 199));
                    PB.Width = (int)(WidthP * 0.95);
                    PB.Height = (int)HeightP;
                    PB.Location = new Point((int)xOffset, 0);
                    xOffset += WidthP;
                }
                else
                {
                    brushs = new SolidBrush(Color.FromArgb(33, 33, 33));
                    PB.Width = (int)(WidthP * 0.95);
                    PB.Height = (int)HeightP / 2;
                    PB.Location = new Point((int)(xOffset - WidthP / 2d), 0);
                }
                PB.Tag = i + Start;


                PB.Image = new Bitmap(PB.Width, PB.Height);
                using (Graphics graphics = Graphics.FromImage(PB.Image))
                {
                    graphics.FillRectangle(brushs, 0, 0, PB.Width, PB.Height);
                    graphics.DrawString(KeyString[i].Replace("/", "-").Split('-')[0], new Font("Arial", 10), Brushes.DarkRed, new PointF(0, PB.Height - 16));
                    graphics.DrawString(double.Parse(KeyString[i].Split('-')[1]).ToString("f1"), new Font("Arial", 10), Brushes.DarkRed, new PointF(0, PB.Height - 32));

                    PB.Refresh();

                }

                PB.MouseDown += (sender1, e1) =>
                {
                    double Vel = e1.Location.Y / (double)PB.Height * 127d;
                    using (Graphics graphics = Graphics.FromImage(PB.Image))
                    {
                        Brush brush = new SolidBrush(Color.FromArgb(192, (int)(Vel * 2), 55, 55));
                        graphics.FillRectangle(brush, 0, 0, PB.Width, PB.Height);
                        brush.Dispose();
                        PB.Refresh();
                    }
                    KeyPlay((int)PB.Tag, Vel);
                };
                PB.MouseUp += (sender1, e1) =>
                {
                    PB.Image = new Bitmap(PB.Width, PB.Height);
                    using (Graphics graphics = Graphics.FromImage(PB.Image))
                    {
                        graphics.FillRectangle(brushs, 0, 0, PB.Width, PB.Height);
                        graphics.DrawString(StaticKeyString[(int)PB.Tag].Replace("/", "-").Split('-')[0], new Font("Arial", 10), Brushes.DarkRed, new PointF(0, PB.Height - 16));
                        graphics.DrawString(double.Parse(StaticKeyString[(int)PB.Tag].Split('-')[1]).ToString("f1"), new Font("Arial", 10), Brushes.DarkRed, new PointF(0, PB.Height - 32));

                        PB.Refresh();
                    }
                };


                Cl.Controls.Add(PB);
                if (KeyString[i].IndexOf("#") >= 0) Cl.Controls[Cl.Controls.Count - 1].BringToFront();
            }
        }
    }
}
