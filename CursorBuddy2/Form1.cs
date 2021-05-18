using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

namespace CursorBuddy2
{
    public partial class Form1 : Form
    {
        const int max = 20;
        static float easingMax = 0.5f;
        static float easingMin = 0.05f;
        static float easingStep = 0.01f;
        float Xoffset, Yoffset, directionX, directionY;
        Point prevpoint;
        struct pont
        {
            public PointF p;
            public SolidBrush brush;
            public Pen pen;
            public float easing;
            public float diameter;
        }
        double h = 0;
        pont[] pontok = new pont[max];
        Random r2 = new Random();
        Rectangle toRedraw = new Rectangle();
        Rectangle pastRedraw = new Rectangle();
        Rectangle[] rekts = new Rectangle[2];
        bool simple = true;
        public Form1()
        {
            InitializeComponent();

        }

        #region DLLImports
        public enum GWL
        {
            ExStyle = -20
        }

        public enum WS_EX
        {
            Transparent = 0x20,
            Layered = 0x80000
        }

        public enum LWA
        {
            ColorKey = 0x1,
            Alpha = 0x2
        }
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern int GetWindowLong(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong(IntPtr hWnd, GWL nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, int crKey, byte alpha, LWA dwFlags);
        float map(float s, float a1, float a2, float b1, float b2)
        {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }
        protected override void OnShown(EventArgs e)
        {

            TopMost = true;
            FormBorderStyle = FormBorderStyle.None;
            Rectangle temprekt = Screen.AllScreens.Select(screen => screen.Bounds).Aggregate(Rectangle.Union);
            Bounds = temprekt;
            for (int i = 0; i < max; i++)
            {
                pontok[i].p.X = 0;
                pontok[i].p.Y = 0;
                int r, g, b;
                HsvToRgb(i, 1, 1, out r, out g, out b);
                Color color = Color.FromArgb(r, g, b);
                pontok[i].brush = new SolidBrush(color);
                pontok[i].pen = new Pen(color);
                pontok[i].easing = easingMax;
                pontok[i].diameter = 20 - map(i, 0, max, 0, 20);
                string hex = r.ToString("X2") + b.ToString("X2") + b.ToString("X2").ToLower();
            }
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.LimeGreen;
            TransparencyKey = Color.LimeGreen;
            prevpoint = Cursor.Position;
            base.OnShown(e);
            int wl = GetWindowLong(Handle, GWL.ExStyle);
            wl = wl | 0x80000 | 0x20;
            SetWindowLong(Handle, GWL.ExStyle, wl);
        }
        #endregion

        void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            // ######################################################################
            // T. Nathan Mundhenk
            // mundhenk@usc.edu
            // C/C++ Macro HSV to RGB

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            for (int i = 1; i < max; i++)
            {
                float targetx = pontok[i - 1].p.X;
                float dx = targetx - pontok[i].p.X;
                pontok[i].p.X += dx * pontok[i].easing;
                float targety = pontok[i - 1].p.Y;
                float dy = targety - pontok[i].p.Y;
                pontok[i].p.Y += dy * pontok[i].easing;
            }
            for (int i = 0; i < max; i++)
            {
                int r, g, b;
                HsvToRgb(h + i, 1, 1, out r, out g, out b);
                pontok[i].brush.Color = Color.FromArgb(r, g, b);
                pontok[i].pen.Color = Color.FromArgb(r, g, b);
                pontok[i].pen.Width = 20 - map(i, 0, max, 0, 20);
                h += 0.1;
            }
            float targetx2 = Cursor.Position.X + Xoffset;
            float dx2 = targetx2 - pontok[0].p.X;
            float targety2 = Cursor.Position.Y + Yoffset;
            float dy2 = targety2 - pontok[0].p.Y;
            pontok[0].p.X += dx2 * pontok[0].easing;
            pontok[0].p.Y += dy2 * pontok[0].easing;
            float minX, minY, maxX, maxY;

            minX = pontok.Min(c => c.p.X);
            minY = pontok.Min(c => c.p.Y);
            maxX = pontok.Max(c => c.p.X);
            maxY = pontok.Max(c => c.p.Y);
            toRedraw = new Rectangle((int)minX - ((int)pontok[0].diameter / 2), (int)minY - ((int)pontok[0].diameter / 2), (int)maxX - (int)minX + ((int)pontok[0].diameter) + 1, (int)maxY - ((int)minY) + ((int)pontok[0].diameter) + 1);

            rekts[0] = toRedraw;
            rekts[1] = pastRedraw;
            //Invalidate(toRedraw);
            //Invalidate(pastRedraw);
            Invalidate(rekts.Aggregate(Rectangle.Union));

            if (!simple)
            {
                if (Math.Abs(Distance(prevpoint, Cursor.Position)) > 20)
                {
                    directionX = 0;
                    directionY = 0;
                    Xoffset = 0;
                    Yoffset = 0;
                    timer2.Stop();
                    if (pontok[0].easing < easingMax)
                    {
                        pontok[0].easing += easingStep;
                    }
                }
                else
                {
                    timer2.Start();
                    if (pontok[0].easing > easingMin)
                    {
                        pontok[0].easing -= easingStep;
                    }
                    Xoffset = directionX;
                    Yoffset = directionY;
                }
            }
            else
            {
                pontok[0].easing = easingMax;
            }

            prevpoint = Cursor.Position;
            pastRedraw = toRedraw;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.S)
            {
                simple = !simple;
            }
        }

        public float Distance(Point p1, Point p2)
        {
            return ((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }
        protected override void OnPaint(PaintEventArgs e)
        {

            //forward
            for (int i = 0; i < max; i++)
            {
                e.Graphics.FillEllipse(pontok[i].brush, pontok[i].p.X - (pontok[i].diameter / 2), pontok[i].p.Y - (pontok[i].diameter / 2), pontok[i].diameter, pontok[i].diameter);
            }
            for (int i = 1; i < max; i++)
            {
                e.Graphics.DrawLine(pontok[i].pen, pontok[i].p.X, pontok[i].p.Y, pontok[i - 1].p.X, pontok[i - 1].p.Y);
            }

            //debug
            //e.Graphics.DrawRectangle(new Pen(Color.Red, 3), toRedraw);
            //e.Graphics.DrawRectangle(new Pen(Color.Blue, 3), pastRedraw);
            //e.Graphics.DrawRectangle(new Pen(Color.Blue, 3), rekts.Aggregate(Rectangle.Union));
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            directionX = r2.Next(-600, 601);
            directionY = r2.Next(-600, 601);
        }
    }
}
