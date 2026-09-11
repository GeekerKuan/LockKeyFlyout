using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace LockKeyFlyout;

internal static unsafe class Program
{
    const int WM_DESTROY=2, WM_PAINT=15, WM_CLOSE=16, WM_COMMAND=0x111, WM_TIMER=0x113, WM_HOTKEY=0x312, WM_APP=0x8000;
    const int WM_KEYUP=0x101, WM_SYSKEYUP=0x105, WH_KEYBOARD_LL=13, SW_HIDE=0, SW_SHOWNOACTIVATE=4;
    const int WS_POPUP=unchecked((int)0x80000000), WS_OVERLAPPED=0, WS_CAPTION=0x00C00000, WS_SYSMENU=0x80000;
    const int WS_EX_TOPMOST=8, WS_EX_TOOLWINDOW=0x80, WS_EX_NOACTIVATE=0x08000000;
    const int TPM_RIGHTBUTTON=2, TPM_BOTTOMALIGN=0x20, MF_STRING=0, MF_SEPARATOR=0x800;
    const int NIM_ADD=0, NIM_MODIFY=1, NIM_DELETE=2, NIF_MESSAGE=1, NIF_ICON=2, NIF_TIP=4;
    const int VK_CAPITAL=0x14, VK_INSERT=0x2D, VK_NUMLOCK=0x90, VK_SCROLL=0x91;
    const int ID_SETTINGS=1001, ID_EXIT=1002, ID_ENABLE=1003, TRAY_MSG=WM_APP+1, TIMER_HIDE=1;
    const uint AW_HIDE=0x10000, AW_SLIDE=0x40000, AW_VER_POSITIVE=0x4, AW_VER_NEGATIVE=0x8;
    const int DWMWA_WINDOW_CORNER_PREFERENCE=33, DWMWCP_ROUND=2;
    static nint instance, host, flyout, settings, hook, trayMenu,appIcon;
    static Native.WndProc? wndProc; static Native.HookProc? hookProc;
    static Config cfg=Config.Load(); static string message=""; static bool state,flyoutVisible; static float barProgress;
    static readonly string MutexName="Local\\LockKeyFlyout.Singleton";

