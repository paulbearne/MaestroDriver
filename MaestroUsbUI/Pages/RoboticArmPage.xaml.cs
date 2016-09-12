using MaestroUsb;
using Pololu.Usc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MaestroUsbUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RoboticArmPage : Page
    {

        private MaestroDeviceListItem maestroDevice;
        // maestro settings
        private UscSettings settings;
        private ServoStatus[] servoStatus;

        // length of arm in feet
        private float BaseHeight = 0.053f + 0.006f + 0.0203f;
        private float BaseRadius = 0.095f / 2f;
        private float LowerArmLength = 0.121f;
        private float UpperArmLength = 0.122f;
        private float GripperLength = 0.0925f;
        private float WristLength = 0.011f + 0.052f;

        private double mXPos = 0, mYPos = 0, mPickAngle = 0;// Slider values
        private Point UpperArmStartPt = new Point(0, 0);
        private Point UpperArmEndPt = new Point(400, 0);
        private Point LowerArmEndPt = new Point(0, 0);
        private Point WristEndPt = new Point(0, 0);
        private Point GripperEndPt = new Point(0, 0);
        private LineGeometry lineLowerArm = new LineGeometry();
        private LineGeometry lineUpperArm = new LineGeometry();
        private LineGeometry lineWrist = new LineGeometry();
        private LineGeometry lineGripper = new LineGeometry();
        private LineGeometry mPickBoxPointer = new LineGeometry();
        EllipseGeometry mEllipse = new EllipseGeometry();
        UIElement mUIPickBoxPointer, LowerArm, UpperArm,Wrist , Gripper, mUIPickBoxCircle;

        // 6dof robot controller 
        public RoboticArmPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {

            if (eventArgs.Parameter as MaestroBoard != null)
            {
                maestroDevice = (eventArgs.Parameter as MaestroBoard).maestro;
                tbDeviceName.Text = maestroDevice.Name + " Connected";
               // DrawArms();
               // MoveArm();
            }
        }


        /* void DrawArms()
          {

              mCanvas.Children.Add(UpperArm = new Path { Data = lineUpperArm, StrokeThickness = 7, Stroke = new SolidColorBrush(Windows.UI.Colors.Blue) });
              mCanvas.Children.Add(LowerArm = new Path() { Data = lineLowerArm, StrokeThickness = 7, Stroke = new SolidColorBrush(Windows.UI.Colors.Green) });
              mCanvas.Children.Add(Wrist = new Path() { Data = lineWrist, StrokeThickness = 7, Stroke = new SolidColorBrush(Windows.UI.Colors.Purple) });
              mCanvas.Children.Add(mUIPickBoxPointer = new Path() { Data = mPickBoxPointer, Stroke = new SolidColorBrush(Windows.UI.Colors.Black) });
              mCanvas.Children.Add(mUIPickBoxCircle = new Path() { Data = mEllipse, Stroke = new SolidColorBrush(Windows.UI.Colors.Red), Fill = new SolidColorBrush(Windows.UI.Colors.Red) });
              Canvas.SetLeft(LowerArm, 200);
              Canvas.SetTop(LowerArm, 200);
              Canvas.SetLeft(UpperArm, 200);
              Canvas.SetTop(UpperArm, 200);
              Canvas.SetLeft(Wrist, 200);
              Canvas.SetTop(Wrist, 200);
              Canvas.SetLeft(Gripper, 200);
              Canvas.SetTop(Gripper, 200);
              Canvas.SetLeft(mUIPickBoxPointer, 200);
              Canvas.SetTop(mUIPickBoxPointer, 200);
              Canvas.SetLeft(mUIPickBoxCircle, 200);
              Canvas.SetTop(mUIPickBoxCircle, 200);
          }

          /// <summary>
          /// Calculates the end point of arm1 and end point of pickBox pointer.
          /// We already know the start point of arm-1(Fixed point) and the end point of arm-2(i.e point p).
          /// </summary>
          /// <param name="p">End point of arm-2 (destination point of robot)</param>
          /// <returns>Arm1 end point</returns>
          Point Calculate(Point p, double arm1Len = 100, double arm2Len = 100)
          {
              double distance = GetDisBtwPts(p, mArm1StartPt),     // Distace btw arm1-start point to arm2-endpoint.
                 angle1 = GetSlopeAngle(mArm1StartPt, p), // Slope of arm 1
                 angle2 = Math.Acos((arm1Len * arm1Len + distance * distance - arm2Len * arm2Len) / (2 * distance * arm1Len)); //Angle between arm1 and line joining arm1Pt1 and point p. 
              p.X = arm1Len * Math.Cos(angle2 + angle1); p.Y = arm1Len * Math.Sin(angle2 + angle1); //Horizondal and vertical component of arm-1 gives the end point of arm-1.
              return p;
          }

          void MoveArm()
          {

                  lineUpperArm.StartPoint = UpperArmStartPt;
                  lineUpperArm.EndPoint = mArm2.StartPoint = mArm1EndPt;
                  mArm2.EndPoint = mArm2EndPt;
                  mPickBoxPointer.StartPoint = mArm2EndPt;
                  mPickBoxPointer.EndPoint = new Point(mArm2EndPt.X + 20 * Math.Cos(mPickAngle), mArm2EndPt.Y + 20 * Math.Sin(mPickAngle));
                  mEllipse.Center = mArm2EndPt;
                  mEllipse.RadiusX = mEllipse.RadiusY = 10;

          }

          double GetDisBtwPts(Point p1, Point p2)
          {
              return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
          }

          double GetSlopeAngle(Point p1, Point p2)
          {
              return Math.Atan2((p2.Y - p1.Y), (p2.X - p1.X));
          }

          void XSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
          {

              MoveArm();
          }

          void YSlider_ValueChanged(object sender,RangeBaseValueChangedEventArgs e)
          {
              mYPos = mYSlider.Value;
              MoveArm();
          }

          void Theta_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
          {
              mPickAngle = mTheta.Value;
              MoveArm();
          }*/


        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();

            }
        }

    }
}
