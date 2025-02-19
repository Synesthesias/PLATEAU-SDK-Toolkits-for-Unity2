﻿using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace PlateauToolkit.Sandbox.RoadNetwork
{
    public class GeometryTool
    {
        // Unity Spline 動的生成
        public static Spline CreateSplineFromPoints(List<Vector3> points)
        {
            Spline spline = new Spline();

            foreach (Vector3 point in points)
            {
                var knot = new BezierKnot(point);
                spline.Add(knot, TangentMode.AutoSmooth);
            }

            return spline;
        }

        // Unity Spline 上のPoint
        public static Vector3 GetPointOnSpline(Spline spline, float percent)
        {
            if (percent >= 1f)
            {
                return spline.EvaluatePosition(1f);
            }

            int splineIndex = (int)math.floor(percent);
            float t = math.frac(percent);
            if (splineIndex > 0 && t == 0f)
            {
                t = splineIndex - 1;
            }

            return spline.EvaluatePosition(t);
        }

        //　リニア（直線）
        public static Vector3 GetPointOnLine(List<Vector3> points, float percentage)
        {
            if (points == null || points.Count < 2)
            {
                return Vector3.zero;
            }


            if (percentage < 0f || percentage > 1f)
            {
                return points.Last();
            }


            // 総距離を計算
            float totalDistance = 0f;
            List<float> segmentDistances = new List<float>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                float distance = Vector3.Distance(points[i], points[i + 1]);
                segmentDistances.Add(distance);
                totalDistance += distance;
            }

            // 目標の距離（前線分のパーセント）を計算
            float targetDistance = totalDistance * percentage;

            // 線をたどりながら、目標の距離に達するまで進む
            float accumulatedDistance = 0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                float segmentDistance = segmentDistances[i];
                if (accumulatedDistance + segmentDistance >= targetDistance)
                {
                    // この区間で目標の距離に達した場合、補間で位置を求める
                    float remainingDistance = targetDistance - accumulatedDistance;
                    float t = remainingDistance / segmentDistance; // 0から1までの補間係数
                    return Vector3.Lerp(points[i], points[i + 1], t);
                }
                accumulatedDistance += segmentDistance;
            }

            // 計算が範囲外の場合、最後の点を返す（通常は起こらない）
            return points.Last();
        }

        // 最も長い線の始点、終点
        public static (Vector3, Vector3) GetLongestLine(List<Vector3> points)
        {
            if (points == null || points.Count < 2)
                return (Vector3.zero, Vector3.zero);

            Vector3 longestStart = Vector3.zero;
            Vector3 longestEnd = Vector3.zero;
            float maxDistance = 0;

            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    float distance = Vector3.Distance(points[i], points[j]);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        longestStart = points[i];
                        longestEnd = points[j];
                    }
                }
            }

            return (longestStart, longestEnd);
        }

        // point1, point2を比較してtargetに対して左側に位置しているかの判定
        public static bool IsFacingLeft(Vector3 point1, Vector3 point2, Vector3 target)
        {
            Vector3 direction = point2 - point1;
            Vector3 planeNormal = Vector3.Cross(Vector3.up, direction);
            float dot = Vector3.Dot(planeNormal, target - point1);
            return dot < 0;
        }
    }
}