    [STAThread] static int Main()
    {
        using var mutex=new Mutex(true,MutexName,out bool first); if(!first)return 0;
        instance=Native.GetModuleHandle(null); wndProc=WindowProc; hookProc=KeyboardProc;GdiPlus.Start();appIcon=Native.LoadIcon(instance,(nint)32512);
        if(!Register("LockKeyFlyout.Host",0)||!Register("LockKeyFlyout.Popup",Native.CS_DROPSHADOW)){Native.MessageBox(0,$"无法注册窗口类。错误代码：{Marshal.GetLastWin32Error()}","LockKeyFlyout 启动失败",0x10);return 1;}
        host=Native.CreateWindowEx(0,"LockKeyFlyout.Host","LockKeyFlyout",0,0,0,0,0,0,0,instance,0);
        flyout=Native.CreateWindowEx(WS_EX_TOPMOST|WS_EX_TOOLWINDOW|WS_EX_NOACTIVATE,"LockKeyFlyout.Popup","",WS_POPUP,0,0,160,50,0,0,instance,0);
        if(host==0||flyout==0){Native.MessageBox(0,$"无法创建后台窗口。错误代码：{Marshal.GetLastWin32Error()}","LockKeyFlyout 启动失败",0x10);return 2;}
        int corner=DWMWCP_ROUND; Native.DwmSetWindowAttribute(flyout,DWMWA_WINDOW_CORNER_PREFERENCE,&corner,sizeof(int));
        trayMenu=Native.CreatePopupMenu(); Native.AppendMenu(trayMenu,MF_STRING,ID_ENABLE,cfg.Enabled?"暂停浮窗":"启用浮窗"); Native.AppendMenu(trayMenu,MF_STRING,ID_SETTINGS,"设置..."); Native.AppendMenu(trayMenu,MF_SEPARATOR,0,null); Native.AppendMenu(trayMenu,MF_STRING,ID_EXIT,"退出");
        AddTray(); hook=Native.SetWindowsHookEx(WH_KEYBOARD_LL,hookProc,instance,0);
        if(hook==0){Native.MessageBox(0,$"无法安装键盘监听。错误代码：{Marshal.GetLastWin32Error()}","LockKeyFlyout 启动失败",0x10);RemoveTray();return 3;}
        if(!File.Exists(Config.PathName))ShowSettings();
        Native.MSG msg; while(Native.GetMessage(&msg,0,0,0)>0){Native.TranslateMessage(&msg);Native.DispatchMessage(&msg);}
        GdiPlus.Stop();return 0;
    }
    static bool Register(string name,uint style){Native.WNDCLASSEX wc=new(){cbSize=(uint)Marshal.SizeOf<Native.WNDCLASSEX>(),style=style,lpfnWndProc=Marshal.GetFunctionPointerForDelegate(wndProc!),hInstance=instance,hIcon=appIcon,hIconSm=appIcon,hCursor=Native.LoadCursor(0,(nint)32512),lpszClassName=name,hbrBackground=name.EndsWith(".Popup",StringComparison.Ordinal)?0:(nint)6};return Native.RegisterClassEx(ref wc)!=0;}
    static void EnableAcrylic()
    {
        // ACCENT_ENABLE_ACRYLICBLURBEHIND asks Desktop Window Manager to blur
        // the actual pixels behind the popup. GradientColor is AABBGGRR.
        Native.ACCENT_POLICY accent=new(){AccentState=4,AccentFlags=2,GradientColor=0xCC222222};
        Native.WINDOWCOMPOSITIONATTRIBDATA data=new(){Attrib=19,pvData=(nint)(&accent),cbData=sizeof(Native.ACCENT_POLICY)};
        Native.SetWindowCompositionAttribute(flyout,&data);
    }
    static void DisableAcrylic()
    {
        Native.ACCENT_POLICY accent=new(){AccentState=0};
        Native.WINDOWCOMPOSITIONATTRIBDATA data=new(){Attrib=19,pvData=(nint)(&accent),cbData=sizeof(Native.ACCENT_POLICY)};
        Native.SetWindowCompositionAttribute(flyout,&data);
    }
    static nint KeyboardProc(int code,nint wp,nint lp)
    {
        if(code>=0&&cfg.Enabled&&(wp==WM_KEYUP||wp==WM_SYSKEYUP)){int vk=Marshal.ReadInt32(lp); if((vk==VK_CAPITAL&&cfg.Caps)||(vk==VK_NUMLOCK&&cfg.Num)||(vk==VK_SCROLL&&cfg.Scroll)||(vk==VK_INSERT&&cfg.Insert)) Native.PostMessage(host,WM_HOTKEY,(nint)vk,0);}
        return Native.CallNextHookEx(hook,code,wp,lp);
    }
    static nint WindowProc(nint hwnd,uint msg,nint wp,nint lp)
    {
        switch((int)msg){
        case WM_HOTKEY: ShowKey((int)wp); return 0;
        case TRAY_MSG: if((int)lp==0x205){Native.POINT p;Native.GetCursorPos(&p);Native.SetForegroundWindow(host);Native.TrackPopupMenu(trayMenu,TPM_RIGHTBUTTON|TPM_BOTTOMALIGN,p.x,p.y,0,host,0);} else if((int)lp==0x203)ShowSettings(); return 0;
        case WM_COMMAND: switch((int)(wp&0xffff)){case ID_SETTINGS:ShowSettings();break;case ID_ENABLE:cfg.Enabled=!cfg.Enabled;cfg.Save();RebuildMenu();break;case ID_EXIT:Native.DestroyWindow(host);break;case 2011:ReadSettings();Native.DestroyWindow(settings);settings=0;break;} return 0;
        case WM_TIMER: if((int)wp==TIMER_HIDE){Native.KillTimer(flyout,TIMER_HIDE);if(flyoutVisible){if(cfg.Animate)Native.AnimateWindow(flyout,145,AW_HIDE|AW_SLIDE|AW_VER_POSITIVE);else Native.ShowWindow(flyout,SW_HIDE);DisableAcrylic();flyoutVisible=false;}}return 0;
        case WM_PAINT:if(hwnd==flyout){PaintFlyout();return 0;}break;
        case WM_CLOSE:if(hwnd==settings){ReadSettings();Native.DestroyWindow(settings);settings=0;return 0;}break;
        case WM_DESTROY:if(hwnd==host){if(hook!=0)Native.UnhookWindowsHookEx(hook);RemoveTray();Native.PostQuitMessage(0);}return 0;
        }
        return Native.DefWindowProc(hwnd,msg,wp,lp);
    }
    static void ShowKey(int vk)
    {
        state=vk==VK_INSERT||((Native.GetKeyState(vk)&1)!=0); message=vk switch{VK_CAPITAL=>"大写锁定 ",VK_NUMLOCK=>"数字锁定 ",VK_SCROLL=>"滚动锁定 ",_=>"Insert 键已按下"};if(vk!=VK_INSERT)message+=state?"开启":"关闭";
        Native.POINT pt;Native.GetCursorPos(&pt);nint mon=cfg.MonitorMode==2?Native.MonitorFromPoint(pt,2):cfg.MonitorMode==1?Native.MonitorFromWindow(Native.GetForegroundWindow(),2):Native.MonitorFromWindow(host,1);
        Native.MONITORINFO mi=new(){cbSize=(uint)sizeof(Native.MONITORINFO)};Native.GetMonitorInfo(mon,&mi);uint dpiX=96,dpiY=96;try{Native.GetDpiForMonitor(mon,0,out dpiX,out dpiY);}catch{}int dpi=(int)dpiX;int w=Mul(160,dpi),h=Mul(50,dpi);int x=mi.rcWork.left+(mi.rcWork.right-mi.rcWork.left-w)/2,y=mi.rcWork.bottom-h-Mul(16,dpi);
        barProgress=state?1f:0f;EnableAcrylic();Native.SetWindowRgn(flyout,Native.CreateRoundRectRgn(0,0,w+1,h+1,Mul(11,dpi),Mul(11,dpi)),true);Native.KillTimer(flyout,TIMER_HIDE);
        Native.SetWindowPos(flyout,(nint)(-1),x,y,w,h,0x10|0x40);Native.InvalidateRect(flyout,0,false);Native.UpdateWindow(flyout);
        if(!flyoutVisible){flyoutVisible=true;if(cfg.Animate)Native.AnimateWindow(flyout,220,AW_SLIDE|AW_VER_NEGATIVE);else Native.ShowWindow(flyout,SW_SHOWNOACTIVATE);}
        Native.SetTimer(flyout,TIMER_HIDE,(uint)cfg.Duration,0);
    }
    static int Mul(int n,int dpi)=>n*dpi/96;
    static void PaintFlyout()
    {
        Native.PAINTSTRUCT ps;nint target=Native.BeginPaint(flyout,&ps);Native.RECT r;Native.GetClientRect(flyout,&r);int dpi=(int)Native.GetDpiForWindow(flyout);nint dc=Native.CreateCompatibleDC(target),bitmap=Native.CreateCompatibleBitmap(target,r.right,r.bottom),oldBitmap=Native.SelectObject(dc,bitmap);Native.PatBlt(dc,0,0,r.right,r.bottom,0x00000042);
        nint brush=Native.GetStockObject(5),border=Native.CreatePen(0,Mul(1,dpi),0x00505050),oldPen=Native.SelectObject(dc,border),oldBrush=Native.SelectObject(dc,brush);Native.RoundRect(dc,0,0,r.right-1,r.bottom-1,Mul(11,dpi),Mul(11,dpi));Native.SelectObject(dc,oldPen);Native.SelectObject(dc,oldBrush);Native.DeleteObject(border);
        GdiPlus.Draw(dc,dpi,state,barProgress,r.right,r.bottom,message,cfg.Bold);Native.BitBlt(target,0,0,r.right,r.bottom,dc,0,0,0x00CC0020);Native.SelectObject(dc,oldBitmap);Native.DeleteObject(bitmap);Native.DeleteDC(dc);Native.EndPaint(flyout,&ps);
    }
    static void AddTray(){Native.NOTIFYICONDATA d=TrayData();d.uFlags=NIF_MESSAGE|NIF_ICON|NIF_TIP;d.uCallbackMessage=TRAY_MSG;d.hIcon=appIcon;d.szTip="锁定键浮窗";Native.Shell_NotifyIcon(NIM_ADD,ref d);}
    static void RemoveTray(){Native.NOTIFYICONDATA d=TrayData();Native.Shell_NotifyIcon(NIM_DELETE,ref d);} static Native.NOTIFYICONDATA TrayData()=>new(){cbSize=(uint)Marshal.SizeOf<Native.NOTIFYICONDATA>(),hWnd=host,uID=1,szTip="",szInfo="",szInfoTitle=""};
    static void RebuildMenu(){Native.DestroyMenu(trayMenu);trayMenu=Native.CreatePopupMenu();Native.AppendMenu(trayMenu,MF_STRING,ID_ENABLE,cfg.Enabled?"暂停浮窗":"启用浮窗");Native.AppendMenu(trayMenu,MF_STRING,ID_SETTINGS,"设置...");Native.AppendMenu(trayMenu,MF_SEPARATOR,0,null);Native.AppendMenu(trayMenu,MF_STRING,ID_EXIT,"退出");}
    static void ShowSettings()
    {
        if(settings!=0){Native.SetForegroundWindow(settings);return;} settings=Native.CreateWindowEx(0,"LockKeyFlyout.Host","锁定键浮窗设置",WS_OVERLAPPED|WS_CAPTION|WS_SYSMENU,200,150,430,390,0,0,instance,0);
        AddCheck("启用锁定键浮窗",2001,20,20,cfg.Enabled);AddCheck("Caps Lock",2002,40,60,cfg.Caps);AddCheck("Num Lock",2003,40,92,cfg.Num);AddCheck("Scroll Lock",2004,40,124,cfg.Scroll);AddCheck("Insert",2005,40,156,cfg.Insert);AddCheck("粗体文字",2006,220,60,cfg.Bold);AddCheck("淡入淡出",2007,220,92,cfg.Animate);AddCheck("登录时启动",2008,220,124,cfg.Startup);
        AddLabel("停留时间（毫秒）",20,205,150,25);AddEdit(cfg.Duration.ToString(),2009,180,202,100,27);AddLabel("显示器：0 固定 / 1 前台窗口 / 2 鼠标",20,245,300,25);AddEdit(cfg.MonitorMode.ToString(),2010,330,242,55,27);Native.CreateWindowEx(0,"BUTTON","保存并关闭",0x50010000,270,300,115,32,settings,(nint)2011,instance,0);Native.ShowWindow(settings,1);Native.UpdateWindow(settings);
    }
    static void AddCheck(string s,int id,int x,int y,bool check){nint h=Native.CreateWindowEx(0,"BUTTON",s,0x50010003,x,y,170,25,settings,(nint)id,instance,0);Native.SendMessage(h,0xF1,check?1:0,0);}static void AddLabel(string s,int x,int y,int w,int h)=>Native.CreateWindowEx(0,"STATIC",s,0x50000000,x,y,w,h,settings,0,instance,0);static void AddEdit(string s,int id,int x,int y,int w,int h)=>Native.CreateWindowEx(0,"EDIT",s,0x50810080,x,y,w,h,settings,(nint)id,instance,0);
    static bool Checked(int id)=>Native.SendMessage(Native.GetDlgItem(settings,id),0xF0,0,0)==1; static string TextOf(int id){var b=new StringBuilder(32);Native.GetWindowText(Native.GetDlgItem(settings,id),b,b.Capacity);return b.ToString();}
    static void ReadSettings(){cfg.Enabled=Checked(2001);cfg.Caps=Checked(2002);cfg.Num=Checked(2003);cfg.Scroll=Checked(2004);cfg.Insert=Checked(2005);cfg.Bold=Checked(2006);cfg.Animate=Checked(2007);cfg.Startup=Checked(2008);if(int.TryParse(TextOf(2009),out int d))cfg.Duration=Math.Clamp(d,300,10000);if(int.TryParse(TextOf(2010),out int m))cfg.MonitorMode=Math.Clamp(m,0,2);cfg.ApplyStartup();cfg.Save();RebuildMenu();}
}

