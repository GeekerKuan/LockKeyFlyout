using System.Runtime.InteropServices;

namespace LockKeyFlyout;

// High-quality vector rendering for the two shapes that GDI cannot render
// smoothly on a transparent composition surface. Coordinates mirror the
// original LockWindow.xaml 24x24 lock paths and 60x4 rounded Rectangle.
internal static unsafe class GdiPlus
{
    static nuint token;
    [StructLayout(LayoutKind.Sequential)] struct StartupInput { public uint Version; public nint Callback; public int NoBackgroundThread,NoExternalCodecs; }

    public static void Start(){StartupInput input=new(){Version=1};GdiplusStartup(out token,&input,0);}
    public static void Stop(){if(token!=0)GdiplusShutdown(token);}
    public static void Draw(nint hdc,int dpi,bool locked,int clientWidth,int clientHeight)
    {
        if(token==0)return;nint g;GdipCreateFromHDC(hdc,out g);GdipSetSmoothingMode(g,4);GdipSetPixelOffsetMode(g,4);GdipSetCompositingQuality(g,4);
        float s=dpi/96f,ox=14*s,oy=14*s,vs=22f/24f*s;
        nint white,pen;GdipCreateSolidFill(0xffffffff,out white);GdipCreatePen1(0xffffffff,1.5f*vs,2,out pen);

        // Rounded lock body: XAML outer geometry x=4..20, y=7..21.
        nint body=RoundedPath(ox+4*vs,oy+7*vs,16*vs,14*vs,3.25f*vs);GdipDrawPath(g,pen,body);GdipDeletePath(body);
        GdipFillEllipse(g,white,ox+10.5f*vs,oy+12.5f*vs,3*vs,3*vs);

        // Exact closed shackle silhouette from the original FluentFlyout path.
        nint shackle;GdipCreatePath(0,out shackle);GdipStartPathFigure(shackle);GdipAddPathLine(shackle,ox+8*vs,oy+7*vs,ox+8*vs,oy+5*vs);
        GdipAddPathBezier(shackle,ox+8*vs,oy+5*vs,ox+8*vs,oy+2.79f*vs,ox+9.79f*vs,oy+1*vs,ox+12*vs,oy+1*vs);
        GdipAddPathBezier(shackle,ox+12*vs,oy+1*vs,ox+14.21f*vs,oy+1*vs,ox+16*vs,oy+2.79f*vs,ox+16*vs,oy+5*vs);
        GdipAddPathLine(shackle,ox+16*vs,oy+5*vs,ox+16*vs,oy+7*vs);GdipAddPathLine(shackle,ox+16*vs,oy+7*vs,ox+14.5f*vs,oy+7*vs);GdipAddPathLine(shackle,ox+14.5f*vs,oy+7*vs,ox+14.5f*vs,oy+5*vs);
        GdipAddPathBezier(shackle,ox+14.5f*vs,oy+5*vs,ox+14.5f*vs,oy+3.62f*vs,ox+13.38f*vs,oy+2.5f*vs,ox+12*vs,oy+2.5f*vs);
        GdipAddPathBezier(shackle,ox+12*vs,oy+2.5f*vs,ox+10.62f*vs,oy+2.5f*vs,ox+9.5f*vs,oy+3.62f*vs,ox+9.5f*vs,oy+5*vs);
        GdipAddPathLine(shackle,ox+9.5f*vs,oy+5*vs,ox+9.5f*vs,oy+7*vs);GdipClosePathFigure(shackle);
        if(!locked){nint matrix;GdipCreateMatrix(out matrix);float cx=ox+16*vs,cy=oy+7*vs;GdipTranslateMatrix(matrix,-cx,-cy,1);GdipRotateMatrix(matrix,25,1);GdipTranslateMatrix(matrix,cx,cy,1);GdipTransformPath(shackle,matrix);GdipDeleteMatrix(matrix);}
        GdipFillPath(g,white,shackle);GdipDeletePath(shackle);

        float barW=(locked?60:36)*s,barH=4*s,barX=(clientWidth-barW)/2f,barY=clientHeight-10*s;nint accent;GdipCreateSolidFill(locked?0xff60cdffu:0xff808080u,out accent);nint bar=RoundedPath(barX,barY,barW,barH,2*s);GdipFillPath(g,accent,bar);
        GdipDeletePath(bar);GdipDeleteBrush(accent);GdipDeletePen(pen);GdipDeleteBrush(white);GdipDeleteGraphics(g);
    }
    static nint RoundedPath(float x,float y,float w,float h,float r){nint p;GdipCreatePath(0,out p);float d=r*2;GdipAddPathArc(p,x,y,d,d,180,90);GdipAddPathArc(p,x+w-d,y,d,d,270,90);GdipAddPathArc(p,x+w-d,y+h-d,d,d,0,90);GdipAddPathArc(p,x,y+h-d,d,d,90,90);GdipClosePathFigure(p);return p;}

    [DllImport("gdiplus")]static extern int GdiplusStartup(out nuint token,StartupInput* input,nint output);[DllImport("gdiplus")]static extern void GdiplusShutdown(nuint token);
    [DllImport("gdiplus")]static extern int GdipCreateFromHDC(nint hdc,out nint graphics);[DllImport("gdiplus")]static extern int GdipDeleteGraphics(nint graphics);[DllImport("gdiplus")]static extern int GdipSetSmoothingMode(nint g,int mode);[DllImport("gdiplus")]static extern int GdipSetPixelOffsetMode(nint g,int mode);[DllImport("gdiplus")]static extern int GdipSetCompositingQuality(nint g,int quality);
    [DllImport("gdiplus")]static extern int GdipCreateSolidFill(uint color,out nint brush);[DllImport("gdiplus")]static extern int GdipDeleteBrush(nint brush);[DllImport("gdiplus")]static extern int GdipCreatePen1(uint color,float width,int unit,out nint pen);[DllImport("gdiplus")]static extern int GdipDeletePen(nint pen);
    [DllImport("gdiplus")]static extern int GdipCreatePath(int mode,out nint path);[DllImport("gdiplus")]static extern int GdipDeletePath(nint path);[DllImport("gdiplus")]static extern int GdipStartPathFigure(nint path);[DllImport("gdiplus")]static extern int GdipClosePathFigure(nint path);[DllImport("gdiplus")]static extern int GdipAddPathLine(nint path,float x1,float y1,float x2,float y2);[DllImport("gdiplus")]static extern int GdipAddPathBezier(nint path,float x1,float y1,float x2,float y2,float x3,float y3,float x4,float y4);[DllImport("gdiplus")]static extern int GdipAddPathArc(nint path,float x,float y,float w,float h,float start,float sweep);[DllImport("gdiplus")]static extern int GdipDrawPath(nint g,nint pen,nint path);[DllImport("gdiplus")]static extern int GdipFillPath(nint g,nint brush,nint path);[DllImport("gdiplus")]static extern int GdipFillEllipse(nint g,nint brush,float x,float y,float w,float h);
    [DllImport("gdiplus")]static extern int GdipCreateMatrix(out nint matrix);[DllImport("gdiplus")]static extern int GdipDeleteMatrix(nint matrix);[DllImport("gdiplus")]static extern int GdipTranslateMatrix(nint matrix,float dx,float dy,int order);[DllImport("gdiplus")]static extern int GdipRotateMatrix(nint matrix,float angle,int order);[DllImport("gdiplus")]static extern int GdipTransformPath(nint path,nint matrix);
}
