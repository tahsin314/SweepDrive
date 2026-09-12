using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace SweepDriveApp {

  public class Cat {
    public string Key, Name, Group, Type;
    public bool Admin, Safe;
    public string[] Paths;
    public string What, Regen, Loss;
    public CheckBox Box;
    public Label SizeLbl;
  }

  static class Native {
    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int SHEmptyRecycleBin(IntPtr hwnd, string root, uint flags);
  }

  public class MainForm : Form {
    static Color Accent   = Color.FromArgb(15,108,189);
    static Color AccentDk = Color.FromArgb(11,84,148);
    static Color SideBg   = Color.FromArgb(28,38,52);
    static Color SideSel  = Color.FromArgb(15,108,189);
    static Color SideTxt  = Color.FromArgb(205,214,224);
    static Color Bg       = Color.FromArgb(245,247,250);
    static Color CardBg   = Color.White;
    static Color Line     = Color.FromArgb(228,232,238);
    static Color TextDim  = Color.FromArgb(120,130,142);
    static Color Ink      = Color.FromArgb(32,42,54);
    static Color Good     = Color.FromArgb(28,145,72);
    static Color Warn     = Color.FromArgb(196,72,40);

    static Font FBody   = new Font("Segoe UI", 10f);
    static Font FSemi   = new Font("Segoe UI Semibold", 10f);
    static Font FH1     = new Font("Segoe UI Semibold", 15f, FontStyle.Bold);
    static Font FH2     = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
    static Font FSmall  = new Font("Segoe UI", 8.5f);
    static Font FMono   = new Font("Consolas", 9f);

    List<Cat> cats = new List<Cat>();
    Dictionary<string,Panel> pages = new Dictionary<string,Panel>();
    List<Button> navButtons = new List<Button>();
    Panel content, detailStrip;
    Label dName, dWhat, dRegen, dLoss, total, status;
    TextBox log;
    CheckedListBox dlList; List<string> dlPaths = new List<string>(); Label dlInfo;
    bool isAdmin;

    string L = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    string A = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    string U = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    string Win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    string[] sections = { "Overview","Windows Junk","Browser Cache","Developer","Graphics","Advanced","Downloads" };
    string[] secGlyph  = { "🏠","🧹","🌐","🧩","🎮","🛡","📁" };

    public MainForm() {
      isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
      Text = "SweepDrive — Disk Cleaner";
      Size = new Size(1000, 680);
      MinimumSize = new Size(920, 620);
      StartPosition = FormStartPosition.CenterScreen;
      BackColor = Bg; Font = FBody; Ink.ToString();
      ForeColor = Ink;
      try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch {}

      Define();

      // header
      var header = new Panel{ Dock=DockStyle.Top, Height=58, BackColor=Accent };
      header.Controls.Add(new Label{ Text="🧹", Font=new Font("Segoe UI Emoji",20), ForeColor=Color.White,
        Location=new Point(16,8), Size=new Size(40,40), TextAlign=ContentAlignment.MiddleCenter, BackColor=Color.Transparent });
      header.Controls.Add(new Label{ Text="SweepDrive", Font=new Font("Segoe UI Semibold",16,FontStyle.Bold),
        ForeColor=Color.White, Location=new Point(58,7), Size=new Size(220,28), BackColor=Color.Transparent });
      header.Controls.Add(new Label{ Text="Disk Cleaner", Font=FSmall, ForeColor=Color.FromArgb(210,226,244),
        Location=new Point(60,36), Size=new Size(200,16), BackColor=Color.Transparent });
      var badge = new Label{ Text = isAdmin?"Administrator":"Standard mode", Font=FSmall, ForeColor=Color.White,
        Location=new Point(840,20), Size=new Size(140,20), TextAlign=ContentAlignment.MiddleRight,
        Anchor=AnchorStyles.Top|AnchorStyles.Right, BackColor=Color.Transparent };
      header.Controls.Add(badge);

      // action bar
      var bar = new Panel{ Dock=DockStyle.Bottom, Height=60, BackColor=Color.White };
      bar.Paint += (s,e)=>{ using(var p=new Pen(Line)) e.Graphics.DrawLine(p,0,0,bar.Width,0); };
      var btnSafe = Btn("✓  Safe Clean (Recommended)", 16, 250, true); btnSafe.Parent=bar;
      var btnScan = Btn("🔍  Scan sizes", 276, 130, false); btnScan.Parent=bar;
      var btnClean= Btn("🧹  Clean checked", 414, 150, false); btnClean.Parent=bar;
      total = new Label{ Text="Free on C: —", Font=FSemi, ForeColor=Ink, Location=new Point(590,20), Size=new Size(390,22),
        TextAlign=ContentAlignment.MiddleRight, Anchor=AnchorStyles.Right|AnchorStyles.Top };
      bar.Controls.Add(total);
      foreach(Control c in new Control[]{btnSafe,btnScan,btnClean}) bar.Controls.Add(c);
      btnSafe.Click += (s,e)=>SafeClean();
      btnScan.Click += (s,e)=>DoScan();
      btnClean.Click+= (s,e)=>CleanList(Checked(), "Clean the {0} checked item(s)? This cannot be undone (except Recycle Bin).");

      // detail strip
      detailStrip = new Panel{ Dock=DockStyle.Bottom, Height=92, BackColor=Color.FromArgb(250,251,253) };
      detailStrip.Paint += (s,e)=>{ using(var p=new Pen(Line)) e.Graphics.DrawLine(p,0,0,detailStrip.Width,0); };
      dName = new Label{ Text="Hover an item to see what it is", Font=FSemi, ForeColor=Accent, Location=new Point(20,10), Size=new Size(940,20), Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top };
      dWhat = new Label{ Font=FBody, ForeColor=Ink, Location=new Point(20,32), Size=new Size(950,20), Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top };
      dRegen= new Label{ Font=FSmall, Location=new Point(20,54), Size=new Size(470,18), Anchor=AnchorStyles.Left|AnchorStyles.Top };
      dLoss = new Label{ Font=FSmall, ForeColor=TextDim, Location=new Point(20,72), Size=new Size(950,18), Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top };
      detailStrip.Controls.Add(dName); detailStrip.Controls.Add(dWhat); detailStrip.Controls.Add(dRegen); detailStrip.Controls.Add(dLoss);

      // sidebar
      var side = new Panel{ Dock=DockStyle.Left, Width=180, BackColor=SideBg };
      int sy=14;
      for(int i=0;i<sections.Length;i++){
        var b = new Button{ Text="  "+secGlyph[i]+"   "+sections[i], Location=new Point(0,sy), Size=new Size(180,42),
          FlatStyle=FlatStyle.Flat, TextAlign=ContentAlignment.MiddleLeft, ForeColor=SideTxt, BackColor=SideBg,
          Font=new Font("Segoe UI Emoji",10.5f), Tag=sections[i] };
        b.FlatAppearance.BorderSize=0; b.FlatAppearance.MouseOverBackColor=Color.FromArgb(40,52,68);
        string sec=sections[i]; b.Click += (s,e)=>ShowPage(sec);
        navButtons.Add(b); side.Controls.Add(b); sy+=44;
      }

      // content host
      content = new Panel{ Dock=DockStyle.Fill, BackColor=Bg, Padding=new Padding(0) };
      foreach(var sec in sections){ var pg=BuildPage(sec); pages[sec]=pg; pg.Visible=false; content.Controls.Add(pg); }

      Controls.Add(content);
      Controls.Add(detailStrip);
      Controls.Add(side);
      Controls.Add(bar);
      Controls.Add(header);

      ShowPage("Overview");
      RefreshFree();
    }

    Button Btn(string text,int x,int w,bool primary){
      var b=new Button{ Text=text, Location=new Point(x,11), Size=new Size(w,38), FlatStyle=FlatStyle.Flat,
        Font=new Font("Segoe UI",10f), ForeColor= primary?Color.White:Ink,
        BackColor= primary?Accent:Color.FromArgb(233,237,242) };
      b.FlatAppearance.BorderSize= primary?0:1;
      b.FlatAppearance.BorderColor=Line;
      b.FlatAppearance.MouseOverBackColor= primary?AccentDk:Color.FromArgb(222,228,236);
      return b;
    }

    void ShowPage(string sec){
      foreach(var kv in pages) kv.Value.Visible = (kv.Key==sec);
      foreach(var b in navButtons){ bool on=(string)b.Tag==sec; b.BackColor= on?SideSel:SideBg; b.ForeColor= on?Color.White:SideTxt; }
      if(sec=="Downloads" && dlList!=null && dlList.Items.Count==0) LoadDownloads();
    }

    Panel BuildPage(string sec){
      var pg=new Panel{ Dock=DockStyle.Fill, BackColor=Bg, AutoScroll=true, Padding=new Padding(22,18,22,10) };
      var title=new Label{ Text=sec, Font=FH1, ForeColor=Ink, Location=new Point(24,16), Size=new Size(700,30), AutoSize=false };
      pg.Controls.Add(title);

      if(sec=="Overview"){ BuildOverview(pg); return pg; }
      if(sec=="Downloads"){ BuildDownloads(pg); return pg; }

      var sub=new Label{ Text=SecSub(sec), Font=FBody, ForeColor=TextDim, Location=new Point(24,50), Size=new Size(720,20) };
      pg.Controls.Add(sub);
      var card=new Panel{ Location=new Point(24,80), Size=new Size(730,430), BackColor=CardBg,
        Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right|AnchorStyles.Bottom, AutoScroll=true };
      card.Paint += (s,e)=>{ using(var p=new Pen(Line)) e.Graphics.DrawRectangle(p,0,0,card.Width-1,card.Height-1); };
      pg.Controls.Add(card);
      int y=10;
      foreach(var c in cats){ if(c.Group!=sec) continue;
        var row=new Panel{ Location=new Point(1,y), Size=new Size(710,40), BackColor=CardBg };
        var cb=new CheckBox{ Text=c.Name, Location=new Point(14,9), Size=new Size(470,22), Font=FBody, Tag=c };
        if(c.Admin && !isAdmin){ cb.ForeColor=Color.Silver; cb.Enabled=false; }
        var sz=new Label{ Text="—", Location=new Point(500,10), Size=new Size(150,20), Font=FMono, ForeColor=TextDim,
          TextAlign=ContentAlignment.MiddleRight };
        var badge=new Label{ Text= c.Admin?"admin": (c.Safe?"safe":""), Location=new Point(560,11), Size=new Size(140,18),
          Font=FSmall, ForeColor= c.Admin?Warn:Good, TextAlign=ContentAlignment.MiddleRight };
        c.Box=cb; c.SizeLbl=sz;
        EventHandler show=(s,e)=>SetDetails(c);
        cb.MouseEnter+=show; cb.Enter+=show; cb.Click+=show; row.MouseEnter+=show;
        row.Controls.Add(cb); row.Controls.Add(sz); row.Controls.Add(badge);
        using(var g=CreateGraphics()){}
        var sep=new Panel{ Location=new Point(14,39), Size=new Size(682,1), BackColor=Line };
        row.Controls.Add(sep);
        card.Controls.Add(row); y+=41;
      }
      if(sec=="Advanced" && !isAdmin){
        var note=new Label{ Text="These need Administrator. Click ‘Restart as Admin’ below to enable them.",
          Location=new Point(24,516), Size=new Size(700,22), ForeColor=Warn, Font=FBody,
          Anchor=AnchorStyles.Left|AnchorStyles.Bottom };
        pg.Controls.Add(note);
        var ab=Btn("🛡  Restart as Admin", 24, 180, false); ab.Location=new Point(540,510);
        ab.Anchor=AnchorStyles.Right|AnchorStyles.Bottom;
        ab.Click += (s,e)=>{ try{ Process.Start(new ProcessStartInfo(Application.ExecutablePath){UseShellExecute=true,Verb="runas"}); Close(); }catch{ MessageBox.Show("Elevation cancelled."); } };
        pg.Controls.Add(ab);
      }
      return pg;
    }

    string SecSub(string sec){
      switch(sec){
        case "Windows Junk": return "Temporary files and caches Windows and your apps leave behind.";
        case "Browser Cache": return "Cached web files only — your passwords, bookmarks, history and logins are never touched.";
        case "Developer": return "Package and build caches from your dev tools. They re-download on demand.";
        case "Graphics": return "Compiled GPU shader caches. Cleared safely; they rebuild as you run apps/games.";
        case "Advanced": return "System-level cleanups that require Administrator rights.";
      }
      return "";
    }

    void BuildOverview(Panel pg){
      var intro=new Label{ Text="SweepDrive frees disk space by removing caches, temporary files and other safe-to-delete junk. "
        +"New here? Use Safe Clean — it removes only items that regenerate and never deletes your files, logins or settings.",
        Font=FBody, ForeColor=Ink, Location=new Point(24,54), Size=new Size(900,44),
        Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right };
      pg.Controls.Add(intro);

      var safeCard=new Panel{ Location=new Point(24,108), Size=new Size(900,148), BackColor=CardBg,
        Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right };
      safeCard.Paint += (s,e)=>{ using(var p=new Pen(Accent,2)) e.Graphics.DrawRectangle(p,1,1,safeCard.Width-3,safeCard.Height-3); };
      var st=new Label{ Text="✓  Safe Clean  (Recommended)", Font=FH2, ForeColor=Accent, Location=new Point(20,16), Size=new Size(500,26) };
      var sd=new Label{ Text="One click clears temp files, browser & app caches, shader caches and developer caches. "
        +"Keeps every document, photo, password, login and setting. No Administrator needed.",
        Font=FBody, ForeColor=Ink, Location=new Point(20,48), Size=new Size(855,40),
        Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right };
      var sb=Btn("✓  Run Safe Clean", 20, 220, true); sb.Location=new Point(20,98); sb.Size=new Size(220,38);
      sb.Click += (s,e)=>SafeClean();
      safeCard.Controls.Add(st); safeCard.Controls.Add(sd); safeCard.Controls.Add(sb);
      pg.Controls.Add(safeCard);

      var lh=new Label{ Text="Activity log", Font=FSemi, ForeColor=TextDim, Location=new Point(24,272), Size=new Size(200,20) };
      pg.Controls.Add(lh);
      log=new TextBox{ Location=new Point(24,294), Size=new Size(900,132), Multiline=true, ReadOnly=true,
        ScrollBars=ScrollBars.Vertical, BackColor=Color.White, BorderStyle=BorderStyle.FixedSingle, Font=FMono,
        Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right|AnchorStyles.Bottom };
      pg.Controls.Add(log);
      status=new Label{ Text="Ready.", Font=FSmall, ForeColor=TextDim, Location=new Point(24,430), Size=new Size(900,18),
        Anchor=AnchorStyles.Left|AnchorStyles.Bottom|AnchorStyles.Right };
      pg.Controls.Add(status);
      WLog(isAdmin?"Running as Administrator — all items available.":"Standard mode. Advanced items need ‘Restart as Admin’.");
    }

    // Downloads page
    void BuildDownloads(Panel pg){
      var sub=new Label{ Text="Biggest items in your Downloads (≥20 MB). Nothing is checked by default — tick what to remove. "
        +"Deletions go to the Recycle Bin, so they’re recoverable.", Font=FBody, ForeColor=TextDim,
        Location=new Point(24,50), Size=new Size(900,40), Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right };
      pg.Controls.Add(sub);
      dlList=new CheckedListBox{ Location=new Point(24,94), Size=new Size(900,360), CheckOnClick=true, Font=FMono,
        Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right|AnchorStyles.Bottom, BorderStyle=BorderStyle.FixedSingle };
      pg.Controls.Add(dlList);
      dlInfo=new Label{ Text="", Font=FSmall, ForeColor=TextDim, Location=new Point(24,458), Size=new Size(900,18),
        Anchor=AnchorStyles.Left|AnchorStyles.Bottom|AnchorStyles.Right };
      pg.Controls.Add(dlInfo);
      var del=Btn("🗑  Delete checked → Recycle Bin", 24, 260, true); del.Location=new Point(24,480);
      del.Anchor=AnchorStyles.Left|AnchorStyles.Bottom; del.Click+=(s,e)=>DeleteDownloads();
      var rs=Btn("↻  Rescan", 300, 110, false); rs.Location=new Point(300,480);
      rs.Anchor=AnchorStyles.Left|AnchorStyles.Bottom; rs.Click+=(s,e)=>LoadDownloads();
      pg.Controls.Add(del); pg.Controls.Add(rs);
    }

    void SetDetails(Cat c){
      dName.Text=c.Name; dWhat.Text=c.What;
      dRegen.Text="Regenerates: "+c.Regen;
      dRegen.ForeColor = c.Regen.StartsWith("Yes")?Good : c.Regen.StartsWith("No")?Warn : TextDim;
      dLoss.Text="What you lose: "+c.Loss;
    }
    void WLog(string t){ if(log!=null) log.AppendText(DateTime.Now.ToString("HH:mm:ss")+"  "+t+"\r\n"); if(status!=null) status.Text=t; }
    void RefreshFree(){ try{ total.Text="Free on C: "+GB(new DriveInfo("C").AvailableFreeSpace); }catch{} }

    // ---------- definitions ----------
    void Define(){
      Action<string,string,string,string,bool,bool,string[],string,string,string> add =
        (key,name,grp,type,admin,safe,paths,what,regen,loss)=>
          cats.Add(new Cat{Key=key,Name=name,Group=grp,Type=type,Admin=admin,Safe=safe,Paths=paths,What=what,Regen=regen,Loss=loss});

      string WJ="Windows Junk", BR="Browser Cache", DV="Developer", GR="Graphics", AD="Advanced";

      add("utemp","User Temp files",WJ,"contents",false,true,new[]{Path.Combine(L,"Temp")},
        "Temporary files apps write and often forget to remove. Usually the biggest quick win.",
        "Yes — recreated as needed.","Nothing. Close running apps first so in-use files can be removed.");
      add("rbin","Recycle Bin",WJ,"recyclebin",false,false,new[]{@"C:\$Recycle.Bin"},
        "Files you deleted that still occupy disk until the bin is emptied.",
        "N/A — refills as you delete.","Anything currently in the bin is gone for good. Check it first. (Not part of Safe Clean.)");
      add("thumb","Thumbnail cache",WJ,"thumbcache",false,true,new[]{Path.Combine(L,@"Microsoft\Windows\Explorer")},
        "Cached preview thumbnails for photos, videos and documents.",
        "Yes — rebuilt as you browse folders.","Nothing; thumbnails just regenerate.");
      add("clip","Clipboard contents",WJ,"clipboard",false,true,new string[0],
        "Whatever is currently on your clipboard.","N/A.","Only what you last copied. Trivial.");
      add("crash","Crash dumps & error reports",WJ,"contents",false,true,new[]{Path.Combine(L,"CrashDumps"),Path.Combine(L,@"Microsoft\Windows\WER")},
        "Data written when apps crash (memory dumps, error reports).",
        "Yes — only created by crashes.","Nothing unless you’re debugging a specific crash now.");
      add("recent","Recent files & jump lists",WJ,"contents",false,false,new[]{Path.Combine(A,@"Microsoft\Windows\Recent")},
        "The Recent-files list and taskbar jump lists (not the files).",
        "Yes — rebuilds as you open files.","Your recent/jump-list shortcuts reset. Files untouched. (Not in Safe Clean.)");
      add("dns","Flush DNS cache",WJ,"dns",false,true,new string[0],
        "Cached domain lookups. Flushing can also fix stale site resolution.",
        "Yes — repopulates instantly.","Nothing.");

      add("chrome","Chrome cache",BR,"contents",false,true,new[]{Path.Combine(L,@"Google\Chrome\User Data\Default\Cache"),Path.Combine(L,@"Google\Chrome\User Data\Default\Code Cache")},
        "Cached web files for faster loading. Passwords, bookmarks, history and logins are NOT touched.",
        "Yes — rebuilds as you browse.","Sites reload once; you stay logged in.");
      add("edge","Edge cache",BR,"contents",false,true,new[]{Path.Combine(L,@"Microsoft\Edge\User Data\Default\Cache"),Path.Combine(L,@"Microsoft\Edge\User Data\Default\Code Cache")},
        "Edge cached web files. Logins/bookmarks/history kept.",
        "Yes — rebuilds as you browse.","Sites reload once; you stay logged in.");
      add("ff","Firefox cache",BR,"contents",false,true,new[]{Path.Combine(L,@"Mozilla\Firefox\Profiles")},
        "Firefox cached web files. Logins/bookmarks kept.",
        "Yes — rebuilds as you browse.","Sites reload once.");

      add("pip","pip cache",DV,"contents",false,true,new[]{Path.Combine(L,@"pip\cache")},
        "Downloaded Python package wheels kept for faster re-installs.",
        "Yes — re-downloads on next install.","Nothing — slower first re-install.");
      add("npm","npm / pnpm cache",DV,"contents",false,true,new[]{Path.Combine(L,"npm-cache"),Path.Combine(L,"pnpm-cache")},
        "Cached Node.js packages for npm and pnpm.","Yes — re-fetched on next install.","Nothing — slower first install.");
      add("gradle","Gradle cache",DV,"contents",false,true,new[]{Path.Combine(U,@".gradle\caches")},
        "Cached Java/Android build dependencies.","Yes — re-downloaded on next build.","Nothing — slower next build.");
      add("pw","Playwright browsers",DV,"contents",false,true,new[]{Path.Combine(L,"ms-playwright"),Path.Combine(L,"ms-playwright-go")},
        "Browser binaries downloaded by Playwright for testing/automation.",
        "Yes — via ‘playwright install’.","Nothing unless you must run Playwright offline immediately.");
      add("dcache","~/.cache",DV,"contents",false,true,new[]{Path.Combine(U,".cache")},
        "General cache used by many dev/CLI tools (e.g. Hugging Face).",
        "Yes — rebuilt on demand.","Usually nothing; some tools re-download large models cached here.");
      add("conda","Conda package cache",DV,"contents",false,true,new[]{Path.Combine(U,@"miniconda3\pkgs")},
        "Downloaded conda package tarballs reused across environments.",
        "Yes — re-downloaded when needed.","Nothing — slower env creation once.");

      add("dx","DirectX shader cache",GR,"contents",false,true,new[]{Path.Combine(L,"D3DSCache")},
        "Compiled GPU shaders cached by DirectX to speed up apps/games.",
        "Yes — rebuilt automatically.","Nothing — brief one-time recompile stutter.");
      add("nv","NVIDIA shader cache",GR,"contents",false,true,new[]{Path.Combine(L,@"NVIDIA\DXCache"),Path.Combine(L,@"NVIDIA\GLCache"),Path.Combine(L,@"NVIDIA Corporation\NV_Cache")},
        "NVIDIA driver’s compiled shader cache for games/3D apps.",
        "Yes — rebuilt by the driver.","Nothing — brief recompile stutter.");

      add("wtemp","Windows Temp",AD,"contents",true,false,new[]{Path.Combine(Win,"Temp")},
        "System-wide temp files under C:\\Windows\\Temp.","Yes — recreated as needed.","Nothing; in-use files are skipped.");
      add("wupd","Windows Update leftover files",AD,"contents",true,false,new[]{Path.Combine(Win,@"SoftwareDistribution\Download")},
        "Update installers left after updates apply.","Yes — re-downloaded if needed.","Nothing on an up-to-date system.");
      add("dopt","Delivery Optimization cache",AD,"contents",true,false,new[]{@"C:\Windows\ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache"},
        "Cached update/app chunks Windows shares on your network.","Yes — repopulated by Windows Update.","Nothing.");
      add("wlogs","Windows log files",AD,"contents",true,false,new[]{Path.Combine(Win,"Logs")},
        "Diagnostic logs from Windows components (CBS, DISM, setup).","Yes — written as needed.","Nothing unless diagnosing a system issue now.");
      add("memdump","Memory dumps",AD,"memdump",true,false,new[]{Path.Combine(Win,"Minidump"),Path.Combine(Win,"MEMORY.DMP")},
        "Crash dumps after a Blue Screen (MEMORY.DMP + minidumps).","Yes — only future crashes create them.","Nothing unless analyzing a past BSOD.");
      add("prefetch","Prefetch data",AD,"contents",true,false,new[]{Path.Combine(Win,"Prefetch")},
        "App-launch optimization data Windows uses to start programs faster.",
        "Yes — rebuilt within a few launches.","Nothing lasting; apps launch slightly slower for a day.");
      add("dism","WinSxS component cleanup (DISM)",AD,"dism",true,false,new string[0],
        "Removes superseded Windows update components from the component store.",
        "N/A — managed by Windows.","You lose the ability to uninstall already-superseded updates. Takes minutes.");
      add("wsl","Compact WSL disk",AD,"wsl",true,false,new string[0],
        "Shrinks the WSL2 Linux virtual disk to reclaim empty space it never releases on its own.",
        "N/A.","Nothing — all Linux files kept; only unused empty space is reclaimed.");
    }

    // ---------- sizing ----------
    long DirSize(string path){
      if(string.IsNullOrEmpty(path)||!Directory.Exists(path)) return 0;
      long sum=0;
      try{ var di=new DirectoryInfo(path); if((di.Attributes&FileAttributes.ReparsePoint)!=0) return 0;
        foreach(var f in di.GetFiles()){ try{sum+=f.Length;}catch{} }
        foreach(var d in di.GetDirectories()){ try{ if((d.Attributes&FileAttributes.ReparsePoint)!=0) continue; sum+=DirSize(d.FullName);}catch{} }
      }catch{}
      return sum;
    }
    long FileLen(string p){ try{ return File.Exists(p)?new FileInfo(p).Length:0; }catch{ return 0; } }
    List<string> WslVhdx(){ var l=new List<string>(); foreach(var r in new[]{Path.Combine(L,"wsl"),Path.Combine(L,"Packages")}){ try{ if(Directory.Exists(r)) l.AddRange(Directory.GetFiles(r,"ext4.vhdx",System.IO.SearchOption.AllDirectories)); }catch{} } return l; }
    long CatSize(Cat c){
      switch(c.Type){
        case "dns": case "dism": case "clipboard": return -1;
        case "wsl": { long s=0; foreach(var v in WslVhdx()) s+=FileLen(v); return s; }
        case "recyclebin": return DirSize(@"C:\$Recycle.Bin");
        case "thumbcache": { long s=0; try{ foreach(var f in Directory.GetFiles(Path.Combine(L,@"Microsoft\Windows\Explorer"),"thumbcache_*.db")) s+=FileLen(f);}catch{} return s; }
        case "memdump": { long s=DirSize(Path.Combine(Win,"Minidump")); s+=FileLen(Path.Combine(Win,"MEMORY.DMP")); return s; }
        default: { long s=0; foreach(var p in c.Paths) s+=DirSize(p); return s; }
      }
    }
    string GB(long b){ return (b/1073741824.0).ToString("0.00")+" GB"; }

    void DoScan(){
      Cursor=Cursors.WaitCursor; WLog("Scanning sizes…"); Application.DoEvents();
      long selc=0;
      foreach(var c in cats){ long b=CatSize(c);
        if(c.SizeLbl!=null) c.SizeLbl.Text = b<0?"—":GB(b);
        if(c.Box!=null && c.Box.Checked && b>0) selc+=b; }
      RefreshFree(); WLog("Scan complete."); Cursor=Cursors.Default;
    }

    List<Cat> Checked(){ var l=new List<Cat>(); foreach(var c in cats) if(c.Box!=null && c.Box.Checked) l.Add(c); return l; }

    void SafeClean(){
      var l=new List<Cat>(); foreach(var c in cats) if(c.Safe && !c.Admin) l.Add(c);
      CleanList(l, "Run Safe Clean on {0} cache/temp item(s)? Your files, logins and settings are kept.");
    }

    void DeleteContents(string path){ if(!Directory.Exists(path)) return;
      foreach(var f in Directory.GetFiles(path)){ try{ File.SetAttributes(f,FileAttributes.Normal); File.Delete(f);}catch{} }
      foreach(var d in Directory.GetDirectories(path)){ try{ Directory.Delete(d,true);}catch{} } }
    void RunProc(string exe,string args){ try{ var p=new Process(); p.StartInfo=new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true}; p.Start(); p.WaitForExit(); }catch(Exception ex){ WLog("  proc error: "+ex.Message); } }

    void CleanList(List<Cat> sel, string confirmFmt){
      if(sel.Count==0){ WLog("Nothing to clean."); return; }
      if(!isAdmin && sel.Exists(c=>c.Admin)){ MessageBox.Show("Some items need Administrator. Use ‘Restart as Admin’ on the Advanced tab."); return; }
      if(MessageBox.Show(string.Format(confirmFmt,sel.Count),"Confirm",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes) return;
      Cursor=Cursors.WaitCursor; long f0=new DriveInfo("C").AvailableFreeSpace;
      foreach(var c in sel){ try{
        switch(c.Type){
          case "recyclebin": Native.SHEmptyRecycleBin(IntPtr.Zero,null,0x7); WLog("Emptied Recycle Bin"); break;
          case "dns": RunProc("ipconfig","/flushdns"); WLog("Flushed DNS cache"); break;
          case "clipboard": try{ Clipboard.Clear(); }catch{} WLog("Cleared clipboard"); break;
          case "thumbcache": try{ foreach(var f in Directory.GetFiles(Path.Combine(L,@"Microsoft\Windows\Explorer"),"thumbcache_*.db")){ try{File.Delete(f);}catch{} } }catch{} WLog("Cleared thumbnail cache"); break;
          case "memdump": DeleteContents(Path.Combine(Win,"Minidump")); try{File.Delete(Path.Combine(Win,"MEMORY.DMP"));}catch{} WLog("Cleared memory dumps"); break;
          case "dism": WLog("Running DISM (minutes)…"); Application.DoEvents(); RunProc("dism.exe","/online /cleanup-image /startcomponentcleanup"); WLog("DISM done"); break;
          case "wsl": WLog("Compacting WSL disk…"); Application.DoEvents(); RunProc("wsl.exe","--shutdown"); System.Threading.Thread.Sleep(4000);
            foreach(var v in WslVhdx()){ string dp=Path.Combine(Path.GetTempPath(),"sweepr_wc.txt");
              File.WriteAllText(dp,"select vdisk file=\""+v+"\"\nattach vdisk readonly\ncompact vdisk\ndetach vdisk\nexit\n");
              RunProc("diskpart.exe","/s \""+dp+"\""); WLog("Compacted "+Path.GetFileName(v)); } break;
          default: foreach(var p in c.Paths) DeleteContents(p); WLog("Cleaned "+c.Name); break;
        }
      }catch(Exception ex){ WLog("ERROR "+c.Name+": "+ex.Message); } }
      long f1=new DriveInfo("C").AvailableFreeSpace; double g=(f1-f0)/1073741824.0;
      WLog("SweepDriveed "+g.ToString("0.00")+" GB. Free now "+GB(f1)); RefreshFree();
      MessageBox.Show("SweepDriveed "+g.ToString("0.00")+" GB.\nFree on C: now "+GB(f1),"Done",MessageBoxButtons.OK,MessageBoxIcon.Information);
      Cursor=Cursors.Default; DoScan();
    }

    // ---------- downloads ----------
    long SizeOf(string p){ try{ if(File.Exists(p)) return new FileInfo(p).Length;
      long s=0; var di=new DirectoryInfo(p); if((di.Attributes&FileAttributes.ReparsePoint)!=0) return 0;
      foreach(var f in di.GetFiles("*",System.IO.SearchOption.AllDirectories)){ try{s+=f.Length;}catch{} } return s; }catch{ return 0; } }
    void LoadDownloads(){
      string dl=Path.Combine(U,"Downloads"); dlList.Items.Clear(); dlPaths.Clear();
      dlInfo.Text="Scanning Downloads…"; Cursor=Cursors.WaitCursor; Application.DoEvents();
      if(!Directory.Exists(dl)){ dlInfo.Text="Downloads folder not found."; Cursor=Cursors.Default; return; }
      var items=new List<KeyValuePair<string,long>>();
      try{ foreach(var e in Directory.GetFileSystemEntries(dl)) items.Add(new KeyValuePair<string,long>(e,SizeOf(e))); }catch{}
      items.Sort((a,b)=>b.Value.CompareTo(a.Value)); long tot=0;
      foreach(var kv in items){ if(kv.Value<20L*1024*1024) continue;
        bool isDir=Directory.Exists(kv.Key); string date="";
        try{ date=(isDir?Directory.GetLastWriteTime(kv.Key):File.GetLastWriteTime(kv.Key)).ToString("yyyy-MM-dd"); }catch{}
        dlList.Items.Add(string.Format("{0,8:0.00} GB   {1}   {2}{3}",kv.Value/1073741824.0,date,isDir?"[folder] ":"         ",Path.GetFileName(kv.Key)));
        dlPaths.Add(kv.Key); tot+=kv.Value; }
      dlInfo.Text=string.Format("{0} items ≥20 MB.  Downloads total ~{1:0.0} GB.",dlList.Items.Count,tot/1073741824.0);
      Cursor=Cursors.Default;
    }
    void DeleteDownloads(){
      var idx=new List<int>(); foreach(int i in dlList.CheckedIndices) idx.Add(i);
      if(idx.Count==0){ MessageBox.Show("Nothing checked."); return; }
      if(MessageBox.Show("Send "+idx.Count+" item(s) to the Recycle Bin? You can restore them from there.","Confirm",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) return;
      Cursor=Cursors.WaitCursor; int ok=0; idx.Reverse();
      foreach(int i in idx){ string p=dlPaths[i];
        try{ if(Directory.Exists(p)) FileSystem.DeleteDirectory(p,UIOption.OnlyErrorDialogs,RecycleOption.SendToRecycleBin);
             else FileSystem.DeleteFile(p,UIOption.OnlyErrorDialogs,RecycleOption.SendToRecycleBin); ok++; }
        catch(Exception ex){ MessageBox.Show("Could not delete "+Path.GetFileName(p)+": "+ex.Message); } }
      Cursor=Cursors.Default; MessageBox.Show(ok+" item(s) moved to Recycle Bin."); LoadDownloads(); RefreshFree();
    }

    [STAThread]
    static void Main(){ Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm()); }
  }
}
