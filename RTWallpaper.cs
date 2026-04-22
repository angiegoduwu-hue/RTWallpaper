using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;

class RTWallpaper : Form {
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumCallback lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr child, string className, string title);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr hObject);
    [DllImport("ntdll.dll")] static extern uint NtSuspendProcess(IntPtr processHandle);
    [DllImport("ntdll.dll")] static extern uint NtResumeProcess(IntPtr processHandle);

    delegate bool EnumCallback(IntPtr hWnd, IntPtr lParam);

    private Button btnOpen, btnStop;
    private Label lblStatus;
    private System.Windows.Forms.Timer mainTimer;
    private bool isSuspended = false;
    private string selectedFile = "";
    private string configFile = "config.txt";

    private List<string> gameProcesses = new List<string>(new string[] { 
        "League of Legends", 
        "FortniteClient-Win64-Shipping", 
        "VALORANT-Win64-Shipping" 
    });

    public RTWallpaper() {
        this.Text = "RTWallpaper Pro";
        this.Size = new Size(500, 320);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        if (File.Exists("fondo.jpg")) {
            this.BackgroundImage = Image.FromFile("fondo.jpg");
            this.BackgroundImageLayout = ImageLayout.Stretch;
        }

        lblStatus = new Label();
        lblStatus.Text = "Estado: Iniciando...";
        lblStatus.Location = new Point(15, 15);
        lblStatus.Size = new Size(250, 20);
        lblStatus.BackColor = Color.FromArgb(180, 255, 255, 255);

        btnOpen = new Button();
        btnOpen.Text = "Seleccionar Video";
        btnOpen.Location = new Point(90, 230);
        btnOpen.Size = new Size(150, 35);

        btnStop = new Button();
        btnStop.Text = "Detener";
        btnStop.Location = new Point(260, 230);
        btnStop.Size = new Size(150, 35);

        mainTimer = new System.Windows.Forms.Timer();
        mainTimer.Interval = 2000;
        mainTimer.Tick += new EventHandler(Timer_Tick);
        mainTimer.Start();

        btnOpen.Click += new EventHandler(btnOpen_Click);
        btnStop.Click += new EventHandler(btnStop_Click);

        this.Controls.Add(lblStatus);
        this.Controls.Add(btnOpen);
        this.Controls.Add(btnStop);

        this.Load += new EventHandler(RTWallpaper_Load);
    }

    private void Timer_Tick(object sender, EventArgs e) {
        CheckIfGaming();
    }

    private void btnOpen_Click(object sender, EventArgs e) {
        SelectVideo();
    }

    private void btnStop_Click(object sender, EventArgs e) {
        StopWallpaper();
    }

    private void RTWallpaper_Load(object sender, EventArgs e) {
        if (File.Exists(configFile)) {
            selectedFile = File.ReadAllText(configFile);
            if (File.Exists(selectedFile)) StartWallpaper();
        }
    }

    private void CheckIfGaming() {
        bool isPlaying = false;
        foreach (string game in gameProcesses) {
            if (Process.GetProcessesByName(game).Length > 0) {
                isPlaying = true;
                break;
            }
        }

        if (isPlaying && !isSuspended) {
            ManageMPV(true);
            isSuspended = true;
            lblStatus.Text = "Estado: PAUSADO (En partida)";
        } else if (!isPlaying && isSuspended) {
            ManageMPV(false);
            isSuspended = false;
            lblStatus.Text = "Estado: Reproduciendo";
        }
    }

    private void ManageMPV(bool suspend) {
        foreach (var p in Process.GetProcessesByName("mpv")) {
            IntPtr hProc = OpenProcess(0x0800, false, p.Id);
            if (hProc != IntPtr.Zero) {
                if (suspend) NtSuspendProcess(hProc);
                else NtResumeProcess(hProc);
                CloseHandle(hProc);
            }
        }
    }

    private void SelectVideo() {
        using (OpenFileDialog ofd = new OpenFileDialog()) {
            ofd.Filter = "Videos|*.mp4;*.mkv";
            if (ofd.ShowDialog() == DialogResult.OK) {
                selectedFile = ofd.FileName;
                File.WriteAllText(configFile, selectedFile);
                StartWallpaper();
            }
        }
    }

    private void StartWallpaper() {
        StopWallpaper();
        IntPtr pman = FindWindow("Progman", null);
        SendMessage(pman, 0x052C, new IntPtr(0), IntPtr.Zero);
        IntPtr workw = IntPtr.Zero;
        EnumWindows(delegate(IntPtr hWnd, IntPtr lParam) {
            if (FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                workw = FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
            return true;
        }, IntPtr.Zero);

        if (workw != IntPtr.Zero) {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "mpv.exe";
            psi.Arguments = "--wid=" + (int)workw + " --loop=inf --hwdec=auto --no-audio \"" + selectedFile + "\"";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            try { Process.Start(psi); lblStatus.Text = "Estado: Reproduciendo"; } catch { }
        }
    }

    private void StopWallpaper() {
        foreach (var p in Process.GetProcessesByName("mpv")) { try { p.Kill(); } catch { } }
        lblStatus.Text = "Estado: Detenido";
    }

    [STAThread]
    static void Main() { 
        Application.EnableVisualStyles();
        Application.Run(new RTWallpaper()); 
    }
}