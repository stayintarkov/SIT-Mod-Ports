using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LateToTheParty.Controllers;
using UnityEngine;

namespace LateToTheParty.Models
{
    public class PathAccessibilityData
    {
        public bool IsAccessible { get; set; } = false;
        public PathVisualizationData PathData { get; set; }
        public PathVisualizationData LastNavPointOutline { get; set; }
        public PathVisualizationData PathEndPointData { get; set; }
        public PathVisualizationData LootOutlineData { get; set; }
        public List<PathVisualizationData> BoundingBoxes { get; set; } = new List<PathVisualizationData>();
        public List<PathVisualizationData> RaycastHitMarkers { get; set; } = new List<PathVisualizationData>();

        public PathAccessibilityData()
        {

        }

        public void Merge(PathAccessibilityData other)
        {
            IsAccessible |= other.IsAccessible;

            if (other.PathData != null)
            {
                if (PathData != null)
                {
                    PathData.Replace(other.PathData);
                }
                else
                {
                    PathData = other.PathData;
                }
            }
            if (other.LastNavPointOutline != null)
            {
                if (LastNavPointOutline != null)
                {
                    LastNavPointOutline.Replace(other.LastNavPointOutline);
                }
                else
                {
                    LastNavPointOutline = other.LastNavPointOutline;
                }
            }
            if (other.PathEndPointData != null)
            {
                if (PathEndPointData != null)
                {
                    PathEndPointData.Replace(other.PathEndPointData);
                }
                else
                {
                    PathEndPointData = other.PathEndPointData;
                }
            }
            if (other.LootOutlineData != null)
            {
                if (LootOutlineData != null)
                {
                    LootOutlineData.Replace(other.LootOutlineData);
                }
                else
                {
                    LootOutlineData = other.LootOutlineData;
                }
            }

            if (other.BoundingBoxes.Count > 0)
            {
                foreach (PathVisualizationData data in BoundingBoxes)
                {
                    PathRender.RemovePath(data);
                }
                BoundingBoxes.Clear();
                foreach (PathVisualizationData data in other.BoundingBoxes)
                {
                    BoundingBoxes.Add(data);
                }
            }

            if (other.RaycastHitMarkers.Count > 0)
            {
                foreach (PathVisualizationData data in RaycastHitMarkers)
                {
                    PathRender.RemovePath(data);
                }
                RaycastHitMarkers.Clear();
                foreach (PathVisualizationData data in other.RaycastHitMarkers)
                {
                    RaycastHitMarkers.Add(data);
                }
            }
        }

        public void MergeAndUpdate(PathAccessibilityData other)
        {
            Merge(other);
            Update();
        }

        public void Update()
        {
            PathRender.AddOrUpdatePath(PathData);
            PathRender.AddOrUpdatePath(LastNavPointOutline);
            PathRender.AddOrUpdatePath(PathEndPointData);
            PathRender.AddOrUpdatePath(LootOutlineData);

            foreach (PathVisualizationData data in BoundingBoxes)
            {
                PathRender.AddOrUpdatePath(data);
            }
            foreach (PathVisualizationData data in RaycastHitMarkers)
            {
                PathRender.AddOrUpdatePath(data);
            }
        }

        public void Clear(bool keepLootOutline = false)
        {
            if (PathData != null)
            {
                PathRender.RemovePath(PathData);
                PathData.Clear();
            }
            if (LastNavPointOutline != null)
            {
                PathRender.RemovePath(LastNavPointOutline);
                LastNavPointOutline.Clear();
            }
            if (PathEndPointData != null)
            {
                PathRender.RemovePath(PathEndPointData);
                PathEndPointData.Clear();
            }

            if (!keepLootOutline)
            {
                if (LootOutlineData != null)
                {
                    PathRender.RemovePath(LootOutlineData);
                    LootOutlineData.Clear();
                }
            }

            foreach (PathVisualizationData data in BoundingBoxes)
            {
                PathRender.RemovePath(data);
                data.Clear();
            }
            BoundingBoxes.Clear();

            foreach (PathVisualizationData data in RaycastHitMarkers)
            {
                PathRender.RemovePath(data);
                data.Clear();
            }
            RaycastHitMarkers.Clear();
        }
    }
}
