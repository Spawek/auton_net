﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Auton.CarVision.Video;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;
using RANSAC.Functions;
using System.Diagnostics;

namespace VisionFilters.Filters.Lane_Mark_Detector
{
    /// <summary>
    /// Road modelling using Bezier
    /// </summary>
    public class ClusterLanes_BezierExperimental : ThreadSupplier<List<Point>, SimpleRoadModel> 
    {
        private Supplier<List<Point>> supplier;
        private double roadCenterDistAvg = 184; // estimated relative road distance [half of width]
        
        const double ROAD_CENTER_MIN = 175;
        const double ROAD_CENTER_MAX = 210;
        const int CENTER_PROBE_OFFSET = 10;
        const int MIN_POINTS_FOR_EACH = 280;
        const int MIN_POINTS_FOR_ONLY_ONE = 300;
        int imgWidth  = CamModel.Width;
        int imgHeight = CamModel.Height;
        int centerProbePoint,
            carCenter;
        
        private void ObtainSimpleModel(List<Point> lanes)
        {
            Bezier leftLane   = null;
            Bezier rightLane  = null;
            Bezier roadCenter = null;

//             if (lanes.Count > MIN_POINTS_FOR_ONLY_ONE)
//                 roadCenter = RANSAC.RANSAC.fitBezier(100, 3, (int)(lanes.Count * 0.75), 5, lanes);
// 
//             if (roadCenter != null) 
//                 create_model_from_single_line(ref roadCenter, ref leftLane, ref rightLane);
//             else if (lanes.Count > MIN_POINTS_FOR_EACH)// no one line mark can be matched. trying to find left and right and then again trying to find model.
//             {
//                 // try to cluster data to distinguish left and right lane
//                 List<Point> first = new List<Point>(6048);
//                 List<Point> second = new List<Point>(6048);
// 
//                 VisionToolkit.Two_Means_Clustering(lanes, ref first, ref second);
// 
//                 ////////////////////////////////////////////////////////////////
// 
//                 if (first.Count > MIN_POINTS_FOR_EACH)
//                     leftLane = RANSAC.RANSAC.fitBezier(100, 8, (int)(first.Count * 0.75), 15, first);
// 
//                 if (second.Count > MIN_POINTS_FOR_EACH)
//                     rightLane = RANSAC.RANSAC.fitBezier(100, 8, (int)(second.Count * 0.75), 15, second);
// 
//                 create_model_from_two_lanes(ref leftLane, ref rightLane, ref roadCenter);
//             }


            LastResult = new SimpleRoadModel(roadCenter, leftLane, rightLane);
            PostComplete();
        }

        private void create_model_from_two_lanes(ref Bezier leftLane, ref Bezier rightLane, ref Bezier roadCenter)
        {
            if (leftLane != null && rightLane != null)
            {
                // swap lanes if necessary
                if (leftLane.at(centerProbePoint) > rightLane.at(centerProbePoint))
                {
                    var t     = leftLane;
                    leftLane  = rightLane;
                    rightLane = leftLane;
                }

                // center is between left and right lane
                roadCenter = Bezier.merge(leftLane, rightLane);
            }
            else if (leftLane != null) // check if this is really a left lane
            {
                if (leftLane.at(centerProbePoint) > carCenter + 5) // this is right lane!!
                {
                    rightLane   = leftLane;
                    leftLane    = null;
                    roadCenter  = Bezier.Moved(rightLane, -roadCenterDistAvg);
                }
                else roadCenter = Bezier.Moved(leftLane, roadCenterDistAvg);
            }
            else if (rightLane != null) // check if this is really a left lane
            {
                if (rightLane.at(centerProbePoint) < carCenter + 5) // this is left lane!!
                {
                    leftLane    = rightLane;
                    rightLane   = null;
                    roadCenter  = Bezier.Moved(leftLane, roadCenterDistAvg);
                }
                else roadCenter = Bezier.Moved(rightLane, -roadCenterDistAvg);
            }
        }

        private void create_model_from_single_line(ref Bezier roadCenter, ref Bezier leftLane, ref Bezier rightLane)
        {
            double x = roadCenter.at(imgHeight - CENTER_PROBE_OFFSET);
            if (x < carCenter) {
                leftLane   = roadCenter;
                roadCenter = Bezier.Moved(leftLane, roadCenterDistAvg);
                rightLane  = Bezier.Moved(roadCenter, roadCenterDistAvg);
            }
            else {
                rightLane  = roadCenter;
                roadCenter = Bezier.Moved(rightLane, -roadCenterDistAvg);
                leftLane   = Bezier.Moved(roadCenter, -roadCenterDistAvg);
            }
        }


        public ClusterLanes_BezierExperimental(Supplier<List<Point>> supplier_)
        {
            supplier = supplier_;
            centerProbePoint = imgHeight - CENTER_PROBE_OFFSET;
            carCenter = imgWidth / 2;

            supplier.ResultReady += MaterialReady;
            Process += ObtainSimpleModel;
        }
    }

