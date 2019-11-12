using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Windows.Input;
using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
using iTextSharp.text.pdf;
using RasterEdge.Imaging.Basic;
using RasterEdge.XDoc.PDF;

namespace webtest
{
    public partial class Form2 : Form
    {
        #region variables
        private FilterInfoCollection CaptureDevices;
        private VideoCaptureDevice VideoSource;
        public System.Drawing.Point current = new System.Drawing.Point();
        public System.Drawing.Point old = new System.Drawing.Point();
        public System.Drawing.Graphics g;
        public System.Drawing.Pen p = new System.Drawing.Pen(Color.Red, 5);
        public int width;
        private Stack<Image> _undoStack = new Stack<Image>();
        private Stack<Image> _redoStack = new Stack<Image>();
        private readonly object _undoRedoLocker = new object();

       
        #endregion
        public Form2()
        {
            InitializeComponent();
            g = pictureBox2.CreateGraphics();
            p.SetLineCap(System.Drawing.Drawing2D.LineCap.Round, System.Drawing.Drawing2D.LineCap.Round, System.Drawing.Drawing2D.DashCap.Round);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Clear();
            VideoSource = new VideoCaptureDevice(CaptureDevices[0].MonikerString);
            VideoSource.NewFrame += new NewFrameEventHandler(VideoSource_NewFrame);
            VideoSource.Start();
        }
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            pictureBox1.Image = (Bitmap)eventArgs.Frame.Clone();
        }
        private void Form2_Load(object sender, EventArgs e)
        {
            CaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            VideoSource = new VideoCaptureDevice();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            pictureBox2.Image = (Bitmap)pictureBox1.Image.Clone();
        }
        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            old = e.Location;
            if (radioButton1.Checked)
                width = 1;
            else if (radioButton2.Checked)
                width = 5;
            else if (radioButton3.Checked)
                width = 10;
            else if (radioButton4.Checked)
                width = 15;
            else  if (radioButton5.Checked)
                width = 30;
             
            p.Width = width;
        }
        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                current = e.Location;
                g.DrawLine(p, old, current);
                old = current;
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            if (cd.ShowDialog() == DialogResult.OK)
                p.Color = cd.Color;
        }
        //UNdo
        private void Undo()
        {
            lock (_undoRedoLocker)
            {
                if (_undoStack.Count > 0)
                {
                    _redoStack.Push(_undoStack.Pop());
                    //OnUndo();
                    pictureBox2.Invalidate();
                    pictureBox2.Image = null;
                    //pictureBox2.Image = _undoStack.Peek();
                    pictureBox2.Image = _redoStack.Peek();
                    //pictureBox2.Refresh();
                }
            }
        }
        private void Redo()
        {
            lock (_undoRedoLocker)
            {
                if (_redoStack.Count > 0)
                {
                    _undoStack.Push(_redoStack.Pop());

                    //OnRedo();
                    pictureBox2.Image = _undoStack.Peek();
                    pictureBox2.Refresh();
                }
            }
        }
        //And whenever image need to be modified, add it to the undo stack first and then modify it
        private void UpdateImageData(Action updateImage)
        {
            lock (_undoRedoLocker)
            {
                if (pictureBox2.Image != null)
                {
                    _undoStack.Push(pictureBox2.Image);
                    try
                    {
                        //manipulate the image here.
                        updateImage();
                    }
                    catch
                    {
                        _undoStack.Pop();//because of exception remove the last added frame from stack
                        throw;
                    }
                }
            }
        }
        private void pictureBox2_MouseClick(object sender, MouseEventArgs e)
        {
            UpdateImageData(delegate ()
            {
                //pictureBox2.Invalidate();
                int radius = 10; //Set the number of pixel you want to use here
                                 //Calculate the numbers based on radius
                int x0 = Math.Max(e.X - (radius / 2), 0),
                    y0 = Math.Max(e.Y - (radius / 2), 0),
                    x1 = Math.Min(e.X + (radius / 2), pictureBox2.Width),
                    y1 = Math.Min(e.Y + (radius / 2), pictureBox2.Height);
                Bitmap bm = pictureBox2.Image as Bitmap; //Get the bitmap (assuming it is stored that way)
                for (int ix = x0; ix < x1; ix++)
                {
                    for (int iy = y0; iy < y1; iy++)
                    {
                        bm.SetPixel(ix, iy, Color.Black); //Change the pixel color, maybe should be relative to bitmap
                    }
                }
               // pictureBox2.Refresh(); //Force refresh
            });
        }
        private void button4_Click(object sender, EventArgs e)
        {
            Undo();
        }
        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {

        }
        private void button5_Click_1(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = "Drawing";
            saveFileDialog1.DefaultExt = "jpg";
            saveFileDialog1.Filter = "JPG images (*.jpg)|*.jpg";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Save Image as Jpg 
                Bitmap bmp = new Bitmap(pictureBox2.Width, pictureBox2.Height);
                pictureBox2.DrawToBitmap(bmp, new Rectangle(0, 0,
                    pictureBox2.Width, pictureBox2.Height));
                var fileName = saveFileDialog1.FileName;
                bmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);

                //Converting Image to SVG
                int startIdx = fileName.LastIndexOf("\\");
                int endIdx = fileName.LastIndexOf(".");
                String docName = fileName.Substring(startIdx + 1, endIdx - startIdx - 1);
                String inputFilePath = fileName;
                String outputDirectory = @"C:\Output\";
                PDFDocument doc = new PDFDocument(inputFilePath);
                doc.ConvertToVectorImages(ContextType.SVG, outputDirectory, docName, RelativeType.SVG);

                Console.WriteLine("Captured Successfully");
                Clear();
            }
        }

        private void Clear()
        {
            VideoSource.Stop();
            pictureBox1.Image = null;
            pictureBox1.Invalidate();
            pictureBox2.Image = null;
            pictureBox2.Invalidate();
            radioButton1.Checked = false;
            radioButton2.Checked = false;
            radioButton3.Checked = false;
            radioButton4.Checked = false;
            radioButton5.Checked = false;

        }

        
    }
}
