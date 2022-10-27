using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//AzureKinectSDKの読み込み
using Microsoft.Azure.Kinect.Sensor;
//(追加)AzureKinectとSystemの変数名の曖昧さをなくすため下記を追加
using Image = Microsoft.Azure.Kinect.Sensor.Image;
using BitmapData = System.Drawing.Imaging.BitmapData;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
namespace WindowsFormsApp3
{
    public partial class Form1 : Form
    {
        //Kinectを扱う変数
        Device kinect;
        //(追加3)カラー/Depth画像をBitmapとして扱う変数
        Bitmap colorBitmap;
        Bitmap depthBitmap;
        //追加4)各画像間の位置合わせ変換
        Transformation transformation;
        //(追加5)Kinectからのデータ取得を継続するフラグ
        bool loop = true;
        public Form1()
        {
            InitializeComponent();
            InitKinect();
            //(追加10)画像の初期化後、Kinectからのデータ取得開始
            InitBitmap();
            Task t = KinectLoop();
        }
        //(追加6)Bitmap画像に関する初期設定
        private void InitBitmap()
        {
            //(追加1)カラー画像の横幅(width)と縦幅(height)を取得
            int width = kinect.GetCalibration().DepthCameraCalibration.ResolutionWidth;
            int height = kinect.GetCalibration().DepthCameraCalibration.ResolutionHeight;
            //(追加2)PictureBoxに貼り付けるBitmap画像を作成。サイズはkinectのカラー画像と同じ
            colorBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            depthBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }
        //(追加7)Kinectからデータを取得して表示するメソッド
        private async Task KinectLoop()
        {
            //loopがtrueの間はデータを取り続ける
            while (loop)
            {
                //kinectから新しいデータをもらう
                using (Capture capture = await Task.Run(() => kinect.GetCapture()).ConfigureAwait(true))
                {
                    //カラー/Depth画像にKinectで取得した情報を書き込む
                    SetDepthBitmap(capture);
                    //カラー/Depth画像をPictureBoxに貼り付ける
                    pictureBox1.Image = depthBitmap;
                }
                //表示を更新
                this.Update();
            }
            //ループが終了したらKinectも停止
            kinect.StopCameras();
        }
        //(追加9)BitmapにKinectのDepth情報を書き込む
        private void SetDepthBitmap(Capture capture)
        {
            //Depth画像を取得
            Image depthImage = capture.Depth;
            //Depth画像の各ピクセルの値(奥行)のみを取得
            ushort[] depthArray = depthImage.GetPixels<ushort>().ToArray();
            //depthBitmapの各画素に値を書き込む準備
            BitmapData bitmapData = depthBitmap.LockBits(new Rectangle(0, 0, depthBitmap.Width, depthBitmap.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                //各ピクセルの値へのポインタ
                byte* pixels = (byte*)bitmapData.Scan0;
                int index;
                int depth;
                //1ピクセルずつ処理
                for (int i = 0; i < depthArray.Length; i++)
                {
                    //500～5000mmを0～255に変換
                    depth = (int)(255 * (depthArray[i] - 500) / 5000.0);
                    if (depth < 0) depth = 0;
                    if (depth > 255) depth = 255;
                    index = i * 4;
                    pixels[index++] = (byte)(depth + 200) ;
                    pixels[index++] = (byte)(depth + 200;
                    pixels[index++] = (byte)(depth + 200;

                    pixels[index++] = 255; //Alphaは不透明でOK
                }
            }
            //書き込み終了
            depthBitmap.UnlockBits(bitmapData);
            //用済みのdepthImageのメモリを解放
            depthImage.Dispose();
        }
        //Kinectの初期化(Form1コンストラクタから呼び出す)
        private void InitKinect()
        {
            //0番目のKinectと接続
            kinect = Device.Open(0);
            //Kinectの各種モードを設定して動作開始(設定内容自体は今回は特に考えなくてOK)
            kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R720p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });
            //(追加)画像間の位置合わせに関する情報を取得し、transformationに代入
            transformation = kinect.GetCalibration().CreateTransformation();
        }
        //アプリ終了時にKinect終了
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            kinect.StopCameras();
        }
    }
}
