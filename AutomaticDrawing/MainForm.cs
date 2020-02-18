using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices; 
using System.Windows.Forms;

namespace AutomaticDrawing
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern int RegisterHotKey(int hwnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern int UnregisterHotKey(int hwnd, int id);

        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [Flags]
        internal enum MOUSEEVENTF : uint
        {
            ABSOLUTE = 0x8000,
            HWHEEL = 0x01000,
            MOVE = 0x0001,
            MOVE_NOCOALESCE = 0x2000,
            LEFTDOWN = 0x0002,
            LEFTUP = 0x0004,
            RIGHTDOWN = 0x0008,
            RIGHTUP = 0x0010,
            MIDDLEDOWN = 0x0020,
            MIDDLEUP = 0x0040,
            VIRTUALDESK = 0x4000,
            WHEEL = 0x0800,
            XDOWN = 0x0080,
            XUP = 0x0100
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            internal int dx;
            internal int dy;
            internal int mouseData;
            internal MOUSEEVENTF dwFlags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)]
            internal MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            internal uint type;
            internal InputUnion IU;
            internal static int Size => Marshal.SizeOf(typeof(INPUT));
        }

        string filename;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            RegisterHotKey((int)this.Handle, 100, 0, (int)Keys.F5);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey((int)this.Handle, 0);
        }

        private string getFileName()
        {
            using (var ofd = new OpenFileDialog())
            {
                if(ofd.ShowDialog() == DialogResult.OK)
                {
                    return ofd.FileName;
                }
            }
            return null;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == (int)0x312)
            {
                Draw();
            }
        }

        public static Bitmap ConvertToBlackAndWhite(Bitmap original)
        {
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            Graphics graphics = Graphics.FromImage(newBitmap);

            ColorMatrix colorMatrix = new ColorMatrix(
                new[] {
                    new float[] {0.7f, 0.7f, 0.7f, 0, 0},
                    new float[] {0.7f, 0.7f, 0.7f, 0, 0},
                    new float[] {1.8f, 1.8f, 1.8f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {-1, -1, -1, 0, 1}
            });

            ImageAttributes attributes = new ImageAttributes();

            attributes.SetColorMatrix(colorMatrix);
            attributes.SetThreshold(1);

            graphics.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            graphics.Dispose();

            return newBitmap;
        }

        private void ImportImageButton_Click(object sender, EventArgs e)
        {
            filename = getFileName();

            if(filename == null)
            {
                return;
            }

            Bitmap image = ConvertToBlackAndWhite((Bitmap)Image.FromFile(filename));
            Size = new Size(image.Width, image.Height);

            using (Graphics graphics = CreateGraphics())
            {
                graphics.Clear(Color.CornflowerBlue);
                graphics.DrawImage(image, 0, 0, Width, Height);
            }
            MessageBox.Show("Start button is F5", "AutomaticDrawing", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button2, MessageBoxOptions.ServiceNotification);
        }

        void Draw()
        {
            Bitmap image = ConvertToBlackAndWhite((Bitmap)Image.FromFile(filename));

            Point position = Cursor.Position;

            ClickMouse(MouseButtons.Left, position.X, position.Y, true);
            ClickMouse(MouseButtons.Left, position.X, position.Y, false);

            for(int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color color = image.GetPixel(x, y);

                    if(color.B == 0)
                    {
                        int drawX = position.X + x;
                        int drawY = position.Y + y;
                        SetCursorPos(drawX, drawY);
                        ClickMouse(MouseButtons.Left, drawX, drawY, true);
                        ClickMouse(MouseButtons.Left, drawX, drawY, false);
                        Thread.Sleep(3);
                    }
                }
            }
            MessageBox.Show("I drew it all^^7", "AutomaticDrawing", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button2, MessageBoxOptions.DefaultDesktopOnly);
        }

        public static void ClickMouse(MouseButtons mouseButtons, int x, int y, bool nextLine)
        {
            var inputs = new INPUT[1];
            var input = new INPUT { type = 0 };

            input.IU.mi.dx = x;
            input.IU.mi.dy = y;

            switch(mouseButtons)
            {
                case MouseButtons.Left:
                    input.IU.mi.dwFlags = nextLine ? MOUSEEVENTF.LEFTDOWN : MOUSEEVENTF.LEFTUP;
                    break;
                case MouseButtons.Right:
                    input.IU.mi.dwFlags = nextLine ? MOUSEEVENTF.RIGHTDOWN : MOUSEEVENTF.RIGHTUP;
                    break;
                case MouseButtons.Middle:
                    input.IU.mi.dwFlags = nextLine ? MOUSEEVENTF.MIDDLEDOWN : MOUSEEVENTF.MIDDLEUP;
                    break;
                case MouseButtons.XButton1:
                case MouseButtons.XButton2:
                    input.IU.mi.mouseData = (mouseButtons == MouseButtons.XButton1) ? 0x0001 : 0x0002;
                    input.IU.mi.dwFlags = nextLine ? MOUSEEVENTF.XDOWN : MOUSEEVENTF.XUP;
                    break;
            }

            inputs[0] = input;

            SendInput(1, inputs, INPUT.Size);
        }
    }
}