    /// <summary>
    /// Road modelling using parabolic equation
    /// </summary>
    public class ClusterLanes : ThreadSupplier<List<Point>, SimpleRoadModel> 
    {
        private Supplier<List<Point>> supplier;
        private double roadCenterDistAvg = 200; // estimated relative road distance [half of width]
        
        const double ROAD_CENTER_MIN = 175;
        const double ROAD_CENTER_MAX = 230;
        const int CENTER_PROBE_OFFSET = 10;
        const int MIN_POINTS_FOR_EACH = 280;
        const int MIN_POINTS_FOR_ONLY_ONE = 300;
        int imgWidth  = CamModel.Width;
        int imgHeight = CamModel.Height;
        int centerProbePoint,
            carCenter;
        
        private void ObtainSimpleModel(List<Point> lanes)
        {
            Parabola leftLane   = null;
            Parabola rightLane  = null;
            Parabola roadCenter = null;

            if (lanes.Count > MIN_POINTS_FOR_ONLY_ONE)
                roadCenter = RANSAC.RANSAC.fitParabola(700, 6, (int)(lanes.Count * 0.75), 5, lanes);

            if (roadCenter != null) 
                create_model_from_single_line(ref roadCenter, ref leftLane, ref rightLane);
            else if (lanes.Count > MIN_POINTS_FOR_EACH)// no one line mark can be matched. trying to find left and right and then again trying to find model.
            {
                // try to cluster data to distinguish left and right lane
                List<Point> first = new List<Point>(2048);
                List<Point> second = new List<Point>(2048);

                VisionToolkit.Two_Means_Clustering(lanes, ref first, ref second);

                ////////////////////////////////////////////////////////////////

                if (first.Count > MIN_POINTS_FOR_EACH)
                    leftLane = RANSAC.RANSAC.fitParabola(700, 6, (int)(first.Count * 0.75), 5, first);

                if (second.Count > MIN_POINTS_FOR_EACH)
                    rightLane = RANSAC.RANSAC.fitParabola(700, 6, (int)(second.Count * 0.75), 5, second);

                create_model_from_two_lanes(ref leftLane, ref rightLane, ref roadCenter);
            }

            CatmullRom cm = new CatmullRom(new Vec2[] {
                new Vec2(carCenter, 150),
                new Vec2(carCenter - 100, 250),
                new Vec2(carCenter + 10, 390),
                new Vec2(50, 460)
            });
            LastResult = new SimpleRoadModel(cm, leftLane, rightLane);
            PostComplete();
        }

        private void create_model_from_two_lanes(ref Parabola leftLane, ref Parabola rightLane, ref Parabola roadCenter)
        {
            if (leftLane != null && rightLane != null)
            {
                // swap lanes if necessary
                if (leftLane.at(centerProbePoint) > rightLane.at(centerProbePoint))
                {
                    var t     = leftLane;
                    leftLane  = rightLane;
                    rightLane = leftLane;
                }

                // center is between left and right lane
                roadCenter = Parabola.merge(leftLane, rightLane);

                // reestimate road center
                double new_road_width = ((rightLane.c - roadCenter.c) + (roadCenter.c - leftLane.c)) * 0.5 * 0.05 + roadCenterDistAvg * 0.95;
                roadCenterDistAvg = Math.Max(Math.Min(new_road_width, ROAD_CENTER_MAX), ROAD_CENTER_MAX);
            }
            else if (leftLane != null) // check if this is really a left lane
            {
                if (leftLane.at(centerProbePoint) > carCenter) // this is right lane!!
                {
                    rightLane   = leftLane;
                    leftLane    = null;
                    roadCenter  = Parabola.Moved(rightLane, -roadCenterDistAvg);
                }
                else roadCenter = Parabola.Moved(leftLane, roadCenterDistAvg);
            }
            else if (rightLane != null) // check if this is really a left lane
            {
                if (rightLane.at(centerProbePoint) <= carCenter) // this is left lane!!
                {
                    leftLane    = rightLane;
                    rightLane   = null;
                    roadCenter  = Parabola.Moved(leftLane, roadCenterDistAvg);
                }
                else roadCenter = Parabola.Moved(rightLane, -roadCenterDistAvg);
            }
        }

        private void create_model_from_single_line(ref Parabola roadCenter, ref Parabola leftLane, ref Parabola rightLane)
        {
            double x = roadCenter.at(imgHeight - CENTER_PROBE_OFFSET);
            if (x < carCenter) {
                leftLane   = roadCenter;
                roadCenter = Parabola.Moved(leftLane, roadCenterDistAvg);
                rightLane  = Parabola.Moved(roadCenter, roadCenterDistAvg);
            }
            else {
                rightLane  = roadCenter;
                roadCenter = Parabola.Moved(rightLane, -roadCenterDistAvg);
                leftLane   = Parabola.Moved(roadCenter, -roadCenterDistAvg);
            }
        }
        

        public ClusterLanes(Supplier<List<Point>> supplier_)
        {
            supplier = supplier_;
            centerProbePoint = imgHeight - CENTER_PROBE_OFFSET;
            carCenter = imgWidth / 2;

            supplier.ResultReady += MaterialReady;
            Process += ObtainSimpleModel;
        }
    }
}
