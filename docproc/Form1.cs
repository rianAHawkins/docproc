using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using System.Diagnostics;
using IronOcr;
using System.IO;

namespace docproc
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private string replaceNewLine(String str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                int x = str.IndexOf('\n', i);
                if (x != -1)
                {
                    str = str.Substring(0, x) + "\r\n" + str.Substring(x + 1);
                    i = x + 2;
                }
                else
                {
                    return str;
                }
            }
            return str;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //String str = "this\nis\ndumb";

            //txtBox.Text = replaceNewLine(str);
        }

        private static byte[] ImageToByte2(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private void proccess()
        {
            var testImagePath = txtFile.Text;

            String strSimple = "";
            try
            {
                var logger = new FormattedConsoleLogger();
                var resultPrinter = new ResultPrinter(logger);
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(testImagePath))
                    {
                        using (logger.Begin("Process image"))
                        {
                            var i = 1;
                            using (var page = engine.Process(img))
                            {
                                var text = page.GetText();
                                strSimple += text;
                                logger.Log("Text: {0}", text);
                                logger.Log("Mean confidence: {0}", page.GetMeanConfidence());

                                using (var iter = page.GetIterator())
                                {
                                    iter.Begin();
                                    do
                                    {
                                        if (i % 2 == 0)
                                        {
                                            using (logger.Begin("Line {0}", i))
                                            {
                                                do
                                                {
                                                    using (logger.Begin("Word Iteration"))
                                                    {
                                                        if (iter.IsAtBeginningOf(PageIteratorLevel.Block))
                                                        {
                                                            logger.Log("New block");
                                                        }
                                                        if (iter.IsAtBeginningOf(PageIteratorLevel.Para))
                                                        {
                                                            logger.Log("New paragraph");
                                                        }
                                                        if (iter.IsAtBeginningOf(PageIteratorLevel.TextLine))
                                                        {
                                                            logger.Log("New line");
                                                        }
                                                        logger.Log("word: " + iter.GetText(PageIteratorLevel.Word));
                                                    }
                                                } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));
                                            }
                                        }
                                        i++;
                                    } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.WriteLine("Unexpected Error: " + e.Message);
                Console.WriteLine("Details: ");
                Console.WriteLine(e.ToString());
                MessageBox.Show("error");
            }
            rtxtBox.Text = strSimple;
            //Console.Write("Press any key to continue . . . ");
            //Console.ReadKey(true);
        }

        private void proccessIOCR()
        {
            var ocr = new IronTesseract();
            using (var ocrInput = new OcrInput())
            {
                ocrInput.LoadImage(txtFile.Text);
                //ocrInput.LoadPdf("document.pdf");

                // Optionally Apply Filters if needed:
                // ocrInput.Deskew();  // use only if image not straight
                // ocrInput.DeNoise(); // use only if image contains digital noise

                var ocrResult = ocr.Read(ocrInput);
                rtxtBox.Text = ocrResult.Text;
                // Console.WriteLine(ocrResult.Text);
            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            txtFile.Text = openFileDialog1.FileName;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                txtFile.Text = files[0];
                var testImagePath = files[0];
                pictureBox1.Image = Image.FromFile(testImagePath);               
            }
        }



        private void btnFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void Form1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void btnTranslate_Click(object sender, EventArgs e)
        {
            //proccessIOCR();
            proccess();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics myCanvas = pictureBox1.CreateGraphics();

        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //Graphics myCanvas = pictureBox1.CreateGraphics();
            Image img = pictureBox1.Image;
            Bitmap bimg = new Bitmap(img);

            pictureBox1.Image = bimg;
            Color c = bimg.GetPixel(e.X, e.Y);
            Console.WriteLine("color: " + c.A + " r: " + c.R + " g: " + c.G + " b: " + c.B);

        }

        public Bitmap GrayScaleFilter(Bitmap image)
        {
            Bitmap grayScale = new Bitmap(image.Width, image.Height);

            for (Int32 y = 0; y < grayScale.Height; y++)
                for (Int32 x = 0; x < grayScale.Width; x++)
                {
                    Color c = image.GetPixel(x, y);

                    Int32 gs = (Int32)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);

                    grayScale.SetPixel(x, y, Color.FromArgb(gs, gs, gs));
                }
            return grayScale;
        }

        private int sSize = 2;
        private void pictureBox1_MouseMove_1(object sender, MouseEventArgs e)
        {
            //Graphics myCanvas = pictureBox1.CreateGraphics();
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                Image img = pictureBox1.Image;
                Bitmap bimg = new Bitmap(img);

                int change = sSize * -1;
                while (change < sSize)
                {
                    int y = e.Y + change;
                    for (int x = e.X - sSize; x <= (e.X + sSize); x++)
                    {
                        if (x >= 0 && x < bimg.Width)
                        {
                            if (y >= 0 && y < bimg.Width)
                            {
                                bimg.SetPixel(x, y, Color.Black);
                            }
                        }

                    }
                    change++;
                }


                pictureBox1.Image = bimg;
                Console.WriteLine("mouse2: " + e.Location);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Image img = pictureBox1.Image;
            Bitmap bimg = new Bitmap(img);
            bimg = GrayScaleFilter(bimg);
            pictureBox1.Image = bimg;
        }

        int limit = 145;
        private void button2_Click(object sender, EventArgs e)
        {
            Image img = pictureBox1.Image;
            Bitmap bimg = new Bitmap(img);

            

            for (Int32 y = 0; y < bimg.Height; y++)
            {
                for (Int32 x = 0; x < bimg.Width; x++)
                {
                    Color c = bimg.GetPixel(x, y);
                    int change = sSize * -1;
                    if (c.R < limit || c.G < limit || c.B < limit)
                    {
                        bimg.SetPixel(x, y, Color.Black);

                        //while (change < sSize)
                        //{
                        //    int iy = y + change;
                        //    for (int ix = x - sSize; ix <= (x + sSize); ix++)
                        //    {
                        //        if (ix >= 0 && ix < bimg.Width)
                        //        {
                        //            if (iy >= 0 && iy < bimg.Height)
                        //            {
                        //                bimg.SetPixel(ix, iy, Color.Black);
                        //            }
                        //        }
                        //    }
                        //    change++;
                        //}
                    }
                }
            }
            pictureBox1.Image = bimg;
        }
    }

    public class ResultPrinter
    {
        readonly FormattedConsoleLogger logger;

        public ResultPrinter(FormattedConsoleLogger logger)
        {
            this.logger = logger;
        }

        public void Print(ResultIterator iter)
        {
            logger.Log("Is beginning of block: {0}", iter.IsAtBeginningOf(PageIteratorLevel.Block));
            logger.Log("Is beginning of para: {0}", iter.IsAtBeginningOf(PageIteratorLevel.Para));
            logger.Log("Is beginning of text line: {0}", iter.IsAtBeginningOf(PageIteratorLevel.TextLine));
            logger.Log("Is beginning of word: {0}", iter.IsAtBeginningOf(PageIteratorLevel.Word));
            logger.Log("Is beginning of symbol: {0}", iter.IsAtBeginningOf(PageIteratorLevel.Symbol));

            logger.Log("Block text: \"{0}\"", iter.GetText(PageIteratorLevel.Block));
            logger.Log("Para text: \"{0}\"", iter.GetText(PageIteratorLevel.Para));
            logger.Log("TextLine text: \"{0}\"", iter.GetText(PageIteratorLevel.TextLine));
            logger.Log("Word text: \"{0}\"", iter.GetText(PageIteratorLevel.Word));
            logger.Log("Symbol text: \"{0}\"", iter.GetText(PageIteratorLevel.Symbol));
        }
    }

}
