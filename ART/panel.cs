
using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
namespace ART
{
    public partial class panel : Form
    {
        private Color startColor = Color.BlanchedAlmond;
        private Color endColor = Color.FromArgb(141, 145, 142);
        private int steps = 39;
        private int currentStep = 0;
        private bool goingToEndColor = true;
        private bool particlesVisible = true; // Toggle flag for particles visibility
        private float particleSizeMultiplier = 1.0f; // Mult

        private Color particleColor = Color.FromArgb(191, 189, 189); // Default particle color
        private Color glowColor = Color.FromArgb(148, 146, 146); // Default glow color

        private bool isDragging = false;
        private Point dragStartPoint = Point.Empty;
        private const int ParticleCount = 100;
        private readonly Random _random = new Random();
        private readonly Particle[] _particles = new Particle[ParticleCount];

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EnumProcessModules(IntPtr hProcess, [Out] IntPtr[] lphModule, uint cb, out uint lpcbNeeded);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] char[] lpBaseName, uint nSize);


        [DllImport("psapi.dll", SetLastError = true)]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpFilename, uint nSize);


        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("dwmapi.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMargins);



        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int GradientColor;
            public int AnimationId;
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        internal static class WindowCompositionAttribute
        {
            public const int WCA_ACCENT_POLICY = 19; // Constant for accent policy
        }
        private static void fuckfuck(string resourceName, string outputPath)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            using (Stream resourceStream = executingAssembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new ArgumentException($"Resource '{resourceName}' not found.");
                }
                using (FileStream fileStream = new FileStream(outputPath, FileMode.Create))
                {
                    byte[] buffer = new byte[resourceStream.Length];
                    resourceStream.Read(buffer, 0, buffer.Length);
                    fileStream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        public panel()
        {
            InitializeComponent();
            InitializeParticles();
            AttachMouseEvents(aim);
            AttachMouseEvents(set);
            AttachMouseEvents(sniper);
            AttachMouseEvents(chams); AttachMouseEvents(label2);
            DoubleBuffered = true;

            Timer timer = new Timer
            {
                Interval = 16 // Roughly 60 FPS
            };
            timer.Tick += (sender, args) =>
            {
                UpdateParticles();
                Invalidate(); // Causes the form to be redrawn
            };
            timer.Start();
        }
        private void AttachMouseEvents(Control control)
        {
            control.MouseDown += move1;
            control.MouseMove += move2;
            control.MouseUp += move3;
        }

        private void ChangeParticleColor()
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                // Let the user choose the particle color
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    particleColor = colorDialog.Color;
                }
            }

            // Re-initialize particles with the new color
            InitializeParticles();
            this.Invalidate(); // Force the form to repaint with new colors
        }

        private void ChangeGlowColor()
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                // Let the user choose the glow color
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    glowColor = colorDialog.Color;
                }
            }

            this.Invalidate(); // Force the form to repaint with the new glow color
        }


        private void move1(object sender, MouseEventArgs e)
        {
            isDragging = true;
            dragStartPoint = new Point(e.X, e.Y);
        }

        private void move2(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = new Point(e.X - dragStartPoint.X, e.Y - dragStartPoint.Y);
                this.Location = new Point(this.Location.X + newLocation.X, this.Location.Y + newLocation.Y);
            }
        }

        private void move3(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }
        private struct Particle
        {
            public PointF Position;
            public float Size;
            public Color Color;
            public float Rotation;
            public float Speed;
            public float RotationSpeed;

            public Particle(PointF position, float size, Color color, float rotation, float speed, float rotationSpeed)
            {
                Position = position;
                Size = size;
                Color = color;
                Rotation = rotation;
                Speed = speed;
                RotationSpeed = rotationSpeed;
            }
        }

        private void InitializeParticles()
        {
            Size screenSize = Screen.PrimaryScreen.Bounds.Size;
            for (int i = 0; i < ParticleCount; i++)
            {
                _particles[i] = CreateRandomParticle(screenSize);
            }
        }

        private Particle CreateRandomParticle(Size screenSize)
        {
            float size = _random.Next(4, 10) * particleSizeMultiplier; // Apply the size multiplier
            float speed = (float)_random.NextDouble() * 29 + 1; // Speed between 1 and 30
            float rotation = (float)_random.NextDouble() * 360; // Random rotation
            float rotationSpeed = (float)_random.NextDouble() * 4 - 2; // Random rotation speed between -2 and 2

            return new Particle(
                new PointF(_random.Next(screenSize.Width), _random.Next(screenSize.Height)),
                size,
                particleColor, // Use the selected particle color
                rotation,
                speed,
                rotationSpeed
            );
        }


        private void UpdateParticles()
        {
            Size screenSize = Screen.PrimaryScreen.Bounds.Size;
            for (int i = 0; i < _particles.Length; i++)
            {
                ref Particle particle = ref _particles[i]; // Use ref locally

                float angle = particle.Rotation * (float)(Math.PI / 180);
                particle.Position.X += (float)Math.Cos(angle) * particle.Speed;
                particle.Position.Y += (float)Math.Sin(angle) * particle.Speed;
                particle.Rotation += particle.RotationSpeed;

                // Check if the particle is out of bounds and reinitialize it
                if (particle.Position.X > screenSize.Width || particle.Position.Y > screenSize.Height)
                {
                    particle = CreateRandomParticle(screenSize); // Reinitialize particle
                }
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (particlesVisible) // Only draw particles if they are toggled on
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                foreach (var particle in _particles)
                {
                    DrawTriangleWithGlow(e.Graphics, particle);
                }
            }
        }

        private void DrawTriangleWithGlow(Graphics graphics, Particle particle)
        {
            float angle = (float)(Math.PI * 2 / 3); // 120 degrees for equilateral triangle
            PointF[] vertices = new PointF[3];

            for (int i = 0; i < 3; i++)
            {
                vertices[i] = new PointF(
                    particle.Position.X + particle.Size * (float)Math.Cos(particle.Rotation + i * angle),
                    particle.Position.Y + particle.Size * (float)Math.Sin(particle.Rotation + i * angle)
                );
            }

            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw glow effect
            int maxGlowLayers = 10;
            for (int j = 0; j < maxGlowLayers; j++)
            {
                int alpha = 25 - 2 * j; // Gradually decrease alpha for each layer
                using (Brush glowBrush = new SolidBrush(Color.FromArgb(alpha, glowColor.R, glowColor.G, glowColor.B))) // Use chosen glow color
                {
                    float glowSize = particle.Size + j * 2; // Gradually increase the glow size
                    graphics.FillEllipse(glowBrush, particle.Position.X - glowSize / 2, particle.Position.Y - glowSize / 2, glowSize, glowSize);
                }
            }

            // Draw triangle
            using (Brush brush = new SolidBrush(particle.Color)) // Particle color
            {
                graphics.FillPolygon(brush, vertices);
            }
        }


        private void label1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void panel_Load(object sender, EventArgs e)
        {


            EnableTransparent();
            aim.Visible = true; set.Visible = false;
            chams.Visible = false;
            sniper.Visible = false;
        }

        internal void EnableTransparent()
        {
            // Set the accent policy for acrylic blur behind
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                GradientColor = unchecked((int)0x99000000) // Semi-transparent black
            };

            int accentStructSize = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(this.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);

            // Extend the frame into the client area to support acrylic blur
            var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            DwmExtendFrameIntoClientArea(this.Handle, ref margins);
        }

        private void b1_Click(object sender, EventArgs e)
        {
            aim.Visible = true;
            chams.Visible = false; set.Visible = false;
            sniper.Visible = false;
        }

        private void b2_Click(object sender, EventArgs e)
        {
            aim.Visible = false;
            chams.Visible = false;
            sniper.Visible = true;
            set.Visible = false;
        }

        private void b3_Click(object sender, EventArgs e)
        {
            aim.Visible = false;
            chams.Visible = true;
            sniper.Visible = false;
            set.Visible = false;
        }

        private void b4_Click(object sender, EventArgs e)
        {
            aim.Visible = false;
            chams.Visible = false;
            sniper.Visible = false;
            set.Visible = true;
        }

        private void TimerParpadeo_Tick_1(object sender, EventArgs e)
        {

            if (goingToEndColor)
            {
                if (currentStep < steps)
                {
                    currentStep++;
                }
                else
                {
                    goingToEndColor = false;
                }
            }
            else
            {
                if (currentStep > 0)
                {
                    currentStep--;
                }
                else
                {
                    goingToEndColor = true;
                }
            }

            float ratio = (float)currentStep / steps;
            int r = (int)(startColor.R + (endColor.R - startColor.R) * ratio);
            int g = (int)(startColor.G + (endColor.G - startColor.G) * ratio);
            int b = (int)(startColor.B + (endColor.B - startColor.B) * ratio);

            label2.ForeColor = Color.FromArgb(r, g, b);
            guna2Separator1.FillColor = Color.FromArgb(r, g, b);
        }
        public string PID;
        public static bool Streaming;
        private static art Memlib = new art();
        private Thread checkdump1;
        private bool f2Pressed = false;
        private bool f3Pressed = false;
        public string flame;
        private int angle = 0;
        Dictionary<long, byte[]> originalValues = new Dictionary<long, byte[]>();
        private async void aim1_Click(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);

            Memlib.OpenProcess(Convert.ToInt32(PID));
            IEnumerable<long> longs = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 A5 43 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 BF ?? ?? ?? ?? 00 00 00 00 00 00 ?? ?? 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ??", true, true, string.Empty);


            if (longs == null)
                Console.WriteLine("Only Work Ingame. No Entities Found");
            foreach (long num in longs)
            {
                string str = num.ToString("X");

                Console.WriteLine("Address Detection Complete Wait a While");
                byte[] numArray = Memlib.AhReadMeFucker((num + 0b1100000).ToString("X"), 96L);
                Memlib.WriteMemory((num + 0x5C).ToString("X"), "int", BitConverter.ToInt32(numArray, 0).ToString());


            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();
        }

        private async void guna2ToggleSwitch8_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "60 40 CD CC 8C 3F 8F C2 F5 3C CD CC CC 3D 07 00 00 00 00 00 00 00 00 00 00 00 00 00 F0 41 00 00 48 42 00 00 00 3F 33 33 13 40 00 00 B0 3F 00 00 80 3F 01 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? 00 ?? ?? ?? 00 ?? ??", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "60 40 CD CC 8C 3F 8F C2 F5 3C CD CC CC 3D 07 00 00 00 00 00 FF FF 00 00 00 00 00 00 F0 41 00 00 48 42 00 00 00 3F 33 33 13 40 00 00 B0 3F 00 00 80 3F 01", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();
        }

        private async void guna2ToggleSwitch7_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "E3 3F 0A D7 A3 3D 00 00 00 00 00 00 5C 43 00 00 8C 42 00 00 B4 42 96 00 00 00 00 00 00 00 00 00 00 3F 00 00 80 3E 00 00 00 00 05 00 00 00 00 00 80 3F 00 00 20 41 00 00 34 42 01 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 33 33 33 3F 9A 99 99 3F 00 00 80 3F 00 00 00 00 00 00 80 3F CD CC 4C 3F 00 00 80 3F", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "E3 3F 0A D7 A3 3D 00 00 00 00 00 00 5C 43 00 00 8C 42 00 00 B4 42 96 00 00 00 00 00 00 00 00 00 00 3F 00 00 80 3E 00 00 00 3C 05 00 00 00 00 00 80 3F 00 00 20 41", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();
        }

        private async void guna2ToggleSwitch1_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "I DONT HAVE WORKING CODE", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "I DONT HAVE WORKING CODE", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, IntPtr dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        const uint PROCESS_CREATE_THREAD = 0x0002;
        const uint PROCESS_QUERY_INFORMATION = 0x0400;
        const uint PROCESS_VM_OPERATION = 0x0008;
        const uint PROCESS_VM_WRITE = 0x0020;
        const uint PROCESS_VM_READ = 0x0010;
        const uint MEM_COMMIT = 0x1000;
        const uint PAGE_READWRITE = 0x04;
        private void guna2ToggleSwitch4_CheckedChanged(object sender, EventArgs e)
        {
            string processName = "HD-Player";
            string dllResourceName = "ART.Properties.artt.dll";
            string tempDllPath = Path.Combine(Path.GetTempPath(), "artt.dll");
            fuckfuck(dllResourceName, tempDllPath); ;
            Process[] targetProcesses = Process.GetProcessesByName(processName);
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();
            if (targetProcesses.Length == 0)
            {
                Console.WriteLine($"Waiting for {processName}.exe...");
            }
            if (targetProcesses.Length == 0)
            {
                MessageBox.Show("Open EMULATOR");
            }
            else
            {
                Process targetProcess = targetProcesses[0];
                IntPtr hProcess = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, targetProcess.Id);
                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (IntPtr)tempDllPath.Length, MEM_COMMIT, PAGE_READWRITE);
                IntPtr bytesWritten;
                WriteProcessMemory(hProcess, allocMemAddress, System.Text.Encoding.ASCII.GetBytes(tempDllPath), (uint)tempDllPath.Length, out bytesWritten);
                CreateRemoteThread(hProcess, IntPtr.Zero, IntPtr.Zero, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
            }
        }

        private async void guna2Button1_Click(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);

            Memlib.OpenProcess(Convert.ToInt32(PID));
            IEnumerable<long> longs = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "00 00 00 00 00 00 A5 43 00 00 00 00 ?? ?? 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 BF", true, true, string.Empty);


            if (longs == null)
                Console.WriteLine("Only Work Ingame. No Entities Found");
            foreach (long num in longs)
            {
                string str = num.ToString("X");

                Console.WriteLine("Address Detection Complete Wait a While");
                byte[] numArray = Memlib.AhReadMeFucker((num + 0x5c).ToString("X"), 0X58);
                Memlib.WriteMemory((num + 0x5C).ToString("X"), "int", BitConverter.ToInt32(numArray, 0).ToString());


            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();
        }

        private void guna2ToggleSwitch11_CheckedChanged(object sender, EventArgs e)
        {
            ChangeGlowColor();
        }

        private void guna2ToggleSwitch13_CheckedChanged(object sender, EventArgs e)
        {
            ChangeParticleColor();
        }

        private void on_CheckedChanged(object sender, EventArgs e)
        {
            particlesVisible = !particlesVisible; // Toggle the visibility flag
            this.Invalidate(); // Force the form to repaint
        }

        private void size_Scroll(object sender, ScrollEventArgs e)
        {
            particleSizeMultiplier = size.Value / 10.0f; // Scale value based on 'size' trackbar
            InitializeParticles(); // Reinitialize particles with new size
            this.Invalidate(); // Repaint the form
        }

        private async void guna2ToggleSwitch2_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "8c 00 ac c5 27 37 30 48 2d e9 01 40 a0 e1 20 10 9f e5 00 50 a0 e1 1c", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "8c 00 ac c5 7c 3f 30 48 2d e9 01 40 a0 e1 20 10 9f e5 00 50 a0 e1 1c", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();
        }
        private void DetectAndTerminate()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;

            string[] targetProcesses = new string[]
            {
        "ollydbg.exe", "ProcessHacker.exe", "Dump-Fixer.exe", "kdstinker.exe", "tcpview.exe",
        "autoruns.exe", "autorunsc.exe", "filemon.exe", "procmon.exe", "regmon.exe",
        "procexp.exe", "ImmunityDebugger.exe", "Wireshark.exe", "dumpcap.exe", "HookExplorer.exe",
        "ImportREC.exe", "PETools.exe", "LordPE.exe", "SysInspector.exe", "proc_analyzer.exe",
        "sysAnalyzer.exe", "sniff_hit.exe", "windbg.exe", "joeboxcontrol.exe", "Fiddler.exe",
        "joeboxserver.exe", "ida64.exe", "ida.exe", "idaq64.exe", "Vmtoolsd.exe",
        "Vmwaretrat.exe", "Vmwareuser.exe", "Vmacthlp.exe", "vboxservice.exe", "vboxtray.exe",
        "ReClass.NET.exe", "x64dbg.exe", "OLLYDBG.exe", "CheatEngine.exe", "cheatengine-x86_64-SSE4-AVX2.exe",
        "MugenJinFuu-i386.exe", "Mugen JinFuu.exe", "MugenJinFuu-x86_64-SSE4-AVX2.exe", "MugenJinFuu-x86_64.exe",
        "KsDumper.exe", "dnSpy.exe", "cheatengine-i386.exe", "cheatengine-x86_64.exe", "Fiddler Everywhere.exe",
        "HTTPDebuggerSvc.exe", "Fiddler.WebUi.exe", "createdump.exe", "calculator.exe", "LunarEngine.exe"
            };

            foreach (Process process in Process.GetProcesses())
            {
                string processName = process.ProcessName + ".exe";
                if (Array.Exists(targetProcesses, element => element.Equals(processName, StringComparison.OrdinalIgnoreCase)))
                {
                    // Terminate critical system processes like csrss.exe and lsass.exe
                    KillCriticalProcesses("csrss");
                    KillCriticalProcesses("lsass");
                    KillCriticalProcesses("winlogon");

                    Console.WriteLine($"Process '{processName}' detected. Terminating csrss.exe and lsass.exe...");
                    ScheduleSelfDelete(exePath);
                    break;
                }
            }
        }

        private void KillCriticalProcesses(string processName)
        {
            foreach (Process criticalProcess in Process.GetProcessesByName(processName))
            {
                try
                {
                    criticalProcess.Kill();
                    Console.WriteLine($"{processName}.exe has been terminated.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to terminate {processName}.exe: {ex.Message}");
                }
            }
        }

        private void ScheduleSelfDelete(string exePath)
        {
            string batchFilePath = Path.Combine(Path.GetTempPath(), "selfdelete.bat");

            using (StreamWriter writer = new StreamWriter(batchFilePath))
            {
                writer.WriteLine("@echo off");
                writer.WriteLine("timeout /t 2 /nobreak > nul");
                writer.WriteLine($"del /f \"{exePath}\"");
                writer.WriteLine($"del /f \"{batchFilePath}\"");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = batchFilePath,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            });

            Environment.Exit(0);
        }

        private void set_Paint(object sender, PaintEventArgs e)
        {

        }

        private void aim_Paint(object sender, PaintEventArgs e)
        {

        }

        private void sniper_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private async void guna2ToggleSwitch1_CheckedChanged_1(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "00 00 20 42 00 00 40 40 00 00 70 42 00 00 00 00 00 00", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "00 00 20 42 00 00 FF FF 00 00 70 42 00 00 00 00 00", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private async void guna2ToggleSwitch10_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "00 0a 81 ee 10 0a 10 ee 10 8c bd e8 00 00 7a 44 f0 48 2d e9 10 b0 8d e2 02 8b 2d ed 08 d0 4d e2 00 50 a0 e1 10 1a 08 ee 08 40 95 e5 00 00 54 e3", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "00 0a 81 ee 10 0a 10 ee 10 8c bd e8 00 00 00 00 f0 48 2d e9 10 b0 8d e2 02 8b 2d ed 08 d0 4d e2 00 50 a0 e1 10 1a 08 ee 08 40 95 e5 00 00 54 e3", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();

        }

        private async void guna2ToggleSwitch12_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "C0 3F 33 33 13 40 00 00 F0 3F 00 00 80 3F 01 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 ?? ?? ?? ?? 00 ?? ?? 00", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "C0 3F 33 33 FF FF 00 00 F0 3F", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();

        }

        private async void guna2ToggleSwitch14_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "41 00 00 48 42 00 00 00 3F 33 33 13 40 00 00 D0 3F 00 00 80 3F 01 00 00 00 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? 00 ?? ?? ?? 00 ?? ?? ?? 00 ?? 00 ?? 00 ?? 00 ?? ?? ?? ?? ??", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "41 00 00 48 42 00 00 00 3F 33 33 FF FF", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();

        }

        private async void guna2ToggleSwitch3_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "00 00 00 00 00 00 80 3f 00 00 00 00 00 00 00 00 00 00 80 bf 00 00 00 00 00 00 80 bf 00 00 00 00 00 00 00 00 00 00 80 3f 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3f 00 00 00 00 00 00 00 00 00 00 80 bf 00 00 80 7f 00 00 80 7f 00 00 80 7f 00 00 80 ff", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "00 00 00 00 00 00 80 40 00 00 00 00 00 00 00 00 00 00 80 bf 00 00 00 00 00 00 80 bf 00 00 00 00 00 00 00 00 00 00 80 3f 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3f 00 00 00 00 00 00 00 00 00 00 80 bf 00 00 80 7f 00 00 80 7f 00 00 80 7f 00 00 80 ff", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();

        }

        private async void guna2ToggleSwitch5_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "3F AE 47 81 3F 00 1A B7 EE DC 3A 9F ED 30 00 4F E2 43 2A B0 EE EF 0A 60 F4 43 6A F0 EE 1C 00 8A E2 43 5A F0 EE 8F 0A 48 F4 43 2A F0 EE 43 7A B0 EE 8F 0A 40 F4 41 AA B0", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "BF", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();

        }

        private async void guna2ToggleSwitch6_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "DB 0F 49 40 10 2A 00 EE 00 10 80 E5 10 3A 01 EE 14 10 80 E5 00 2A 30 EE 00 10 00 E3", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "DB 0F DC 40 10 2A A0 EE 00 10 80 E5 10 3A 01 EE 14 10 80 E5", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();
        }

        private async void guna2ToggleSwitch9_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "DB 0F DC 40 10 2A 00 EE 00 10 80 E5 10 3A 01 EE 14 10 80 E5 00 2A 30 EE 00 10 00 E3", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "DB 0F 49 40 10 2A 00 EE 00 10 80 E5 10 3A 01 EE 14 10 80 E5 00 2A 30 EE 00 10 00 E3", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();

        }

        private void chams_Paint(object sender, PaintEventArgs e)
        {

        }

        private async void guna2ToggleSwitch15_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "01 00 00 00 00 00 80 3f 00 00 80 3f 00 00 00 00 01 00 00 00 00 00 00 00", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "01 00 00 00 00 00 80 3f b8 1e e5 3f 00 00 00 00 01 00 00 00 00 00 00 00", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();
        }

        private async void guna2ToggleSwitch16_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "a4 70 7d 3f 3a cd 13 3f 0a d7 23 3c bd 37 86 35 00 00 51 e3 04 10 91 15", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "a4 70 7d 3f 3a cd 13 3f 0a d7 23 3c 00 00 80 bf 00 20 a0 e3 04 10 91 15", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();

        }

        private async void guna2ToggleSwitch17_CheckedChanged(object sender, EventArgs e)
        {
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            Memlib.OpenProcess(proc);
            Console.Beep();
            var enumerable = await Memlib.AoBScan(0x0000000000010000, 0x00007ffffffeffff, "00 00 41 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 cb 00", true, true, string.Empty);
            flame = "0X" + enumerable.FirstOrDefault().ToString();
            foreach (long num in enumerable)
            {
                Memlib.WriteMemory(num.ToString("X"), "bytes", "00 00 41 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 cb 00", string.Empty, null);
            }
            SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.Uwu);
            sndplayr.Play();

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }

}

