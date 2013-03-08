﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Auton.CarVision.Video;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;
using RANSAC.Functions;

namespace VisionFilters.Filters.Lane_Mark_Detector
{
    /// <summary>
    /// Road modeling using parabolic equation
    /// </summary>
    public class ClusterLanes_Quadratic : ThreadSupplier<List<Point>, SimpleRoadModel> 
    {
        private Supplier<List<Point>> supplier;
        private double roadCenterDistAvg = 200; // estimated relative road distance [half of width]
        
        const double ROAD_CENTER_MIN = 175;
        const double ROAD_CENTER_MAX = 230;
        const int CENTER_PROBE_OFFSET = 10;
        const int MIN_POINTS_FOR_EACH = 280;
        const int MIN_POINTS_FOR_ONLY_ONE = 300;

        const int RANSAC_ITERATIONS = 700;
        const int RANSAC_MODEL_SIZE = 6;
        const int RANSAC_MAX_ERROR = 5;
        const double RANSAC_INLINERS = 0.75;
        
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
                roadCenter = RANSAC.RANSAC.fitParabola(RANSAC_ITERATIONS, RANSAC_MODEL_SIZE, (int)(lanes.Count * RANSAC_INLINERS), RANSAC_MAX_ERROR, lanes);

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
                    leftLane = RANSAC.RANSAC.fitParabola(RANSAC_ITERATIONS, RANSAC_MODEL_SIZE, (int)(first.Count * RANSAC_INLINERS), RANSAC_MAX_ERROR, first);

                if (second.Count > MIN_POINTS_FOR_EACH)
                    rightLane = RANSAC.RANSAC.fitParabola(RANSAC_ITERATIONS, RANSAC_MODEL_SIZE, (int)(second.Count * RANSAC_INLINERS), RANSAC_MAX_ERROR, second);

                create_model_from_two_lanes(ref leftLane, ref rightLane, ref roadCenter);
            }

            LastResult = new SimpleRoadModel(roadCenter, leftLane, rightLane);
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
        

        public ClusterLanes_Quadratic(Supplier<List<Point>> supplier_)
        {
            supplier = supplier_;
            centerProbePoint = imgHeight - CENTER_PROBE_OFFSET;
            carCenter = imgWidth / 2;

            supplier.ResultReady += MaterialReady;
            Process += ObtainSimpleModel;
        }
    }
}