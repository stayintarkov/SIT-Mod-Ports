using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SphereOverlapTest
{
    enum Plane
    {
        XZ,
        XY,
        YZ,
    }

    public partial class SphereOverlapTestForm : Form
    {
        private Pen overallCirclePen = new Pen(Color.Black, 1);
        private Pen overlappingCirclesPen = new Pen(Color.Red, 1);
        private IEnumerable<UnityEngine.Vector3> overlappingCirclesSphere = Enumerable.Empty<UnityEngine.Vector3>();
        private IEnumerable<UnityEngine.Vector3> overlappingCirclesBox = Enumerable.Empty<UnityEngine.Vector3>();
        private UnityEngine.Bounds boxBounds;
        private static object circlesLockObj = new object();

        public SphereOverlapTestForm()
        {
            InitializeComponent();
        }

        private void RefreshSphere(object sender, EventArgs e)
        {
            lock (circlesLockObj)
            {
                overlappingCirclesSphere = GetOverlappingCircles(overallRadiusTrackBar.Value, maxYRadiusTrackBar.Value, 1f - minOverlapTrackBar.Value / 100.0f, minCirclesPerRingTrackBar.Value);
                totalCirclesValueLabel.Text = overlappingCirclesSphere.Count().ToString();
            }

            XYPanel.Refresh();
            XZPanel.Refresh();
        }

        private void RefreshBox(object sender, EventArgs e)
        {
            lock (circlesLockObj)
            {
                boxBounds = GetOverallBoxBounds(widthTrackBar.Value, lengthTrackBar.Value, heightTrackBar.Value);
                overlappingCirclesBox = GetOverlappingCircles(boxBounds, maxRadiusTrackBar.Value, 1f - minOverlapTrackBar2.Value / 100.0f);
                totalCirclesValueLabel2.Text = overlappingCirclesBox.Count().ToString();
            }

            XYPanel2.Refresh();
            XZPanel2.Refresh();
        }

        private void SphereXYPanelPaint(object sender, PaintEventArgs e)
        {
            Point center = new Point(XYPanel.Size.Width / 2, XYPanel.Size.Height / 2);

            Graphics g = e.Graphics;
            g.Clear(Color.White);
            DrawCircle(g, overallCirclePen, center.X, center.Y, overallRadiusTrackBar.Value);
            lock (circlesLockObj)
            {
                DrawCirclesFromOrigin(g, overlappingCirclesPen, center, overlappingCirclesSphere, maxYRadiusTrackBar.Value, Plane.XY);
            }
        }

        private void SphereXZPanelPaint(object sender, PaintEventArgs e)
        {
            Point center = new Point(XZPanel.Size.Width / 2, XZPanel.Size.Height / 2);

            Graphics g = e.Graphics;
            g.Clear(Color.White);
            DrawCircle(g, overallCirclePen, center.X, center.Y, overallRadiusTrackBar.Value);
            lock (circlesLockObj)
            {
                DrawCirclesFromOrigin(g, overlappingCirclesPen, center, overlappingCirclesSphere, maxYRadiusTrackBar.Value, Plane.XZ);
            }
        }

        private void BoxXYPanelPaint(object sender, PaintEventArgs e)
        {
            Point center = new Point(XYPanel.Size.Width / 2, XYPanel.Size.Height / 2);

            Graphics g = e.Graphics;
            g.Clear(Color.White);
            DrawBox(g, overallCirclePen, center, boxBounds, Plane.XY);
            lock (circlesLockObj)
            {
                DrawCirclesFromOrigin(g, overlappingCirclesPen, center, overlappingCirclesBox, maxRadiusTrackBar.Value, Plane.XY);
            }
        }

        private void BoxXZPanelPaint(object sender, PaintEventArgs e)
        {
            Point center = new Point(XZPanel.Size.Width / 2, XZPanel.Size.Height / 2);

            Graphics g = e.Graphics;
            g.Clear(Color.White);
            DrawBox(g, overallCirclePen, center, boxBounds, Plane.XZ);
            lock (circlesLockObj)
            {
                DrawCirclesFromOrigin(g, overlappingCirclesPen, center, overlappingCirclesBox, maxRadiusTrackBar.Value, Plane.XZ);
            }
        }

        private UnityEngine.Bounds GetOverallBoxBounds(float width, float length, float height)
        {
            return new UnityEngine.Bounds(UnityEngine.Vector3.zero, new UnityEngine.Vector3(width, height, length));
        }

        private IEnumerable<UnityEngine.Vector3> GetOverlappingCircles(float overallRadius, float maxYRadius, float minOverlap, int minCirclesPerRing)
        {
            if (overallRadius <= maxYRadius)
            {
                return Enumerable.Empty<UnityEngine.Vector3>();
            }
            
            List<UnityEngine.Vector3> circles = new List<UnityEngine.Vector3>
            {
                new UnityEngine.Vector3(0, 0, 0)
            };

            int rings = (int)Math.Max(Math.Ceiling((overallRadius - 3 * maxYRadius) / (2 * maxYRadius * minOverlap)), 1);
            float ringStepSize = Math.Max(overallRadius - 3 * maxYRadius, maxYRadius) / rings;
            float minRad = Math.Min(maxYRadius, overallRadius - maxYRadius);
            for (float ringRad = overallRadius - maxYRadius; ringRad >= minRad; ringRad -= ringStepSize)
            {
                int ringCircles = (int)Math.Max(Math.Ceiling(Math.PI * 2 / minCirclesPerRing * ringRad / (2 * maxYRadius * minOverlap)), 1) * minCirclesPerRing;
                float ringAngStepSize = (float)Math.PI * 2 / ringCircles;
                
                for (float ringAng = 0; ringAng < Math.PI * 2; ringAng += ringAngStepSize)
                {
                    circles.Add(new UnityEngine.Vector3(0 + (float)Math.Sin(ringAng) * ringRad, 0, 0 + (float)Math.Cos(ringAng) * ringRad));
                }
            }

            return circles;
        }

        private IEnumerable<UnityEngine.Vector3> GetOverlappingCircles(UnityEngine.Bounds boxBounds, float maxRadius, float minOverlap)
        {
            if ((boxBounds.extents.x <= maxRadius) || (boxBounds.extents.y <= maxRadius) || (boxBounds.extents.z <= maxRadius))
            {
                return Enumerable.Empty<UnityEngine.Vector3>();
            }

            UnityEngine.Vector3 origin = new UnityEngine.Vector3(boxBounds.min.x + maxRadius, boxBounds.min.y + maxRadius, boxBounds.min.z + maxRadius);

            List<UnityEngine.Vector3> circles = new List<UnityEngine.Vector3>();

            int widthCount = (int)Math.Max(1, Math.Ceiling((boxBounds.size.x - (maxRadius * 2)) / (2 * maxRadius * minOverlap)));
            int lengthCount = (int)Math.Max(1, Math.Ceiling((boxBounds.size.z - (maxRadius * 2)) / (2 * maxRadius * minOverlap)));
            int heightCount = (int)Math.Max(1, Math.Ceiling((boxBounds.size.y - (maxRadius * 2)) / (2 * maxRadius * minOverlap)));

            float widthSpacing = Math.Max(0, (boxBounds.size.x - (maxRadius * 2)) / widthCount);
            float lengthSpacing = Math.Max(0, (boxBounds.size.z - (maxRadius * 2)) / lengthCount);
            float heightSpacing = Math.Max(0, (boxBounds.size.y - (maxRadius * 2)) / heightCount);

            for (int x = 0; x <= widthCount; x++)
            {
                for (int y = 0; y <= heightCount; y++)
                {
                    for (int z = 0; z <= lengthCount; z++)
                    {
                        circles.Add(new UnityEngine.Vector3(origin.x + (widthSpacing * x), origin.y + (heightSpacing * y), origin.z + (lengthSpacing * z)));
                    }
                }
            }

            return circles;
        }

        private void DrawCirclesFromOrigin(Graphics g, Pen pen, Point origin, IEnumerable<UnityEngine.Vector3> circles, float radius, Plane plane)
        {
            foreach (UnityEngine.Vector3 circle in circles)
            {
                DrawCircleFromOrigin(g, pen, origin, circle, radius, plane);
            }
        }

        private void DrawCircleFromOrigin(Graphics g, Pen pen, Point origin, UnityEngine.Vector3 circle, float radius, Plane plane)
        {
            Point circleCenterPoint;
            switch (plane)
            {
                case Plane.XZ:
                case Plane.YZ:
                    circleCenterPoint = new Point(origin.X + (int)circle.x, origin.Y + (int)circle.z);
                    break;
                case Plane.XY:
                    circleCenterPoint = new Point(origin.X + (int)circle.x, origin.Y + (int)circle.y);
                    break;
                default:
                    throw new ArgumentException("Invalid plane: " + plane, "plane");
            }

            DrawCircle(g, pen, circleCenterPoint, radius);
        }

        private void DrawCircle(Graphics g, Pen pen, Point center, float radius)
        {
            DrawEllipse(g, pen, center.X, center.Y, radius, radius);
        }

        private void DrawCircle(Graphics g, Pen pen, int centerX, int centerY, float radius)
        {
            DrawEllipse(g, pen, centerX, centerY, radius, radius);
        }

        private void DrawEllipse(Graphics g, Pen pen, int centerX, int centerY, float xRadius, float yRadius)
        {
            g.DrawEllipse(pen, centerX - xRadius, centerY -  yRadius, xRadius * 2, yRadius * 2);
        }

        private void DrawBox(Graphics g, Pen pen, Point origin, UnityEngine.Bounds boxBounds, Plane plane)
        {
            switch (plane)
            {
                case Plane.XZ:
                    g.DrawLine(pen, new Point(origin.X + (int)boxBounds.min.x, origin.Y + (int)boxBounds.min.z), new Point(origin.X + (int)boxBounds.max.x, origin.Y + (int)boxBounds.min.z));
                    g.DrawLine(pen, new Point(origin.X + (int)boxBounds.min.x, origin.Y + (int)boxBounds.min.z), new Point(origin.X + (int)boxBounds.min.x, origin.Y + (int)boxBounds.max.z));
                    g.DrawLine(pen, new Point(origin.X + (int)boxBounds.min.x, origin.Y + (int)boxBounds.max.z), new Point(origin.X + (int)boxBounds.max.x, origin.Y + (int)boxBounds.max.z));
                    g.DrawLine(pen, new Point(origin.X + (int)boxBounds.max.x, origin.Y + (int)boxBounds.min.z), new Point(origin.X + (int)boxBounds.max.x, origin.Y + (int)boxBounds.max.z));
                    break;
                case Plane.YZ:
                    g.DrawLine(pen, new Point(origin.X + (int)boxBounds.min.y, origin.Y + (int)boxBounds.min.z), new Point(origin.X + (int)boxBounds.max.y, origin.Y + (int)boxBounds.min.z));
                    g.DrawLine(pen, new Point(origin.X + (int)boxBounds.min.y, origin.Y + (int)boxBounds.min.z), new Point(origin.X + (int)boxBounds.min.y, origin.Y + (int)boxBounds.max.z));
                    g.DrawLine(pen, new Point(origin.X + (int)boxBounds.min.y, origin.Y + (int)boxBounds.max.z), new Point(origin.X + (int)boxBounds.max.y, origin.Y + (int)boxBounds.max.z));
                    g.DrawLine(pen, new Point(origin.X + (int)boxBounds.max.y, origin.Y + (int)boxBounds.min.z), new Point(origin.X + (int)boxBounds.max.y, origin.Y + (int)boxBounds.max.z));
                    break;
                case Plane.XY:
                    g.DrawLine(pen, new Point(origin.X + (int)boxBounds.min.x, origin.Y + (int)boxBounds.min.y), new Point(origin.X + (int)boxBounds.max.x, origin.Y + (int)boxBounds.min.y));
                    g.DrawLine(pen, new Point(origin.X + (int)boxBounds.min.x, origin.Y + (int)boxBounds.min.y), new Point(origin.X + (int)boxBounds.min.x, origin.Y + (int)boxBounds.max.y));
                    g.DrawLine(pen, new Point(origin.X + (int)boxBounds.min.x, origin.Y + (int)boxBounds.max.y), new Point(origin.X + (int)boxBounds.max.x, origin.Y + (int)boxBounds.max.y));
                    g.DrawLine(pen, new Point(origin.X + (int)boxBounds.max.x, origin.Y + (int)boxBounds.min.y), new Point(origin.X + (int)boxBounds.max.x, origin.Y + (int)boxBounds.max.y));
                    break;
                default:
                    throw new ArgumentException("Invalid plane: " + plane, "plane");
            }
        }
    }
}
