using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace _0525_3skeleton
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        KinectSensor kinect;

        BodyFrameReader bodyFrameReader;
        Body[] bodies;

        int rightflag = 0;

        public MainWindow()
        {
            InitializeComponent();
            FillCommports();
            SPBluetooth.GetInsntace().DataReceived += MainWindow_DataReceived;
        }

        void MainWindow_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            Console.Write(SPBluetooth.GetInsntace().ReadExisting());
        }

        // Port names all in the computer

        private string[] GetPortNames()
        {
            return System.IO.Ports.SerialPort.GetPortNames();

        }

        // fill ports to combobox

        private void FillCommports()
        {
            comboCOMMPorts.Items.Clear();

            foreach (string port in GetPortNames())
            {
                comboCOMMPorts.Items.Add(port);
            }

            if (comboCOMMPorts.Items.Count > 0)
            {
                comboCOMMPorts.SelectedIndex = 0;
            }
        }

        private void Window_Loaded(object sensor, RoutedEventArgs e)
        {
            try
            {
                //kinectを開く
                kinect = KinectSensor.GetDefault();
                if (kinect == null)
                {
                    throw new Exception("kinectを開けません");
                }

                kinect.Open();
                
                //ボディーリーダーを開く
                bodyFrameReader = kinect.BodyFrameSource.OpenReader();
                bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

                //Bodyを入れる配列を作る
                bodies = new Body[kinect.BodyFrameSource.BodyCount];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                    Close();
            }
        }

        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {

            
            UpdateBodyFrame(e);
            CheckJointPositions();
            DrawBodyFrame();
            

        }

        private void CheckJointPositions()
        {
            // get the zeroth body
            if (bodies.Length == 0)
                return;

            Body body = bodies[0];

            double handRight = Math.Round(body.Joints[JointType.HandRight].Position.X * 10,0);
            double elbowRight = Math.Round(body.Joints[JointType.ElbowRight].Position.X * 10,0);

            if (body.Joints[JointType.HandRight].Position.Y <= body.Joints[JointType.ElbowRight].Position.Y)
            {
                lblTitle.Content = "No";
                return;
            }

            if (handRight > elbowRight)
                rightflag = 1;
            else if (handRight < elbowRight)
                rightflag = 2;
            else if (handRight == elbowRight) {
                if (rightflag == 2)
                {
                    Console.WriteLine("Waved");
                    lblTitle.Content = "Waved";
                    try
                    {
                        if (SPBluetooth.GetInsntace().IsOpen)
                        {
                            SPBluetooth.GetInsntace().DiscardInBuffer();
                            SPBluetooth.GetInsntace().DiscardOutBuffer();

                            SPBluetooth.GetInsntace().WriteLine("SUW,123456789012345678901234567890AB,10");
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                    catch (Exception r)
                    {
                        Console.WriteLine(r.Message);
                    }
                }
                else {
                   
                }
            }
            
        }

        //ボディの更新
        private void UpdateBodyFrame(BodyFrameArrivedEventArgs e)
        {


            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    return;
                }

                //ボディデータを取得する
                bodyFrame.GetAndRefreshBodyData(bodies);
            }
        }

        //ボディの表示
        private void DrawBodyFrame()
        {

            CanvasBody.Children.Clear();
            if (bodies.Length == 0)
                return;
            Body body = bodies[0];
            //追跡しているBodyのみループする
            //foreach (var body in bodies.Where(b => b.IsTracked ))
            //{

                

                foreach (var joint in body.Joints)
                {
                    

                    

                    //手の位置が追跡状態
                    if (joint.Value.TrackingState == TrackingState.Tracked)
                    {
                        DrawEllipse(joint.Value, 10, Brushes.Blue);
                    }
                    //手の位置が推測状態
                    else if (joint.Value.TrackingState == TrackingState.Inferred)
                    {
                        DrawEllipse(joint.Value, 10, Brushes.Yellow);
                    }
                }
            //}
        }

        private void DrawEllipse(Joint joint, int R, Brush brush )
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                Fill = brush,
            };

            //カメラ座標をdepth座標に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToDepthSpace(
                                                                    joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            //Depth座標系で円を配置する
            Canvas.SetLeft(ellipse, point.X - (R / 2));
            Canvas.SetTop(ellipse, point.Y - (R / 2));

            CanvasBody.Children.Add(ellipse);
        }
        private void Window_Closing(object sensor,
                System.ComponentModel.CancelEventArgs e)
        {
            if (bodyFrameReader != null)
            {
                bodyFrameReader.Dispose();
                bodyFrameReader = null;
            }
            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
        }

        private void cmdConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SPBluetooth.GetInsntace().IsOpen)
                {
                    SPBluetooth.GetInsntace().Close();
                    cmdConnect.Content = "Connect";
                }
                else
                {
                    SPBluetooth.GetInsntace().PortName = comboCOMMPorts.SelectedItem.ToString().Trim();

                    // configs
                    SPBluetooth.GetInsntace().BaudRate = 115200;
                    SPBluetooth.GetInsntace().DataBits = 8;
                    SPBluetooth.GetInsntace().StopBits = System.IO.Ports.StopBits.One;
                    SPBluetooth.GetInsntace().Parity = System.IO.Ports.Parity.None;

                    SPBluetooth.GetInsntace().Open();
                    cmdConnect.Content = "Disconnect";
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Comm port Not Available \n コンポート接続されていません", "Comm port Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmdEchoOn_Click(object sender, RoutedEventArgs e)
        {
            SPBluetooth.GetInsntace().WriteLine("+");
        }

        private void cmdEchoOff_Click(object sender, RoutedEventArgs e)
        {
            SPBluetooth.GetInsntace().WriteLine("+");
        }

        private void cmdSearch_Click(object sender, RoutedEventArgs e)
        {
            SPBluetooth.GetInsntace().WriteLine("F");
        }

        private void cmdStopSearch_Click(object sender, RoutedEventArgs e)
        {
            SPBluetooth.GetInsntace().WriteLine("X");
        }

        private void cmdsconnectBlootooth_Click(object sender, RoutedEventArgs e)
        {
            SPBluetooth.GetInsntace().WriteLine("E,0,001EC0462DE8");
        }

        private void cmdbuzzer_Click(object sender, RoutedEventArgs e)
        {
            SPBluetooth.GetInsntace().WriteLine("SUW,123456789012345678901234567890AB,10");
        }
    }
}