internal sealed class Config
{
    public bool Enabled=true,Caps=true,Num=true,Scroll=true,Insert=true,Bold=false,Animate=true,Startup=false;public int Duration=2000,MonitorMode=2;
    public static string PathName=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LockKeyFlyout","settings.ini");
    public static Config Load(){var c=new Config();if(!File.Exists(PathName))return c;foreach(var line in File.ReadAllLines(PathName)){var p=line.Split('=',2);if(p.Length<2)continue;bool B()=>p[1]=="1";switch(p[0]){case"Enabled":c.Enabled=B();break;case"Caps":c.Caps=B();break;case"Num":c.Num=B();break;case"Scroll":c.Scroll=B();break;case"Insert":c.Insert=B();break;case"Bold":c.Bold=B();break;case"Animate":c.Animate=B();break;case"Startup":c.Startup=B();break;case"Duration":int.TryParse(p[1],out c.Duration);break;case"Monitor":int.TryParse(p[1],out c.MonitorMode);break;}}return c;}
    public void Save(){Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);File.WriteAllLines(PathName,new[]{S("Enabled",Enabled),S("Caps",Caps),S("Num",Num),S("Scroll",Scroll),S("Insert",Insert),S("Bold",Bold),S("Animate",Animate),S("Startup",Startup),$"Duration={Duration}",$"Monitor={MonitorMode}"});}static string S(string k,bool v)=>$"{k}={(v?1:0)}";
    public void ApplyStartup()
    {
        // A Run-key process starts at medium integrity and therefore cannot observe
        // keystrokes sent to elevated windows.  An elevated on-logon task preserves
        // the integrity level without showing UAC on every sign-in.
        using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))key.DeleteValue("LockKeyFlyout",false);
        string exe=Environment.ProcessPath??throw new InvalidOperationException("无法确定程序路径");
        string script=Path.Combine(AppContext.BaseDirectory,"Register-StartupTask.ps1");
        if(!File.Exists(script))throw new FileNotFoundException("缺少自启动注册脚本",script);
        string quotedScript=$"\"{script}\"",quotedExe=$"\"{exe}\"";
        string args=Startup
            ? $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File {quotedScript} -Install -Executable {quotedExe}"
            : $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File {quotedScript} -Remove";
        using var p=Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,@"WindowsPowerShell\v1.0\powershell.exe"),args){UseShellExecute=false,CreateNoWindow=true});
        if(p is null||!p.WaitForExit(10000)||p.ExitCode!=0)throw new InvalidOperationException("注册开机自启动失败");
    }
}
